/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using System;
using System.IO;
using static SpdReaderWriterDll.Ram;
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
        public static Ram.Type GetRamType(Arduino device) {

            if (device == null) {
                throw new NullReferenceException($"Invalid device");
            }

            if (!device.IsConnected) {
                throw new IOException($"Device not connected ({device.PortName})");
            }

            if (device.DetectDdr5()) {
                return Ram.Type.DDR5;
            }

            if (device.DetectDdr4()) {
                return Ram.Type.DDR4;
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
        public static Ram.Type GetRamType(byte[] input) {

            // Byte at offset 0x02 in SPD indicates RAM type
            return input.Length >= 3 && Enum.IsDefined(typeof(Ram.Type), (Ram.Type)input[0x02]) ? (Ram.Type)input[0x02] : Ram.Type.UNKNOWN;
        }

        /// <summary>
        /// Gets total EEPROM size
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns>SPD size</returns>
        public static SpdSize GetSpdSize(Arduino device) {

            if (device == null) {
                throw new NullReferenceException($"Invalid device");
            }

            if (!device.IsConnected) {
                throw new IOException($"Device not connected ({device.PortName})");
            }

            if (device.DetectDdr5()) {
                return SpdSize.DDR5;
            }

            if (device.DetectDdr4()) { 
                return SpdSize.DDR4;
            }

            if (device.Scan().Length != 0) {
                return SpdSize.MINIMUM;
            }

            return 0;            
        }

        /// <summary>
        /// Gets total EEPROM size
        /// </summary>
        /// <param name="ramType">Ram Type</param>
        /// <returns>SPD size</returns>
        public static SpdSize GetSpdSize(Ram.Type ramType) {

            switch (ramType) {
                case Ram.Type.SDRAM:
                case Ram.Type.DDR:
                case Ram.Type.DDR2:
                case Ram.Type.DDR2_FB_DIMM:
                case Ram.Type.DDR3:
                    return SpdSize.MINIMUM;
                case Ram.Type.DDR4:
                    return SpdSize.DDR4;
                case Ram.Type.DDR5:
                    return SpdSize.DDR5;
                default:
                    return SpdSize.UNKNOWN;
            }
        }

        /// <summary>
        /// Validates SPD data
        /// </summary>
        /// <param name="input">SPD contents</param>
        /// <returns><see langword="true"/> if <paramref name="input"/> data is a valid SPD dump</returns>
        public static bool ValidateSpd(byte[] input) {

            switch (input.Length) {
                case (int)SpdSize.DDR5 when GetRamType(input) == Ram.Type.DDR5:
                case (int)SpdSize.DDR4 when GetRamType(input) == Ram.Type.DDR4:
                    return true;
                case (int)SpdSize.MINIMUM:
                    return GetRamType(input) == Ram.Type.DDR3 || 
                           GetRamType(input) == Ram.Type.DDR2 || 
                           GetRamType(input) == Ram.Type.DDR  ||
                           GetRamType(input) == Ram.Type.SDRAM;
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
                case Ram.Type.DDR5:
                    manufacturerId = (UInt16)((UInt16)(input[0x200] << 8 | input[0x201]) & 0x7FFF);
                    break;
                case Ram.Type.DDR4:
                    manufacturerId = (UInt16)((input[0x140] << 8 | input[0x141]) & 0x7FFF);
                    break;
                case Ram.Type.DDR3:
                case Ram.Type.DDR2_FB_DIMM:
                    manufacturerId = (UInt16)((input[0x75] << 8 | input[0x76]) & 0x7FFF);
                    break;

                // Vendor ID location for DDR2 and older RAM SPDs
                default:
                    int vendorIdOffsetStart = 0x40;
                    int vendorIdOffsetEnd   = 0x47;

                    byte[] manufacturerIdArray = new byte[vendorIdOffsetEnd - vendorIdOffsetStart];

                    for (UInt8 i = 0; i < manufacturerIdArray.Length; i++) {
                        manufacturerIdArray[i] = input[vendorIdOffsetStart + i];

                        if (manufacturerIdArray[i] == 0x7F) {
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

            switch (manufacturerId) {
                case 0x0B01: return "ABIT Electronics";
                case 0x0AD9: return "Adamway";
                case 0x04CB: return "A-DATA Technology";
                case 0x0557: return "AENEON";
                case 0x017A: return "Apacer Technology";
                case 0x0816: return "Approved Memory Corporation";
                case 0x08F1: return "Asgard";
                case 0x06C1: return "ASint Technology";
                case 0x09F1: return "ASK Technology Group";
                case 0x0616: return "Avexir Technologies Corporation";
                case 0x0383: return "Buffalo Technology";
                case 0x0319: return "Centon Electronics";
                case 0x04E9: return "Century Micro Inc.";
                case 0x0A45: return "Chunwell";
                case 0x09EC: return "Colorful Technology";
                case 0x029E: return "Corsair Memory";
                case 0x059B: return "Crucial Technology";
                case 0x055D: return "CSX";
                case 0x01DA: return "DANE-ELEC Memory";
                case 0x08F7: return "DSL (Data Specialties Co, Ltd)";
                case 0x070B: return "EKMemory";
                case 0x02FE: return "Elpida Memory";
                case 0x0757: return "Eorex Corporation";
                case 0x0898: return "Essencore Limited";
                case 0x3842: return "EVGA Corporation";
                case 0x0975: return "EXCELERAM";
                case 0x04CD: return "G.Skill International";
                case 0x0313: return "GeIL (Golden Empire)";
                case 0x09F2: return "GIGA-BYTE Technology";
                case 0x0746: return "Gloway International (HK)";
                case 0x0813: return "Gloway International Co Ltd";
                case 0x0562: return "GoldenMars Technology";
                case 0x0945: return "GoldKey Technology";
                case 0x00C1: return "Infineon";
                case 0x06F1: return "InnoDisk Corporation";
                case 0x0968: return "Kimtigo Semiconductor (HK)";
                case 0x0B92: return "Kingbank Technology";
                case 0x0768: return "KingboMars Technology";
                case 0x0451: return "KINGBOX Technology";
                case 0x0651: return "Kinglife";
                case 0x0325: return "Kingmax Semiconductor";
                case 0x0198: return "Kingston";
                case 0x06A7: return "KINGXCON";
                case 0x09C2: return "Kllisre";
                case 0x0A76: return "Lexar Co Limited";
                case 0x09A2: return "MAXSUN";
                case 0x0264: return "MDT Technologies GmbH";
                case 0x036D: //return "Memorysolution GmbH";
                case 0x08B3: return "Memorysolution GmbH";
                case 0x0752: return "Memphis Electronic";
                case 0x002C: return "Micron Technology";
                case 0x01D5: return "MSC Vertriebs GmbH";
                case 0x0394: return "Mushkin Enhanced MFG";
                case 0x07CE: return "Mustang";
                case 0x030B: return "Nanya Technology";
                case 0x09AD: return "Neo Forza";
                case 0x0216: return "Netlist";
                case 0x04B0: return "OCZ Technology";
                case 0x0770: return "Panram";
                case 0x0502: return "Patriot Memory";
                case 0x09F8: return "PCCOOLER";
                case 0x01BA: return "PNY Electronics";
                case 0x053E: return "PQI";
                case 0x0040: return "ProMOS";
                case 0x0551: return "Qimonda AG";
                case 0x0443: return "Ramaxel Technology";
                case 0x0725: return "RAmos Technology";
                case 0x071C: return "Ramsta";
                case 0x0AFE: return "Reeinno Technology";
                case 0x00CE: return "Samsung (SEC)";
                case 0x06E9: return "SanMax Technologies";
                case 0x06D3: return "Silicon Power";
                case 0x0AAB: return "SINKER";
                case 0x00AD: return "SK hynix";
                case 0x0194: return "Smart Modular";
                case 0x02B5: return "SpecTek Incorporated";
                case 0x01A8: return "STEC";
                case 0x0634: return "Super Talent";
                case 0x03DA: return "Swissbit";
                case 0x0358: return "takeMS International AG";
                case 0x04EF: return "Team Group Inc.";
                case 0x0973: return "Terabyte Co. Ltd.";
                case 0x0AC2: return "Thermaltake Technology";
                case 0x0710: return "TIGO Semiconductor";
                case 0x074F: return "TOPRAM Technology";
                case 0x014F: return "Transcend Information";
                case 0x08A2: return "TSP Global";
                case 0x066B: return "TwinMOS";
                case 0x0B29: return "UltraMemory Inc";
                case 0x088C: return "UMAX Technology";
                case 0x0707: return "Unifosa";
                case 0x09D0: return "Veineda Technology";
                case 0x0A94: return "V-GEN";
                case 0x0140: return "Viking Technology";
                case 0x05BA: return "Virtium Technology";
                case 0x05D6: return "Walton Chaintech";
                case 0x075D: return "Wilk Elektronik";
                case 0x0161: return "Wintec Industries";

                default: return "";
            }
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
                case Ram.Type.DDR5:
                    modelNameStart = 0x209;
                    modelNameEnd   = 0x226;
                    break;

                // Part number location for DDR4 SPDs
                case Ram.Type.DDR4:
                    modelNameStart = 0x149;
                    modelNameEnd   = 0x15C;
                    break;

                // Part number location for DDR3 SPDs
                case Ram.Type.DDR3:
                case Ram.Type.DDR2_FB_DIMM:
                    modelNameStart = 0x80;
                    modelNameEnd   = 0x91;
                    break;

                // Part number for Kingston DDR2 and DDR SPDs
                case Ram.Type.DDR2 when GetModuleManufacturer(input).StartsWith("Kingston"):
                case Ram.Type.DDR  when GetModuleManufacturer(input).StartsWith("Kingston"):
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
    }
}