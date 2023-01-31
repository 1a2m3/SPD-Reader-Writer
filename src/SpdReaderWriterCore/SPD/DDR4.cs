/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;

namespace SpdReaderWriterCore {
    public partial class Spd {

        /// <summary>
        /// DDR4 SPD
        /// </summary>
        public struct DDR4 : ISpd {

            /// <summary>
            /// New instance of DDR4 SPD class
            /// </summary>
            /// <param name="input">Raw SPD data</param>
            public DDR4(byte[] input) {
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
            public int Length => DataLength.DDR4;

            public override string ToString() =>
                $"{GetManufacturerName(ManufacturerIdCode.ManufacturerId)} {PartNumber}".Trim();

            /// <summary>
            /// Byte 0 (0x000): Number of Bytes Used / Number of Bytes in SPD Device
            /// </summary>
            public BytesData Bytes {
                get => new BytesData {
                    Used  = (ushort)(Data.SubByte(RawData[0], 3, 4) * 128),
                    Total = (ushort)(Data.SubByte(RawData[0], 6, 3) * 256),
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
                    HybridMedia    = Data.GetBit(RawData[3], 4),
                    BaseModuleType = (BaseModuleType)Data.SubByte(RawData[3], 3, 4)
                };
            }

            /// <summary>
            /// Module Type data
            /// </summary>
            public struct ModuleTypeData {
                public bool Hybrid;
                public bool HybridMedia;
                public BaseModuleType BaseModuleType;

                public override string ToString() =>
                    $"{Data.GetEnumDescription(BaseModuleType)}";
            }

            /// <summary>
            /// Base Module Type
            /// </summary>
            public enum BaseModuleType {
                [Description("Extended DIMM")]
                Extended_DIMM = 0b0000,

                [Description("RDIMM")]
                RDIMM         = 0b0001,

                [Description("UDIMM")] 
                UDIMM         = 0b0010,

                [Description("SO-DIMM")] 
                SO_DIMM       = 0b0011,

                [Description("LRDIMM")] 
                LRDIMM        = 0b0100,

                [Description("Mini-RDIMM")] 
                Mini_RDIMM    = 0b0101,

                [Description("Mini-UDIMM")] 
                Mini_UDIMM    = 0b0110,

                [Description("72b-SO-RDIMM")] 
                _72b_SO_RDIMM = 0b1000,

                [Description("72b-SO-UDIMM")] 
                _72b_SO_UDIMM = 0b1001,

                [Description("16b-SO-DIMM")] 
                _16b_SO_DIMM  = 0b1100,

                [Description("32b-SO-DIMM")] 
                _32b_SO_DIMM  = 0b1101,
            }

            /// <summary>
            /// Byte 4 (0x004): SDRAM Density and Banks
            /// </summary>
            public DensityBanksData DensityBanks {
                get {
                    byte capacityPerDie = Data.SubByte(RawData[4], 3, 4);

                    return new DensityBanksData {
                        BankGroup           = (byte)(Data.SubByte(RawData[4], 7, 2) * 2),
                        BankAddress         = (byte)(1 << (Data.SubByte(RawData[4], 5, 2) + 2)),
                        TotalCapacityPerDie = !Data.GetBit(RawData[4], 3)
                            ? (ushort)(2 << (capacityPerDie + 7))  // 256Mb-32Gb
                            : (ushort)(3 << (capacityPerDie + 4)), // 12Gb-24Gb
                    };
                }
            }

            /// <summary>
            /// SDRAM Density and Banks data
            /// </summary>
            public struct DensityBanksData {
                /// <summary>
                /// Number of bank Groups
                /// </summary>
                public byte BankGroup;
                /// <summary>
                /// Number of banks in each <see cref="BankGroup"/>
                /// </summary>
                public byte BankAddress;
                /// <summary>
                /// Total SDRAM capacity per die, in megabits
                /// </summary>
                public ushort TotalCapacityPerDie;
            }

            /// <summary>
            /// Byte 5 (0x005): SDRAM Addressing
            /// </summary>
            public AddressingData Addressing {
                get => new AddressingData {
                    Rows    = (byte)(Data.SubByte(RawData[5], 5, 3) + 12),
                    Columns = (byte)(Data.SubByte(RawData[5], 2, 3) + 9),
                };
            }

            /// <summary>
            /// Byte 6 (0x006): Primary SDRAM Package Type
            /// </summary>
            public PrimaryPackageTypeData PrimaryPackageType {
                get => new PrimaryPackageTypeData {
                    Monolithic    = !Data.GetBit(RawData[6], 7),
                    DieCount      = (byte)(Data.SubByte(RawData[6], 6, 3) + 1),
                    SignalLoading = (SignalLoadingData)Data.SubByte(RawData[6], 1, 2),
                };
            }

            /// <summary>
            /// Byte 7 (0x007): SDRAM Optional Features
            /// </summary>
            public MaximumActivateFeaturesData MaximumActivateFeatures {
                get => new MaximumActivateFeaturesData {
                    MaximumActivateWindow = (ushort)(8192 >> Data.SubByte(RawData[7], 5, 2)),
                    MaximumActivateCount  = (MaximumActivateCount)Data.SubByte(RawData[7], 3, 4),
                };
            }

            /// <summary>
            /// Byte 10 (0x00A): Secondary SDRAM Package Type
            /// </summary>
            public SecondaryPackageTypeData SecondaryPackageType {
                get => new SecondaryPackageTypeData {
                    Monolithic    = !Data.GetBit(RawData[10], 7),
                    DieCount      = (byte)(Data.SubByte(RawData[10], 6, 3) + 1),
                    DensityRatio  = Data.SubByte(RawData[10], 3, 2),
                    SignalLoading = (SignalLoadingData)(Data.SubByte(RawData[10], 1, 2)),
                };
            }

            /// <summary>
            /// Secondary SDRAM Package Type data
            /// </summary>
            /// <example>
            /// For modules having asymmetrical assembly of multiple SDRAM package types, this byte defines the secondary set of SDRAMs.
            /// For modules with symmetrical assembly, this byte must be coded as 0x00.
            /// </example>
            public struct SecondaryPackageTypeData {
                public bool Monolithic;
                public Byte DieCount;
                public Byte DensityRatio;
                public SignalLoadingData SignalLoading;

                public override string ToString() {
                    return Monolithic ? "Monolithic" : Data.GetEnumDescription(SignalLoading);
                }
            }

            /// <summary>
            /// Module Nominal Voltage, VDD
            /// </summary>
            public ModuleNominalVoltageData ModuleNominalVoltage {
                get => new ModuleNominalVoltageData {
                    Endurant = Data.GetBit(RawData[11], 1),
                    Operable = Data.GetBit(RawData[11], 0),
                };
            }

            /// <summary>
            /// Describes the Voltage Level for DRAM and other components on the module such as the register or memory buffer, if applicable.
            /// </summary>
            public struct ModuleNominalVoltageData {
                public bool Endurant;
                public bool Operable;
            }

            /// <summary>
            /// Byte 12 (0x00C): Module Organization
            /// </summary>
            public ModuleOrganizationData ModuleOrganization {
                get => new ModuleOrganizationData {
                    RankMix          = (RankMix)Data.BoolToNum<byte>((Data.GetBit(RawData[12], 6))),
                    PackageRankCount = (byte)(Data.SubByte(RawData[12], 5, 3) + 1),
                    DeviceWidth      = (byte)(4 << Data.SubByte(RawData[12], 2, 3))
                };
            }

            /// <summary>
            /// Describes the organization of the SDRAM module.
            /// </summary>
            public struct ModuleOrganizationData {
                public RankMix RankMix;
                public byte PackageRankCount;
                public byte DeviceWidth;
            }

            /// <summary>
            /// Module Organization Rank Mix
            /// </summary>
            public enum RankMix {
                Symmetrical  = 0,
                Asymmetrical = 1
            }

            /// <summary>
            /// Byte 13 (0x00D): Module Memory Bus Width
            /// </summary>
            public BusWidthData BusWidth {
                get => new BusWidthData {
                    Extension       = Data.GetBit(RawData[13], 3),
                    PrimaryBusWidth = (byte)(1 << (Data.SubByte(RawData[13], 2, 3) + 3))
                };
            }

            /// <summary>
            /// Calculated die density in bits
            /// </summary>
            public ulong DieDensity {
                get => (ulong)((1L << Addressing.Rows) *
                               (1L << Addressing.Columns) *
                               DensityBanks.BankAddress *
                               DensityBanks.BankGroup *
                               ModuleOrganization.DeviceWidth);
            }

            /// <summary>
            /// The total calculated memory capacity of the DRAM on the module in bytes
            /// </summary>
            public ulong TotalModuleCapacity {
                get => (ulong)(
                    (1L << Addressing.Rows) * 
                    (1L << Addressing.Columns) * 
                    DensityBanks.BankAddress * 
                    DensityBanks.BankGroup * 
                    (PrimaryPackageType.SignalLoading == SignalLoadingData.Single_Load_Stack
                        ? PrimaryPackageType.DieCount 
                        : 1) * 
                    BusWidth.PrimaryBusWidth * 
                    ModuleOrganization.PackageRankCount / 8);
            }

            /// <summary>
            /// The total programmed memory capacity of the DRAM on the module in bytes
            /// </summary>
            public ulong TotalModuleCapacityProgrammed {
                get => (ulong)(
                    DensityBanks.TotalCapacityPerDie / 8 * 
                    BusWidth.PrimaryBusWidth / ModuleOrganization.DeviceWidth * 
                    ModuleOrganization.PackageRankCount * 
                    (PrimaryPackageType.SignalLoading == SignalLoadingData.Single_Load_Stack
                        ? PrimaryPackageType.DieCount
                        : 1) // PrimaryPackageType.DieCount is 1, when SignalLoading is Monolithic
                    * 1024L * 1024L); 
            }

            /// <summary>
            /// Byte 14 (0x00E): Module Thermal Sensor
            /// </summary>
            public bool ThermalSensor {
                get => Data.GetBit(RawData[14], 7);
                set => Data.SetBit(RawData[14], 7, value);
            }

            /// <summary>
            /// Byte 17 (0x011): Timebases
            /// These values in picoseconds are used as a multiplier for formulating subsequent timing parameters
            /// </summary>
            public static Timing Timebase {
                get => new Timing {
                    Medium = Data.SubByte(RawData[15], 3, 2) == 0 ? 125 : 0,
                    Fine   = (sbyte)(Data.SubByte(RawData[15], 1, 2) == 0 ? 1 : 0)
                };
            }

            /// <summary>
            /// Defines a value in picoseconds that represents the fundamental timebase for fine grain and medium grain timing calculations
            /// </summary>
            public struct Timing {
                public int Medium;
                public sbyte Fine;

                /// <summary>
                /// Allows Timing values to be divided by each other to get clock cycles
                /// </summary>
                /// <param name="t1">First timing</param>
                /// <param name="t2">Second timing</param>
                /// <returns>Number of clock cycles</returns>
                public static int operator /(Timing t1, Timing t2) =>
                    (int)Math.Ceiling((t1.Medium * Timebase.Medium + t1.Fine * Timebase.Fine) / 1000F /
                                      ((t2.Medium * Timebase.Medium + t2.Fine * Timebase.Fine) / 1000F));

                /// <summary>
                /// Allows integer to be divided by timing value to get frequency
                /// </summary>
                /// <param name="d1">Dividend</param>
                /// <param name="t1">Timing</param>
                /// <returns>Frequency in MHz</returns>
                public static int operator /(int d1, Timing t1) =>
                    (int)(d1 / ((t1.Medium * Timebase.Medium + t1.Fine * Timebase.Fine) / 1000F));

                /// <summary>
                /// Converts timing value to clock cycles
                /// </summary>
                /// <param name="t1">Reference timing</param>
                /// <returns>Number of clock cycles</returns>
                public int ToClockCycles(Timing t1) =>
                    (int)Math.Ceiling(ToNanoSeconds() / ((t1.Medium * Timebase.Medium + t1.Fine * Timebase.Fine) / 1000F));

                /// <summary>
                /// Converts timing value to nanoseconds
                /// </summary>
                /// <returns>Delay in nanoseconds</returns>
                public float ToNanoSeconds() =>
                    (Medium * Timebase.Medium + Fine * Timebase.Fine) / 1000F;

                public override string ToString() =>
                    $"{ToNanoSeconds():F3}";
            }

            /// <summary>
            /// Returns timing information based on Medium and Fine grain timing details
            /// </summary>
            /// <param name="mediumOffset">Medium grain timing byte location</param>
            /// <param name="fineOffset">Fine grain timing byte location</param>
            /// <returns>Timing</returns>
            public static Timing TimingAdjustable(short mediumOffset, ushort fineOffset) {
                return new Timing {
                    Medium = RawData[mediumOffset],
                    Fine   = (sbyte)RawData[fineOffset],
                };
            }

            /// <summary>
            /// Byte 18 (0x012): SDRAM Minimum Cycle Time (tCKAVGmin)
            /// Byte 125 (0x07D): Fine Offset for SDRAM Minimum Cycle Time (tCKAVGmin)
            /// </summary>
            public Timing tCKAVGmin {
                get => TimingAdjustable(18, 125);
            }

            /// <summary>
            /// Byte 19 (0x013): SDRAM Maximum Cycle Time (tCKAVGmax)
            /// Byte 124 (0x07C): Fine Offset for SDRAM Maximum Cycle Time (tCKAVGmax)
            /// </summary>
            public Timing tCKAVGmax {
                get => TimingAdjustable(19, 124);
            }

            /// <summary>
            /// Byte 20 (0x014) - Byte 23 (0x017): CAS Latencies Supported (First-Fourth bytes)
            /// </summary>
            public CasLatenciesData tCL {
                get => new CasLatenciesData {
                    HighRange = Data.GetBit(RawData[23], 7),
                    Bitmask   = (uint)(RawData[20] | RawData[21] << 8 | RawData[22] << 16 | RawData[23] << 24),
                };
            }

            /// <summary>
            /// CAS Latencies Supported data
            /// </summary>
            public struct CasLatenciesData {
                public uint Bitmask;
                public bool HighRange;

                /// <summary>
                /// Returns a numeric array of latencies
                /// </summary>
                /// <returns>An array of supported latencies</returns>
                public int[] ToArray() {
                    Queue<int> latencies = new Queue<int>();
                    for (byte i = 0; i < 29; i++) {
                        if (Data.GetBit(Bitmask, i)) {
                            latencies.Enqueue((byte)(i + 7 + (HighRange ? 16 : 0)));
                        }
                    }

                    return latencies.ToArray();
                }

                public override string ToString() {

                    string latenciesString = "";
                    foreach (byte latency in this.ToArray()) {
                        latenciesString += $"{latency},";
                    }

                    return latenciesString.TrimEnd(',');
                }
            }

            /// <summary>
            /// Byte 24 (0x018): Minimum CAS Latency Time (tAAmin)
            /// Byte 123 (0x07B): Fine Offset for Minimum CAS Latency Time (tAAmin)
            /// </summary>
            public Timing tAAmin {
                get => TimingAdjustable(24, 123);
            }

            /// <summary>
            /// Byte 25 (0x019): Minimum RAS to CAS Delay Time (tRCDmin)
            /// Byte 122 (0x07A): Fine Offset for Minimum RAS to CAS Delay Time (tRCDmin)
            /// </summary>
            public Timing tRCDmin {
                get => TimingAdjustable(25, 122);
            }

            /// <summary>
            /// Byte 26 (0x01A): Minimum Row Precharge Delay Time (tRPmin)
            /// Byte 121 (0x079): Fine Offset for Minimum Row Precharge Delay Time (tRPmin)
            /// </summary>
            public Timing tRPmin {
                get => TimingAdjustable(26, 121);
            }

            /// <summary>
            /// Returns timing information based on medium and fine grain timing details
            /// </summary>
            /// <param name="mediumOffset">Medium grain timing byte location</param>
            /// <returns>Timing</returns>
            public static Timing TimingLongAdjustable(short mediumOffset) {
                return new Timing {
                    Medium = mediumOffset,
                };
            }

            /// <summary>
            /// Returns timing information based on medium and fine grain timing details
            /// </summary>
            /// <param name="mediumOffset">Medium grain timing byte location</param>
            /// <param name="fineOffset">Fine grain timing byte location</param>
            /// <returns>Timing</returns>
            public static Timing TimingLongAdjustable(short mediumOffset, sbyte fineOffset) {
                return new Timing {
                    Medium = mediumOffset,
                    Fine   = fineOffset,
                };
            }

            /// <summary>
            /// Byte 28 (0x01C): Minimum Active to Precharge Delay Time (tRASmin)
            /// </summary>
            public Timing tRASmin {
                get => new Timing {
                    Medium = (short)(
                        // Least Significant Byte
                        RawData[28] |
                        // Upper Nibble for tRASmin
                        Data.SubByte(RawData[27], 3, 4) << 8)
                };
            }

            /// <summary>
            /// Byte 29 (0x01D): Minimum Active to Active/Refresh Delay Time (tRCmin)
            /// Byte 120 (0x078): Fine Offset for Minimum Active to Active/Refresh Delay Time (tRCmin)
            /// </summary>
            public Timing tRCmin {
                get => new Timing {
                    Medium = (short)(
                        // Least Significant Byte
                        RawData[29] |
                        // Upper Nibble for tRCmin
                        Data.SubByte(RawData[27], 7, 4) << 8),
                    Fine = (sbyte)RawData[120]
                };
            }

            /// <summary>
            /// Byte 30 (0x01E): Minimum Refresh Recovery Delay Time (tRFC1min), LSB
            /// Byte 31 (0x01F): Minimum Refresh Recovery Delay Time(tRFC1min), MSB
            /// </summary>
            public Timing tRFC1min {
                get => TimingLongAdjustable((short)(RawData[30] | RawData[31] << 8));
            }

            /// <summary>
            /// Byte 32 (0x020): Minimum Refresh Recovery Delay Time (tRFC2min), LSB
            /// Byte 33 (0x021): Minimum Refresh Recovery Delay Time(tRFC2min), MSB
            /// </summary>
            public Timing tRFC2min {
                get => TimingLongAdjustable((short)(RawData[32] | RawData[33] << 8));
            }

            /// <summary>
            /// Byte 34 (0x022): Minimum Refresh Recovery Delay Time (tRFC4min), LSB
            /// Byte 35 (0x023): Minimum Refresh Recovery Delay Time(tRFC4min), MSB
            /// </summary>
            public Timing tRFC4min {
                get => TimingLongAdjustable((short)(RawData[34] | RawData[35] << 8));
            }

            /// <summary>
            /// Byte 36 (0x024): Upper Nibble for tFAW
            /// Byte 37 (0x025): Minimum Four Activate Window Delay Time (tFAWmin), Least Significant Byte
            /// </summary>
            public Timing tFAWmin {
                get => TimingLongAdjustable((short)(RawData[37] | (Data.SubByte(RawData[36], 3, 4) << 8)));
            }

            /// <summary>
            /// Byte 38 (0x026): Minimum Activate to Activate Delay Time (tRRD_Smin), different bank group
            /// Byte 119 (0x077): Fine Offset for Minimum Activate to Activate Delay Time (tRRD_Smin), different bank
            /// </summary>
            public Timing tRRD_Smin {
                get => TimingAdjustable(38, 119);
            }

            /// <summary>
            /// Byte 39 (0x027): Minimum Activate to Activate Delay Time (tRRD_Lmin), same bank group
            /// Byte 118 (0x076): Fine Offset for Minimum Activate to Activate Delay Time (tRRD_Lmin), same bank group
            /// </summary>
            public Timing tRRD_Lmin {
                get => TimingAdjustable(39, 118);
            }

            /// <summary>
            /// Byte 40 (0x028): Minimum CAS to CAS Delay Time (tCCD_Lmin), same bank group
            /// Byte 117 (0x075): Fine Offset for Minimum CAS to CAS Delay Time (tCCD_Lmin), same bank group
            /// </summary>
            public Timing tCCD_Lmin {
                get => TimingAdjustable(40, 117);
            }

            /// <summary>
            /// Byte 41 (0x029): Upper Nibble for tWRmin
            /// Byte 42 (0x02A): Minimum Write Recovery Time (tWRmin)
            /// </summary>
            public Timing tWRmin {
                get => new Timing {
                    Medium = (short)(RawData[42] | Data.SubByte(RawData[41], 3, 4) << 8),
                };
            }

            /// <summary>
            /// Byte 44 (0x02C): Minimum Write to Read Time (tWTR_Smin), different bank group
            /// </summary>
            public Timing tWTR_Smin {
                get => new Timing {
                    Medium = (short)(RawData[44] | Data.SubByte(RawData[43], 3, 4) << 8),
                };
            }

            /// <summary>
            /// Byte 45 (0x02D): Minimum Write to Read Time (tWTR_Lmin), same bank group
            /// </summary>
            public Timing tWTR_Lmin {
                get => new Timing {
                    Medium = (short)(RawData[45] | Data.SubByte(RawData[43], 7, 4) << 8),
                };
            }

            /// <summary>
            /// Byte 126 (0x07E) & Byte 127 (0x07F): Cyclical Redundancy Codes (CRC) for Base Configuration Section
            /// </summary>
            public Crc16Data[] Crc {
                get {
                    byte sectionCount  = 2;
                    byte sectionLength = 128;

                    Crc16Data[] crc = new Crc16Data[sectionCount];

                    for (byte i = 0; i < sectionCount; i++) {
                        crc[i].Contents = new byte[sectionLength];

                        Array.Copy(
                            sourceArray      : RawData,
                            sourceIndex      : sectionLength * i,
                            destinationArray : crc[i].Contents,
                            destinationIndex : 0,
                            length           : sectionLength);
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

                byte sectionCount  = 2;

                for (byte i = 0; i < sectionCount; i++) {
                    Array.Copy(
                        sourceArray      : Crc[i].Fix(),
                        sourceIndex      : 0,
                        destinationArray : RawData,
                        destinationIndex : Crc[i].Contents.Length * i,
                        length           : Crc[i].Contents.Length);
                }

                return CrcStatus;
            }

            // Module-Specific Section: Bytes 128~191 (0x080~0x0BF)

            /// <summary>
            /// Byte 128 (0x080): Raw Card Extension
            /// </summary>
            public byte RawCardExtension {
                get {
                    byte value = Data.SubByte(RawData[128], 7, 3);

                    if (value > 0) {
                        return (byte)(value + 3);
                    }

                    return ReferenceRawCard.Revision;
                }
            }

            /// <summary>
            /// Byte 128 (0x080): Module Nominal Height
            /// </summary>
            public ModuleHeightData ModuleHeight {
                get => new ModuleHeightData {
                    Minimum = (byte)(Data.SubByte(RawData[128], 4, 5) + 15),
                    Maximum = (byte)(Data.SubByte(RawData[128], 4, 5) + 16),
                    Unit    = HeightUnit.mm
                };
            }

            /// <summary>
            /// Byte 129 (0x081): Module Maximum Thickness
            /// </summary>
            public ModuleMaximumThicknessSideData ModuleMaximumThickness {
                get => new ModuleMaximumThicknessSideData {
                    Back = new ModuleHeightData {
                        Minimum = Data.SubByte(RawData[129], 7, 4),
                        Maximum = (byte)(Data.SubByte(RawData[129], 7, 4) + 1),
                        Unit    = HeightUnit.mm
                    },
                    Front = new ModuleHeightData {
                        Minimum = Data.SubByte(RawData[129], 3, 4),
                        Maximum = (byte)(Data.SubByte(RawData[129], 3, 4) + 1),
                        Unit    = HeightUnit.mm
                    }
                };
            }

            /// <summary>
            /// Byte 130 (0x082) (Unbuffered): Reference Raw Card Used
            /// </summary>
            public ReferenceRawCardData ReferenceRawCard {
                get {
                    bool extension = Data.GetBit(RawData[130], 7);

                    ReferenceRawCardData cardData = new ReferenceRawCardData {
                        Extension = extension,
                        Revision  = Data.SubByte(RawData[130], 6, 2),
                    };

                    byte cardValue = Data.SubByte(RawData[130], 4, 5);

                    cardData.Name = cardValue == 0b11111
                        ? ReferenceRawCardName.ZZ
                        : (ReferenceRawCardName)cardValue +
                          (extension ? 1 << 5 : 0);

                    return cardData;
                }
            }

            /// <summary>
            /// Byte 136 (0x088) (Registered, Load Reduced): Address Mapping from Register to DRAM
            /// </summary>
            public AddressMappingType AddressMapping {
                get => ModuleType.BaseModuleType == BaseModuleType.RDIMM ||
                       ModuleType.BaseModuleType == BaseModuleType.LRDIMM
                    ? (AddressMappingType)Data.BoolToNum<byte>(Data.GetBit(RawData[136], 0))
                    : AddressMappingType.None;
            }

            /// <summary>
            /// Describes the connection of register output pins for address bits to the corresponding input pins of the DDR4 SDRAMs for rank 1 and rank 3 only
            /// </summary>
            public enum AddressMappingType {
                Standard,
                Mirrored,
                None
            }

            /// <summary>
            /// Indicates the manufacturer of the module
            /// </summary>
            public ManufacturerIdCodeData ManufacturerIdCode {
                get => new ManufacturerIdCodeData {
                    ContinuationCode = RawData[320],
                    ManufacturerCode = RawData[321]
                };
            }

            /// <summary>
            /// An identifier that uniquely defines the manufacturing location of the memory module.
            /// </summary>
            public byte ManufacturingLocation {
                get => RawData[322];
            }

            /// <summary>
            /// Bytes 323~324 (0x143~0x144): Module Manufacturing Date
            /// </summary>
            public DateCodeData ModuleManufacturingDate {
                get => new DateCodeData {
                    Year = RawData[323],
                    Week = RawData[324]
                };
            }

            /// <summary>
            /// Bytes 325~328 (0x145~0x148): Module Serial Number
            /// </summary>
            public SerialNumberData SerialNumber {

                get {
                    byte[] serialNumberBytes = new byte[4];

                    Array.Copy(
                        sourceArray      : RawData,
                        sourceIndex      : 325,
                        destinationArray : serialNumberBytes,
                        destinationIndex : 0,
                        length           : serialNumberBytes.Length);

                    return new SerialNumberData {
                        SerialNumber = serialNumberBytes
                    };
                }
            }

            /// <summary>
            /// Bytes 329~348 (0x149~15C): Module Part Number
            /// </summary>
            public string PartNumber {
                get {
                    int modelNameStart = 0x149;

                    char[] chars = new char[0x15C - modelNameStart + 1];

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
            /// Byte 350 (0x15E): DRAM Manufacturer ID Code, LSB; Byte 351 (0x15F): DRAM Manufacturer ID Code, MSB
            /// </summary>
            public ManufacturerIdCodeData DramManufacturerIdCode {
                get => new ManufacturerIdCodeData {
                    ContinuationCode = RawData[350],
                    ManufacturerCode = RawData[351]
                };
            }

            // XMP

            /// <summary>
            /// XMP header (magic bytes)
            /// </summary>
            public bool XmpPresence {
                get => Data.MatchArray(RawData, ProfileId.XMP, 384);
            }

            /// <summary>
            /// XMP profile type
            /// </summary>
            public enum XmpProfileName {
                Enthusiast,
                Extreme
            }

            /// <summary>
            /// XMP profiles
            /// </summary>
            public Xmp20ProfileData[] XmpProfile {
                get {
                    Xmp20ProfileData[] xmpProfile = new Xmp20ProfileData[2];

                    for (byte i = 0; i < xmpProfile.Length; i++) {
                        xmpProfile[i].Number = i;
                    }

                    return xmpProfile;
                }
            }

            /// <summary>
            /// DDR4 XMP 2.0 data
            /// </summary>
            public struct Xmp20ProfileData {

                private ushort _offset => (ushort)(Number * 63);

                /// <summary>
                /// XMP profile number
                /// </summary>
                public byte Number {
                    get => _number;
                    set {
                        if (value > 1) {
                            throw new ArgumentOutOfRangeException();
                        }
                        _number = value;
                    }
                }
                private byte _number;

                public bool Enabled {
                    get => Data.GetBit(RawData[386], Number);
                }

                /// <summary>
                /// XMP profile name
                /// </summary>
                public XmpProfileName Name => (XmpProfileName)Number;

                /// <summary>
                /// XMP version in Binary Coded Decimal format
                /// </summary>
                public byte Version => RawData[387];

                // tCKAVGmin
                public Timing tCKAVGmin {
                    get => TimingAdjustable((short)(0x18C + _offset), (ushort)(0x1AF + _offset));
                }

                // tAA
                public Timing tAAmin {
                    get => TimingAdjustable((short)(0x191 + _offset), (ushort)(0x1AE + _offset));
                }

                // CAS latencies supported (7-30)
                public CasLatenciesData CasLatencies {
                    get => new CasLatenciesData {
                        Bitmask = (ushort)(RawData[0x18F + _offset] | RawData[0x18E + _offset] << 8 | RawData[0x18D + _offset] << 16),
                    };
                }

                // tRCD
                public Timing tRCDmin {
                    get => TimingAdjustable((short)(0x192 + _offset), (ushort)(0x1AD + _offset));
                }

                // tRP
                public Timing tRPmin {
                    get => TimingAdjustable((short)(0x193 + _offset), (ushort)(0x1AC + _offset));
                }

                // tRAS
                public Timing tRASmin {
                    get => TimingLongAdjustable((short)(RawData[0x195 + _offset] | Data.SubByte(RawData[0x194 + _offset], 7, 4) << 8));
                }

                // tRC
                public Timing tRCmin {
                    get => TimingLongAdjustable(
                        mediumOffset : (short)(RawData[0x196 + _offset] | Data.SubByte(RawData[0x194 + _offset], 3, 4) << 8),
                        fineOffset   : (sbyte)RawData[0x1AB + _offset]);
                }

                // tFAW
                public Timing tFAWmin {
                    get => TimingLongAdjustable((short)(RawData[0x19E + _offset] | Data.SubByte(RawData[0x19D + _offset], 3, 4) << 8));
                }

                // tRRDS
                public Timing tRRD_Smin {
                    get => TimingAdjustable((short)(0x19F + _offset), (ushort)(0x1AA + _offset));
                }

                // tRRDL
                public Timing tRRD_Lmin {
                    get => TimingAdjustable((short)(0x1A0 + _offset), (ushort)(0x1A9 + _offset));
                }

                // tRFC1
                public Timing tRFC1min {
                    get => TimingLongAdjustable((short)(RawData[0x197 + _offset] | RawData[0x198 + _offset] << 8));
                }

                // tRFC2
                public Timing tRFC2min {
                    get => TimingLongAdjustable((short)(RawData[0x199 + _offset] | RawData[0x19A + _offset] << 8));
                }

                // tRFC4
                public Timing tRFC4min {
                    get => TimingLongAdjustable((short)(RawData[0x19B + _offset] | RawData[0x19C + _offset] << 8));
                }

                // Voltage
                public XmpVoltageData Voltage {
                    get => new XmpVoltageData { Value = RawData[0x189 + _offset] };
                }

                /// <summary>
                /// Number of DIMMs per channel
                /// </summary>
                public byte ChannelConfig {
                    get => (byte)(Data.SubByte(RawData[0x182 + _offset], 3, 2) + 1);
                }

                public override string ToString() {
                    return !Enabled ? "" :
                        $"{1000 / tCKAVGmin.ToNanoSeconds()} MHz " +
                        $"{tAAmin.ToClockCycles(tCKAVGmin)}-" +
                        $"{tRCDmin.ToClockCycles(tCKAVGmin)}-" +
                        $"{tRPmin.ToClockCycles(tCKAVGmin)}-" +
                        $"{tRASmin.ToClockCycles(tCKAVGmin)} " +
                        $"{Voltage}V";
                }
            }

            /// <summary>
            /// XMP profile voltage data
            /// </summary>
            public struct XmpVoltageData {
                public byte Value;

                public override string ToString() {
                    return (Data.BoolToNum<byte>(Data.GetBit(Value, 7)) + Data.SubByte(Value, 6, 7) / 100F).ToString(CultureInfo.InvariantCulture);
                }
            }
        }
    }
}