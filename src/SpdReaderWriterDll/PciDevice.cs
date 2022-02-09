using OpenLibSys;
using System;
using System.Collections.Generic;
using UInt8 = System.Byte;

namespace SpdReaderWriterDll {
    /// <summary>
    /// PCI Device class
    /// </summary>
    public class PciDevice {
        /// <summary>
        /// Platform Vendor ID
        /// </summary>
        public struct VENDOR_ID {
            public const UInt16 AMD                          = 0x1022;
            public const UInt16 INTEL                        = 0x8086;
        }

        /// <summary>
        /// Intel SMBus Device ID
        /// </summary>
        public struct DEVICE_ID {
            // LGA 2066
            public const UInt16 X299                         = 0xA2A3; // X299 SMbus
            public const UInt16 SKLX_SMBUS                   = 0x2085; // SKL-X & CLX-X
        }

        /// <summary>
        /// DDR4 EEPROM commands
        /// </summary>
        public struct EEPROM_COMMAND {
            // DDR4 Page commands
            public const byte SPA0                           = 0x6C;
            public const byte SPA1                           = 0x6E;
            public const byte RPA                            = 0x6D;

            // DDR4 RSWP commands
            public const byte RPS0                           = 0x63;
            public const byte RPS1                           = 0x69;
            public const byte RPS2                           = 0x6B;
            public const byte RPS3                           = 0x61;
        }

        /// <summary>
        /// Indicates SMBus controller data offsets
        /// </summary>
        public struct SMBUS_OFFSET {
            public const byte I2CADDRESS                     = 0x9D;
            public const byte OFFSET                         = 0x9C;
            public const byte INPUT                          = 0xB6;
            public const byte COMMAND                        = 0x9E;
            public const byte OUTPUT                         = 0xB4;
            public const byte STATUS                         = 0xA8; 
            public const byte DIMMCFG                        = 0x94;
        }

        /// <summary>
        /// Indicates SMBus controller commands and modifiers
        /// </summary>
        public struct SMBUS_COMMAND {
            public const byte EXEC_CMD                       = 0x08; // 0b00001000
            
            // SMBUS_OFFSET.COMMAND modifiers
            public const byte MOD_WORD                       = 0x0A; // 0b00000010
            public const byte MOD_NEXT                       = 0x04; // 0b00000100

            // SMBUS_OFFSET.I2CADDRESS modifiers
            public const byte READ                           = 0x00; // 0b00000000
            public const byte WRITE                          = 0x80; // 0b10000000
        }

        /// <summary>
        /// Describes SMBus controller status codes at <see cref="SMBUS_OFFSET.STATUS">status register</see>
        /// </summary>
        public struct SMBUS_STATUS {
            public const byte READY                          = 0b00000000;
            public const byte BUSY                           = 0b00000001;
            public const byte ACK                            = 0b00000000;
            public const byte NACK                           = 0b00000010;
        }

        /// <summary>
        /// PCI Base Class codes
        /// </summary>
        public struct PCI_BASE_CLASS {
            public const byte SERIAL                         = 0x0C;
        }

        /// <summary>
        /// PCI sub class codes
        /// </summary>
        public struct PCI_SUB_CLASS {
            public const byte SMBUS                          = 0x05;
        }

        /// <summary>
        /// Initializes default PciDevice instance
        /// </summary>
        public PciDevice() {
            core                = new Ols();
            IsRunning          = Connect();
        }

        /// <summary>
        /// Initializes PciDevice instance based on its memory address location
        /// </summary>
        /// <param name="memoryAddress">PCI device memory location</param>
        public PciDevice(uint memoryAddress) {
            core                = new Ols();
            IsRunning          = Connect();

            _pciBusNumber      = core.PciGetBus(memoryAddress);
            _pciDeviceNumber   = core.PciGetDev(memoryAddress);
            _pciFunctionNumber = core.PciGetFunc(memoryAddress);
        }

        /// <summary>
        /// Initializes PciDevice instance based on its memory address location and selects SMBus number
        /// </summary>
        /// <param name="memoryAddress">PCI device memory location</param>
        /// <param name="SmbusNumber">SMBus number</param>
        public PciDevice(uint memoryAddress, byte SmbusNumber) {
            core                = new Ols();
            IsRunning          = Connect();

            _pciBusNumber      = core.PciGetBus(memoryAddress);
            _pciDeviceNumber   = core.PciGetDev(memoryAddress);
            _pciFunctionNumber = core.PciGetFunc(memoryAddress);

            _smBusNumber       = SmbusNumber;

            Eeprom.ResetPageAddress(this);
        }


