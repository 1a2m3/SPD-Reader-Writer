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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using static SpdReaderWriterCore.PciDevice;

namespace SpdReaderWriterCore {

    /// <summary>
    /// SMBus class
    /// </summary>
    public class Smbus : IDisposable, IDevice {

        /// <summary>
        /// Kernel Driver version
        /// </summary>
        public string Version {
            get {
                byte[] v = new byte[4];
                if (KernelDriver.IsRunning) {
                    KernelDriver.GetDriverVersion(out v[0], out v[1], out v[2], out v[3]);
                }

                return $"{v[0]}.{v[1]}.{v[2]}.{v[3]}";
            }
        }

        /// <summary>
        /// Smbus Device Info
        /// </summary>
        public DeviceInfo Info { get; private set; }

        /// <summary>
        /// SMBus bus number
        /// </summary>
        public byte BusNumber {
            get => _busNumber;
            set {
                _busNumber = value;

                // Rescan for slave addresses
                Addresses = Scan();

                // Reset Eeprom page when bus is set
                Eeprom.ResetPageAddress(this);

                // Force SPD size and DDR5 flag update
                I2CAddress = _i2CAddress;
            }
        }
        private byte _busNumber;

        /// <summary>
        /// Current I2C address
        /// </summary>
        public byte I2CAddress {
            get => _i2CAddress;
            set {
                _i2CAddress = value;

                // Check for DDR5 presence
                IsDdr5Present = Eeprom.ValidateEepromAddress(_i2CAddress) &&
                                ProbeAddress((byte)(Eeprom.LidCode.Pmic0 << 3 | (Eeprom.Spd5Register.LocalHid & value))) &&
                                ReadByte(_i2CAddress, 00) == 0x51;

                // Reset Eeprom page
                Eeprom.ResetPageAddress(this);

                // Get or update SPD size
                MaxSpdSize = Eeprom.ValidateEepromAddress(_i2CAddress) ? GetMaxSpdSize(_i2CAddress) : Spd.DataLength.Unknown;
            }
        }
        private byte _i2CAddress;

        /// <summary>
        /// DDR5 presence flag
        /// </summary>
        public bool IsDdr5Present { get; private set; }

        /// <summary>
        /// Maximum SPD size on this device
        /// </summary>
        public ushort MaxSpdSize { get; private set; }

        /// <summary>
        /// SPD BIOS write disable state (ICH/PCH only)
        /// </summary>
        public bool SpdWriteDisabled { get; private set; }

        /// <summary>
        /// PCI device instance
        /// </summary>
        public PciDevice PciDevice { get; private set; }

        /// <summary>
        /// IO port instance
        /// </summary>
        public IoPort IoPort { get; private set; }

        /// <summary>
        /// Initialize SMBus with default settings
        /// </summary>
        public Smbus() => Connect();

        /// <summary>
        /// Established SMBus connection
        /// </summary>
        /// <returns><see langword="true"/> upon successful initialization</returns>
        public bool Connect() => Initialize();

        /// <summary>
        /// SMBus instance destructor
        /// </summary>
        ~Smbus() {
            Dispose();
        }

        /// <summary>
        /// Deinitializes SMBus instance
        /// </summary>
        /// <returns><see langword="true"/> upon successful disposal</returns>
        public bool Disconnect() {
            
            Dispose();
            KernelDriver.Stop();

            return !KernelDriver.IsRunning;
        }

        /// <summary>
        /// Disposes SMBus instance
        /// </summary>
        public void Dispose() {
            IoPort    = null;
            PciDevice = null;

            if (!IsConnected) {
                SMBuses          = null;
                IsConnected      = false;
                SpdWriteDisabled = false;
            }

            KernelDriver.Stop();
        }

        /// <summary>
        /// SMBus name
        /// </summary>
        /// <returns>Human readable SMBus name in a form of platform vendor name and chipset model name</returns>
        public override string ToString() {
            return $"{Info.VendorId} {Info.DeviceId}";
        }

        /// <summary>
        /// Describes SMBus connection state
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Available SMBuses
        /// </summary>
        public byte[] SMBuses { get; private set; }

        /// <summary>
        /// Available slave addresses on selected bus
        /// </summary>
        public byte[] Addresses { get; private set; }

        /// <summary>
        /// Intel ICH/PCH and AMD FCH SMBus controller register offsets
        /// </summary>
        public struct DefaultSmbusRegister {

            /// <summary>
            /// Host status
            /// </summary>
            public const byte Status = 0x00;

            /// <summary>
            /// Host control
            /// </summary>
            /// <remarks>Used to execute <see cref="SmbusCmd"/> after setting registers <see cref="Address"/>,
            /// <see cref="HostCmd"/>, (and <see cref="Data0"/>, if <see cref="SmbusAccessMode.Write"/> mode is used)</remarks>
            public const byte Control = 0x02;

