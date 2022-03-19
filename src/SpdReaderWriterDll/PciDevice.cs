using System;
using System.Collections.Generic;
using System.IO;
using UInt8 = System.Byte;

namespace SpdReaderWriterDll {

    /// <summary>
    /// PCI Device class
    /// </summary>
    public class PciDevice {

        /// <summary>
        /// PCI Base Class codes
        /// </summary>
        public struct BaseClass {
            public const byte Memory                  = 0x05;
            public const byte Bridge                  = 0x06;
            public const byte Serial                  = 0x0C;
        }

        /// <summary>
        /// PCI sub class codes
        /// </summary>
        public struct SubClass {
            public const byte Isa                     = 0x01;
            public const byte Smbus                   = 0x05;
        }

        /// <summary>
        /// PCI registers
        /// </summary>
        public struct RegisterOffset {
            public const byte VendorId                = 0x00;
            public const byte DeviceId                = 0x02;
            public const byte Status                  = 0x06;
            public static readonly byte[] BaseAddress = { 0x10, 0x14, 0x18, 0x1C, 0x20, 0x24 };
            public const byte SubsystemId             = 0x2E;
            public const byte SubsystemVendorId       = 0x2C;
        }

        /// <summary>
        /// Initializes default PciDevice instance
        /// </summary>
        public PciDevice() {
        }

        /// <summary>
        /// Initializes PciDevice instance based on its memory address location
        /// </summary>
        /// <param name="memoryAddress">PCI device memory location</param>
        public PciDevice(uint memoryAddress) {
            PciInfo.BusNumber      = Smbus.Driver.PciGetBus(memoryAddress);
            PciInfo.DeviceNumber   = Smbus.Driver.PciGetDev(memoryAddress);
            PciInfo.FunctionNumber = Smbus.Driver.PciGetFunc(memoryAddress);
        }

        /// <summary>
        /// Initializes PciDevice instance based on its PCI location
        /// </summary>
        /// <param name="busNumber">PCI bus number</param>
        /// <param name="deviceNumber">PCI device number</param>
        /// <param name="functionNumber">PCI function number</param>
        public PciDevice(byte busNumber, byte deviceNumber, byte functionNumber) {
            PciInfo.BusNumber      = busNumber;
            PciInfo.DeviceNumber   = deviceNumber;
            PciInfo.FunctionNumber = functionNumber;
        }

        /// <summary>
        /// PCI device instance string
        /// </summary>
        /// <returns>Readable PCI device instance string</returns>
        public override string ToString() {
            return $"PCI{PciInfo.BusNumber:D}/{PciInfo.DeviceNumber:D}/{PciInfo.FunctionNumber:D}";
        }

        /// <summary>
        /// Finds PCI devices by Vendor and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <returns>PCI device location matching <paramref name="vendorId">Vendor ID</paramref> and <paramref name="deviceId">Device ID</paramref></returns>
        public static UInt32 FindDeviceById(UInt16 vendorId, UInt16 deviceId) {
            try {
                UInt32[] pciDevice = new PciDevice().FindDeviceById(vendorId, deviceId, 1);

                if (pciDevice.Length > 0 && pciDevice[0] != UInt16.MaxValue) {
                    return pciDevice[0];
                }

                return UInt16.MaxValue;
            }
            catch {
                throw new IOException("PCI device not found");
            }
        }

