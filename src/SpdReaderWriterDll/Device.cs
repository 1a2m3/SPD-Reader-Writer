using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using UInt8 = System.Byte;

namespace SpdReaderWriterDll {

    /// <summary>
    /// Device commands
    /// </summary>
    public class Command {
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
    public class Response {
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
        public const byte ACK   = SUCCESS;
        public const byte NOACK = ERROR;
        public const byte NACK  = ERROR;
        public const byte FAIL  = ERROR;
        public const byte ZERO  = NULL;
    }

    /// <summary>
    /// Class describing digital pin states
    /// </summary>
    public class PinState {
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
    public class Pin {
        public const int SA0 = 0;
        public const int SA1 = 1;
        public const int SA2 = 2;
    }

    /// <summary>
    /// Defines Device class, properties, and methods to handle the communication with the device
    /// </summary>
    public class Device {

        #region PublicRegion

        /// <summary>
        /// Initializes the SPD reader/writer device
        /// </summary>
        public Device() {
        }

        /// <summary>
        /// Initializes the SPD reader/writer device
        /// </summary>
        /// <param name="PortName">Serial port name</param>
        public Device(string PortName) {
            this.PortName = PortName;
            this.Connect();
        }

        /// <summary>
        /// Initializes the SPD reader/writer device
        /// </summary>
        /// <param name = "PortName" >Serial port name</param>
        /// <param name = "eepromAddress">EEPROM address on the device's i2c bus</param>
        public Device(string PortName, UInt8 eepromAddress) {
            this.PortName      = PortName;
            this.EepromAddress = eepromAddress;
            this.Connect();
        }

