using System;
using System.Collections.Generic;
using System.Text;

namespace Inu.Linker.Z80
{
    class Z80Linker : Linker
    {
        protected override void SaveTargetFile(string fileName, string ext)
        {
            if (ext == ".CMT") {
                using (CmtFile file = new CmtFile(fileName)) {
                    base.SaveTargetFile(file);
                }
            }
            else if (ext == ".P6") {
                using (P6File file = new P6File(fileName)) {
                    base.SaveTargetFile(file);
                }
            }
            else if (ext == ".MZT") {
                using (MztFile file = new MztFile(fileName)) {
                    base.SaveTargetFile(file);
                }
            }
            else if (ext == ".HEX") {
                using (HexFile file = new HexFile(fileName)) {
                    base.SaveTargetFile(file);
                }
            }
            else {
                base.SaveTargetFile(fileName, ext);
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
