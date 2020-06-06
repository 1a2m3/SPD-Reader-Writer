using System;

namespace SpdReaderWriter {
	/// <summary>
	/// Defines EEPROM class, properties, and methods to handle EEPROM operations
	/// </summary>
	internal class Eeprom {

		/// <summary>
		/// Defines SPD sizes
		/// </summary>
		public const int DDR3_SPD_SIZE = 0x100; // 256 bytes
		public const int DDR4_SPD_SIZE = 0x200; // 512 bytes

		/// <summary>
		/// Reads a single byte from the EEPROM
		/// </summary>
		/// <param name="device">SPD reader/writer device instance</param>
		/// <param name="offset">Byte offset</param>
		/// <returns>Byte value at <paramref name="offset"/> </returns>
		public static byte ReadByte(Device device, int offset) {

			lock (device.PortLock) {
				device.ExecuteCommand($"r {device.EepromAddress} {offset}");
				return device.GetResponse(0);
			}
		}

		/// <summary>
		/// Reads bytes from the EEPROM
		/// </summary>
		/// <param name="device">SPD reader/writer device instance</param>
		/// <param name="offset">Byte position to start reading from</param>
		/// <param name="count">Total number of bytes to read from <paramref name="offset" /> </param>
		/// <returns>A byte array containing byte values</returns>
		public static byte[] ReadByte(Device device, int offset, int count = 1) {
			
			byte[] output = new byte[count];

			for (int i = 0; i < count; i++) {
				output[i] = ReadByte(device, i + offset);
			}

			return output;
		}

		/// <summary>
		/// Write a byte to the EEPROM
		/// </summary>
		/// <param name="device">SPD reader/writer device instance</param>
		/// <param name="offset">Byte position</param>
		/// <param name="value">Byte value</param>
		/// <returns><see langword="true" /> if <paramref name="value"/> is written at <paramref name="offset"/> </returns>
		public static bool WriteByte(Device device, int offset, byte value) {

			lock (device.PortLock) {
				device.ExecuteCommand($"w {device.EepromAddress} {offset} {value}");

				if (device.GetResponse(0) == 0) { // The device responds with 0 upon successful write
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Write a byte to the EEPROM. The value is written only if differs from the one already saved at the same address.
		/// </summary>
		/// <param name="device">SPD reader/writer device instance</param>
		/// <param name="offset">Byte position</param>
		/// <param name="value">Byte value</param>
		/// <returns><see langword="true" /> if byte read at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
		public static bool UpdateByte(Device device, int offset, byte value) {

			return VerifyByte(device, offset, value) || WriteByte(device, offset, value);
		}

		/// <summary>
		/// Verifies if the offset content matches the input specified
		/// </summary>
		/// <param name="device">SPD reader/writer device instance</param>
		/// <param name="offset">Byte position</param>
		/// <param name="value">Byte value</param>
		/// <returns><see langword="true" /> if byte at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
		public static bool VerifyByte(Device device, int offset, byte value) {

			return ReadByte(device, offset) == value;
		}

		/// <summary>
		/// Enables software write protection on the specified EEPROM block 
		/// </summary>
		/// <param name="device">SPD reader/writer device instance</param>
		/// <param name="block">Block number to be write protected</param>
		/// <returns><see langword="true" /> when the write protection has been enabled</returns>
		public static bool SetWriteProtection(Device device, int block) {

			lock (device.PortLock) {
				device.ExecuteCommand($"e {block}"); // WP commands don't use address, all devices on the bus will respond simultaneously
				if (device.GetResponse(0) == 0) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Enables software write protection on all 4 EEPROM blocks
		/// </summary>
		/// <param name="device">Device instance</param>
		/// <returns><see langword="true" /> when the write protection has been enabled on all blocks</returns>
		public static bool SetWriteProtection(Device device) {

			for (int i = 0; i <= 3; i++) {
				if (!SetWriteProtection(device, i)) {
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Clears EEPROM write protection 
		/// </summary>
		/// <param name="device">Device instance</param>
		/// <returns><see langword="true" /> if the write protection has been disabled</returns>
		public static bool ClearWriteProtection(Device device) {

			lock (device.PortLock) {
				device.ExecuteCommand("c"); // WP commands don't use address, all devices on the bus will respond simultaneously
				if (device.GetResponse(0) == 0) {

					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Prints bytes in a grid pattern
		/// </summary>
		/// <param name="pos">Byte offset</param>
		/// <param name="b">Byte value</param>
		/// <param name="bpr">Bytes per row</param>
		/// <param name="showOffset">Show or hide offsets on top and at the beginning of each line</param>
		/// <param name="color">Set to true to display colored output, or false to disable colors</param>
		public static void DisplayByte(int pos, byte b, int bpr = 16, bool showOffset = true, bool color = true) {

			ConsoleColor _defaultForeColor = Console.ForegroundColor;	// Text Color
			ConsoleColor _defaultBackColor = Console.BackgroundColor;	// Background Color

			ConsoleColor[] colors = {
				ConsoleColor.DarkGray,
				ConsoleColor.Gray,
				ConsoleColor.Red,
				ConsoleColor.DarkRed,
				ConsoleColor.DarkYellow,
				ConsoleColor.Yellow,
				ConsoleColor.Green,
				ConsoleColor.DarkGreen,
				ConsoleColor.DarkCyan,
				ConsoleColor.Cyan,
				ConsoleColor.Blue,
				ConsoleColor.DarkBlue,
				ConsoleColor.DarkMagenta,
				ConsoleColor.Magenta,
				ConsoleColor.White,
				ConsoleColor.Gray,
			};

			// Print top row (offsets)
			if (pos == 0 && showOffset) {
				Console.Write("     "); // Indentation
				for (int i = 0; i < bpr; i++) {
					Console.Write($"{i:x2} ");
				}
			}

			// Print contents
			if (pos % bpr == 0) {
				Console.Write(Environment.NewLine);
				if (showOffset) {
					// Print row offsets
					Console.Write("{0:x3}: ", pos); 
				}
			}

			// Set colors
			if (color) {
				// Print byte values on black background, so the output looks good in cmd and in powershell
				Console.BackgroundColor = ConsoleColor.Black;
				// Set color 
				Console.ForegroundColor = colors[b >> 4];
			}
			// Print byte value
			Console.Write($"{b:X2}"); 

			// Print blank space between each byte, but not at the end of the line
			if (pos % bpr != bpr - 1) {
				Console.Write(" "); 
			}

			// Reset foreground (text) color
			Console.ForegroundColor = _defaultForeColor;
			// Reset background color
			Console.BackgroundColor = _defaultBackColor;
		}

		/// <summary>
		/// Calculates CRC16/XMODEM checksum
		/// </summary>
		/// <param name="input">A byte array to be checked</param>
		/// <returns>A calculated checksum</returns>
		public static ushort Crc16(byte[] input) {

			ushort[] table = new ushort[256];
			ushort initialValue = 0;
			ushort crc = initialValue;
			for (int i = 0; i < table.Length; ++i) {
				ushort temp = 0;
				ushort a = (ushort)(i << 8);
				for (int j = 0; j < 8; ++j) {
					if (((temp ^ a) & 0x8000) != 0) {
						temp = (ushort)((temp << 1) ^ 0x1021);
					}
					else {
						temp <<= 1;
					}
					a <<= 1;
				}
				table[i] = temp;
			}
			for (int i = 0; i < input.Length; ++i) {
				crc = (ushort)((crc << 8) ^ table[((crc >> 8) ^ (0xff & input[i]))]);
			}

			return crc;
		}
	}
}