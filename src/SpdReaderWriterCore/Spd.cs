/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using SpdReaderWriterCore.Properties;

namespace SpdReaderWriterCore {

    /// <summary>
    /// SPD class to deal with SPD data
    /// </summary>
    public partial class Spd {

        /// <summary>
        /// Gets RAM type present on the device's I2C bus
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns>RAM Type</returns>
        public static RamType GetRamType(Arduino device) {

            if (device == null) {
                throw new NullReferenceException($"Invalid device");
            }

            if (!device.IsConnected) {
                throw new IOException($"Device not connected ({device.PortName})");
            }

            if (device.DetectDdr5()) {
                return RamType.DDR5;
            }

            if (device.DetectDdr4()) {
                return RamType.DDR4;
            }

            // Byte at offset 2 in SPD indicates RAM type
            try {
                byte controlByte = Eeprom.Read(device, 2);
                return Enum.IsDefined(typeof(RamType), controlByte) ? (RamType)controlByte : RamType.UNKNOWN;
            }
            catch {
                throw new Exception($"Unable to detect RAM type at {device.I2CAddress} on {device.PortName}");
            }
        }

        /// <summary>
        /// Gets RAM type from SPD data
        /// </summary>
        /// <param name="input">SPD dump</param>
        /// <returns>RAM Type</returns>
        public static RamType GetRamType(byte[] input) {

            // Byte at offset 2 in SPD indicates RAM type
            return input.Length >= 3 && Enum.IsDefined(typeof(RamType), (RamType)input[2]) ? (RamType)input[2] : RamType.UNKNOWN;
        }

        /// <summary>
        /// Gets total EEPROM size
        /// </summary>
        /// <param name="ramType">Ram Type</param>
        /// <returns>SPD size</returns>
        public static int GetSpdSize(RamType ramType) {

            switch (ramType) {
                case RamType.SDRAM:
                case RamType.DDR:
                case RamType.DDR2:
                case RamType.DDR2_FB_DIMM:
                case RamType.DDR3:
                    return DataLength.Minimum;
                case RamType.DDR4:
                case RamType.DDR4E:
                case RamType.LPDDR3:
                case RamType.LPDDR4:
                    return DataLength.DDR4;
                case RamType.DDR5:
                    return DataLength.DDR5;
                default:
                    return DataLength.Unknown;
            }
        }

