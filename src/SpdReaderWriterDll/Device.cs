using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using static SpdReaderWriterDll.Command;
using static SpdReaderWriterDll.Pin;
using static SpdReaderWriterDll.Pin.Name;
using static SpdReaderWriterDll.Spd;
using UInt8 = System.Byte;

namespace SpdReaderWriterDll {
    /// <summary>
    /// Defines Device class, properties, and methods to handle the communication with the device
    /// </summary>
    public class Device {
        
        /// <summary>
        /// Initializes the SPD reader/writer device
        /// </summary>
        /// <param name="portSettings">Serial port settings</param>
        public Device(SerialPortSettings portSettings) {
            PortSettings = portSettings;
        }

        /// <summary>
        /// Initializes the SPD reader/writer device
        /// </summary>
        /// <param name="portSettings">Serial port settings</param>
        /// <param name="portName">Serial port name</param>
        public Device(SerialPortSettings portSettings, string portName) {
            PortSettings = portSettings;
            PortName     = portName;
        }

        /// <summary>
        /// Initializes the SPD reader/writer device
        /// </summary>
        /// <param name="portSettings">Serial port settings</param>
        /// <param name="portName" >Serial port name</param>
        /// <param name="i2cAddress">EEPROM address on the device's i2c bus</param>
        public Device(SerialPortSettings portSettings, string portName, UInt8 i2cAddress) {
            PortSettings = portSettings;
            PortName     = portName;
            I2CAddress   = i2cAddress;
        }

        /// <summary>
        /// Initializes the SPD reader/writer device
        /// </summary>
        /// <param name="portSettings">Serial port settings</param>
        /// <param name="portName">Serial port name</param>
        /// <param name="i2cAddress">EEPROM address on the device's i2c bus</param>
        /// <param name="spdSize">Total EEPROM size</param>
        public Device(SerialPortSettings portSettings, string portName, UInt8 i2cAddress, Ram.SpdSize spdSize) {
            PortSettings = portSettings;
            PortName     = portName;
            I2CAddress   = i2cAddress;
            SpdSize      = spdSize;
        }

        /// <summary>
        /// Serial Port Settings class
        /// </summary>
        public struct SerialPortSettings {
            // Connection settings
            public int BaudRate;
            public bool DtrEnable;
            public bool RtsEnable;

            // Response settings
            public int ResponseTimeout;

