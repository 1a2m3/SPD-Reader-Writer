using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UInt8 = System.Byte;
using Settings = SpdReaderWriterDll.Properties.Settings;


namespace SpdReaderWriterDll {

    /// <summary>
    /// Device commands
    /// </summary>
    public struct Command {
        /// <summary>
        /// Read byte
        /// </summary>
        public const char READBYTE         = 'r';
        /// <summary>
        /// Write byte
        /// </summary>
        public const char WRITEBYTE        = 'w';
        /// <summary>
        /// Scan i2c bus
        /// </summary>
        public const char SCANBUS          = 's';
        /// <summary>
        /// Probe i2c address
        /// </summary>
        public const char PROBEADDRESS     = 'a';
        /// <summary>
        /// Set EEPROM SA pin state
        /// </summary>
        public const char SETADDRESSPIN    = 'p';
        /// <summary>
        /// Get EEPROM SA pin state
        /// </summary>
        public const char GETADDRESSPIN    = 'q';
        /// <summary>
        /// Set High Voltage state on SA0
        /// </summary>
        public const char SETHVSTATE       = '9';
        /// <summary>
        /// Get High Voltage status on SA0
        /// </summary>
        public const char GETHVSTATE       = 'h';
        /// <summary>
        /// Enable Reversible SWP
        /// </summary>
        public const char SETREVERSIBLESWP = 'b';
        /// <summary>
        /// Read Reversible SWP status
        /// </summary>
        public const char GETREVERSIBLESWP = 'o';
        /// <summary>
        /// Clear Reversible SWP
        /// </summary>
        public const char CLEARSWP         = 'c';
        /// <summary>
        /// Enable Permanent SWP
        /// </summary>
        public const char SETPERMANENTSWP  = 'l';
        /// <summary>
        /// Read Permanent SWP status
        /// </summary>
        public const char GETPSWP          = 'u';
        /// <summary>
        /// Get Firmware version
        /// </summary>
        public const char GETVERSION       = 'v';
        /// <summary>
        /// Device Communication Test
        /// </summary>
        public const char TESTCOMM         = 't';
    }

    /// <summary>
    /// Class describing different responses received from the device
    /// </summary>
    public struct Response {
        /// <summary>
        /// Indicates the operation was executed successfully
        /// </summary>
        public const byte SUCCESS = 0;
        /// <summary>
        /// Indicates the operation has failed
        /// </summary>
        public const byte ERROR   = 1;
        /// <summary>
        /// A response used to indicate an error when normally a numeric non-zero answer is expected if the operation was executed successfully
        /// </summary>
        public const byte NULL    = 0;
        /// <summary>
        /// A response used to describe when SA pin is tied to VCC
        /// </summary>
        public const byte ON      = 1;
        /// <summary>
        /// A response used to describe when SA pin is tied to GND
        /// </summary>
        public const byte OFF     = 0;
        /// <summary>
        /// A response expected from the device after executing Command.TESTCOMM command to identify the correct device
        /// </summary>
        public const char WELCOME = '!';

        // Aliases
        public const byte ACK     = SUCCESS;
        public const byte NOACK   = ERROR;
        public const byte NACK    = ERROR;
        public const byte FAIL    = ERROR;
        public const byte ZERO    = NULL;
    }

    /// <summary>
    /// Class describing digital pin states
    /// </summary>
    public struct PinState {
        /// <summary>
        /// Pin state describing condition when SA pin is tied to <b>power</b>
        /// </summary>
        public const int VCC      = 1;

        /// <summary>
        /// Pin state describing condition when SA pin is tied to <b>ground</b>
        /// </summary>
        public const int GND      = 0;

        // Aliases for VCC
        public const int VDDSPD   = VCC;
        public const int PULLUP   = VCC;
        public const int HIGH     = VCC;
        public const int ON       = VCC;

        // Aliases for GND
        public const int VSSSPD   = GND;
        public const int VSS      = GND;
        public const int LOW      = GND;
        public const int OFF      = GND;
        public const int PUSHDOWN = GND;
        public const int DEFAULT  = GND;
    }

