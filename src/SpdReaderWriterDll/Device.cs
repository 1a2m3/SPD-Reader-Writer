using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
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

            // Data settings
            public int DataBits;
            public Handshake Handshake;
            public string NewLine;
            public Parity Parity;
            public StopBits StopBits;

            // Response settings
            public bool RaiseEvent;
            public int ResponseTimeout;

            public SerialPortSettings(
                int baudRate = 115200,
                bool dtrEnable = true,
                bool rtsEnable = true,
                int dataBits = 8,
                Handshake handshake = Handshake.None,
                string newLine = "\n",
                Parity parity = Parity.None,
                StopBits stopBits = StopBits.One,
                bool raiseEvent = false,
                int responseTimeout = 10) {
                BaudRate = baudRate;
                DtrEnable = dtrEnable;
                RtsEnable = rtsEnable;
                DataBits = dataBits;
                Handshake = handshake;
                NewLine = newLine.Replace("\\n", "\n").Replace("\\r", "\r");
                Parity = parity;
                StopBits = stopBits;
                RaiseEvent = raiseEvent;
                ResponseTimeout = responseTimeout;
            }

            public override string ToString() {
                string _stopBits = (int)StopBits == 3 ? "1.5" : ((int)StopBits).ToString();
                string _parity = Parity.ToString().Substring(0, 1);

                return $"{BaudRate}-{_parity}-{DataBits}-{_stopBits}";
            }
        }

        /// <summary>
        /// Attempts to establish a connection with the SPD reader/writer device
        /// </summary>
        /// <returns><see langword="true" /> if the connection is established</returns>
        public bool Connect() {
            return Connect(this);
        }

        /// <summary>
        /// Disconnects the SPD reader/writer device
        /// </summary>
        /// <returns><see langword="true" /> once the device is disconnected</returns>
        public bool Disconnect() {
            return Disconnect(this);
        }

        /// <summary>
        /// Disposes device instance
        /// </summary>
        public void Dispose() {
            Dispose(this);
        }

        /// <summary>
        /// Tests if the device responds to a test command
        /// </summary>
        /// <returns><see langword="true" /> if the device responds properly to test command</returns>
        public bool Test() {
            return Test(this);
        }

        /// <summary>
        /// Gets supported RAM type(s)
        /// </summary>
        /// <returns>A bitmask representing available RAM supported defined in the <see cref="Ram.Type"/> struct</returns>
        public byte RamTypeSupport() {
            return RamTypeSupport(this);
        }

        /// <summary>
        /// Test if the device supports RAM type at firmware level
        /// </summary>
        /// <param name="ramTypeBitmask">RAM type bitmask</param>
        /// <returns><see langword="true" /> if the device supports <see cref="Ram.Type"/> at firmware level</returns>
        public bool RamTypeSupport(Ram.BitMask ramTypeBitmask) {
            return RamTypeSupport(this, ramTypeBitmask);
        }

        /// <summary>
        /// Tests if the device is capable of SA pin control
        /// </summary>
        /// <returns>Bitmask representing programmatic address pins configuration and HV control availability</returns>
        public byte GetPinControls() {
            return GetPinControls(this);
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
            return Scan(this);
        }

        /// <summary>
        /// Scans for EEPROM addresses on the device's I2C bus
        /// </summary>
        /// <param name="bitmask">Enable bitmask response</param>
        /// <returns>A bitmask representing available EEPROM devices on the device's I2C bus. Bit 0 is address 80, bit 1 is address 81, and so on.</returns>
        public UInt8 Scan(bool bitmask) {
            return Scan(this, bitmask);
        }

        /// <summary>
        /// Gets or sets SA1 control pin
        /// </summary>
        public bool PIN_SA1 {
            get => GetConfigPin(Pin.SA1_SWITCH);
            set => SetConfigPin(Pin.SA1_SWITCH, value);
        }

        /// <summary>
        /// Gets or sets DDR5 offline mode control pin
        /// </summary>
        public bool PIN_OFFLINE {
            get => GetConfigPin(Pin.OFFLINE_MODE_SWITCH);
            set => SetOfflineMode(value);
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
        /// <returns><see langword="true" /> when operation is successful</returns>
        public bool SetHighVoltage(bool state) {
            return SetHighVoltage(this, state);
        }

        /// <summary>
        /// Gets high voltage state on pin SA0
        /// </summary>
        /// <returns><see langword="true" /> if high voltage is applied to pin SA0</returns>
        public bool GetHighVoltage() {
            return GetHighVoltage(this);
        }

        /// <summary>
        /// Sets specified configuration pin to desired state
        /// </summary>
        /// <param name="pin">Pin name</param>
        /// <param name="state">Pin state</param>
        /// <returns><see langword="true" /> if the address has been set</returns>
        public bool SetConfigPin(byte pin, bool state) {
            return SetConfigPin(this, pin, state);
        }

        /// <summary>
        /// Get specified configuration pin state
        /// </summary>
        /// <returns><see langword="true" /> if pin is high, or <see langword="false" /> when pin is low</returns>
        public bool GetConfigPin(byte pin) {
            return GetConfigPin(this, pin) == Pin.State.ON;
        }

        /// <summary>
        /// Controls DDR5 offline mode operation
        /// </summary>
        /// <param name="state">Offline mode state</param>
        /// <returns><see langword="true" /> when operation completes successfully</returns>
        public bool SetOfflineMode(bool state) {
            return SetOfflineMode(this, state);
        }

        /// <summary>
        /// Gets DDR5 offline mode status
        /// </summary>
        /// <returns><see langword="true" /> when DDR5 is in offline mode</returns>
        public bool GetOfflineMode() {
            return GetOfflineMode(this);
        }

        /// <summary>
        /// Resets all config pins to their default state
        /// </summary>
        /// <returns><see langword="true" /> when all config pins are reset</returns>
        public bool ResetAddressPins() {

            PIN_SA1 = Pin.State.DEFAULT;
            PIN_VHV = Pin.State.DEFAULT;
            PIN_OFFLINE = Pin.State.DEFAULT;

            return !PIN_SA1 && !PIN_VHV && !PIN_OFFLINE;
        }

        /// <summary>
        /// Probes specified EEPROM address
        /// </summary>
        /// <returns><see langword="true" /> if EEPROM is detected at the specified address</returns>
        public bool ProbeAddress() {
            return I2CAddress != 0 && ProbeAddress(this, I2CAddress);
        }

        /// <summary>
        /// Probes specified EEPROM address
        /// </summary>
        /// <param name="address">EEPROM address</param>
        /// <returns><see langword="true" /> if EEPROM is detected at the specified address</returns>
        public bool ProbeAddress(UInt8 address) {
            return ProbeAddress(this, address);
        }

        /// <summary>
        /// Clears serial port buffers from unneeded data to prevent unwanted behavior and delays
        /// </summary>
        public void ClearBuffer() {
            ClearBuffer(this);
        }

        /// <summary>
        /// Clears serial port buffers and causes any buffered data to be written
        /// </summary>
        public void FlushBuffer() {
            FlushBuffer(this);
        }

        /// <summary>
        /// Executes commands on the device.
        /// </summary>
        /// <param name="command">Space separated commands to be executed on the device</param>
        /// <returns>A byte received from the device in response</returns>
        public byte ExecuteCommand(byte[] command) {
            return ExecuteCommand(command, 1)[0];
        }

        /// <summary>
        /// Executes commands on the device.
        /// </summary>
        /// <param name="command">Space separated commands to be executed on the device</param>
        /// <param name="length">Number of bytes to receive in response</param>
        /// <returns>A byte array received from the device in response</returns>
        public byte[] ExecuteCommand(byte[] command, uint length) {
            return ExecuteCommand(this, command, length);
        }

        /// <summary>
        /// Get device's firmware version 
        /// </summary>
        /// <returns>Firmware version number</returns>
        public int GetFirmwareVersion() {
            return GetFirmwareVersion(this);
        }

        /// <summary>
        /// Device's current assigned user  name
        /// </summary>
        public string CurrentName;

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
        /// <returns><see langword="true" /> when the device name is set</returns>
        public bool SetName(string name) {
            return SetName(this, name);
        }

        /// <summary>
        /// Gets Device's name
        /// </summary>
        /// <returns>Device's name</returns>
        public string GetName() {
            return GetName(this);
        }

        /// <summary>
        /// Finds devices connected to computer by sending a test command to every serial port device detected
        /// </summary>
        /// <returns>An array of serial port names which have a device or devices connected to</returns>
        public string[] Find() {
            return Find(this);
        }

        /// <summary>
        /// Describes device's connection state
        /// </summary>
        public bool IsConnected {
            get {
                try {
                    return _sp != null && _sp.IsOpen && IsValid;
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
            get => _IsValid;
            set => _IsValid = value;
        }

        /// <summary>
        /// Detects if DDR4 RAM is present on the device's I2C bus
        /// </summary>
        /// <returns><see langword="true" /> if DDR4 is found</returns>
        public bool DetectDdr4() {
            return DetectDdr4(I2CAddress);
        }

        /// <summary>
        /// Detects if DDR4 RAM is present on the device's I2C bus at specified <see cref="address"/>
        /// </summary>
        /// <param name="address">I2C address</param>
        /// <returns><see langword="true" /> if DDR4 is found at <see cref="address"/></returns>
        public bool DetectDdr4(UInt8 address) {
            return DetectDdr4(this, address);
        }

        /// <summary>
        /// Detects if DDR5 RAM is present on the device's I2C bus
        /// </summary>
        /// <returns><see langword="true" /> if DDR5 is found</returns>
        public bool DetectDdr5() {
            return DetectDdr5(I2CAddress);
        }

        /// <summary>
        /// Detects if DDR5 RAM is present on the device's I2C bus at specified <see cref="address"/>
        /// </summary>
        /// <param name="address">I2C address</param>
        /// <returns><see langword="true" /> if DDR5 is found at <see cref="address"/></returns>
        public bool DetectDdr5(UInt8 address) {
            return DetectDdr5(this, address);
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
        public int FirmwareVersion => GetFirmwareVersion(this);

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
        /// Indicates whether the device supports RSWP and PSWP capabilities, the value is assigned by GetPinControls method
        /// </summary>
        public bool AdvancedPinControlSupported;

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
        private bool _IsValid;

        /// <summary>
        /// Attempts to establish a connection with the device
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> if the connection is established</returns>
        private static bool Connect(Device device) {
            lock (device.PortLock) {
                if (!device.IsConnected) {
                    // New connection settings
                    device._sp = new SerialPort {
                        // Port settings
                        PortName  = device.PortName,
                        BaudRate  = device.PortSettings.BaudRate,
                        DtrEnable = device.PortSettings.DtrEnable,
                        RtsEnable = device.PortSettings.RtsEnable,
                        Handshake = device.PortSettings.Handshake,

                        // Data settings
                        DataBits  = device.PortSettings.DataBits,
                        NewLine   = device.PortSettings.NewLine,
                        Parity    = device.PortSettings.Parity,
                        StopBits  = device.PortSettings.StopBits,
                    };

                    // Event to handle Data Reception
                    if (device.PortSettings.RaiseEvent) {
                        device._sp.DataReceived += DataReceivedHandler;
                    }

                    // Event to handle Errors
                    device._sp.ErrorReceived += ErrorReceivedHandler;

                    // Test the connection
                    try {
                        // Establish a connection
                        device._sp.Open();

                        // Reset 'valid' state to allow Test() run
                        device._IsValid = true;

                        if (!device.Test()) {
                            device._IsValid = false;
                        }

                        if (!device._IsValid) {
                            throw new Exception("Invalid device");
                        }
                    }
                    catch (Exception ex) {
                        throw new Exception($"Unable to connect ({device.PortName}): {ex.Message}");
                    }
                }
            }
            return device.IsConnected && device.IsValid;
        }

        /// <summary>
        /// Disconnect from the device
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> once the device is disconnected</returns>
        private static bool Disconnect(Device device) {

            lock (device.PortLock) {
                if (device.IsConnected) {
                    try {
                        if (device.PortSettings.RaiseEvent) {
                            device._sp.DataReceived -= DataReceivedHandler;
                        }
                        device._sp.ErrorReceived -= ErrorReceivedHandler;
                        device._sp.Close();
                        device.IsValid = false;
                    }
                    catch (Exception ex) {
                        throw new Exception($"Unable to disconnect ({device.PortName}): {ex.Message}");
                    }  
                    
                }

                return !device.IsConnected;
            }
        }

        /// <summary>
        /// Disposes device instance
        /// </summary>
        /// <param name="device">Device instance</param>
        private static void Dispose(Device device) {
            lock (device.PortLock) {
                if (device.IsConnected) {
                    device.ClearBuffer();
                    device._sp.Dispose();
                }

                device = null;
            }
        }

        /// <summary>
        /// Tests if the device is able to communicate
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> if the device responds to a test command</returns>
        private static bool Test(Device device) {
            lock (device.PortLock) {
                try {
                    return device.IsConnected &&
                           device.ExecuteCommand(new[] {Command.TESTCOMM}) == Response.WELCOME;
                }
                catch {
                    throw new Exception($"Unable to test {device.PortName}");
                }
            }
        }

        /// <summary>
        /// Gets supported RAM type(s)
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns>A bitmask representing available RAM supported defined in the RAMTYPE struct</returns>
        private static byte RamTypeSupport(Device device) {
            lock (device.PortLock) {
                try {
                    return device.ExecuteCommand(new[] { Command.RAMSUPPORT});
                }
                catch {
                    throw new Exception($"Unable to get {device.PortName} supported RAM");
                }
            }
        }

        /// <summary>
        /// Test if the device supports RAM type at firmware level
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="ramTypeBitmask">RAM type bitmask</param>
        /// <returns><see langword="true" /> if the device supports <see cref="Ram.Type"/> at firmware level</returns>
        private static bool RamTypeSupport(Device device, Ram.BitMask ramTypeBitmask) {
            return ((Ram.BitMask)device.RamTypeSupport() & ramTypeBitmask) == ramTypeBitmask;
        }

        /// <summary>
        /// Sets DDR5 offline mode 
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="state">Offline mode state</param>
        /// <returns><see langword="true" /> when operation is successful</returns>
        private bool SetOfflineMode(Device device, bool state) {
            lock (device.PortLock) {
                try {
                    return device.ExecuteCommand(new[] { Command.PINCONTROL, Pin.OFFLINE_MODE_SWITCH, Spd.BoolToInt(state) }) == Response.SUCCESS;
                }
                catch {
                    throw new Exception($"Unable to set offline mode on {device.PortName}");
                }
            }
        }

        private bool GetOfflineMode(Device device) {
            lock (device.PortLock) {
                try {
                    return device.ExecuteCommand(new[] { Command.PINCONTROL, Pin.OFFLINE_MODE_SWITCH, Command.GET }) == Response.SUCCESS;
                }
                catch {
                    throw new Exception($"Unable to get offline mode status on {device.PortName}");
                }
            }
        }

        /// <summary>
        /// Tests if the device supports programmatic address pins configuration and HV control, used to determine device's RSWP and PSWP capabilities
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns>Bitmask representing programmatic address pins configuration and HV control availability</returns>
        private static byte GetPinControls(Device device) {
            lock (device.PortLock) {
                try {
                    if (device.IsConnected) {
                        // Skip tests if multiple or no EEPROMs are present, as results might be inaccurate or unpredictable
                        if (device.Scan().Length != 1) {
                            return 0;
                        }

                        bool _testSA1 = false;
                        bool _testVHV = false;
                        bool _testOFL = false;
                        byte _i2cBus  = 0;

                        // Reset SA pins
                        while (!device.ResetAddressPins()) {
                            Wait();
                        }

                        // Test SA1 pin control
                        _i2cBus   = device.Scan(true);
                        device.PIN_SA1 = Pin.State.ON;
                        _testSA1  = device.PIN_SA1;
                        _testSA1 &= _i2cBus != device.Scan(true);

                        
                        // Test HV pin control
                        device.PIN_VHV = Pin.State.ON;
                        _testVHV = device.PIN_VHV;

                        // Test Offline switch control
                        device.PIN_OFFLINE = Pin.State.ON;
                        _testOFL = device.PIN_OFFLINE;

                        // Reset SA pins
                        device.ResetAddressPins();

                        return (byte)
                            ((_testSA1 ? Response.SA1_TEST_OK : Response.SA1_TEST_NA) |
                             (_testVHV ? Response.VHV_TEST_OK : Response.VHV_TEST_NA) |
                             (_testOFL ? Response.OFFLINE_TEST_OK: Response.OFFLINE_TEST_NA));
                    }
                }
                catch {
                    throw new Exception($"GetPinControls: Unable to determine if {device.PortName} supports pin control.");
                }
            }

            return Response.SA1_TEST_NA | Response.VHV_TEST_NA | Response.OFFLINE_TEST_NA;
        }
        
        /// <summary>
        /// Scans for EEPROM addresses on the device's I2C bus
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns>An array of EEPROM addresses present on the device's I2C bus</returns>
        private static UInt8[] Scan(Device device) {
            Queue<UInt8> addresses = new Queue<UInt8>();

            lock (device.PortLock) {
                try {
                    if (device.IsConnected) {
                        byte _response = device.Scan(true);

                        for (UInt8 i = 0; i <= 8; i++) {
                            if ((byte)((_response >> i) & 1) == 1) {
                                addresses.Enqueue((byte)(80 + i));
                            }
                        }
                    }
                }
                catch {
                    throw new Exception($"Unable to scan I2C bus on {device.PortName}");
                }
            }

            return addresses.ToArray();
        }

        /// <summary>
        /// Scans for EEPROM addresses on the device's I2C bus
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="bitmask">Enable bitmask response</param>
        /// <returns>A bitmask representing available EEPROM devices on the device's I2C bus. Bit 0 is address 80, bit 1 is address 81, and so on.</returns>
        private static UInt8 Scan(Device device, bool bitmask) {
            if (bitmask) {
                lock (device.PortLock) {
                    try {
                        if (device.IsConnected) {
                            return device.ExecuteCommand(new[] { Command.SCANBUS });
                        }
                    }
                    catch {
                        throw new Exception($"Unable to scan I2C bus on {device.PortName}");
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Sets specified configuration pin to desired state
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="state">Config pin state</param>
        /// <returns><see langword="true" /> if the Select Address pin has been set</returns>
        private static bool SetConfigPin(Device device, byte pin, bool state) {
            lock (device.PortLock) {
                try {
                    return device.IsConnected &&
                           device.ExecuteCommand(new[] { Command.PINCONTROL, pin, Spd.BoolToInt(state) }) == Response.SUCCESS;
                }
                catch {
                    throw new Exception($"Unable to set SA pin state on {device.PortName}");
                }
            }
        }

        /// <summary>
        /// Get specified configuration pin state
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> if pin is high, or <see langword="false" /> when pin is low</returns>
        private static bool GetConfigPin(Device device, byte pin) {
            lock (device.PortLock) {
                try {
                    return device.IsConnected &&
                           device.ExecuteCommand(new[] { Command.PINCONTROL, pin, Command.GET }) == Response.ON;
                }
                catch {
                    throw new Exception($"Unable to get address SA pin state on {device.PortName}");
                }
            }
        }

        /// <summary>
        /// Sets high voltage on or off on pin SA0
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="state">High voltage supply state</param>
        /// <returns><see langword="true" /> if operation is completed</returns>
        private static bool SetHighVoltage(Device device, bool state) {
            lock (device.PortLock) {
                try {
                    return device.IsConnected &&
                           device.ExecuteCommand(new[] { Command.PINCONTROL, Pin.HIGH_VOLTAGE_SWITCH, Spd.BoolToInt(state) }) == Response.SUCCESS;
                }
                catch {
                    throw new Exception($"Unable to set High Voltage state on {device.PortName}");
                }
            }
        }

        /// <summary>
        /// Gets high voltage state on pin SA0
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> if high voltage is applied to pin SA0</returns>
        private static bool GetHighVoltage(Device device) {
            lock (device.PortLock) {
                try {
                    return device.IsConnected &&
                           device.ExecuteCommand(new[] { Command.PINCONTROL, Pin.HIGH_VOLTAGE_SWITCH, Command.GET }) == Response.ON;
                }
                catch {
                    throw new Exception($"Unable to get High Voltage state on {device.PortName}");
                }
            }
        }

        /// <summary>
        /// Tests if the address is present on the device's I2C bus
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="address">EEPROM address</param>
        /// <returns><see langword="true" /> if the address is accessible</returns>
        private static bool ProbeAddress(Device device, UInt8 address) {
            lock (device.PortLock) {
                try {
                    return device.IsConnected &&
                           device.ExecuteCommand(new[] { Command.PROBEADDRESS, address }) == Response.ACK;
                }
                catch {
                    throw new Exception($"Unable to probe address {address} on {device.PortName}");
                }
            }
        }

        /// <summary>
        /// Get Device's firmware version 
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns>Firmware version number</returns>
        private int GetFirmwareVersion(Device device) {
            int _version = 0;
            lock (device.PortLock) {
                try {
                    if (device.IsConnected) {
                        _version = Int32.Parse(
                            Encoding.Default.GetString(
                                device.ExecuteCommand(new[] { Command.GETVERSION }, (uint)Settings.MINVERSION.ToString().Length)
                            )
                        );
                    }
                }
                catch {
                    throw new Exception($"Unable to get firmware version on {device.PortName}");
                }
            }
            return _version;
        }

        /// <summary>
        /// Assigns a name to the Device
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="name">Device name</param>
        /// <returns><see langword="true" /> when the device name is set</returns>
        private bool SetName(Device device, string name) {
            if (name == null) throw new ArgumentNullException("Name can't be null");
            if (name == "") throw new ArgumentException("Name can't be blank");

            lock (device.PortLock) {
                try {
                    if (device.IsConnected) {
                        string _name = name.Trim();

                        if (_name == GetName()) {
                            return false;
                        }

                        // byte array containing cmd byte + name length + name
                        byte[] _nameCommand = new byte[1 + 1 + _name.Length];
                        // command byte at position 0
                        _nameCommand[0] = Command.NAME;
                        // name length at position 1
                        _nameCommand[1] = (byte)_name.Length;
                        // copy new name to byte array
                        Array.Copy(Encoding.ASCII.GetBytes(_name), 0, _nameCommand, 2, _name.Length);
                        
                        if (device.ExecuteCommand(_nameCommand) == Response.SUCCESS) {
                            CurrentName = _name;
                            return true;
                        }
                    }
                }
                catch {
                    throw new Exception($"Unable to assign name to {device.PortName}");
                }
            }

            return false;
        }

        /// <summary>
        /// Gets Device's name
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns>Device's name</returns>
        private string GetName(Device device) {
            lock (device.PortLock) {
                try {
                    if (CurrentName == null) {
                        CurrentName = Encoding.Default.GetString(device.ExecuteCommand(new[] { Command.NAME, Command.GET }, 16)).Split('\0')[0];
                    }
                    return CurrentName;
                }
                catch {
                    throw new Exception($"Unable to get {device.PortName} name");
                }
            }
        }

        /// <summary>
        /// Finds devices connected to computer 
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns>An array of serial port names which have a device or devices connected to</returns>
        public string[] Find(Device device) {
            Stack<string> _result = new Stack<string>();

            lock (device.FindLock) {
                foreach (string _portName in SerialPort.GetPortNames().Distinct().ToArray()) {
                    try {
                        Device _device = new Device(PortSettings, _portName);

                        lock (_device.PortLock) {
                            if (_device.Connect()) {
                                _result.Push(_portName);
                                _device.Dispose();
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
        /// <param name="device">Device instance</param>
        /// <param name="address">I2C address</param>
        /// <returns><see langword="true" /> if DDR4 is found</returns>
        private bool DetectDdr4(Device device, UInt8 address) {
            lock (device.PortLock) {
                try {
                    return device.IsConnected &&
                           device.ExecuteCommand(new[] { Command.DDR4DETECT, address }) == Response.SUCCESS;
                }
                catch {
                    throw new Exception($"Error detecting DDR4 on {device.PortName}");
                }
            }
        }

        /// <summary>
        /// Detects if DDR5 RAM is present on the device's I2C bus
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="address">I2C address</param>
        /// <returns><see langword="true" /> if DDR5 is found</returns>
        private bool DetectDdr5(Device device, UInt8 address) {
            lock (device.PortLock) {
                try {
                    return device.IsConnected &&
                           device.ExecuteCommand(new[] { Command.DDR5DETECT, address }) == Response.SUCCESS;
                }
                catch {
                    throw new Exception($"Error detecting DDR5 on {device.PortName}");
                }
            }
        }

        /// <summary>
        /// Clears serial port buffers from unneeded data to prevent unwanted behavior and delays
        /// </summary>
        /// <param name="device">Device instance</param>
        private static void ClearBuffer(Device device) {
            lock (device.PortLock) {
                try {
                    if (device.IsConnected) {
                        // Clear response data
                        if (device.PortSettings.RaiseEvent && ResponseData.Count > 0) {
                            ResponseData.Clear();
                        }

                        // Clear receive buffer
                        if (device.BytesToRead > 0) {
                            device._sp.DiscardInBuffer();
                        }

                        // Clear transmit buffer
                        if (device.BytesToWrite > 0) {
                            device._sp.DiscardOutBuffer();
                        }
                    }
                }
                catch {
                    throw new Exception($"Unable to clear {device.PortName} buffer");
                }
            }
        }

        /// <summary>
        /// Clears serial port buffers and causes any buffered data to be written
        /// </summary>
        /// <param name="device">Device instance</param>
        private static void FlushBuffer(Device device) {
            lock (device.PortLock) {
                if (device.IsConnected) {
                    device._sp.BaseStream.Flush();
                }
            }
        }

        /// <summary>
        /// Executes commands on the device.
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="command">Bytes to be sent to the device</param>
        /// <param name="length">Number of bytes to receive in response</param>
        /// <returns>A byte array received from the device in response</returns>
        private byte[] ExecuteCommand(Device device, byte[] command, uint length) {
            if (command.Length == 0) {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(command));
            }

            Queue<byte> _response = new Queue<byte>();

            lock (device.PortLock) {
                if (device.IsConnected) {
                    // Clear input and output buffers
                    device.ClearBuffer();

                    // Send the command to device
                    device._sp.Write(command, 0, command.Length);

                    // Flush the buffer
                    device.FlushBuffer();

                    // Check response length
                    if (length == 0) {
                        return new byte[0];
                    }

                    // Timeout monitoring start
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    while (device.PortSettings.ResponseTimeout * 1000 > sw.ElapsedMilliseconds) {
                        // Check connection
                        if (!device.IsConnected) {
                            throw new IOException($"{device.PortName} not connected");
                        }

                        // Wait for data
                        if (device.PortSettings.RaiseEvent) {
                            if (ResponseData != null && ResponseData.Count >= length && !DataReceiving) {
                                _response = ResponseData;
                                break;
                            }
                        }
                        else {
                            if (device.BytesToRead >= length) {
                                while (device.BytesToRead != 0 && _response.Count < length) {
                                    _response.Enqueue(device.ReadByte());
                                }
                                break;
                            }
                        }

                        if (command[0] == Command.READBYTE ||
                            command[0] == Command.WRITEBYTE) {
                            Wait(1);
                        }
                        else {
                            Wait();
                        }
                    }

                    return _response.ToArray();
                }
            }

            throw new IOException($"{device.PortName} failed to execute command {command}");
        }

        /// <summary>
        /// Delays execution by 10ms
        /// </summary>
        private static void Wait() {
            Wait(10);
        }

        /// <summary>
        /// Delays execution 
        /// </summary>
        /// <param name="timeout">Timeout in milliseconds</param>
        private static void Wait(int timeout) {
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