            public SerialPortSettings(
                int baudRate        = 115200,
                bool dtrEnable      = true,
                bool rtsEnable      = true,
                int responseTimeout = 10) {
                        BaudRate        = baudRate;
                        DtrEnable       = dtrEnable;
                        RtsEnable       = rtsEnable;
                        ResponseTimeout = responseTimeout;
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
        /// Attempts to establish a connection with the SPD reader/writer device
        /// </summary>
        /// <returns><see langword="true" /> if the connection is established</returns>
        public bool Connect() {
            return ConnectInternal();
        }

        /// <summary>
        /// Disconnects the SPD reader/writer device
        /// </summary>
        /// <returns><see langword="true" /> once the device is disconnected</returns>
        public bool Disconnect() {
            return DisconnectInternal();
        }

        /// <summary>
        /// Disposes device instance
        /// </summary>
        public void Dispose() {
            DisposeInternal();
        }

        /// <summary>
        /// Tests if the device responds to a test command
        /// </summary>
        /// <returns><see langword="true" /> if the device responds properly to test command</returns>
        public bool Test() {
            return TestInternal();
        }

        /// <summary>
        /// Gets supported RAM type(s)
        /// </summary>
        /// <returns>A bitmask representing available RAM supported defined in the <see cref="Ram.Type"/> struct</returns>
        public byte GetRamTypeSupport() {
            return GetRamTypeSupportInternal();
        }

        /// <summary>
        /// Test if the device supports RAM type at firmware level
        /// </summary>
        /// <param name="ramTypeBitmask">RAM type bitmask</param>
        /// <returns><see langword="true" /> if the device supports <see cref="Ram.Type"/> at firmware level</returns>
        public bool GetRamTypeSupport(byte ramTypeBitmask) {
            return GetRamTypeSupportInternal(ramTypeBitmask);
        }

        /// <summary>
        /// Re-evaluate device's RSWP capabilities
        /// </summary>
        /// <returns>A bitmask representing available RAM supported defined in the <see cref="Ram.Type"/> struct</returns>
        public byte RswpRetest() {
            return RswpRetestInternal();
        }

        /// <summary>
        /// Reads a byte from the device
        /// </summary>
        /// <returns>A single byte value received from the device</returns>
        public byte ReadByte() {
            return (byte)_sp.ReadByte();
        }

        /// <summary>
        /// Scans the device for I2C bus devices
        /// </summary>
        /// <returns>An array of addresses on the device's I2C bus</returns>
        public UInt8[] Scan() {
            return ScanInternal();
        }

        /// <summary>
        /// Scans for EEPROM addresses on the device's I2C bus
        /// </summary>
        /// <param name="bitmask">Enable bitmask response</param>
        /// <returns>A bitmask representing available EEPROM devices on the device's I2C bus. Bit 0 is address 80, bit 1 is address 81, and so on.</returns>
        public UInt8 Scan(bool bitmask) {
            return ScanInternal(bitmask);
        }

        /// <summary>
        /// Gets or sets SA1 control pin
        /// </summary>
        public bool PIN_SA1 {
            get => GetConfigPinInternal(SA1_SWITCH);
            set => SetConfigPinInternal(SA1_SWITCH, value);
        }

        /// <summary>
        /// Gets or sets DDR5 offline mode control pin
        /// </summary>
        public bool PIN_OFFLINE {
            get => GetConfigPinInternal(OFFLINE_MODE_SWITCH);
            set => SetOfflineMode(value);
        }

        /// <summary>
        /// Gets or sets High voltage control pin
        /// </summary>
        public bool PIN_VHV {
            get => GetHighVoltageInternal();
            set => SetHighVoltageInternal(value);
        }

        /// <summary>
        /// Controls high voltage state on pin SA0
        /// </summary>
        /// <param name="state">High voltage supply state</param>
        /// <returns><see langword="true" /> when operation is successful</returns>
        public bool SetHighVoltage(bool state) {
            return SetHighVoltageInternal(state);
        }

        /// <summary>
        /// Gets high voltage state on pin SA0
        /// </summary>
        /// <returns><see langword="true" /> if high voltage is applied to pin SA0</returns>
        public bool GetHighVoltage() {
            return GetHighVoltageInternal();
        }

        /// <summary>
        /// Sets specified configuration pin to desired state
        /// </summary>
        /// <param name="pin">Pin name</param>
        /// <param name="state">Pin state</param>
        /// <returns><see langword="true" /> if the config pin has been set</returns>
        public bool SetConfigPin(byte pin, bool state) {
            return SetConfigPinInternal(pin, state);
        }

        /// <summary>
        /// Get specified configuration pin state
        /// </summary>
        /// <returns><see langword="true" /> if pin is high, or <see langword="false" /> when pin is low</returns>
        public bool GetConfigPin(byte pin) {
            return GetConfigPinInternal(pin) == State.ON;
        }

        /// <summary>
        /// Controls DDR5 offline mode operation
        /// </summary>
        /// <param name="state">Offline mode state</param>
        /// <returns><see langword="true" /> when operation completes successfully</returns>
        public bool SetOfflineMode(bool state) {
            return SetOfflineModeInternal(state);
        }

        /// <summary>
        /// Gets DDR5 offline mode status
        /// </summary>
        /// <returns><see langword="true" /> when DDR5 is in offline mode</returns>
        public bool GetOfflineMode() {
            return GetOfflineModeInternal();
        }

        /// <summary>
        /// Resets all config pins to their default state
        /// </summary>
        /// <returns><see langword="true" /> when all config pins are reset</returns>
        public bool ResetAddressPins() {

            PIN_SA1     = State.DEFAULT;
            PIN_VHV     = State.DEFAULT;
            PIN_OFFLINE = State.DEFAULT;

            return !PIN_SA1 && !PIN_VHV && !PIN_OFFLINE;
        }

        /// <summary>
        /// Probes specified EEPROM address
        /// </summary>
        /// <returns><see langword="true" /> if EEPROM is detected at the specified address</returns>
        public bool ProbeAddress() {
            return I2CAddress != 0 && ProbeAddressInternal(I2CAddress);
        }

        /// <summary>
        /// Probes specified EEPROM address
        /// </summary>
        /// <param name="address">EEPROM address</param>
        /// <returns><see langword="true" /> if EEPROM is detected at the specified address</returns>
        public bool ProbeAddress(UInt8 address) {
            return ProbeAddressInternal(address);
        }

        /// <summary>
        /// Clears serial port buffers from unneeded data to prevent unwanted behavior and delays
        /// </summary>
        public void ClearBuffer() {
            ClearBufferInternal();
        }

        /// <summary>
        /// Clears serial port buffers and causes any buffered data to be written
        /// </summary>
        public void FlushBuffer() {
            FlushBufferInternal();
        }

        /// <summary>
        /// Executes commands on the device.
        /// </summary>
        /// <param name="command">Space separated commands to be executed on the device</param>
        /// <returns>A byte received from the device in response</returns>
        public byte ExecuteCommand(byte[] command) {
            return ExecuteCommandInternal(command, 1)[0];
        }

        /// <summary>
        /// Executes commands on the device.
        /// </summary>
        /// <param name="command">Space separated commands to be executed on the device</param>
        /// <param name="length">Number of bytes to receive in response</param>
        /// <returns>A byte array received from the device in response</returns>
        public byte[] ExecuteCommand(byte[] command, uint length) {
            return ExecuteCommandInternal(command, length);
        }

        /// <summary>
        /// Get device's firmware version 
        /// </summary>
        /// <returns>Firmware version number</returns>
        public int GetFirmwareVersion() {
            return GetFirmwareVersionInternal();
        }

        /// <summary>
        /// Device's current assigned user  name
        /// </summary>
        public string CurrentName;

        /// <summary>
        /// Device's user assigned name
        /// </summary>
        public string Name {
            get => GetNameInternal();
            set => SetNameInternal(value);
        }

        /// <summary>
        /// Assigns a name to the Device
        /// </summary>
        /// <param name="name">Device name</param>
        /// <returns><see langword="true" /> when the device name is set</returns>
        public bool SetName(string name) {
            return SetNameInternal(name);
        }

        /// <summary>
        /// Gets Device's name
        /// </summary>
        /// <returns>Device's name</returns>
        public string GetName() {
            return GetNameInternal();
        }

        /// <summary>
        /// Finds devices connected to computer by sending a test command to every serial port device detected
        /// </summary>
        /// <returns>An array of serial port names which have a device or devices connected to</returns>
        public string[] Find() {
            return FindInternal();
        }

        /// <summary>
        /// Describes device's connection state
        /// </summary>
        public bool IsConnected {
            get {
                try {
                    return _sp != null && _sp.IsOpen && _isValid;
                }
                catch {
                    return false;
                }
            }
        }

        /// <summary>
        /// Describes if the device is a valid programmer
        /// </summary>
        public bool IsValid {
            get => _isValid;
            set => _isValid = value;
        }

        /// <summary>
        /// Detects if DDR4 RAM is present on the device's I2C bus
        /// </summary>
        /// <returns><see langword="true" /> if DDR4 is found</returns>
        public bool DetectDdr4() {
            return DetectDdr4Internal(I2CAddress);
        }

        /// <summary>
        /// Detects if DDR4 RAM is present on the device's I2C bus at specified <see cref="address"/>
        /// </summary>
        /// <param name="address">I2C address</param>
        /// <returns><see langword="true" /> if DDR4 is found at <see cref="address"/></returns>
        public bool DetectDdr4(UInt8 address) {
            return DetectDdr4Internal(address);
        }

        /// <summary>
        /// Detects if DDR5 RAM is present on the device's I2C bus
        /// </summary>
        /// <returns><see langword="true" /> if DDR5 is found</returns>
        public bool DetectDdr5() {
            return DetectDdr5Internal(I2CAddress);
        }

        /// <summary>
        /// Detects if DDR5 RAM is present on the device's I2C bus at specified <see cref="address"/>
        /// </summary>
        /// <param name="address">I2C address</param>
        /// <returns><see langword="true" /> if DDR5 is found at <see cref="address"/></returns>
        public bool DetectDdr5(UInt8 address) {
            return DetectDdr5Internal(address);
        }

        /// <summary>
        /// Serial Port connection and data settings
        /// </summary>
        public SerialPortSettings PortSettings;

        /// <summary>
        /// Serial port name the device is connected to
        /// </summary>
        public string PortName;

        /// <summary>
        /// Device's firmware version
        /// </summary>
        public int FirmwareVersion => GetFirmwareVersionInternal();

        /// <summary>
        /// EEPROM address
        /// </summary>
        public UInt8 I2CAddress;

        /// <summary>
        /// EEPROM size
        /// </summary>
        public Ram.SpdSize SpdSize;

        /// <summary>
        /// PortLock object used to prevent other threads from acquiring the lock 
        /// </summary>
        public object PortLock = _portLock;

        /// <summary>
        /// FindLock object used to prevent other threads from acquiring the lock 
        /// </summary>
        public object FindLock = _findLock;

        /// <summary>
        /// Number of bytes to be read from the device
        /// </summary>
        public int BytesToRead {
            get {
                try {
                    return _sp.BytesToRead;
                }
                catch {
                    throw new IOException("No bytes to read");
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
                    throw new IOException("No bytes to write");
                }
            }
        }

        /// <summary>
        /// Bitmask value representing RAM type supported defined in Ram.Bitmask enum
        /// </summary>
        public byte RamTypeSupport {
            get {
                try {
                    return GetRamTypeSupportInternal();
                }
                catch {
                    throw new Exception("Unable to get supported RAM type");
                }
            }
        }


        /// <summary>
        /// Value representing whether the device supports RSWP capabilities based on RAM type supported reported by the device
        /// </summary>
        public bool RswpPresent {
            get {
                return IsConnected &&
                       ((RamTypeSupport & Ram.BitMask.DDR2) == Ram.BitMask.DDR2 ||
                        (RamTypeSupport & Ram.BitMask.DDR3) == Ram.BitMask.DDR3 ||
                        (RamTypeSupport & Ram.BitMask.DDR4) == Ram.BitMask.DDR4 ||
                        (RamTypeSupport & Ram.BitMask.DDR5) == Ram.BitMask.DDR5);
            }
        }

        /// <summary>
        /// Byte stack containing data received from Serial Port
        /// </summary>
        public static Queue<byte> ResponseData = new Queue<byte>();

        /// <summary>
        /// Value indicating whether data reception is complete
        /// </summary>
        public static bool DataReceiving = false;

        /// <summary>
        /// Data Received Handler which read data and puts it into ResponseData queue
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        public static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e) {
            SerialPort __sp = (SerialPort)sender;

            while (__sp != null && 
                   __sp.IsOpen && 
                   __sp.BytesToRead > 0) {
                DataReceiving = true;
                ResponseData.Enqueue((byte)__sp.ReadByte());
            }

            DataReceiving = false;
        }

