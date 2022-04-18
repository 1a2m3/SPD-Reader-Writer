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
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using UInt8 = System.Byte;

namespace SpdReaderWriterDll {

    /// <summary>
    /// SMBus class
    /// </summary>
    public class Smbus : IDisposable {

        /// <summary>
        /// Kernel Driver instance
        /// </summary>
        public WinRing0 Driver {
            get => _driver;
            set => _driver = value;
        }
        internal static WinRing0 _driver;

        /// <summary>
        /// Kernel Driver version
        /// </summary>
        public string Version {
            get {
                byte[] v = new byte[4];
                if (_driver != null && _driver.IsReady) {
                    _driver.GetDriverVersion(ref v[0], ref v[1],ref v[2],ref v[3]);
                }
                return $"{v[0]}.{v[1]}.{v[2]}.{v[3]}";
            }
        }

        /// <summary>
        /// Device info struct
        /// </summary>
        public struct DeviceInfo {
            public PlatformVendorId vendorId;
            public ChipsetDeviceId deviceId;
        }
        public DeviceInfo deviceInfo {
            get => _deviceInfo;
            set => _deviceInfo = value;
        }
        private static DeviceInfo _deviceInfo;

        /// <summary>
        /// SMBus bus number
        /// </summary>
        public byte BusNumber {
            get => _busNumber;
            set {
                _busNumber = value;
                // Reset Eeprom page when bus is set
                Eeprom.ResetPageAddress(this);
            }
        }

        private static byte _busNumber;

        /// <summary>
        /// Last set slave address
        /// </summary>
        public UInt8 I2CAddress;

        /// <summary>
        /// Maximum SPD size on this device
        /// </summary>
        public UInt16 MaxSpdSize;

        /// <summary>
        /// SPD BIOS write disable state (ICH/PCH only)
        /// </summary>
        public bool SpdWriteDisabled;

        /// <summary>
        /// PCI device instance
        /// </summary>
        public PciDevice pciDevice {
            get => _pciDevice;
            set => _pciDevice = value;
        }
        private static PciDevice _pciDevice;

        /// <summary>
        /// IO port instance
        /// </summary>
        public IoPort ioPort {
            get => _ioPort;
            set => _ioPort = value;
        }
        private static IoPort _ioPort;

        /// <summary>
        /// Command execution delays
        /// </summary>
        private struct ExecutionDelay {
            internal static readonly UInt8 WriteDelay = 10;
            internal static readonly UInt8 WaitDelay  =  0;
        }

        /// <summary>
        /// Initialize SMBus with default settings
        /// </summary>
        public Smbus() {
            Init();
        }

        /// <summary>
        /// SMBus instance destructor
        /// </summary>
        ~Smbus() {
            Dispose();
        }

        /// <summary>
        /// Disposes SMBus instance
        /// </summary>
        public void Dispose() {
            ioPort     = null;
            pciDevice  = null;
            deviceInfo = default;
            Driver     = null;

            if (!IsRunning) {
                I2CAddress       = 0;
                TotalSMBuses     = 0;
                MaxSpdSize       = 0;
                IsConnected      = false;
                SpdWriteDisabled = false;
            }
        }

        /// <summary>
        /// SMBus name
        /// </summary>
        /// <returns>Human readable SMBus name in a form of platform vendor name and chipset model name</returns>
        public override string ToString() {
            return $"{_deviceInfo.vendorId} {_deviceInfo.deviceId}";
        }

        /// <summary>
        /// Describes SMBus state
        /// </summary>
        public bool IsRunning => (ioPort != null || pciDevice != null) && TotalSMBuses > 0 && Addresses > 0;

        /// <summary>
        /// Describes SMBus connection state
        /// </summary>
        public bool IsConnected;

        /// <summary>
        /// Number of SMBuses
        /// </summary>
        public UInt8 TotalSMBuses;

        /// <summary>
        /// Number of slave addresses on selected bus
        /// </summary>
        public UInt8 Addresses;
        
        /// <summary>
        /// Platform Vendor ID
        /// </summary>
        public enum PlatformVendorId : UInt16 {
             AMD  = 0x1022,
            Intel = 0x8086,
        }

