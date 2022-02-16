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
        public struct PCI_BASE_CLASS {
            public const byte MEMORY = 0x05;
            public const byte BRIDGE = 0x06;
            public const byte SERIAL = 0x0C;
        }

        /// <summary>
        /// PCI sub class codes
        /// </summary>
        public struct PCI_SUB_CLASS {
            public const byte ISA = 0x01;
            public const byte SMBUS = 0x05;
        }

        /// <summary>
        /// Initializes default PciDevice instance
        /// </summary>
        public PciDevice() {
            //Core = new Ols();
            IsRunning = Connect();
        }

        /// <summary>
        /// Initializes PciDevice instance based on its memory address location
        /// </summary>
        /// <param name="memoryAddress">PCI device memory location</param>
        public PciDevice(uint memoryAddress) {
            //Core                = new Ols();
            IsRunning = Connect();

            _pciBusNumber = Smbus.Driver.PciGetBus(memoryAddress);
            _pciDeviceNumber = Smbus.Driver.PciGetDev(memoryAddress);
            _pciFunctionNumber = Smbus.Driver.PciGetFunc(memoryAddress);
        }

        /// <summary>
        /// Initializes PciDevice instance based on its PCI location
        /// </summary>
        /// <param name="pciBusNumber">PCI bus number</param>
        /// <param name="pciDeviceNumber">PCI device number</param>
        /// <param name="pciFunctionNumber">PCI function number</param>
        public PciDevice(byte pciBusNumber, byte pciDeviceNumber, byte pciFunctionNumber) {
            //Core                = new Ols();
            IsRunning = Connect();

            _pciBusNumber = pciBusNumber;
            _pciDeviceNumber = pciDeviceNumber;
            _pciFunctionNumber = pciFunctionNumber;
        }

        /// <summary>
        /// PCI device instance string
        /// </summary>
        /// <returns>Readable PCI device instance string</returns>
        public override string ToString() {
            return $"PCI{_pciBusNumber:D}/{_pciDeviceNumber:D}/{_pciFunctionNumber:D}";
        }

        /// <summary>
        /// PCI device instance destructor
        /// </summary>
        ~PciDevice() {
            Disconnect();
        }

        /// <summary>
        /// Maximum SPD size in bytes
        /// </summary>
        public UInt16 MaxSpdSize = (int)Ram.SpdSize.DDR4;

        /// <summary>
        /// Load driver
        /// </summary>
        /// <returns><see langword="true" /> once the driver is loaded and PCI device is ready</returns>
        public bool Connect() {
            if (!IsRunning) {
                return Smbus.Driver.GetStatus() == 0 &&
                       Smbus.Driver.GetDllStatus() == 0;
            }

            return IsRunning;
        }

        /// <summary>
        /// Reset PCI device instance 
        /// </summary>
        /// <returns><see langword="true" /> once the PCI device values are reset</returns>
        public bool Disconnect() {
            IsRunning = false;
            _pciBusNumber = _pciDeviceNumber = _pciFunctionNumber = _smBusNumber = 0xFF;

            return Smbus.Driver.GetDllStatus() != 0;
        }

        /// <summary>
        /// Finds PCI devices by Vendor and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <returns>An array of PCI devices locations matching <paramref name="vendorId">Vendor ID</paramref> and <paramref name="deviceId">Device ID</paramref> </returns>
        public static UInt32 FindDeviceById(UInt16 vendorId, UInt16 deviceId) {

            UInt32[] _pciDevice = new PciDevice().FindDeviceById(vendorId, deviceId, 1);

            if (_pciDevice.Length > 0 && _pciDevice[0] != UInt16.MaxValue) {
                return _pciDevice[0];
            }

            throw new IOException("PCI device not found");
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
        /// <returns>System VendorID</returns>
        public UInt16 GetVendorId() {
            if (!IsRunning) {
                return 0xDEAD;
            }

            return ReadWord(0x00);
        }

        /// <summary>
        /// Returns PCI device's ID
        /// </summary>
        /// <returns></returns>
        public UInt16 GetDeviceId() {
            if (!IsRunning) {
                return 0xDEAD;
            }
            return ReadWord(0x02);
        }

        /// <summary>
        /// Read byte from PCI device memory space
        /// </summary>
        /// <param name="offset">Byte location</param>
        /// <returns>Byte value at <paramref name="offset"/> location</returns>
        public UInt8 ReadByte(byte offset) {
            if (IsRunning) {
                return Smbus.Driver.ReadPciConfigByte(_pciDeviceMemoryLocation, GetOffset(offset));
            }

            throw new Exception("Unable to read byte");
        }

        /// <summary>
        /// Read word from PCI device memory space
        /// </summary>
        /// <param name="offset">Word location</param>
        /// <returns>Word value at <paramref name="offset"/> location</returns>
        public UInt16 ReadWord(byte offset) {
            if (IsRunning) {
                return Smbus.Driver.ReadPciConfigWord(_pciDeviceMemoryLocation, offset);
            }
            throw new Exception("Unable to read word");
        }

        /// <summary>
        /// Read Dword from PCI device memory space
        /// </summary>
        /// <param name="offset">Dword location</param>
        /// <returns>Dword value at <paramref name="offset"/> location</returns>
        public UInt32 ReadDword(byte offset) {
            if (IsRunning) {
                return Smbus.Driver.ReadPciConfigDword(_pciDeviceMemoryLocation, GetOffset(offset));
            }
            throw new Exception("Unable to read dword");
        }

        /// <summary>
        /// Write byte to PCI device memory space
        /// </summary>
        /// <param name="offset">Byte location</param>
        /// <param name="value">Byte value</param>
        public void WriteByte(byte offset, UInt8 value) {
            Smbus.Driver.WritePciConfigByte(_pciDeviceMemoryLocation, offset, value);
            //Core.WritePciConfigByte(_pciDeviceMemoryLocation, GetOffset(offset), value);
        }

        /// <summary>
        /// Write word PCI device memory space
        /// </summary>
        /// <param name="offset">Word location</param>
        /// <param name="value">Word value</param>
        public void WriteWord(byte offset, UInt16 value) {
            Smbus.Driver.WritePciConfigWord(_pciDeviceMemoryLocation, GetOffset((byte)(offset - 1)), value);
        }

        /// <summary>
        /// Write Dword to PCI device memory space
        /// </summary>
        /// <param name="offset">Dword location</param>
        /// <param name="value">Dword value</param>
        public void WriteDword(byte offset, UInt32 value) {
            Smbus.Driver.WritePciConfigDword(_pciDeviceMemoryLocation, GetOffset((byte)(offset - 3)), value);
        }

        /// <summary>
        /// Adjust register offset location according to SMBus number
        /// </summary>
        /// <param name="offset">Offset location</param>
        /// <returns>New offset location</returns>
        public byte GetOffset(byte offset) {
            return (byte)(offset + 4 * _smBusNumber);
        }

        public bool IsRunning = false;
        public bool IsValid = false;

        private uint _pciBusNumber;
        private uint _pciDeviceNumber;
        private uint _pciFunctionNumber;
        private uint _pciDeviceMemoryLocation {
            get => Smbus.Driver.PciBusDevFunc(_pciBusNumber, _pciDeviceNumber, _pciFunctionNumber);
        }
        private byte _smBusNumber;
        private byte _i2cAddress;
        private byte _eepromPageNumber = 0;
    }
}
