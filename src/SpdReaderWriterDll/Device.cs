using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using UInt8 = System.Byte;

namespace SpdReaderWriterDll {

    /// <summary>
    /// Device commands
    /// </summary>
    public struct Command {
        /// <summary>
        /// Read byte
        /// </summary>
        public const char READBYTE      = 'r';
        /// <summary>
        /// Write byte
        /// </summary>
        public const char WRITEBYTE     = 'w';
        /// <summary>
        /// Scan i2c bus
        /// </summary>
        public const char SCANBUS       = 's';
        /// <summary>
        /// Probe i2c address
        /// </summary>
        public const char PROBEADDRESS  = 'a';
        /// <summary>
        /// Set EEPROM SA pin state
        /// </summary>
        public const char SETADDRESSPIN = 'p';
        /// <summary>
        /// Get EEPROM SA pin state
        /// </summary>
        public const char GETADDRESSPIN = 'q';
        /// <summary>
        /// Set High Voltage state on SA0
        /// </summary>
        public const char SETHVSTATE    = '9';
        /// <summary>
        /// Get High Voltage status on SA0
        /// </summary>
        public const char GETHVSTATE    = 'h';
        /// <summary>
        /// Enable Reversible SWP
        /// </summary>
        public const char SETRSWP       = 'b';
        /// <summary>
        /// Read Reversible SWP status
        /// </summary>
        public const char GETRSWP       = 'o';
        /// <summary>
        /// Clear Reversible SWP
        /// </summary>
        public const char CLEARSWP      = 'c';
        /// <summary>
        /// Enable Permanent SWP
        /// </summary>
        public const char SETPSWP       = 'l';
        /// <summary>
        /// Read Permanent SWP status
        /// </summary>
        public const char GETPSWP       = 'u';
        /// <summary>
        /// Get Firmware version
        /// </summary>
        public const char GETVERSION    = 'v';
        /// <summary>
        /// Device Communication Test
        /// </summary>
        public const char TESTCOMM      = 't';
        /// <summary>
        /// Device get identification
        /// </summary>
        public const char GETNAME       = 'i';
        /// <summary>
        /// Device assign name
        /// </summary>
        public const char SETNAME       = 'n';
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
        /// <summary>
        /// Bitmask value indicating SA0 control is OK
        /// </summary>
        public const byte SA0_TEST_OK = 0b0001;
        /// <summary>
        /// Bitmask value indicating SA0 control is N/A
        /// </summary>
        public const byte SA0_TEST_NA = SA0_TEST_OK << 4;
        /// <summary>
        /// Bitmask value indicating SA1 control is OK
        /// </summary>
        public const byte SA1_TEST_OK = 0b0010;
        /// <summary>
        /// Bitmask value indicating SA1 control is N/A
        /// </summary>
        public const byte SA1_TEST_NA = SA1_TEST_OK << 4;
        /// <summary>
        /// Bitmask value indicating SA2 control is OK
        /// </summary>
        public const byte SA2_TEST_OK = 0b0100;
        /// <summary>
        /// Bitmask value indicating SA2 control is N/A
        /// </summary>
        public const byte SA2_TEST_NA = SA2_TEST_OK << 4;
        /// <summary>
        /// Bitmask value indicating VHV control is OK
        /// </summary>
        public const byte VHV_TEST_OK = 0b1000;
        /// <summary>
        /// Bitmask value indicating VHV control is N/A
        /// </summary>
        public const byte VHV_TEST_NA = VHV_TEST_OK << 4;

        /// <summary>
        /// Bitmask value indicating minimum required RSWP/PSWP pin controls are OK
        /// </summary>
        public const byte REQ_TEST = SA1_TEST_OK | VHV_TEST_OK;

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
                int baudRate        = 115200,
                bool dtrEnable      = true,
                bool rtsEnable      = true,
                int dataBits        = 8,
                Handshake handshake = Handshake.None,
                string newLine      = "\n",
                Parity parity       = Parity.None,
                StopBits stopBits   = StopBits.One,
                bool raiseEvent     = false,
                int responseTimeout = 10) {
                        BaudRate        = baudRate;
                        DtrEnable       = dtrEnable;
                        RtsEnable       = rtsEnable;
                        DataBits        = dataBits;
                        Handshake       = handshake;
                        NewLine         = newLine.Replace("\\n", "\n").Replace("\\r", "\r");
                        Parity          = parity;
                        StopBits        = stopBits;
                        RaiseEvent      = raiseEvent;
                        ResponseTimeout = responseTimeout;
            }