        /// <summary>
        /// Intel: ICH/PCH device ID (LPC/eSPI controller or ISA bridge)
        /// AMD:   TBD
        /// </summary>
        public enum ChipsetDeviceId : UInt16 {

            // DDR3

            #region LGA1156
            H55   = 0x3B06,
            H57   = 0x3B08,
            P55   = 0x3B02,
            Q57   = 0x3B0A,
            #endregion

            #region LGA1155
            B65   = 0x1C50,
            B75   = 0x1E49,
            H61   = 0x1C5C,
            H67   = 0x1C4A,
            H77   = 0x1E4A,
            P67   = 0x1C46,
            Q65   = 0x1C4C,
            Q67   = 0x1C4E,
            Q75   = 0x1E48,
            Q77   = 0x1E47,
            Z68   = 0x1C44,
            Z75   = 0x1E46,
            Z77   = 0x1E44,
            #endregion

            #region LGA1150
            B85   = 0x8C50,
            H81   = 0x8C5C,
            H87   = 0x8C4A,
            H97   = 0x8CC6,
            Q85   = 0x8C4C,
            Q87   = 0x8C4E,
            Z87   = 0x8C44,
            Z97   = 0x8CC4,
            #endregion

            #region MOBILE 5/6/7/8/9 Series
            HM55  = 0x3B09,
            HM57  = 0x3B0B,
            HM65  = 0x1C49,
            HM67  = 0x1C4B,
            HM70  = 0x1E5E,
            HM75  = 0x1E5D,
            HM76  = 0x1E59,
            HM77  = 0x1E57,
            HM86  = 0x8C49,
            HM87  = 0x8C4B,
            HM97  = 0x8CC3,
            NM10  = 0x27BC,
            NM70  = 0x1E5F,
            PM55  = 0x3B03,
            QM57  = 0x3B07,
            QM67  = 0x1C4F,
            QM77  = 0x1E55,
            QM87  = 0x8C4F,
            QS57  = 0x3B0F,
            QS67  = 0x1C4D,
            QS77  = 0x1E56,
            UM67  = 0x1C47,
            UM77  = 0x1E58,
            #endregion

            // DDR4

            #region LGA1151
            B150  = 0xA148,
            B250  = 0xA2C8,
            B360  = 0xA308,
            B365  = 0xA2CC,
            C232  = 0xA14A,
            C236  = 0xA149,
            C242  = 0xA30A,
            C246  = 0xA309,
            CM236 = 0xA150,
            CM238 = 0xA154,
            CM246 = 0xA30E,
            H110  = 0xA143,
            H170  = 0xA144,
            H270  = 0xA2C4,
            H310  = 0xA303,
            H310D = 0x438E,
            H310M = 0xA2CA,
            H370  = 0xA304,
            HM170 = 0xA14E,
            HM175 = 0xA152,
            HM370 = 0xA30D,
            Q150  = 0xA147,
            Q170  = 0xA146,
            Q250  = 0xA2C7,
            Q270  = 0xA2C6,
            Q370  = 0xA306,
            QM170 = 0xA14D,
            QM175 = 0xA153,
            QM370 = 0xA30C,
            Z170  = 0xA145,
            Z270  = 0xA2C5,
            Z370  = 0xA2C9,
            Z390  = 0xA305,
            #endregion

            #region LGA1200
            B460  = 0xA3C8,
            B560  = 0x4387,
            C252  = 0x438C,
            C256  = 0x438D,
            H410  = 0xA3DA,
            H470  = 0x0684,
            H510  = 0x4388,
            H570  = 0x4386,
            Q470  = 0x0687,
            Q570  = 0x4384,
            W480  = 0x0697,
            W580  = 0x438F,
            Z490  = 0x0685,
            Z590  = 0x4385,
            #endregion

            #region LGA2066
            X299 = 0xA2D2, // CPU SMBus x2 (8086h:2085h)
            #endregion
        }

        /// <summary>
        /// Intel CPU SMBus Device ID
        /// </summary>
        public enum IntelCpuSmbusDeviceId : UInt16 {
            // LGA 2066 SKL-X & CLX-X
            SKLX_SMBUS = 0x2085,
        }