        /// <summary>
        /// Error Received Handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        public static void ErrorReceivedHandler(object sender, SerialErrorReceivedEventArgs e) {
            SerialPort __sp = (SerialPort)sender;

            throw new Exception($"Error received: {__sp.PortName}");
        }

        /// <summary>
        /// Serial port instance
        /// </summary>
        private SerialPort _sp = new SerialPort();

        /// <summary>
        /// Describes whether the device is valid
        /// </summary>
        private bool _isValid;

        /// <summary>
        /// Attempts to establish a connection with the device
        /// </summary>
        /// <returns><see langword="true" /> if the connection is established</returns>
        private bool ConnectInternal() {
            lock (PortLock) {
                if (!IsConnected) {
                    // New connection settings
                    _sp = new SerialPort {
                        PortName  = PortName,
                        BaudRate  = PortSettings.BaudRate,
                        DtrEnable = PortSettings.DtrEnable,
                        RtsEnable = PortSettings.RtsEnable,
                    };

                    // Event to handle Data Reception
                    _sp.DataReceived += DataReceivedHandler;

                    // Event to handle Errors
                    _sp.ErrorReceived += ErrorReceivedHandler;

                    // Test the connection
                    try {
                        // Establish a connection
                        _sp.Open();

                        // Set valid state to true to allow Test to execute
                        _isValid = true;
                        try {
                            _isValid = TestInternal();
                        }
                        catch {
                            _isValid = false;
                            Dispose();
                        }

                        if (!_isValid) {
                            throw new Exception("Invalid device");
                        }
                    }
                    catch (Exception ex) {
                        throw new Exception($"Unable to connect ({PortName}): {ex.Message}");
                    }
                }
            }
            return IsConnected && _isValid;
        }

