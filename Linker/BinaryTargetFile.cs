using System;
using System.IO;

namespace Inu.Linker
{
    abstract class BinaryTargetFile : TargetFile, IDisposable
    {
        protected Stream Stream { get; private set; }

        public BinaryTargetFile(string fileName)
        {
            Stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
        }

        public void Dispose()
        {
            Stream.Dispose();
        }
    }
}