using System;
using System.Collections.Generic;
using System.Text;

namespace Inu.Linker
{
    class External
    {
        public readonly int Id;
        public readonly int ObjIndex;
        public readonly int Offset;

        public External(int id, int objIndex, int offset)
        {
            Id = id;
            ObjIndex = objIndex;
            Offset = offset;
        }
    }
}