        /// <summary>
        /// Intel X299 SMBus controller register offsets
        /// </summary>
        public struct X299SmbusRegister {
            public const byte ADDRESS  = 0x9D;
            public const byte OFFSET   = 0x9C;
            public const byte INPUT    = 0xB6;
            public const byte COMMAND  = 0x9E;
            public const byte OUTPUT   = 0xB4;
            public const byte STATUS   = 0xA8;
            public const byte DIMMCFG  = 0x94;
        }

        /// <summary>
        /// Intel chipset SMBus controller register offsets
        /// </summary>
        public struct IntelSmbusRegister {
            public const byte STATUS   = 0x00;
            public const byte COMMAND  = 0x02;
            public const byte OFFSET   = 0x03;
            public const byte ADDRESS  = 0x04;
            public const byte DATA     = 0x05;
        }

        /// <summary>
        /// Indicates SMBus controller commands and modifiers
        /// </summary>
        public struct X299SmbusCommand {
            public const byte EXEC_CMD = 0x08; // 0b00001000

            // X299SmbusRegister.COMMAND modifiers
            public const byte MOD_WORD = 0x0A; // 0b00000010
            public const byte MOD_NEXT = 0x04; // 0b00000100

            // X299SmbusRegister.ADDRESS modifiers
            public const byte READ     = 0x00; // 0b00000000
            public const byte WRITE    = 0x80; // 0b10000000
        }

        /// <summary>
        /// Describes SMBus controller status codes at <see cref="X299SmbusRegister.STATUS">status register</see>
        /// </summary>
        public struct X299SmbusStatus {
            public const byte READY    = 0b00000000;
            public const byte BUSY     = 0b00000001;
            public const byte ACK      = 0b00000000;
            public const byte NACK     = 0b00000010;
        }

        /// <summary>
        /// Initializes SMBus controller class
        /// </summary>
        private void Init() {

            try {
                Driver = new WinRing0();

                if (Driver.IsReady) {
                    // Load device info
                    deviceInfo = GetDeviceInfo();
                }
            }
            catch (Exception e) {
                throw new Exception($"Initialization failure: {e.Message}");
            }

            if (deviceInfo.vendorId == PlatformVendorId.Intel) {
                switch (deviceInfo.deviceId) {
                    case ChipsetDeviceId.X299:
                        // Locate CPU SMBus controller
                        pciDevice = new PciDevice(PciDevice.FindDeviceById((UInt16)deviceInfo.vendorId, (UInt16)IntelCpuSmbusDeviceId.SKLX_SMBUS));
                        break;

                    default: // ICH/PCH
                        if (!CheckChipsetSupport(deviceInfo.deviceId)) {
                            break;
                        }

                        // Locate ICH/PCH SMBus controller
                        pciDevice = new PciDevice(PciDevice.FindDeviceByClass(PciDevice.BaseClass.Serial, PciDevice.SubClass.Smbus));
                        
                        // Read IO port address and info
                        UInt16 ioPortAddress = pciDevice.ReadWord(PciDevice.RegisterOffset.BaseAddress[4]);

                        // Check SPD write disable bit
                        SpdWriteDisabled = Data.GetBit(pciDevice.ReadByte(0x40), 4);

                        // Check if SMbus is port mapped
                        if ((ioPortAddress & 1) != 1) {
                            break;
                        }

                        // Initialize new SMBus IO port instance
                        ioPort = new IoPort((UInt16)(ioPortAddress & 0xFFFE));

                        break;
                }

                // Common properties
                TotalSMBuses = (byte)FindBus().Length;
                Addresses    = (byte)Scan().Length;
                BusNumber    = 0;
                MaxSpdSize   = GetMaxSpdSize();
            }

            else if (deviceInfo.vendorId == PlatformVendorId.AMD) {
                //throw new NotSupportedException("No AMD support yet");
            }
        }

        /// <summary>
        /// Get platform information
        /// </summary>
        /// <returns>Platform and chipset Device/Vendor ID</returns>
        public static DeviceInfo GetDeviceInfo() {

            DeviceInfo result  = new DeviceInfo();
            PciDevice platform = new PciDevice();

            result.vendorId = (PlatformVendorId)platform.GetVendorId();

            if (result.vendorId == PlatformVendorId.Intel) {
                // Find ISA bridge to get chipset ID
                try {
                    UInt32 _isa = PciDevice.FindDeviceByClass(PciDevice.BaseClass.Bridge, PciDevice.SubClass.Isa);
                    UInt16 _device = new PciDevice(_isa).GetDeviceId();
                    result.deviceId = (ChipsetDeviceId)_device;
                }
                catch {
                    result.deviceId = 0x00;
                }
            }

            else if (result.vendorId == PlatformVendorId.AMD) {
                //throw new NotSupportedException("No AMD support yet");
            }

            return result;
        }

