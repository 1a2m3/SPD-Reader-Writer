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
        /// SDRAM SPD
        /// </summary>
        public struct SDRAM : ISpd {
            /// <summary>
            /// New instance of SDRAM SPD class
            /// </summary>
            /// <param name="input">Raw SPD data</param>
            public SDRAM(byte[] input) {
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
            /// Byte 0 - Number of Bytes used by Module Manufacturer (General)
            /// Byte 1 - Total SPD Memory Size (General)
            /// </summary>
            public BytesData Bytes {
                get => new BytesData {
                    Used  = RawData[0],
                    Total = (ushort)(1 << RawData[1])
                };
            }

            public int SpdBytesUsed => Bytes.Used;

            /// <summary>
            /// Byte 2 - Memory Type (General)
            /// </summary>
            public RamType DramDeviceType {
                get => (RamType)RawData[2];
            }

            /// <summary>
            /// Byte 3 - Number of Row Address Bits (SDRAM specific)
            /// BYTE 4 - Number of Column Address Bits (SDRAM specific)
            /// </summary>
            public AddressingData Addressing {
                get => new AddressingData {
                    Rows    = RawData[3],
                    Columns = RawData[4]
                };
            }

            /// <summary>
            /// BYTE 5 - Number of Module Ranks
            /// </summary>
            public byte ModuleRanks {
                get => RawData[5];
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
            /// The total calculated memory capacity of the DRAM on the module in bytes
            /// </summary>
            public ulong TotalModuleCapacity {
                get => (ulong)(
                    (1L << Addressing.Rows) *
                    (1L << Addressing.Columns) *
                    DeviceBanks *
                    (DataWidth & 0xF0) *
                    ModuleRanks / 8);
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
                public int Whole;      // x1
                public int Tenth;      // x0.1
                public int Hundredth;  // x0.01
                public int Quarter;    // x0.25

                /// <summary>
                /// Converts timing value to nanoseconds
                /// </summary>
                /// <returns>Delay in nanoseconds</returns>
                public float ToNanoSeconds() {
                    return Whole +
                           Tenth * 0.1F +
                           Quarter * 0.25F +
                           Hundredth * 0.01F;
                }

                /// <summary>
                /// Converts timing value to clock cycles
                /// </summary>
                /// <param name="refTiming">Reference timing</param>
                /// <returns>Number of clock cycles</returns>
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
                    Whole = Data.SubByte(RawData[10], 7, 4),
                    Tenth = Data.SubByte(RawData[10], 3, 4)
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
                    SelfRefresh = Data.GetBit(RawData[12], 7)
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
                        attributes[i].Length = (byte)(1 << i);
                        attributes[i].Supported = Data.GetBit(RawData[16], i);
                    }

                    return attributes;
                }
            }

            /// <summary>
            /// Byte 17: SDRAM Device Attributes – Number of Banks on SDRAM Device
            /// </summary>
            public byte DeviceBanks {
                get => RawData[17];
            }

            /// <summary>
            /// Describes which of the programmable CAS latencies are supported
            /// </summary>
            public struct CasLatenciesData {
                public byte Bitmask;

                public int[] ToArray() {
                    Queue<int> latencies = new Queue<int>();
                    for (byte i = 0; i <= 6; i++) {
                        if (Data.GetBit(Bitmask, i)) {
                            latencies.Enqueue(i + 1);
                        }
                    }

                    return latencies.ToArray();
                }

                public override string ToString() {

                    string latenciesString = "";
                    foreach (int latency in ToArray()) {
                        latenciesString += $"{latency},";
                    }

                    return latenciesString.TrimEnd(',');
                }
            }

            /// <summary>
            /// Byte 18: SDRAM Device Attributes, CAS Latency
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
                public bool RedundantRowAddress;
                public bool DifferentialClockInput;
                public bool RegisteredDQMBInputs;
                public bool BufferedDQMBInputs;
                public bool OnCardPLL;
                public bool RegisteredAddressControlInputs;
                public bool BufferedAddressControlInputs;
            }

            /// <summary>
            /// Byte 21: SDRAM Modules Attributes
            /// </summary>
            public ModulesAttributesData ModulesAttributes {
                get => new ModulesAttributesData {
                    RedundantRowAddress            = Data.GetBit(RawData[21], 6),
                    DifferentialClockInput         = Data.GetBit(RawData[21], 5),
                    RegisteredDQMBInputs           = Data.GetBit(RawData[21], 4),
                    BufferedDQMBInputs             = Data.GetBit(RawData[21], 3),
                    OnCardPLL                      = Data.GetBit(RawData[21], 2),
                    RegisteredAddressControlInputs = Data.GetBit(RawData[21], 1),
                    BufferedAddressControlInputs   = Data.GetBit(RawData[21], 0)
                };
            }

            /// <summary>
            /// Defines various aspects of the SDRAMs on the module
            /// </summary>
            public struct DeviceAttributesData {
                public bool UpperVccTolerance;
                public bool LowerVccTolerance;
                public bool Write1ReadBurst;
                public bool PrechargeAll;
                public bool AutoPrecharge;
                public bool EarlyRasPrecharge;
            }

            /// <summary>
            /// Byte 22: SDRAM Device Attributes – General
            /// </summary>
            public DeviceAttributesData DeviceAttributes {
                get => new DeviceAttributesData {
                    UpperVccTolerance = Data.GetBit(RawData[22], 5),
                    LowerVccTolerance = Data.GetBit(RawData[22], 4),
                    Write1ReadBurst   = Data.GetBit(RawData[22], 3),
                    PrechargeAll      = Data.GetBit(RawData[22], 2),
                    AutoPrecharge     = Data.GetBit(RawData[22], 1),
                    EarlyRasPrecharge = Data.GetBit(RawData[22], 0)
                };
            }

            /// <summary>
            /// Byte 23: SDRAM Cycle time (2nd highest CAS latency)
            /// </summary>
            public Timing tCKminX1 {
                get => new Timing {
                    Whole = Data.SubByte(RawData[23], 7, 4),
                    Tenth = Data.SubByte(RawData[23], 3, 4)
                };
            }

            /// <summary>
            /// Byte 24: SDRAM Access from Clock (2nd highest CAS latency)
            /// </summary>
            public Timing tACmaxX1 {
                get => new Timing {
                    Whole = Data.SubByte(RawData[24], 7, 4),
                    Tenth = Data.SubByte(RawData[24], 3, 4)
                };
            }

            /// <summary>
            /// Byte 25: SDRAM Cycle time (3rd highest CAS latency)
            /// </summary>
            public Timing tCKminX2 {
                get => new Timing {
                    Whole   = Data.SubByte(RawData[25], 7, 6),
                    Quarter = Data.SubByte(RawData[25], 1, 2)
                };
            }

            /// <summary>
            /// Byte 26: Maximum Data Access Time (tAC) from Clock at CL X-1
            /// </summary>
            public Timing tACmaxX2 {
                get => new Timing {
                    Whole   = Data.SubByte(RawData[26], 7, 6),
                    Quarter = Data.SubByte(RawData[26], 1, 2)
                };
            }

            /// <summary>
            /// Byte 27: Minimum Row Precharge Time (tRP)
            /// </summary>
            public Timing tRP {
                get => new Timing {
                    Whole = RawData[27]
                };
            }

            /// <summary>
            /// Byte 28: Minimum Row Active to Row Active Delay (tRRD)
            /// </summary>
            public Timing tRRD {
                get => new Timing {
                    Whole = RawData[28]
                };
            }

            /// <summary>
            /// Byte 29: Minimum RAS to CAS Delay (tRCD)
            /// </summary>
            public Timing tRCD {
                get => new Timing {
                    Whole = (sbyte)RawData[29]
                };
            }

            /// <summary>
            /// Byte 30: Minimum Active to Precharge Time (tRAS)
            /// </summary>
            public Timing tRAS {
                get => new Timing {
                    Whole = (sbyte)RawData[30]
                };
            }

            /// <summary>
            /// BYTE 31 - Density of Each Row on Module
            /// </summary>
            public uint RowDensity {
                get {
                    for (byte i = 7; i != 0; i--) {
                        if (Data.GetBit(RawData[31], i)) {
                            return (uint)(4 << i);
                        }
                    }
                    return (uint)(RawData[31] * 4);
                }
            }

            /// <summary>
            /// BYTE 32 - Command and Address signal input setup time
            /// </summary>
            public Timing tIS {
                get => new Timing {
                    Whole = Data.SubByte(RawData[32], 6, 3) * (Data.GetBit(RawData[32], 7) ? (-1) : 1),
                    Tenth = Data.SubByte(RawData[32], 3, 4)
                };
            }

            /// <summary>
            /// BYTE 33 - Command and Address signal input hold time
            /// </summary>
            public Timing tIH {
                get => new Timing {
                    Whole = Data.SubByte(RawData[33], 6, 3) * (Data.GetBit(RawData[33], 7) ? (-1) : 1),
                    Tenth = Data.SubByte(RawData[33], 3, 4)
                };
            }

            /// <summary>
            /// BYTE 34 - Data signal input setup time
            /// </summary>
            public Timing tDS {
                get => new Timing {
                    Whole = Data.SubByte(RawData[34], 6, 3) * (Data.GetBit(RawData[34], 7) ? (-1) : 1),
                    Tenth = Data.SubByte(RawData[34], 3, 4)
                };
            }

            /// <summary>
            /// BYTE 35 - Data signal input hold time
            /// </summary>
            public Timing tDH {
                get => new Timing {
                    Whole = Data.SubByte(RawData[35], 6, 3) * (Data.GetBit(RawData[35], 7) ? (-1) : 1),
                    Tenth = Data.SubByte(RawData[35], 3, 4)
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
                    Week = RawData[94]
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