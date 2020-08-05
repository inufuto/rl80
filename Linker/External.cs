using System;
using System.Collections.Generic;
using System.Text;
using Inu.Assembler;

namespace Inu.Linker
{
    class External
    {
        public readonly int Id;
        public readonly int ObjIndex;
        public readonly int Offset;
        public readonly AddressPart Part;

        public External(int id, int objIndex, int offset, AddressPart part)
        {
            Id = id;
            ObjIndex = objIndex;
            Offset = offset;
            Part = part;
        }
    }
}
