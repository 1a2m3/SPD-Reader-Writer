/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.String;
using static SpdReaderWriterCore.Data;

namespace SpdReaderWriterCore {

    /// <summary>
    /// Defines Device class, properties, and methods to handle the communication with the device
    /// </summary>
    public class Arduino : IDisposable, IDevice {

        /// <summary>
        /// Initializes default Arduino device instance
        /// </summary>
        public Arduino() {
            PortSettings = new SerialPortSettings();
        }

        /// <summary>
        /// Initializes Arduino device instance
        /// </summary>
        /// <param name="portName">Serial port name</param>
        public Arduino(string portName) {
            PortSettings = new SerialPortSettings();
            PortName     = portName;
        }

        /// <summary>
        /// Initializes Arduino device instance
        /// </summary>
        /// <param name="portName">Serial port name</param>
        /// <param name="baudRate">Serial port baud rate</param>
        public Arduino(string portName, int baudRate) {
            SerialPortSettings ps = new SerialPortSettings {
                BaudRate = baudRate
            };
            PortSettings = ps;
            PortName = portName;
        }

        /// <summary>
        /// Initializes Arduino device instance
        /// </summary>
        /// <param name="portSettings">Serial port settings</param>
        public Arduino(SerialPortSettings portSettings) {
            PortSettings = portSettings;
        }

        /// <summary>
        /// Initializes Arduino device instance
        /// </summary>
        /// <param name="portSettings">Serial port settings</param>
        /// <param name="portName">Serial port name</param>
        public Arduino(SerialPortSettings portSettings, string portName) {
            PortSettings = portSettings;
            PortName     = portName;
        }

        /// <summary>
        /// Initializes Arduino device instance
        /// </summary>
        /// <param name="portSettings">Serial port settings</param>
        /// <param name="portName">Serial port name</param>
        /// <param name="i2CAddress">EEPROM address on the device's i2c bus</param>
        public Arduino(SerialPortSettings portSettings, string portName, byte i2CAddress) {
            PortSettings = portSettings;
            PortName     = portName;
            I2CAddress   = i2CAddress;
        }

        /// <summary>
        /// Arduino device instance string
        /// </summary>
        /// <returns>Arduino device instance string</returns>
        public override string ToString() =>
            IsNullOrEmpty(PortName) ? "N/A" : $"{PortName}:{PortSettings.BaudRate}";

        /// <summary>
        /// Arduino device instance destructor
        /// </summary>
        ~Arduino() {
            Dispose();
        }

        /// <summary>
        /// Serial Port Settings class
        /// </summary>
        public struct SerialPortSettings {
            // Connection settings
            public int  BaudRate;
            public bool DtrEnable;
            public bool RtsEnable;

            // Response settings
            public int Timeout;

            /// <summary>
            /// Default port settings
            /// </summary>
            /// <param name="baudRate">Baud rate</param>
            /// <param name="dtrEnable">Enable DTR</param>
            /// <param name="rtsEnable">Enable RTS</param>
            /// <param name="timeout">Response timeout in seconds</param>
            public SerialPortSettings(
                int baudRate   = 115200,
                bool dtrEnable = true,
                bool rtsEnable = true,
                int timeout    = 10) {
                    BaudRate  = baudRate;
                    DtrEnable = dtrEnable;
                    RtsEnable = rtsEnable;
                    Timeout   = timeout;
            }

            /// <summary>
            /// Serial port settings string
            /// </summary>
            /// <returns>Serial port settings string</returns>
            public override string ToString() {
                return $"{BaudRate:D}";
            }
        }

        /// <summary>
        /// Supported baud rates
        /// </summary>
        public static int[] BaudRates = { 
            300,
            600,
            1200,
            2400,
            4800,
            9600,
            14400,
            19200,
            38400,
            57600,
            115200,
            230400,
            250000,
            460800,
            500000,
            1000000,
            2000000,
        };

        /// <summary>
        /// Number of bytes received from the device
        /// </summary>
        public int BytesReceived => _bytesReceived;

        /// <summary>
        /// Number of bytes sent to the device
        /// </summary>
        public int BytesSent => _bytesSent;

        /// <summary>
        /// Attempts to establish a connection with the SPD reader/writer device
        /// </summary>
        /// <returns><see langword="true"/> if the connection is established</returns>
        public bool Connect() {

            lock (_portLock) {
                if (IsConnected) {
                    return IsConnected;
                }

                _sp = new SerialPort {
                    // New connection settings
                    PortName               = PortName,
                    BaudRate               = PortSettings.BaudRate,
                    DtrEnable              = PortSettings.DtrEnable,
                    RtsEnable              = PortSettings.RtsEnable,
                    ReadTimeout            = 1000,
                    WriteTimeout           = 1000,
                    ReceivedBytesThreshold = PacketData.MinSize,
                };

                // Event to handle Data Reception
                _sp.DataReceived  += DataReceivedHandler;

                // Event to handle Errors
                _sp.ErrorReceived += ErrorReceivedHandler;
                
                try {
                    // Establish connection
                    _sp.Open();

                    // Reset stats
                    _bytesSent     = 0;
                    _bytesReceived = 0;

                    // Wait till ready signal
                    Stopwatch sw = Stopwatch.StartNew();
                    while (!IsReady && sw.ElapsedMilliseconds < PortSettings.Timeout * 1000) {
                        Thread.Sleep(10);
                    }

                    if (!IsReady) {
                        return false;
                    }

                    // Test the device
                    if (Test()) {
                        new Thread(ConnectionMonitor) {
#if DEBUG
                            Name = "ConnectionMonitor",
#endif
                            Priority = ThreadPriority.BelowNormal,
                        }.Start();

                        _connectionEstablished.Set();
                    }
                    else {
                        Dispose();
                        return false;
                    }
                }
                catch (Exception ex) {
                    throw new Exception($"Unable to connect ({PortName}): {ex.Message}");
                }
            }

            return IsConnected;
        }

