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
using System.Text;
using System.Threading;
using static SpdReaderWriterCore.Data;

namespace SpdReaderWriterCore {

    /// <summary>
    /// PCI configuration space class
    /// </summary>
    public class PciConfig {

        /// <summary>
        /// Initializes default PCI Config instance
        /// </summary>
        public PciConfig() {
            Location.Bus      = 0;
            Location.Device   = 0;
            Location.Function = 0;
        }

        /// <summary>
        /// Initializes a new PCI Config instance based on its PCI location
        /// </summary>
        /// <param name="bus">PCI bus number</param>
        /// <param name="device">PCI configuration space device number</param>
        /// <param name="function">PCI configuration space function number</param>
        public PciConfig(byte bus, byte device, byte function) {
            Location.Bus      = bus;
            Location.Device   = device;
            Location.Function = function;
        }

        /// <summary>
        /// Copies existing PCI Config instance to a new PCI Config instance
        /// </summary>
        /// <param name="pciConfig">Source PCI Config instance</param>
        public PciConfig(PciConfig pciConfig) {
            Location.Bus      = pciConfig.Bus;
            Location.Device   = pciConfig.Device;
            Location.Function = pciConfig.Function;
        }

        /// <summary>
        /// PCI Config instance location info
        /// </summary>
        public LocationInfo Location;

        /// <summary>
        /// PCI Config instance bus number
        /// </summary>
        public byte Bus {
            get => Location.Bus;
            set => Location.Bus = value;
        }

        /// <summary>
        /// PCI Config instance device number
        /// </summary>
        public byte Device {
            get => Location.Device;
            set => Location.Device = value;
        }

        /// <summary>
        /// PCI device instance function number
        /// </summary>
        public byte Function {
            get => Location.Function;
            set => Location.Function = value;
        }

        /// <summary>
        /// Sets PCI device instance new bus number
        /// </summary>
        /// <param name="bus">PCI device bus number</param>
        /// <returns>PCI device instance with new <paramref name="bus"/> number</returns>
        public PciConfig SetBus(byte bus) {
            Bus = bus;
            return this;
        }

        /// <summary>
        /// Sets PCI device instance new device number
        /// </summary>
        /// <param name="device">PCI device device number</param>
        /// <returns>PCI device instance with new <paramref name="device"/> number</returns>
        public PciConfig SetDevice(byte device) {
            Device = device;
            return this;
        }

        /// <summary>
        /// Sets PCI device instance new function number
        /// </summary>
        /// <param name="function">PCI device function number</param>
        /// <returns>PCI device instance with new <paramref name="function"/> number</returns>
        public PciConfig SetFunction(byte function) {
            Function = function;
            return this;
        }

        /// <summary>
        /// PCI config instance description
        /// </summary>
        /// <returns>Readable PCI config instance description</returns>
        public override string ToString() => Location.ToString();

        /// <summary>
        /// PCI device's vendor ID
        /// </summary>
        public VendorId VendorId => (VendorId)Read<ushort>(Register.VendorId);

        /// <summary>
        /// PCI device's device ID
        /// </summary>
        public DeviceId DeviceId => (DeviceId)Read<ushort>(Register.DeviceId);

        /// <summary>
        /// PCI device's Revision ID
        /// </summary>
        public byte RevisionId => Read<byte>(Register.RevisionId);

        /// <summary>
        /// PCI device's BaseClass
        /// </summary>
        public BaseClassType BaseClass => (BaseClassType)Read<byte>(Register.BaseClass);

        /// <summary>
        /// PCI device's SubClass
        /// </summary>
        public SubClassType SubClass => (SubClassType)Read<byte>(Register.SubClass);

        /// <summary>
        /// PCI device's program interface
        /// </summary>
        public ProgramIf ProgInterface => (ProgramIf)Read<byte>(Register.ProgramInterface);

        /// <summary>
        /// Reads data from PCI configuration space
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register offset</param>
        /// <returns>Data value at <paramref name="offset"/> location</returns>
        public T Read<T>(ushort offset) {

            if (!LockMutex(PciMutex, PciMutexTimeout)) {
                return default;
            }

            ReadEx(offset, out T output);
            UnlockMutex(PciMutex);

            return output;
        }

        /// <summary>
        /// Reads data from PCI configuration space
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register offset</param>
        /// <param name="output">Register value</param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public bool ReadEx<T>(ushort offset, out T output) {

            output = default;

