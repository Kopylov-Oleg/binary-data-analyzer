using System;

namespace BinaryDataAnalyzer
{
    internal partial class BinaryReaderFacade
    {
        public class EofException : Exception
        {
            public EofException()
            {
            }

            public EofException(string message)
                : base(message)
            {
            }

            public EofException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }
}