        /// <summary>
        /// Disconnects the SPD reader/writer device
        /// </summary>
        /// <returns><see langword="true"/> once the device is disconnected</returns>
        public bool Disconnect() {
            lock (_portLock) {
                if (!IsConnected) {
                    return false;
                }

                try {
                    // Remove handlers
                    _sp.DataReceived  -= DataReceivedHandler;
                    _sp.ErrorReceived -= ErrorReceivedHandler;
                    Dispose();
                }
                catch (Exception ex) {
                    throw new Exception($"Unable to disconnect ({PortName}): {ex.Message}");
                }

                return !IsConnected;
            }
        }

        /// <summary>
        /// Disposes device instance
        /// </summary>
        public void Dispose() {
            lock (_portLock) {
                if (IsConnected) {
                    _sp.Close();
                }

                _connectionEstablished.Reset();

                if (_sp != null) {
                    _sp.DataReceived  -= DataReceivedHandler;
                    _sp.ErrorReceived -= ErrorReceivedHandler;
                    _sp = null;
                }

                IsReady          = false;
                _addresses       = null;
                _rswpTypeSupport = 0;
            }
        }

        /// <summary>
        /// Tests if the device responds to a test command
        /// </summary>
        /// <returns><see langword="true"/> if the device responds properly to a test command</returns>
        public bool Test() {
            lock (_portLock) {
                try {
                    return ExecuteCommand<bool>(Command.Test);
                }
                catch (Exception e) {
                    throw new Exception($"Unable to test {PortName} ({e.Message})");
                }
            }
        }

        /// <summary>
        /// Gets supported RAM type(s)
        /// </summary>
        /// <returns>A bitmask representing available RAM supported defined in the <see cref="RswpSupport"/> struct</returns>
        public byte GetRswpSupport() {
            lock (_portLock) {
                try {
                    return ExecuteCommand<byte>(Command.RswpReport);
                }
                catch (Exception e) {
                    throw new Exception($"Unable to get {PortName} supported RAM ({e.Message})");
                }
            }
        }

        /// <summary>
        /// Test if the device supports RAM type RSWP at firmware level
        /// </summary>
        /// <param name="rswpTypeBitmask">RAM type bitmask</param>
        /// <returns><see langword="true"/> if the device supports RSWP</returns>
        public bool GetRswpSupport(byte rswpTypeBitmask) {
            return (GetRswpSupport() & rswpTypeBitmask) == rswpTypeBitmask;
        }

        /// <summary>
        /// Reads a byte from the device
        /// </summary>
        /// <returns>A single byte value received from the device</returns>
        public byte ReadIncomingByte() {
            int value = _sp.ReadByte();
            
            if (value != -1) {
                return (byte)(value & 0xFF);
            }

            throw new EndOfStreamException("No data to read");
        }

        /// <summary>
        /// I2C addresses available on Arduino
        /// </summary>
        public byte[] Addresses {
            get {
                _addresses = Scan();

                if (_addresses.Length == 0) {
                    I2CAddress = 0;
                }

                return _addresses;
            }
        }

        /// <summary>
        /// Scans the device for I2C bus devices
        /// </summary>
        /// <returns>An array of addresses on the device's I2C bus</returns>
        public byte[] Scan() {
            Queue<byte> addresses = new Queue<byte>();

            lock (_portLock) {
                try {
                    if (IsConnected) {
                        byte response = ExecuteCommand<byte>(Command.ScanBus);

                        if (response == 0) {
                            return new byte[0];
                        }

                        for (byte i = 0; i <= 7; i++) {
                            if (GetBit(response, i)) {
                                addresses.Enqueue((byte)(80 + i));
                            }
                        }
                    }
                }
                catch {
                    throw new Exception($"Unable to scan I2C bus on {PortName}");
                }
            }

            return addresses.ToArray();
        }

        /// <summary>
        /// Sets clock frequency for I2C communication
        /// </summary>
        /// <param name="fastMode">Fast mode or standard mode</param>
        /// <returns><see langword="true"/> if the operation is successful</returns>
        public bool SetI2CClock(bool fastMode) {
            lock (_portLock) {
                try {
                    return ExecuteCommand<bool>(Command.BusClock, fastMode);
                }
                catch {
                    throw new Exception($"Unable to set I2C clock mode on {PortName}");
                }
            }
        }

        /// <summary>
        /// Gets current device I2C clock mode
        /// </summary>
        /// <returns><see langword="true"/> if the device's I2C bus is running in fast mode,
        /// or <see langword="false"/> if it is running in standard mode</returns>
        private bool GetI2CClock() {
            lock (_portLock) {
                try {
                    return ExecuteCommand<bool>(Command.BusClock, Command.Get);
                }
                catch {
                    throw new Exception($"Unable to get I2C clock mode on {PortName}");
                }
            }
        }

        /// <summary>
        /// Gets current device I2C clock
        /// </summary>
        public ushort I2CClock => GetI2CClock() ? ClockMode.Fast : ClockMode.Standard;