        /// <summary>
        /// Locates available SMBuses on the device
        /// </summary>
        /// <returns>An array of bytes containing SMBus numbers</returns>
        public byte[] FindBus() {
            try {
                Queue<byte> result = new Queue<byte>();

                if (deviceInfo.vendorId == PlatformVendorId.Intel) {

                    switch (deviceInfo.deviceId) {
                        case ChipsetDeviceId.X299:
                            // Save existing bus number
                            byte _currentBus = BusNumber;

                            for (byte i = 0; i <= 1; i++) {
                                BusNumber = i;
                                if (pciDevice.ReadByte((byte)(X299SmbusRegister.DIMMCFG + (i * 4))) > 0 || TryScan()) {
                                    // the bus is valid
                                    result.Enqueue(i);
                                }
                            }

                            // Restore original bus number
                            BusNumber = _currentBus;

                            break;

                        default:
                            if (!CheckChipsetSupport(deviceInfo.deviceId)) {
                                break;
                            }

                            return new byte[] { 0 };
                    }
                }

                return result.ToArray();
            }
            catch {
                return new byte[0];
            }
        }

        /// <summary>
        /// Scan SMBus for available slave devices
        /// </summary>
        /// <returns><see langword="true"/> if <see cref="BusNumber"/> has at least one slave device present</returns>
        public bool TryScan() {
            for (byte i = 0x50; i <= 0x57; i++) {
                if (ProbeAddress(i)) {
                    return true;
                }
            }

            return Scan(this, minimumResults: true).Length > 0;
        }

        /// <summary>
        /// Validates slave address by reading first byte from it
        /// </summary>
        /// <param name="slaveAddress">Slave address</param>
        /// <returns>><see langword="true"/> if <paramref name="slaveAddress"/> responds to <see cref="ReadByte"/></returns>
        public bool ProbeAddress(byte slaveAddress) {
            return ReadByte(this, slaveAddress);
        }

        /// <summary>
        /// Scan SMBus for available slave devices
        /// </summary>
        /// <returns>An array of available bus addresses</returns>
        public UInt8[] Scan() {
            return Scan(this, minimumResults: false);
        }

        /// <summary>
        /// Scan SMBus for available slave devices
        /// </summary>
        /// <param name="bitmask">Set to <see langword="true"/> to enable bitmask result</param>
        /// <returns>A bitmask value representing available bus addresses. Bit 0 is address 80, bit 1 is address 81, and so on.</returns>
        public byte Scan(bool bitmask) {
            byte result = 0;

            if (bitmask) {
                byte[] scanResult = Scan();

                foreach (byte address in scanResult) {
                    result = Data.SetBit(result, (byte)(address - 0x50), true);
                }
                return result;
            }

            throw new ArgumentException("Invalid use of method argument " + nameof(bitmask));
        }

