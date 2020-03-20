using System;
using System.Collections.Generic;
using System.Text;

namespace Inu.Linker
{
    class External
    {
        public int Id { get; private set; }
        public int ObjIndex { get; private set; }
        public External(int id, int objIndex)
        {
            Id = id;
            this.ObjIndex = objIndex;
        }
    }
}