    /// <summary>
    /// Class describing EEPROM 'Select Address' pin names
    /// </summary>
    public struct Pin {
        public const int SA0 = 0;
        public const int SA1 = 1;
        public const int SA2 = 2;
    }

    /// <summary>
    /// Defines Device class, properties, and methods to handle the communication with the device
    /// </summary>
    public class Device {

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
                int BaudRate,
                bool DtrEnable,
                bool RtsEnable,
                int DataBits,
                Handshake Handshake,
                string NewLine,
                Parity Parity,
                StopBits StopBits,
                bool RaiseEvent,
                int ResponseTimeout) {
                    this.BaudRate        = BaudRate;
                    this.DtrEnable       = DtrEnable;
                    this.RtsEnable       = RtsEnable;
                    this.DataBits        = DataBits;
                    this.Handshake       = Handshake;
                    this.NewLine         = NewLine.Replace("\\n", "\n").Replace("\\r", "\r");
                    this.Parity          = Parity;
                    this.StopBits        = StopBits;
                    this.RaiseEvent      = RaiseEvent;
                    this.ResponseTimeout = ResponseTimeout;
            }

            public override string ToString() {
                string _stopBits = (int)StopBits == 3 ? "1.5" : ((int)StopBits).ToString();
                string _parity   = Parity.ToString().Substring(0, 1);
                return $"{BaudRate}-{DataBits}-{_parity}-{_stopBits}";
            }
        }

        /// <summary>
        /// Initializes the SPD reader/writer device
        /// </summary>
        public Device(SerialPortSettings portSettings) {
            this.PortSettings = portSettings;
        }

        /// <summary>
        /// Initializes the SPD reader/writer device
        /// </summary>
        /// <param name="PortName">Serial port name</param>
        public Device(SerialPortSettings portSettings, string PortName) {
            this.PortSettings = portSettings;
            this.PortName     = PortName;
        }

        /// <summary>
        /// Initializes the SPD reader/writer device
        /// </summary>
        /// <param name="PortName" >Serial port name</param>
        /// <param name="eepromAddress">EEPROM address on the device's i2c bus</param>
        public Device(SerialPortSettings portSettings, string PortName, UInt8 eepromAddress) {
            this.PortSettings  = portSettings;
            this.PortName      = PortName;
            this.EepromAddress = eepromAddress;
        }

        /// <summary>
        /// Initializes the SPD reader/writer device
        /// </summary>
        /// <param name="PortName">Serial port name</param>
        /// <param name="eepromAddress">EEPROM address on the device's i2c bus</param>
        /// <param name="spdSize">Total EEPROM size</param>
        public Device(SerialPortSettings portSettings, string PortName, UInt8 eepromAddress, SpdSize spdSize) {
            this.PortSettings  = portSettings;
            this.PortName      = PortName;
            this.EepromAddress = eepromAddress;
            this.SpdSize       = spdSize;
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
        /// <returns><see langword="true" /> once the device is disposed</returns>
        public bool Dispose() {
            return Dispose(this);
        }

        /// <summary>
        /// Tests if the device responds to a test command
        /// </summary>
        /// <returns><see langword="true" /> if the device responds properly</returns>
        public bool Test() {
            return Test(this);
        }

        /// <summary>
        /// Tests if the device is capable of SA pin control
        /// </summary>
        /// <returns><see langword="true" /> if the device supports SA pin control</returns>
        public bool TestAdvancedFeatures() {
            return TestAdvancedFeatures(this);
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
        /// <param name="startAddress">First address</param>
        /// <param name="endAddress">Last address</param>
        /// <returns>An array of addresses on the device's I2C bus</returns>
        public UInt8[] Scan(int startAddress, int endAddress) {
            return Scan(this, startAddress, endAddress);
        }

        /// <summary>
        /// Controls high voltage state on pin SA0
        /// </summary>
        /// <param name="state">High voltage supply state</param>
        public bool SetHighVoltage(int state) {
            return SetHighVoltage(this, state);
        }

        /// <summary>
        /// Gets high voltage state on pin SA0
        /// </summary>
        /// <returns><see langword="true" /> if high voltage is applied to pin SA0</returns>
        public int GetHighVoltageState() {
            return GetHighVoltageState(this);
        }

        /// <summary>
        /// Sets specified Select Address pin to desired state
        /// </summary>
        /// <param name="pin">Pin name</param>
        /// <param name="state">Pin state</param>
        /// <returns><see langword="true" /> if the address has been set</returns>
        public bool SetAddressPin(int pin, int state) {
            return SetAddressPin(this, pin, state);
        }

        /// <summary>
        /// Sets all Select Address pins to desired state
        /// </summary>
        /// <param name="state">Pin state</param>
        /// <returns><see langword="true" /> if pin state has been set</returns>
        public bool SetAddressPin(int state) {
            for (int p = Pin.SA0; p <= Pin.SA2; p++) {
                if (!SetAddressPin(p, state)) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Get Select Address pin state
        /// </summary>
        /// <param name="pin">Pin name</param>
        /// <returns><see langword="true" /> if pin is high, or <see langword="false" /> when pin is low</returns>
        public int GetAddressPin(int pin) {
            return GetAddressPin(this, pin);
        }

        /// <summary>
        /// Resets all Select Address pins to default state
        /// </summary>
        /// <returns><see langword="true" /> when all SA pins are pulled to GND</returns>
        public bool ResetAddressPins() {
            return SetHighVoltage(PinState.OFF) && SetAddressPin(PinState.PUSHDOWN) && GetHighVoltageState() == PinState.OFF;
        }

        /// <summary>
        /// Probes specified EEPROM address
        /// </summary>
        /// <returns><see langword="true" /> if EEPROM is detected at the specified address</returns>
        public bool ProbeAddress() {
            return EepromAddress != 0 && ProbeAddress(this, EepromAddress);
        }

        /// <summary>
        /// Probes specified EEPROM address
        /// </summary>
        /// <param name="address">EEPROM address</param>
        /// <returns><see langword="true" /> if EEPROM is detected at the specified address</returns>
        public bool ProbeAddress(int address) {
            return ProbeAddress(this, address);
        }

        /// <summary>
        /// Clears serial port buffers from unneeded data to prevent unwanted behavior and delays
        /// </summary>
        /// <returns><see langword="true" /> when the buffer is empty</returns>
        public bool ClearBuffer() {
            return ClearBuffer(this);
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
        public byte ExecuteCommand(string command) {
            return ExecuteCommand(command, 1)[0];
        }

        /// <summary>
        /// Executes commands on the device.
        /// </summary>
        /// <param name="command">Space separated commands to be executed on the device</param>
        /// <param name="length">Number of bytes to receive in response</param>
        /// <returns>A byte array received from the device in response</returns>
        public byte[] ExecuteCommand(string command, int length) {
            return ExecuteCommand(this, command, length);
        }

        /// <summary>
        /// Get device's firmware version 
        /// </summary>
        /// <returns>Version number</returns>
        public int GetFirmwareVersion() {
            return GetFirmwareVersion(this);
        }

        /// <summary>
        /// Finds devices connected to computer by sending a test command to every serial port device detected
        /// </summary>
        /// <returns>An array of serial port names which have a device or devices connected to</returns>
        public string[] Find() {
            return Find(this);
        }

        /// <summary>
        /// Serial port instance
        /// </summary>
        private SerialPort _sp = new SerialPort();

        /// <summary>
        /// Describes device's connection state
        /// </summary>
        public bool IsConnected => _sp != null && _sp.IsOpen;

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
        public int Version => GetFirmwareVersion(this);

        /// <summary>
        /// EEPROM address
        /// </summary>
        public UInt8 EepromAddress;

        /// <summary>
        /// EEPROM size
        /// </summary>
        public SpdSize SpdSize;

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
        public int BytesToRead => _sp.BytesToRead;

        /// <summary>
        /// Number of bytes to be sent to the device
        /// </summary>
        public int BytesToWrite => _sp.BytesToWrite;

        /// <summary>
        /// Clear to Send status
        /// </summary>
        public bool ClearToSend  => _sp.CtsHolding;

        /// <summary>
        /// Data Set Ready signal
        /// </summary>
        public bool DataSetReady => _sp.DsrHolding;

        /// <summary>
        /// Carrier Detect line ready state
        /// </summary>
        public bool CarrierDetect => _sp.CDHolding;

        /// <summary>
        /// Indicates whether the device supports RSWP and PSWP capabilities, the value is assigned by TestAdvancedFeatures method
        /// </summary>
        public bool AdvancedFeaturesSupported;

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

            while (__sp != null && __sp.IsOpen && __sp.BytesToRead > 0) {
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
        /// Attempts to establish a connection with the device
        /// </summary>
        /// <param name="device">Device</param>
        /// <returns><see langword="true" /> if the connection is established</returns>
        private static bool Connect(Device device) {
            lock (device.PortLock) {
                if (!device.IsConnected) {
                    try {
                        device._sp = new SerialPort();

                        // Port settings
                        device._sp.PortName  = device.PortName;
                        device._sp.BaudRate  = device.PortSettings.BaudRate;
                        device._sp.DtrEnable = device.PortSettings.DtrEnable;
                        device._sp.RtsEnable = device.PortSettings.RtsEnable;
                        device._sp.Handshake = device.PortSettings.Handshake;

                        // Data settings
                        device._sp.DataBits  = device.PortSettings.DataBits;
                        device._sp.NewLine   = device.PortSettings.NewLine;
                        device._sp.Parity    = device.PortSettings.Parity;
                        device._sp.StopBits  = device.PortSettings.StopBits;

                        // Event to handle Data Reception
                        if (device.PortSettings.RaiseEvent) {
                            device._sp.DataReceived += DataReceivedHandler;
                        }

                        // Event to handle Errors
                        device._sp.ErrorReceived += ErrorReceivedHandler;

                        device._sp.Open();
                    }
                    catch (Exception ex) {
                        throw new Exception($"Unable to connect ({device.PortName}): {ex.Message}");
                    }
                }
            }
            return device.IsConnected;
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
                        do {
                            device.ResetAddressPins();
                        } while (device.GetAddressPin(Pin.SA0) == PinState.HIGH || device.GetAddressPin(Pin.SA1) == PinState.HIGH);                        
                    }
                    catch (Exception ex) {
                        throw new Exception($"Unable to disconnect ({device.PortName}): {ex.Message}");
                    }  
                    finally {
                        device.Dispose();
                    }
                }
                return !device.IsConnected;
            }
        }

        /// <summary>
        /// Disposes device instance
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword ="true" /> once the device is disposed</returns>
        private static bool Dispose(Device device) {
            lock (device.PortLock) {
                if (device._sp != null && device._sp.IsOpen) {
                    //device.ClearBuffer();
                    device._sp.Close();
                }
                device._sp = null;
            }
            return device._sp == null;
        }

        /// <summary>
        /// Tests if the device is able to communicate
        /// </summary>
        /// <param name="device">Device</param>
        /// <returns><see langword="true" /> if the device responds to a test command</returns>
        private static bool Test(Device device) {
            lock (device.PortLock) {
                return device.IsConnected && device.ExecuteCommand($"{Command.TESTCOMM}") == (byte) Response.WELCOME;
            }
        }

        /// <summary>
        /// Tests if the device supports programmatic address pins configuration and HV control, used to determine device's RSWP and PSWP capabilities
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> if the device supports programmatic address pins configuration and HV control</returns>
        private static bool TestAdvancedFeatures(Device device) {

            lock (device.PortLock) {
                if (device.IsConnected) {
                    // Test default configuration
                    do {
                        device.ResetAddressPins();
                        Wait(10);
                    } while (device.GetAddressPin(Pin.SA0) == PinState.HIGH || device.GetAddressPin(Pin.SA1) == PinState.HIGH);

                    bool _testSA0 = false;
                    bool _testSA1 = false;
                    bool _testVHV = false;

                    // Test SA0 pin
                    _testSA0 = device.SetAddressPin(Pin.SA0, PinState.HIGH) && device.GetAddressPin(Pin.SA0) == PinState.HIGH && device.ProbeAddress(81);

                    if (_testSA0) {
                        // Test SA1 pin
                        _testSA1 = device.SetAddressPin(Pin.SA1, PinState.HIGH) && device.GetAddressPin(Pin.SA1) == PinState.HIGH && device.ProbeAddress(83);

                        // Reset SA pins
                        do {
                            device.ResetAddressPins();
                            Wait(10);
                        } while (device.GetAddressPin(Pin.SA0) == PinState.HIGH || device.GetAddressPin(Pin.SA1) == PinState.HIGH);

                        if (_testSA1) {
                            // Test HV pin      
                            _testVHV = device.SetHighVoltage(PinState.ON) && device.GetHighVoltageState() == PinState.ON;

                            if (_testVHV) {
                                while (device.GetHighVoltageState() == PinState.ON) {
                                    device.SetHighVoltage(PinState.OFF);
                                    Wait(10);
                                }
                            }
                        }
                    }

                    do {
                        device.ResetAddressPins();
                    } while (device.GetAddressPin(Pin.SA0) == PinState.HIGH || device.GetAddressPin(Pin.SA1) == PinState.HIGH);

                    device.AdvancedFeaturesSupported = _testSA0 && _testSA1 && _testVHV;
                }
            }

            return device.AdvancedFeaturesSupported;
        }

        /// <summary>
        /// Scans for EEPROM addresses on the device's I2C bus
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="startAddress">First address</param>
        /// <param name="endAddress">Last address</param>
        /// <returns>An array of EEPROM addresses present on the device's I2C bus</returns>
        private static UInt8[] Scan(Device device, int startAddress = 0x50, int endAddress = 0x57) {

            Queue<UInt8> addresses = new Queue<UInt8>();

            lock (device.PortLock) {
                if (device.IsConnected) {

                    foreach (byte _address in device.ExecuteCommand($"{Command.SCANBUS} {startAddress} {endAddress}", endAddress - startAddress + 1)) {
                        if (_address != 0) {
                            addresses.Enqueue(_address);
                        }
                    }
                }
            }

            return addresses.ToArray();
        }

        /// <summary>
        /// Sets desired EEPROM address on the devices I2C bus
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="pin">SA pin number</param>
        /// <param name="state">SA pin state</param>
        /// <returns><see langword="true" /> if the address has been set</returns>
        private static bool SetAddressPin(Device device, int pin, int state) {
            lock (device.PortLock) {
                return device.ExecuteCommand($"{Command.SETADDRESSPIN} {pin} {state}") == Response.SUCCESS;
            }
        }

        /// <summary>
        /// Get Select Address pin state
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="pin">Pin name</param>
        /// <returns><see langword="true" /> if pin is high, or <see langword="false" /> when pin is low</returns>
        private static int GetAddressPin(Device device, int pin) {
            lock (device.PortLock) {
                return device.ExecuteCommand($"{Command.GETADDRESSPIN} {pin}") == Response.ON ? PinState.HIGH : PinState.LOW;
            }
        }

        /// <summary>
        /// Sets high voltage on or off on pin SA0
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="state">High voltage supply state</param>
        /// <returns><see langword="true" /> if operation is completed</returns>
        private static bool SetHighVoltage(Device device, int state) {
            lock (device.PortLock) {
                return device.ExecuteCommand($"{Command.SETHVSTATE} {state}") == Response.SUCCESS;
            }
        }

        /// <summary>
        /// Gets high voltage state on pin SA0
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> if high voltage is applied to pin SA0</returns>
        private static int GetHighVoltageState(Device device) {
            lock (device.PortLock) {
                return device.ExecuteCommand($"{Command.GETHVSTATE}") == Response.ON ? PinState.ON : PinState.OFF;
            }
        }

        /// <summary>
        /// Tests if the EEPROM is present on the device's I2C bus
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="address">EEPROM address</param>
        /// <returns><see langword="true" /> if the address is accessible</returns>
        private static bool ProbeAddress(Device device, int address) {
            lock (device.PortLock) {
                return device.ExecuteCommand($"{Command.PROBEADDRESS} {address}") == address;
            }
        }

        /// <summary>
        /// Get Device's firmware version 
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns>Version number</returns>
        private int GetFirmwareVersion(Device device) {
            int _version = 0;
            lock (device.PortLock) {
                if (device.IsConnected) {
                    _version = Int32.Parse(
                        System.Text.Encoding.Default.GetString(
                            device.ExecuteCommand($"{Command.GETVERSION}", Settings.Default.MINVERSION.ToString().Length)
                        )
                    );
                }
            }
            return _version;
        }

        /// <summary>
        /// Finds devices connected to computer by sending a test command to every serial port device detected
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns>An array of serial port names which have a device or devices connected to</returns>
        public string[] Find(Device device) {
            Stack<string> _result = new Stack<string>();

            lock (FindLock) {

                string[] _ports = SerialPort.GetPortNames().Distinct().ToArray();

                foreach (string _portName in _ports) {
                    Device _device = new Device(PortSettings, _portName);

                    lock (_device.PortLock) {
                        try {
                            if (_device.Connect()) {
                                _result.Push(_portName);
                                _device.Dispose();
                            }
                        }
                        catch {
                            // Do nothing
                        }
                    }
                }
            }

            return _result.ToArray();
        }

        /// <summary>
        /// Clears serial port buffers from unneeded data to prevent unwanted behavior and delays
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> when the buffer is empty</returns>
        private static bool ClearBuffer(Device device) {
            lock (device.PortLock) {
                if (device._sp != null && device._sp.IsOpen) {
                    if (device.PortSettings.RaiseEvent && ResponseData.Count > 0) {
                        ResponseData.Clear();
                    }
                    while (device.BytesToRead > 0 || device.BytesToWrite > 0) {
                        device._sp.DiscardInBuffer();
                        device._sp.DiscardOutBuffer();
                        Wait(10);
                    }
                }
            }
            return ResponseData.Count == 0 && device.BytesToRead == 0 && device.BytesToWrite == 0;
        }

        /// <summary>
        /// Clears serial port buffers and causes any buffered data to be written
        /// </summary>
        /// <param name="device">Device instance</param>
        private static void FlushBuffer(Device device) {
            lock (device.PortLock) {
                if (device._sp != null && device._sp.IsOpen) {
                    device._sp.BaseStream.Flush();
                }
            }
        }

        /// <summary>
        /// Executes commands on the device.
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="Command">Space separated commands to be executed on the device</param>
        /// <param name="Length">Number of bytes to receive in response</param>
        /// <returns>A byte array received from the device in response</returns>
        private byte[] ExecuteCommand(Device device, string Command, int Length) {
            
            if (Length < 0) {
                throw new ArgumentOutOfRangeException(nameof(Length));
            }

            if (string.IsNullOrWhiteSpace(Command)) {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(Command));
            }

            Queue<byte> _response = new Queue<byte>();

            lock (device.PortLock) {
                if (device.IsConnected) {

                    device.ClearBuffer();

                    foreach (string cmd in Command.Split(' ')) {
                        device._sp.WriteLine(cmd);
                    }

                    device.FlushBuffer();

                    if (Length == 0) {
                        return new byte[0];
                    }

                    // Timeout monitoring start
                    DateTime _start = DateTime.Now;

                    while (device.PortSettings.ResponseTimeout * 1000 > (DateTime.Now - _start).TotalMilliseconds) {

                        // Check connection
                        if (!device.IsConnected) {
                            throw new IOException($"Device {device.PortName} not responding");
                        }

                        // Wait for data
                        if (device.PortSettings.RaiseEvent) {
                            if (ResponseData != null && ResponseData.Count == Length && !DataReceiving) {
                                _response = ResponseData;
                                break;
                            }
                        }
                        else {
                            if (device.BytesToRead == Length) {
                                while (device.BytesToRead != 0) {
                                    _response.Enqueue(device.ReadByte());
                                }
                                break;
                            }
                        }
                    }

                    byte[] _responseArray = _response.ToArray();

                    if (device.PortSettings.RaiseEvent) { }
                    device.ClearBuffer();

                    return _responseArray;
                }
            }

            throw new Exception("No data");
        }

        /// <summary>
        /// Delays execution 
        /// </summary>
        /// <param name="timeout">Timeout time in milliseconds</param>
        private static void Wait(int timeout = 100) {
            Thread.Sleep(timeout);
            //Task.Delay(timeout);
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
