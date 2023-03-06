namespace BinaryDataAnalyzer
{
    internal class FrameTypeStatistics
    {
        public FrameTypeStatistics(FrameType frameName) 
        {
            FrameName = frameName;
        }

        public FrameType FrameName { get; }

        public int FramesCount { get; set; } = 0;

        public int NumberingErrorsCount { get; set; } = 0;

        public int CrcErrorsCount { get; set; } = 0;
    }
}
