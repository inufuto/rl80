using System;
using System.Collections.Generic;

namespace Inu.Linker
{
    abstract class TargetFile
    {
        public abstract void Write(int address, byte[] bytes);

        protected int Write(int address, byte[] bytes, int maxRecordSize)
        {
            int offset = 0;
            while (offset < bytes.Length) {
                int recordSize = Math.Min(maxRecordSize, bytes.Length - offset);
                WriteRecord(address + offset, bytes, offset, recordSize);
                offset += recordSize;
            }
            return offset;
        }

        protected abstract void WriteRecord(int address, byte[] bytes, int offset, int recordSize);
    }
}