            public override string ToString() {
                string _stopBits = (int)StopBits == 3 ? "1.5" : ((int)StopBits).ToString();
                string _parity   = Parity.ToString().Substring(0, 1);

                return $"{BaudRate}-{_parity}-{DataBits}-{_stopBits}";
            }
        }

        /// <summary>
        /// Initializes the SPD reader/writer device
        /// </summary>
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
        public Device(SerialPortSettings portSettings, string portName, UInt8 i2cAddress, SpdSize spdSize) {
            PortSettings = portSettings;
            PortName     = portName;
            I2CAddress   = i2cAddress;
            SpdSize      = spdSize;
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
        /// <returns><see langword="true" /> if the device responds properly</returns>
        public bool Test() {
            return Test(this);
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
        /// <param name="bitmask">Enable bitmask</param>
        /// <returns>A bitmask representing available EEPROM devices on the device's I2C bus. Bit 0 is address 80, bit 1 is address 81, and so on.</returns>
        public UInt8 Scan(bool bitmask) {
            return Scan(this, bitmask);
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
        /// <returns><see langword="true" /> when all SA pins are pushed to GND</returns>
        public bool ResetAddressPins() {
            return SetHighVoltage(PinState.OFF) && 
                   SetAddressPin(PinState.PUSHDOWN) && 
                   GetHighVoltageState() == PinState.OFF;
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
        public bool ProbeAddress(int address) {
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
        public byte ExecuteCommand(string command) {
            return ExecuteCommand(command, 1)[0];
        }

        /// <summary>
        /// Executes commands on the device.
        /// </summary>
        /// <param name="command">Space separated commands to be executed on the device</param>
        /// <param name="length">Number of bytes to receive in response</param>
        /// <returns>A byte array received from the device in response</returns>
        public byte[] ExecuteCommand(string command, uint length) {
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
        /// Assigns a name to the Device
        /// </summary>
        /// <param name="name">Device name</param>
        /// <returns><see langword="true" /> when the device name is set</returns>
        public bool AssignName(string name) {
            return AssignName(this, name);
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
                    return _sp != null && _sp.IsOpen;
                }
                catch {
                    return false;
                }
            }
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
        /// Device's name
        /// </summary>
        public string DeviceName;

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
        /// Attempts to establish a connection with the device
        /// </summary>
        /// <param name="device">Device</param>
        /// <returns><see langword="true" /> if the connection is established</returns>
        private static bool Connect(Device device) {
            lock (device.PortLock) {
                if (!device.IsConnected) {
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

                    try {
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
                        if (device.AdvancedPinControlSupported) {
                            while (!device.ResetAddressPins()) {
                                Wait();
                            }
                        }
                        if (device.PortSettings.RaiseEvent) {
                            device._sp.DataReceived -= DataReceivedHandler;
                        }
                        device._sp.ErrorReceived -= ErrorReceivedHandler;
                        device._sp.Close();
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
        /// <param name="device">Device</param>
        /// <returns><see langword="true" /> if the device responds to a test command</returns>
        private static bool Test(Device device) {
            lock (device.PortLock) {
                try {
                    return device.IsConnected && 
                           device.ExecuteCommand($"{Command.TESTCOMM}") == Response.WELCOME;
                }
                catch {
                    throw new Exception($"Unable to test {device.PortName}");
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

                        bool _testSA0 = false;
                        bool _testSA1 = false;
                        bool _testSA2 = false;
                        bool _testVHV = false;
                        byte _i2cBus  = 0;

                        // Reset SA pins
                        while (!device.ResetAddressPins()) {
                            Wait();
                        }

                        // Test SA0 pin control
                        //_i2cBus = device.Scan(true);
                        //_testSA0 = device.SetAddressPin(Pin.SA0, PinState.ON);
                        //_testSA0 &= device.GetAddressPin(Pin.SA0) == PinState.HIGH;
                        //_testSA0 &= _i2cBus != device.Scan(true);


                        // Test SA1 pin control
                        _i2cBus   = device.Scan(true);
                        _testSA1  = device.SetAddressPin(Pin.SA1, PinState.ON);
                        _testSA1 &= device.GetAddressPin(Pin.SA1) == PinState.HIGH;
                        _testSA1 &= _i2cBus != device.Scan(true);

                        // Test SA2 pin control
                        //_i2cBus = device.Scan(true);
                        //_testSA2 = device.SetAddressPin(Pin.SA2, PinState.ON);
                        //_testSA2 &= device.GetAddressPin(Pin.SA2) == PinState.HIGH;
                        //_testSA2 &= _i2cBus != device.Scan(true);


                        // Test HV pin control
                        _testVHV = device.SetHighVoltage(PinState.ON);
                        _testVHV &= device.GetHighVoltageState() == PinState.ON;

                        // Reset SA pins
                        while (!device.ResetAddressPins()) {
                            Wait();
                        }

                        return (byte)
                            ((_testSA0 ? Response.SA0_TEST_OK : Response.SA0_TEST_NA) |
                             (_testSA1 ? Response.SA1_TEST_OK : Response.SA1_TEST_NA) |
                             (_testSA2 ? Response.SA2_TEST_OK : Response.SA2_TEST_NA) |
                             (_testVHV ? Response.VHV_TEST_OK : Response.VHV_TEST_NA));
                    }
                }
                catch {
                    throw new Exception($"Unable to determine if {device.PortName} supports pin control.");
                }
            }

            return Response.SA0_TEST_NA | Response.SA1_TEST_NA | Response.SA2_TEST_NA | Response.VHV_TEST_NA;
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

                        for (int i = 0; i <= 8; i++) {
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
        /// <param name="bitmask">Enable bitmask</param>
        /// <returns>A bitmask representing available EEPROM devices on the device's I2C bus. Bit 0 is address 80, bit 1 is address 81, and so on.</returns>
        private static UInt8 Scan(Device device, bool bitmask) {
            if (bitmask) {
                lock (device.PortLock) {
                    try {
                        if (device.IsConnected) {
                            return device.ExecuteCommand($"{Command.SCANBUS}");
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
        /// Sets Select Address pin state
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <param name="pin">SA pin number</param>
        /// <param name="state">SA pin state</param>
        /// <returns><see langword="true" /> if the Select Address pin has been set</returns>
        private static bool SetAddressPin(Device device, int pin, int state) {
            lock (device.PortLock) {
                try {
                    return device.IsConnected && 
                           device.ExecuteCommand($"{Command.SETADDRESSPIN} {pin} {state}") == Response.SUCCESS;
                }
                catch {
                    throw new Exception($"Unable to set address pin {pin} state on {device.PortName}");
                }
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
                try {
                    return device.IsConnected && 
                           device.ExecuteCommand($"{Command.GETADDRESSPIN} {pin}") == Response.ON
                        ? PinState.HIGH
                        : PinState.LOW;
                }
                catch {
                    throw new Exception($"Unable to get address pin {pin} state on {device.PortName}");
                }
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
                try {
                    return device.IsConnected && 
                           device.ExecuteCommand($"{Command.SETHVSTATE} {state}") == Response.SUCCESS;
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
        private static int GetHighVoltageState(Device device) {
            lock (device.PortLock) {
                try {
                    return device.IsConnected && 
                           device.ExecuteCommand($"{Command.GETHVSTATE}") == Response.ON 
                        ? PinState.ON 
                        : PinState.OFF;
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
        private static bool ProbeAddress(Device device, int address) {
            lock (device.PortLock) {
                try {
                    return device.IsConnected && 
                           device.ExecuteCommand($"{Command.PROBEADDRESS} {address}") == Response.ACK;
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
                            System.Text.Encoding.Default.GetString(
                                device.ExecuteCommand($"{Command.GETVERSION}", (uint)Settings.MINVERSION.ToString().Length)
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
        private bool AssignName(Device device, string name) {
            if (name == null) throw new ArgumentNullException("Name can't be null");
            if (name == "") throw new ArgumentException("Name can't be blank");

            lock (device.PortLock) {
                try {
                    if (device.IsConnected) {
                        string _name = name.Trim();

                        if (_name == GetName()) {
                            return false;
                        }

                        if (device.ExecuteCommand($"{Command.SETNAME} {_name}.") == Response.SUCCESS) {
                            this.DeviceName = _name;
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
                    if (string.IsNullOrEmpty(DeviceName)) {
                        DeviceName = System.Text.Encoding.Default.GetString(device.ExecuteCommand($"{Command.GETNAME}", 16)).Split('\0')[0];
                    }

                    return DeviceName;
                }
                catch {
                    throw new Exception($"Unable to get {device.PortName} name");
                }
            }
        }

        /// <summary>
        /// Finds devices connected to computer by sending a test command to every serial port device detected
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
        /// <param name="command">Space separated commands to be executed on the device</param>
        /// <param name="length">Number of bytes to receive in response</param>
        /// <returns>A byte array received from the device in response</returns>
        private byte[] ExecuteCommand(Device device, string command, uint length) {
            if (string.IsNullOrWhiteSpace(command)) {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(command));
            }

            Queue<byte> _response = new Queue<byte>();

            lock (device.PortLock) {
                if (device.IsConnected) {
                    // Clear input and output buffers
                    device.ClearBuffer();

                    // Send the command to device
                    device._sp.WriteLine(command);

                    // Flush the buffer
                    device.FlushBuffer();

                    // Check response length
                    if (length == 0) {
                        return new byte[0];
                    }

                    // Timeout monitoring start
                    DateTime _start = DateTime.Now;

                    while (device.PortSettings.ResponseTimeout * 1000 > (DateTime.Now - _start).TotalMilliseconds) {
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

                        if (command.Substring(0, 1) == Command.READBYTE.ToString() ||
                            command.Substring(0, 1) == Command.WRITEBYTE.ToString()) {
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
        /// Delays execution 
        /// </summary>
        /// <param name="timeout">Timeout time in milliseconds</param>
        private static void Wait(int timeout = 10) {
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
