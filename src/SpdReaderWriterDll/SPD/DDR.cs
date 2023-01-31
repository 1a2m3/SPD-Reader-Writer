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
using System.Data;

namespace SpdReaderWriterCore {
    public partial class Spd {

        /// <summary>
        /// DDR SPD
        /// </summary>
        public struct DDR : ISpd {
            /// <summary>
            /// New instance of DDR SPD class
            /// </summary>
            /// <param name="input">Raw SPD data</param>
            public DDR(byte[] input) {
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

            public int SpdBytesUsed => Bytes.Used;

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
            /// Describes the number of ranks
            /// </summary>
            public struct BanksData {
                public byte Physical; // Byte 5
                public byte Logical;  // Byte 17
            }

            /// <summary>
            /// Byte 5: Number of DIMM Banks
            /// Byte 17: SDRAM Device Attributes – Number of Banks on SDRAM Device
            /// </summary>
            public BanksData Banks {
                get => new BanksData {
                    Physical = RawData[5],  // Ranks
                    Logical  = RawData[17], // Bank addresses
                };
            }

            /// <summary>
            /// Byte 6: Module Data Width
            /// Byte 7: Module Data Width Continuation
            /// </summary>
            public ushort DataWidth {
                get => (ushort)(RawData[6] | RawData[7] << 8);
            }

            /// <summary>
            /// Calculated die density in bits
            /// </summary>
            public ulong DieDensity {
                get => (ulong)(
                    (1L << Addressing.Rows) *
                    (1L << Addressing.Columns) *
                    DeviceBanks *
                    PrimarySDRAMWidth.Width);
            }

            /// <summary>
            /// The total memory capacity of the DRAM on the module in bytes
            /// </summary>
            public ulong TotalModuleCapacity {
                get => (ulong)(
                    (1L << Addressing.Rows) *
                    (1L << Addressing.Columns) *
                    DeviceBanks *
                    (DataWidth & 0xF0) *
                    Banks.Physical / 8);
            }

            /// <summary>
            /// Byte 8: Voltage Interface Level of this assembly
            /// </summary>
            public VoltageLevel VoltageInterfaceLevel {
                get => (VoltageLevel)RawData[8];
            }
            
            /// <summary>
            /// Defines the minimum cycle time for the SDRAM module at the highest CAS Latency
            /// </summary>
            public struct Timing {
                public byte Whole;      // x1
                public byte Tenth;      // x0.1 or tenthExtenstion if greater than 9
                public byte Hundredth;  // x0.01
                public byte Quarter;    // x0.25
                public byte Fraction;   // x fraction

                public float ToNanoSeconds() {
                    // Extension of tenths
                    float[] tenthExtenstion = { 0.25F, 0.33F, 0.66F, 0.75F };
                    // Fractions for tRC & tRFC
                    float[] fractions = { 0F, 0.25F, 0.33F, 0.5F, 0.66F, 0.75F };

                    return Whole +
                           Quarter * 0.25F +
                           (10 <= Tenth && Tenth <= 13 ? tenthExtenstion[Tenth - 10] : Tenth * 0.1F) +
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
                    DataECC    = Data.GetBit(RawData[11], 1),
                    DataParity = Data.GetBit(RawData[11], 0)
                };
            }

            /// <summary>
            /// Describes the module’s error detection and/or correction schemes
            /// </summary>
            public struct DIMMConfigurationData {
                public bool DataECC;
                public bool DataParity;
            }

            /// <summary>
            /// Byte 12: Refresh Rate
            /// </summary>
            public RefreshRateData RefreshRate {
                get => new RefreshRateData {
                    RefreshPeriod = Data.SubByte(RawData[12], 6, 7),
                    SelfRefresh   = Data.GetBit(RawData[12], 7)
                };
            }

            /// <summary>
            /// Indicate the width of the primary data SDRAM.
            /// </summary>
            public struct SDRAMWidthData {
                public byte Width;
                public bool Bank2;
            }

            /// <summary>
            /// Byte 13: Primary SDRAM Width
            /// </summary>
            public SDRAMWidthData PrimarySDRAMWidth {
                get => new SDRAMWidthData {
                    Width = Data.SubByte(RawData[13], 6, 7),
                    Bank2 = Data.GetBit(RawData[13], 7)
                };
            }

            /// <summary>
            /// Byte 14: Error Checking SDRAM Width
            /// </summary>
            public SDRAMWidthData ErrorCheckingSDRAMWidth {
                get => new SDRAMWidthData {
                    Width = Data.SubByte(RawData[14], 6, 7),
                    Bank2 = Data.GetBit(RawData[14], 7)
                };
            }

            /// <summary>
            /// Byte 15: SDRAM Device Attributes – Minimum Clock Delay, Back-to-Back Random Column Access
            /// </summary>
            public byte tCCD {
                get => RawData[15];
            }

            /// <summary>
            /// Describes which various programmable burst lengths are supported
            /// </summary>
            public struct BurstLengthData {
                public byte Length;
                public bool Supported;
            }

            /// <summary>
            /// Byte 16: SDRAM Device Attributes – Burst Lengths Supported 
            /// </summary>
            public BurstLengthData[] BurstLength {
                get {
                    BurstLengthData[] attributes = new BurstLengthData[4];

                    for (byte i = 0; i < attributes.Length; i++) {
                        attributes[i].Length    = (byte)(1 << i);
                        attributes[i].Supported = Data.GetBit(RawData[16], i);
                    }

                    return attributes;
                }
            }

            /// <summary>
            /// Byte 17: SDRAM Device Attributes – Number of Banks on SDRAM Device
            /// </summary>
            //TODO: overlaps with BanksData Banks
            public byte DeviceBanks {
                get => RawData[17];
            }

            /// <summary>
            /// Describes which of the programmable CAS latencies are acceptable for the module
            /// </summary>
            public struct CasLatenciesData {
                public byte Bitmask;

                public float[] ToArray() {
                    Queue<float> latencies = new Queue<float>();
                    for (byte i = 0; i <= 6; i++) {
                        if (Data.GetBit(Bitmask, i)) {
                            latencies.Enqueue(i / 2F + 1);
                        }
                    }

                    return latencies.ToArray();
                }

                public override string ToString() {

                    string latenciesString = "";
                    foreach (float latency in ToArray()) {
                        latenciesString += $"{latency},";
                    }

                    return latenciesString.TrimEnd(',');
                }
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
            /// Describes which of the programmable CS latencies are acceptable for the module
            /// </summary>
            public struct LatenciesData {
                public byte Bitmask;

                public int[] ToArray() {
                    Queue<int> latencies = new Queue<int>();
                    for (byte i = 0; i <= 6; i++) {
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
            /// Byte 19: SDRAM Device Attributes – CS Latency
            /// </summary>
            public LatenciesData CS {
                get => new LatenciesData {
                    Bitmask = RawData[19]
                };
            }

            /// <summary>
            /// Byte 20: SDRAM Device Attributes – WE Latency
            /// </summary>
            public LatenciesData WE {
                get => new LatenciesData {
                    Bitmask = RawData[20]
                };
            }

            /// <summary>
            /// Depicts various aspects of the module
            /// </summary>
            public struct ModulesAttributesData {
                public bool DifferentialClockInput;
                public bool FETSwitchExternal;
                public bool FETSwitchOnCard;
                public bool OnCardPLL;
                public bool RegisteredAddressControlInputs;
                public bool BufferedAddressControlInputs;
            }

            /// <summary>
            /// Byte 21: SDRAM Modules Attributes
            /// </summary>
            public ModulesAttributesData ModulesAttributes {
                get => new ModulesAttributesData {
                    DifferentialClockInput         = Data.GetBit(RawData[21], 5),
                    FETSwitchExternal              = Data.GetBit(RawData[21], 4),
                    FETSwitchOnCard                = Data.GetBit(RawData[21], 3),
                    OnCardPLL                      = Data.GetBit(RawData[21], 2),
                    RegisteredAddressControlInputs = Data.GetBit(RawData[21], 1),
                    BufferedAddressControlInputs   = Data.GetBit(RawData[21], 0)
                };
            }

            /// <summary>
            /// Depicts various aspects of the SDRAMs on the module
            /// </summary>
            public struct DeviceAttributesData {
                public bool FastAP;
                public bool ConcurrentAutoPrecharge;
                public bool UpperVccTolerance;
                public bool LowerVccTolerance;
                public bool WeakDriver;
            }

            /// <summary>
            /// Byte 22: SDRAM Device Attributes – General
            /// </summary>
            public DeviceAttributesData DeviceAttributes {
                get => new DeviceAttributesData {
                    FastAP                  = Data.GetBit(RawData[22], 7),
                    ConcurrentAutoPrecharge = Data.GetBit(RawData[22], 6),
                    UpperVccTolerance       = Data.GetBit(RawData[22], 5),
                    LowerVccTolerance       = Data.GetBit(RawData[22], 4),
                    WeakDriver              = Data.GetBit(RawData[22], 0)
                };
            }

            /// <summary>
            /// Byte 23: Minimum Clock Cycle Time at Reduced CAS Latency, X- 0.5
            /// </summary>
            public Timing tCKminX05 {
                get => new Timing {
                    Whole = Data.SubByte(RawData[23], 7, 4),
                    Tenth = Data.SubByte(RawData[23], 3, 4)
                };
            }

            /// <summary>
            /// Byte 24: Maximum Data Access Time (tAC) from Clock at CLX- 0.5
            /// </summary>
            public Timing tACmaxX05 {
                get => new Timing {
                    Tenth     = Data.SubByte(RawData[24], 7, 4),
                    Hundredth = Data.SubByte(RawData[24], 3, 4)
                };
            }

            /// <summary>
            /// Byte 25: Minimum Clock Cycle Time at CL X-1
            /// </summary>
            public Timing tCKminX1 {
                get => new Timing {
                    Whole = Data.SubByte(RawData[25], 7, 4),
                    Tenth = Data.SubByte(RawData[25], 3, 4)
                };
            }

            /// <summary>
            /// Byte 26: Maximum Data Access Time (tAC) from Clock at CL X-1
            /// </summary>
            public Timing tACmaxX1 {
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
            /// Byte 31: Module Bank Density in Megabytes
            /// </summary>
            /// <remarks>Densities 4MB-16MB overlap with 1GB-4GB</remarks>
            public ushort RankDensity {
                get => (ushort)(RawData[31] * 4);
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
            /// Byte 41: SDRAM Device Minimum Activate to Activate/Refresh Time (tRC)
            /// </summary>
            public Timing tRC {
                get => new Timing {
                    Whole = RawData[41],
                };
            }

            /// <summary>
            /// Byte 42: SDRAM Device Minimum Refresh to Activate/Refresh Command Period (tRFC)
            /// </summary>
            public Timing tRFC {
                get => new Timing {
                    Whole = RawData[42],
                };
            }

            /// <summary>
            /// Byte 43: SDRAM Device Maximum Device Cycle Time (tCK max)
            /// </summary>
            public Timing tCKmax {
                get => new Timing {
                    Whole   = Data.SubByte(RawData[43], 7, 6),
                    Quarter = Data.SubByte(RawData[43], 1, 2)
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
                    Tenth     = Data.SubByte(RawData[45], 7, 4),
                    Hundredth = Data.SubByte(RawData[45], 3, 4)
                };
            }

            /// <summary>
            /// Byte 47: SDRAM Device Attributes - DDR SDRAM DIMM Height
            /// </summary>
            public ModuleHeightData Height {
                get {
                    float[] heights = { 1.125F, 1.25F, 1.7F };

                    switch (Data.SubByte(RawData[47], 1, 2)) {
                        case 1:
                            return new ModuleHeightData {
                                Minimum = heights[0],
                                Maximum = heights[1],
                                Unit    = HeightUnit.IN
                            };
                        case 2:
                            return new ModuleHeightData {
                                Minimum = heights[2],
                                Maximum = heights[2],
                                Unit    = HeightUnit.IN
                            };
                        default:
                            // No DIMM height available
                            return new ModuleHeightData();
                    }
                }
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

                return Crc.Validate();
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
        }
    }
}