using System;
using System.Collections.Generic;

namespace BinaryDataAnalyzer
{
    internal class FrameAnalyzer
    {
        private static readonly int frameNumberSize = 4;
        private static readonly int frameSize = 2048;
        private static readonly int crcSize = 2;

        private static byte[] defaultMarkerBeginning;
        private static Dictionary<FrameType, byte[]> defaultMarkerEndings;
        private static int maxMarkerEndingLength = 0;


        private int markerBeginningIndex;
        private Dictionary<FrameType, FrameTypeStatistics> frameTypesStatistics;
        private FrameTypeStatistics currentFrameTypeStatistics;
        private Dictionary<FrameType, uint> frameCounters;

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

        private static ushort CRC16CCITT(params byte[][] bytes)
        {
            //https://www.devcoons.com/c-crc16-algorithm/
            const ushort poly = 4129;
            ushort[] table = new ushort[256];
            ushort initialValue = 0xffff;
            ushort temp, a;
            ushort crc = initialValue;
            for (int i = 0; i < table.Length; ++i)
            {
                temp = 0;
                a = (ushort)(i << 8);
                for (int j = 0; j < 8; ++j)
                {
                    if (((temp ^ a) & 0x8000) != 0)
                        temp = (ushort)((temp << 1) ^ poly);
                    else
                        temp <<= 1;
                    a <<= 1;
                }
                table[i] = temp;
            }
            for (int i = 0; i < bytes.Length; ++i)
            {
                for (int j = 0; j < bytes[i].Length; j++)
                {
                    crc = (ushort)((crc << 8) ^ table[((crc >> 8) ^ (0xff & bytes[i][j]))]);
                }
            }
            return crc;
        }

        /*
        public static byte[] ModbusCRC16Calc(params byte[][] Message)
        {
            //выдаваемый массив CRC
            byte[] CRC = new byte[2];
            ushort Register = 0xFFFF; // создаем регистр, в котором будем сохранять высчитанный CRC
            ushort Polynom = 0xA001; //Указываем полином, он может быть как 0xA001(старший бит справа), так и его реверс 0x8005(старший бит слева, здесь не рассматривается), при сдвиге вправо используется 0xA001
            
            for(int i1 = 0; i1 < Message.Length; i1++)
            for (int i2 = 0; i2 < Message[i1].Length; i2++) // для каждого байта в принятом\отправляемом сообщении проводим следующие операции(байты сообщения без принятого CRC)
            {
                Register = (ushort)(Register ^ Message[i1][i2]); // Делим через XOR регистр на выбранный байт сообщения(от младшего к старшему)

                for (int j = 0; j < 8; j++) // для каждого бита в выбранном байте делим полученный регистр на полином
                {
                    if ((ushort)(Register & 0x01) == 1) //если старший бит равен 1 то
                    {
                        Register = (ushort)(Register >> 1); //сдвигаем на один бит вправо
                        Register = (ushort)(Register ^ Polynom); //делим регистр на полином по XOR
                    }
                    else //если старший бит равен 0 то
                    {
                        Register = (ushort)(Register >> 1); // сдвигаем регистр вправо
                    }
                }
            }

            CRC[1] = (byte)(Register >> 8); // присваеваем старший байт полученного регистра младшему байту результата CRC (CRClow)
            CRC[0] = (byte)(Register & 0x00FF); // присваеваем младший байт полученного регистра старшему байту результата CRC (CRCHi) это условность Modbus — обмен байтов местами.

            return CRC;
        }
        */

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
                - currentFrameTypeStatistics.FrameName == FrameType.Frame10 ? 0 : frameNumberSize
                - crcSize;
            var dataBytes = binaryReader.ReadBytes(dataBytesCount);


            var crc = CRC16CCITT(
            markerBeginningBytes,
            markerEndingBytes,
            frameNumberBytes,
            dataBytes);


            var crcBytes = binaryReader.ReadBytes(crcSize);

            if (!ProcessFrameCrc(crc, crcBytes))
            {
                binaryReader.MoveBack(
                    currentFrameTypeStatistics.FrameName == FrameType.Frame10 ? 0 : frameNumberSize
                    + dataBytesCount
                    + crcSize);
            }
        }

        private void ProcessFrameNumber(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            var frameNumber = BitConverter.ToUInt32(bytes, 0);

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
