using System;
using System.IO;
using Timing = System.Int32;
using Voltage = System.Int32;
using UInt8 = System.Byte;

namespace SpdReaderWriterDll {

    /// <summary>
    /// Defines RAM Type 
    /// </summary>
    public enum RamType {
        UNKNOWN              = 0,
        //FPM                = 1,
        //EDO                = 2,
        //PIPELINED_NIBBLE   = 3,
        SDRAM                = 4,
        //ROM                = 5,
        //DDR_SGRAM          = 6,
        DDR                  = 7,
        DDR2                 = 8,
        DDR2_FB_DIMM         = 9,
        //DDR2_FB_DIMM_PROBE = 10,
        DDR3                 = 11,
        DDR4                 = 12,
    }

    /// <summary>
    /// Defines SPD sizes
    /// </summary>
    public enum SpdSize {
        //NONE    = 0,
        //MINIMUM = 128,
        SDRAM   = 256,
        DDR     = 256,
        DDR2    = 256,
        DDR3    = 256,
        DDR4    = 512,
    }

    /// <summary>
    /// Detailed SDP data
    /// </summary>
    public struct SpdData {
        //Product details
        private int               _moduleManufacturer;
        private int               _dramManufacturer;
        private string            _modulePartNumber;
        private int               _serialNumber;
        private ManufacturingDate _manufacturingDate;
        private int               _manufacturingLocation;
        private int               _moduleRevisionCode;
        private int               _dramDieStepping;

        // Addressing and capacity
        private int               _rowAddressing;
        private int               _columnAddressing;
        private int               _bankAddressing;
        private int               _bankGroupBits;
        private int               _dataWidth;
        private int               _signalLoading;
        private int               _dieCount;
        private int               _packageRanks;
        private int               _primaryBusWidth;
        private bool              _eccExtension;
        private string            _rawCard;
        private int               _revision;


        // Timings
        // (XMP4 indicates the value is present in DDR4 XMP)
        private TimingAdjustable _minCycle;      // XMP4
        private TimingAdjustable _maxCycle;
        private TimingAdjustable _minCasLatency; // XMP4
        private TimingAdjustable _cl;            // XMP4
        private TimingAdjustable _rcd;           // XMP4
        private TimingAdjustable _rp;            // XMP4
        private Timing           _ras;           // XMP4
        private TimingAdjustable _rc;            // XMP4
        private Timing           _faw;           // XMP4
        private TimingAdjustable _rrds;          // XMP4
        private TimingAdjustable _rrdl;          // XMP4
        private Timing           _wr;
        private Timing           _wtrs;
        private Timing           _wtrl;
        private TimingAdjustable _ccdl;
        private Timing           _rfc1;          // XMP4
        private Timing           _rfc2;          // XMP4
        private Timing           _rfc4;          // XMP4

        private Voltage          _voltage;       // XMP4

        private bool             _xmp1;
        private bool             _xmp2;          // XMP4
    }

    /// <summary>
    /// When a timing value tXX cannot be expressed by an integer number of MTB units, the SPD must be encoded using both
    /// the MTB and FTB.The Fine Offsets are encoded using a two complement values which, when multiplied by the FTB yields
    /// a positive or negative correction factor. Typically, for safety and for legacy compatibility, the MTB portion is
    /// rounded UP and the FTB correction is a negative value
    /// </summary>
    struct TimingAdjustable {
        private Timing _mtb; // medium time base 
        private Timing _ftb; // fine time base 
    }

    /// <summary>
    /// Manufacturing date
    /// </summary>
    struct ManufacturingDate {
        private UInt8 _year;
        private UInt8 _week;
    }

    /// <summary>
    /// SPD class to deal with SPD data
    /// </summary>
    public class Spd {

        /// <summary>
        /// Gets RAM type from SPD data
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns>RAM Type</returns>
        public static RamType GetRamType(Device device) {

            if (device == null ) {
                throw new NullReferenceException($"Invalid device ({device.PortName.ToString()})");
            }

            if (!device.IsConnected) {
                throw new IOException($"Device not connected ({device.PortName.ToString()})");
            }

            // Byte at offset 0x02 in SPD indicates RAM type
            byte _rt = Eeprom.ReadByte(device, 0x02);
            if (Enum.IsDefined(typeof(RamType), (RamType)_rt)) {
                return (RamType)_rt;
            }
            return RamType.UNKNOWN;
        }

        /// <summary>
        /// Gets RAM type from SPD data
        /// </summary>
        /// <param name="spd">SPD dump</param>
        /// <returns>RAM Type</returns>
        public static RamType GetRamType(byte[] spd) {
            // Byte at offset 0x02 in SPD indicates RAM type
            if (spd.Length >= 3) {
                byte _rt = spd[0x02];
                if (Enum.IsDefined(typeof(RamType), (RamType)_rt)) {
                    return (RamType)_rt;
                }
            }

            return RamType.UNKNOWN;
        }

        /// <summary>
        /// Gets total EEPROM size
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns>SPD size</returns>
        public static SpdSize GetSpdSize(Device device) {
            return GetSpdSize(GetRamType(device));
        }

