using Inu.Assembler;

namespace Inu.Linker
{
    class Symbol
    {
        public int Id { get; private set; }
        public Address Address { get; set; }
        public int ObjIndex { get; private set; }
        public Symbol(int id, Address address, int objIndex)
        {
            Id = id;
            Address = address;
            this.ObjIndex = objIndex;
        }
    }
}
