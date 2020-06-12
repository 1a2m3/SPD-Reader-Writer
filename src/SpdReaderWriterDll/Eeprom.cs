using System;

namespace SpdReaderWriterDll {

	/// <summary>
	/// Defines RAM Type 
	/// </summary>
	public enum RamType {
		SDR  = 4,
		DDR  = 7,
		DDR2 = 8,
		DDR3 = 11,
		DDR4 = 12,
	}

	/// <summary>
	/// Defines SPD sizes
	/// </summary>
	public enum SpdSize {
		/// <summary>
		/// DDR RAM SPD Total Size
		/// </summary>
		DDR_SPD_SIZE = 0x80, // 128 bytes
		/// <summary>
		/// DDR2 RAM SPD Total Size
		/// </summary>
		DDR2_SPD_SIZE = 0x100, // 256 bytes
		/// <summary>
		/// DDR3 RAM SPD Total Size
		/// </summary>
		DDR3_SPD_SIZE = 0x100, // 256 bytes
		/// <summary>
		/// DDR4 RAM SPD Total Size
		/// </summary>
		DDR4_SPD_SIZE = 0x200, // 512 bytes
	}

	/// <summary>
	/// Defines EEPROM class, properties, and methods to handle EEPROM operations
	/// </summary>
	public class Eeprom {

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
		/// Gets RAM type from SPD data
		/// </summary>
		/// <param name="device">Device instance</param>
		/// <returns>RAM Type</returns>
		public static RamType GetRamType(Device device) {
			// Byte at offset 0x02 in SPD indicates RAM type
			return (RamType) Eeprom.ReadByte(device, 0x02, 1)[0];
		}

		// TODO: Get EEPROM size from SPD data (byte 1)
		public static SpdSize GetEepromSize(Device device) {
			if (GetRamType(device) == RamType.DDR3) {
				//
			}
			else {

			}
			return SpdSize.DDR4_SPD_SIZE;
		}

		/// <summary>
		/// Calculates CRC16/XMODEM checksum
		/// </summary>
		/// <param name="input">A byte array to be checked</param>
		/// <returns>A calculated checksum</returns>
		public static ushort Crc16(byte[] input) {

			ushort[] table = new ushort[256];
			ushort crc = 0;

			for (int i = 0; i < table.Length; ++i) {

				ushort temp = 0;
				ushort a = (ushort)(i << 8);

				for (int j = 0; j < 8; ++j) {
					temp = (((temp ^ a) & 0x8000) != 0) ? (ushort)((temp << 1) ^ 0x1021) : (temp <<= 1);
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