        /// <summary>
        /// Gets total EEPROM size
        /// </summary>
        /// <param name="ramType">Ram Type</param>
        /// <returns>SPD size</returns>
        public static SpdSize GetSpdSize(RamType ramType) {
            switch (ramType) {
                case RamType.SDRAM:                  //return SpdSize.SDRAM;
                case RamType.DDR:                    //return SpdSize.DDR;
                case RamType.DDR2:                   //return SpdSize.DDR2;
                case RamType.DDR2_FB_DIMM:           //return SpdSize.DDR2;
                case RamType.DDR3:
                    return SpdSize.DDR3;
                case RamType.DDR4:
                    return SpdSize.DDR4;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Gets model name from SPD contents
        /// </summary>
        /// <param name="input">SPD contents</param>
        /// <returns>Model name</returns>
        public static string GetModulePartNumberName(byte[] input) {

            int modelNameStart;
            int modelNameEnd;

            switch (GetRamType(input)) {
                case RamType.DDR4: // Part number location for DDR4 SPDs
                    modelNameStart = 0x149;
                    modelNameEnd   = 0x15C;
                    break;
                case RamType.DDR3: // Part number location for DDR3 SPDs
                    modelNameStart = 0x80;
                    modelNameEnd   = 0x91;
                    break;
                default:		   // Part number location for DDR2 and older RAM SPDs
                    modelNameStart = 0x49;
                    modelNameEnd   = 0x5A;
                    break;
            }

            if (input.Length < modelNameEnd) {
                throw new InvalidDataException("Incomplete Data");
            }

            char[] _chars = new char[modelNameEnd - modelNameStart + 1];

            for (int i = 0; i < _chars.Length; i++) {
                _chars[i] = (char)input[modelNameStart + i];
            }
            return new string(_chars).Trim();
        }

        /// <summary>
        /// Calculates CRC16/XMODEM checksum
        /// </summary>
        /// <param name="input">A byte array to be checked</param>
        /// <returns>A calculated checksum</returns>
        public static UInt16 Crc16(byte[] input) {

            UInt16[] table = new UInt16[256];
            UInt16 crc = 0;

            for (int i = 0; i < table.Length; ++i) {

                UInt16 temp = 0;
                UInt16 a = (UInt16)(i << 8);

                for (int j = 0; j < 8; ++j) {
                    //temp = ((temp ^ a) & 0x8000) != 0 ? (ushort)((temp << 1) ^ 0x1021) : temp <<= 1;
                    if (((temp ^ a) & 0x8000) != 0) {
                        temp = (UInt16)((temp << 1) ^ 0x1021);
                    }
                    else {
                        temp <<= 1;
                    }
                    a <<= 1;
                }

                table[i] = temp;
            }

            for (int i = 0; i < input.Length; ++i) {
                crc = (UInt16)((crc << 8) ^ table[(crc >> 8) ^ (0xff & input[i])]);
            }

            return crc;
        }

        /// <summary>
        /// Calculates CRC8 checksum
        /// </summary>
        /// <param name="input">A byte array to be checked</param>
        /// <returns>A calculated checksum</returns>
        public static UInt16 Crc(byte[] input) {
            UInt16 crc = 0;

            for (int i = 0; i < input.Length; i++) {
                crc += input[i];
            }

            return crc;
        }

        /// <summary>
        /// Gets bit value specified at position from a byte
        /// </summary>
        /// <param name="input">Input byte to get bit value from</param>
        /// <param name="position">Bit position from 0 (LSB) to 7 (MSB)</param>
        /// <returns>A bit value</returns>
        public static byte GetBit(byte input, UInt8 position) {

            if (position > 7) {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return (byte)((input >> position) & 1);
        }

        /// <summary>
        /// Gets bit values from a byte at specified offset position
        /// </summary>
        /// <param name="input">Input byte to get a bit value from</param>
        /// <param name="position">Bit position from 0 (LSB) to 7 (MSB)</param>
        /// <param name="count">The number of bits to read</param>
        /// <returns>An array of bit values</returns>
        public static byte[] GetBits(byte input, UInt8 position, UInt8 count) {

            if (count < 1) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count > 8 || position > 7 || count > position + 1) {
                return new byte[] { 0 };
            }

            byte[] bits = new byte[count];

            for (int i = 0; i < bits.Length; i++) {
                bits[i] = GetBit(input, (byte)(position - i));
            }

            return bits;
        }

        /// <summary>
        /// Sets specified bit in a byte at specified offset position
        /// </summary>
        /// <param name="input">Input byte to set bit in</param>
        /// <param name="position">Bit position to set</param>
        /// <param name="value">Bit value to set</param>
        public static byte SetBit(byte input, UInt8 position, byte value) {

            if (position > 7) {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return (byte)(value == 1 ? input | (1 << position) : input &~ (1 << position));
        }

        /// <summary>
        /// Gets number of bits from input byte at position and converts them to a new byte
        /// </summary>
        /// <param name="input">Input byte to get bits from</param>
        /// <param name="position">Bit position from 0 (LSB) to 7 (MSB)</param>
        /// <param name="count">The number of bits to read</param>
        /// <returns>Byte matching bit pattern at input position of length count</returns>
        public static byte GetByteFromBits(byte input, UInt8 position, UInt8 count) {

            if (count < 1) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // Generate bit mask
            byte mask = (byte)(Math.Pow(2, count) - 1);

            // Calculate shift position for the input
            byte shift = (byte)(position - count + 1);

            // Bitwise AND shifted input and mask
            return (byte)((input >> shift) & mask);
        }
    }
}