        /// <summary>
        /// Initializes PciDevice instance based on its PCI location
        /// </summary>
        /// <param name="pciBusNumber">PCI bus number</param>
        /// <param name="pciDeviceNumber">PCI device number</param>
        /// <param name="pciFunctionNumber">PCI function number</param>
        public PciDevice(byte pciBusNumber, byte pciDeviceNumber, byte pciFunctionNumber) {
            core                = new Ols();
            IsRunning          = Connect();

            _pciBusNumber      = pciBusNumber;
            _pciDeviceNumber   = pciDeviceNumber;
            _pciFunctionNumber = pciFunctionNumber;
        }

        /// <summary>
        /// Initializes PciDevice instance based on its PCI location and selects SMBus number
        /// </summary>
        /// <param name="pciBusNumber">PCI bus number</param>
        /// <param name="pciDeviceNumber">PCI device number</param>
        /// <param name="pciFunctionNumber">PCI function number</param>
        /// <param name="smbusNumber">SMBus number</param>
        public PciDevice(byte pciBusNumber, byte pciDeviceNumber, byte pciFunctionNumber, byte smbusNumber) {
            core                = new Ols();
            IsRunning          = Connect();

            _pciBusNumber      = pciBusNumber;
            _pciDeviceNumber   = pciDeviceNumber;
            _pciFunctionNumber = pciFunctionNumber;

            _smBusNumber       = smbusNumber;

            Eeprom.ResetPageAddress(this);
        }

        /// <summary>
        /// PCI device instance string
        /// </summary>
        /// <returns>Readable PCI device instance string</returns>
        public override string ToString() {
            return $"PCI{_pciBusNumber:D}/{_pciDeviceNumber:D}/{_pciFunctionNumber:D}/{_smBusNumber:D}";
        }

        /// <summary>
        /// PCI device instance destructor
        /// </summary>
        ~PciDevice() {
            Disconnect();
        }

        /// <summary>
        /// OpenLibSys instance
        /// </summary>
        public Ols core;

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
                return core.GetStatus() == (uint)Ols.Status.NO_ERROR &&
                       core.GetDllStatus() == (uint)Ols.OlsDllStatus.OLS_DLL_NO_ERROR;
            }

