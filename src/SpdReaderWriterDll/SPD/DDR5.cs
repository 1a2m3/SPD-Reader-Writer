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
using System.Data;

namespace SpdReaderWriterCore {
    public partial class Spd {

        /// <summary>
        /// DDR5 SPD
        /// </summary>
        public struct DDR5 : ISpd {

            /// <summary>
            /// New instance of DDR5 SPD class
            /// </summary>
            /// <param name="input">Raw SPD data</param>
            public DDR5(byte[] input) {
                if (input.Length == Length) {
                    RawData = input;
                }
                else {
                    throw new DataException();
                }
            }

            /// <summary>
            /// Total SPD size
            /// </summary>
            public int Length => DataLength.DDR5;

            public override string ToString() =>
                $"{GetManufacturerName(ManufacturerIdCode.ManufacturerId)} {PartNumber}".Trim();

            /// <summary>
            /// Byte 0 (0x000): Number of Bytes in SPD Device
            /// </summary>
            public BytesData Bytes {
                get => new BytesData {
                    Used = (ushort)(128 * (ushort)Math.Pow(2, Data.SubByte(RawData[0], 6, 3)))
                };
            }

            /// <summary>
            /// Number of SPD Bytes Used
            /// </summary>
            public int SpdBytesUsed => Bytes.Used;

            /// <summary>
            /// Byte 1 (0x001): SPD Revision
            /// </summary>
            public SpdRevisionData SpdRevision {
                get => new SpdRevisionData {
                    EncodingLevel  = Data.SubByte(RawData[1], 7, 4),
                    AdditionsLevel = Data.SubByte(RawData[1], 3, 4)
                };
            }

            /// <summary>
            /// Byte 2 (0x002): Key Byte / DRAM Device Type
            /// </summary>
            public RamType DramDeviceType {
                get => (RamType)RawData[2];
            }

            /// <summary>
            /// Byte 3 (0x003): Key Byte / Module Type
            /// </summary>
            public ModuleTypeData ModuleType {
                get => new ModuleTypeData {
                    Hybrid         = Data.GetBit(RawData[3], 7),
                    HybridMedia    = (HybridMediaData)Data.SubByte(RawData[3], 6, 3),
                    BaseModuleType = (BaseModuleType)Data.SubByte(RawData[3], 3, 4)
                };
            }

            /// <summary>
            /// Module Type data
            /// </summary>
            public struct ModuleTypeData {
                public bool Hybrid;
                public HybridMediaData HybridMedia;
                public BaseModuleType BaseModuleType;

                public override string ToString() =>
                    $"{Data.GetEnumDescription(BaseModuleType)}";
            }

            /// <summary>
            /// Hybrid memory extensions
            /// </summary>
            public enum HybridMediaData {
                [Description("Not hybrid")]
                NONE    = 0,
                [Description("NVDIMM-N Hybrid")]
                NVDIMMN = 1,
                [Description("NVDIMM-P Hybrid")]
                NVDIMMP = 2,
            }

            /// <summary>
            /// Base Module Type
            /// </summary>
            public enum BaseModuleType {
                
                [Description("RDIMM")]
                RDIMM       = 0b0001,

                [Description("UDIMM")]
                UDIMM       = 0b0010,

                [Description("SO-DIMM")]
                SO_DIMM     = 0b0011,

                [Description("LRDIMM")]
                LRDIMM      = 0b0100,

                [Description("DDIMM")]
                DDIMM       = 0b1010,

                [Description("Solder down")]
                Solder_Down = 0b1010,
            }

            /// <summary>
            /// Asymmetric DRAM values offset
            /// </summary>
            private const int ASYMM_RAM_DATA_OFFSET = 4;

