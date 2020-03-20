using System;
using System.IO;
using System.Text;

namespace Inu.Linker
{
    abstract class AsciiTargetFile : TargetFile, IDisposable
    {
        protected StreamWriter Writer { get; private set; }

        public AsciiTargetFile(string fileName)
        {
            Writer = new StreamWriter(fileName, false, Encoding.ASCII);
        }

        public void Dispose()
        {
            Writer.Dispose();
        }
    }
}