            /// <summary>
            /// Host command
            /// </summary>
            /// <remarks>This registers is used to specify offset</remarks>
            public const byte HostCmd = 0x03;

            /// <summary>
            /// Transmit slave address
            /// </summary>
            /// <remarks>Bits 7-1: i2c address;
            /// bit 0: r/w mode (1 = <see cref="SmbusAccessMode.Read"/>, 0 = <see cref="SmbusAccessMode.Write"/>)</remarks>
            /// <example>Set to 0xA1 to read from 0x50, or 0xA0 to write to 0x50</example>
            public const byte Address = 0x04;

            /// <summary>
            /// Data 0
            /// </summary>
            /// <remarks>Input for <see cref="SmbusAccessMode.Write"/>,
            /// or output for <see cref="SmbusAccessMode.Read"/> operation</remarks>
            public const byte Data0 = 0x05;
        }

        /// <summary>
        /// Intel CPU SMBus Device ID
        /// </summary>
        public enum SkylakeXDeviceId : ushort {
            // LGA 2066 SKL-X & CLX-X IMC Smbus
            CpuImcSmbus = 0x2085,

            // LGA 2011-3 HW-E & BW-E
            //HSWE_SMBUS_0 = 0x2F68,
            //HSWE_SMBUS_1 = 0x2FA8,
        }

        /// <summary>
        /// Intel Skylake-X CPU SMBus controller register offsets
        /// </summary>
        private struct SkylakeXSmbusRegister {
            public const byte Address    = 0x9D;
            public const byte Offset     = 0x9C;
            public const byte Input      = 0xB6;
            public const byte Command    = 0x9E;
            public const byte Output     = 0xB4;
            public const byte Status     = 0xA8;
            public const byte DimmConfig = 0x94;
        }

        /// <summary>
        /// Describes Skylake-X CPU SMBus controller status codes at <see cref="SkylakeXSmbusRegister.Status">status register</see>
        /// </summary>
        private struct SkylakeXSmbusStatus {
            public const byte Ready    = 0b00000000;
            public const byte Complete = 0b00000100;
            public const byte Busy     = 0b00000001;
            public const byte Ack      = 0b00000000;
            public const byte Nack     = 0b00000010;
        }

        /// <summary>
        /// Nvidia Smbus register offsets
        /// </summary>
        private struct NvidiaSmbusRegister {
            public const byte Protocol    = 0x00;
            public const byte Status      = 0x01;
            public const byte Address     = 0x02;
            public const byte Command     = 0x03; // Offset
            public const byte Data        = 0x04; // Input (write) or output (read)
            public const byte ByteCount   = 0x24; // Number of data bytes
            public const byte Control     = 0x3E; // Control register
            public const byte AbortStatus = 0x3C; // register used to check the status of the abort command
        }

        /// <summary>
        /// Nvidia Smbus protocol commands for the <see cref="NvidiaSmbusRegister.Protocol"/> register
        /// </summary>
        private struct NvidiaSmbusProtocol {
            public const byte Quick     = 0x02;
            public const byte Byte      = 0x04;
            public const byte ByteData  = 0x06;
            public const byte WordData  = 0x08;
            public const byte BlockData = 0x0A;

            public const byte Abort     = 0x20;

            public const byte Pec       = 0x80;
        }

        /// <summary>
        /// Describes Nvidia Smbus status codes at <see cref="NvidiaSmbusRegister.Status">status register</see>
        /// </summary>
        private struct NvidiaSmbusStatus {
            public const byte Done    = 0x80;
            public const byte Invalid = 0x10;
            public const byte Error   = 0x1F;
        }

        /// <summary>
        /// Check if the device ID is supported
        /// </summary>
        /// <param name="deviceId">Chipset DeviceID</param>
        /// <returns><see langword="true"/> if <paramref name="deviceId"/> is present in the <see cref="DeviceId"/> enum</returns>
        private bool CheckChipsetSupport(DeviceId deviceId) {
            return Enum.IsDefined(typeof(DeviceId), deviceId);
        }

        /// <summary>
        /// Check if the device ID belongs to Skylake-X or Cascade Lake-X platform
        /// </summary>
        /// <param name="deviceId">Chipset DeviceID</param>
        /// <returns><see langword="true"/> if <paramref name="deviceId"/> belongs to Skylake-X or Cascade Lake-X platform</returns>
        private bool IsSkylakeX(DeviceId deviceId) {

            DeviceId[] SkylakeX = {
                DeviceId.X299,
                DeviceId.C422,
            };

            return SkylakeX.Contains(deviceId);
        }

        /// <summary>
        /// Gets platform type
        /// </summary>
        private Platform PlatformType {
            get {
                if (IsSkylakeX(Info.DeviceId)) {
                    return Platform.SkylakeX;
                }

                if (CheckChipsetSupport(Info.DeviceId)) {
                    return Platform.Default;
                }

                return Platform.Unknown;
            }
        }