            /// <summary>
            /// Byte 4 (0x004): First SDRAM Density and Package
            /// Byte 8 (0x008): Second SDRAM Density and Package
            /// </summary>
            public DensityPackageData[] DensityPackage {
                get {
                    DensityPackageData[] densityPackage = new DensityPackageData[2];

                    byte[] densityList = { 0, 4, 8, 12, 16, 24, 32, 48, 64 };

                    for (byte i = 0; i < densityPackage.Length; i++) {

                        byte diePerPackageValue = Data.SubByte(RawData[4 + ASYMM_RAM_DATA_OFFSET * i], 7, 4);

                        densityPackage[i].DiePerPackageCount = (byte)(diePerPackageValue == 0 ? 1 : diePerPackageValue);
                        densityPackage[i].DieDensity         = densityList[Data.SubByte(RawData[4 + ASYMM_RAM_DATA_OFFSET * i], 3, 4)];
                    }

                    return densityPackage;
                }
            }
            
            /// <summary>
            /// Describes SDRAM package type, loading as seen by the system, and device density in Gbits
            /// </summary>
            public struct DensityPackageData {
                /// <summary>
                /// Die Per Package 
                /// </summary>
                public byte DiePerPackageCount;
                /// <summary>
                /// SDRAM Density Per Die in Gigabits
                /// </summary>
                public byte DieDensity;
            }

            /// <summary>
            /// Byte 5 (0x005): First SDRAM Addressing
            /// Byte 9 (0x009): Second SDRAM Addressing
            /// </summary>
            public AddressingData[] Addressing {
                get {
                    AddressingData[] addressing = new AddressingData[2];

                    for (byte i = 0; i < addressing.Length; i++) {
                        addressing[i].Columns = (byte)(Data.SubByte(RawData[5 + ASYMM_RAM_DATA_OFFSET * i], 7, 3) + 10);
                        addressing[i].Rows    = (byte)(Data.SubByte(RawData[5 + ASYMM_RAM_DATA_OFFSET * i], 4, 5) + 16);
                    }

                    return addressing;
                }
            }

            /// <summary>
            /// Byte 6 (0x006): First SDRAM I/O Width
            /// Byte 10 (0x00A): Secondary SDRAM I/O Width
            /// </summary>
            public byte[] IoWidth {
                get {
                    byte[] ioWidth = new byte[2];

                    for (int i = 0; i < ioWidth.Length; i++) {
                        ioWidth[i] = Data.SubByte(RawData[6 + ASYMM_RAM_DATA_OFFSET * i], 7, 3);
                    }

                    return ioWidth;
                }
            }

            /// <summary>
            /// Byte 7 (0x007): First SDRAM Bank Groups and Banks Per Bank Group
            /// Byte 11 (0x00B): Second SDRAM Bank Groups and Banks Per Bank Group
            /// </summary>
            public BankGroupsData[] BankGroups {
                get {
                    BankGroupsData[] bankGroups = new BankGroupsData[2];

                    for (int i = 0; i < bankGroups.Length; i++) {
                        bankGroups[i].BankGroupCount        = (byte)Math.Pow(2, Data.SubByte(RawData[7 + ASYMM_RAM_DATA_OFFSET * i], 7, 3));
                        bankGroups[i].BankPerBankGroupCount = (byte)Math.Pow(2, Data.SubByte(RawData[7 + ASYMM_RAM_DATA_OFFSET * i], 2, 3));
                    }

                    return bankGroups;
                }
            }

            /// <summary>
            /// Describes the number SDRAM banks per bank group
            /// </summary>
            public struct BankGroupsData {
                public byte BankGroupCount;
                public byte BankPerBankGroupCount;
            }

            /// <summary>
            /// (Common): Byte 234 (0x0EA) Module Organization
            /// </summary>
            public ModuleOrganizationData ModuleOrganization {
                get => new ModuleOrganizationData {
                        RankMix          = (RankMix)Data.BoolToNum<byte>(Data.GetBit(RawData[234], 6)),
                        PackageRankCount = Data.SubByte(RawData[234], 5, 3) + 1,
                };
            }

            /// <summary>
            /// Describes the organization of the SDRAM module.
            /// </summary>
            public struct ModuleOrganizationData {
                public RankMix RankMix;
                public int PackageRankCount;
            }

            /// <summary>
            /// Module Organization Rank Mix
            /// </summary>
            public enum RankMix {
                Symmetrical  = 0,
                Asymmetrical = 1
            }

