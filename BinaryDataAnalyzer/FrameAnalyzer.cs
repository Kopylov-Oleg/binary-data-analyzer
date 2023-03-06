using System;
using System.Collections.Generic;

namespace BinaryDataAnalyzer
{
    internal class FrameAnalyzer
    {
        private static readonly int frameNumberSize = 4;
        private static readonly int frameSize = 2048;
        private static readonly int crcSize = 2;

        private static readonly byte[] defaultMarkerBeginning;
        private static readonly Dictionary<FrameType, byte[]> defaultMarkerEndings;
        private static int maxMarkerEndingLength = 0;


        private int markerBeginningIndex;
        private readonly Dictionary<FrameType, FrameTypeStatistics> frameTypesStatistics;
        private FrameTypeStatistics currentFrameTypeStatistics;
        private readonly Dictionary<FrameType, uint> frameCounters;

        static FrameAnalyzer()
        {
            defaultMarkerBeginning = new byte[] { 0x7C, 0x6E, 0xA1 };
            defaultMarkerEndings = new Dictionary<FrameType, byte[]>
            {
                { FrameType.Frame1, new byte[] {0x2C, 0xFA} },
                { FrameType.Frame2, new byte[] {0x2D, 0x00} },
                { FrameType.Frame3, new byte[] {0x2D, 0x01} },
                { FrameType.Frame4, new byte[] {0x2D, 0x02} },
                { FrameType.Frame5, new byte[] {0x2D, 0x03} },
                { FrameType.Frame6, new byte[] {0x2D, 0x04} },
                { FrameType.Frame7, new byte[] {0x2D, 0x05} },
                { FrameType.Frame8, new byte[] {0x2D, 0x06} },
                { FrameType.Frame9, new byte[] {0x2F} },
                { FrameType.Frame10, new byte[] {0x30} }
            };
        }

        public FrameAnalyzer()
        {
            markerBeginningIndex = 0;

            frameTypesStatistics = new Dictionary<FrameType, FrameTypeStatistics>();
            frameCounters = new Dictionary<FrameType, uint>();

            foreach (FrameType frameName in (FrameType[])Enum.GetValues(typeof(FrameType)))
            {
                FrameTypesStatistics.Add(frameName, new FrameTypeStatistics(frameName));
                frameCounters.Add(frameName, 0);
            }
        }

        public static int MarkerBeginningLength => defaultMarkerBeginning.Length;

        public static int MaxMarkerEndingLength
        {
            get
            {
                if (maxMarkerEndingLength >0)
                {
                    return maxMarkerEndingLength;
                }

                int max = 0;
                foreach(var defaultMarkerEnding in defaultMarkerEndings)
                {
                    int tmp = defaultMarkerEnding.Value.Length;
                    if (tmp > max) 
                    {
                        max = tmp;
                    }
                }
                maxMarkerEndingLength = max;

                return maxMarkerEndingLength;
            }
        }

        public Dictionary<FrameType, FrameTypeStatistics> FrameTypesStatistics => frameTypesStatistics;

        public void AddByte(byte b)
        {
            if (b == defaultMarkerBeginning[markerBeginningIndex])
            {
                markerBeginningIndex++;
            }
            else
            {
                markerBeginningIndex = 0;
            }
        }

        public bool IsFrame()
            => markerBeginningIndex == MarkerBeginningLength;

        public void ProcessPotentialFrame(BinaryReaderFacade binaryReader)
        {
            markerBeginningIndex = 0;
            var bufferSize = MaxMarkerEndingLength;
            var buffer = binaryReader.ReadBytes(bufferSize);

            foreach (var defaultMarkerEnding in defaultMarkerEndings)
            {
                if(BeginsWith(buffer, defaultMarkerEnding.Value))
                {
                    var extraBytes = buffer.Length - defaultMarkerEnding.Value.Length;
                    binaryReader.MoveBack(extraBytes);

                    currentFrameTypeStatistics = FrameTypesStatistics.GetValueOrDefault(defaultMarkerEnding.Key);

                    ProcessFrame(binaryReader);

                    currentFrameTypeStatistics.FramesCount++;

                    return;
                }
            }

            binaryReader.MoveBack(bufferSize);
        }

        private static bool BeginsWith(byte[] array, byte[] subArray)
        {
            if(subArray.Length > array.Length)
            {
                return false;
            }

            for (int i = 0; i < subArray.Length; i++)
            {
                if (subArray[i] != array[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void ProcessFrame(BinaryReaderFacade binaryReader)
        {
            var markerBeginningBytes = defaultMarkerBeginning;
            var markerEndingBytes = defaultMarkerEndings.GetValueOrDefault(currentFrameTypeStatistics.FrameName);

            var frameNumberBytes = Array.Empty<byte>();
            if (currentFrameTypeStatistics.FrameName != FrameType.Frame10)
            {
                frameNumberBytes = binaryReader.ReadBytes(frameNumberSize);
                ProcessFrameNumber(frameNumberBytes);
            }

            var dataBytesCount = frameSize
                - MarkerBeginningLength
                - markerEndingBytes.Length
                - frameNumberBytes.Length
                - crcSize;
            var dataBytes = binaryReader.ReadBytes(dataBytesCount);

            var crcBytes = binaryReader.ReadBytes(crcSize);

            var crc = CRC.CRC16(
                markerBeginningBytes,
                markerEndingBytes,
                frameNumberBytes,
                dataBytes);            

            if (!ProcessFrameCrc(crc, crcBytes))
            {
                binaryReader.MoveBack(
                    (currentFrameTypeStatistics.FrameName == FrameType.Frame10 ? 0 : frameNumberSize)
                    + dataBytesCount
                    + crcSize);
            }
        }

        private void ProcessFrameNumber(byte[] bytes)
        {
            var frameBytes = new byte[bytes.Length];
            Array.Copy(bytes, 0, frameBytes, 0, bytes.Length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(frameBytes);
            }

            var frameNumber = BitConverter.ToUInt32(frameBytes, 0);

            if (frameNumber - 1 != frameCounters[currentFrameTypeStatistics.FrameName] 
                && frameCounters[currentFrameTypeStatistics.FrameName] > 0)
            {
                currentFrameTypeStatistics.NumberingErrorsCount++;
            }

            frameCounters[currentFrameTypeStatistics.FrameName] = frameNumber;
        }

        private bool ProcessFrameCrc(ushort crc, byte[] crcBytes)
        { 
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(crcBytes);
            }

            var crcRead = BitConverter.ToUInt16(crcBytes);

            var result = crc == crcRead;

            if (!result)
            {
                currentFrameTypeStatistics.CrcErrorsCount++;
            }

            return result;
        }
    }
}