            if (!LockMutex(PciMutex, PciMutexTimeout)) {
                return false;
            }

            bool result = Kernel.ReadPciConfigEx(Bus, Device, Function, offset, out output);
            UnlockMutex(PciMutex);

            return result;
        }

        /// <summary>
        /// Writes data to PCI configuration space
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Data value</param>
        public void Write<T>(ushort offset, T value) {
            if (!LockMutex(PciMutex, PciMutexTimeout)) {
                return;
            }

            WriteEx(offset, value);
            UnlockMutex(PciMutex);
        }

        /// <summary>
        /// Writes data to PCI configuration space
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Data value</param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public bool WriteEx<T>(ushort offset, T value) {

            if (!LockMutex(PciMutex, PciMutexTimeout)) {
                return false;
            }

            bool result = Kernel.WritePciConfigEx(Bus, Device, Function, offset, value);
            UnlockMutex(PciMutex);

            return result;
        }

        /// <summary>
        /// Finds PCI device matching Vendor ID and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <returns>PCI Device matching input <paramref name="vendorId">Vendor ID</paramref>
        /// and <paramref name="deviceId">Device ID</paramref></returns>
        public static PciConfig FindDeviceById(VendorId vendorId, DeviceId deviceId) =>
            FindDeviceById(vendorId, deviceId, 0);

        /// <summary>
        /// Finds PCI device matching Vendor ID and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <param name="index">Device index to find</param>
        /// <returns>PCI Device matching input <paramref name="vendorId">Vendor ID</paramref>
        /// and <paramref name="deviceId">Device ID</paramref></returns>
        public static PciConfig FindDeviceById(VendorId vendorId, DeviceId deviceId, ushort index) {

            if (index > MaxPciCount) {
                throw new ArgumentOutOfRangeException();
            }

            if (vendorId == ushort.MinValue || vendorId == (VendorId)ushort.MaxValue) {
                return null;
            }

            PciConfig[] device = FindDevicesById(vendorId, deviceId, index);

            return device.Length >= index + 1 ? device[index] : null;
        }

        /// <summary>
        /// Finds PCI devices matching Vendor ID and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <returns>An array of PCI Devices matching input <paramref name="vendorId">Vendor ID</paramref>
        /// and <paramref name="deviceId">Device ID</paramref></returns>
        public static PciConfig[] FindDevicesById(VendorId vendorId, DeviceId deviceId) =>
            FindDevicesById(vendorId, deviceId, MaxPciCount);