        /// <summary>
        /// Resets the device's settings to defaults
        /// </summary>
        /// <returns><see langword="true"/> once the device's settings are successfully reset to defaults</returns>
        public bool FactoryReset() {
            lock (_portLock) {
                try {
                    return ExecuteCommand<bool>(Command.FactoryReset);
                }
                catch {
                    throw new Exception($"Unable to reset device settings on {PortName}");
                }
            }
        }

        /// <summary>
        /// Gets or sets SA1 control pin
        /// </summary>
        public bool PIN_SA1 {
            get => GetConfigPin(ConfigPin.SA1_SWITCH);
            set => SetConfigPin(ConfigPin.SA1_SWITCH, value);
        }

        /// <summary>
        /// Gets or sets High voltage control pin
        /// </summary>
        public bool PIN_VHV {
            get => GetHighVoltage();
            set => SetHighVoltage(value);
        }

        /// <summary>
        /// Controls high voltage state on pin SA0
        /// </summary>
        /// <param name="state">High voltage supply state</param>
        /// <returns><see langword="true"/> when operation is successful</returns>
        public bool SetHighVoltage(bool state) {
            lock (_portLock) {
                try {
                    return ExecuteCommand<bool>(Command.PinControl, ConfigPin.HV_SWITCH, state);
                }
                catch {
                    throw new Exception($"Unable to set High Voltage state on {PortName}");
                }
            }
        }

        /// <summary>
        /// Gets high voltage state on pin SA0
        /// </summary>
        /// <returns><see langword="true"/> if high voltage is applied to pin SA0</returns>
        public bool GetHighVoltage() {
            lock (_portLock) {
                try {
                    return ExecuteCommand<bool>(Command.PinControl, ConfigPin.HV_SWITCH, Command.Get);
                }
                catch {
                    throw new Exception($"Unable to get High Voltage state on {PortName}");
                }
            }
        }

        /// <summary>
        /// Sets specified configuration pin to desired state
        /// </summary>
        /// <param name="pin">Pin name</param>
        /// <param name="state">Pin state</param>
        /// <returns><see langword="true"/> if the config pin has been set</returns>
        public bool SetConfigPin(ConfigPin pin, bool state) {
            lock (_portLock) {
                try {
                    return ExecuteCommand<bool>(Command.PinControl, pin, state);
                }
                catch {
                    throw new Exception($"Unable to set config pin state on {PortName}");
                }
            }
        }

        /// <summary>
        /// Get specified configuration pin state
        /// </summary>
        /// <returns><see langword="true"/> if pin is high, or <see langword="false"/> when pin is low</returns>
        public bool GetConfigPin(ConfigPin pin) {
            lock (_portLock) {
                try {
                    return ExecuteCommand<bool>(Command.PinControl, pin, Command.Get);
                }
                catch {
                    throw new Exception($"Unable to get config pin state on {PortName}");
                }
            }
        }

        /// <summary>
        /// Resets all config pins to their default state
        /// </summary>
        /// <returns><see langword="true"/> when all config pins are reset</returns>
        public bool ResetConfigPins() {
            lock (_portLock) {
                try {
                    return ExecuteCommand<bool>(Command.PinReset);
                }
                catch {
                    throw new Exception($"Unable to reset pin state on {PortName}");
                }
            }
        }

        /// <summary>
        /// Gets DDR5 offline mode status
        /// </summary>
        /// <returns><see langword="true"/> when DDR5 is in offline mode</returns>
        public bool GetOfflineMode() {
            lock (_portLock) {
                try {
                    return GetRswpSupport(RswpSupport.DDR5);
                }
                catch {
                    throw new Exception($"Unable to get offline mode status on {PortName}");
                }
            }
        }

        /// <summary>
        /// Probes default EEPROM address
        /// </summary>
        /// <returns><see langword="true"/> if EEPROM is detected at assigned <see cref="I2CAddress"/></returns>
        public bool ProbeAddress() {
            return Eeprom.ValidateEepromAddress(I2CAddress) && ProbeAddress(I2CAddress);
        }

        /// <summary>
        /// Probes specified EEPROM address
        /// </summary>
        /// <param name="busAddress">EEPROM address</param>
        /// <returns><see langword="true"/> if EEPROM is detected at the specified <see cref="I2CAddress"/></returns>
        public bool ProbeAddress(byte busAddress) {
            lock (_portLock) {
                try {
                    return ExecuteCommand<bool>(Command.ProbeAddress, busAddress);
                }
                catch {
                    throw new Exception($"Unable to probe address {busAddress} on {PortName}");
                }
            }
        }

        /// <summary>
        /// Clears serial port buffers from unneeded data to prevent unwanted behavior and delays
        /// </summary>
        private void ClearBuffer() {
            lock (_portLock) {
                try {
                    // Clear receive buffer
                    if (BytesToRead > 0) {
                        _sp.DiscardInBuffer();
                    }

                    // Clear transmit buffer
                    if (BytesToWrite > 0) {
                        _sp.DiscardOutBuffer();
                    }
                }
                catch {
                    throw new Exception($"Unable to clear {PortName} buffer");
                }
            }
        }

        /// <summary>
        /// Device's firmware version
        /// </summary>
        public int FirmwareVersion => GetFirmwareVersion();

