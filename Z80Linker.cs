using System;
using System.Collections.Generic;
using System.Text;

namespace Inu.Linker.Z80
{
    class Z80Linker : Linker
    {
        protected override void SaveTargetFile(string fileName, string ext)
        {
            switch (ext)
            {
                case ".CMT":
                {
                    using CmtFile file = new CmtFile(fileName);
                    base.SaveTargetFile(file);
                    break;
                }
                case ".P6":
                {
                    using P6File file = new P6File(fileName);
                    base.SaveTargetFile(file);
                    break;
                }
                case ".MZT":
                {
                    using MztFile file = new MztFile(fileName);
                    base.SaveTargetFile(file);
                    break;
                }
                case ".CAS":
                {
                    using CasFile file = new CasFile(fileName);
                    base.SaveTargetFile(file);
                    break;
                }
                case ".RAM":
                {
                    using RamPakFile file = new RamPakFile(fileName);
                    base.SaveTargetFile(file);
                    break;
                }
                case ".HEX":
                {
                    using HexFile file = new HexFile(fileName);
                    base.SaveTargetFile(file);
                    break;
                }
                default:
                    base.SaveTargetFile(fileName, ext);
                    break;
            }
        }

        protected override byte[] ToBytes(int value)
        {
            return new byte[] {
                (byte)(value & 0xff),
                (byte)((value >> 8) & 0xff)
            };
        }
    }
}
