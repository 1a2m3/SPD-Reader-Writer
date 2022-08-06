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
    public class Spd {

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

            if (device.Scan().Length != 0) {
                return DataLength.MINIMUM;
            }

            return 0;            
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
                    return DataLength.MINIMUM;
                case RamType.DDR4:
                    return DataLength.DDR4;
                case RamType.DDR5:
                    return DataLength.DDR5;
                default:
                    return DataLength.UNKNOWN;
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
                case (int)DataLength.MINIMUM:
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
        public static string GetModuleManufacturer(byte[] input) {
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

            // Manufacturer's identification code table
            UInt8[] manufacturerCodeTable = {
                0x01, 0x02, 0x83, 0x04, 0x85, 0x86, 0x07, 0x08, 0x89, 0x8A, 0x0B, 0x8C, 0x0D, 0x0E, 0x8F, 0x10,
                0x91, 0x92, 0x13, 0x94, 0x15, 0x16, 0x97, 0x98, 0x19, 0x1A, 0x9B, 0x1C, 0x9D, 0x9E, 0x1F, 0x20,
                0xA1, 0xA2, 0x23, 0xA4, 0x25, 0x26, 0xA7, 0xA8, 0x29, 0x2A, 0xAB, 0x2C, 0xAD, 0xAE, 0x2F, 0xB0,
                0x31, 0x32, 0xB3, 0x34, 0xB5, 0xB6, 0x37, 0x38, 0xB9, 0xBA, 0x3B, 0xBC, 0x3D, 0x3E, 0xBF, 0x40,
                0xC1, 0xC2, 0x43, 0xC4, 0x45, 0x46, 0xC7, 0xC8, 0x49, 0x4A, 0xCB, 0x4C, 0xCD, 0xCE, 0x4F, 0xD0,
                0x51, 0x52, 0xD3, 0x54, 0xD5, 0xD6, 0x57, 0x58, 0xD9, 0xDA, 0x5B, 0xDC, 0x5D, 0x5E, 0xDF, 0xE0,
                0x61, 0x62, 0xE3, 0x64, 0xE5, 0xE6, 0x67, 0x68, 0xE9, 0xEA, 0x6B, 0xEC, 0x6D, 0x6E, 0xEF, 0x70,
                0xF1, 0xF2, 0x73, 0xF4, 0x75, 0x76, 0xF7, 0xF8, 0x79, 0x7A, 0xFB, 0x7C, 0xFD, 0xFE };

            // Decompress database
            const byte separatorByte = 0x00;
            byte[] idTableCharArray = Data.DecompressGzip(Resources.jedecManufacturersIds);
            string[] names = Data.BytesToString(idTableCharArray).Split((char)separatorByte);

            // Lookup name by continuation code and manufacturer's ID
            byte spdContinuationCode = (byte)((manufacturerId >> 8) & 0xFF);
            byte spdManufacturerCode = (byte)(manufacturerId & 0xFF);

            byte startIndex = (byte)((spdManufacturerCode - 1) & 0x0F);

            for (byte i = startIndex; i < manufacturerCodeTable.Length; i += 0x10) {
                int j = spdContinuationCode * manufacturerCodeTable.Length + i;
                if (spdManufacturerCode == manufacturerCodeTable[i]) {
                    return j < names.Length ? names[j] : "";
                }
            }

            // Unknown
            return "";
        }

        /// <summary>
        /// Gets model name from SPD contents
        /// </summary>
        /// <param name="input">SPD contents</param>
        /// <returns>Model part number</returns>
        public static string GetModulePartNumberName(byte[] input) {

            int modelNameStart;
            int modelNameEnd;

            switch (GetRamType(input)) {

                // Part number location for DDR5 SPDs
                case RamType.DDR5:
                    modelNameStart = 0x209;
                    modelNameEnd   = 0x226;
                    break;

                // Part number location for DDR4 SPDs
                case RamType.DDR4:
                    modelNameStart = 0x149;
                    modelNameEnd   = 0x15C;
                    break;

                // Part number location for DDR3 SPDs
                case RamType.DDR3:
                case RamType.DDR2_FB_DIMM:
                    modelNameStart = 0x80;
                    modelNameEnd   = 0x91;
                    break;

                // Part number for Kingston DDR2 and DDR SPDs
                case RamType.DDR2 when GetModuleManufacturer(input).StartsWith("Kingston"):
                case RamType.DDR  when GetModuleManufacturer(input).StartsWith("Kingston"):
                    modelNameStart = 0xF0;
                    modelNameEnd   = 0xFF;
                    break;

                // Part number location for DDR2 and older RAM SPDs
                default:
                    modelNameStart = 0x49;
                    modelNameEnd   = 0x5A;
                    break;
            }

            if (input.Length < modelNameEnd) {
                throw new InvalidDataException("Incomplete SPD Data");
            }

            char[] chars = new char[modelNameEnd - modelNameStart + 1];

            for (int i = 0; i < chars.Length; i++) {
                chars[i] = (char)input[modelNameStart + i];
            }

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
            [Description("DDR2 FB DIMM")]
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
        }

        /// <summary>
        /// Defines SPD sizes
        /// </summary>
        public enum DataLength {
            UNKNOWN       = 0,
            MINIMUM       = 256, //DDR3, DDR2, DDR, and SDRAM
            DDR4          = 512,
            DDR5          = 1024,
        }
    }
}