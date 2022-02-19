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
        public struct PciBaseClass {
            public const byte MEMORY = 0x05;
            public const byte BRIDGE = 0x06;
            public const byte SERIAL = 0x0C;
        }

        /// <summary>
        /// PCI sub class codes
        /// </summary>
        public struct PciSubClass {
            public const byte ISA    = 0x01;
            public const byte SMBUS  = 0x05;
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
            pciInfo._pciBusNumber      = Smbus.Driver.PciGetBus(memoryAddress);
            pciInfo._pciDeviceNumber   = Smbus.Driver.PciGetDev(memoryAddress);
            pciInfo._pciFunctionNumber = Smbus.Driver.PciGetFunc(memoryAddress);
        }

        /// <summary>
        /// Initializes PciDevice instance based on its PCI location
        /// </summary>
        /// <param name="pciBusNumber">PCI bus number</param>
        /// <param name="pciDeviceNumber">PCI device number</param>
        /// <param name="pciFunctionNumber">PCI function number</param>
        public PciDevice(byte pciBusNumber, byte pciDeviceNumber, byte pciFunctionNumber) {
            pciInfo._pciBusNumber      = pciBusNumber;
            pciInfo._pciDeviceNumber   = pciDeviceNumber;
            pciInfo._pciFunctionNumber = pciFunctionNumber;
        }

        /// <summary>
        /// PCI device instance string
        /// </summary>
        /// <returns>Readable PCI device instance string</returns>
        public override string ToString() {
            return $"PCI{pciInfo._pciBusNumber:D}/{pciInfo._pciDeviceNumber:D}/{pciInfo._pciFunctionNumber:D}";
        }

        /// <summary>
        /// Finds PCI devices by Vendor and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <returns>An array of PCI devices locations matching <paramref name="vendorId">Vendor ID</paramref> and <paramref name="deviceId">Device ID</paramref> </returns>
        public static UInt32 FindDeviceById(UInt16 vendorId, UInt16 deviceId) {

            try {
                UInt32[] _pciDevice = new PciDevice().FindDeviceById(vendorId, deviceId, 1);

                if (_pciDevice.Length > 0 && _pciDevice[0] != UInt16.MaxValue) {
                    return _pciDevice[0];
                }

                return 0;
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
        /// <returns>An array of PCI devices locations matching <paramref name="vendorId">Vendor ID</paramref> and <paramref name="deviceId">Device ID</paramref> </returns>
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

            UInt32[] _pciDevice = new PciDevice().FindDeviceByClass(baseClass, subClass, programIf, 1);
            if (_pciDevice.Length > 0 && _pciDevice[0] != UInt16.MaxValue) {
                return _pciDevice[0];
            }

            throw new IOException("PCI device not found");
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
            return ReadWord(0x00);
        }

        /// <summary>
        /// Returns PCI device's ID
        /// </summary>
        /// <returns>Device ID</returns>
        public UInt16 GetDeviceId() {
            return ReadWord(0x02);
        }

        /// <summary>
        /// Read byte from PCI device memory space
        /// </summary>
        /// <param name="offset">Byte location</param>
        /// <returns>Byte value at <paramref name="offset"/> location</returns>
        public UInt8 ReadByte(byte offset) {
            return Smbus.Driver.ReadPciConfigByte(pciInfo.pciDeviceMemoryLocation, offset);
        }

        /// <summary>
        /// Read word from PCI device memory space
        /// </summary>
        /// <param name="offset">Word location</param>
        /// <returns>Word value at <paramref name="offset"/> location</returns>
        public UInt16 ReadWord(byte offset) {
            return Smbus.Driver.ReadPciConfigWord(pciInfo.pciDeviceMemoryLocation, offset);
        }

        /// <summary>
        /// Read Dword from PCI device memory space
        /// </summary>
        /// <param name="offset">Dword location</param>
        /// <returns>Dword value at <paramref name="offset"/> location</returns>
        public UInt32 ReadDword(byte offset) {
            return Smbus.Driver.ReadPciConfigDword(pciInfo.pciDeviceMemoryLocation, offset);
        }

        /// <summary>
        /// Write byte to PCI device memory space
        /// </summary>
        /// <param name="offset">Byte location</param>
        /// <param name="value">Byte value</param>
        public void WriteByte(byte offset, UInt8 value) {
            Smbus.Driver.WritePciConfigByte(pciInfo.pciDeviceMemoryLocation, offset, value);
        }

        /// <summary>
        /// Write word PCI device memory space
        /// </summary>
        /// <param name="offset">Word location</param>
        /// <param name="value">Word value</param>
        public void WriteWord(byte offset, UInt16 value) {
            Smbus.Driver.WritePciConfigWord(pciInfo.pciDeviceMemoryLocation, offset, value);
        }

        /// <summary>
        /// Write Dword to PCI device memory space
        /// </summary>
        /// <param name="offset">Dword location</param>
        /// <param name="value">Dword value</param>
        public void WriteDword(byte offset, UInt32 value) {
            Smbus.Driver.WritePciConfigDword(pciInfo.pciDeviceMemoryLocation, offset, value);
        }

        /// <summary>
        /// PCI device information
        /// </summary>
        private struct pciInfo {
            // PCI device info
            public static uint _pciBusNumber;
            public static uint _pciDeviceNumber;
            public static uint _pciFunctionNumber;

            // PCI device memory location
            public static uint pciDeviceMemoryLocation {
                get {
                    return Smbus.Driver.PciBusDevFunc(_pciBusNumber, _pciDeviceNumber, _pciFunctionNumber);
                }
            }
        }
    }
}
