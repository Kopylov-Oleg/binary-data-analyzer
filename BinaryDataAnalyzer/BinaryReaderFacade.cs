using System;
using System.IO;

namespace BinaryDataAnalyzer
{
    internal partial class BinaryReaderFacade : IDisposable
    {
        private FileStream inputFile;

        public BinaryReaderFacade(string filePath)
        {
            inputFile = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }

        public void Dispose()
        {
            inputFile?.Dispose();
        }

        public byte ReadByte()
        {
            var tmp = inputFile.ReadByte();
            if (tmp == -1)
            {
                throw new EofException();
            }

            return (byte)tmp;
        }

        public byte[] ReadBytes(int bytesCount)
        {
            var bytes = new byte[bytesCount];
            if (inputFile.Read(bytes, 0, bytesCount) < bytesCount)
            {
                throw new EofException();
            }

            return bytes;
        }

        public void MoveBack(int bytesCount)
        {
            inputFile.Seek(-bytesCount, SeekOrigin.Current);
        }
    }
}
