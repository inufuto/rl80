using Inu.Assembler;
using Inu.Language;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace Inu.Linker
{
    abstract class Linker
    {
        public const int Failure = 1;
        public const int Success = 0;

        private readonly int[] addresses = new int[(int)AddressType.SegmentCount];
        private readonly Segment[] segments = new Segment[] { new Segment(AddressType.Code), new Segment(AddressType.Data) };
        private string? targetName;
        private readonly List<string> objNames = new List<string>();
        private readonly StringTable identifiers = new StringTable(1);
        private readonly Dictionary<int, Symbol> symbols = new Dictionary<int, Symbol>();
        private readonly SortedDictionary<Address, External> externals = new SortedDictionary<Address, External>();
        private readonly List<string> errors = new List<string>();

        public int Main(string[] args)
        {
            if (args.Length < 1) {
                Console.Error.WriteLine("No target file.");
                return Failure;
            }
            targetName = args[0];
            var directory = Path.GetDirectoryName(targetName);
            if (string.IsNullOrEmpty(directory)) {
                directory = Directory.GetCurrentDirectory();
            }

            if (args.Length < 2) {
                Console.Error.WriteLine("No code address.");
                return Failure;
            }
            addresses[(int)AddressType.Code] = int.Parse(args[1], NumberStyles.AllowHexSpecifier);

            if (args.Length < 3) {
                Console.Error.WriteLine("No data address.");
                return Failure;
            }
            addresses[(int)AddressType.Data] = int.Parse(args[2], NumberStyles.AllowHexSpecifier);

            if (args.Length < 4) {
                Console.Error.WriteLine("No object file.");
                return Failure;
            }

            int objIndex = 0;
            for (int i = 3; i < args.Length; ++i) {
                string objName = args[i];
                objNames.Add(objName);
                var objDirect = Path.GetDirectoryName(objName);
                if (string.IsNullOrEmpty(objDirect)) {
                    objName = directory + Path.DirectorySeparatorChar + objName;
                }
                ReadObjectFile(objName, objIndex++);
                if (errors.Count > 0) {
                    return Failure;
                }
            }

            ResolveExternals();
            if (errors.Count > 0) return Failure;

            SaveTargetFile(targetName);

            string symbolFileName = directory + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(targetName) + ".symbols.txt";
            SaveSymbolFile(symbolFileName);

            return errors.Count <= 0 ? Success : Failure;
        }

        protected abstract byte[] ToBytes(int value);

        private void ShowError(string error)
        {
            errors.Add(error);
            Console.Error.WriteLine(error);
        }

        private static Dictionary<int, string> ReadIdentifiers(Stream stream)
        {
            Dictionary<int, string> identifiers = new Dictionary<int, string>();
            int n = stream.ReadWord();
            for (int i = 0; i < n; ++i) {
                int id = stream.ReadWord();
                string name = stream.ReadString();
                identifiers[id] = name;
            }
            return identifiers;
        }

        private void RegisterSymbol(string name, Address address, int objIndex)
        {
            int id = identifiers.Add(name);
            if (symbols.TryGetValue(id, out var symbol)) {
                if (symbol.Address.Type == AddressType.External) {
                    if (address.Type != AddressType.External) {
                        symbol.Address = address;
                    }
                }
                else if (address.Type != AddressType.External) {
                    ShowError("Duplicated symbol: " + name + "\n\t" + objNames[symbol.ObjIndex] + "\n\t" + objNames[objIndex]);
                }
            }
            else {
                symbols[id] = new Symbol(id, address, objIndex);
            }
        }

        private void ReadSymbols(Stream stream, int[] offsets, Dictionary<int, string> identifiers, int objIndex)
        {
            int n = stream.ReadWord();
            for (int i = 0; i < n; ++i) {
                int id = stream.ReadWord();
                Address address = new Address(stream);
                string name = identifiers[id];
                switch (address.Type) {
                    case AddressType.Undefined:
                    case AddressType.External:
                        Debug.Assert(false);
                        break;
                    case AddressType.Const:
                        RegisterSymbol(name, address, objIndex);
                        break;
                    default:
                        Debug.Assert(address.Type >= 0 && address.Type < AddressType.SegmentCount);
                        address = address.Add(offsets[(int)address.Type]);
                        RegisterSymbol(name, address, objIndex);
                        break;
                }
            }
        }

        private void ReadAddressUsages(Stream stream, int[] offsets, Dictionary<int, string> identifiers, int objIndex)
        {
            int n = stream.ReadWord();
            for (int i = 0; i < n; ++i) {
                Address location = new Address(stream);
                Debug.Assert(location.Type >= 0 && location.Type < AddressType.SegmentCount);
                location = location.Add(offsets[(int)location.Type]);
                Address value = new Address(stream);
                if (value.Type == AddressType.External) {
                    Debug.Assert(value.Id != null);
                    string name = identifiers[value.Id.Value];
                    int id = this.identifiers.Add(name);
                    externals[location] = new External(id, objIndex, value.Value, value.Part);
                }
                else {
                    Debug.Assert(value.Type >= 0 && value.Type < AddressType.SegmentCount);
                    value = value.Add(offsets[(int)value.Type]);
                    FixAddress(location, value, 0, value.Part);
                }
            }
        }

        private void FixAddress(Address location, Address value, int offset, AddressPart part)
        {
            Debug.Assert(location.Type >= 0 && location.Type < AddressType.SegmentCount);
            Debug.Assert(value.Type == AddressType.Const || value.Type >= 0 && value.Type < AddressType.SegmentCount);
            int address = value.Value;
            if (value.Type >= 0) {
                address += addresses[(int)value.Type];
            }

            var addedValue = address + offset;
            switch (part) {
                case AddressPart.Word:
                    segments[(int)location.Type].WriteAddress(location.Value, ToBytes(addedValue));
                    break;
                case AddressPart.LowByte:
                    segments[(int)location.Type].Bytes[location.Value] = (byte)(addedValue & 0xff);
                    break;
                case AddressPart.HighByte:
                    segments[(int)location.Type].Bytes[location.Value] = (byte)((addedValue >> 8) & 0xff);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ReadObjectFile(string fileName, int objIndex)
        {
            if (!File.Exists(fileName)) {
                ShowError("File not found: " + fileName);
                return;
            }

            using Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            stream.ReadWord();  // version
            int[] offsets = new int[(int)AddressType.SegmentCount];
            var segmentIndex = 0;
            foreach (Segment segment in segments) {
                offsets[segmentIndex++] = segment.Size;
                var n = stream.ReadWord();
                if (n <= 0) continue;
                byte[] bytes = new byte[n];
                stream.Read(bytes, 0, n);
                segment.Append(bytes);
            }

            Dictionary<int, string> identifiers = ReadIdentifiers(stream);
            ReadSymbols(stream, offsets, identifiers, objIndex);
            ReadAddressUsages(stream, offsets, identifiers, objIndex);
        }

        private void ResolveExternals()
        {
            foreach (KeyValuePair<Address, External> pair in externals) {
                var external = pair.Value;
                if (symbols.TryGetValue(external.Id, out var symbol)) {
                    FixAddress(pair.Key, symbol.Address, external.Offset, external.Part);
                }
                else {
                    var name = identifiers.FromId(external.Id);
                    ShowError("Undefined external: " + name + " in " + objNames[external.ObjIndex]);
                }
            }
        }


        private void SaveTargetFile(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToUpper();
            SaveTargetFile(fileName, ext);
        }

        private static void PrintColumn(StreamWriter writer, string s, int maxLength)
        {
            writer.Write(s);
            int n = maxLength + 2 - s.Length;
            for (int i = 0; i < n; ++i) {
                writer.Write(' ');
            }
        }

        private static string ToHex(int addressValue)
        {
            return string.Format("{0:X04}", addressValue);
        }

        private const int AddressColumnLength = 5;

        private void SaveSymbolFile(string fileName)
        {
            using (StreamWriter stream = new StreamWriter(fileName, false, Encoding.UTF8)) {
                int maxNameLength = 0;
                int maxFileNameLength = 0;
                SortedDictionary<string, Symbol> nameIndexedSymbols = new SortedDictionary<string, Symbol>();
                foreach (KeyValuePair<int, Symbol> pair in symbols) {
                    var name = identifiers.FromId(pair.Key);
                    Debug.Assert(name != null);
                    string objName = objNames[pair.Value.ObjIndex];
                    maxNameLength = Math.Max(name.Length, maxNameLength);
                    maxFileNameLength = Math.Max(objName.Length, maxFileNameLength);
                    nameIndexedSymbols[name] = pair.Value;
                }
                PrintColumn(stream, "Symbol", maxNameLength);
                PrintColumn(stream, "Value", AddressColumnLength);
                PrintColumn(stream, "File", maxNameLength);
                stream.WriteLine();
                for (int i = 0; i < (maxNameLength + AddressColumnLength + maxFileNameLength) * 4 / 3; ++i) {
                    stream.Write('=');
                }
                stream.WriteLine();
                foreach (KeyValuePair<string, Symbol> pair in nameIndexedSymbols) {
                    string name = pair.Key;
                    string objName = objNames[pair.Value.ObjIndex];
                    Address address = pair.Value.Address;

                    int addressValue;
                    if (address.Type == AddressType.Const) {
                        addressValue = address.Value;
                    }
                    else {
                        Debug.Assert(address.Type >= 0 && address.Type < AddressType.SegmentCount);
                        addressValue = addresses[(int)address.Type] + address.Value;
                    }
                    PrintColumn(stream, name, maxNameLength);
                    PrintColumn(stream, ToHex(addressValue), AddressColumnLength);
                    PrintColumn(stream, objName, maxFileNameLength);
                    stream.WriteLine();
                }

                stream.WriteLine();
                stream.WriteLine("CSEG " + ToHex(addresses[0]) + '-' + ToHex(addresses[0] + (segments[0].Size - 1)));
                stream.WriteLine("DSEG " + ToHex(addresses[1]) + '-' + ToHex(addresses[1] + (segments[1].Size - 1)));
            }
        }

        protected virtual void SaveTargetFile(string fileName, string ext)
        {
            using (BinFile file = new BinFile(fileName)) {
                SaveTargetFile(file);
            }
        }

        protected void SaveTargetFile(TargetFile targetFile)
        {
            targetFile.Write(addresses[0], segments[0].Bytes.ToArray());
        }
    }
}