            /// <summary>
            /// (Common): Byte 235 (0x0EB) Memory Channel Bus Width
            /// </summary>
            public ChannelBusWidthData ChannelBusWidth {
                get {
                    return new ChannelBusWidthData {
                        ChannelCount              = (byte)Math.Pow(2, Data.SubByte(RawData[235], 6, 2)),
                        BusWidthExtension         = (byte)(Data.SubByte(RawData[235], 4, 2) * 4),
                        PrimaryBusWidthPerChannel = (byte)((1 << Data.SubByte(RawData[235], 2, 3) + 3) & 0xF8)
                    };
                }
            }

            /// <summary>
            /// Describes the width of the SDRAM memory bus on the module
            /// </summary>
            public struct ChannelBusWidthData {
                public byte ChannelCount;
                public byte BusWidthExtension;
                public byte PrimaryBusWidthPerChannel;
            }

            /// <summary>
            /// Total memory capacity of the DRAM on a DIMM
            /// </summary>
            public ulong TotalModuleCapacity {
                get {
                    return ModuleOrganization.RankMix == RankMix.Symmetrical
                        ? (ulong)(
                        // Number of channels per DIMM *
                        ChannelBusWidth.ChannelCount *
                        // Primary bus width per channel / SDRAM I/O Width *
                        ChannelBusWidth.PrimaryBusWidthPerChannel / IoWidth[0] *
                        // Die per package*
                        DensityPackage[0].DiePerPackageCount *
                        // SDRAM density per die / 8 *
                        DensityPackage[0].DieDensity / 8 *
                        // Package ranks per channel
                        ModuleOrganization.PackageRankCount)
                        : 0;
                }
            }

            /// <summary>
            /// CRC checksums
            /// </summary>
            public Crc16Data[] Crc {
                get {
                    // Base Configuration, DRAM and Module Parameters
                    int sectionCount = 1;
                    
                    // Add 1 AMD Expo profile, if present
                    if (ExpoPresence) {
                        sectionCount++;
                    }

                    // Add 1 XMP header and up to 5 XMP profiles, if present
                    if (XmpPresence) {

                        // XMP header
                        sectionCount++;

                        // XMP profiles
                        for (int i = 0; i <= 5; i++) {
                            if (RawData[i * Xmp30ProfileData.Length + Xmp30ProfileData.Offset] == 0x30) {
                                sectionCount++;
                            }
                        }
                    }

                    Crc16Data[] crc = new Crc16Data[sectionCount];

                    // Base checksum
                    ushort baseSectionLength = 512;
                    crc[0].Contents = new byte[baseSectionLength];

                    Array.Copy(
                        sourceArray      : RawData,
                        destinationArray : crc[0].Contents,
                        length           : crc[0].Contents.Length);

                    if (sectionCount > 1) {

                        byte i = 1; // Profile counter
                        byte j = 1; // Offset counter

                        while (i < sectionCount) {

                            int offset = Xmp30ProfileData.Offset + (j - 1) * Xmp30ProfileData.Length;
                            j++;

                            // XMP checksum(s)
                            if (XmpPresence && (RawData[offset] == 0x30 || offset == Xmp30ProfileData.Offset)) {
                                crc[i].Contents = new byte[Xmp30ProfileData.Length];
                            }
                            // Expo checksum
                            else if (ExpoPresence && offset == ExpoProfileData.Offset) {
                                crc[i].Contents = new byte[ExpoProfileData.Length];
                            }
                            else {
                                continue;
                            }

                            Array.Copy(
                                sourceArray      : RawData,
                                sourceIndex      : offset,
                                destinationArray : crc[i].Contents,
                                destinationIndex : 0,
                                length           : crc[i].Contents.Length);

                            i++;
                        }
                    }

                    return crc;
                }
            }

            /// <summary>
            /// CRC validation status
            /// </summary>
            public bool CrcStatus {
                get {
                    foreach (Crc16Data crc16Data in Crc) {
                        if (!crc16Data.Validate()) {
                            return false;
                        }
                    }

                    return true;
                }
            }

