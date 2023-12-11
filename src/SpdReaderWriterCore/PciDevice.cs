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

namespace SpdReaderWriterCore {

    /// <summary>
    /// PCI Device class
    /// </summary>
    public class PciDevice {

        /// <summary>
        /// Initializes default PciDevice instance
        /// </summary>
        public PciDevice() {
            Location.Bus      = 0;
            Location.Device   = 0;
            Location.Function = 0;
        }

        /// <summary>
        /// Initializes PciDevice instance based on its PCI location
        /// </summary>
        /// <param name="bus">PCI bus number</param>
        /// <param name="device">PCI device number</param>
        /// <param name="function">PCI function number</param>
        public PciDevice(byte bus, byte device, byte function) {
            Location.Bus      = bus;
            Location.Device   = device;
            Location.Function = function;
        }

        /// <summary>
        /// Invalid PCI device instance
        /// </summary>
        public static PciDevice InvalidDevice => new PciDevice(byte.MaxValue, byte.MaxValue, byte.MaxValue);

        /// <summary>
        /// PCI device location info
        /// </summary>
        public LocationInfo Location;

        /// <summary>
        /// PCI device bus number
        /// </summary>
        public byte Bus {
            get => Location.Bus;
            set => Location.Bus = value;
        }

        /// <summary>
        /// PCI device device number
        /// </summary>
        public byte Device {
            get => Location.Device;
            set => Location.Device = value;
        }

        /// <summary>
        /// PCI device function number
        /// </summary>
        public byte Function {
            get => Location.Function;
            set => Location.Function = value;
        }

        /// <summary>
        /// PCI device instance location
        /// </summary>
        /// <returns>Readable PCI device instance location</returns>
        public override string ToString() {
            return $"{Location.Bus:D}/{Location.Device:D}/{Location.Function:D}";
        }

        /// <summary>
        /// PCI device's vendor ID
        /// </summary>
        public VendorId VendorId => Read<VendorId>(Register.VendorId);

        /// <summary>
        /// PCI device's device ID
        /// </summary>
        public DeviceId DeviceId => Read<DeviceId>(Register.DeviceId);

        /// <summary>
        /// PCI device's Revision ID
        /// </summary>
        public ushort RevisionId => Read<byte>(Register.RevisionId);

        /// <summary>
        /// PCI device's BaseClass
        /// </summary>
        public BaseClassType BaseClass => Read<BaseClassType>(Register.BaseClass);

        /// <summary>
        /// PCI device's SubClass
        /// </summary>
        public SubClassType SubClass => Read<SubClassType>(Register.SubClass);

        /// <summary>
        /// PCI device's program interface
        /// </summary>
        public byte ProgramInterface => Read<byte>(Register.ProgramInterface);

        /// <summary>
        /// Reads data from PCI device configuration space
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register offset</param>
        /// <returns>Data value at <paramref name="offset"/> location</returns>
        public T Read<T>(ushort offset) {
            Read(offset, out T output);
            return output;
        }

        /// <summary>
        /// Reads data from PCI device configuration space
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register offset</param>
        /// <param name="output">Register value</param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public bool Read<T>(ushort offset, out T output) => 
            KernelDriver.ReadPciConfigEx(Bus, Device, Function, offset, out output);

        /// <summary>
        /// Write data to PCI device configuration space
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Data value</param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public bool Write<T>(ushort offset, T value) => 
            KernelDriver.WritePciConfigEx(Bus, Device, Function, offset, value);

        /// <summary>
        /// Finds PCI device matching Vendor ID and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <returns>PCI Device Address matching input <paramref name="vendorId">Vendor ID</paramref> and <paramref name="deviceId">Device ID</paramref></returns>
        public static PciDevice FindDeviceById(VendorId vendorId, DeviceId deviceId) => 
            FindDeviceById(vendorId, deviceId, 0);