        /// <summary>
        /// Platform type
        /// </summary>
        private enum Platform : sbyte {

            /// <summary>
            /// Unknown or unsupported platform type
            /// </summary>
            Unknown  = -1,

            /// <summary>
            /// Intel ICH/PCH, AMD FCH, and Nvidia MCP platforms
            /// </summary>
            Default  = 0,

            /// <summary>
            /// Skylake X (incl. Refresh) and Cascade Lake X platforms
            /// </summary>
            SkylakeX = 1,
        }

        /// <summary>
        /// Initializes SMBus controller class
        /// </summary>
        /// <returns><see langword="true"/> if SMBus instance is initialized successfully</returns>
        private bool Initialize() {
            try {
                if (!KernelDriver.Start()) {
                    throw new Exception($"{KernelDriver.DriverInfo.ServiceName} did not start.");
                }
            }
            catch (Exception e) {
                throw new Exception($"{KernelDriver.DriverInfo.ServiceName} initialization failure ({e.Message})");
            }

            Info = GetDeviceInfo(); 

            // Find default SMBus controller(s)
            PciDevice[] smbusPciDevice = FindDeviceByClass(BaseClassType.Serial, SubClassType.Smbus, 0);

            if (smbusPciDevice.Length == 1) {
                PciDevice = smbusPciDevice[0];
            }
            else if (smbusPciDevice.Length > 1) {
                foreach (PciDevice smbusPciLocation in smbusPciDevice) {
                    if (smbusPciLocation.VendorId  == Info.VendorId && 
                        smbusPciLocation.BaseClass == BaseClassType.Serial &&
                        smbusPciLocation.SubClass  == SubClassType.Smbus) {
                        PciDevice = smbusPciLocation;
                        break;
                    }
                }
            }
            else {
                if (Info.VendorId != VendorId.VIA) {
                    return false;
                }
            }

            switch (Info.VendorId) {
                // Skylake-X
                case VendorId.Intel when PlatformType == Platform.SkylakeX:
                    // Locate CPU SMBus controller
                    PciDevice = FindDeviceById(Info.VendorId, (DeviceId)SkylakeXDeviceId.CpuImcSmbus);
                    break;
                // ICH/PCH
                case VendorId.Intel:
                case VendorId.Nvidia:
                    if (PlatformType == Platform.Default) {
                        // Read IO port address and info
                        ushort ioPortAddress = PciDevice.Read<ushort>(Register.BaseAddress[4]);

                        // Check SPD write disable bit
                        SpdWriteDisabled = Data.GetBit(PciDevice.Read<byte>(0x40), 4);

                        // Check if SMbus is port mapped
                        if (Data.GetBit(ioPortAddress, 0)) {

                            // Initialize new SMBus IO port instance
                            IoPort = new IoPort(Data.SetBit(ioPortAddress, 0, false));
                        }
                    }

                    break;

                case VendorId.AMD:
                    // AMD AM4, AM1, FM1, FM2(+)
                    if ((PciDevice.DeviceId == DeviceId.ZEN && PciDevice.RevisionId >= 0x49) ||
                        (PciDevice.DeviceId == DeviceId.FCH && PciDevice.RevisionId >= 0x41)) {

                        // PMIO registers accessible via IO ports
                        const ushort SB800_PIIX4_SMB_IDX = 0xCD6;
                        const ushort SB800_PIIX4_SMB_DAT = 0xCD7;

                        const byte smb_en = 0x00; // AMD && (Hudson2 && revision >= 0x41 || FCH && revision >= 0x49)

                        KernelDriver.WriteIoPort(SB800_PIIX4_SMB_IDX, smb_en);
                        byte smba_en_lo = KernelDriver.ReadIoPort<byte>(SB800_PIIX4_SMB_DAT);
                        KernelDriver.WriteIoPort(SB800_PIIX4_SMB_IDX, (byte)(smb_en + 1));
                        byte smba_en_hi = KernelDriver.ReadIoPort<byte>(SB800_PIIX4_SMB_DAT);

                        if (smba_en_hi == byte.MaxValue || smba_en_lo == byte.MaxValue) {
                            // PMIO is disabled, get SMBus port from memory
                            const uint SB800_PIIX4_FCH_PM_ADDR = 0xFED80300;

                            uint smbusBase = KernelDriver.ReadMemory<uint>(SB800_PIIX4_FCH_PM_ADDR);
                            IoPort = new IoPort((ushort)(((smbusBase >> 8) & 0xFF) << 8));
                        }
                        else if (Data.GetBit(smba_en_lo, 4)) {
                            // Primary bus
                            ushort piix4_smba = (ushort)(smba_en_hi << 8); // 0x0B00

                            if (piix4_smba != 0x00) {
                                IoPort = new IoPort(piix4_smba);
                            }
                        }
                    }

                    break;

                case VendorId.VIA:

                    // Find PCI to ISA bridge
                    PciDevice[] viaPciIsaBridge = FindDeviceByClass(BaseClassType.Bridge, SubClassType.Isa);

                    if (viaPciIsaBridge.Length == 0) {
                        return false;
                    }

                    PciDevice = viaPciIsaBridge[0];

                    // Assign smbus base address offset
                    byte viaSmbBa = 0;

                    switch (PciDevice.DeviceId) {
                        case DeviceId.VT8233:
                        case DeviceId.VT8233A:
                        case DeviceId.VT8235:
                        case DeviceId.VT8237A:
                        case DeviceId.VT8237R:
                        case DeviceId.VT8237S:
                        case DeviceId.VT8251:
                        case DeviceId.CX700:
                        case DeviceId.VX8x0:
                        case DeviceId.VX8x5:
                        case DeviceId.VX900:
                            viaSmbBa = 0xD0;
                            break;
                    }

                    if (viaSmbBa == 0) {
                        // Switch to ACPI Power Management system which includes an SMBus interface controller
                        PciDevice.Function = 4;

                        switch (PciDevice.DeviceId) {
                            case DeviceId.VT8231:
                            case DeviceId.VT82C596A:
                            case DeviceId.VT82C596B:
                            case DeviceId.VT82C686x:
                                viaSmbBa = 0x90;
                                break;
                        }
                    }

                    // Unsupported chipset
                    if (viaSmbBa == 0) {
                        return false;
                    }

                    // Update info
                    Info = new DeviceInfo {
                        VendorId = PciDevice.VendorId,
                        DeviceId = PciDevice.DeviceId
                    };

                    // Read SMBus I/O Base from base address offset
                    IoPort = new IoPort((ushort)(PciDevice.Read<ushort>(viaSmbBa) & 0xFFF0));

                    break;
            }

            if (PlatformType == Platform.Unknown) {
                return false;
            }

            // Common properties
            SMBuses     = FindBus();
            IsConnected = KernelDriver.IsRunning;

            if (IsConnected) {
                BusNumber = SMBuses[0];
            }

            return IsConnected;
        }