        /// <summary>
        /// Finds PCI devices by Vendor and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <param name="maxCount">Maximum number of results</param>
        /// <returns>An array of PCI devices locations matching <paramref name="vendorId">Vendor ID</paramref> and <paramref name="deviceId">Device ID</paramref></returns>
        public UInt32[] FindDeviceById(UInt16 vendorId, UInt16 deviceId, byte maxCount) {

            Queue<UInt32> results = new Queue<UInt32>();

            for (int i = 0; i < byte.MaxValue; i++) {

                UInt32 id = Smbus.Driver.FindPciDeviceById(vendorId, deviceId, (byte)i);

                if (id != UInt32.MaxValue) {
                    results.Enqueue(id);
                }

                if (results.Count == maxCount) {
                    break;
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// Finds PCI devices by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <returns>PCI device location matching Device Class</returns>
        public static UInt32 FindDeviceByClass(byte baseClass, byte subClass) {
            return FindDeviceByClass(baseClass, subClass, 0);
        }

        /// <summary>
        /// Finds PCI devices by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <param name="programIf">Program Interface</param>
        /// <returns>PCI device location matching Device Class</returns>
        public static UInt32 FindDeviceByClass(byte baseClass, byte subClass, byte programIf) {
            try {
                UInt32[] pciDevice = new PciDevice().FindDeviceByClass(baseClass, subClass, programIf, 1);
                if (pciDevice.Length > 0 && pciDevice[0] != UInt32.MaxValue) {
                    return pciDevice[0];
                }

                return UInt32.MaxValue;
            }
            catch {
                throw new IOException("PCI device not found");
            }
        }

        /// <summary>
        /// Finds PCI devices by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <param name="programIf">Program Interface</param>
        /// <param name="maxCount">Maximum number of results</param>
        /// <returns>An array of PCI devices locations matching Device Class</returns>
        public UInt32[] FindDeviceByClass(byte baseClass, byte subClass, byte programIf, byte maxCount) {

            Queue<UInt32> results = new Queue<UInt32>();

            for (int i = 0; i < byte.MaxValue; i++) {

                UInt32 id = Smbus.Driver.FindPciDeviceByClass(baseClass, subClass, programIf, (byte)i);

                if (id != UInt32.MaxValue) {
                    results.Enqueue(id);
                }

                if (results.Count == maxCount) {
                    break;
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// Returns PCI device's vendor ID
        /// </summary>
        /// <returns>Vendor ID</returns>
        public UInt16 GetVendorId() {
            return ReadWord(RegisterOffset.VendorId);
        }

        /// <summary>
        /// Returns PCI device's ID
        /// </summary>
        /// <returns>Device ID</returns>
        public UInt16 GetDeviceId() {
            return ReadWord(RegisterOffset.DeviceId);
        }

        /// <summary>
        /// Read byte from PCI device memory space
        /// </summary>
        /// <param name="offset">Byte location</param>
        /// <returns>Byte value at <paramref name="offset" /> location</returns>
        public UInt8 ReadByte(byte offset) {
            return Smbus.Driver.ReadPciConfigByte(PciInfo.DeviceMemoryLocation, offset);
        }

        /// <summary>
        /// Read word from PCI device memory space
        /// </summary>
        /// <param name="offset">Word location</param>
        /// <returns>Word value at <paramref name="offset" /> location</returns>
        public UInt16 ReadWord(byte offset) {
            return Smbus.Driver.ReadPciConfigWord(PciInfo.DeviceMemoryLocation, offset);
        }

        /// <summary>
        /// Read Dword from PCI device memory space
        /// </summary>
        /// <param name="offset">Dword location</param>
        /// <returns>Dword value at <paramref name="offset" /> location</returns>
        public UInt32 ReadDword(byte offset) {
            return Smbus.Driver.ReadPciConfigDword(PciInfo.DeviceMemoryLocation, offset);
        }

        /// <summary>
        /// Write byte to PCI device memory space
        /// </summary>
        /// <param name="offset">Byte location</param>
        /// <param name="value">Byte value</param>
        public void WriteByte(byte offset, UInt8 value) {
            Smbus.Driver.WritePciConfigByte(PciInfo.DeviceMemoryLocation, offset, value);
        }

        /// <summary>
        /// Write word to PCI device memory space
        /// </summary>
        /// <param name="offset">Word location</param>
        /// <param name="value">Word value</param>
        public void WriteWord(byte offset, UInt16 value) {
            Smbus.Driver.WritePciConfigWord(PciInfo.DeviceMemoryLocation, offset, value);
        }

        /// <summary>
        /// Write Dword to PCI device memory space
        /// </summary>
        /// <param name="offset">Dword location</param>
        /// <param name="value">Dword value</param>
        public void WriteDword(byte offset, UInt32 value) {
            Smbus.Driver.WritePciConfigDword(PciInfo.DeviceMemoryLocation, offset, value);
        }

        /// <summary>
        /// PCI device information
        /// </summary>
        private struct PciInfo {
            // PCI device info
            public static uint BusNumber;
            public static uint DeviceNumber;
            public static uint FunctionNumber;

            // PCI device memory location
            public static uint DeviceMemoryLocation => Smbus.Driver.PciBusDevFunc(BusNumber, DeviceNumber, FunctionNumber);
        }
    }
}
