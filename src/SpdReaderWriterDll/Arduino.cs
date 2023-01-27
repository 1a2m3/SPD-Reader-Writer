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
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SpdReaderWriterDll {

    /// <summary>
    /// Defines Device class, properties, and methods to handle the communication with the device
    /// </summary>
    public class Arduino : IDisposable {

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
        /// <param name="i2cAddress">EEPROM address on the device's i2c bus</param>
        public Arduino(SerialPortSettings portSettings, string portName, byte i2cAddress) {
            PortSettings = portSettings;
            PortName     = portName;
            I2CAddress   = i2cAddress;
        }

        /// <summary>
        /// Arduino device instance string
        /// </summary>
        /// <returns>Arduino device instance string</returns>
        public override string ToString() => 
            PortName == null ? "N/A" : $"{PortName}:{PortSettings.BaudRate}";

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
                return $"{BaudRate}";
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
                if (!IsConnected) {
                    _sp = new SerialPort {
                        // New connection settings
                        PortName     = PortName,
                        BaudRate     = PortSettings.BaudRate,
                        DtrEnable    = PortSettings.DtrEnable,
                        RtsEnable    = PortSettings.RtsEnable,
                        ReadTimeout  = PortSettings.Timeout,
                        WriteTimeout = PortSettings.Timeout,
                    };

                    // Event to handle Data Reception
                    _sp.DataReceived  += DataReceivedHandler;

                    // Event to handle Errors
                    _sp.ErrorReceived += ErrorReceivedHandler;

                    // Test the connection
                    try {
                        // Establish a connection
                        _sp.Open();

                        // Reset stats
                        _bytesSent     = 0;
                        _bytesReceived = 0;

                        try {
                            IsValid = Test();
                        }
                        catch {
                            IsValid = false;
                            Dispose();
                        }

                        if (!IsValid) {
                            try {
                                Dispose();
                            }
                            finally {
                                throw new Exception("Invalid device");
                            }
                        }
                    }
                    catch (Exception ex) {
                        throw new Exception($"Unable to connect ({PortName}): {ex.Message}");
                    }
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
                if (IsConnected) {
                    try {
                        // Remove handlers
                        _sp.DataReceived  -= DataReceivedHandler;
                        _sp.ErrorReceived -= ErrorReceivedHandler;
                        Dispose();
                    }
                    catch (Exception ex) {
                        throw new Exception($"Unable to disconnect ({PortName}): {ex.Message}");
                    }
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

                if (_sp != null) {
                    _sp.DataReceived  -= DataReceivedHandler;
                    _sp.ErrorReceived -= ErrorReceivedHandler;
                    _sp = null;
                }

                ResponseData.Clear();
                DataReceiving   = false;
                IsValid         = false;
                _addresses      = null;
                _ramTypeSupport = -1;
            }
        }

        /// <summary>
        /// Tests if the device responds to a test command
        /// </summary>
        /// <returns><see langword="true"/> if the device responds properly to a test command</returns>
        public bool Test() {
            lock (_portLock) {
                try {
                    return ExecuteCommand(Command.TEST) == Response.WELCOME;
                }
                catch {
                    throw new Exception($"Unable to test {PortName}");
                }
            }
        }

        /// <summary>
        /// Gets supported RAM type(s)
        /// </summary>
        /// <returns>A bitmask representing available RAM supported defined in the <see cref="Response.RswpSupport"/> struct</returns>
        public byte GetRswpSupport() {
            lock (_portLock) {
                try {
                    return ExecuteCommand(Command.RSWPREPORT);
                }
                catch {
                    throw new Exception($"Unable to get {PortName} supported RAM");
                }
            }
        }

        /// <summary>
        /// Test if the device supports RAM type RSWP at firmware level
        /// </summary>
        /// <param name="rswpTypeBitmask">RAM type bitmask</param>
        /// <returns><see langword="true"/> if the device supports <see cref="Response.RswpSupport"/> RSWP at firmware level</returns>
        public bool GetRswpSupport(byte rswpTypeBitmask) {
            return (GetRswpSupport() & rswpTypeBitmask) == rswpTypeBitmask;
        }

        /// <summary>
        /// Reads a byte from the device
        /// </summary>
        /// <returns>A single byte value received from the device</returns>
        public byte ReadByte() {
            return (byte)_sp.ReadByte();
        }

        /// <summary>
        /// I2C addresses available on Arduino
        /// </summary>
        public byte[] Addresses {
            get {
                if (_addresses == null) {
                    _addresses = Scan();
                }

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
                        byte response = Scan(true);

                        if (response == Response.NULL) {
                            return new byte[0];
                        }

                        for (byte i = 0; i <= 7; i++) {
                            if (Data.GetBit(response, i)) {
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
        /// Scans for EEPROM addresses on the device's I2C bus
        /// </summary>
        /// <param name="bitmask">Enable bitmask response</param>
        /// <returns>A bitmask representing available addresses on the device's I2C bus.</returns>
        /// <example>Bit 0 is address 80, bit 1 is address 81, and so on.</example>
        public byte Scan(bool bitmask) {
            if (bitmask) {
                lock (_portLock) {
                    try {
                        return ExecuteCommand(Command.SCANBUS);
                    }
                    catch {
                        throw new Exception($"Unable to scan I2C bus on {PortName}");
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Sets clock frequency for I2C communication
        /// </summary>
        /// <param name="fastMode">Fast mode or standard mode</param>
        /// <returns><see langword="true"/> if the operation is successful</returns>
        public bool SetI2CClock(bool fastMode) {
            lock (_portLock) {
                try {
                    return ExecuteCommand(new[] { Command.I2CCLOCK, Data.BoolToNum<byte>(fastMode) }) == Response.SUCCESS;
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
        public bool GetI2CClock() {
            lock (_portLock) {
                try {
                    return ExecuteCommand(new[] { Command.I2CCLOCK, Command.GET }) == Response.SUCCESS;
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
                    return ExecuteCommand(Command.FACTORYRESET) == Response.SUCCESS;
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
            get => GetConfigPin(Pin.Name.SA1_SWITCH);
            set => SetConfigPin(Pin.Name.SA1_SWITCH, value);
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
                    return ExecuteCommand(new[] { Command.PINCONTROL, Pin.Name.HV_SWITCH, Data.BoolToNum<byte>(state) }) == Response.SUCCESS;
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
                    return ExecuteCommand(new[] { Command.PINCONTROL, Pin.Name.HV_SWITCH, Command.GET }) == Response.ON;
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
        public bool SetConfigPin(byte pin, bool state) {
            lock (_portLock) {
                try {
                    return ExecuteCommand(new[] { Command.PINCONTROL, pin, Data.BoolToNum<byte>(state) }) == Response.SUCCESS;
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
        public bool GetConfigPin(byte pin) {
            lock (_portLock) {
                try {
                    return ExecuteCommand(new[] { Command.PINCONTROL, pin, Command.GET }) == Response.ON;
                }
                catch {
                    throw new Exception($"Unable to get config pin state on {PortName}");
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
                    return GetRswpSupport(Response.RswpSupport.DDR5);
                }
                catch {
                    throw new Exception($"Unable to get offline mode status on {PortName}");
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
                    return ExecuteCommand(new[] { Command.PINRESET }) == Response.SUCCESS;
                }
                catch {
                    throw new Exception($"Unable to reset pin state on {PortName}");
                }
            }
        }

        /// <summary>
        /// Probes default EEPROM address
        /// </summary>
        /// <returns><see langword="true"/> if EEPROM is detected at assigned <see cref="I2CAddress"/></returns>
        public bool ProbeAddress() {
            return I2CAddress != 0 && ProbeAddress(I2CAddress);
        }

        /// <summary>
        /// Probes specified EEPROM address
        /// </summary>
        /// <param name="address">EEPROM address</param>
        /// <returns><see langword="true"/> if EEPROM is detected at the specified <see cref="I2CAddress"/></returns>
        public bool ProbeAddress(byte address) {
            lock (_portLock) {
                try {
                    return ExecuteCommand(new[] { Command.PROBEADDRESS, address }) == Response.ACK;
                }
                catch {
                    throw new Exception($"Unable to probe address {address} on {PortName}");
                }
            }
        }

        /// <summary>
        /// Clears serial port buffers from unneeded data to prevent unwanted behavior and delays
        /// </summary>
        public void ClearBuffer() {
            lock (_portLock) {
                try {
                    if (IsConnected) {
                        // Clear response data
                        if (ResponseData.Count > 0) {
                            ResponseData.Clear();
                        }

                        // Clear receive buffer
                        if (BytesToRead > 0) {
                            _sp.DiscardInBuffer();
                        }

                        // Clear transmit buffer
                        if (BytesToWrite > 0) {
                            _sp.DiscardOutBuffer();
                        }
                    }
                }
                catch {
                    throw new Exception($"Unable to clear {PortName} buffer");
                }
            }
        }

        /// <summary>
        /// Clears serial port buffers and causes any buffered data to be written
        /// </summary>
        public void FlushBuffer() {
            lock (_portLock) {
                if (IsConnected) {
                    _sp.BaseStream.Flush();
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
                    return int.Parse(Data.BytesToString(ExecuteCommand(Command.VERSION, Command.VERSIONLENGTH)));
                }
                catch {
                    throw new Exception($"Unable to get firmware version on {PortName}");
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
                byte[] fwHeader         = Data.GzipPeek(Properties.Resources.Firmware.SpdReaderWriter_ino, 1024);
                Regex versionPattern    = new Regex($@"([\d]{{{Command.VERSIONLENGTH}}})"); // ([\d]{8})
                MatchCollection matches = versionPattern.Matches(Data.BytesToString(fwHeader));

                if (matches.Count > 0 && matches[0].Length == Command.VERSIONLENGTH) {
                    return int.Parse(matches[0].Value);
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
        /// Assigns a name to the Device
        /// </summary>
        /// <param name="name">Device name</param>
        /// <returns><see langword="true"/> when the device name is set</returns>
        public bool SetName(string name) {
            
            if (name == null) {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length == 0) {
                throw new ArgumentException("Name can't be blank");
            }
            if (name.Length > Command.NAMELENGTH) {
                throw new ArgumentOutOfRangeException($"Name can't be longer than {Command.NAMELENGTH} characters");
            }

            lock (_portLock) {
                try {
                    string newName = name.Trim();

                    if (newName == GetName()) {
                        return false;
                    }

                    // Prepare a byte array containing cmd byte + name length + name
                    byte[] command = { Command.NAME, (byte)newName.Length };

                    return ExecuteCommand(Data.MergeArray(command, Encoding.ASCII.GetBytes(newName))) == Response.SUCCESS;
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
        public string GetName() {
            lock (_portLock) {
                try {
                    return Data.BytesToString(ExecuteCommand(new[] { Command.NAME, Command.GET }, Command.NAMELENGTH)).Trim();
                }
                catch {
                    throw new Exception($"Unable to get {PortName} name");
                }
            }
        }

        /// <summary>
        /// Finds Arduinos connected to computer
        /// </summary>
        /// <returns>An array of Arduinos</returns>
        public static Arduino[] Find(SerialPortSettings settings) {
            Stack<Arduino> result = new Stack<Arduino>();

            string[] ports = SerialPort.GetPortNames().Distinct().ToArray();

            foreach (string portName in ports) {
                using (Arduino device = new Arduino(settings, portName)) {
                    try {
                        if (device.Connect()) {
                            result.Push(device);
                        }
                    }
                    catch {
                        continue;
                    }
                }
            }

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
        /// Describes if the device passed connection and communication tests
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Detects if DDR4 RAM is present on the device's I2C bus at specified <see cref="address"/>
        /// </summary>
        /// <returns><see langword="true"/> if DDR4 is found at <see cref="address"/></returns>
        public bool DetectDdr4() {
            lock (_portLock) {
                try {
                    return ExecuteCommand(new[] { Command.DDR4DETECT, I2CAddress }) == Response.SUCCESS;
                }
                catch {
                    throw new Exception($"Error detecting DDR4 on {PortName}");
                }
            }
        }

        /// <summary>
        /// Detects if DDR5 RAM is present on the device's I2C bus at specified <see cref="address"/>
        /// </summary>
        /// <returns><see langword="true"/> if DDR5 is found at <see cref="I2CAddress"/></returns>
        public bool DetectDdr5() {
            lock (_portLock) {
                try {
                    return ExecuteCommand(new[] { Command.DDR5DETECT, I2CAddress }) == Response.SUCCESS;
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
                    return ExecuteCommand(new[] { Command.SPD5HUBREG, I2CAddress, register, Command.GET });
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
                    return ExecuteCommand(new[] { Command.SPD5HUBREG, I2CAddress, register, Command.SET, value }) == Response.SUCCESS;
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
                    return Spd.DataLength.Length[ExecuteCommand(new[] { Command.SIZE, I2CAddress })];
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
                if (_i2CAddress == value) {
                    //return;
                }

                _i2CAddress = value;

                if (IsConnected && Eeprom.ValidateAddress(_i2CAddress)) {
                    DataLength = GetSpdSize();
                }
            }
        }

        private byte _i2CAddress;

        /// <summary>
        /// EEPROM size
        /// </summary>
        public int DataLength { get; private set; }

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
        /// Bitmask value representing RAM type supported defined in <see cref="Response.RswpSupport"/> enum
        /// </summary>
        public byte RamTypeSupport {
            get {
                try {
                    if (_ramTypeSupport == -1) {
                        _ramTypeSupport = GetRswpSupport();
                    }

                    return (byte)_ramTypeSupport;
                }
                catch {
                    throw new Exception("Unable to get supported RAM type");
                }
            }
        }

        /// <summary>
        /// Value representing whether the device supports RSWP capabilities based on RAM type supported reported by the device
        /// </summary>
        public bool RswpPresent => (RamTypeSupport & (Response.RswpSupport.DDR3 | 
                                                      Response.RswpSupport.DDR4 |
                                                      Response.RswpSupport.DDR5)) > 0;

        /// <summary>
        /// Indicates whether or not a response is expected after <see cref="ExecuteCommand(byte)"/>
        /// </summary>
        public bool ResponseExpected { get; private set; }

        /// <summary>
        /// Byte stack containing data received from Serial Port
        /// </summary>
        public Queue<byte> ResponseData = new Queue<byte>();

        /// <summary>
        /// Indicates whether data reception is complete
        /// </summary>
        public bool DataReceiving { get; private set; }

        /// <summary>
        /// Raises an alert
        /// </summary>
        private void RaiseAlert(ArduinoEventArgs e) {
            OnAlertReceived(e);
        }

        /// <summary>
        /// Indicates an unexpected alert has been received from Arduino
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
            if (sender == null || sender.GetType() != typeof(SerialPort)) {
                return;
            }

            SerialPort receiver = (SerialPort)sender;

            _bytesReceived += receiver.BytesToRead;

            if (!ResponseExpected) {
                if (receiver.BytesToRead >= 2) {
                    if (ReadByte() == (byte)Alert.ALERT) {

                        byte notificationReceived = ReadByte();

                        if (Enum.IsDefined(typeof(Alert), (Alert)notificationReceived)) {
                            HandleAlert((Alert)notificationReceived);
                            RaiseAlert(new ArduinoEventArgs {
                                Notification = (Alert)notificationReceived
                            });
                            return;
                        }
                    }
                }
            }

            while (receiver.IsOpen && receiver.BytesToRead > 0) {
                DataReceiving = true;
                ResponseData.Enqueue(ReadByte());
            }

            DataReceiving = false;
        }

        /// <summary>
        /// Alert handler
        /// </summary>
        /// <param name="alert">Alert type</param>
        private void HandleAlert(Alert alert) {
            if (alert == Alert.SLAVEDEC || 
                alert == Alert.SLAVEINC) {
                _addresses      = null;
                _ramTypeSupport = -1;
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
        /// Executes a single byte command on the device and expects a single byte response
        /// </summary>
        /// <param name="command">Byte to be sent to the device</param>
        /// <returns>A byte received from the device in response</returns>
        public byte ExecuteCommand(byte command) {
            return ExecuteCommand(new[] { command }, 1)[0];
        }

        /// <summary>
        /// Executes a multi byte command on the device and expects a single byte response
        /// </summary>
        /// <param name="command">Bytes to be sent to the device</param>
        /// <returns>A byte received from the device in response</returns>
        public byte ExecuteCommand(byte[] command) {
            return ExecuteCommand(command, 1)[0];
        }

        /// <summary>
        /// Executes a single byte command on the device and expects a multi byte response
        /// </summary>
        /// <param name="command">Byte to be sent to the device</param>
        /// <param name="length">Number of bytes to receive in response</param>
        /// <returns>A byte array received from the device in response</returns>
        public byte[] ExecuteCommand(byte command, uint length) {
            return ExecuteCommand(new[] { command }, length);
        }

        /// <summary>
        /// Executes commands on the device.
        /// </summary>
        /// <param name="command">Bytes to be sent to the device</param>
        /// <param name="responseLength">Number of bytes to receive in response</param>
        /// <returns>A byte array received from the device in response</returns>
        public byte[] ExecuteCommand(byte[] command, uint responseLength) {
            if (command.Length == 0) {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(command));
            }

            if (!IsConnected) {
                throw new InvalidOperationException("Device is not connected");
            }

            byte[] response = new byte[responseLength];

            lock (_portLock) {
                try {
                    // Check response length
                    ResponseExpected = responseLength > 0;

                    // Clear input and output buffers
                    ClearBuffer();

                    // Send the command to device
                    _sp.Write(command, 0, command.Length);

                    _bytesSent += command.Length;

                    // Flush the buffer
                    FlushBuffer();

                    if (!ResponseExpected) {
                        return new byte[0];
                    }

                    // Timeout monitoring start
                    bool timeout = true;
                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    // Get response
                    while (sw.ElapsedMilliseconds < PortSettings.Timeout * 1000) {

                        // Wait for data
                        if (ResponseData.Count >= responseLength && !DataReceiving) {
                            for (int i = 0; i < response.Length; i++) {
                                response[i] = ResponseData.Dequeue();
                            }

                            timeout = false;
                            break;
                        }

                        // Allow sleep during low data transfer
                        if (ResponseData.Count == 0 &&
                            !(command[0] == Command.READBYTE ||
                              command[0] == Command.WRITEBYTE ||
                              command[0] == Command.WRITEPAGE ||
                              command[0] == Command.DDR5DETECT ||
                              command[0] == Command.DDR4DETECT)) {
                            Thread.Sleep(10);
                        }
                    }

                    if (timeout) {
                        throw new TimeoutException($"{PortName} response timeout");
                    }

                    return response;
                }
                catch {
                    throw new IOException($"{PortName} failed to execute command {command}");
                }
                finally {
                    ResponseExpected = false;
                }
            }
        }

        /// <summary>
        /// Serial port instance
        /// </summary>
        private SerialPort _sp;

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
        /// Bitmask value representing RAM type supported defined in <see cref="Response.RswpSupport"/> enum
        /// </summary>
        private int _ramTypeSupport = -1;

        /// <summary>
        /// PortLock object used to prevent other threads from acquiring the lock 
        /// </summary>
        private readonly object _portLock = new object();

        /// <summary>
        /// Device commands
        /// </summary>
        public struct Command {
            /// <summary>
            /// Read byte
            /// </summary>
            public const byte READBYTE      = (byte)'r';

            /// <summary>
            /// Write byte
            /// </summary>
            public const byte WRITEBYTE     = (byte)'w';

            /// <summary>
            /// Write page
            /// </summary>
            public const byte WRITEPAGE     = (byte)'g';

            /// <summary>
            /// Scan i2c bus
            /// </summary>
            public const byte SCANBUS       = (byte)'s';

            /// <summary>
            /// Set i2c clock 
            /// </summary>
            public const byte I2CCLOCK      = (byte)'c';

            /// <summary>
            /// Probe i2c address
            /// </summary>
            public const byte PROBEADDRESS  = (byte)'a';

            /// <summary>
            /// Config pin state control
            /// </summary>
            public const byte PINCONTROL    = (byte)'p';

            /// <summary>
            /// Pins state reset
            /// </summary>
            public const byte PINRESET      = (byte)'d';

            /// <summary>
            /// RSWP control
            /// </summary>
            public const byte RSWP          = (byte)'b';

            /// <summary>
            /// PSWP control
            /// </summary>
            public const byte PSWP          = (byte)'l';

            /// <summary>
            /// Offset write protection test
            /// </summary>
            public const byte OVERWRITE     = (byte)'o';

            /// <summary>
            /// Get Firmware version
            /// </summary>
            public const byte VERSION       = (byte)'v';

            /// <summary>
            /// Firmware version number length
            /// </summary>
            public const byte VERSIONLENGTH = 8;

            /// <summary>
            /// Device Communication Test
            /// </summary>
            public const byte TEST          = (byte)'t';

            /// <summary>
            /// Report current RSWP RAM support
            /// </summary>
            public const byte RSWPREPORT    = (byte)'f';

            /// <summary>
            /// Device name controls
            /// </summary>
            public const byte NAME          = (byte)'n';

            /// <summary>
            /// Maximum name length
            /// </summary>
            public const byte NAMELENGTH    = 16;

            /// <summary>
            /// DDR4 detection
            /// </summary>
            public const byte DDR4DETECT    = (byte)'4';

            /// <summary>
            /// DDR5 detection
            /// </summary>
            public const byte DDR5DETECT    = (byte)'5';

            /// <summary>
            /// Access SPD5 Hub register space
            /// </summary>
            public const byte SPD5HUBREG    = (byte)'h';

            /// <summary>
            /// Get EEPROM size
            /// </summary>
            public const byte SIZE          = (byte)'z';

            /// <summary>
            /// Restore device settings to default
            /// </summary>
            public const byte FACTORYRESET  = (byte)'-';

            /// <summary>
            /// Command modifier used to modify variable value
            /// </summary>
            public const byte SET           = 1;

            /// <summary>
            /// Suffix added to get current state
            /// </summary>
            public const byte GET           = (byte)'?';

            /// <summary>
            /// Suffix added to set state equivalent to true/on/enable etc
            /// </summary>
            public const byte ON            = 1;

            /// <summary>
            /// Suffix added to set state equivalent to false/off/disable etc
            /// </summary>
            public const byte OFF           = 0;

            /// <summary>
            /// "Do not care" byte
            /// </summary>
            public const byte DNC           = 0;
        }

        /// <summary>
        /// Class describing configuration pins
        /// </summary>
        public struct Pin {
            /// <summary>
            /// Struct describing config pin names
            /// </summary>
            public struct Name {

                /// <summary>
                /// High voltage (9V) control pin
                /// </summary>
                public const byte HV_SWITCH  = 0;

                /// <summary>
                /// Slave address 1 (SA1) control pin
                /// </summary>
                public const byte SA1_SWITCH = 1;
            }

            /// <summary>
            /// Struct describing config pin states
            /// </summary>
            public struct State {
                /// <summary>
                /// Pin state name describing condition when pin is <b>HIGH</b>
                /// </summary>
                public const bool HIGH     = true;

                /// <summary>
                /// Pin state name describing condition when pin is <b>LOW</b>
                /// </summary>
                public const bool LOW      = false;

                // Aliases for HIGH
                public const bool VDDSPD   = HIGH;
                public const bool PULLUP   = HIGH;
                public const bool VCC      = HIGH;
                public const bool ON       = HIGH;
                public const bool UP       = HIGH;
                public const bool ENABLE   = HIGH;
                public const bool ENABLED  = HIGH;

                // Aliases for LOW
                public const bool VSSSPD   = LOW;
                public const bool PUSHDOWN = LOW;
                public const bool VSS      = LOW;
                public const bool GND      = LOW;
                public const bool OFF      = LOW;
                public const bool DOWN     = LOW;
                public const bool DISABLE  = LOW;
                public const bool DISABLED = LOW;
                public const bool DEFAULT  = LOW;
            }
        }

        /// <summary>
        /// Responses received from Arduino
        /// </summary>
        public struct Response {
            /// <summary>
            /// Boolean <see langword="true"/> response
            /// </summary>
            public const byte TRUE     = 0x01;

            /// <summary>
            /// Boolean <see langword="false"/> response
            /// </summary>
            public const byte FALSE    = 0x00;

            /// <summary>
            /// Indicates the operation was executed successfully
            /// </summary>
            public const byte SUCCESS  = 0x01;

            /// <summary>
            /// A response used to indicate an error when normally a numeric non-zero answer is expected if the operation was executed successfully
            /// </summary>
            public const byte NULL     = 0x00;

            /// <summary>
            /// A response used to describe when SA pin is tied to VCC
            /// </summary> 
            public const byte ON       = 0x01;

            /// <summary>
            /// A response used to describe when SA pin is tied to GND
            /// </summary>
            public const byte OFF      = 0x00;

            /// <summary>
            /// A response expected from the device after executing <see cref="Command.TEST"/> command to identify the correct device
            /// </summary>
            public const char WELCOME  = '!';

            /// <summary>
            /// A response indicating the command or syntax was not in a correct format
            /// </summary>
            public const char UNKNOWN  = '?';

            // Aliases
            public const byte ACK      = SUCCESS;
            public const byte ENABLED  = TRUE;
            public const byte DISABLED = FALSE;
            public const byte NACK     = FALSE;
            public const byte NOACK    = FALSE;
            public const byte FAIL     = FALSE;
            public const byte ZERO     = NULL;

            /// <summary>
            /// Bitmask values describing specific RSWP support in response to <see cref="Command.RSWPREPORT"/> command
            /// </summary>
            public struct RswpSupport {

                /// <summary>
                /// Value describing the device supports VHV and SA1 controls for DDR3 and below RSWP support
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
            /// A notification received header
            /// </summary>
            ALERT     = '@',

            /// <summary>
            /// Notification the number of slave addresses on the Arduino's I2C bus has increased
            /// </summary>
            SLAVEINC  = '+',

            /// <summary>
            /// Notification the number of slave addresses on the Arduino's I2C bus has decreased
            /// </summary>
            SLAVEDEC  = '-',

            /// <summary>
            /// Notification of I2C clock frequency increase
            /// </summary>
            CLOCKINC  = '/',

            /// <summary>
            /// Notification of I2C clock frequency decrease
            /// </summary>
            CLOCKDDEC = '\\',
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