        /// <summary>
        /// Get platform information
        /// </summary>
        /// <returns>Platform and chipset Device/Vendor ID</returns>
        private DeviceInfo GetDeviceInfo() {

            DeviceInfo result  = new DeviceInfo();
            PciDevice platform = new PciDevice();

            result.VendorId = platform.VendorId;

            switch (result.VendorId) {
                case VendorId.Intel:
                case VendorId.VIA:
                    // Find ISA bridge to get chipset ID
                    try {
                        PciDevice isa = FindDeviceByClass(BaseClassType.Bridge, SubClassType.Isa)[0];
                        result.DeviceId = isa.DeviceId;
                    }
                    catch {
                        result.DeviceId = default;
                    }

                    break;

                case VendorId.AMD:
                case VendorId.Nvidia:
                    PciDevice smbus = FindDeviceByClass(BaseClassType.Serial, SubClassType.Smbus)[0];
                    result.DeviceId = smbus.DeviceId;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Locates available SMBuses on the device
        /// </summary>
        /// <returns>An array of bytes containing SMBus numbers</returns>
        public byte[] FindBus() {

            byte originalBus = BusNumber;

            try {
                Queue<byte> result = new Queue<byte>();

                for (byte i = 0; i <= 1; i++) {
                    BusNumber = i;
                    if (TryScan()) {
                        result.Enqueue(i); // The bus is valid
                    }
                }

                return result.ToArray();
            }
            catch {
                return new byte[0];
            }
            finally {
                BusNumber = originalBus;
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

            return Scan(minimumResults: true).Length > 0;
        }

        /// <summary>
        /// Validates slave address by reading first byte from it
        /// </summary>
        /// <param name="slaveAddress">Slave address</param>
        /// <returns><see langword="true"/> if <paramref name="slaveAddress"/> exists on the bus</returns>
        public bool ProbeAddress(byte slaveAddress) {
            return ReadByte(slaveAddress);
        }

        /// <summary>
        /// Gets DDR5 offline mode state
        /// </summary>
        /// <returns><see langword="true"/> if DDR5 RAM is in offline mode</returns>
        public bool GetOfflineMode() {
            return I2CAddress == 80 && GetOfflineMode(I2CAddress);
        }

        /// <summary>
        /// Gets DDR5 offline mode state
        /// </summary>
        /// <param name="slaveAddress">Slave address</param>
        /// <returns><see langword="true"/> if DDR5 at <paramref name="slaveAddress"/> is in offline mode</returns>
        public bool GetOfflineMode(byte slaveAddress) {
            return Data.GetBit(ReadByte(slaveAddress, Eeprom.Spd5Register.MR48), 2);
        }

        /// <summary>
        /// Scan SMBus for available slave devices
        /// </summary>
        /// <returns>An array of available bus addresses</returns>
        public byte[] Scan() {
            return Scan(minimumResults: false);
        }

        /// <summary>
        /// Scan SMBus for available slave devices
        /// </summary>
        /// <param name="minimumResults">Set to <see langword="true"/> to stop scanning once at least one slave address is found,
        /// or <see langword="false"/> to scan the entire range</param>
        /// <returns>An array of found bus addresses on <see cref="BusNumber"/></returns>
        private byte[] Scan(bool minimumResults) {

            Queue<byte> result = new Queue<byte>();

            for (byte i = 0; i <= 7; i++) {
                try {
                    byte address = (byte)(i + 0x50);
                    if (ProbeAddress(address)) {
                        result.Enqueue(address);
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
        /// <param name="slaveAddress">Slave address</param>
        /// <returns><see langword="true"/> if SMBus responds with
        /// <see cref="SmbStatus.Success"/> or <see cref="SmbStatus.Ready"/></returns>
        public bool ReadByte(byte slaveAddress) {

            SmbusData busData = new SmbusData {
                BusNumber   = BusNumber,
                Address     = slaveAddress,
                AccessMode  = SmbusAccessMode.Read,
                DataCommand = SmbusDataCommand.Byte,
            };

            try {
                return ExecuteCommand(ref busData);
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Reads a single byte from specified offset of specified slave address (disregarding page address)
        /// </summary>
        /// <param name="slaveAddress">Slave address</param>
        /// <param name="offset">Byte position</param>
        /// <returns>Byte value read from the device</returns>
        public byte ReadByte(byte slaveAddress, ushort offset) {

            SmbusData busData = new SmbusData {
                BusNumber   = BusNumber,
                Address     = slaveAddress,
                Offset      = offset,
                AccessMode  = SmbusAccessMode.Read,
                DataCommand = SmbusDataCommand.ByteData,
            };

            try {
                ExecuteCommand(ref busData);
                return busData.Output;
            }
            catch {
                throw new IOException($"Read error: {this}:{slaveAddress}:{offset}");
            }
        }

        /// <summary>
        /// Writes DNC data to the device
        /// </summary>
        /// <param name="slaveAddress">Slave address</param>
        /// <returns><see langword="true"/> if SMBus responds with
        /// <see cref="SmbStatus.Success"/> or <see cref="SmbStatus.Ready"/></returns>
        public bool WriteByte(byte slaveAddress) {

            SmbusData busData = new SmbusData {
                BusNumber   = BusNumber,
                Address     = slaveAddress,
                AccessMode  = SmbusAccessMode.Write,
                DataCommand = SmbusDataCommand.Byte,
            };

            try {
                return ExecuteCommand(ref busData);
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Write specified byte value to specified offset
        /// </summary>
        /// <param name="slaveAddress">Slave address</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langwod="true"/> if SMBus responds with
        /// <see cref="SmbStatus.Success"/> or <see cref="SmbStatus.Ready"/></returns>
        public bool WriteByte(byte slaveAddress, ushort offset, byte value) {

            SmbusData busData = new SmbusData {
                BusNumber   = BusNumber,
                Address     = slaveAddress,
                Offset      = offset,
                Input       = value,
                AccessMode  = SmbusAccessMode.Write,
                DataCommand = SmbusDataCommand.ByteData,
            };

            try {
                return ExecuteCommand(ref busData);
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Programs SMBus controller with input data 
        /// </summary>
        /// <param name="smbusData">SMBus input data</param>
        /// <returns><see langword="true"/> if SMBus doesn't return
        /// <see cref="SmbStatus.Error"/> or <see cref="SmbStatus.Timeout"/></returns>
        private bool ExecuteCommand(ref SmbusData smbusData) {

            lock (_smbusLock) {

                if (PlatformType == Platform.Unknown) {
                    return false;
                }
                
                try {
                    KernelDriver.LockHandle();

                    if (PlatformType == Platform.SkylakeX) {
                        // Set input for writing
                        if (smbusData.AccessMode == SmbusAccessMode.Write) {
                            PciDevice.Write(
                                offset : (byte)(SkylakeXSmbusRegister.Input + smbusData.BusNumber * 4),
                                value  : smbusData.Input);
                        }

                        // Set slave address, offset, and Execute command
                        PciDevice.Write(
                            offset : (byte)(SkylakeXSmbusRegister.Offset + smbusData.BusNumber * 4),
                            value  : (uint)(
                                // Enable byte read
                                0x20 << 24 |
                                // Execute command
                                (SmbusCmd.CmdByteData | SmbusCmd.Start) << 16 |
                                // Address and R/W mode
                                (smbusData.Address | (byte)(~(byte)smbusData.AccessMode << 7)) << 8 |
                                // Offset
                                (byte)smbusData.Offset
                            )
                        );

                        // Wait after writing
                        if (smbusData.AccessMode == SmbusAccessMode.Write) {
                            Thread.Sleep(Eeprom.ValidateEepromAddress(smbusData.Address)
                                ? ExecutionDelay.WriteDelay
                                : ExecutionDelay.WaitDelay);
                        }

                        // Wait for completion
                        if (!WaitForStatus(
                                statuses : new[] { SmbStatus.Ready, SmbStatus.Success, SmbStatus.Error },
                                timeout  : 1000)) {
                            smbusData.Status = SmbStatus.Timeout;
                            return false;
                        }

                        // Set status
                        smbusData.Status = GetBusStatus();

                        // Read and assign output if read mode is specified
                        if (smbusData.Status == SmbStatus.Error) {
                            return false;
                        }

                        if (smbusData.AccessMode == SmbusAccessMode.Read) {
                            smbusData.Output = PciDevice.Read<byte>(
                                offset : (byte)(SkylakeXSmbusRegister.Output + smbusData.BusNumber * 4));
                        }
                    }
                    else if (PlatformType == Platform.Default && Info.VendorId == VendorId.Nvidia) {

                        if (smbusData.BusNumber > 0) {
                            return false;
                        }

                        // Set Smbus address
                        IoPort.WriteEx(
                            offset : NvidiaSmbusRegister.Address,
                            value  : (byte)(smbusData.Address << 1));

                        // Set offset
                        IoPort.WriteEx(
                            offset : NvidiaSmbusRegister.Command,
                            value  : (byte)smbusData.Offset);

                        // Set data byte register if Write mode is set
                        if (smbusData.AccessMode == SmbusAccessMode.Write) {
                            IoPort.WriteEx(
                                offset : NvidiaSmbusRegister.Data,
                                value  : smbusData.Input);
                        }

                        // Protocol command
                        byte protocolCmd = 0x00;

                        // Check access mode and set protocol mode bit accordingly
                        protocolCmd |= (byte)smbusData.AccessMode;

                        // Command type
                        switch (smbusData.DataCommand) {
                            case SmbusDataCommand.Quick:
                                protocolCmd |= NvidiaSmbusProtocol.Quick;
                                break;
                            case SmbusDataCommand.Byte:
                                protocolCmd |= NvidiaSmbusProtocol.Byte;
                                break;
                            case SmbusDataCommand.ByteData:
                            default:
                                protocolCmd |= NvidiaSmbusProtocol.ByteData;
                                break;
                            case SmbusDataCommand.WordData:
                                protocolCmd |= NvidiaSmbusProtocol.WordData;
                                break;
                        }

                        // Execute command
                        IoPort.WriteEx(
                            offset : NvidiaSmbusRegister.Protocol,
                            value  : protocolCmd);

                        // Wait after writing
                        if (smbusData.AccessMode == SmbusAccessMode.Write) {
                            Thread.Sleep(Eeprom.ValidateEepromAddress(smbusData.Address)
                                ? ExecutionDelay.WriteDelay
                                : ExecutionDelay.WaitDelay);
                        }

                        // Wait for completion
                        if (!WaitForStatus(
                                statuses : new[] { SmbStatus.Success, SmbStatus.Error },
                                timeout  : 1000)) {
                            smbusData.Status = SmbStatus.Timeout;
                            return false;
                        }

                        // Set status
                        smbusData.Status = GetBusStatus();

                        if (smbusData.Status == SmbStatus.Error) {
                            return false;
                        }

                        // Read and assign output if read mode is specified
                        if (smbusData.AccessMode == SmbusAccessMode.Read) {
                            smbusData.Output = IoPort.Read<byte>(NvidiaSmbusRegister.Data);
                        }
                    }
                    else if (PlatformType == Platform.Default && Info.VendorId != VendorId.Nvidia) {
                        // These platforms don't support multiple SMbuses
                        if ((Info.VendorId == VendorId.Intel || Info.VendorId == VendorId.VIA) && smbusData.BusNumber > 0) {
                            return false;
                        }

                        // Alternative AMD SMBus
                        byte portOffset = 0;

                        if (Info.VendorId == VendorId.AMD) {
                            portOffset = (byte)(BusNumber * 20);
                        }

                        // Clear status bitmask to reset status
                        byte clearStatusMask = SmbusStatus.Interrupt |
                                               SmbusStatus.DeviceErr |
                                               SmbusStatus.BusCollision |
                                               SmbusStatus.Failed;

                        // Stop current transaction
                        IoPort.WriteEx(
                            offset : (byte)(DefaultSmbusRegister.Control + portOffset),
                            value  : SmbusCmd.Stop);

                        // Reset status
                        IoPort.WriteEx(
                            offset : (byte)(DefaultSmbusRegister.Status + portOffset),
                            value  : clearStatusMask);

                        // Wait for ready status
                        if (!WaitForStatus(SmbStatus.Ready, 1000)) {
                            smbusData.Status = SmbStatus.Timeout;
                            return false;
                        }

                        // Set slave address
                        IoPort.WriteEx(
                            offset : (byte)(DefaultSmbusRegister.Address + portOffset),
                            value  : (byte)(smbusData.Address << 1 | (byte)smbusData.AccessMode));

                        // Set input data for writing
                        if (smbusData.AccessMode == SmbusAccessMode.Write) {
                            IoPort.WriteEx(
                                offset : (byte)(DefaultSmbusRegister.Data0 + portOffset),
                                value  : smbusData.Input);
                        }

                        // Set offset
                        IoPort.WriteEx(
                            offset : (byte)(DefaultSmbusRegister.HostCmd + portOffset),
                            value  : (byte)smbusData.Offset);

                        // Command type
                        byte smbusDataCmd;

                        switch (smbusData.DataCommand) {
                            case SmbusDataCommand.Quick:
                                smbusDataCmd = SmbusCmd.CmdQuick;
                                break;
                            case SmbusDataCommand.Byte:
                                smbusDataCmd = SmbusCmd.CmdByte;
                                break;
                            case SmbusDataCommand.ByteData:
                            default:
                                smbusDataCmd = SmbusCmd.CmdByteData;
                                break;
                            case SmbusDataCommand.WordData:
                                smbusDataCmd = SmbusCmd.CmdWordData;
                                break;
                        }

                        // Execute
                        IoPort.WriteEx(
                            offset : (byte)(DefaultSmbusRegister.Control + portOffset),
                            value  : (byte)(SmbusCmd.Interrupt | smbusDataCmd | SmbusCmd.Start));

                        // Wait after writing
                        if (smbusData.AccessMode == SmbusAccessMode.Write) {
                            Thread.Sleep(Eeprom.ValidateEepromAddress(smbusData.Address)
                                ? ExecutionDelay.WriteDelay
                                : ExecutionDelay.WaitDelay);
                        }

                        // Wait for completion
                        if (!WaitForStatus(
                                statuses : new[] { SmbStatus.Ready, SmbStatus.Success, SmbStatus.Error },
                                timeout  : 1000)) {

                            // Abort current execution
                            IoPort.WriteEx((byte)(DefaultSmbusRegister.Control + portOffset), SmbusCmd.Stop);
                            smbusData.Status = SmbStatus.Timeout;
                            return false;
                        }

                        // Set status
                        smbusData.Status = GetBusStatus();

                        // Check status
                        if (smbusData.Status == SmbStatus.Error) {
                            return false;
                        }

                        // Read and assign output if read mode is specified
                        if (smbusData.AccessMode == SmbusAccessMode.Read) {
                            smbusData.Output = IoPort.Read<byte>(offset: DefaultSmbusRegister.Data0);
                        }
                    }
                }
                finally {
                    KernelDriver.UnlockHandle();
                }

                return true;
            }
        }

        /// <summary>
        /// Smbus lock to prevent SMBus access from multiple threads simultaneously
        /// </summary>
        private static object _smbusLock = new object();

        /// <summary>
        /// SMBus Host Control Register data
        /// </summary>
        public struct SmbusCmd {

            /// <summary>
            /// Initiates the Smbus command 
            /// </summary>
            public const byte Start       = 1 << 6;

            /// <summary>
            /// Stops the current host transaction in process
            /// </summary>
            public const byte Stop        = 1 << 1;

            /// <summary>
            /// Enables the generation of an interrupt upon the completion of the command
            /// </summary>
            public const byte Interrupt   = 1 << 0;

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
        }

        /// <summary>
        /// SMBus data for <see cref="ExecuteCommand"/>
        /// </summary>
        public struct SmbusData {
            internal byte BusNumber;
            internal byte Address;
            internal ushort Offset;
            internal SmbusAccessMode AccessMode;
            internal SmbusDataCommand DataCommand;
            internal byte Input;
            internal byte Output;
            internal SmbStatus Status;
        }

        /// <summary>
        /// SMBus Read/Write access
        /// </summary>
        public enum SmbusAccessMode : byte {
            Write,
            Read
        }

        /// <summary>
        /// Smbus command type
        /// </summary>
        public enum SmbusDataCommand : byte {
            Quick,
            Byte,
            ByteData,
            WordData,
        }

        /// <summary>
        /// Command execution delays
        /// </summary>
        private struct ExecutionDelay {
            internal static readonly byte WriteDelay = 25;
            internal static readonly byte WaitDelay  =  0;
        }

        /// <summary>
        /// Gets SMBus status
        /// </summary>
        /// <returns>SMBus status</returns>
        public SmbStatus GetBusStatus() {

            byte status;

            if (PlatformType == Platform.SkylakeX) {
                status = PciDevice.Read<byte>((byte)(SkylakeXSmbusRegister.Status + BusNumber * 4));

                if ((status & SkylakeXSmbusStatus.Busy) > 0) {
                    return SmbStatus.Busy;
                }

                if ((status & SkylakeXSmbusStatus.Nack) > 0) {
                    return SmbStatus.Error;
                }

                if ((status & SkylakeXSmbusStatus.Complete) > 0) {
                    return SmbStatus.Success;
                }

                return SmbStatus.Ready;
            }

            if (Info.VendorId == VendorId.Nvidia) {

                if (IoPort.Read<byte>(NvidiaSmbusRegister.Protocol) != 0) {
                    return SmbStatus.Busy;
                }

                status = IoPort.Read<byte>(NvidiaSmbusRegister.Status);

                switch (status) {
                    case NvidiaSmbusStatus.Done:
                        return SmbStatus.Success;
                    case NvidiaSmbusStatus.Error:
                    case NvidiaSmbusStatus.Invalid:
                        return SmbStatus.Error;
                }
            }

            if (PlatformType == Platform.Default) {

                status = IoPort.Read<byte>(DefaultSmbusRegister.Status);

                // Check status flags
                if ((status & (SmbusStatus.Failed |
                               SmbusStatus.BusCollision |
                               SmbusStatus.DeviceErr |
                               SmbusStatus.Interrupt |
                               SmbusStatus.HostBusy)) == 0) {
                    return SmbStatus.Ready;
                }

                // Check busy flag
                if ((status & SmbusStatus.HostBusy) == SmbusStatus.HostBusy) {
                    return SmbStatus.Busy;
                }

                // Check for errors
                if ((status & (SmbusStatus.Failed |
                               SmbusStatus.BusCollision |
                               SmbusStatus.DeviceErr)) > 0) {
                    return SmbStatus.Error;
                }

                // Check Interrupt flag
                if ((status & SmbusStatus.Interrupt) == SmbusStatus.Interrupt) {
                    return SmbStatus.Success;
                }
            }

            return SmbStatus.Error;
        }

        /// <summary>
        /// SMBus status timeout handler
        /// </summary>
        /// <param name="status">Desired SMBus status</param>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <returns><see langword="true"/> if SMBus status matches <paramref name="status"/> before timeout occurs</returns>
        private bool WaitForStatus(SmbStatus status, int timeout) {

            Stopwatch sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeout) {
                if (GetBusStatus() == status) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// SMBus status timeout handler
        /// </summary>
        /// <param name="statuses">An array of SMBus statuses to wait for</param>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <returns><see langword="true"/> if SMBus status matches one of <paramref name="statuses"/> before timeout occurs</returns>
        private bool WaitForStatus(SmbStatus[] statuses, int timeout) {
            Stopwatch sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeout) {
                foreach (SmbStatus status in statuses) {
                    if (GetBusStatus() == status) {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// SMBus status
        /// </summary>
        public enum SmbStatus : byte {
            Ready,
            Busy,
            Error,
            Success,
            Timeout,
            Aborted,
        }

        /// <summary>
        /// Host status (<see cref="DefaultSmbusRegister.Status"/>) register bits description
        /// </summary>
        public struct SmbusStatus {

            /// <summary>
            /// Indicates the SMBus controller is in the process of completing a command
            /// </summary>
            public const byte HostBusy     = 1 << 0;

            /// <summary>
            /// Indicates the successful completion of the last host command
            /// </summary>
            public const byte Interrupt    = 1 << 1;

            /// <summary>
            /// Indicates an error: illegal command, unclaimed cycle, or timeout
            /// </summary>
            public const byte DeviceErr    = 1 << 2;

            /// <summary>
            /// Indicates an SMBus transaction collision
            /// </summary>
            public const byte BusCollision = 1 << 3;

            /// <summary>
            /// Indicates a failed bus transaction
            /// </summary>
            public const byte Failed       = 1 << 4;
        }

        /// <summary>
        /// Gets supported platform maximum SPD size
        /// </summary>
        /// <returns>SPD size</returns>
        private ushort GetMaxSpdSize(byte address) {

            if (IsDdr5Present) {
                return Spd.DataLength.DDR5;
            }

            // Read dram device type byte
            byte ramTypeByte = ReadByte(address, 2);

            // SPD header for GetRamType
            byte[] spdHeader = { 0x00, 0x00, ramTypeByte };

            // Check if dram device type byte value is in the Ram.Type enum
            return (ushort)(Enum.IsDefined(typeof(Spd.RamType), (Spd.RamType)ramTypeByte)
                ? Spd.GetSpdSize(Spd.GetRamType(spdHeader))
                : Spd.DataLength.Minimum); // DDR3 and older
        }
    }
}