            return IsRunning;
        }

        /// <summary>
        /// Reset PCI device instance 
        /// </summary>
        /// <returns><see langword="true" /> once the PCI device values are reset</returns>
        public bool Disconnect() {
            //core.DeinitializeOls(); // this unloads driver
            IsRunning = IsConnected = false;
            _pciBusNumber = _pciDeviceNumber = _pciFunctionNumber = _smBusNumber = 0xFF;

            return core.GetDllStatus() == (uint)Ols.OlsDllStatus.OLS_DLL_DRIVER_UNLOADED;
        }

        /// <summary>
        /// Finds PCI devices by Vendor and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <returns>An array of PCI devices locations matching <paramref name="vendorId">Vendor ID</paramref> and <paramref name="deviceId">Device ID</paramref> </returns>
        public UInt32[] FindDeviceById(UInt16 vendorId, UInt16 deviceId) {
            return FindDeviceById(vendorId, deviceId, 1);
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

                UInt16 id = (UInt16)core.FindPciDeviceById(vendorId, deviceId, (byte)i);

                if (id != UInt16.MaxValue) {
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
        /// <param name="programIf">Program Interface</param>
        /// <returns>An array of PCI devices locations matching Device Class</returns>
        public UInt32[] FindDeviceByClass(byte baseClass, byte subClass, byte programIf) {
            return FindDeviceByClass(baseClass, subClass, programIf, 1);
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

                UInt16 id = (UInt16)core.FindPciDeviceByClass(baseClass, subClass, programIf, (byte)i);

                if (id != UInt16.MaxValue) {
                    results.Enqueue(id);
                }

                if (results.Count == maxCount) {
                    break;
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// Find available SMBus memory locations in the PCI space
        /// </summary>
        /// <returns>An array of SMBus controllers PCI memory locations</returns>
        public uint[] FindDevice() {

            // Verify CPU and chipset model
            bool valid = FindDeviceByClass(PCI_BASE_CLASS.SERIAL, PCI_SUB_CLASS.SMBUS, 0x00).Length > 0 && 
                         FindDeviceById(VENDOR_ID.INTEL, DEVICE_ID.X299).Length > 0;
            
            return valid ? FindDeviceById(VENDOR_ID.INTEL, DEVICE_ID.SKLX_SMBUS) : new uint[0];
        }

        /// <summary>
        /// Finds available SMBuses on the PCI device
        /// </summary>
        /// <returns>An array of available SMBuses on the PCI device</returns>
        public byte[] FindBus() {

            Stack<byte> busNumber = new Stack<byte>();

            for (byte i = 0; i <= 1; i++) {
                if (// number of valid SPDs
                    ReadByte(SMBUS_OFFSET.DIMMCFG) > 0 ||
                    // number of I2C slave devices
                    Scan(i).Length > 0) {
                    busNumber.Push(i);
                }
            }

            byte[] result = busNumber.ToArray();
            Array.Sort(result);

            return result;
        }

        /// <summary>
        /// Scan SMBus for available I2C addresses
        /// </summary>
        /// <param name="smbusNumber">SMBus number</param>
        /// <returns>An array of available i2c addresses on <paramref name="smbusNumber"/> SMBus</returns>
        public byte[] Scan(byte smbusNumber) {

            Stack<byte> addresses = new Stack<byte>();

            for (byte i = 0x50; i <= 0x57; i++) {
                I2CAddress = i;
                SmbusNumber = smbusNumber;
                Eeprom.ReadByte(this, 0);
                if (!GetError()) {
                    addresses.Push(i);
                }
            }

            byte[] result = addresses.ToArray();
            Array.Sort(result);

            return result;
        }

        /// <summary>
        /// Returns PCI device's vendor ID
        /// </summary>
        /// <returns>System VendorID</returns>
        public UInt16 GetVendorId() {
            if (!IsRunning) {
                return 0xFF;
            }

            return ReadWord(0x01);
        }

        /// <summary>
        /// Returns PCI device's ID
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public UInt16 GetDeviceId() {
            if (!IsRunning) {
                return 0xFF;
            }
            return ReadWord(0x03);
        }

        /// <summary>
        /// Read byte from PCI device memory space
        /// </summary>
        /// <param name="offset">Byte location</param>
        /// <returns>Byte value at <paramref name="offset"/> location</returns>
        public UInt8 ReadByte(byte offset) {
            if (IsRunning) {
                return core.ReadPciConfigByte(_pciDeviceMemoryLocation, GetOffset(offset));
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
                return core.ReadPciConfigWord(_pciDeviceMemoryLocation, (byte)(offset - 1));
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
                return core.ReadPciConfigDword(_pciDeviceMemoryLocation, GetOffset((byte)(offset - 3)));
            }
            throw new Exception("Unable to read dword");
        }

        /// <summary>
        /// Write byte to PCI device memory space
        /// </summary>
        /// <param name="offset">Byte location</param>
        /// <param name="value">Byte value</param>
        public void WriteByte(byte offset, UInt8 value) {
            core.WritePciConfigByte(_pciDeviceMemoryLocation, GetOffset(offset), value);
        }

        /// <summary>
        /// Write word PCI device memory space
        /// </summary>
        /// <param name="offset">Word location</param>
        /// <param name="value">Word value</param>
        public void WriteWord(byte offset, UInt16 value) {
            core.WritePciConfigWord(_pciDeviceMemoryLocation, GetOffset((byte)(offset - 1)), value);
        }

        /// <summary>
        /// Write Dword to PCI device memory space
        /// </summary>
        /// <param name="offset">Dword location</param>
        /// <param name="value">Dword value</param>
        public void WriteDword(byte offset, UInt32 value) {
            core.WritePciConfigDword(_pciDeviceMemoryLocation, GetOffset((byte)(offset - 3)), value);
        }

        /// <summary>
        /// Returns bus error status
        /// </summary>
        /// <returns><see langword="true" /> if the last operation resulted in error or NACK</returns>
        public bool GetError() {
            return (ReadByte(SMBUS_OFFSET.STATUS) & SMBUS_STATUS.NACK) == SMBUS_STATUS.NACK;
        }

        /// <summary>
        /// Checks SMBus device status
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> if the device is busy, or <see langword="false" /> when the device is ready</returns>
        public bool IsBusy() {
            return (ReadByte(SMBUS_OFFSET.STATUS) & SMBUS_STATUS.BUSY) == SMBUS_STATUS.BUSY;
        }

        /// <summary>
        /// Adjust register offset location according to SMBus number
        /// </summary>
        /// <param name="offset">Offset location</param>
        /// <returns>New offset location</returns>
        public byte GetOffset(byte offset) {
            return (byte)(offset + 4 * _smBusNumber);
        }
        
        public bool IsRunning   = false;
        public bool IsConnected = false;
        public bool IsValid     = false;

        /// <summary>
        /// Last set or read EEPROM page number
        /// </summary>
        public byte EepromPageNumber;

        /// <summary>
        /// Last set I2C address
        /// </summary>
        public byte I2CAddress {
            get {
                return _i2cAddress;
            }
            set {
                _i2cAddress = value;
            }
        }

        /// <summary>
        /// Last set SMBus number
        /// </summary>
        public byte SmbusNumber {
            get {
                return _smBusNumber;
            }
            set {
                _smBusNumber = value;
            }
        }

        private uint _pciBusNumber;
        private uint _pciDeviceNumber;
        private uint _pciFunctionNumber;
        private uint _pciDeviceMemoryLocation {
            get => core.PciBusDevFunc(_pciBusNumber, _pciDeviceNumber, _pciFunctionNumber);
        }
        private byte _smBusNumber;
        private byte _i2cAddress;
        private byte _eepromPageNumber = 0;
    }
}
