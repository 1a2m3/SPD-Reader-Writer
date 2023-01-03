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
using SpdReaderWriterDll.Driver;

namespace SpdReaderWriterDll {

    /// <summary>
    /// PCI Device class
    /// </summary>
    public class PciDevice {

        /// <summary>
        /// PCI Base Class codes
        /// </summary>
        public struct BaseClass {
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
            PciInfo.BusNumber      = 0;
            PciInfo.DeviceNumber   = 0;
            PciInfo.FunctionNumber = 0;
        }

        /// <summary>
        /// Initializes PciDevice instance based on its memory address location
        /// </summary>
        /// <param name="memoryAddress">PCI device memory location</param>
        public PciDevice(uint memoryAddress) {
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
        public static uint FindDeviceById(ushort vendorId, ushort deviceId) {
            try {
                uint[] pciDevice = Smbus.Driver.FindPciDeviceByIdArray(vendorId, deviceId, 1);

                if (pciDevice.Length > 0 && pciDevice[0] != uint.MaxValue) {
                    return pciDevice[0];
                }

                return ushort.MaxValue;
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
        public static uint FindDeviceByClass(byte baseClass, byte subClass) {
            return FindDeviceByClass(baseClass, subClass, 0);
        }

        /// <summary>
        /// Finds PCI devices by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <param name="programIf">Program Interface</param>
        /// <returns>PCI device location matching Device Class</returns>
        public static uint FindDeviceByClass(byte baseClass, byte subClass, byte programIf) {
            try {
                uint[] pciDevice = Smbus.Driver.FindPciDeviceByClassArray(baseClass, subClass, programIf, 1);
                if (pciDevice.Length > 0 && pciDevice[0] != uint.MaxValue) {
                    return pciDevice[0];
                }

                return uint.MaxValue;
            }
            catch {
                throw new IOException("PCI device not found");
            }
        }

        /// <summary>
        /// PCI device's vendor ID
        /// </summary>
        public ushort VendorId => ReadWord(RegisterOffset.VendorId);

        /// <summary>
        /// PCI device's ID
        /// </summary>
        public ushort DeviceId => ReadWord(RegisterOffset.DeviceId);

        /// <summary>
        /// PCI device's Revision ID
        /// </summary>
        public ushort RevisionId => ReadByte(RegisterOffset.RevisionId);

        /// <summary>
        /// Reads data from PCI device memory space
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register location</param>
        /// <returns>Data value at <paramref name="offset"/> location</returns>
        public T Read<T>(uint offset) {

            object output = null;

            if (typeof(T) == typeof(byte)) {
                output = ReadByte((ushort)offset);
            }
            else if (typeof(T) == typeof(ushort)) {
                output = ReadWord((ushort)offset);
            }
            else if (typeof(T) == typeof(uint)) {
                output = ReadDword((ushort)offset);
            }

            if (output != null) {
                return (T)Convert.ChangeType(output, typeof(T));
            }

            throw new Exception("Wrong data type");
        }

        /// <summary>
        /// Write data to PCI device memory space
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register location</param>
        /// <param name="value">Data value</param>
        /// <returns></returns>
        public bool Write<T>(uint offset, T value) {

            if (typeof(T) == typeof(byte)) {
                return WriteByte((ushort)offset, (byte)Convert.ChangeType(value, typeof(T)));
            }

            if (typeof(T) == typeof(ushort)) {
                return WriteWord((ushort)offset, (ushort)Convert.ChangeType(value, typeof(T)));
            }

            if (typeof(T) == typeof(uint)) {
                return WriteDword((ushort)offset, (uint)Convert.ChangeType(value, typeof(T)));
            }

            throw new Exception("Wrong data type");
        }

        /// <summary>
        /// Read byte from PCI device memory space
        /// </summary>
        /// <param name="offset">Byte location</param>
        /// <returns>Byte value at <paramref name="offset"/> location</returns>
        private byte ReadByte(uint offset) {
            return Smbus.Driver.ReadPciConfigByte(PciInfo.DeviceMemoryLocation, offset);
        }

        /// <summary>
        /// Read word from PCI device memory space
        /// </summary>
        /// <param name="offset">Word location</param>
        /// <returns>Word value at <paramref name="offset"/> location</returns>
        private ushort ReadWord(uint offset) {
            return Smbus.Driver.ReadPciConfigWord(PciInfo.DeviceMemoryLocation, offset);
        }

        /// <summary>
        /// Read Dword from PCI device memory space
        /// </summary>
        /// <param name="offset">Dword location</param>
        /// <returns>Dword value at <paramref name="offset"/> location</returns>
        private uint ReadDword(uint offset) {
            return Smbus.Driver.ReadPciConfigDword(PciInfo.DeviceMemoryLocation, offset);
        }

        /// <summary>
        /// Write byte to PCI device memory space
        /// </summary>
        /// <param name="offset">Byte location</param>
        /// <param name="value">Byte value</param>
        private bool WriteByte(uint offset, byte value) {
            return Smbus.Driver.WritePciConfigByteEx(PciInfo.DeviceMemoryLocation, offset, value);
        }

        /// <summary>
        /// Write word to PCI device memory space
        /// </summary>
        /// <param name="offset">Word location</param>
        /// <param name="value">Word value</param>
        private bool WriteWord(uint offset, ushort value) {
            return Smbus.Driver.WritePciConfigWordEx(PciInfo.DeviceMemoryLocation, offset, value);
        }

        /// <summary>
        /// Write Dword to PCI device memory space
        /// </summary>
        /// <param name="offset">Dword location</param>
        /// <param name="value">Dword value</param>
        private bool WriteDword(uint offset, uint value) {
            return Smbus.Driver.WritePciConfigDwordEx(PciInfo.DeviceMemoryLocation, offset, value);
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