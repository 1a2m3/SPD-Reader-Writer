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
using SpdReaderWriterDll.Properties;
using UInt8 = System.Byte;

namespace SpdReaderWriterDll {

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

            // Byte at offset 0x02 in SPD indicates RAM type
            try {
                return GetRamType(Eeprom.ReadByte(device, 0, 3));
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

            // Byte at offset 0x02 in SPD indicates RAM type
            return input.Length >= 3 && Enum.IsDefined(typeof(RamType), (RamType)input[0x02]) ? (RamType)input[0x02] : RamType.UNKNOWN;
        }

        /// <summary>
        /// Gets total EEPROM size
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns>SPD size</returns>
        public static DataLength GetSpdSize(Arduino device) {

            if (device == null) {
                throw new NullReferenceException($"Invalid device");
            }

            if (!device.IsConnected) {
                throw new IOException($"Device not connected ({device.PortName})");
            }

            if (device.DetectDdr5()) {
                return DataLength.DDR5;
            }

            if (device.DetectDdr4()) {
                return DataLength.DDR4;
            }

            return device.Scan().Length > 0 ? DataLength.Minimum : DataLength.Unknown;
        }

        /// <summary>
        /// Gets total EEPROM size
        /// </summary>
        /// <param name="ramType">Ram Type</param>
        /// <returns>SPD size</returns>
        public static DataLength GetSpdSize(RamType ramType) {

            switch (ramType) {
                case RamType.SDRAM:
                case RamType.DDR:
                case RamType.DDR2:
                case RamType.DDR2_FB_DIMM:
                case RamType.DDR3:
                    return DataLength.Minimum;
                case RamType.DDR4:
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

            switch (input.Length) {
                case (int)DataLength.DDR5 when GetRamType(input) == RamType.DDR5:
                case (int)DataLength.DDR4 when GetRamType(input) == RamType.DDR4:
                    return true;
                case (int)DataLength.Minimum:
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
        public static string GetManufacturerName(byte[] input) {
            UInt16 manufacturerId = 0;

            switch (GetRamType(input)) {
                case RamType.DDR5:
                    manufacturerId = (UInt16)((UInt16)(input[0x200] << 8 | input[0x201]) & 0x7FFF);
                    break;
                case RamType.DDR4:
                    manufacturerId = (UInt16)((input[0x140] << 8 | input[0x141]) & 0x7FFF);
                    break;
                case RamType.DDR3:
                case RamType.DDR2_FB_DIMM:
                    manufacturerId = (UInt16)((input[0x75] << 8 | input[0x76]) & 0x7FFF);
                    break;

                // Vendor ID location for DDR2 and older RAM SPDs
                default:
                    int vendorIdOffsetStart = 0x40;
                    int vendorIdOffsetEnd   = 0x47;

                    const byte continuationCode = 0x7F;

                    byte[] manufacturerIdArray = new byte[vendorIdOffsetEnd - vendorIdOffsetStart];

                    for (UInt8 i = 0; i < manufacturerIdArray.Length; i++) {
                        manufacturerIdArray[i] = input[vendorIdOffsetStart + i];

                        if (manufacturerIdArray[i] == continuationCode) {
                            // Set manufacturer's code LSB
                            manufacturerId = (UInt16)((i + 1) << 8);
                        }
                        else {
                            // Set manufacturer's code MSB
                            manufacturerId |= manufacturerIdArray[i];
                            break;
                        }
                    }

                    break;
            }

            return GetManufacturerName(manufacturerId);
        }

        /// <summary>
        /// Gets manufacturer from ID codes
        /// </summary>
        /// <param name="input">MSB and LSB of ID Code</param>
        /// <returns>Manufacturer's name</returns>
        public static string GetManufacturerName(UInt16 input) {

            // Manufacturer's identification code table

            int codeTableCount = 126;

            //UInt8[] manufacturerCodeTable = new byte[codeTableCount];
            //for (UInt8 i = 0; i < 126; i++) {
            //    byte code = (byte)(i + 1);
            //    manufacturerCodeTable[i] = (byte)(code | Data.GetParity(code, Data.Parity.Odd) << 7);
            //}

            // Lookup name by continuation code and manufacturer's ID
            byte spdContinuationCode = (byte)((input >> 8) & 0x7F);
            byte spdManufacturerCode = (byte)(input & 0x7F); // Ignore parity bit

            if (spdContinuationCode > Resources.Database.jedecManufacturersIds.Length - 1) {
                return "";
            }

            // Decompress database
            const byte separatorByte = 0x0A;
            byte[] idTableCharArray = Data.DecompressGzip(Resources.Database.jedecManufacturersIds[spdContinuationCode]);
            string[] names = Data.BytesToString(idTableCharArray).Split((char)separatorByte);

            return spdManufacturerCode <= names.Length ? names[spdManufacturerCode - 1] : "";
        }

        /// <summary>
        /// Gets model name from SPD contents
        /// </summary>
        /// <param name="input">SPD contents</param>
        /// <returns>Model part number</returns>
        public static string GetModulePartNumberName(byte[] input) {

            int[] modelNameLocation;

            switch (GetRamType(input)) {

                // Part number location for DDR5 SPDs
                case RamType.DDR5:
                    modelNameLocation = new[] { 0x209, 0x226 };
                    break;

                // Part number location for DDR4 SPDs
                case RamType.DDR4:
                    modelNameLocation = new[] { 0x149, 0x15C };
                    break;

                // Part number location for DDR3 SPDs
                case RamType.DDR3:
                case RamType.DDR2_FB_DIMM:
                    modelNameLocation = new[] { 0x80, 0x91 };
                    break;

                // Part number for Kingston DDR2 and DDR SPDs
                case RamType.DDR2 when GetManufacturerName(input).StartsWith("Kingston"):
                case RamType.DDR  when GetManufacturerName(input).StartsWith("Kingston"):
                    modelNameLocation = new[] { 0xF0, 0xFF };
                    break;

                // Part number location for DDR2 and older RAM SPDs
                default:
                    modelNameLocation = new[] { 0x49, 0x5A };
                    break;
            }

            if (input.Length < modelNameLocation[1]) {
                throw new InvalidDataException("Incomplete SPD Data");
            }

            char[] chars = new char[modelNameLocation[1] - modelNameLocation[0] + 1];

            Array.Copy(
                sourceArray      : input,
                sourceIndex      : modelNameLocation[0],
                destinationArray : chars,
                destinationIndex : 0,
                length           : chars.Length);

            return Data.BytesToString(chars);
        }

        /// <summary>
        /// Defines basic memory type byte value
        /// </summary>
        public enum RamType {
            UNKNOWN       = 0x00,
            SDRAM         = 0x04,
            DDR           = 0x07,
            DDR2          = 0x08,
            [Description("DDR2 Fully-Buffered DIMM")]
            DDR2_FB_DIMM  = 0x09,
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
        public enum DataLength {
            Unknown = 0,
            Minimum = 256, // DDR3, DDR2, DDR, and SDRAM
            DDR4    = 512, // incl. LPDDR3
            DDR5    = 1024,
        }

        /// <summary>
        /// Raw SPD data contents
        /// </summary>
        public static byte[] RawData {
            get => _rawData;
            set => _rawData = value;
        }
        private static byte[] _rawData;

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
            public UInt16 Used;
            public UInt16 Total;

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
            public UInt8 PrimaryBusWidth;

            public override string ToString() => ((UInt8)(PrimaryBusWidth + (Extension ? 8 : 0))).ToString();
        }

        /// <summary>
        /// Row addressing and the column addressing in the SDRAM device
        /// </summary>
        public struct AddressingData {
            public UInt8 Rows;
            public UInt8 Columns;
        }

        /// <summary>
        /// Describes the type of SDRAM devices on the module
        /// </summary>
        public struct PrimaryPackageTypeData {
            public bool Monolithic;
            public UInt8 DieCount;
            public SignalLoadingData SignalLoading;

            public override string ToString() {
                return Monolithic ? "Monolithic" : SignalLoading.ToString().Replace("_", " ").Trim();
            }
        }

        /// <summary>
        /// Refers to loading on signals at the SDRAM balls
        /// </summary>
        public enum SignalLoadingData {
            Not_Specified, // Monolithic
            Multi_Load_Stack,
            Single_Load_Stack,
        }

        /// <summary>
        /// Data Size prefixes
        /// </summary>
        public enum CapacityPrefix : UInt64 {
            K = 1024,
            M = K * K,
            G = M * K,
            T = G * K,
            P = T * K,
            E = P * K,
        }

        /// <summary>
        /// Support for certain SDRAM features
        /// </summary>
        public struct MaximumActivateFeaturesData {
            public UInt16 MaximumActivateWindow;
            public MaximumActivateCount MaximumActivateCount;
        }

        /// <summary>
        /// Maximum Activate Count (MAC)
        /// </summary>
        public enum MaximumActivateCount {
            Untested,
            _700K, _600K, _500K, _400K, _300K, _200K, Reserved,
            Unlimited
        }

        /// <summary>
        /// Module’s voltage interface
        /// </summary>
        public enum VoltageLevel {
            /// <summary>
            /// TTL/5 V tolerant
            /// </summary>
            TTL,
            /// <summary>
            /// LVTTL (not 5 V tolerant)
            /// </summary>
            LVTTL,
            /// <summary>
            /// HSTL 1.5 V
            /// </summary>
            HSTL,
            /// <summary>
            /// SSTL 3.3 V
            /// </summary>
            SSTL33,
            /// <summary>
            /// SSTL 2.5 V
            /// </summary>
            SSTL25,
            /// <summary>
            /// SSTL 1.8 V
            /// </summary>
            SSTL18,
        }

        /// <summary>
        /// Manufacturer ID Code
        /// </summary>
        public struct ManufacturerIdCodeData {
            public byte ContinuationCode;
            public byte ManufacturerCode;

            public override string ToString() => GetManufacturerName((UInt16)(ContinuationCode << 8 | ManufacturerCode));
        }

        /// <summary>
        /// Date code
        /// These bytes must be represented in Binary Coded Decimal
        /// </summary>
        public struct DateCodeData {
            public UInt8 Year;
            public UInt8 Week;

            public override string ToString() {
                UInt16 year = (UInt16)(Data.ByteToBinaryCodedDecimal(Year) + 2000);
                UInt8 week  = Data.ByteToBinaryCodedDecimal(Week);

                return 0 < week && week < 53 ? $"{year:D4}/{week:D2}" : "";
            }
        }

        /// <summary>
        /// Module Serial Number data
        /// </summary>
        public struct SerialNumberData {
            public byte[] SerialNumber;

            public override string ToString() {
                string output = "";

                foreach (byte b in SerialNumber) {
                    output += $"{b:X2}";
                }

                return output;
            }
        }

        /// <summary>
        /// CRC16 header and checksum
        /// </summary>
        public struct Crc16Data {
            public byte[] Contents;
            public UInt16 Checksum;

            /// <summary>
            /// Validates data checksum
            /// </summary>
            /// <returns><see langword="true"/> if <see cref="Checksum"/> is valid for <see cref="Contents"/></returns>
            public bool Validate() => Data.Crc16(Contents, 0x1021) == Checksum;

            public override string ToString() => ((CrcStatus)Data.BoolToNum(Validate())).ToString();
        }

        /// <summary>
        /// CRC header and checksum
        /// </summary>
        public struct Crc8Data {
            public byte[] Contents;
            public UInt8 Checksum;

            /// <summary>
            /// Validates data checksum
            /// </summary>
            /// <returns><see langword="true"/> if <see cref="Checksum"/> is valid for <see cref="Contents"/></returns>
            public bool Validate() => Data.Crc(Contents) == Checksum;

            public override string ToString() => ((CrcStatus)Data.BoolToNum(Validate())).ToString();
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
            public UInt8 Revision;
            public ReferenceRawCardName Name;
        }

        /// <summary>
        /// Reference Raw Card
        /// </summary>
        /// <remarks>
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

                if (Minimum == Maximum) {
                    value += Maximum;
                }
                else if (Minimum < Maximum) {
                    value += $"{Minimum}-{Maximum}";
                }
                else if (Minimum > Maximum) {
                    value += $"{Minimum}+";
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
        public struct ModuleMaximumThicknessSide {
            public ModuleHeightData Back;
            public ModuleHeightData Front;
        }
    }
}