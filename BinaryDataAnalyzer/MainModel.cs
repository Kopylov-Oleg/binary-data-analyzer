using System;
using System.Collections.Generic;
using System.Windows.Controls;
using static BinaryDataAnalyzer.BinaryReaderFacade;

namespace BinaryDataAnalyzer
{
    internal class MainModel
    {
        public IEnumerable<FrameTypeStatistics> AnalyzeFile(string filePath)
        {
            using var binaryReader = new BinaryReaderFacade(filePath);

            var frameAnalyzer = new FrameAnalyzer();

            try
            {
                for (; ; )
                {
                    frameAnalyzer.AddByte(binaryReader.ReadByte());

                    if (frameAnalyzer.IsFrame())
                    {
                        frameAnalyzer.ProcessPotentialFrame(binaryReader);
                    }
                }
            }
            catch(EofException)
            {
                return frameAnalyzer.FrameTypesStatistics.Values;
            }
        }

        public int GetFrameTypesCount() => Enum.GetNames(typeof(FrameType)).Length;
    }
}
