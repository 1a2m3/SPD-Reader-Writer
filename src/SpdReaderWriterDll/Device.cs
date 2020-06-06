using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

namespace SpdReaderWriterDll {
	/// <summary>
	/// Defines Device class, properties, and methods to handle the communication with the device
	/// </summary>
	public class Device {

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
		/// <param name="eepromAddress">EEPROM address on the i2c bus</param>
		public Device(string PortName, int eepromAddress) {
			this.PortName = PortName;
			this.EepromAddress = eepromAddress;
			this.Connect();
		}

		/// <summary>
		/// Initializes the SPD reader/writer device
		/// </summary>
		/// <param name="PortName">Serial port name</param>
		/// <param name="eepromAddress">EEPROM address on the device's i2c bus</param>
		/// <param name="SpdSize">Total EEPROM size</param>
		public Device(string PortName, int eepromAddress, SpdSize spdSize) {
			this.PortName = PortName;
			this.EepromAddress = eepromAddress;
			this.SpdSize = spdSize;
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
		/// <returns></returns>
		public bool Disconnect() {
			return Disconnect(this);
		}

		/// <summary>
		/// Tests if the device responds to a test command
		/// </summary>
		/// <returns><see langword="true" /> if the device responds properly</returns>
		public bool Test() {
			return Test(this);
		}

		/// <summary>
		/// Reads a byte from the device
		/// </summary>
		/// <returns>A single byte value received from the device</returns>
		public int ReadByte() {
			return this.Sp.ReadByte();
		}

		/// <summary>
		/// Scans the device for I2C bus devices
		/// </summary>
		/// <param name="startAddress">First address</param>
		/// <param name="endAddress">Last address</param>
		/// <returns>An array of addresses on the device's I2C bus</returns>
		public int[] Scan(int startAddress = 0x50, int endAddress = 0x57) {
			return Scan(this, startAddress, endAddress);
		}

		/// <summary>
		/// Probes specified EEPROM address
		/// </summary>
		/// <returns><see langword="true" /> if EEPROM is detected at the specified address</returns>
		public bool Probe() {
			if (this.EepromAddress != 0) {
				return Probe(this, this.EepromAddress);
			}
			return false;
		}

		/// <summary>
		/// Probes specified EEPROM address
		/// </summary>
		/// <param name="address">EEPROM address</param>
		/// <returns><see langword="true" /> if EEPROM is detected at the specified address</returns>
		public bool Probe(int address) {
			return Probe(this, address);
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

		public void ExecuteCommand(string Command) {
			this.ClearBuffer();

			foreach (string cmd in Command.Split(' ')) {
				this.Sp.WriteLine(cmd);
			}
		}

		/// <summary>
		/// Gets a response from the device
		/// </summary>
		/// <returns>A byte array the device has sent</returns>
		public byte[] GetResponse() {

			int retryCount = 0;
			int retryLimit = 1000;

			Queue<byte> _response = new Queue<byte>();

			while (this.BytesToRead < 1) {
				retryCount++;
				if (retryCount == retryLimit) {
					throw new Exception("GetResponse timed out");
					//break;
				}
				Wait();
			}

			while (this.BytesToRead != 0) {
				_response.Enqueue((byte)this.ReadByte());

			}

			this.ClearBuffer();

			return _response.ToArray();
		}

		/// <summary>
		/// Gets a single byte from the device's response
		/// </summary>
		/// <param name="offset">A byte offset to get from the device's response</param>
		/// <returns></returns>
		public byte GetResponse(int offset) {
			return GetResponse()[offset]; ;
		}

		/// <summary>
		/// Finds devices connected to computer by sending a test command to every serial port device detected
		/// </summary>
		/// <returns>An array of serial port names the device is connected to</returns>
		public static string[] Find() {
			Stack<string> _result = new Stack<string>();

			foreach (string _portName in SerialPort.GetPortNames()) {
				Device _device = new Device(_portName);
				if (_device.Test()) {
					_result.Push(_portName);
					_device.Disconnect();
				}
			}
			return _result.ToArray();
		}

		/// <summary>
		/// Attempts to establish a connection with the device
		/// </summary>
		/// <param name="device">Device</param>
		/// <returns><see langword="true" /> if the connection is established</returns>
		private static bool Connect(Device device) {

			lock (device.PortLock) {
				if (!device.IsConnected) {
					device.Sp.PortName = device.PortName;
					try {
						device.Sp.Open();
						device.IsConnected = true;
						if (!device.Test()) {
							device.IsConnected = false;
						}
					}
					catch (Exception) {
						device.IsConnected = false;
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
			if (device.IsConnected) {
				device.ClearBuffer();
				device.Sp.Close();
				device.IsConnected = false;
			}

			if (!device.IsConnected) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Tests if the device is able to communicate
		/// </summary>
		/// <param name="device">Device</param>
		/// <returns><see langword="true" /> if the device responds to a test command</returns>
		private static bool Test(Device device) {

			lock (device.PortLock) {
				if (device.IsConnected) {

					char welcomeString = '!';
					int retryCount = 0;
					int retryLimit = 1000; // If the device didn't respond after this many tries, it's probably dead

					while (device.BytesToRead < 1) {

						if (retryCount > retryLimit) {
							return false;
						}

						// This is inside the loop, because it takes a few tries to get the device to start responding if it hasn't been accessed after it's been plugged in.
						device.ExecuteCommand("t");
						Wait(10);
						retryCount++;
					}

					try {
						if (device.GetResponse(0) == welcomeString) {
							return true;
						}
					}
					catch {
						return false;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Scans for EEPROM addresses on the device's I2C bus
		/// </summary>
		/// <param name="device">Device</param>
		/// <param name="startAddress">First address (cannot be lower than 0x50)</param>
		/// <param name="endAddress">Last address (cannot be higher than 0x57)</param>
		/// <returns>An array of EEPROM addresses present on the device's I2C bus</returns>
		private static int[] Scan(Device device, int startAddress = 0x50, int endAddress = 0x57) {

			Queue<int> addresses = new Queue<int>();

			lock (device.PortLock) {
				if (device.IsConnected) {
					device.ExecuteCommand($"s {startAddress} {endAddress}");

					byte[] response = device.GetResponse();

					foreach (int location in response) {
						if (location != 0) {
							// An accessible EEPROM address was found
							addresses.Enqueue(location);
						}
					}
				}
			}

			return addresses.ToArray();
		}

		/// <summary>
		/// Tests if the EEPROM is present on the device's I2C bus
		/// </summary>
		/// <param name="device">Device instance</param>
		/// <param name="address">EEPROM address</param>
		/// <returns><see langword="true" /> if the address is accessible</returns>
		private static bool Probe(Device device, int address) {

			lock (device.PortLock) {

				if (device.IsConnected) {

					device.ExecuteCommand($"p {address}");
					if (device.GetResponse(0) == address) {
						// Valid address confirmed
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Clears serial port buffers from unneeded data to prevent unwanted behavior and delays
		/// </summary>
		/// <param name="device">Device instance</param>
		/// <returns><see langword="true" /> when the buffer is empty</returns>
		private static bool ClearBuffer(Device device) {
			while (device.BytesToRead > 0 || device.BytesToWrite > 0) {
				device.Sp.DiscardInBuffer();
				device.Sp.DiscardOutBuffer();
				device.Sp.BaseStream.Flush();
				Wait();
			}
			return true;
		}

		/// <summary>
		/// Delays execution 
		/// </summary>
		private static void Wait(int timeout = 1) {
			Thread.Sleep(timeout);
			//Thread.SpinWait(timeout);
		}

		/// <summary>
		/// Serial port default connection settings
		/// </summary>
		private readonly SerialPort Sp = new SerialPort {
			BaudRate = 115200,  // BaudRate must match Arduino's serial baud rate
			NewLine = "\r",
			ReadTimeout = 1000,
			WriteTimeout = 1000,
			//Handshake = Handshake.XOnXOff,
			//DtrEnable = true,
			//RtsEnable = true,
			//ReadBufferSize = 32,
			//WriteBufferSize = 32,
			//Parity = Parity.None,
		};

		/// <summary>
		/// Gets or set device's connection state
		/// </summary>
		public bool IsConnected {
			get => this._isConnected;
			set => this._isConnected = value;
		}

		private bool _isConnected = false; // Why does RS think it's not used, when it's right above ^^^

		public string PortName;
		public int EepromAddress;
		public SpdSize SpdSize;

		public object PortLock = _PortLock; // TODO: DELETE
		private static readonly object _PortLock = new object();

		public int BytesToRead {
			get => this.Sp.BytesToRead;
		}

		public int BytesToWrite {
			get => this.Sp.BytesToWrite;
		}
	}
}