            /// <summary>
            /// Fixes CRC checksums
            /// </summary>
            /// <returns><see langword="true"/> if checksum(s) has been fixed</returns>
            public bool FixCrc() {

                int sectionCount = Crc.Length;

                if (sectionCount > 0) {
                    Array.Copy(
                        sourceArray      : Crc[0].Fix(),
                        destinationArray : RawData,
                        length           : Crc[0].Contents.Length);
                }

                if (sectionCount > 1) {

                    byte i = 1; // Profile counter
                    byte j = 1; // Offset counter

                    while (i < sectionCount) {

                        int destinationIndex = Xmp30ProfileData.Offset + (j - 1) * Xmp30ProfileData.Length;
                        j++;

                        // AMD Expo
                        if (ExpoPresence && 
                            Crc[i].Contents.Length == ExpoProfileData.Length && 
                            Data.MatchArray(Crc[i].Contents, ProfileId.EXPO, 0)) {
                            destinationIndex = ExpoProfileData.Offset;
                        }
                        // Not Intel XMP
                        else if (
                            !(XmpPresence && Crc[i].Contents.Length == Xmp30ProfileData.Length &&
                             (RawData[destinationIndex] == 0x30 || destinationIndex == Xmp30ProfileData.Offset))) {
                            continue;
                        }
                        
                        Array.Copy(
                            sourceArray      : Crc[i].Fix(),
                            sourceIndex      : 0,
                            destinationArray : RawData,
                            destinationIndex : destinationIndex,
                            length           : Crc[i].Contents.Length);

                        i++;
                    }
                }

                return CrcStatus;
            }

            /// <summary>
            /// Byte 512 (0x200): Module Manufacturer ID Code, First Byte
            /// Byte 513 (0x201): Module Manufacturer ID Code, Second Byte
            /// </summary>
            public ManufacturerIdCodeData ManufacturerIdCode {
                get => new ManufacturerIdCodeData {
                    ContinuationCode = RawData[512],
                    ManufacturerCode = RawData[513]
                };
            }

            /// <summary>
            /// Bytes 515~516 (0x203~0x204): Module Manufacturing Date
            /// </summary>
            public DateCodeData ModuleManufacturingDate {
                get => new DateCodeData {
                    Year = RawData[515],
                    Week = RawData[516]
                };
            }

            /// <summary>
            /// Bytes 517~520 (0x205~0x208): Module Serial Number
            /// </summary>
            public SerialNumberData SerialNumber {

                get {
                    byte[] serialNumberBytes = new byte[4];

                    Array.Copy(
                        sourceArray      : RawData,
                        sourceIndex      : 517,
                        destinationArray : serialNumberBytes,
                        destinationIndex : 0,
                        length           : serialNumberBytes.Length);

                    return new SerialNumberData {
                        SerialNumber = serialNumberBytes
                    };
                }
            }

            /// <summary>
            /// Bytes 521~550 (0x209~0x226): Module Part Number
            /// </summary>
            public string PartNumber {
                get {
                    int modelNameStart = 521;

                    char[] chars = new char[550 - modelNameStart + 1];

                    Array.Copy(
                        sourceArray      : RawData,
                        sourceIndex      : modelNameStart,
                        destinationArray : chars,
                        destinationIndex : 0,
                        length           : chars.Length);

                    return Data.BytesToString(chars).Trim();
                }
            }

            /// <summary>
            /// XMP header (magic bytes)
            /// </summary>
            public bool XmpPresence {
                get => Data.MatchArray(RawData, ProfileId.XMP, Xmp30ProfileData.Offset);
            }

            /// <summary>
            /// XMP profile type
            /// </summary>
            public enum XmpProfileType {
                Performance,
                Extreme,
                Fastest,
                User1,
                User2
            }

            /// <summary>
            /// DDR5 XMP 3.0 data
            /// </summary>
            public struct Xmp30ProfileData {
                public static ushort Length = 64;
                public static ushort Offset = 0x280; // 640
                
                public XmpProfileType Type;
                public string Name;
            }

            /// <summary>
            /// AMD Expo presence
            /// </summary>
            public bool ExpoPresence {
                get => Data.MatchArray(RawData, ProfileId.EXPO, ExpoProfileData.Offset);
            }

            /// <summary>
            /// DDR5 AMD Expo data
            /// </summary>
            public struct ExpoProfileData {
                public static ushort Length = 128;
                public static ushort Offset = 0x340; // 832
            }
        }
    }
}