        /// <summary>
        /// Finds PCI device matching Vendor ID and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <param name="index">Device index to find</param>
        /// <returns>PCI Device Address matching input <paramref name="vendorId">Vendor ID</paramref> and <paramref name="deviceId">Device ID</paramref></returns>
        public static PciDevice FindDeviceById(VendorId vendorId, DeviceId deviceId, ushort index) {

            if (index > _maxPciBus * _maxPciDevice * _maxPciFunction) {
                throw new ArgumentOutOfRangeException();
            }

            if (vendorId == ushort.MinValue || vendorId == (VendorId)ushort.MaxValue) {
                return InvalidDevice;
            }

            PciDevice[] device = FindPciDeviceById(vendorId, deviceId);

            return device.Length >= index + 1 ? device[index] : InvalidDevice;
        }

        /// <summary>
        /// Finds PCI devices matching Vendor ID and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <returns>An array of PCI Device Addresses matching input <paramref name="vendorId">Vendor ID</paramref> and <paramref name="deviceId">Device ID</paramref></returns>
        public static PciDevice[] FindPciDeviceById(VendorId vendorId, DeviceId deviceId) => 
            FindPciDeviceById(vendorId, deviceId, _maxPciBus * _maxPciDevice * _maxPciFunction);

        /// <summary>
        /// Finds PCI devices matching Vendor ID and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <param name="maxCount">Maximum number of devices to find</param>
        /// <returns>An array of PCI Device Addresses matching input <paramref name="vendorId">Vendor ID</paramref> and <paramref name="deviceId">Device ID</paramref></returns>
        public static PciDevice[] FindPciDeviceById(VendorId vendorId, DeviceId deviceId, int maxCount) {

            if (maxCount > _maxPciBus * _maxPciDevice * _maxPciFunction || maxCount == 0) {
                throw new ArgumentOutOfRangeException();
            }

            uint count = 0;

            if (vendorId == default || deviceId == default) {
                return new PciDevice[0];
            }

            Queue<PciDevice> result = new Queue<PciDevice>();
            
            for (short bus = 0; bus < _maxPciBus; bus++) {
                for (byte dev = 0; dev < _maxPciDevice; dev++) {

                    DeviceId devId = KernelDriver.ReadPciConfig<DeviceId>((byte)bus, dev, 0, Register.DeviceId);

                    if (devId == ushort.MinValue || devId == DeviceId.Invalid) {
                        continue;
                    }

                    for (byte func = 0; func < _maxPciFunction; func++) {

                        uint header = KernelDriver.ReadPciConfig<uint>((byte)bus, dev, func, 0);

                        if (header != ((ushort)vendorId | (ushort)deviceId << 16)) {
                            continue;
                        }

                        result.Enqueue(new PciDevice((byte)bus, dev, func));

                        if (++count == maxCount) {
                            return result.ToArray();
                        }
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Finds PCI device by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <param name="programIf">Program Interface</param>
        /// <returns>PCI Device Address matching input <paramref name="baseClass"/>, <paramref name="subClass"/>, and <paramref name="programIf"/></returns>
        public static PciDevice FindPciDeviceByClass(BaseClassType baseClass, SubClassType subClass, ProgramIf programIf) => 
            FindPciDeviceByClass(baseClass, subClass, programIf, 0);

        /// <summary>
        /// Finds PCI device by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <param name="programIf">Program Interface</param>
        /// <param name="index">Device index to find</param>
        /// <returns>PCI Device Address matching input <paramref name="baseClass"/>, <paramref name="subClass"/>, and <paramref name="programIf"/></returns>
        public static PciDevice FindPciDeviceByClass(BaseClassType baseClass, SubClassType subClass, ProgramIf programIf, ushort index) {

            if (index > _maxPciBus * _maxPciDevice * _maxPciFunction) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            PciDevice[] device = FindDeviceByClass(baseClass, subClass, programIf);

            return device.Length >= index + 1 ? device[index] : InvalidDevice;
        }

        /// <summary>
        /// Finds PCI devices by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <returns>An array of PCI Device Addresses matching input <paramref name="baseClass"/> and <paramref name="subClass"/></returns>
        public static PciDevice[] FindDeviceByClass(BaseClassType baseClass, SubClassType subClass) => 
            FindDeviceByClass(baseClass, subClass, 0);

        /// <summary>
        /// Finds PCI devices by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <param name="programIf">Program Interface</param>
        /// <returns>An array of PCI Device Addresses matching input <paramref name="baseClass"/>, <paramref name="subClass"/>, and <paramref name="programIf"/></returns>
        public static PciDevice[] FindDeviceByClass(BaseClassType baseClass, SubClassType subClass, ProgramIf programIf) => 
            FindDeviceByClass(baseClass, subClass, programIf, _maxPciBus * _maxPciDevice * _maxPciFunction);

        /// <summary>
        /// Finds PCI devices by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <param name="programIf">Program Interface</param>
        /// <param name="maxCount">Maximum number of devices to find</param>
        /// <returns>An array of PCI Device Addresses matching input <paramref name="baseClass"/>, <paramref name="subClass"/>, and <paramref name="programIf"/></returns>
        public static PciDevice[] FindDeviceByClass(BaseClassType baseClass, SubClassType subClass, ProgramIf programIf, int maxCount) {

            if (maxCount > _maxPciBus * _maxPciDevice * _maxPciFunction) {
                throw new ArgumentOutOfRangeException(nameof(maxCount));
            }

            if (maxCount == 0) {
                return new PciDevice[0];
            }

            uint count = 0;

            Queue<PciDevice> result = new Queue<PciDevice>();

            for (short bus = 0; bus < _maxPciBus; bus++) {
                for (byte dev = 0; dev < _maxPciDevice; dev++) {

                    ushort devId = KernelDriver.ReadPciConfig<ushort>((byte)bus, dev, 0, 0x00);

                    if (devId == ushort.MinValue || devId == ushort.MaxValue) {
                        continue;
                    }

                    for (byte func = 0; func < _maxPciFunction; func++) {

                        if ((KernelDriver.ReadPciConfig<uint>((byte)bus, dev, func, 0x08) & 0xFFFFFF00) !=
                            (uint)((byte)baseClass << 24 | ((byte)subClass << 16) | (byte)programIf << 8)) {
                            continue;
                        }

                        result.Enqueue(new PciDevice((byte)bus, dev, func));

                        if (++count == maxCount) {
                            return result.ToArray();
                        }
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Sets the maximum PCI bus index to scan by <see cref="FindPciDeviceById(SpdReaderWriterCore.VendorId,SpdReaderWriterCore.DeviceId,int)"/>
        /// and <see cref="FindDeviceByClass(BaseClassType,SubClassType,ProgramIf,int)"/>
        /// </summary>
        /// <param name="max">Maximum PCI bus index to scan</param>
        public static void SetPciMaxBusIndex(ushort max) => _maxPciBus = max;

        /// <summary>
        /// PCI device location
        /// </summary>
        public struct LocationInfo {
            public byte Bus;
            public byte Device;
            public byte Function;
        }

        /// <summary>
        /// PCI Device info
        /// </summary>
        public struct DeviceInfo {
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
        public enum BaseClassType {
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
        /// PCI sub class codes
        /// </summary>
        public enum SubClassType {
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
        /// Maximum number of PCI buses assigned by <see cref="SetPciMaxBusIndex"/>
        /// </summary>
        private static ushort _maxPciBus = 256;

        /// <summary>
        /// Maximum number of PCI devices per bus
        /// </summary>
        private static byte _maxPciDevice = 32;

        /// <summary>
        /// Maximum number of PCI functions per device
        /// </summary>
        private static byte _maxPciFunction = 8;
    }
}