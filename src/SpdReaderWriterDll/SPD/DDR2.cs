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
        /// DDR2 SPD
        /// </summary>
        public struct DDR2 : ISpd {

            /// <summary>
            /// New instance of DDR2 SPD class
            /// </summary>
            /// <param name="input">Raw SPD data</param>
            public DDR2(byte[] input) {
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

            public override string ToString() {
                return $"{GetManufacturerName(ManufacturerIdCode.ManufacturerId)} {PartNumber}".Trim();
            }

            /// <summary>
            /// Byte 0: Number of Bytes Utilized by Module Manufacturer
            /// Byte 1: Total Number of Bytes in Serial PD Device
            /// </summary>
            public BytesData Bytes {
                get => new BytesData {
                    Used  = RawData[0],
                    Total = (ushort)Math.Pow(2, RawData[1])
                };
            }

            int ISpd.SpdBytesUsed => Bytes.Used;

            /// <summary>
            /// Byte 2: Memory Type
            /// </summary>
            public RamType DramDeviceType {
                get => (RamType)RawData[2];
            }

            /// <summary>
            /// Byte 3: Number of Row Addresses
            /// Byte 4: Number of Column Addresses
            /// </summary>
            public AddressingData Addressing {
                get => new AddressingData {
                    Rows    = RawData[3],
                    Columns = RawData[4]
                };
            }

            /// <summary>
            /// Byte 5: Module Attributes - Number of Ranks, Package and Height
            /// </summary>
            public ModuleAttributesData ModuleAttributes {
                get {
                    float[] heights = { 25.4F, 30.0F, 30.5F };

                    ModuleAttributesData attributes = new ModuleAttributesData {
                        Package    = (DRAMPackage)Data.BoolToNum<byte>(Data.GetBit(RawData[5], 4)),
                        CardOnCard = Data.GetBit(RawData[5], 3),
                        Ranks      = (byte)(Data.SubByte(RawData[5], 2, 3) + 1)
                    };

                    switch (Data.SubByte(RawData[5], 7, 3)) {
                        default:
                            // less than 25.4 mm
                            attributes.Height = new ModuleHeightData {
                                Minimum = 0,
                                Maximum = heights[0],
                                Unit    = HeightUnit.mm
                            };
                            break;
                        case 1:
                            // 25.4 mm
                            attributes.Height = new ModuleHeightData {
                                Minimum = heights[0],
                                Maximum = heights[0],
                                Unit    = HeightUnit.mm
                            };
                            break;
                        case 2:
                            // greater than 25.4 mm and less than 30 mm
                            attributes.Height = new ModuleHeightData {
                                Minimum = heights[0],
                                Maximum = heights[1],
                                Unit    = HeightUnit.mm
                            };
                            break;
                        case 3:
                            // 30.0 mm
                            attributes.Height = new ModuleHeightData {
                                Minimum = heights[1],
                                Maximum = heights[1],
                                Unit    = HeightUnit.mm
                            };
                            break;
                        case 4:
                            // 30.5 mm
                            attributes.Height = new ModuleHeightData {
                                Minimum = heights[2],
                                Maximum = heights[2],
                                Unit    = HeightUnit.mm
                            };
                            break;
                        case 5:
                            // greater than 30.5 mm
                            attributes.Height = new ModuleHeightData {
                                Minimum = heights[2],
                                Unit    = HeightUnit.mm
                            };
                            break;
                    }

                    return attributes;
                }
            }

            /// <summary>
            /// Describes the number of ranks and package on the SDRAM module, and module height
            /// </summary>
            public struct ModuleAttributesData {
                public ModuleHeightData Height;
                public DRAMPackage Package;
                public bool CardOnCard;
                public byte Ranks;
            }

            /// <summary>
            /// Package on the SDRAM module
            /// </summary>
            public enum DRAMPackage {
                Planar,
                Stack
            }

            /// <summary>
            /// Byte 6: Module Data Width
            /// </summary>
            public byte DataWidth {
                get => RawData[6];
            }

            /// <summary>
            /// Byte 8: Voltage Interface Level of this assembly
            /// </summary>
            public VoltageLevel VoltageInterfaceLevel {
                get => (VoltageLevel)RawData[8];
            }

            /// <summary>
            /// Timing data
            /// </summary>
            public struct Timing {
                public int Whole;      // x1
                public int Tenth;      // x0.1 or extension
                public int Hundredth;  // x0.01
                public int Quarter;    // x0.25
                public int Fraction;   // x fraction

                public float ToNanoSeconds() {
                    float[] tenthExtenstion = { 0.25F, 0.33F, 0.66F, 0.75F }; // Extension of tenths
                    float[] fractions = { 0F, 0.25F, 0.33F, 0.5F, 0.66F, 0.75F }; // for tRC & tRFC

                    return Whole +
                           Quarter * 0.25F +
                           (10 <= Tenth && Tenth <= 13 ? tenthExtenstion[Tenth - 10] : (Tenth * 0.1F)) +
                           Hundredth * 0.01F +
                           fractions[Fraction];
                }

                public int ToClockCycles(Timing refTiming) {
                    return (int)Math.Ceiling(ToNanoSeconds() / refTiming.ToNanoSeconds());
                }

                public override string ToString() {
                    return ToNanoSeconds().ToString("F2");
                }
            }

            /// <summary>
            /// Byte 9: SDRAM Cycle Time
            /// </summary>
            public Timing tCKmin {
                get => new Timing {
                    Whole = Data.SubByte(RawData[9], 7, 4),
                    Tenth = Data.SubByte(RawData[9], 3, 4)
                };
            }

            /// <summary>
            /// Byte 10: SDRAM Access from Clock (tAC)
            /// </summary>
            public Timing tAC {
                get => new Timing {
                    Tenth     = Data.SubByte(RawData[10], 7, 4),
                    Hundredth = Data.SubByte(RawData[10], 3, 4)
                };
            }

            /// <summary>
            /// Byte 11: DIMM Configuration Type
            /// </summary>
            public DIMMConfigurationData DIMMConfiguration {
                get => new DIMMConfigurationData {
                    AddressCommandParity = Data.GetBit(RawData[11], 2),
                    DataECC              = Data.GetBit(RawData[11], 1),
                    DataParity           = Data.GetBit(RawData[11], 0)
                };
            }

            /// <summary>
            /// Describes the module’s error detection and/or correction schemes on the data, address and command buses
            /// </summary>
            public struct DIMMConfigurationData {
                public bool AddressCommandParity;
                public bool DataECC;
                public bool DataParity;
            }

            /// <summary>
            /// Byte 12: Refresh Rate
            /// </summary>
            public RefreshRateData RefreshRate {
                get => new RefreshRateData {
                    RefreshPeriod = RawData[12]
                };
            }

            /// <summary>
            /// Describes the module’s refresh rate in microseconds
            /// </summary>
            public struct RefreshRateData {
                public byte RefreshPeriod;

                public float ToMicroseconds() {

                    float normal = 15.625F;

                    // Normal
                    if (RefreshPeriod == 0x80) {
                        return normal;
                    }

                    // Reduced
                    if (0x81 <= RefreshPeriod && RefreshPeriod <= 0x82) {
                        return normal * 0.25F * (RefreshPeriod - 0x80);
                    }

                    // Extended
                    if (0x83 <= RefreshPeriod && RefreshPeriod <= 0x85) {
                        return normal * (1 << RefreshPeriod - 0x81);
                    }

                    throw new ArgumentOutOfRangeException(nameof(RefreshPeriod));
                }

                public override string ToString() {
                    return ToMicroseconds().ToString("F3");
                }
            }

            /// <summary>
            /// Byte 13: Primary SDRAM Width
            /// </summary>
            public byte PrimarySDRAMWidth {
                get => RawData[13];
            }

            /// <summary>
            /// Byte 14: Error Checking SDRAM Width
            /// </summary>
            public byte ErrorCheckingSDRAMWidth {
                get => RawData[14];
            }

            /// <summary>
            /// Byte 16: SDRAM Device Attributes – Burst Lengths Supported 
            /// </summary>
            public BurstLengthData[] BurstLength {
                get {
                    BurstLengthData[] attributes = new BurstLengthData[2];

                    for (byte i = 0; i < attributes.Length; i++) {
                        attributes[i].Length    = (byte)(1 << i);
                        attributes[i].Supported = Data.GetBit(RawData[16], (byte)(i + 2));
                    }

                    return attributes;
                }
            }

            /// <summary>
            /// Describes which various programmable burst lengths are supported
            /// </summary>
            public struct BurstLengthData {
                public byte Length;
                public bool Supported;
            }

            /// <summary>
            /// Byte 17: SDRAM Device Attributes – Number of Banks on SDRAM Device
            /// </summary>
            public byte DeviceBanks {
                get => RawData[17];
            }

            /// <summary>
            /// Byte 18: SDRAM Device Attributes – CAS Latency
            /// </summary>
            public CasLatenciesData tCL {
                get => new CasLatenciesData {
                    Bitmask = RawData[18]
                };
            }

            /// <summary>
            /// Describes which of the programmable CAS latencies are acceptable for the module
            /// </summary>
            public struct CasLatenciesData {
                public byte Bitmask;

                /// <summary>
                /// Returns a numeric array of latencies
                /// </summary>
                /// <returns>An array of supported latencies</returns>
                public int[] ToArray() {
                    Queue<int> latencies = new Queue<int>();
                    for (byte i = 2; i <= 7; i++) {
                        if (Data.GetBit(Bitmask, i)) {
                            latencies.Enqueue(i);
                        }
                    }

                    return latencies.ToArray();
                }

                public override string ToString() {

                    string latenciesString = "";
                    foreach (byte latency in ToArray()) {
                        latenciesString += $"{latency},";
                    }

                    return latenciesString.TrimEnd(',');
                }
            }

            /// <summary>
            /// Byte 19: DIMM Mechanical Characteristics
            /// </summary>
            public ModuleThicknessData Thickness {
                get => new ModuleThicknessData {
                    Encoding = Data.SubByte(RawData[19], 2, 3)
                };
            }

            /// <summary>
            /// Defines a unique encoding of thickness
            /// </summary>
            public struct ModuleThicknessData {
                public byte Encoding;
            }

            /// <summary>
            /// Byte 20: DIMM type information
            /// </summary>
            public BaseModuleType DimmType {
                get => (BaseModuleType)(Data.SubByte(RawData[20], 5, 6));
            }

            /// <summary>
            /// Identifies the DDR2 SDRAM memory module type
            /// </summary>
            public enum BaseModuleType {
                [Description("Registered Dual In-Line Memory Module")]
                RDIMM = 1,
                [Description("Unbuffered Dual In-Line Memory Module")]
                UDIMM,
                [Description("Small Outline Dual In-Line Memory Module")] 
                SO_DIMM,
                [Description("Clocked SO-DIMM with 72-bit data bus")]
                _72b_SO_CDIMM,
                [Description("Registered SO-DIMM with 72-bit data bus")]
                _72b_SO_RDIMM,
                [Description("Micro Dual In-Line Memory Module")]
                Micro_DIMM,
                [Description("Mini Registered Dual In-Line Memory Module")]
                Mini_RDIMM,
                [Description("Mini Unbuffered Dual In-Line Memory Module")]
                Mini_UDIMM
            }

            /// <summary>
            /// Byte 21: SDRAM Modules Attributes
            /// </summary>
            public ModulesAttributes SDRAMModulesAttributes {
                get => new ModulesAttributes {
                    AnalysisProbeInstalled = Data.GetBit(RawData[21], 6),
                    FETSwitchExternal      = Data.GetBit(RawData[21], 4),
                    PLLs                   = Data.SubByte(RawData[20], 3, 2),
                    ActiveRegisters        = Data.SubByte(RawData[20], 1, 2)
                };
            }

            /// <summary>
            /// Depicts various aspects of the module
            /// </summary>
            public struct ModulesAttributes {
                public bool AnalysisProbeInstalled;
                public bool FETSwitchExternal;
                public byte PLLs;
                public byte ActiveRegisters;
            }

            /// <summary>
            /// Byte 22: SDRAM Device Attributes – General
            /// </summary>
            public DeviceAttributes GeneralAttributes {
                get => new DeviceAttributes {
                    PartialArraySelfRefresh = Data.GetBit(RawData[22], 2),
                    Supports50ohmODT        = Data.GetBit(RawData[22], 1),
                    WeakDriver              = Data.GetBit(RawData[22], 0)
                };
            }

            /// <summary>
            /// Depicts various aspects of the SDRAMs
            /// </summary>
            public struct DeviceAttributes {
                public bool PartialArraySelfRefresh;
                public bool Supports50ohmODT;
                public bool WeakDriver;
            }

            /// <summary>
            /// Byte 23: Minimum Clock Cycle Time at Reduced CAS Latency, X-1
            /// </summary>
            public Timing tCKminX1 {
                get => new Timing {
                    Whole = Data.SubByte(RawData[23], 7, 4),
                    Tenth = Data.SubByte(RawData[23], 3, 4)
                };
            }

            /// <summary>
            /// Byte 24: Maximum Data Access Time (tAC) from Clock at CL X-1
            /// </summary>
            public Timing tACmaxX1 {
                get => new Timing {
                    Tenth     = Data.SubByte(RawData[24], 7, 4),
                    Hundredth = Data.SubByte(RawData[24], 3, 4)
                };
            }

            /// <summary>
            /// Byte 25: Minimum Clock Cycle Time at CL X-2
            /// </summary>
            public Timing tCKminX2 {
                get => new Timing {
                    Whole = Data.SubByte(RawData[25], 7, 4),
                    Tenth = Data.SubByte(RawData[25], 3, 4)
                };
            }

            /// <summary>
            /// Byte 26: Maximum Data Access Time (tAC) from Clock at CL X-2
            /// </summary>
            public Timing tACmaxX2 {
                get => new Timing {
                    Tenth     = Data.SubByte(RawData[26], 7, 4),
                    Hundredth = Data.SubByte(RawData[26], 3, 4)
                };
            }

            /// <summary>
            /// Byte 27: Minimum Row Precharge Time (tRP)
            /// </summary>
            public Timing tRP {
                get => new Timing {
                    Whole   = Data.SubByte(RawData[27], 7, 6),
                    Quarter = Data.SubByte(RawData[27], 1, 2)
                };
            }

            /// <summary>
            /// Byte 28: Minimum Row Active to Row Active Delay (tRRD)
            /// </summary>
            public Timing tRRD {
                get => new Timing {
                    Whole   = Data.SubByte(RawData[28], 7, 6),
                    Quarter = Data.SubByte(RawData[28], 1, 2)
                };
            }

            /// <summary>
            /// Byte 29: Minimum RAS to CAS Delay (tRCD)
            /// </summary>
            public Timing tRCD {
                get => new Timing {
                    Whole   = Data.SubByte(RawData[29], 7, 6),
                    Quarter = Data.SubByte(RawData[29], 1, 2)
                };
            }

            /// <summary>
            /// Byte 30: Minimum Active to Precharge Time (tRAS)
            /// </summary>
            public Timing tRAS {
                get => new Timing {
                    Whole = RawData[30],
                };
            }

            /// <summary>
            /// Byte 31: Module Rank Density in Megabytes
            /// </summary>
            public ushort RankDensity {
                get {
                    byte densityData = RawData[31];
                    return densityData <= 16 ? (ushort)(densityData * 1024) : (ushort)(densityData * 4);
                }
            }

            /// <summary>
            /// Calculated die density in bits
            /// </summary>
            public ulong DieDensity {
                get => (ulong)(
                    (1L << Addressing.Rows) *
                    (1L << Addressing.Columns) *
                    DeviceBanks *
                    PrimarySDRAMWidth);
            }

            /// <summary>
            /// The total memory capacity of the DRAM on the module in bytes
            /// </summary>
            public ulong TotalModuleCapacity {
                get => (ulong)(
                    (1L << Addressing.Rows) *
                    (1L << Addressing.Columns) *
                    DeviceBanks *
                    (DataWidth & 0xF0) * // do not include ECC width
                    ModuleAttributes.Ranks / 8);
            }

            /// <summary>
            /// Byte 32: Address and Command Setup Time Before Clock (tIS)
            /// </summary>
            public Timing tIS {
                get => new Timing {
                    Tenth     = Data.SubByte(RawData[32], 7, 4),
                    Hundredth = Data.SubByte(RawData[32], 3, 4)
                };
            }

            /// <summary>
            /// Byte 33: Address and Command Hold Time After Clock (tIH)
            /// </summary>
            public Timing tIH {
                get => new Timing {
                    Tenth     = Data.SubByte(RawData[33], 7, 4),
                    Hundredth = Data.SubByte(RawData[33], 3, 4)
                };
            }

            /// <summary>
            /// Byte 34: Data Input Setup Time Before Strobe (tDS)
            /// </summary>
            public Timing tDS {
                get => new Timing {
                    Tenth     = Data.SubByte(RawData[34], 7, 4),
                    Hundredth = Data.SubByte(RawData[34], 3, 4)
                };
            }

            /// <summary>
            /// Byte 35: Data Input Hold Time After Strobe (tDH)
            /// </summary>
            public Timing tDH {
                get => new Timing {
                    Tenth     = Data.SubByte(RawData[35], 7, 4),
                    Hundredth = Data.SubByte(RawData[35], 3, 4)
                };
            }

            /// <summary>
            /// Byte 36: Write Recovery Time (tWR)
            /// </summary>
            public Timing tWR {
                get => new Timing {
                    Whole   = Data.SubByte(RawData[36], 7, 6),
                    Quarter = Data.SubByte(RawData[36], 1, 2)
                };
            }

            /// <summary>
            /// Byte 37: Internal write to read command delay (tWTR)
            /// </summary>
            public Timing tWTR {
                get => new Timing {
                    Whole   = Data.SubByte(RawData[37], 7, 6),
                    Quarter = Data.SubByte(RawData[37], 1, 2)
                };
            }

            /// <summary>
            /// Byte 38: Internal read to precharge command delay (tRTP)
            /// </summary>
            public Timing tRTP {
                get => new Timing {
                    Whole   = Data.SubByte(RawData[38], 7, 6),
                    Quarter = Data.SubByte(RawData[38], 1, 2)
                };
            }

            /// <summary>
            /// Byte 40: Extension of Byte 41 tRC
            /// Byte 41: SDRAM Device Minimum Activate to Activate/Refresh Time (tRC)
            /// </summary>
            public Timing tRC {
                get => new Timing {
                    Whole    = RawData[41],
                    Fraction = Data.SubByte(RawData[40], 7, 4)
                };
            }

            /// <summary>
            /// Byte 42: SDRAM Device Minimum Refresh to Activate/Refresh Command Period (tRFC)
            /// </summary>
            public Timing tRFC {
                get => new Timing {
                    Whole    = RawData[42],
                    Fraction = Data.SubByte(RawData[40], 3, 3)
                };
            }

            /// <summary>
            /// Byte 43: SDRAM Device Maximum Device Cycle Time (tCK max)
            /// </summary>
            public Timing tCKmax {
                get => new Timing {
                    Whole = Data.SubByte(RawData[43], 7, 4),
                    Tenth = Data.SubByte(RawData[43], 3, 4)
                };
            }

            /// <summary>
            /// Byte 44: SDRAM Device DQS-DQ Skew for DQS and associated DQ signals (tDQSQ max)
            /// </summary>
            public Timing tDQSQmax {
                get => new Timing {
                    Hundredth = RawData[44]
                };
            }

            /// <summary>
            /// Byte 45: SDRAM Device Read Data Hold Skew Factor (tQHS)
            /// </summary>
            public Timing tQHS {
                get => new Timing {
                    Hundredth = RawData[45]
                };
            }

            /// <summary>
            /// Byte 46: PLL Relock Time
            /// </summary>
            public byte PLLRelockTime {
                get => RawData[46];
            }

            public struct TemperatureData {
                public float Granularity;
                public byte Multiplier;

                public float ToDegrees() {
                    return Granularity * Multiplier;
                }

                public override string ToString() {
                    return ToDegrees().ToString("F3");
                }
            }

            public struct TcasemaxData {
                public TemperatureData Tcasemax;
                public TemperatureData DT4R4WDelta;
            }

            /// <summary>
            /// Byte 47: Tcasemax
            /// </summary>
            public TcasemaxData Tcasemax {
                get => new TcasemaxData {
                    Tcasemax = new TemperatureData {
                        Granularity = 2,
                        Multiplier  = Data.SubByte(RawData[47], 7, 4)
                    },
                    DT4R4WDelta = new TemperatureData {
                        Granularity = 0.4F,
                        Multiplier  = Data.SubByte(RawData[47], 3, 4)
                    }
                };
            }

            /// <summary>
            /// Byte 48: Thermal Resistance of DRAM Package from Top (Case) to Ambient ( Psi T-A DRAM )
            /// </summary>
            public TemperatureData PsiTADRAM {
                get => new TemperatureData {
                    Granularity = 0.5F,
                    Multiplier  = RawData[48]
                };
            }

            /// <summary>
            /// Byte 49: DRAM Case Temperature Rise from Ambient due to Activate-Precharge/Mode Bits(DT0/Mode Bits)
            /// </summary>
            public TemperatureData DT0 {
                get => new TemperatureData {
                    Granularity = 0.3F,
                    Multiplier  = Data.SubByte(RawData[49], 7, 6)
                };
            }

            /// <summary>
            /// Defines whether or not double refresh is required for DRAM case temperature exceeding 85 deg C
            /// </summary>
            public bool DoubleRefreshRateRequired {
                get => Data.GetBit(RawData[49], 1);
            }

            /// <summary>
            /// Indicates DDR2 SDRAM "High Temperature Self Refresh" support
            /// </summary>
            public bool HighTemperatureSelfRefreshSupported {
                get => Data.GetBit(RawData[49], 0);
            }

            /// <summary>
            /// Byte 50: DRAM Case Temperature Rise from Ambient due to Precharge/Quiet Standby (DT2N/DT2Q)
            /// </summary>
            public TemperatureData DT2N {
                get => new TemperatureData {
                    Granularity = 0.1F,
                    Multiplier  = RawData[50]
                };
            }

            /// <summary>
            /// Byte 51: DRAM Case Temperature Rise from Ambient due to Precharge Power-Down (DT2P)
            /// </summary>
            public TemperatureData DT2P {
                get => new TemperatureData {
                    Granularity = 0.015F,
                    Multiplier  = RawData[51]
                };
            }

            /// <summary>
            /// Byte 52: DRAM Case Temperature Rise from Ambient due to Active Standby (DT3N)
            /// </summary>
            public TemperatureData DT3N {
                get => new TemperatureData {
                    Granularity = 0.15F,
                    Multiplier  = RawData[52]
                };
            }

            /// <summary>
            /// Byte 53: DRAM Case temperature Rise from Ambient due to Active Power-Down with Fast PDN Exit(DT3Pfast)
            /// </summary>
            public TemperatureData DT3Pfast {
                get => new TemperatureData {
                    Granularity = 0.05F,
                    Multiplier  = RawData[53]
                };
            }

            /// <summary>
            /// Byte 54: DRAM Case temperature Rise from Ambient due to Active Power-Down with Slow PDN Exit(DT3Pslow)
            /// </summary>
            public TemperatureData DT3Pslow {
                get => new TemperatureData {
                    Granularity = 0.025F,
                    Multiplier  = RawData[54]
                };
            }

            /// <summary>
            /// Byte 55: DRAM Case Temperature Rise from Ambient due to Page Open Burst Read/DT4R4W Mode Bit(DT4R/DT4R4W Mode Bit)
            /// </summary>
            public TemperatureData DT4R {
                get => new TemperatureData {
                    Granularity = 0.4F,
                    Multiplier  = Data.SubByte(RawData[55], 7, 7)
                };
            }

            /// <summary>
            /// Specifies <see langword="false"/> if DT4W  is greater than or equal to DT4R,
            /// or <see langword="false"/> if DT4W less than DT4R .
            /// </summary>
            public bool DT4R4WMode {
                get => Data.GetBit(RawData[55], 0);
            }

            /// <summary>
            /// Byte 56: DRAM Case Temperature Rise from Ambient due to Burst Refresh (DT5B)
            /// </summary>
            public TemperatureData DT5B {
                get => new TemperatureData {
                    Granularity = 0.5F,
                    Multiplier  = RawData[56]
                };
            }

            /// <summary>
            /// Byte 57: DRAM Case Temperature Rise from Ambient due to Bank Interleave Reads with Auto-Precharge(DT7)
            /// </summary>
            public TemperatureData DT7 {
                get => new TemperatureData {
                    Granularity = 0.5F,
                    Multiplier  = RawData[57]
                };
            }

            /// <summary>
            /// Byte 58: Thermal Resistance of PLL Package from Top (Case) to Ambient ( Psi T-A PLL )
            /// </summary>
            public TemperatureData PsiTAPLL {
                get => new TemperatureData {
                    Granularity = 0.5F,
                    Multiplier  = RawData[58]
                };
            }

            /// <summary>
            /// Byte 59: Thermal Resistance of Register Package from Top (Case) to Ambient ( Psi T-A Register)
            /// </summary>
            public TemperatureData PsiTARegister {
                get => new TemperatureData {
                    Granularity = 0.5F,
                    Multiplier  = RawData[59]
                };
            }

            /// <summary>
            /// Byte 60: PLL Case Temperature Rise from Ambient due to PLL Active (DT PLL Active)
            /// </summary>
            public TemperatureData DTPLLActive {
                get => new TemperatureData {
                    Granularity = 0.25F,
                    Multiplier  = RawData[60]
                };
            }

            /// <summary>
            /// Byte 62: SPD Revision
            /// </summary>
            public SpdRevisionData SpdRevision {
                get => new SpdRevisionData {
                    EncodingLevel  = Data.SubByte(RawData[62], 7, 4),
                    AdditionsLevel = Data.SubByte(RawData[62], 3, 4)
                };
            }

            /// <summary>
            /// Byte 63: Checksum for Bytes 0-62
            /// </summary>
            public Crc8Data Crc {
                get {
                    Crc8Data crc = new Crc8Data {
                        Contents = new byte[64]
                    };

                    Array.Copy(
                        sourceArray      : RawData,
                        destinationArray : crc.Contents,
                        length           : crc.Contents.Length);

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

                Array.Copy(
                    sourceArray      : Crc.Fix(),
                    destinationArray : RawData, 
                    length           : Crc.Contents.Length);

                return CrcStatus;
            }

            /// <summary>
            /// Bytes 64-71: Module Manufacturer’s JEDEC ID Code
            /// </summary>
            public ManufacturerIdCodeData ManufacturerIdCode {
                get {
                    byte continuationCode = 0;
                    byte manufacturerCode = 0;

                    for (byte i = 64; i <= 71; i++) {
                        if (RawData[i] == 0x7F) {
                            continuationCode++;
                        }
                        else {
                            manufacturerCode = RawData[i];
                            break;
                        }
                    }

                    return new ManufacturerIdCodeData {
                        ContinuationCode = continuationCode,
                        ManufacturerCode = manufacturerCode
                    };
                }
            }

            /// <summary>
            /// Byte 72: Module Manufacturing Location
            /// </summary>
            public byte ManufacturingLocation {
                get => RawData[72];
            }

            /// <summary>
            /// Bytes 73-90: Module Part Number
            /// </summary>
            public string PartNumber {
                get {
                    int modelNameStart = 73;

                    char[] chars = new char[90 - modelNameStart + 1];

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
            /// Byte 72: Module Manufacturing Location
            /// </summary>
            public byte ModuleManufacturingLocation {
                get => RawData[72];
            }

            /// <summary>
            /// Bytes 91-92: Module Revision Code
            /// </summary>
            public ushort RevisionCode {
                get => (ushort)(RawData[92] | RawData[91] << 8);
            }

            /// <summary>
            /// Bytes 93-94: Module Manufacturing Date
            /// </summary>
            public DateCodeData ModuleManufacturingDate {
                get => new DateCodeData {
                    Year = RawData[93],
                    Week = RawData[94],
                };
            }

            /// <summary>
            /// Bytes 95-98: Module Serial Number
            /// </summary>
            public SerialNumberData ModuleSerialNumber {
                get {
                    byte[] serialNumberBytes = new byte[4];

                    Array.Copy(
                        sourceArray      : RawData,
                        sourceIndex      : 95,
                        destinationArray : serialNumberBytes,
                        destinationIndex : 0,
                        length           : serialNumberBytes.Length);

                    return new SerialNumberData {
                        SerialNumber = serialNumberBytes
                    };
                }
            }

            /// <summary>
            /// EPP Identifier String ("NVm")
            /// </summary>
            public bool EppPresence {
                get => Data.MatchArray(RawData, ProfileId.EPP, 99);
            }

            /// <summary>
            /// EPP Profile Type Identifier
            /// </summary>
            public EppProfileType EppType {
                get => (EppProfileType)RawData[102];
            }

            /// <summary>
            /// One of two defined profile types
            /// </summary>
            public enum EppProfileType {
                Abbreviated = 0xA1,
                Full        = 0xB1
            }

            /// <summary>
            /// Full EPP Profiles
            /// </summary>
            public EppFullProfileData[] EppFullProfile {
                get {
                    EppFullProfileData[] eppFullProfile = new EppFullProfileData[EppType == EppProfileType.Full ? 2 : 0];
                    for (byte i = 0; i < eppFullProfile.Length; i++) {
                        eppFullProfile[i].Number = i;
                    }

                    return eppFullProfile;
                }
            }

            /// <summary>
            /// Abbreviated EPP Profiles
            /// </summary>
            public EppAbbreviatedProfileData[] EppAbbreviatedProfile {
                get {
                    EppAbbreviatedProfileData[] eppAbbreviatedProfile = new EppAbbreviatedProfileData[EppType == EppProfileType.Abbreviated ? 4 : 0];
                    for (byte i = 0; i < eppAbbreviatedProfile.Length; i++) {
                        eppAbbreviatedProfile[i].Number = i;
                    }

                    return eppAbbreviatedProfile;
                }
            }

            /// <summary>
            /// Full Performance Profile Map
            /// </summary>
            public struct EppFullProfileData {

                public byte Number;

                private byte _offset => (byte)(Number * 12);

                /// <summary>
                /// Profile for Optimal Performance
                /// </summary>
                public bool IsOptimal {
                    get => Data.SubByte(RawData[103], 1, 2) == Number;
                }

                /// <summary>
                /// Specifies which profiles contain valid data
                /// </summary>
                public bool Enabled {
                    get => Data.GetBit(Data.SubByte(RawData[103], 5, 2), Number);
                }

                /// <summary>
                /// Describes the voltage level required for this profile
                /// </summary>
                public float VoltageLevel {
                    get => (Data.SubByte(RawData[104 + _offset], 6, 7) * 25 + 1800) / 1000F;
                }

                /// <summary>
                /// Defines the address command rate
                /// </summary>
                public byte AddressCmdRate {
                    get => (byte)(Data.SubByte(RawData[104 + _offset], 7, 1) + 1);
                }

                /// <summary>
                /// Defines the address drive strength
                /// </summary>
                public float AddressDriveStrength {
                    get {
                        float[] values = { 1, 1.25F, 1.5F, 2 };
                        return values[Data.SubByte(RawData[105 + _offset], 1, 2)];
                    }
                }

                /// <summary>
                /// Defines the chip select drive strength
                /// </summary>
                public float ChipSelectDriveStrength {
                    get {
                        float[] values = { 1, 1.25F, 1.5F, 2 };
                        return values[Data.SubByte(RawData[105 + _offset], 3, 2)];
                    }
                }

                /// <summary>
                /// Defines the clock drive strength
                /// </summary>
                public float ClockDriveStrength {
                    get => Data.SubByte(RawData[105 + _offset], 5, 2) * 0.25F + 0.75F;
                }

                /// <summary>
                /// Defines the data drive strength
                /// </summary>
                public float DataDriveStrength {
                    get => Data.SubByte(RawData[105 + _offset], 7, 2) * 0.25F + 0.75F;
                }

                /// <summary>
                /// Defines the DQS drive strength
                /// </summary>
                public float DQSDriveStrength {
                    get => Data.SubByte(RawData[106 + _offset], 1, 2) * 0.25F + 0.75F;
                }

                /// <summary>
                /// Defines how long the address and command pins are delayed with respect to the default setup time
                /// </summary>
                public DelayData AddressCommandFineDelay {
                    get => new DelayData {
                        Delay = Data.SubByte(RawData[107 + _offset], 4, 5)
                    };
                }

                /// <summary>
                /// Defines the default setup time for address and command pins
                /// </summary>
                public float AddressCommandSetupTime {
                    get => (Data.BoolToNum<byte>(Data.GetBit(RawData[107 + _offset], 5)) + 1) / 2F;
                }

                /// <summary>
                /// Defines how long the chip-select and ODT pins are delayed with respect to the default setup time
                /// </summary>
                public DelayData ChipSelectDelay {
                    get => new DelayData {
                        Delay = Data.SubByte(RawData[108 + _offset], 4, 5)
                    };
                }

                /// <summary>
                /// Defines the default setup time for chip select and ODT pins
                /// </summary>
                public float ChipSelectSetupTime {
                    get => (Data.SubByte(RawData[108 + _offset], 5, 1) + 1) / 2F;
                }

                /// <summary>
                /// Specifies the minimum cycle time at the desired CAS Latency
                /// </summary>
                public Timing tCK {
                    get => new Timing {
                        Whole = Data.SubByte(RawData[109 + _offset], 7, 4),
                        Tenth = Data.SubByte(RawData[109 + _offset], 3, 4)
                    };
                }

                /// <summary>
                /// Specifies which CAS Latency should be programmed for this Profile
                /// </summary>
                public byte tCL {
                    get {
                        for (byte i = 2; i < 8; i++) {
                            if (Data.GetBit((RawData[110 + _offset] >> i), 0)) {
                                return i;
                            }
                        }

                        return 0;
                    }
                }

                /// <summary>
                /// Minimum RAS to CAS delay
                /// </summary>
                public Timing tRCD {
                    get => new Timing {
                        Whole   = Data.SubByte(RawData[111 + _offset], 7, 6),
                        Quarter = Data.SubByte(RawData[111 + _offset], 1, 2)
                    };
                }

                /// <summary>
                /// Minimum Row Precharge Time
                /// </summary>
                public Timing tRP {
                    get => new Timing {
                        Whole   = Data.SubByte(RawData[112 + _offset], 7, 6),
                        Quarter = Data.SubByte(RawData[112 + _offset], 1, 2)
                    };
                }

                /// <summary>
                /// Minimum Active to Precharge Time
                /// </summary>
                public Timing tRAS {
                    get => new Timing {
                        Whole = RawData[113 + _offset]
                    };
                }

                /// <summary>
                /// Write recovery time
                /// </summary>
                public Timing tWR {
                    get => new Timing {
                        Whole   = Data.SubByte(RawData[114 + _offset], 7, 6),
                        Quarter = Data.SubByte(RawData[114 + _offset], 1, 2)
                    };
                }

                /// <summary>
                /// Minimum Active to Active/Refresh Time
                /// </summary>
                public Timing tRC {
                    get => new Timing {
                        Whole = RawData[115 + _offset]
                    };
                }

                public override string ToString() {
                    return (Enabled ? $"{1000F / tCK.ToNanoSeconds()} MHz " +
                                      $"{tCL}-{tRCD.ToClockCycles(tCK)}-{tRP.ToClockCycles(tCK)}-{tRAS.ToClockCycles(tCK)} " +
                                      $"{VoltageLevel}V" : "") +
                           (IsOptimal ? "+" : "");
                }
            }

            /// <summary>
            /// Abbreviated Performance Profile Map
            /// </summary>
            public struct EppAbbreviatedProfileData {

                public byte Number;

                private byte _offset => (byte)(Number * 6);

                /// <summary>
                /// Profile for Optimal Performance
                /// </summary>
                public bool IsOptimal {
                    get => Data.SubByte(RawData[103], 1, 2) == Number;
                }

                /// <summary>
                /// Specifies which profiles contain valid data
                /// </summary>
                public bool Enabled {
                    get => Data.GetBit(Data.SubByte(RawData[103], 7, 4), Number);
                }

                /// <summary>
                /// Describes the voltage level required for this profile
                /// </summary>
                public float VoltageLevel {
                    get => (Data.SubByte(RawData[104 + _offset], 6, 7) * 25 + 1800) / 1000F;
                }

                /// <summary>
                /// Defines the address command rate
                /// </summary>
                public byte AddressCmdRate {
                    get => (byte)(Data.SubByte(RawData[104 + _offset], 7, 1) + 1);
                }

                /// <summary>
                /// Specifies the minimum cycle time at the desired CAS Latency
                /// </summary>
                public Timing tCK {
                    get => new Timing {
                        Whole = Data.SubByte(RawData[105 + _offset], 7, 4),
                        Tenth = Data.SubByte(RawData[105 + _offset], 3, 4)
                    };
                }

                /// <summary>
                /// Specifies which CAS Latency should be programmed for this Profile
                /// </summary>
                public byte tCL {
                    get {
                        for (byte i = 2; i < 8; i++) {
                            if (Data.GetBit((RawData[106 + _offset] >> i), 0)) {
                                return i;
                            }
                        }

                        return 0;
                    }
                }

                /// <summary>
                /// Minimum RAS to CAS delay
                /// </summary>
                public Timing tRCD {
                    get => new Timing {
                        Whole   = Data.SubByte(RawData[107 + _offset], 7, 6),
                        Quarter = Data.SubByte(RawData[107 + _offset], 1, 2)
                    };
                }

                /// <summary>
                /// Minimum Row Precharge Time
                /// </summary>
                public Timing tRP {
                    get => new Timing {
                        Whole   = Data.SubByte(RawData[108 + _offset], 7, 6),
                        Quarter = Data.SubByte(RawData[108 + _offset], 1, 2)
                    };
                }

                /// <summary>
                /// Minimum Active to Precharge Time
                /// </summary>
                public Timing tRAS {
                    get => new Timing {
                        Whole = RawData[109 + _offset]
                    };
                }

                public override string ToString() {
                    return (Enabled 
                        ? $"{1000F / tCK.ToNanoSeconds()}MHz {tCL}-{tRCD.ToClockCycles(tCK)}-{tRP.ToClockCycles(tCK)}-{tRAS.ToClockCycles(tCK)} {VoltageLevel}V" 
                        : "N/A") + (IsOptimal ? "+" : "");
                }
            }

            /// <summary>
            /// Delays with respect to the default setup time
            /// </summary>
            public struct DelayData {
                public byte Delay;

                public override string ToString() {
                    return $"{Delay}/64";
                }
            }
        }
    }
}