        /// <summary>
        /// Get device's firmware version 
        /// </summary>
        /// <returns>Firmware version number</returns>
        private int GetFirmwareVersion() {
            lock (_portLock) {
                try {
                    return ExecuteCommand<int>(Command.Version);
                }
                catch (Exception ex) {
                    throw new Exception($"Unable to get firmware version on {PortName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Included firmware version number
        /// </summary>
        public static int IncludedFirmwareVersion => GetIncludedFirmwareVersion();

        /// <summary>
        /// Required firmware version number
        /// </summary>
        public static int RequiredFirmwareVersion => IncludedFirmwareVersion;

        /// <summary>
        /// Gets included firmware version number
        /// </summary>
        /// <returns>Included firmware version number</returns>
        private static int GetIncludedFirmwareVersion() {
            try {
                byte[] fwHeader         = GzipPeek(Resources.Firmware.FirmwareFile.RawData, 1024);
                int versionLength       = (int)DataSize.Dword * 2;
                Regex versionPattern    = new Regex($@"([\d]{{{versionLength}}})"); // ([\d]{8})
                MatchCollection matches = versionPattern.Matches(BytesToString(fwHeader));

                if (matches.Count > 0 && 
                    matches[0].Length == versionLength &&
                    int.TryParse(matches[0].Value, out int includedFwVersion)) {
                    return includedFwVersion;
                }

                throw new Exception();
            }
            catch {
                throw new Exception("Unable to get included firmware version number");
            }
        }

        /// <summary>
        /// Device's user assigned name
        /// </summary>
        public string Name {
            get => GetName();
            set => SetName(value);
        }

        /// <summary>
        /// Maximum name length
        /// </summary>
        public static readonly byte NameLength = 16;

        /// <summary>
        /// Assigns a name to the Device
        /// </summary>
        /// <param name="name">Device name</param>
        /// <returns><see langword="true"/> when the device name is set</returns>
        private bool SetName(string name) {
            
            if (name == null) {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length == 0) {
                throw new ArgumentException("Name can't be blank");
            }
            if (name.Length > NameLength) {
                throw new ArgumentOutOfRangeException($"Name can't be longer than {NameLength} characters");
            }

            lock (_portLock) {
                try {
                    string newName = name.Trim();

                    if (newName == GetName()) {
                        return false;
                    }

                    // Prepare a byte array containing cmd byte + name length + name
                    byte[] command = { (byte)Command.Name, (byte)newName.Length };

                    return ExecuteCommand<bool>(MergeArray(command, Encoding.ASCII.GetBytes(newName)));
                }
                catch {
                    throw new Exception($"Unable to assign name to {PortName}");
                }
            }
        }

        /// <summary>
        /// Gets Device's name
        /// </summary>
        /// <returns>Device's name</returns>
        private string GetName() {
            lock (_portLock) {
                try {
                    return ExecuteCommand<string>(Command.Name, Command.Get).Trim();
                }
                catch (Exception e) {
                    throw new Exception($"Unable to get {PortName} name ({e.Message})");
                }
            }
        }

        /// <summary>
        /// Reads Arduino's internal EEPROM memory
        /// </summary>
        /// <param name="offset">EEPROM offset</param>
        /// <param name="length">Number of bytes to read</param>
        /// <returns>Device's EEPROM contents at <paramref name="offset"/></returns>
        public byte[] ReadEeprom(ushort offset, ushort length) {
            lock (_portLock) {
                try {
                    return ExecuteCommand<byte[]>(
                        Command.Eeprom, 
                        Command.Get, 
                        offset >> 8, offset & 0xFF, // offset MSB and LSB
                        length >> 8, length & 0xFF  // length MSB and LSB
                    );
                }
                catch (Exception e) {
                    throw new Exception($"Unable to read EEPROM on {PortName} ({e.Message})");
                }
            }
        }

        /// <summary>
        /// Finds Arduinos connected to computer
        /// </summary>
        /// <param name="baudRate">Serial port baud rate</param>
        /// <returns>An array of Arduinos</returns>
        public static Arduino[] Find(int baudRate) {
            SerialPortSettings spSettings = new SerialPortSettings {
                BaudRate = baudRate
            };

            return Find(spSettings);
        }

        /// <summary>
        /// Finds Arduinos connected to computer
        /// </summary>
        /// <param name="settings">Serial port settings</param>
        /// <returns>An array of Arduinos</returns>
        public static Arduino[] Find(SerialPortSettings settings) {
            Stack<Arduino> result = new Stack<Arduino>();

            string[] ports = SerialPort.GetPortNames().Distinct().ToArray();

            // Sort results in numeric order
            int[] portsNumbers = new int[ports.Length];
            for (int i = 0; i < ports.Length; i++) {
                portsNumbers[i] = StringToNum<int>(ports[i].Replace("COM", ""));
            }

            Array.Sort(portsNumbers);

            for (int i = 0; i < ports.Length; i++) {
                ports[i] = $"COM{portsNumbers[i]}";
            }

            Parallel.ForEach(ports, portName => {
                using (Arduino device = new Arduino(settings, portName)) {
                    try {
                        if (device.Connect()) {
                            result.Push(device);
                        }
                    }
                    catch {
                        return;
                    }
                }
            });

            return result.ToArray();
        }

        /// <summary>
        /// Describes device's connection state
        /// </summary>
        public bool IsConnected {
            get {
                try {
                    return _sp != null && _sp.IsOpen;
                }
                catch {
                    return false;
                }
            }
        }

        /// <summary>
        /// Describes device's ready state
        /// </summary>
        public bool IsReady { get; private set; }

        /// <summary>
        /// Detects if DDR4 RAM is present on the device's I2C bus at specified <see cref="I2CAddress"/>
        /// </summary>
        /// <returns><see langword="true"/> if DDR4 is found at <see cref="I2CAddress"/></returns>
        public bool DetectDdr4() {
            lock (_portLock) {
                try {
                    return ExecuteCommand<bool>(Command.Ddr4Detect, I2CAddress);
                }
                catch {
                    throw new Exception($"Error detecting DDR4 on {PortName}");
                }
            }
        }

        /// <summary>
        /// Detects if DDR5 RAM is present on the device's I2C bus at specified <see cref="I2CAddress"/>
        /// </summary>
        /// <returns><see langword="true"/> if DDR5 is found at <see cref="I2CAddress"/></returns>
        public bool DetectDdr5() {
            lock (_portLock) {
                try {
                    return ExecuteCommand<bool>(Command.Ddr5Detect, I2CAddress);
                }
                catch {
                    throw new Exception($"Error detecting DDR5 on {PortName}");
                }
            }
        }

        /// <summary>
        /// Reads SPD5 hub register data
        /// </summary>
        /// <param name="register">Register address</param>
        /// <returns>Register data</returns>
        public byte ReadSpd5Hub(byte register) {
            lock (_portLock) {
                try {
                    return ExecuteCommand<byte>(Command.Spd5HubReg, I2CAddress, register, Command.Get);
                }
                catch {
                    throw new Exception($"Unable to read SPD5 hub on {PortName}");
                }
            }
        }

        /// <summary>
        /// Writes data to SPD5 hub register
        /// </summary>
        /// <param name="register">Register address</param>
        /// <param name="value">Register value</param>
        /// <returns><see langword="true"/> if command is successfully executed</returns>
        public bool WriteSpd5Hub(byte register, byte value) {
            lock (_portLock) {
                try {
                    bool result = ExecuteCommand<bool>(Command.Spd5HubReg, I2CAddress, register, Command.Enable, value);

                    if (register == Eeprom.Spd5Register.MR12 ||
                        register == Eeprom.Spd5Register.MR13) {
                        return result && ReadSpd5Hub(register) == value;
                    }

                    return result;
                }
                catch {
                    throw new Exception($"Unable to read SPD5 hub on {PortName}");
                }
            }
        }

        /// <summary>
        /// Gets EEPROM size on specified address
        /// </summary>
        /// <returns>EEPROM size on <see cref="I2CAddress"/></returns>
        public ushort GetSpdSize() {
            lock (_portLock) {
                try {
                    return Spd.DataLength.Length[ExecuteCommand<byte>(Command.Size, I2CAddress)];
                }
                catch {
                    throw new Exception($"Unable to get SPD size on {PortName}:{I2CAddress}");
                }
            }
        }

        /// <summary>
        /// Serial Port connection and data settings
        /// </summary>
        public SerialPortSettings PortSettings { get; set; }

        /// <summary>
        /// Serial port name the device is connected to
        /// </summary>
        public string PortName { get; }

        /// <summary>
        /// EEPROM address
        /// </summary>
        public byte I2CAddress {
            get => _i2CAddress;
            set {
                _i2CAddress = value;

                if (IsConnected && (Eeprom.ValidateEepromAddress(_i2CAddress) || 
                                    Eeprom.ValidatePmicAddress(_i2CAddress))) {
                    MaxSpdSize = GetSpdSize();
                }
            }
        }
        private byte _i2CAddress;

        /// <summary>
        /// EEPROM size
        /// </summary>
        public ushort MaxSpdSize { get; private set; }

        /// <summary>
        /// Number of bytes to be read from the device
        /// </summary>
        public int BytesToRead {
            get {
                try {
                    return _sp.BytesToRead;
                }
                catch {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Number of bytes to be sent to the device
        /// </summary>
        public int BytesToWrite {
            get {
                try {
                    return _sp.BytesToWrite;
                }
                catch {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Bitmask value representing RAM type supported defined in <see cref="RswpSupport"/> enum
        /// </summary>
        public byte RswpTypeSupport {
            get {
                try {
                    _rswpTypeSupport = GetRswpSupport();

                    return (byte)_rswpTypeSupport;
                }
                catch {
                    throw new Exception("Unable to get supported RAM type");
                }
            }
        }

        /// <summary>
        /// Value representing whether the device supports RSWP capabilities based on RAM type supported reported by the device
        /// </summary>
        public bool RswpPresent => RswpTypeSupport > 0;

        /// <summary>
        /// Occurs when an alert has been received from Arduino
        /// </summary>
        public event EventHandler AlertReceived;

        /// <summary>
        /// Invokes <see cref="AlertReceived"/> event
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void OnAlertReceived(ArduinoEventArgs e) {
            AlertReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Data Received Handler which reads data received from Arduino
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e) {

            lock (_receiveLock) {
                if (sender != _sp || !IsConnected) {
                    return;
                }

                // Set current thread priority above normal
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

                // Wait till data is ready
                while (BytesToRead < _sp.ReceivedBytesThreshold) {
                    Thread.Sleep(10);
                }

                // Prepare input buffer
                _inputBuffer = new byte[PacketData.MaxSize];

                // Read buffer data header
                _bytesReceived += _sp.Read(_inputBuffer, 0, _sp.ReceivedBytesThreshold);

                // Process input data header
                switch (_inputBuffer[0]) {
                    case Header.Alert:

                        // Read alert type
                        byte messageReceived = _inputBuffer[1];

                        // Invoke alert event
                        if (Enum.IsDefined(typeof(Alert), (Alert)messageReceived)) {
                            new Thread(() => HandleAlert((Alert)messageReceived)).Start();
                        }

                        break;

                    case Header.Response:

                        // Wait till full packet is ready
                        while (BytesToRead < _inputBuffer[1] + 1) { }

                        // Read the rest of the data
                        _bytesReceived += _sp.Read(_inputBuffer, PacketData.MinSize, _inputBuffer[1] + 1);

                        // Put data into response data packet
                        _response.RawBytes = _inputBuffer;
                        _dataReady.Set();

                        break;
                }

                // Reset input buffer
                _inputBuffer = null;
            }
        }

        /// <summary>
        /// Alert handler
        /// </summary>
        /// <param name="message">Alert type</param>
        private void HandleAlert(Alert message) {

            OnAlertReceived(new ArduinoEventArgs {
                Notification = message
            });

            switch (message) {
                case Alert.SLAVEDEC:
                case Alert.SLAVEINC:
                    // Update capabilities properties
                    _addresses       = Scan();
                    _rswpTypeSupport = _addresses.Length > 0 ? GetRswpSupport() : 0;
                    break;
                case Alert.READY:
                    // Set ready flag
                    IsReady = true;
                    break;
            }
        }

        /// <summary>
        /// Error Received Handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void ErrorReceivedHandler(object sender, SerialErrorReceivedEventArgs e) {
            if (sender != null && sender.GetType() == typeof(SerialPort)) {
                throw new Exception($"Error received on {((SerialPort)sender).PortName}");
            }
        }

        /// <summary>
        /// Connection monitor
        /// </summary>
        private void ConnectionMonitor() {

            if (_connectionEstablished.WaitOne(5000)) {
                while (IsConnected) {
                    Thread.Sleep(50);
                }
                OnConnectionLost(EventArgs.Empty);
            }

            Dispose();
        }

        /// <summary>
        /// Occurs when the connection has been lost
        /// </summary>
        public event EventHandler ConnectionLost;

        /// <summary>
        /// Invokes the <see cref="ConnectionLost"/> event
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void OnConnectionLost(EventArgs e) {
            ConnectionLost?.Invoke(this, e);
        }

        /// <summary>
        /// Executes a command on the device
        /// </summary>
        /// <typeparam name="T">Response data type</typeparam>
        /// <param name="command">Command to be executed on the device</param>
        /// <returns>Data type value</returns>
        public T ExecuteCommand<T>(object command) => ExecuteCommand<T>(new[] { command });

        /// <summary>
        /// Executes a command with one parameter on the device
        /// </summary>
        /// <typeparam name="T">Response data type</typeparam>
        /// <param name="command">Command to be executed on the device</param>
        /// <param name="p1">Command parameter</param>
        /// <returns>Data type value</returns>
        public T ExecuteCommand<T>(object command, object p1) => ExecuteCommand<T>(new[] { command, p1 });

        /// <summary>
        /// Executes a command with two parameters on the device
        /// </summary>
        /// <typeparam name="T">Response data type</typeparam>
        /// <param name="command">Command to be executed on the device</param>
        /// <param name="p1">First parameter</param>
        /// <param name="p2">Second parameter</param>
        /// <returns>Data type value</returns>
        public T ExecuteCommand<T>(object command, object p1, object p2) => ExecuteCommand<T>(new[] { command, p1, p2 });

        /// <summary>
        /// Executes a command with three parameters on the device
        /// </summary>
        /// <typeparam name="T">Response data type</typeparam>
        /// <param name="command">Command to be executed on the device</param>
        /// <param name="p1">First parameter</param>
        /// <param name="p2">Second parameter</param>
        /// <param name="p3">Third parameter</param>
        /// <returns>Data type value</returns>
        public T ExecuteCommand<T>(object command, object p1, object p2, object p3) => ExecuteCommand<T>(new[] { command, p1, p2, p3 });

        /// <summary>
        /// Executes a command with four parameters on the device
        /// </summary>
        /// <typeparam name="T">Response data type</typeparam>
        /// <param name="command">Command to be executed on the device</param>
        /// <param name="p1">First parameter</param>
        /// <param name="p2">Second parameter</param>
        /// <param name="p3">Third parameter</param>
        /// <param name="p4">Fourth parameter</param>
        /// <returns>Data type value</returns>
        public T ExecuteCommand<T>(object command, object p1, object p2, object p3, object p4) => ExecuteCommand<T>(new[] { command, p1, p2, p3, p4 });

        /// <summary>
        /// Executes a command with five parameters on the device
        /// </summary>
        /// <typeparam name="T">Response data type</typeparam>
        /// <param name="command">Command to be executed on the device</param>
        /// <param name="p1">First parameter</param>
        /// <param name="p2">Second parameter</param>
        /// <param name="p3">Third parameter</param>
        /// <param name="p4">Fourth parameter</param>
        /// <param name="p5">Fifth parameter</param>
        /// <returns>Data type value</returns>
        public T ExecuteCommand<T>(object command, object p1, object p2, object p3, object p4, object p5) => ExecuteCommand<T>(new[] { command, p1, p2, p3, p4, p5 });

        /// <summary>
        /// Executes a multi byte command on the device
        /// </summary>
        /// <typeparam name="T">Response data type</typeparam>
        /// <param name="command">Command and parameters to be executed on the device</param>
        /// <returns>Data type value</returns>
        private T ExecuteCommand<T>(IEnumerable command) {

            Queue<byte> rawBytes = new Queue<byte>();

            foreach (byte b in ObjectToArray<byte>(command)) {
                rawBytes.Enqueue(b);
            }

            byte[] response = ExecuteCommand(rawBytes.ToArray());

            if (response.Length == 1) {
                return ConvertTo<T>(response[0]);
            }

            Type t = typeof(T);
            TypeCode typeCode = Type.GetTypeCode(t);

            if (t.IsArray) {
                return ConvertTo<T>(response);
            }

            if (typeCode == TypeCode.String) {
                return ConvertTo<T>(BytesToString(response));
            }

            if (IsNumeric(typeCode)) {
                ulong outputBuffer = 0;

                int dataSize = (int)GetDataSize(typeCode);
                int dataSizeLimit = response.Length >= dataSize
                    ? dataSize
                    : response.Length;

                for (int i = 0; i < dataSizeLimit; i++) {
                    outputBuffer |= (uint)(response[i] << (8 * i));
                }

                return ConvertTo<T>(outputBuffer);
            }

            return default;
        }

        /// <summary>
        /// Executes commands on the device.
        /// </summary>
        /// <param name="command">Bytes to be sent to the device</param>
        /// <returns>A byte array received from the device in response</returns>
        private byte[] ExecuteCommand(byte[] command) {
            if (command.Length == 0) {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(command));
            }

            if (!IsConnected) {
                throw new InvalidOperationException($"{PortName} is not connected");
            }

            lock (_portLock) {
                try {
                    // Clear input and output buffers
                    ClearBuffer();

                    // Send the command to device
                    _sp.BaseStream.Write(command, 0, command.Length);
                    _sp.BaseStream.Flush();

                    // Update stats
                    _bytesSent += command.Length;

                    // Wait for data
                    if (!_dataReady.WaitOne(PortSettings.Timeout * 1000)) {
                        throw new TimeoutException($"{PortName} response timeout");
                    }

                    // Validate response
                    if (_response.Type != Header.Response) {
                        throw new InvalidDataException("Invalid response header");
                    }

                    // Verify checksum
                    if (!_response.IsChecksumValid) {
                        throw new DataException("Response CRC error");
                    }

                    // Return response body
                    return _response.Body;
                }
                catch (Exception e) {
                    throw new IOException($"{PortName} failed to execute command 0x{BytesToHexString(command)} ({e.Message})");
                }
                finally {
                    _response = new PacketData();
                    _dataReady.Reset();
                }
            }
        }

        /// <summary>
        /// Serial port instance
        /// </summary>
        private SerialPort _sp;

        /// <summary>
        /// Response packet
        /// </summary>
        private PacketData _response;

        /// <summary>
        /// Incoming data buffer
        /// </summary>
        private byte[] _inputBuffer;

        /// <summary>
        /// Number of bytes received from the device
        /// </summary>
        private int _bytesReceived;

        /// <summary>
        /// Number of bytes sent to the device
        /// </summary>
        private int _bytesSent;

        /// <summary>
        /// I2C addresses available
        /// </summary>
        private byte[] _addresses;

        /// <summary>
        /// Bitmask value representing RSWP type supported defined in <see cref="RswpSupport"/> enum
        /// </summary>
        private int _rswpTypeSupport;

        /// <summary>
        /// PortLock object used to prevent other threads from acquiring the lock
        /// </summary>
        private static object _portLock = new object();

        /// <summary>
        /// Lock object to prevent simultaneous <see cref="DataReceivedHandler"/> calls
        /// </summary>
        private static object _receiveLock = new object();

        /// <summary>
        /// Data ready event
        /// </summary>
        private readonly AutoResetEvent _dataReady = new AutoResetEvent(false);

        /// <summary>
        /// Connection established event
        /// </summary>
        private readonly AutoResetEvent _connectionEstablished = new AutoResetEvent(false);

        /// <summary>
        /// Device commands
        /// </summary>
        internal enum Command : byte {

            #region Command modifiers

            /// <summary>
            /// Gets current variable value
            /// </summary>
            Get = unchecked((byte)unchecked(-1)),

            /// <summary>
            /// Resets variable to default value
            /// </summary>
            Disable = 0,

            /// <summary>
            /// Modifies variable value
            /// </summary>
            Enable,

            #endregion

            #region Diagnostics & info

            /// <summary>
            /// Get Firmware version
            /// </summary>
            Version,

            /// <summary>
            /// Device Communication Test
            /// </summary>
            Test,

            /// <summary>
            /// Name controls
            /// </summary>
            Name,

            /// <summary>
            /// Restore device settings to default
            /// </summary>
            FactoryReset,

            #endregion

            #region Control commands

            /// <summary>
            /// Read SPD byte or page
            /// </summary>
            SpdReadPage,

            /// <summary>
            /// Write byte
            /// </summary>
            SpdWriteByte,

            /// <summary>
            /// Write page
            /// </summary>
            SpdWritePage,

            /// <summary>
            /// Write protection test
            /// </summary>
            SpdWriteTest,

            /// <summary>
            /// DDR4 detection
            /// </summary>
            Ddr4Detect,

            /// <summary>
            /// DDR5 detection
            /// </summary>
            Ddr5Detect,

            /// <summary>
            /// Access SPD5 Hub register space
            /// </summary>
            Spd5HubReg,

            /// <summary>
            /// Get EEPROM size
            /// </summary>
            Size,

            /// <summary>
            /// Scan I2C bus
            /// </summary>
            ScanBus,

            /// <summary>
            /// I2C clock control
            /// </summary>
            BusClock,

            /// <summary>
            /// Probe I2C address
            /// </summary>
            ProbeAddress,

            /// <summary>
            /// Read from I2C slave device
            /// </summary>
            I2cRead,

            /// <summary>
            /// Write to I2C slave device
            /// </summary>
            I2cWrite,

            /// <summary>
            /// Config pin control
            /// </summary>
            PinControl,

            /// <summary>
            /// Reset config pins state to defaults
            /// </summary>
            PinReset,

            /// <summary>
            /// RSWP operation
            /// </summary>
            Rswp,

            /// <summary>
            /// PSWP operation
            /// </summary>
            Pswp,

            /// <summary>
            /// Report current RSWP capabilities
            /// </summary>
            RswpReport,

            /// <summary>
            /// Internal EEPROM controls
            /// </summary>
            Eeprom,

            #endregion
        }

        /// <summary>
        /// Configuration pins
        /// </summary>
        public enum ConfigPin : byte {
            /// <summary>
            /// High voltage (9V) control pin
            /// </summary>
            HV_SWITCH,

            /// <summary>
            /// Slave address 1 (SA1) control pin
            /// </summary>
            SA1_SWITCH
        }

        /// <summary>
        /// Incoming packet data
        /// </summary>
        private struct PacketData {

            /// <summary>
            /// Raw packet contents
            /// </summary>
            public byte[] RawBytes {
                get => _rawBytes;
                set {
                    if (MaxSize < value.Length || value.Length < MinSize) {
                        throw new ArgumentOutOfRangeException(value.Length.ToString());
                    }

                    _rawBytes = value;
                }
            }

            private byte[] _rawBytes;

            /// <summary>
            /// Maximum packet length
            /// </summary>
            /// <remarks>
            /// 1 byte for <see cref="Header.Response"/>,
            /// 1 byte for <see cref="Length"/>,
            /// 32 bytes for <see cref="Body"/>, and
            /// 1 byte for <see cref="Checksum"/>
            /// </remarks>
            public const int MaxSize = 1 + 1 + 32 + 1;

            /// <summary>
            /// Minimum packet length
            /// </summary>
            /// <remarks>
            /// 1 byte for <see cref="Header.Alert"/> and
            /// 1 byte for <see cref="Alert"/>
            /// </remarks>
            public const int MinSize = 1 + 1;

            /// <summary>
            /// Packet state
            /// </summary>
            public bool Ready => _rawBytes != null && _rawBytes.Length >= MinSize;

            /// <summary>
            /// Packet header
            /// </summary>
            public byte Type => _rawBytes[0];

            /// <summary>
            /// Packet body length, when <see cref="Type"/> is <see cref="Header.Response"/>
            /// </summary>
            public byte Length => _rawBytes[1];

            /// <summary>
            /// Packet body contents
            /// </summary>
            public byte[] Body => SubArray(_rawBytes, MinSize, Length);

            /// <summary>
            /// Packet body checksum
            /// </summary>
            private byte Checksum => _rawBytes[Length + MinSize];

            /// <summary>
            /// Data checksum state
            /// </summary>
            public bool IsChecksumValid => Crc(Body) == Checksum;
        }

        /// <summary>
        /// Incoming serial data headers
        /// </summary>
        private struct Header {
            /// <summary>
            /// A notification received header
            /// </summary>
            internal const byte Alert    = (byte)'@';

            /// <summary>
            /// Response data header
            /// </summary>
            internal const byte Response = (byte)'&';
        }

        /// <summary>
        /// Bitmask values describing specific RSWP support in response to <see cref="Command.RswpReport"/> command
        /// </summary>
        public struct RswpSupport {

            /// <summary>
            /// Value describing the device supports VHV and SA1 controls for DDR3 and older RAM RSWP support
            /// </summary>
            public const byte DDR3 = 1 << 3;

            /// <summary>
            /// Value describing the device supports VHV control for DDR4 RSWP support
            /// </summary>
            public const byte DDR4 = 1 << 4;

            /// <summary>
            /// Value describing the device supports Offline mode for DDR5 RSWP support
            /// </summary>
            public const byte DDR5 = 1 << 5;
        }

        /// <summary>
        /// Arduino's I2C bus clock mode
        /// </summary>
        public struct ClockMode {

            /// <summary>
            /// High speed mode (400 kHz)
            /// </summary>
            public const ushort Fast     = 400;

            /// <summary>
            /// Standard mode (100 kHz)
            /// </summary>
            public const ushort Standard = 100;
        }

        /// <summary>
        /// Alerts received from Arduino
        /// </summary>
        public enum Alert {

            /// <summary>
            /// Notification the device is ready to receive commands
            /// </summary>
            READY    = '!',

            /// <summary>
            /// Notification the number of slave addresses on the Arduino's I2C bus has increased
            /// </summary>
            SLAVEINC = '+',

            /// <summary>
            /// Notification the number of slave addresses on the Arduino's I2C bus has decreased
            /// </summary>
            SLAVEDEC = '-',

            /// <summary>
            /// Notification of I2C clock frequency increase
            /// </summary>
            CLOCKINC = '/',

            /// <summary>
            /// Notification of I2C clock frequency decrease
            /// </summary>
            CLOCKDEC = '\\',
        }

        /// <summary>
        /// Arduino Event Arguments
        /// </summary>
        public class ArduinoEventArgs : EventArgs {

            /// <summary>
            /// Notification event argument
            /// </summary>
            public Alert Notification { get; set; }

            /// <summary>
            /// Notification timestamp
            /// </summary>
            public DateTime TimeStamp => DateTime.Now;

            /// <summary>
            /// Provides a value to use with events that do not have event data
            /// </summary>
            public new static readonly ArduinoEventArgs Empty = new ArduinoEventArgs();
        }
    }
}