        /// <summary>
        /// Finds PCI devices matching Vendor ID and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <param name="maxCount">Maximum number of devices to find</param>
        /// <returns>An array of PCI Devices matching input <paramref name="vendorId">Vendor ID</paramref>
        /// and <paramref name="deviceId">Device ID</paramref></returns>
        public static PciConfig[] FindDevicesById(VendorId vendorId, DeviceId deviceId, int maxCount) {

            if (maxCount > MaxPciCount || maxCount == 0) {
                throw new ArgumentOutOfRangeException(nameof(maxCount));
            }

            Queue<PciConfig> result = new Queue<PciConfig>();

            bool stopFlag = false;

            if (LockMutex(PciMutex, PciMutexTimeout)) {

                PciConfig pciConfig = new PciConfig();

                // Bus loop
                for (short bus = 0; bus <= _maxPciBusIndex && !stopFlag; bus++) {
                    // Device loop
                    for (byte dev = 0; dev <= MaxPciDeviceIndex && !stopFlag; dev++) {

                        pciConfig.SetBus((byte)bus).SetDevice(dev).SetFunction(0);

                        DeviceId devId = pciConfig.DeviceId;

                        if (devId == ushort.MinValue ||
                            devId == DeviceId.Invalid) {
                            continue;
                        }

                        // Function loop
                        for (byte func = 0; func <= MaxPciFunctionIndex; func++) {

                            pciConfig.SetFunction(func);

                            // Check header
                            if (Kernel.ReadPciConfig<uint>((byte)bus, dev, func, Register.VendorId) !=
                                (uint)((ushort)vendorId | ((ushort)deviceId << 16))) {
                                continue;
                            }

                            result.Enqueue(new PciConfig(pciConfig));

                            if (result.Count != maxCount) {
                                continue;
                            }

                            stopFlag = true;
                            break;
                        }
                    }
                }

                UnlockMutex(PciMutex);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Finds PCI device by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <returns>PCI Devices matching input <paramref name="baseClass"/> and <paramref name="subClass"/></returns>
        public static PciConfig FindDeviceByClass(BaseClassType baseClass, SubClassType subClass) =>
            FindDeviceByClass(baseClass, subClass, 0);

        /// <summary>
        /// Finds PCI device by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <param name="programIf">Program Interface</param>
        /// <returns>PCI Devices matching input <paramref name="baseClass"/>, <paramref name="subClass"/>,
        /// and <paramref name="programIf"/></returns>
        public static PciConfig FindDeviceByClass(BaseClassType baseClass, SubClassType subClass, ProgramIf programIf) =>
            FindDeviceByClass(baseClass, subClass, programIf, 0);

        /// <summary>
        /// Finds PCI device by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <param name="programIf">Program Interface</param>
        /// <param name="index">Device index to find</param>
        /// <returns>PCI Devices matching input <paramref name="baseClass"/>, <paramref name="subClass"/>,
        /// and <paramref name="programIf"/></returns>
        public static PciConfig FindDeviceByClass(BaseClassType baseClass, SubClassType subClass, ProgramIf programIf, ushort index) {

            if (index > MaxPciCount) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            PciConfig[] device = FindDevicesByClass(baseClass, subClass, programIf);

            return device.Length >= index + 1 ? device[index] : null;
        }

        /// <summary>
        /// Finds PCI devices by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <returns>An array of PCI Devices matching input <paramref name="baseClass"/> and <paramref name="subClass"/></returns>
        public static PciConfig[] FindDevicesByClass(BaseClassType baseClass, SubClassType subClass) =>
            FindDevicesByClass(baseClass, subClass, 0);

        /// <summary>
        /// Finds PCI devices by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <param name="programIf">Program Interface</param>
        /// <returns>An array of PCI Devices matching input <paramref name="baseClass"/>, <paramref name="subClass"/>,
        /// and <paramref name="programIf"/></returns>
        public static PciConfig[] FindDevicesByClass(BaseClassType baseClass, SubClassType subClass, ProgramIf programIf) =>
            FindDevicesByClass(baseClass, subClass, programIf, MaxPciCount);

        /// <summary>
        /// Finds PCI devices by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <param name="programIf">Program Interface</param>
        /// <param name="maxCount">Maximum number of devices to find</param>
        /// <returns>An array of PCI Devices matching input <paramref name="baseClass"/>, <paramref name="subClass"/>,
        /// and <paramref name="programIf"/></returns>
        public static PciConfig[] FindDevicesByClass(BaseClassType baseClass, SubClassType subClass, ProgramIf programIf, int maxCount) {

            if (maxCount > MaxPciCount || maxCount == 0) {
                throw new ArgumentOutOfRangeException(nameof(maxCount));
            }

            Queue<PciConfig> result = new Queue<PciConfig>();

            bool stopFlag = false;

            if (LockMutex(PciMutex, PciMutexTimeout)) {

                PciConfig pciConfig = new PciConfig();

                // Bus loop
                for (short bus = 0; bus <= _maxPciBusIndex && !stopFlag; bus++) {
                    // Device loop
                    for (byte dev = 0; dev <= MaxPciDeviceIndex && !stopFlag; dev++) {

                        pciConfig.SetBus((byte)bus).SetDevice(dev).SetFunction(0);

                        DeviceId devId = pciConfig.DeviceId;

                        if (devId == ushort.MinValue ||
                            devId == DeviceId.Invalid) {
                            continue;
                        }

                        for (byte func = 0; func <= MaxPciFunctionIndex; func++) {

                            pciConfig.SetFunction(func);

                            if ((Kernel.ReadPciConfig<uint>((byte)bus, dev, func, 0x08) & 0xFFFFFF00) !=
                                (uint)(((byte)baseClass << 24) | 
                                       ((byte)subClass  << 16) |
                                       ((byte)programIf <<  8))) {
                                continue;
                            }

                            result.Enqueue(new PciConfig(pciConfig));

                            if (result.Count != maxCount) {
                                continue;
                            }

                            stopFlag = true;
                            break;
                        }
                    }
                }

                UnlockMutex(PciMutex);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Sets the maximum PCI bus index to scan by <see cref="FindDevicesById(SpdReaderWriterCore.VendorId,SpdReaderWriterCore.DeviceId,int)"/>
        /// and <see cref="FindDevicesByClass(BaseClassType,SubClassType,ProgramIf,int)"/>
        /// </summary>
        /// <param name="max">Maximum PCI bus index to scan</param>
        public static void SetPciMaxBusIndex(int max) => _maxPciBusIndex = (byte)(max & 0xFF);

        /// <summary>
        /// PCI device location
        /// </summary>
        public struct LocationInfo {
            public byte Bus;
            public byte Device;
            public byte Function;

            /// <summary>
            /// PCI config instance description
            /// </summary>
            /// <returns>PCI config location</returns>
            public override string ToString() => ToString("D");

            /// <summary>
            /// PCI config instance description
            /// </summary>
            /// <param name="format">Numeric format</param>
            /// <returns>PCI config location</returns>
            public string ToString(string format) => $"{Bus.ToString(format)}/{Device.ToString(format)}/{Function.ToString(format)}";
        }

        /// <summary>
        /// PCI Device info
        /// </summary>
        public struct PciInfo {
            public VendorId VendorId;
            public DeviceId DeviceId;

            public BaseClassType BaseClass;
            public SubClassType SubClass;

            public override string ToString() {

                StringBuilder sb = new StringBuilder();

                sb.Append(Enum.IsDefined(typeof(VendorId), VendorId)
                    ? VendorId.ToString()
                    : $"0x{VendorId:X4}".ToString());

                sb.Append(" ");

                sb.Append(Enum.IsDefined(typeof(DeviceId), DeviceId)
                    ? DeviceId.ToString()
                    : $"0x{DeviceId:X4}".ToString());

                return sb.ToString();
            }
        }

        /// <summary>
        /// PCI Base Class codes
        /// </summary>
        public enum BaseClassType : byte {
            Obsolete        = 0x00,
            Storage         = 0x01,
            Network         = 0x02,
            Display         = 0x03,
            Multimedia      = 0x04,
            Memory          = 0x05,
            Bridge          = 0x06,
            Communication   = 0x07,
            System          = 0x08,
            Input           = 0x09,
            Docking         = 0x0A,
            Processor       = 0x0B,
            Serial          = 0x0C,
            Wireless        = 0x0D,
            Intelligent     = 0x0E,
            Satellite       = 0x0F,
            Encryption      = 0x10,
            Processing      = 0x11,
            Accelerator     = 0x12,
            Instrumentation = 0x13,
            Undefined       = 0xFF
        }

        /// <summary>
        /// PCI Sub Class codes
        /// </summary>
        public enum SubClassType : byte {
            Isa   = 0x01,
            Smbus = 0x05,
        }

        /// <summary>
        /// Programming interface
        /// </summary>
        public enum ProgramIf : byte { }

        /// <summary>
        /// PCI registers
        /// </summary>
        public struct Register {
            public static byte VendorId          = 0x00;
            public static byte DeviceId          = 0x02;
            public static byte Status            = 0x06;
            public static byte RevisionId        = 0x08;
            public static byte ProgramInterface  = 0x09;
            public static byte SubClass          = 0x0A;
            public static byte BaseClass         = 0x0B;
            public static byte HeaderType        = 0x0E;
            public static byte[] BaseAddress     = { 0x10, 0x14, 0x18, 0x1C, 0x20, 0x24 };
            public static byte SubsystemId       = 0x2E;
            public static byte SubsystemVendorId = 0x2C;
        }

        /// <summary>
        /// Maximum PCI <see cref="Bus">bus</see> index assigned by <see cref="SetPciMaxBusIndex"/>
        /// </summary>
        private static byte _maxPciBusIndex = 255;

        /// <summary>
        /// Maximum PCI <see cref="Device">device</see> index per <see cref="Bus">bus</see>
        /// </summary>
        internal static readonly byte MaxPciDeviceIndex = 31;

        /// <summary>
        /// Maximum PCI <see cref="Function">function</see> index per <see cref="Device">device</see>
        /// </summary>
        internal static readonly byte MaxPciFunctionIndex = 7;

        /// <summary>
        /// Absolute maximum number of PCI devices possible
        /// </summary>
        private static int MaxPciCount => (_maxPciBusIndex + 1) * 256 /*(MAX_PCI_DEVICE_INDEX + 1) * (MAX_PCI_FUNCTION_INDEX + 1)*/;

        /// <summary>
        /// Global PCI access mutex
        /// </summary>
        internal static Mutex PciMutex = CreateMutex(@"Global\Access_PCI");

        /// <summary>
        /// Global PCI access mutex timeout
        /// </summary>
        internal static int PciMutexTimeout = 5000;
    }
}