        /// <summary>
        /// Scan SMBus for available slave devices
        /// </summary>
        /// <param name="device">SMBus instance</param>
        /// <param name="minimumResults">Set to <see langword="true"/> to stop scanning once at least one slave address is found,
        /// or <see langword="false"/> to scan the entire range</param>
        /// <returns>An array of found bus addresses on <see cref="BusNumber"/></returns>
        private UInt8[] Scan(Smbus device, bool minimumResults) {

            Queue<byte> result = new Queue<byte>();

            for (byte i = 0; i <= 7; i++) {
                try {
                    if (ReadByte(device, (byte)(i + 0x50))) {
                        result.Enqueue((byte)(i + 0x50));
                        if (minimumResults) {
                            break;
                        }
                    }
                }
                catch {
                    continue;
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Attempts to read from specified slave address
        /// </summary>
        /// <param name="device">SMBus instance</param>
        /// <param name="slaveAddress">Slave address</param>
        /// <returns><see langword="true"/> if <paramref name="slaveAddress"/> responds to read with ACK</returns>
        public static bool ReadByte(Smbus device, byte slaveAddress) {
            try {
                SetSlaveAddress(slaveAddress);
                SetSlaveOffset(0x00);
                SetSlaveReadMode();
                Execute(IntelSmbusCmd.Start | IntelSmbusCmd.CmdByte); // CmdQuick causes SMBus to lock out, requiring restart

                while (GetBusStatus() == SmbStatus.BUSY) { }

                return GetBusStatus() != SmbStatus.ERROR;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Read a single byte from smbus slave device
        /// </summary>
        /// <param name="device">SMBus instance</param>
        /// <param name="slaveAddress">Slave address</param>
        /// <param name="offset">Byte position</param>
        /// <returns>Byte value read from the device</returns>
        public static byte ReadByte(Smbus device, byte slaveAddress, UInt16 offset) {

            if (device.deviceInfo.vendorId == PlatformVendorId.Intel) {

                try {
                    SetSlaveAddress(slaveAddress);
                    SetSlaveOffset(offset);
                    SetSlaveReadMode();
                    Execute(IntelSmbusCmd.Start | IntelSmbusCmd.CmdByte);

                    while (GetBusStatus() == SmbStatus.BUSY) { }

                    if (GetBusStatus() != SmbStatus.ERROR) {
                        return GetSlaveOutputData();
                    }
                }
                catch {
                    throw new IOException($"Read error: {device}:{slaveAddress}:{offset}");
                }
            }

            else if (device.deviceInfo.vendorId == PlatformVendorId.AMD) {
                //throw new NotSupportedException("No AMD support yet");
            }

            throw new IOException($"General Read error");
        }

        /// <summary>
        /// Writes DNC data to the device
        /// </summary>
        /// <param name="device">SMBus instance</param>
        /// <param name="slaveAddress">Slave address</param>
        /// <returns><see langwod="true"/> if write is successful</returns>
        public static bool WriteByte(Smbus device, byte slaveAddress) {
            return WriteByte(device, slaveAddress, 0x00, 0x00);
        }

        /// <summary>
        /// Write specified byte value to specified offset
        /// </summary>
        /// <param name="device">SMBus instance</param>
        /// <param name="slaveAddress">Slave address</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte value</param>
        /// <returns>Byte read from slave device at <paramref name="slaveAddress"/></returns>
        public static bool WriteByte(Smbus device, byte slaveAddress, UInt16 offset, byte value) {

            if (device.deviceInfo.vendorId == PlatformVendorId.Intel) {
                try {
                    SetSlaveAddress(slaveAddress);
                    SetSlaveOffset(offset);
                    SetSlaveInputData(value);
                    SetSlaveWriteMode();
                    Execute(IntelSmbusCmd.Start | IntelSmbusCmd.CmdByteData);

                    Thread.Sleep(slaveAddress >= 0x50 && slaveAddress <= 0x57 
                        ? ExecutionDelay.WriteDelay 
                        : ExecutionDelay.WaitDelay);

                    while (GetBusStatus() == SmbStatus.BUSY) { }

                    return GetBusStatus() != SmbStatus.ERROR;
                }
                catch {
                    return false;
                }
            }

            else if (device.deviceInfo.vendorId == PlatformVendorId.AMD) {
                //throw new NotSupportedException("No AMD support yet");
            }

            return false;
        }

        /// <summary>
        /// Sets slave address for read or write operation
        /// </summary>
        /// <param name="address">Slave address</param>
        private static void SetSlaveAddress(UInt8 address) {

            if (_deviceInfo.vendorId == PlatformVendorId.Intel) {
                switch (_deviceInfo.deviceId) {
                    case ChipsetDeviceId.X299:
                        _pciDevice.WriteByte((UInt8)(X299SmbusRegister.ADDRESS + (_busNumber * 4)), address);
                        break;

                    default:
                        if (!CheckChipsetSupport(_deviceInfo.deviceId)) {
                            break;
                        }

                        _ioPort.WriteByte(IntelSmbusRegister.ADDRESS, (byte)(address << 1));
                        break;
                }
            }

            else if (_deviceInfo.vendorId == PlatformVendorId.AMD) {
                //throw new NotSupportedException("No AMD support yet");
            }
        }

        /// <summary>
        /// Sets byte offset for read or write operation
        /// </summary>
        /// <param name="offset">Byte offset</param>
        private static void SetSlaveOffset(UInt16 offset) {
            if (_deviceInfo.vendorId == PlatformVendorId.Intel) {
                switch (_deviceInfo.deviceId) {

                    case ChipsetDeviceId.X299:
                        _pciDevice.WriteByte((UInt8)(X299SmbusRegister.OFFSET + (_busNumber * 4)), (byte)offset);
                        break;

                    default:
                        if (!CheckChipsetSupport(_deviceInfo.deviceId)) {
                            break;
                        }

                        _ioPort.WriteByte(IntelSmbusRegister.OFFSET, (UInt8)offset);
                        break;
                }
            }

            else if (_deviceInfo.vendorId == PlatformVendorId.AMD) {
                //throw new NotSupportedException("No AMD support yet");
            }
        }

        /// <summary>
        /// Sets mode for read operation
        /// </summary>
        private static void SetSlaveReadMode() {
            if (_deviceInfo.vendorId == PlatformVendorId.Intel) {
                switch (_deviceInfo.deviceId) {
                    case ChipsetDeviceId.X299:
                        // Don't need to do anything
                        break;

                    default:
                        if (!CheckChipsetSupport(_deviceInfo.deviceId)) {
                            break;
                        }

                        // Set bit 0 to 1 to enable read
                        _ioPort.WriteByte(
                            offset : IntelSmbusRegister.ADDRESS, 
                             value : Data.SetBit(_ioPort.ReadByte(IntelSmbusRegister.ADDRESS), 0, true));
                        break;
                }
            }
        }

        /// <summary>
        /// Sets mode for write operation
        /// </summary>
        private static void SetSlaveWriteMode() {
            if (_deviceInfo.vendorId == PlatformVendorId.Intel) {
                switch (_deviceInfo.deviceId) {
                    case ChipsetDeviceId.X299:
                        _pciDevice.WriteByte(
                            offset : (UInt8)(X299SmbusRegister.ADDRESS + (_busNumber * 4)),
                             value : (byte)(_pciDevice.ReadByte((UInt8)(X299SmbusRegister.ADDRESS + (_busNumber * 4))) | X299SmbusCommand.WRITE));
                        break;

                    default:
                        if (!CheckChipsetSupport(_deviceInfo.deviceId)) {
                            break;
                        }
                        // Don't need to do anything
                        break;
                }
            }
        }

        /// <summary>
        /// Sets byte value for write operation
        /// </summary>
        /// <param name="input">Byte value</param>
        private static void SetSlaveInputData(byte input) {
            if (_deviceInfo.vendorId == PlatformVendorId.Intel) {
                switch (_deviceInfo.deviceId) {
                    case ChipsetDeviceId.X299:
                        _pciDevice.WriteByte(
                            offset : (byte)(X299SmbusRegister.INPUT + (_busNumber * 4)), 
                             value : input);
                        break;

                    default:
                        if (!CheckChipsetSupport(_deviceInfo.deviceId)) {
                            break;
                        }
                        _ioPort.WriteByte(
                            offset : IntelSmbusRegister.DATA, 
                             value : input);
                        break;
                }
            }

            else if (_deviceInfo.vendorId == PlatformVendorId.AMD) {
                //throw new NotSupportedException("No AMD support yet");
            }
        }

        /// <summary>
        /// Reads output byte value after read operation
        /// </summary>
        /// <returns>Byte value</returns>
        private static byte GetSlaveOutputData() {

            if (_deviceInfo.vendorId == PlatformVendorId.Intel) {
                switch (_deviceInfo.deviceId) {
                    case ChipsetDeviceId.X299:
                        return _pciDevice.ReadByte((byte)(X299SmbusRegister.OUTPUT + (_busNumber * 4)));

                    default:
                        if (!CheckChipsetSupport(_deviceInfo.deviceId)) {
                            break;
                        }

                        return _ioPort.ReadByte(IntelSmbusRegister.DATA);
                }
            }

            else if (_deviceInfo.vendorId == PlatformVendorId.AMD) {
                //throw new NotSupportedException("No AMD support yet");
            }

            throw new IOException("No data");
        }

        /// <summary>
        /// Execute SMB command
        /// </summary>
        private static void Execute(byte cmd) {
            if (_deviceInfo.vendorId == PlatformVendorId.Intel) {
                switch (_deviceInfo.deviceId) {
                    case ChipsetDeviceId.X299:
                        _pciDevice.WriteByte(
                            offset : (byte)(X299SmbusRegister.COMMAND + (_busNumber * 4)),
                             value : X299SmbusCommand.EXEC_CMD);
                        while (GetBusStatus() == SmbStatus.BUSY) { }
                        break;

                    default:
                        if (!CheckChipsetSupport(_deviceInfo.deviceId)) {
                            break;
                        }

                        // Clear last status
                        while (GetBusStatus() != SmbStatus.READY) {
                            _ioPort.WriteByte(
                                offset : IntelSmbusRegister.STATUS,
                                 value : Data.SetBit(
                                       input : _ioPort.ReadByte(offset: IntelSmbusRegister.STATUS), 
                                    position : 0, 
                                       value : true)); 
                            while (GetBusStatus() == SmbStatus.BUSY) { }
                        }
                        
                        // Execute
                        _ioPort.WriteByte(
                            offset : IntelSmbusRegister.COMMAND,
                             value : cmd);
                        while (GetBusStatus() == SmbStatus.BUSY) { }

                        // Wait for success or error status, because on some systems
                        // clear busy flag is not a guarantee the execution has completed
                        while (true) {
                            if (GetBusStatus() == SmbStatus.SUCCESS ||
                                GetBusStatus() == SmbStatus.ERROR) {
                                break;
                            }
                        }

                        break;
                }
            }

            else if (_deviceInfo.vendorId == PlatformVendorId.AMD) {
                //throw new NotSupportedException("No AMD support yet");
            }
        }

        /// <summary>
        /// SMBus Host Control Register data
        /// </summary>
        public struct IntelSmbusCmd {
            
            /// <summary>
            /// Initiates the Smbus command 
            /// </summary>
            public const byte Start       = 1 << 6;

            /// <summary>
            /// <b>Quick:</b>
            /// The slave address and read/write value (bit 0) are stored in the transmit slave address register.
            /// </summary>
            public const byte CmdQuick    = 0b000 << 2;
            /// <summary>
            /// <b>Byte:</b>
            /// This command uses the transmit slave address and command registers.
            /// Bit 0 of the slave address register determines if this is a read or write command.
            /// </summary>
            public const byte CmdByte     = 0b001 << 2;
            /// <summary>
            /// <b>Byte Data:</b>
            /// This command uses the transmit slave address, command, and DATA0 registers.
            /// Bit 0 of the slave address register determines if this is a read or write command.
            /// If it is a read, the DATA0 register will contain the read data.
            /// </summary>
            public const byte CmdByteData = 0b010 << 2;
            /// <summary>
            /// <b>Word Data:</b>
            /// This command uses the transmit slave address, command, DATA0 and DATA1 registers.
            /// Bit 0 of the slave address register determines if this is a read or write command.
            /// If it is a read, after the command completes, the DATA0 and DATA1 registers will contain the read data.
            /// </summary>
            public const byte CmdWordData = 0b011 << 2;
            /// <summary>
            /// <b>Process Call:</b>
            /// This command uses the transmit slave address, command, DATA0 and DATA1 registers.
            /// Bit 0 of the slave address register determines if this is a read or write command.
            /// After the command completes, the DATA0 and DATA1 registers will contain the read data.
            /// </summary>
            public const byte CmdPrcCall  = 0b100 << 2;
            /// <summary>
            /// <b>Block:</b>
            /// This command uses the transmit slave address, command, DATA0 registers, and the Block Data Byte register.
            /// For block write, the count is stored in the DATA0 register and indicates how many bytes of data will be transferred.
            /// For block reads, the count is received and stored in the DATA0 register.
            /// Bit 0 of the slave address register selects if this is a read or write command.
            /// For writes, data is retrieved from the first n (where n is equal to the specified count) addresses of the SRAM array.
            /// For reads, the data is stored in the Block Data Byte register.
            /// </summary>
            public const byte CmdBlock    = 0b101 << 2;
            /// <summary>
            /// <b>I2C Read:</b>
            /// This command uses the transmit slave address, command, DATA0, DATA1 registers, and the Block Data Byte register.
            /// The read data is stored in the Block Data Byte register.
            /// The ICH/PCH continues reading data until the NAK is received.
            /// </summary>
            public const byte CmdI2CRead  = 0b110 << 2;
        }

        /// <summary>
        /// SMBus status
        /// </summary>
        public enum SmbStatus : byte {
            READY,
            BUSY,
            ERROR,
            SUCCESS,
        }

        /// <summary>
        /// Gets SMBus status
        /// </summary>
        /// <returns>SMBus status</returns>
        public static SmbStatus GetBusStatus() {

            byte _status;

            if (_deviceInfo.vendorId == PlatformVendorId.Intel) {
                switch (_deviceInfo.deviceId) {

                    case ChipsetDeviceId.X299:
                        _status = _pciDevice.ReadByte((byte)(X299SmbusRegister.STATUS + (_busNumber * 4)));
                        if ((_status & X299SmbusStatus.BUSY) > 0) {
                            return SmbStatus.BUSY;
                        }
                        else if ((_status & X299SmbusStatus.NACK) > 0) {
                            return SmbStatus.ERROR;
                        }
                        else if ((_status & X299SmbusStatus.ACK)  > 0) {
                            return SmbStatus.SUCCESS;
                        }

                        return SmbStatus.READY;

                    default:
                        if (!CheckChipsetSupport(_deviceInfo.deviceId)) {
                            break;
                        }

                        _status = _ioPort.ReadByte(IntelSmbusRegister.STATUS);

                        // Check first 6 bits (SMSTS, FAIL, BERR, DERR, INTR, HBSY)
                        if (Data.GetByteFromBits(_status, 5) == 0) {
                            return SmbStatus.READY;
                        }
                        else if (Data.GetBit(_status, 1)) { // 0x42
                            return SmbStatus.SUCCESS;
                        }

                        else if (Data.GetBit(_status, 0)) { // 0x41
                            return SmbStatus.BUSY;
                        }
                        else if (
                            Data.GetBit(_status, 4) ||      // FAIL (0x50)
                            Data.GetBit(_status, 3) ||      // BERR (0x48)
                            Data.GetBit(_status, 2)         // DERR (0x44)
                        ) {
                            return SmbStatus.ERROR;
                        }

                        break;
                }
            }

            else if (_deviceInfo.vendorId == PlatformVendorId.AMD) {
                //throw new NotSupportedException("No AMD support yet");
            }

            return SmbStatus.ERROR;
        }

        /// <summary>
        /// Check if the device ID is supported
        /// </summary>
        /// <param name="deviceId">Chipset DeviceID</param>
        /// <returns><see langword="true"/> if <paramref name="deviceId"/> is present in the <see cref="ChipsetDeviceId"/> enum</returns>
        private static bool CheckChipsetSupport(ChipsetDeviceId deviceId) {
            return Enum.IsDefined(typeof(ChipsetDeviceId), deviceId);
        }

        /// <summary>
        /// Gets supported platform maximum SPD size
        /// </summary>
        /// <returns>SPD size</returns>
        private UInt16 GetMaxSpdSize() {

            if (deviceInfo.vendorId == PlatformVendorId.Intel) {

                UInt16 modelNumber = (UInt16)Int32.Parse(Regex.Match(deviceInfo.deviceId.ToString(), @"\d+").Value);

                // Intel 90 series and older models before 100 series all support 256 byte EEPROM
                if (modelNumber < 100) {
                    return (UInt16)Ram.SpdSize.MINIMUM; // DDR3 and below
                }

                // Intel 200 series and up to before 600 series all support DDR4
                if (modelNumber >= 200 && modelNumber < 600) {
                    return (UInt16)(Ram.SpdSize.DDR4);
                }

                // Other platforms supporting multiple RAM types (Intel 100 series):
                // Read first DIMM's EEPROM byte at offset 0x02
                foreach (byte address in Scan()) {
                    I2CAddress = address;
                    return (UInt16)Spd.GetSpdSize(Spd.GetRamType(new byte[] { 0x00, 0x00, Eeprom.ReadByte(this, 2) }));
                }
            }

            return (UInt16)Ram.SpdSize.MINIMUM; // DDR3 and below
        }
    }
}