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

namespace SpdReaderWriterCore {
    public partial class Spd {

        /// <summary>
        /// DDR3 SPD
        /// </summary>
        public struct DDR3 : ISpd {

            /// <summary>
            /// New instance of DDR3 SPD class
            /// </summary>
            /// <param name="input">Raw SPD data</param>
            public DDR3(byte[] input) {
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
            public int Length => DataLength.Minimum;

            public override string ToString() => $"{GetManufacturerName(ManufacturerIdCode.ManufacturerId)} {PartNumber}".Trim();

            /// <summary>
            /// Byte 0: Number of Bytes Used / Number of Bytes in SPD Device / CRC Coverage
            /// </summary>
            public BytesData Bytes {
                get {
                    ushort usedBytes;

                    switch (Data.SubByte(RawData[0], 3, 4)) {
                        case 1:
                            usedBytes = 128;
                            break;
                        case 2:
                            usedBytes = 176;
                            break;
                        case 3:
                            usedBytes = 256;
                            break;
                        default:
                            usedBytes = 0;
                            break;
                    }
                    return new BytesData {
                        Used = usedBytes,
                        Total = (ushort)(Data.SubByte(RawData[0], 6, 3) == 1 ? 256 : 0),
                    };
                }
            }

            public int SpdBytesUsed => Bytes.Used;

            /// <summary>
            /// Describes whether the unique module identifier is covered by the CRC
            /// </summary>
            public bool CrcCoverage {
                get => Data.GetBit(RawData[0], 7);
            }

            /// <summary>
            /// Byte 1: SPD Revision
            /// </summary>
            public SpdRevisionData SpdRevision {
                get => new SpdRevisionData {
                    EncodingLevel  = Data.SubByte(RawData[1], 7, 4),
                    AdditionsLevel = Data.SubByte(RawData[1], 3, 4)
                };
            }

            /// <summary>
            /// Byte 2: Key Byte / DRAM Device Type
            /// </summary>
            public RamType DramDeviceType {
                get => (RamType)RawData[2];
            }

            /// <summary>
            /// Byte 3 (0x003): Key Byte / Module Type
            /// </summary>
            public ModuleTypeData ModuleType {
                get => new ModuleTypeData {
                    HybridMedia    = Data.GetBit(RawData[3], 4),
                    BaseModuleType = (BaseModuleType)Data.SubByte(RawData[3], 3, 4)
                };
            }

            /// <summary>
            /// Module Type data
            /// </summary>
            public struct ModuleTypeData {
                public bool HybridMedia;
                public BaseModuleType BaseModuleType;

                public override string ToString() => $"{BaseModuleType.ToString().TrimStart('_').Replace("_", "-").Trim()}";
            }

            /// <summary>
            /// Base Module Type
            /// </summary>
            public enum BaseModuleType {
                
                Undefined     = 0b0000,

                [Description("Registered Dual In-Line Memory Module")]
                RDIMM         = 0b0001,

                [Description("Unbuffered Dual In-Line Memory Module")] 
                UDIMM         = 0b0010,

                [Description("Unbuffered 64-bit Small Outline Dual In-Line Memory Module")] 
                SO_DIMM       = 0b0011,

                [Description("Micro Dual In-Line Memory Module")] 
                Micro_DIMM    = 0b0100,

                [Description("Mini Registered Dual In-Line Memory Module")] 
                Mini_RDIMM    = 0b0101,

                [Description("Mini Unbuffered Dual In-Line Memory Module")] 
                Mini_UDIMM    = 0b0110,

                [Description("Clocked 72-bit Mini Dual In-Line Memory Module")] 
                Mini_CDIMM    = 0b0111,

                [Description("Unbuffered 72-bit Small Outline Dual In-Line Memory Module")] 
                _72b_SO_UDIMM = 0b1000,

                [Description("Registered 72-bit Small Outline Dual In-Line Memory Module")] 
                _72b_SO_RDIMM = 0b1001,

                [Description("Clocked 72-bit Small Outline Dual In-Line Memory Module")] 
                _72b_SO_CDIMM = 0b1010,

                [Description("Load Reduced Dual In-Line Memory Module")] 
                LRDIMM        = 0b1011,

                [Description("Unbuffered 16-bit Small Outline Dual In-Line Memory Module")] 
                _16b_SO_DIMM  = 0b1100,

                [Description("Unbuffered 32-bit Small Outline Dual In-Line Memory Module")] 
                _32b_SO_DIMM  = 0b1101,
            }

            /// <summary>
            /// Byte 4: SDRAM Density and Banks
            /// </summary>
            public DensityBanksData DensityBanks {
                get => new DensityBanksData {
                    BankAddress         = (byte)Math.Pow(2, (Data.SubByte(RawData[4], 6, 3) + 3)),
                    TotalCapacityPerDie = (ushort)(1 << (Data.SubByte(RawData[4], 3, 4)) + 8),
                };
            }

            /// <summary>
            /// SDRAM Density and Banks data
            /// </summary>
            public struct DensityBanksData {
                /// <summary>
                /// Number of banks
                /// </summary>
                public Byte BankAddress;
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
            /// Byte 6: Module Nominal Voltage, VDD
            /// </summary>
            public ModuleNominalVoltageData[] ModuleNominalVoltage {
                get {
                    float[] voltages = { 1.5F, 1.35F, 1.25F };
                    ModuleNominalVoltageData[] operability = new ModuleNominalVoltageData[voltages.Length];

                    for (int i = 0; i < operability.Length; i++) {
                        operability[i].Voltage = voltages[i];
                        operability[i].Operable = (i == 0) ? !Data.GetBit(RawData[6], (byte)i) : Data.GetBit(RawData[6], (byte)i);
                    }

                    return operability;
                }
            }

            /// <summary>
            /// Describes the Voltage Level for DRAM and other components on the module such as the register if applicable
            /// </summary>
            public struct ModuleNominalVoltageData {
                public float Voltage;
                public bool Operable;
            }

            /// <summary>
            /// Byte 7: Module Organization
            /// </summary>
            public ModuleOrganizationData ModuleOrganization {
                get {
                    byte rankData = Data.SubByte(RawData[7], 5, 3);
                    return new ModuleOrganizationData {
                        PackageRankCount = (byte)(rankData == 4 ? 8 : rankData + 1),
                        DeviceWidth      = (byte)(4 << Data.SubByte(RawData[7], 2, 3))
                    };
                }
            }

            /// <summary>
            /// Describes the organization of the SDRAM module.
            /// </summary>
            public struct ModuleOrganizationData {
                public byte PackageRankCount;
                public byte DeviceWidth;
            }

            /// <summary>
            /// Byte 8: Module Memory Bus Width
            /// </summary>
            public BusWidthData BusWidth {
                get => new BusWidthData {
                    Extension       = Data.GetBit(RawData[8], 7),
                    PrimaryBusWidth = (byte)(1 << Data.SubByte(RawData[8], 2, 3) + 3),
                };
            }

            /// <summary>
            /// Calculated die density in bits
            /// </summary>
            public ulong DieDensity {
                get => (ulong)(
                    (1L << Addressing.Rows) *
                    (1L << Addressing.Columns) *
                    DensityBanks.BankAddress *
                    ModuleOrganization.DeviceWidth);
            }

            /// <summary>
            /// Total memory capacity of the module calculated from SPD values in bytes
            /// </summary>
            public ulong TotalModuleCapacity {
                get => (ulong)(
                    (1L << Addressing.Rows) *
                    (1L << Addressing.Columns) *
                    DensityBanks.BankAddress *
                    ModuleOrganization.PackageRankCount *
                    (BusWidth.PrimaryBusWidth & 0xF0) / 8);
            }

            /// <summary>
            /// Total memory capacity based on programmed density
            /// </summary>
            public ulong TotalModuleCapacityProgrammed {
                get =>
                    (ulong)(DensityBanks.TotalCapacityPerDie / 8 * 
                             BusWidth.PrimaryBusWidth / ModuleOrganization.DeviceWidth * 
                             ModuleOrganization.PackageRankCount) * 1024L * 1024L;
            }

            /// <summary>
            /// Defines a value in picoseconds that represents the fundamental timebase for fine grain timing calculations.
            /// This value is used as a multiplier for formulating subsequent timing parameters.
            /// </summary>
            public struct TimebaseData {
                public float Dividend; // X/y
                public float Divisor;  // x/Y

                /// <summary>
                /// Converts timing value to nanoseconds
                /// </summary>
                /// <returns>Delay in nanoseconds</returns>
                public float ToNanoSeconds() => Dividend / Divisor;

                public override string ToString() {
                    return $"{ToNanoSeconds():F4}";
                }
            }

            /// <summary>
            /// Byte 9: Fine Timebase (FTB)
            /// </summary>
            public static TimebaseData FTB {
                get => new TimebaseData {
                    Dividend = Data.SubByte(RawData[9], 7, 4),
                    Divisor  = Data.SubByte(RawData[9], 3, 4)
                };
            }

            /// <summary>
            /// Byte 10: Medium Timebase (MTB) Dividend
            /// Byte 11: Medium Timebase (MTB) Divisor
            /// </summary>
            public static TimebaseData MTB {
                get => new TimebaseData {
                    Dividend = RawData[10],
                    Divisor  = RawData[11]
                };
            }

            /// <summary>
            /// Timing Data
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
                    (int)Math.Ceiling((t1.Medium * MTB.Dividend / MTB.Divisor + t1.Fine * FTB.Dividend / FTB.Divisor) / 1000F /
                                      ((t2.Medium * MTB.Dividend / MTB.Divisor + t2.Fine * FTB.Dividend / FTB.Divisor) / 1000F));

                /// <summary>
                /// Allows integer to be divided by timing value to get frequency
                /// </summary>
                /// <param name="d1">Dividend</param>
                /// <param name="t1">Timing</param>
                /// <returns>Frequency in MHz</returns>
                public static int operator /(int d1, Timing t1) =>
                    (int)(d1 / ((t1.Medium * MTB.Dividend / MTB.Divisor + t1.Fine * FTB.Dividend / FTB.Divisor) / 1000F));

                /// <summary>
                /// Converts timing value to clock cycles
                /// </summary>
                /// <param name="refTiming">Reference timing</param>
                /// <returns>Number of clock cycles</returns>
                public int ToClockCycles(Timing refTiming) =>
                    (int)Math.Ceiling(ToNanoSeconds() / refTiming.ToNanoSeconds());

                /// <summary>
                /// Converts timing value to nanoseconds
                /// </summary>
                /// <returns>Delay in nanoseconds</returns>
                public float ToNanoSeconds() => Medium * (MTB.Dividend / MTB.Divisor) +
                                                Fine * (FTB.Dividend / FTB.Divisor) / 1000F;

                public override string ToString() => $"{this.ToNanoSeconds():F3}";
            }

            /// <summary>
            /// Byte 12: SDRAM Minimum Cycle Time (tCKmin)
            /// </summary>
            public Timing tCKmin {
                get => new Timing {
                    Medium = RawData[12],
                    Fine   = (sbyte)RawData[34]
                };
            }

            /// <summary>
            /// Byte 14: CAS Latencies Supported, Least Significant Byte, Byte 15: CAS Latencies Supported, Most Significant Byte
            /// </summary>
            public CasLatenciesData tCL {
                get => new CasLatenciesData {
                    Bitmask = (ushort)(RawData[14] | RawData[15] << 8),
                };
            }

            /// <summary>
            /// Define which CAS Latency (CL) values are supported
            /// </summary>
            public struct CasLatenciesData {
                public ushort Bitmask;

                public int[] ToArray() {
                    Queue<int> latencies = new Queue<int>();
                    for (byte i = 0; i < 16; i++) {
                        if (Data.GetBit(Bitmask, i)) {
                            latencies.Enqueue(i + 4);
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
            /// Byte 16: Minimum CAS Latency Time (tAAmin)
            /// </summary>
            [Category("Timings")]
            [DisplayName("Minimum CAS Latency Time (tAAmin)")]
            [Description("This byte defines the minimum CAS Latency in medium timebase (MTB) units.")]
            public Timing tAAmin {
                get => new Timing {
                    Medium = RawData[16],
                    Fine   = (sbyte)RawData[35]
                };
            }

            /// <summary>
            /// Byte 17: Minimum Write Recovery Time (tWRmin)
            /// </summary>
            public Timing tWRmin {
                get => new Timing {
                    Medium = RawData[17],
                };
            }

            /// <summary>
            /// Byte 18: Minimum RAS# to CAS# Delay Time (tRCDmin)
            /// </summary>
            public Timing tRCDmin {
                get => new Timing {
                    Medium = RawData[18],
                    Fine   = (sbyte)RawData[36]
                };
            }

            /// <summary>
            /// Byte 19: Minimum Row Active to Row Active Delay Time (tRRDmin)
            /// </summary>
            public Timing tRRDmin {
                get => new Timing {
                    Medium = RawData[19],
                };
            }

            /// <summary>
            /// Byte 20: Minimum Row Precharge Delay Time (tRPmin)
            /// </summary>
            public Timing tRPmin {
                get => new Timing {
                    Medium = RawData[20],
                    Fine   = (sbyte)RawData[37]
                };
            }

            /// <summary>
            /// Byte 22: Minimum Active to Precharge Delay Time (tRASmin)
            /// </summary>
            public Timing tRASmin {
                get => new Timing {
                    Medium = (short)(RawData[22] | Data.SubByte(RawData[21], 3, 4) << 8),
                };
            }

            /// <summary>
            /// Byte 23: Minimum Active to Active/Refresh Delay Time (tRCmin)
            /// </summary>
            public Timing tRCmin {
                get => new Timing {
                    Medium = (short)(RawData[23] | Data.SubByte(RawData[21], 7, 4) << 8),
                };
            }

            /// <summary>
            /// Byte 24: Minimum Refresh Recovery Delay Time (tRFCmin), Least Significant Byte
            /// Byte 25: Minimum Refresh Recovery Delay Time(tRFCmin), Most Significant Byte
            /// </summary>
            public Timing tRFCmin {
                get => new Timing {
                    Medium = (short)(RawData[24] | RawData[25] << 8),
                };
            }

            /// <summary>
            /// Byte 27: Minimum Internal Read to Precharge Command Delay Time (tRTPmin)
            /// </summary>
            public Timing tRTPmin {
                get => new Timing {
                    Medium = RawData[27],
                };
            }

            /// <summary>
            /// Byte 29: Minimum Four Activate Window Delay Time (tFAWmin)
            /// </summary>
            public Timing tFAWmin {
                get => new Timing {
                    Medium = (short)(RawData[29] | Data.SubByte(RawData[28], 3, 4) << 8),
                };
            }

            /// <summary>
            /// Byte 33: SDRAM Device Type
            /// </summary>
            public PrimaryPackageTypeData SDRAMDeviceType {
                get => new PrimaryPackageTypeData {
                    Monolithic    = !Data.GetBit(RawData[33], 7),
                    DieCount      = (byte)((Data.SubByte(RawData[33], 6, 3) * 1) << (Data.SubByte(RawData[33], 6, 3) - 1)),
                    SignalLoading = (SignalLoadingData)(Data.SubByte(RawData[33], 1, 2)),
                };
            }

            /// <summary>
            /// Byte 41: SDRAM Maximum Active Count (MAC) Value
            /// </summary>
            public MaximumActivateFeaturesData SDRAMMaximumActiveCount {
                get => new MaximumActivateFeaturesData {
                    MaximumActivateWindow = (ushort)(8192 >> Data.SubByte(RawData[41], 5, 2)),
                    MaximumActivateCount  = (MaximumActivateCount)Data.SubByte(RawData[41], 3, 4),
                };
            }

            /// <summary>
            /// Byte 117: Module Manufacturer ID Code, Least Significant Byte
            /// Byte 118: Module Manufacturer ID Code, Most Significant Byte
            /// </summary>
            public ManufacturerIdCodeData ManufacturerIdCode {
                get => new ManufacturerIdCodeData {
                    ContinuationCode = RawData[117],
                    ManufacturerCode = RawData[118]
                };
            }

            /// <summary>
            /// Byte 119: Module Manufacturing Location
            /// </summary>
            public byte ModuleManufacturingLocation {
                get => RawData[119];
            }

            /// <summary>
            /// Bytes 120 ~ 121: Module Manufacturing Date
            /// </summary>
            public DateCodeData ModuleManufacturingDate {
                get => new DateCodeData {
                    Year = RawData[120],
                    Week = RawData[121]
                };
            }

            /// <summary>
            /// Bytes 122 ~ 125: Module Serial Number
            /// </summary>
            public SerialNumberData ModuleSerialNumber {
                get {
                    byte[] serialNumberBytes = new byte[4];

                    Array.Copy(
                        sourceArray      : RawData,
                        sourceIndex      : 122,
                        destinationArray : serialNumberBytes,
                        destinationIndex : 0,
                        length           : serialNumberBytes.Length);

                    return new SerialNumberData {
                        SerialNumber = serialNumberBytes,
                    };
                }
            }

            /// <summary>
            /// Bytes 126 ~ 127: SPD Cyclical Redundancy Code (CRC)
            /// </summary>
            public Crc16Data Crc {
                get {
                    Crc16Data crc = new Crc16Data {
                        Contents = new byte[(CrcCoverage ? 117 : 126) + 2] // Add 2 bytes for CRC data
                    };

                    Array.Copy(
                        sourceArray      : RawData,
                        destinationArray : crc.Contents,
                        length           : crc.Contents.Length);

                    // Get CRC from SPD
                    Array.Copy(
                        sourceArray      : RawData,
                        sourceIndex      : 126,
                        destinationArray : crc.Contents,
                        destinationIndex : crc.Contents.Length - 2,
                        length           : 2);

                    return crc;
                }
            }

            /// <summary>
            /// CRC validation status
            /// </summary>
            public bool CrcStatus => Crc.Validate();

            /// <summary>
            /// Fixes CRC checksum
            /// </summary>
            /// <returns><see langword="true"/> if checksum has been fixed</returns>
            public bool FixCrc() {

                ushort validCrc = Data.Crc16(Data.TrimArray(RawData, CrcCoverage ? 117 : 126, Data.TrimPosition.End), 0x1021);

                // Replace CRC only
                RawData[126] = (byte)validCrc;
                RawData[127] = (byte)(validCrc >> 8);

                return CrcStatus;
            }

            /// <summary>
            /// Bytes 128 ~ 145: Module Part Number
            /// </summary>
            public string PartNumber {
                get {
                    int modelNameStart = 128;

                    char[] chars = new char[145 - modelNameStart + 1];

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
            /// Byte 148: DRAM Manufacturer ID Code, Least Significant Byte
            /// Byte 149: DRAM Manufacturer ID Code, Most Significant Byte
            /// </summary>
            public ManufacturerIdCodeData DramManufacturerIdCode {
                get => new ManufacturerIdCodeData {
                    ContinuationCode = RawData[148],
                    ManufacturerCode = RawData[149]
                };
            }

            // Raw Card Extension (bytes 60 ~ 116)

            /// <summary>
            /// Byte 60 : Raw Card Extension
            /// </summary>
            public ReferenceRawCardData RawCardExtension {
                get {
                    int rev = Data.SubByte(RawData[60], 7, 3);
                    byte cardValue = Data.SubByte(RawData[62], 4, 5);

                    return new ReferenceRawCardData {
                        Extension = Data.GetBit(RawData[62], 7),
                        Revision  = (byte)(rev == 0 ? Data.SubByte(RawData[62], 6, 2) : rev),
                        Name      = cardValue == 0b11111
                            ? ReferenceRawCardName.ZZ
                            : (ReferenceRawCardName)cardValue + (Data.BoolToNum<byte>(Data.GetBit(RawData[62], 7)) << 5)
                    };
                }
            }

            /// <summary>
            /// Byte 60: Module Nominal Height
            /// </summary>
            public ModuleHeightData ModuleHeight {
                get => new ModuleHeightData {
                    Minimum = (byte)(Data.SubByte(RawData[60], 4, 5) + 14),
                    Maximum = (byte)(Data.SubByte(RawData[60], 4, 5) + 15),
                    Unit    = HeightUnit.mm
                };
            }

            /// <summary>
            /// Byte 61 (Unbuffered): Module Maximum Thickness
            /// </summary>
            public ModuleMaximumThicknessSideData ModuleMaximumThickness {
                get => new ModuleMaximumThicknessSideData {
                    Back = new ModuleHeightData {
                        Minimum = Data.SubByte(RawData[61], 7, 4),
                        Maximum = (byte)(Data.SubByte(RawData[61], 7, 4) + 1),
                        Unit    = HeightUnit.mm
                    },
                    Front = new ModuleHeightData {
                        Minimum = Data.SubByte(RawData[61], 3, 4),
                        Maximum = (byte)(Data.SubByte(RawData[61], 3, 4) + 1),
                        Unit    = HeightUnit.mm
                    }
                };
            }

            // XMP 1.1

            /// <summary>
            /// XMP header (magic bytes)
            /// </summary>
            public bool XmpPresence {
                get => Data.MatchArray(RawData, ProfileId.XMP, 176);
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
            public Xmp10ProfileData[] XmpProfile {
                get {
                    Xmp10ProfileData[] xmpProfile = new Xmp10ProfileData[2];

                    for (byte i = 0; i < xmpProfile.Length; i++) {
                        xmpProfile[i].Number = i;
                    }

                    return xmpProfile;
                }
            }

            /// <summary>
            /// DDR3 XMP 1.x data
            /// TODO: ignore fine timing adjustment, if XMP version is < 0x13
            /// </summary>
            public struct Xmp10ProfileData {

                private byte _offset => (byte)(Number * 35);

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

                /// <summary>
                /// Profile number
                /// </summary>
                private byte _number;

                /// <summary>
                /// Byte 178: Intel Extreme Memory Profile
                /// </summary>
                public bool Enabled {
                    get => Data.GetBit(RawData[178], Number);
                }

                /// <summary>
                /// XMP profile name
                /// </summary>
                public XmpProfileName Name => (XmpProfileName)Number;

                /// <summary>
                /// Byte 178: Number of DIMMs per channel Organization
                /// </summary>
                public byte ChannelConfig {
                    get => Data.SubByte(RawData[178], (byte)((Number * 2) + 1), 2);
                }

                /// <summary>
                /// Byte 179: Intel Extreme Memory Profile Revision
                /// </summary>
                public byte Version => RawData[179];

                /// <summary>
                /// Byte 180: Medium Timebase (MTB) Dividend for Profile 1
                /// Byte 181: Medium Timebase (MTB) Divisor for Profile 1
                /// Byte 182: Medium Timebase (MTB) Dividend for Profile 2
                /// Byte 183: Medium Timebase (MTB) Divisor for Profile 2
                /// </summary>
                public TimebaseData MediumTimebase {
                    get => new TimebaseData {
                        Dividend = RawData[180 + Number],
                        Divisor  = RawData[181 + Number]
                    };
                }

                /// <summary>
                /// Byte 185 or 220: Module VDD Voltage Level
                /// </summary>
                public VoltageLevelData VDDVoltage {
                    get => new VoltageLevelData {
                        Integer  = Data.SubByte(RawData[185 + _offset], 6, 2),
                        Fraction = Data.SubByte(RawData[185 + _offset], 4, 5)
                    };
                }

                /// <summary>
                /// Describes the Modules Voltage Level
                /// </summary>
                public struct VoltageLevelData {
                    public byte Integer;
                    public byte Fraction;

                    public override string ToString() => $"{Integer + Fraction * 0.05F:F2}";
                }

                /// <summary>
                /// Byte 186 or 221: Minimum SDRAM Cycle Time (tCKmin)
                /// </summary>
                public Timing tCKmin {
                    get => new Timing {
                        Medium = RawData[186 + _offset],
                        Fine   = (sbyte)RawData[211 + _offset]
                    };
                }

                /// <summary>
                /// Byte 187 or 222: Minimum CAS Latency Time (tAAmin)
                /// </summary>
                public Timing tAAmin {
                    get => new Timing {
                        Medium = RawData[187 + _offset],
                        Fine   = (sbyte)RawData[212 + _offset]
                    };
                }

                /// <summary>
                /// Byte 188 or 223: CAS Latencies Supported, Low Byte
                /// Byte 189 or 224: CAS Latencies Supported, High Byte
                /// </summary>
                public CasLatenciesData CasLatencies {
                    get => new CasLatenciesData {
                        Bitmask = (ushort)(RawData[188 + _offset] | RawData[189 + _offset] << 8)
                    };
                }

                /// <summary>
                /// Byte 191 or 226: Minimum Row Precharge Delay Time (tRPmin)
                /// </summary>
                public Timing tRPmin {
                    get => new Timing {
                        Medium = RawData[191 + _offset],
                        Fine   = (sbyte)RawData[213 + _offset]
                    };
                }

                /// <summary>
                /// Byte 192 or 227: Minimum RAS# to CAS# Delay Time (tRCDmin)
                /// </summary>
                public Timing tRCDmin {
                    get => new Timing {
                        Medium = RawData[192 + _offset],
                        Fine   = (sbyte)RawData[214 + _offset]
                    };
                }

                /// <summary>
                /// Byte 193 or 228: Minimum Write Recovery Time (tWRmin)
                /// </summary>
                public Timing tWRmin {
                    get => new Timing {
                        Medium = RawData[193 + _offset],
                    };
                }

                /// <summary>
                /// Byte 195 or 230: Minimum Active to Precharge Delay Time (tRASmin), Least Significant Byte
                /// Byte 194 or 229: Upper Nibble for tRAS 
                /// </summary>
                public Timing tRASmin {
                    get => new Timing {
                        Medium = (short)(RawData[195 + _offset] | Data.SubByte(RawData[194 + _offset], 3, 4) << 8)
                    };
                }

                /// <summary>
                /// Byte 196 or 231: Minimum Active to Active/Refresh Delay Time (tRCmin), Least Significant Byte
                /// Byte 194 or 229: Upper Nibble for tRC 
                /// </summary>
                public Timing tRCmin {
                    get => new Timing {
                        Medium = (short)(RawData[196 + _offset] | Data.SubByte(RawData[194 + _offset], 7, 4) << 8),
                        Fine   = (sbyte)RawData[215 + _offset]
                    };
                }

                /// <summary>
                /// Byte 197 or 232: Maximum tREFI Time (Average Periodic Refresh Interval), Least Significant Byte
                /// Byte 198 or 233: Maximum tREFI Time (Average Periodic Refresh Interval), Most Significant Byte
                /// </summary>
                public Timing tREFI {
                    get => new Timing {
                        Medium = (short)(RawData[197 + _offset] | RawData[198 + _offset] << 8)
                    };
                }

                /// <summary>
                /// Byte 199 or 234: Minimum Refresh Recovery Delay Time (tRFCmin), Least Significant Byte
                /// Byte 200 or 235: Minimum Refresh Recovery Delay Time(tRFCmin), Most Significant Byte
                /// </summary>
                public Timing tRFCmin {
                    get => new Timing {
                        Medium = (short)(RawData[199 + _offset] | RawData[200 + _offset] << 8)
                    };
                }

                /// <summary>
                /// Byte 201 or 236: Minimum Internal Read to Precharge Command Delay Time (tRTPmin)
                /// </summary>
                public Timing tRTPmin {
                    get => new Timing {
                        Medium = RawData[201 + _offset]
                    };
                }

                /// <summary>
                /// Byte 202 or 237: Minimum Row Active to Row Active Delay Time (tRRDmin)
                /// </summary>
                public Timing tRRDmin {
                    get => new Timing {
                        Medium = RawData[202 + _offset]
                    };
                }

                /// <summary>
                /// Byte 204 or 239: Minimum Four Activate Window Delay Time (tFAWmin), Least Significant Byte
                /// Byte 203 or 238: Upper Nibble for tFAW
                /// </summary>
                public Timing tFAWmin {
                    get => new Timing {
                        Medium = (short)(RawData[204 + _offset] | RawData[203 + _offset] << 8)
                    };
                }

                /// <summary>
                /// Byte 205 or 240: Minimum Internal Write to Read Command Delay Time (tWTRmin)
                /// </summary>
                public Timing tWTRmin {
                    get => new Timing {
                        Medium = RawData[205 + _offset]
                    };
                }

                /// <summary>
                /// Describes the ability to ‘potentially’ remove some of the turn around time spacing between read and write commands
                /// </summary>
                public struct CmdTurnAroundTimeOptimization {
                    public TurnAroundAdjustment Adjustment;
                    public Byte Clocks;

                }
                public enum TurnAroundAdjustment {
                    PullIn,
                    PushOut
                }

                /// <summary>
                /// CMD Turn-around Time Optimizations data
                /// </summary>
                public struct CmdTurnAroundTimeOptimizationData {
                    public CmdTurnAroundTimeOptimization ReadToWrite;
                    public CmdTurnAroundTimeOptimization WriteToRead;
                    public CmdTurnAroundTimeOptimization BackToBack;
                }

                /// <summary>
                /// Byte 206 or 241: Write to Read and Read to Write CMD Turn-around Time Optimizations
                /// Byte 207 or 242: Back 2 Back CMD Turn-around Time Optimizations
                /// </summary>
                public CmdTurnAroundTimeOptimizationData CmdTurnAroundTimeOptimizations {
                    get => new CmdTurnAroundTimeOptimizationData {
                        ReadToWrite = new CmdTurnAroundTimeOptimization {
                            Adjustment = (TurnAroundAdjustment)Data.BoolToNum<byte>(Data.GetBit(RawData[206 + _offset], 7)),
                            Clocks     = Data.SubByte(RawData[206 + _offset], 6, 3)
                        },
                        WriteToRead = new CmdTurnAroundTimeOptimization {
                            Adjustment = (TurnAroundAdjustment)Data.BoolToNum<byte>(Data.GetBit(RawData[206 + _offset], 3)),
                            Clocks     = Data.SubByte(RawData[206 + _offset], 2, 3)
                        },
                        BackToBack = new CmdTurnAroundTimeOptimization {
                            Adjustment = (TurnAroundAdjustment)Data.BoolToNum<byte>(Data.GetBit(RawData[207 + _offset], 3)),
                            Clocks     = Data.SubByte(RawData[207 + _offset], 2, 3)
                        }
                    };
                }

                /// <summary>
                /// Byte 210: Memory controller voltage level
                /// </summary>
                public VoltageLevelData MemoryControllerVoltage {
                    get => new VoltageLevelData {
                        Integer  = Data.SubByte(RawData[210 + _offset], 7, 3),
                        Fraction = Data.SubByte(RawData[210 + _offset], 4, 5)
                    };
                }

                /// <summary>
                /// Byte 208 or 243: System CMD Rate Mode
                /// </summary>
                public Timing SystemCmdRateMode {
                    get => new Timing {
                        Medium = RawData[208 + _offset],
                    };
                }

                public override string ToString() {

                    return !Enabled ? "" :
                        $"{1000 / tCKmin.ToNanoSeconds()} MHz " +
                        $"{tAAmin.ToClockCycles(tCKmin)}-" +
                        $"{tRCDmin.ToClockCycles(tCKmin)}-" +
                        $"{tRPmin.ToClockCycles(tCKmin)}-" +
                        $"{tRASmin.ToClockCycles(tCKmin)} " +
                        $"{VDDVoltage}V";
                }
            }
        }
    }
}