/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using System;
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
            public const byte I3C                     = 0x0A; // MIPI I3C Host Controller Interface
        }

        /// <summary>
        /// PCI registers
        /// </summary>
        public struct RegisterOffset {
            public const byte VendorId                = 0x00;
            public const byte DeviceId                = 0x02;
            public const byte Status                  = 0x06;
            public const byte RevisionId              = 0x08;
            public const byte SubType                 = 0x0A;
            public const byte BaseType                = 0x0B;
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
        public PciDevice(UInt32 memoryAddress) {
            PciInfo.BusNumber      = WinRing0.PciGetBus(memoryAddress);
            PciInfo.DeviceNumber   = WinRing0.PciGetDev(memoryAddress);
            PciInfo.FunctionNumber = WinRing0.PciGetFunc(memoryAddress);
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
        /// PCI device instance location
        /// </summary>
        /// <returns>Readable PCI device instance location</returns>
        public override string ToString() {
            return $"PCI {PciInfo.BusNumber:D}/{PciInfo.DeviceNumber:D}/{PciInfo.FunctionNumber:D}";
        }

        /// <summary>
        /// Finds PCI device by Vendor and Device IDs
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <returns>PCI device location matching <paramref name="vendorId">Vendor ID</paramref> and <paramref name="deviceId">Device ID</paramref></returns>
        public static UInt32 FindDeviceById(UInt16 vendorId, UInt16 deviceId) {
            try {
                UInt32[] pciDevice = Smbus._driver.FindPciDeviceByIdArray(vendorId, deviceId, 1);

                if (pciDevice.Length > 0 && pciDevice[0] != UInt32.MaxValue) {
                    return pciDevice[0];
                }

                return UInt16.MaxValue;
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
                UInt32[] pciDevice = Smbus._driver.FindPciDeviceByClassArray(baseClass, subClass, programIf, 1);
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
        /// PCI device's vendor ID
        /// </summary>
        public UInt16 VendorId => ReadWord(RegisterOffset.VendorId);

        /// <summary>
        /// PCI device's ID
        /// </summary>
        public UInt16 DeviceId => ReadWord(RegisterOffset.DeviceId);

        /// <summary>
        /// PCI device's Revision ID
        /// </summary>
        public UInt16 RevisionId => ReadByte(RegisterOffset.RevisionId);

        /// <summary>
        /// Read byte from PCI device memory space
        /// </summary>
        /// <param name="offset">Byte location</param>
        /// <returns>Byte value at <paramref name="offset"/> location</returns>
        public UInt8 ReadByte(UInt32 offset) {
            return Smbus._driver.ReadPciConfigByte(PciInfo.DeviceMemoryLocation, offset);
        }

        /// <summary>
        /// Read word from PCI device memory space
        /// </summary>
        /// <param name="offset">Word location</param>
        /// <returns>Word value at <paramref name="offset"/> location</returns>
        public UInt16 ReadWord(UInt32 offset) {
            return Smbus._driver.ReadPciConfigWord(PciInfo.DeviceMemoryLocation, offset);
        }

        /// <summary>
        /// Read Dword from PCI device memory space
        /// </summary>
        /// <param name="offset">Dword location</param>
        /// <returns>Dword value at <paramref name="offset"/> location</returns>
        public UInt32 ReadDword(UInt32 offset) {
            return Smbus._driver.ReadPciConfigDword(PciInfo.DeviceMemoryLocation, offset);
        }

        /// <summary>
        /// Write byte to PCI device memory space
        /// </summary>
        /// <param name="offset">Byte location</param>
        /// <param name="value">Byte value</param>
        public void WriteByte(UInt32 offset, UInt8 value) {
            Smbus._driver.WritePciConfigByte(PciInfo.DeviceMemoryLocation, offset, value);
        }

        /// <summary>
        /// Write word to PCI device memory space
        /// </summary>
        /// <param name="offset">Word location</param>
        /// <param name="value">Word value</param>
        public void WriteWord(UInt32 offset, UInt16 value) {
            Smbus._driver.WritePciConfigWord(PciInfo.DeviceMemoryLocation, offset, value);
        }

        /// <summary>
        /// Write Dword to PCI device memory space
        /// </summary>
        /// <param name="offset">Dword location</param>
        /// <param name="value">Dword value</param>
        public void WriteDword(UInt32 offset, UInt32 value) {
            Smbus._driver.WritePciConfigDword(PciInfo.DeviceMemoryLocation, offset, value);
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
            public static uint DeviceMemoryLocation => WinRing0.PciBusDevFunc(BusNumber, DeviceNumber, FunctionNumber);
        }
    }
}