        /// <summary>
        /// Initializes the SPD reader/writer device
        /// </summary>
        /// <param name="PortName">Serial port name</param>
        /// <param name="eepromAddress">EEPROM address on the device's i2c bus</param>
        /// <param name="spdSize">Total EEPROM size</param>
        public Device(string PortName, UInt8 eepromAddress, SpdSize spdSize) {
            this.PortName      = PortName;
            this.EepromAddress = eepromAddress;
            this.SpdSize       = spdSize;
            this.Connect();
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
        public UInt8[] Scan(int startAddress = 0x50, int endAddress = 0x57) {
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
        public bool GetHighVoltageState() {
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
        public bool GetAddressPin(int pin) {
            return GetAddressPin(this, pin);
        }

        /// <summary>
        /// Resets all Select Address pins to default state
        /// </summary>
        /// <returns><see langword="true" /> when all SA pins are pulled to GND</returns>
        public bool ResetAddressPins() {
            return SetHighVoltage(PinState.OFF) && SetAddressPin(PinState.PUSHDOWN) && GetHighVoltageState() == Convert.ToBoolean(PinState.OFF);
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
        /// Executes commands on the device.
        /// </summary>
        /// <param name="Command">Space separated commands to be executed on the device</param>
        /// <returns>A byte array received from the device in response</returns>
        public byte[] ExecuteCommand(string Command) {
            return ExecuteCommand(this, Command);
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
        public static string[] Find() {

            Stack<string> _result = new Stack<string>();

            lock (_findLock) {
                foreach (string _portName in SerialPort.GetPortNames()) {
                    Device _device = new Device(_portName);
                    lock (_device.PortLock) {
                        if (_device.Test()) {
                            _result.Push(_portName);
                            _device.Dispose();
                            _device.Disconnect();
                        }
                    }
                }
            }

            return _result.ToArray();
        }

        /// <summary>
        /// Serial port default connection settings
        /// </summary>
        private SerialPort _sp = new SerialPort {
            BaudRate     = Settings.BAUDRATE,
            ReadTimeout  = 10000,
            WriteTimeout = 10000,
            RtsEnable    = true,
            DtrEnable    = true,
            Handshake    = Handshake.None,
            NewLine      = "\n",
            StopBits     = StopBits.One,
            Parity       = Parity.None,
            DataBits     = 8,
        };

        /// <summary>
        /// Describes device's connection state
        /// </summary>
        public bool IsConnected => _sp != null && _sp.IsOpen;

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
        /// Indicates whether the device supports RSWP and PSWP capabilities, the value is assigned by TestAdvancedFeatures method
        /// </summary>
        public bool AdvancedFeaturesSupported;

        #endregion

        #region PrivateRegion

        /// <summary>
        /// Attempts to establish a connection with the device
        /// </summary>
        /// <param name="device">Device</param>
        /// <returns><see langword="true" /> if the connection is established</returns>
        private static bool Connect(Device device) {

            lock (device.PortLock) {
                if (!device.IsConnected) {
                    device._sp.PortName = device.PortName;
                    try {
                        device._sp.Open();
                        if (!device.Test()) {
                            return false;
                        }
                    }
                    catch (Exception) {
                        return false;
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
                            Wait();
                        } while (device.GetAddressPin(Pin.SA0) || device.GetAddressPin(Pin.SA1));
                    }
                    finally {
                        device._sp.Dispose();
                        //device.IsConnected = false;
                    }
                }
                return !device.IsConnected;
            }
        }

        /// <summary>
        /// Disposes device instance
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> once the device is disposed</returns>
        private static bool Dispose(Device device) {
            lock (device.PortLock) {
                if (device._sp != null) {
                    if (device._sp.IsOpen) {
                        device._sp.Dispose();
                    }
                }
                device._sp = null;
            }
            return true;
        }

        /// <summary>
        /// Tests if the device is able to communicate
        /// </summary>
        /// <param name="device">Device</param>
        /// <returns><see langword="true" /> if the device responds to a test command</returns>
        private static bool Test(Device device) {
            lock (device.PortLock) {
                return device.ExecuteCommand($"{Command.TESTCOMM}")[0] == Response.WELCOME;
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
                    } while (device.GetAddressPin(Pin.SA0) || device.GetAddressPin(Pin.SA1));

                    bool _testSA0 = false;
                    bool _testSA1 = false;
                    bool _testVHV = false;

                    // Test SA0 pin
                    device.SetAddressPin(Pin.SA0, PinState.HIGH);
                    _testSA0 = device.GetAddressPin(Pin.SA0) && device.ProbeAddress(81);

                    if (_testSA0) {
                        // Test SA1 pin
                        device.SetAddressPin(Pin.SA1, PinState.HIGH);
                        _testSA1 = device.GetAddressPin(Pin.SA1) && device.ProbeAddress(83);

                        // Reset SA pins
                        do {
                            device.ResetAddressPins();
                        } while (device.GetAddressPin(Pin.SA0) || device.GetAddressPin(Pin.SA1));

                        if (_testSA1) {
                            // Test HV pin      
                            device.SetHighVoltage(PinState.ON);
                            _testVHV = device.GetHighVoltageState();

                            if (_testVHV) {
                                device.SetHighVoltage(PinState.OFF);
                            }
                        }
                    }

                    do {
                        device.ResetAddressPins();
                    } while (device.GetAddressPin(Pin.SA0) || device.GetAddressPin(Pin.SA1));

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
        private static UInt8[] Scan(Device device, int startAddress = 0x50, int endAddress = 0x52) {

            Queue<UInt8> addresses = new Queue<UInt8>();

            lock (device.PortLock) {
                if (device.IsConnected) {

                    // Probe each address
                    for (int i = startAddress; i <= endAddress; i++) {
                        if (device.ProbeAddress(i)) {
                            addresses.Enqueue((byte)i);
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
                return device.ExecuteCommand($"{Command.SETADDRESSPIN} {pin} {state}")[0] == Response.SUCCESS;
            }
        }

        /// <summary>
        /// Get Select Address pin state
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="pin">Pin name</param>
        /// <returns><see langword="true" /> if pin is high, or <see langword="false" /> when pin is low</returns>
        private static bool GetAddressPin(Device device, int pin) {
            lock (device.PortLock) {
                return device.ExecuteCommand($"{Command.GETADDRESSPIN} {pin}")[0] == PinState.ON;
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
                return device.ExecuteCommand($"{Command.SETHVSTATE} {state}")[0] == Response.SUCCESS;
            }
        }

        /// <summary>
        /// Gets high voltage state on pin SA0
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> if high voltage is applied to pin SA0</returns>
        private static bool GetHighVoltageState(Device device) {
            lock (device.PortLock) {
                return device.ExecuteCommand($"{Command.GETHVSTATE}")[0] == Response.ON;
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
                return device.ExecuteCommand($"{Command.PROBEADDRESS} {address}")[0] == address;
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
                            device.ExecuteCommand($"{Command.GETVERSION}")
                            )
                        );
                }
            }
            return _version;
        }

        /// <summary>
        /// Clears serial port buffers from unneeded data to prevent unwanted behavior and delays
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> when the buffer is empty</returns>
        private static bool ClearBuffer(Device device) {
            lock (device.PortLock) {
                if (device._sp != null && device._sp.IsOpen) {
                    while (device.BytesToRead > 0 || device.BytesToWrite > 0) {
                        device._sp.DiscardInBuffer();
                        device._sp.DiscardOutBuffer();
                        Wait(1);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Executes commands on the device.
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="Command">Space separated commands to be executed on the device</param>
        /// <returns>A byte array received from the device in response</returns>
        private static byte[] ExecuteCommand(Device device, string Command) {

            Queue<byte> _response = new Queue<byte>();

            lock (device.PortLock) {
                if (device.IsConnected) {

                    DateTime _start = DateTime.Now;

                    device.ClearBuffer();

                    //foreach (string cmd in Command.Split(' ')) {
                    //    device._sp.WriteLine(cmd);
                    //}
                    device._sp.WriteLine(Command);

                    while (device.BytesToRead == 0) {
                        if ((DateTime.Now - _start).TotalMilliseconds > device._sp.ReadTimeout) {
                            device.Dispose();
                            throw new TimeoutException("Response timeout");
                        }
                        Wait(10);
                    }

                    while (device.BytesToRead != 0) {
                        _response.Enqueue(device.ReadByte());
                    }

                    device.ClearBuffer();
                }
            }

            return _response.ToArray();
        }

        /// <summary>
        /// Delays execution 
        /// </summary>
        /// <param name="timeout">Timeout time in milliseconds</param>
        private static void Wait(int timeout = 100) {
            //Thread.Sleep(timeout);
            Task.Delay(timeout);
        }

        /// <summary>
        /// PortLock object used to prevent other threads from acquiring the lock 
        /// </summary>
        private static readonly object _portLock = new object();

        /// <summary>
        /// FindLock object used to prevent other threads from acquiring the lock
        /// </summary>
        private static readonly object _findLock = new object();

        #endregion
    }
}