        /// <summary>
        /// Disconnect from the device
        /// </summary>
        /// <returns><see langword="true" /> once the device is disconnected</returns>
        private bool DisconnectInternal() {
            lock (PortLock) {
                if (IsConnected) {
                    try {
                        // Remove handlers
                        _sp.DataReceived  -= DataReceivedHandler;
                        _sp.ErrorReceived -= ErrorReceivedHandler;
                        // Close connection
                        _sp.Close();
                        // Reset valid state
                        _isValid = false;
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
        private void DisposeInternal() {
            lock (PortLock) {
                if (_sp.IsOpen) {
                    _sp.Close();
                }
            }
        }

        /// <summary>
        /// Tests if the device is able to communicate
        /// </summary>
        /// <returns><see langword="true" /> if the device responds to a test command</returns>
        private bool TestInternal() {
            lock (PortLock) {
                try {
                    return IsConnected && 
                           ExecuteCommand(new[] { TESTCOMM }) == Response.WELCOME;
                }
                catch {
                    throw new Exception($"Unable to test {PortName}");
                }
            }
        }

        /// <summary>
        /// Gets supported RAM type(s)
        /// </summary>
        /// <returns>A bitmask representing available RAM supported defined in the <see cref="Ram.Type"/> struct</returns>
        private byte GetRamTypeSupportInternal() {
            lock (PortLock) {
                try {
                    return ExecuteCommand(new[] { RAMSUPPORT });
                }
                catch {
                    throw new Exception($"Unable to get {PortName} supported RAM");
                }
            }
        }

        /// <summary>
        /// Test if the device supports RAM type at firmware level
        /// </summary>
        /// <param name="ramTypeBitmask">RAM type bitmask</param>
        /// <returns><see langword="true" /> if the device supports <see cref="Ram.Type"/> at firmware level</returns>
        private bool GetRamTypeSupportInternal(byte ramTypeBitmask) {
            return (GetRamTypeSupportInternal() & ramTypeBitmask) == ramTypeBitmask;
        }

        /// <summary>
        /// Re-evaluate device's RSWP capabilities
        /// </summary>
        /// <returns>A bitmask representing available RAM supported defined in the <see cref="Ram.Type"/> struct</returns>
        private byte RswpRetestInternal() {
            lock (PortLock) {
                try {
                    return ExecuteCommand(new[] { RETEST });
                }
                catch {
                    throw new Exception($"Unable to get {PortName} supported RAM");
                }
            }
        }

        /// <summary>
        /// Sets DDR5 offline mode 
        /// </summary>
        /// <param name="state">Offline mode state</param>
        /// <returns><see langword="true" /> when operation is successful</returns>
        private bool SetOfflineModeInternal(bool state) {
            lock (PortLock) {
                try {
                    return ExecuteCommand(new[] { PINCONTROL, OFFLINE_MODE_SWITCH, BoolToInt(state) }) == Response.SUCCESS;
                }
                catch {
                    throw new Exception($"Unable to set offline mode on {PortName}");
                }
            }
        }

        /// <summary>
        /// Gets DDR5 offline mode status
        /// </summary>
        /// <returns><see langword="true" /> when DDR5 is in offline mode</returns>
        private bool GetOfflineModeInternal() {
            lock (PortLock) {
                try {
                    return ExecuteCommand(new[] { PINCONTROL, OFFLINE_MODE_SWITCH, GET }) == Response.SUCCESS;
                }
                catch {
                    throw new Exception($"Unable to get offline mode status on {PortName}");
                }
            }
        }

        /// <summary>
        /// Scans for EEPROM addresses on the device's I2C bus
        /// </summary>
        /// <returns>An array of EEPROM addresses present on the device's I2C bus</returns>
        private UInt8[] ScanInternal() {
            Queue<UInt8> addresses = new Queue<UInt8>();

            lock (PortLock) {
                try {
                    if (IsConnected) {
                        byte _response = Scan(true);

                        if (_response == Response.NULL) {
                            return new byte[0];
                        }

                        for (UInt8 i = 0; i < 8; i++) {
                            if (GetBit(_response, i) == 1) {
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
        /// <returns>A bitmask representing available EEPROM addresses on the device's I2C bus. Bit 0 is address 80, bit 1 is address 81, and so on.</returns>
        private UInt8 ScanInternal(bool bitmask) {
            if (bitmask) {
                lock (PortLock) {
                    try {
                        if (IsConnected) {
                            return ExecuteCommand(new[] { SCANBUS });
                        }
                    }
                    catch {
                        throw new Exception($"Unable to scan I2C bus on {PortName}");
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Sets specified configuration pin to desired state
        /// </summary>
        /// <param name="pin">Config pin</param>
        /// <param name="state">Config pin state</param>
        /// <returns><see langword="true" /> if the config pin has been set</returns>
        private bool SetConfigPinInternal(byte pin, bool state) {
            lock (PortLock) {
                try {
                    return IsConnected &&
                           ExecuteCommand(new[] { PINCONTROL, pin, BoolToInt(state) }) == Response.SUCCESS;
                }
                catch {
                    throw new Exception($"Unable to set SA pin state on {PortName}");
                }
            }
        }

        /// <summary>
        /// Get specified configuration pin state
        /// </summary>
        /// <param name="pin">Config pin</param>
        /// <returns><see langword="true" /> if pin is high, or <see langword="false" /> when pin is low</returns>
        private bool GetConfigPinInternal(byte pin) {
            lock (PortLock) {
                try {
                    return IsConnected &&
                           ExecuteCommand(new[] { PINCONTROL, pin, GET }) == Response.ON;
                }
                catch {
                    throw new Exception($"Unable to get address SA pin state on {PortName}");
                }
            }
        }

        /// <summary>
        /// Sets high voltage on or off on pin SA0
        /// </summary>
        /// <param name="state">High voltage supply state</param>
        /// <returns><see langword="true" /> if operation is completed</returns>
        private bool SetHighVoltageInternal(bool state) {
            lock (PortLock) {
                try {
                    return IsConnected &&
                           ExecuteCommand(new[] { PINCONTROL, HIGH_VOLTAGE_SWITCH, BoolToInt(state) }) == Response.SUCCESS;
                }
                catch {
                    throw new Exception($"Unable to set High Voltage state on {PortName}");
                }
            }
        }

        /// <summary>
        /// Gets high voltage state on pin SA0
        /// </summary>
        /// <returns><see langword="true" /> if high voltage is applied to pin SA0</returns>
        private bool GetHighVoltageInternal() {
            lock (PortLock) {
                try {
                    return IsConnected &&
                           ExecuteCommand(new[] { PINCONTROL, HIGH_VOLTAGE_SWITCH, GET }) == Response.ON;
                }
                catch {
                    throw new Exception($"Unable to get High Voltage state on {PortName}");
                }
            }
        }

        /// <summary>
        /// Tests if the address is present on the device's I2C bus
        /// </summary>
        /// <param name="address">EEPROM address</param>
        /// <returns><see langword="true" /> if the address is accessible</returns>
        private bool ProbeAddressInternal(UInt8 address) {
            lock (PortLock) {
                try {
                    return IsConnected &&
                           ExecuteCommand(new[] { PROBEADDRESS, address }) == Response.ACK;
                }
                catch {
                    throw new Exception($"Unable to probe address {address} on {PortName}");
                }
            }
        }

        /// <summary>
        /// Get Device's firmware version 
        /// </summary>
        /// <returns>Firmware version number</returns>
        private int GetFirmwareVersionInternal() {
            int _version = 0;
            lock (PortLock) {
                try {
                    if (IsConnected) {
                        _version = Int32.Parse(
                            Encoding.Default.GetString(
                                ExecuteCommand(new[] { GETVERSION }, (uint)Settings.MINVERSION.ToString().Length)
                            )
                        );
                    }
                }
                catch {
                    throw new Exception($"Unable to get firmware version on {PortName}");
                }
            }
            return _version;
        }

        /// <summary>
        /// Assigns a name to the Device
        /// </summary>
        /// <param name="name">Device name</param>
        /// <returns><see langword="true" /> when the device name is set</returns>
        private bool SetNameInternal(string name) {
            if (name == null) throw new ArgumentNullException("Name can't be null");
            if (name == "") throw new ArgumentException("Name can't be blank");
            if (name.Length > 16) throw new ArgumentException("Name can't be longer than 16 characters");

            lock (PortLock) {
                try {
                    if (IsConnected) {
                        string _name = name.Trim();

                        if (_name == GetName()) {
                            return false;
                        }

                        // Prepare a byte array containing cmd byte + name length + name
                        byte[] _nameCommand = new byte[1 + 1 + _name.Length];
                        // command byte at position 0
                        _nameCommand[0] = NAME;
                        // name length at position 1
                        _nameCommand[1] = (byte)_name.Length;
                        // copy new name to byte array
                        Array.Copy(Encoding.ASCII.GetBytes(_name), 0, _nameCommand, 2, _name.Length);
                        
                        if (ExecuteCommand(_nameCommand) == Response.SUCCESS) {
                            CurrentName = _name;
                            return true;
                        }
                    }
                }
                catch {
                    throw new Exception($"Unable to assign name to {PortName}");
                }
            }

            return false;
        }

        /// <summary>
        /// Gets Device's name
        /// </summary>
        /// <returns>Device's name</returns>
        private string GetNameInternal() {
            lock (PortLock) {
                try {
                    if (CurrentName == null) {
                        CurrentName = Encoding.Default.GetString(ExecuteCommand(new[] { NAME, GET }, 16)).Split('\0')[0];
                    }
                    return CurrentName;
                }
                catch {
                    throw new Exception($"Unable to get {PortName} name");
                }
            }
        }

        /// <summary>
        /// Finds devices connected to computer 
        /// </summary>
        /// <returns>An array of serial port names which have a device or devices connected to</returns>
        public string[] FindInternal() {
            Stack<string> _result = new Stack<string>();

            lock (FindLock) {
                foreach (string _portName in SerialPort.GetPortNames().Distinct().ToArray()) {
                    try {
                        Device _device = new Device(PortSettings, _portName);

                        lock (_device.PortLock) {
                            if (_device.Connect()) {
                                _device.Dispose();
                                _result.Push(_portName);
                            }
                        }
                    }
                    catch {
                        // Do nothing
                    }
                }
            }

            return _result.ToArray();
        }

        /// <summary>
        /// Detects if DDR4 RAM is present on the device's I2C bus
        /// </summary>
        /// <param name="address">I2C address</param>
        /// <returns><see langword="true" /> if DDR4 is found</returns>
        private bool DetectDdr4Internal(UInt8 address) {
            lock (PortLock) {
                try {
                    return IsConnected &&
                           ExecuteCommand(new[] { DDR4DETECT, address }) == Response.SUCCESS;
                }
                catch {
                    throw new Exception($"Error detecting DDR4 on {PortName}");
                }
            }
        }

        /// <summary>
        /// Detects if DDR5 RAM is present on the device's I2C bus
        /// </summary>
        /// <param name="address">I2C address</param>
        /// <returns><see langword="true" /> if DDR5 is found</returns>
        private bool DetectDdr5Internal(UInt8 address) {
            lock (PortLock) {
                try {
                    return IsConnected &&
                           ExecuteCommand(new[] { DDR5DETECT, address }) == Response.SUCCESS;
                }
                catch {
                    throw new Exception($"Error detecting DDR5 on {PortName}");
                }
            }
        }

        /// <summary>
        /// Clears serial port buffers from unneeded data to prevent unwanted behavior and delays
        /// </summary>
        private void ClearBufferInternal() {
            lock (PortLock) {
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
        private void FlushBufferInternal() {
            lock (PortLock) {
                if (IsConnected) {
                    _sp.BaseStream.Flush();
                }
            }
        }

        /// <summary>
        /// Executes commands on the device.
        /// </summary>
        /// <param name="command">Bytes to be sent to the device</param>
        /// <param name="responseLength">Number of bytes to receive in response</param>
        /// <returns>A byte array received from the device in response</returns>
        private byte[] ExecuteCommandInternal(byte[] command, uint responseLength) {
            if (command.Length == 0) {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(command));
            }

            Queue<byte> _response = new Queue<byte>();

            lock (PortLock) {
                try {
                    // Clear input and output buffers
                    ClearBuffer();

                    // Send the command to device
                    _sp.Write(command, 0, command.Length);

                    // Flush the buffer
                    FlushBuffer();

                    // Check response length
                    if (responseLength == 0) {
                        return new byte[0];
                    }

                    // Timeout monitoring start
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    while (PortSettings.ResponseTimeout * 1000 > sw.ElapsedMilliseconds) {
                        // Check connection
                        if (!IsConnected) {
                            throw new IOException($"{PortName} not connected");
                        }

                        // Wait for data
                        if (ResponseData != null && ResponseData.Count >= responseLength && !DataReceiving) {
                            _response = ResponseData;
                            break;
                        }

                        if (command[0] == READBYTE ||
                            command[0] == WRITEBYTE) {
                            Wait(1);
                        }
                        else {
                            Wait();
                        }
                    }

                    if (_response.Count == 0 || _response.Count < responseLength) {
                        throw new TimeoutException("Response timeout");
                    }

                    return _response.ToArray();
                }
                catch {
                    throw new IOException($"{PortName} failed to execute command {command}");
                }
            }
        }

        /// <summary>
        /// Delays execution 
        /// </summary>
        /// <param name="timeout">Delay in milliseconds</param>
        private void Wait(int timeout = 10) {
            Thread.Sleep(timeout);
        }

        /// <summary>
        /// PortLock object used to prevent other threads from acquiring the lock 
        /// </summary>
        private static readonly object _portLock = new object();

        /// <summary>
        /// FindLock object used to prevent other threads from acquiring the lock
        /// </summary>
        private static readonly object _findLock = new object();
    }
}