        /// <summary>
        /// Validates SPD data
        /// </summary>
        /// <param name="input">SPD contents</param>
        /// <returns><see langword="true"/> if <paramref name="input"/> data is a valid SPD dump</returns>
        public static bool ValidateSpd(byte[] input) {

            if (input == null || input.Length < DataLength.Minimum) {
                return false;
            }

            switch (input.Length) {
                case DataLength.DDR5 when GetRamType(input) == RamType.DDR5:
                case DataLength.DDR4 when GetRamType(input) == RamType.DDR4:
                    return true;
                case DataLength.Minimum:
                    return GetRamType(input) == RamType.DDR3 ||
                           GetRamType(input) == RamType.DDR2 ||
                           GetRamType(input) == RamType.DDR  ||
                           GetRamType(input) == RamType.SDRAM;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets manufacturer from SPD contents
        /// </summary>
        /// <param name="input">SPD contents</param>
        /// <returns>Manufacturer's name</returns>
        public static ushort GetManufacturerId(byte[] input) {
            
            ISpd spd = null;

            switch (GetRamType(input)) {
                case RamType.DDR5:
                    spd = new DDR5(input);
                    break;
                case RamType.DDR4:
                    spd = new DDR4(input);
                    break;
                case RamType.DDR3:
                case RamType.DDR2_FB_DIMM:
                    spd = new DDR3(input);
                    break;
                case RamType.DDR2:
                    spd = new DDR2(input);
                    break;
                case RamType.DDR:
                    spd = new DDR(input);
                    break;
                case RamType.SDRAM:
                    spd = new SDRAM(input);
                    break;
            }

            return spd != null ? spd.ManufacturerIdCode.ManufacturerId : (ushort)0;
        }

        /// <summary>
        /// Finds manufacturer's ID code by name
        /// </summary>
        /// <param name="name">Manufacturer's name</param>
        /// <returns>Manufacturer's <see cref="ManufacturerIdCodeData.ContinuationCode"/> and <see cref="ManufacturerIdCodeData.ManufacturerCode"/></returns>
        public static ManufacturerIdCodeData FindManufacturerId(string name) {

            // Iterate through continuation codes
            for (byte i = 0; i < Resources.Database.IdCodes.Length; i++) {

                // Decompress database
                const byte separatorByte = 0x0A;
                string[] names = Data.BytesToString(Data.Gzip(Resources.Database.IdCodes[i], Data.GzipMethod.Decompress)).Split((char)separatorByte);

                // Iterate through manufacturer codes
                for (byte j = 0; j < names.Length; j++) {
                    if (Data.StringContains(names[j], name)) {
                        return new ManufacturerIdCodeData {
                            ContinuationCode = i,
                            ManufacturerCode = Data.SetBit(j, 7, Data.GetParity(j, Data.Parity.Odd) == 1)
                        };
                    }
                }
            }

            return new ManufacturerIdCodeData();
        }

        /// <summary>
        /// Gets manufacturer from ID codes
        /// </summary>
        /// <param name="input">MSB and LSB of ID Code</param>
        /// <returns>Manufacturer's name</returns>
        public static string GetManufacturerName(ushort input) {

            // Manufacturer's identification code table

            //int codeTableCount = 126;
            //byte[] manufacturerCodeTable = new byte[codeTableCount];
            //for (byte i = 0; i < 126; i++) {
            //    byte code = (byte)(i + 1);
            //    manufacturerCodeTable[i] = (byte)(code | Data.GetParity(code, Data.Parity.Odd) << 7);
            //}

            // Lookup name by continuation code and manufacturer's ID
            byte spdContinuationCode = Data.SetBit((byte)(input >> 8), 7, false);
            byte spdManufacturerCode = Data.SetBit((byte)input, 7, false); // Ignore parity bit

            if (spdContinuationCode > Resources.Database.IdCodes.Length - 1) {
                return "";
            }

            // Decompress database
            const byte separatorByte = 0x0A;
            byte[] idTableCharArray = Data.Gzip(Resources.Database.IdCodes[spdContinuationCode], Data.GzipMethod.Decompress);
            string[] names = Data.BytesToString(idTableCharArray).Split((char)separatorByte);

            return spdManufacturerCode <= names.Length ? names[spdManufacturerCode - 1] : "";
        }

        /// <summary>
        /// Gets model name from SPD contents
        /// </summary>
        /// <param name="input">SPD contents</param>
        /// <returns>Model part number</returns>
        public static string GetModulePartNumberName(byte[] input) {

            ISpd spd = null;

            RamType rt = GetRamType(input);

            switch (rt) {
                case RamType.DDR5:
                    spd = new DDR5(input);
                    break;
                case RamType.DDR4:
                    spd = new DDR4(input);
                    break;
                case RamType.DDR3:
                case RamType.DDR2_FB_DIMM:
                    spd = new DDR3(input);
                    break;
                case RamType.DDR2:
                    spd = new DDR2(input);
                    break;
                case RamType.DDR:
                    spd = new DDR(input);
                    break;
                case RamType.SDRAM:
                    spd = new SDRAM(input);
                    break;
            }

            // Part number for Kingston DDR2 and DDR SPDs
            if ((rt == RamType.DDR2 || rt == RamType.DDR) &&
                (GetManufacturerId(input) == 0x0198 || FindManufacturerId("Kingston") == spd?.ManufacturerIdCode)) {

                byte[] chars = Data.TrimArray(input, 16, Data.TrimPosition.Start);

                return Data.BytesToString(chars);
            }

            return Data.TrimNonAscii(spd?.PartNumber);
        }

        /// <summary>
        /// Defines basic memory type byte value
        /// </summary>
        public enum RamType : byte {
            UNKNOWN       = 0x00,
            SDRAM         = 0x04,
            DDR           = 0x07,
            DDR2          = 0x08,
            [Description("DDR2 Fully-Buffered DIMM")]
            DDR2_FB_DIMM  = 0x09,
            [Description("DDR2 Fully-Buffered DIMM Probe")]
            DDR2_FB_DIMMP = 0x0A,
            DDR3          = 0x0B,
            LPDDR3        = 0x0F,
            DDR4          = 0x0C,
            DDR4E         = 0x0E,
            LPDDR4        = 0x10,
            LPDDR4X       = 0x11,
            DDR5          = 0x12,
            LPDDR5        = 0x13,
            [Description("DDR5 NVDIMM-P")]
            DDR5_NVDIMM_P = 0x14,
            LPDDR5X       = 0x15,
        }

        /// <summary>
        /// Defines SPD sizes
        /// </summary>
        public struct DataLength {

            public static ushort[] Length = { Unknown, Minimum, DDR4, DDR5 };

            public const ushort Unknown = 0;
            public const ushort Minimum = 256; // DDR3, DDR2, DDR, and SDRAM
            public const ushort DDR4    = 512; // incl. LPDDR3
            public const ushort DDR5    = 1024;
        }

        /// <summary>
        /// Raw SPD data contents
        /// </summary>
        public static byte[] RawData { get; set; }

        /// <summary>
        /// SPD Revision level data
        /// </summary>
        public struct SpdRevisionData {
            public byte EncodingLevel;
            public byte AdditionsLevel;

            public override string ToString() => $"{EncodingLevel:D1}.{AdditionsLevel:D1}";
        }

        /// <summary>
        /// Describes the total number of bytes used and the total size of the serial memory used
        /// </summary>
        public struct BytesData {
            public ushort Used;
            public ushort Total;

            public override string ToString() => $"{Used}/{Total}";
        }

        /// <summary>
        /// Describes the width of the SDRAM memory bus on the module
        /// </summary>
        public struct BusWidthData {
            /// <summary>
            /// ECC extension
            /// </summary>
            public bool Extension;
            /// <summary>
            /// Primary bus width
            /// </summary>
            public byte PrimaryBusWidth;

            public override string ToString() => ((byte)(PrimaryBusWidth + (Extension ? 8 : 0))).ToString();
        }

        /// <summary>
        /// Row addressing and the column addressing in the SDRAM device
        /// </summary>
        public struct AddressingData {
            public byte Rows;
            public byte Columns;
        }

        /// <summary>
        /// Describes the type of SDRAM devices on the module
        /// </summary>
        public struct PrimaryPackageTypeData {
            public bool Monolithic;
            public byte DieCount;
            public SignalLoadingData SignalLoading;

            public override string ToString() {
                return Monolithic ? "Monolithic" : Data.GetEnumDescription(SignalLoading);
            }
        }

        /// <summary>
        /// Refers to loading on signals at the SDRAM balls
        /// </summary>
        public enum SignalLoadingData {
            [Description("Not Specified")]
            Not_Specified, // Monolithic
            [Description("Multi Load Stack")]
            Multi_Load_Stack,
            [Description("Single Load Stack")] 
            Single_Load_Stack,
        }

        /// <summary>
        /// Data Size prefixes
        /// </summary>
        public enum CapacityPrefix : ulong {
            [Description("kilo")]
            K = 1024,
            [Description("mega")]
            M = K * K,
            [Description("giga")]
            G = M * K,
            [Description("tera")]
            T = G * K,
            [Description("peta")]
            P = T * K,
            [Description("exa")]
            E = P * K,
        }

        /// <summary>
        /// Support for certain SDRAM features
        /// </summary>
        public struct MaximumActivateFeaturesData {
            public ushort MaximumActivateWindow;
            public MaximumActivateCount MaximumActivateCount;
        }

        /// <summary>
        /// Maximum Activate Count (MAC)
        /// </summary>
        public enum MaximumActivateCount {
            [Description("Untested MAC")]
            Untested,
            [Description("700 K")]
            _700K,
            [Description("600 K")] 
            _600K,
            [Description("500 K")] 
            _500K,
            [Description("400 K")] 
            _400K,
            [Description("300 K")] 
            _300K, 
            [Description("200 K")] 
            _200K,
            
            Reserved,
            [Description("Unlimited MAC")]
            Unlimited
        }

        /// <summary>
        /// Module’s voltage interface
        /// </summary>
        public enum VoltageLevel {
            /// <summary>
            /// TTL/5 V tolerant
            /// </summary>
            [Description("TTL/5 V tolerant")]
            TTL,

            /// <summary>
            /// LVTTL (not 5 V tolerant)
            /// </summary>
            [Description("LVTTL (not 5 V tolerant)")]
            LVTTL,

            /// <summary>
            /// HSTL 1.5 V
            /// </summary>
            [Description("HSTL 1.5 V")] 
            HSTL,

            /// <summary>
            /// SSTL 3.3 V
            /// </summary>
            [Description("SSTL 3.3 V")] 
            SSTL33,

            /// <summary>
            /// SSTL 2.5 V
            /// </summary>
            [Description("SSTL 2.5 V")] 
            SSTL25,

            /// <summary>
            /// SSTL 1.8 V
            /// </summary>
            [Description("SSTL 1.8 V")] 
            SSTL18,
        }

        /// <summary>
        /// Manufacturer ID Code
        /// </summary>
        public struct ManufacturerIdCodeData {
            public byte ContinuationCode;
            public byte ManufacturerCode;

            public ushort ManufacturerId => (ushort)(ContinuationCode << 8 | ManufacturerCode);

            public override string ToString() => GetManufacturerName(ManufacturerId);

            public static bool operator ==(ManufacturerIdCodeData d1, ManufacturerIdCodeData d2) {
                return d1.ContinuationCode == d2.ContinuationCode &&
                       d1.ManufacturerCode == d2.ManufacturerCode;
            }

            public static bool operator !=(ManufacturerIdCodeData d1, ManufacturerIdCodeData d2) {
                return d1.ContinuationCode != d2.ContinuationCode ||
                       d1.ManufacturerCode != d2.ManufacturerCode;
            }

            public override bool Equals(object o) {
                if (o != null && o.GetType() != typeof(ManufacturerIdCodeData)) {
                    return false;
                }

                return o != null &&
                       ContinuationCode.Equals(((ManufacturerIdCodeData)o).ContinuationCode) &&
                       ManufacturerCode.Equals(((ManufacturerIdCodeData)o).ManufacturerCode);
            }

            public override int GetHashCode() {
                return ManufacturerId;
            }
        }

        /// <summary>
        /// Date code
        /// These bytes must be represented in Binary Coded Decimal
        /// </summary>
        public struct DateCodeData {
            public byte Year;
            public byte Week;

            public override string ToString() {
                ushort year = (ushort)(Data.ByteToBinaryCodedDecimal(Year) + 2000);
                byte week   = Data.ByteToBinaryCodedDecimal(Week);

                return 0 < week && week < 53 ? $"{year:D4}/{week:D2}" : "";
            }
        }

        /// <summary>
        /// Module Serial Number data
        /// </summary>
        public struct SerialNumberData {
            public byte[] SerialNumber;

            public override string ToString() {

                StringBuilder sb = new StringBuilder();

                foreach (byte b in SerialNumber) {
                    sb.Append($"{b:X2}");
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// CRC16 header and checksum
        /// </summary>
        public struct Crc16Data {
            public byte[] Contents; // Contents incl. checksum
            public ushort Checksum => Data.Crc16(Data.TrimArray(Contents, Contents.Length - 2, Data.TrimPosition.End), 0x1021);

            /// <summary>
            /// Validates data checksum
            /// </summary>
            /// <returns><see langword="true"/> if <see cref="Checksum"/> is valid for <see cref="Contents"/></returns>
            public bool Validate() => (ushort)(Contents[Contents.Length - 1] << 8 | Contents[Contents.Length - 2]) == Checksum;

            /// <summary>
            /// Corrects Crc16 value
            /// </summary>
            /// <returns><see cref="Contents"/> with the correct <see cref="Checksum"/></returns>
            public byte[] Fix() {
                if (!Validate()) {
                    Contents[Contents.Length - 1] = (byte)(Checksum >> 8);
                    Contents[Contents.Length - 2] = (byte)Checksum;
                }

                return Contents;
            }

            public override string ToString() => ((CrcStatus)Data.BoolToNum<byte>(Validate())).ToString();
        }

        /// <summary>
        /// CRC header and checksum
        /// </summary>
        public struct Crc8Data {
            public byte[] Contents; // Contents incl. checksum
            public byte Checksum => Data.Crc(Data.TrimArray(Contents, Contents.Length - 1, Data.TrimPosition.End));

            /// <summary>
            /// Validates data checksum
            /// </summary>
            /// <returns><see langword="true"/> if <see cref="Checksum"/> is valid for <see cref="Contents"/></returns>
            public bool Validate() => Contents[Contents.Length - 1] == Checksum;

            /// <summary>
            /// Corrects CRC checksum
            /// </summary>
            /// <returns><see cref="Contents"/> with the correct <see cref="Checksum"/></returns>
            public byte[] Fix() {
                if (!Validate()) {
                    Contents[Contents.Length - 1] = Checksum;
                }

                return Contents;
            }

            public override string ToString() => ((CrcStatus)Data.BoolToNum<byte>(Validate())).ToString();
        }

        /// <summary>
        /// Crc status
        /// </summary>
        public enum CrcStatus {
            OK  = 1,
            Bad = 0
        }

        /// <summary>
        /// Indicates which JEDEC reference design raw card was used as the basis for the module assembly
        /// </summary>
        public struct ReferenceRawCardData {
            public bool Extension;
            public byte Revision;
            public ReferenceRawCardName Name;
        }

        /// <summary>
        /// Reference Raw Card
        /// </summary>
        /// <remarks>
        /// DDR5: https://www.jedec.org/standards-documents/focus/memory-module-designs-dimms/ddr5/all
        /// DDR4: https://www.jedec.org/standards-documents/focus/memory-module-designs-dimms/ddr4/all
        /// DDR3: https://www.jedec.org/standards-documents/focus/memory-module-designs-dimms/ddr3/all
        /// </remarks>
        public enum ReferenceRawCardName {
            // When ReferenceRawCardUsed extension bit = 0
            A, B, C, D, E, F, G, H, J, K, L, M, N, P, R, T, U, V, W, Y, AA, AB, AC, AD, AE, AF, AG, AH, AJ, AK, AL,
            // When ReferenceRawCardUsed extension bit = 1
            AM, AN, AP, AR, AT, AU, AV, AW, AY, BA, BB, BC, BD, BE, BF, BG, BH, BJ, BK, BL, BM, BN, BP, BR, BT, BU, BV, BW, BY, CA, CB,
            // No JEDEC reference raw card design used
            ZZ,
        }

        /// <summary>
        /// Defines the nominal height in millimeters of the fully assembled module including heat spreaders or other added components
        /// </summary>
        public struct ModuleHeightData {
            public float Minimum;
            public float Maximum;
            public HeightUnit Unit;

            public override string ToString() {

                string value = "";

                if (Minimum.Equals(Maximum)) {
                    value = $"{Maximum}";
                }
                else if (Minimum < Maximum) {
                    value = $"{Minimum}-{Maximum}";
                }
                else if (Minimum > Maximum) {
                    value = $"{Minimum}+";
                }

                return $"{value} {Unit}";
            }
        }

        /// <summary>
        /// Height units
        /// </summary>
        public enum HeightUnit {
            mm,
            IN,
        }

        /// <summary>
        /// Defines the maximum thickness in millimeters of the fully assembled module including heat spreaders
        /// or other added components above the module circuit board surface
        /// </summary>
        public struct ModuleMaximumThicknessSideData {
            public ModuleHeightData Back;
            public ModuleHeightData Front;
        }

        /// <summary>
        /// Describes the module’s refresh rate in microseconds
        /// </summary>
        public struct RefreshRateData {
            public byte RefreshPeriod;
            public bool SelfRefresh;

            public float ToMicroseconds() {

                byte refPerValue = Data.SetBit(RefreshPeriod, 7, false);
                float normal = 15.625F;

                // Normal
                if (refPerValue == 0x00) {
                    return normal;
                }

                // Reduced
                if (0x01 <= refPerValue && refPerValue <= 0x02) {
                    return normal * 0.25F * refPerValue;
                }

                // Extended
                if (0x03 <= refPerValue && refPerValue <= 0x05) {
                    return (float)(normal * Math.Pow(2, refPerValue - 1));
                }

                throw new ArgumentOutOfRangeException(nameof(RefreshPeriod));
            }

            public override string ToString() {
                return ToMicroseconds().ToString("F3");
            }
        }

        /// <summary>
        /// SPD performance profiles IDs as they appear in SPD
        /// </summary>
        public struct ProfileId {
            /// <summary>
            /// Intel Extreme Memory Profile ID String ("magic bytes")
            /// </summary>
            public static byte[] XMP = { 0x0C, 0x4A };

            /// <summary>
            /// EPP Identifier String ("NVm")
            /// </summary>
            public static byte[] EPP => Data.ReverseArray(Encoding.ASCII.GetBytes("NVm"));

            /// <summary>
            /// AMD EXPO Identifier String ("EXPO")
            /// </summary>
            public static byte[] EXPO => Encoding.ASCII.GetBytes("EXPO");
        }
    }
}