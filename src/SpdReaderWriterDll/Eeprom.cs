using System;
using static SpdReaderWriterDll.Command;
using UInt8 = System.Byte;

namespace SpdReaderWriterDll {
    /// <summary>
    /// Defines EEPROM class, properties, and methods to handle EEPROM operations
    /// </summary>
    public class Eeprom {

        /// <summary>
        /// Reads a single byte from the EEPROM
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="offset">Byte offset</param>
        /// <returns>Byte value at <paramref name="offset"/></returns>
        public static byte ReadByte(Device device, UInt16 offset) {
            if (offset > (int)Ram.SpdSize.DDR5) {
                throw new IndexOutOfRangeException($"Invalid offset");
            }

            try {
                //return ReadByte(device, offset, 1)[0];
                return device.ExecuteCommand(new[] { 
                    READBYTE, 
                    device.I2CAddress, 
                    (byte)(offset >> 8),   // MSB
                    (byte)(offset & 0xFF), // LSB
                    (UInt8)1 });
            }
            catch {
                throw new Exception($"Unable to read byte 0x{offset:X4} at {device.PortName}:{device.I2CAddress}");
            }
        }

        /// <summary>
        /// Reads bytes from the EEPROM
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position to start reading from</param>
        /// <param name="count">Total number of bytes to read from <paramref name="offset" /> </param>
        /// <returns>A byte array containing byte values</returns>
        public static byte[] ReadByte(Device device, UInt16 offset, UInt8 count) {
            if (offset > (int)Ram.SpdSize.DDR5) {
                throw new IndexOutOfRangeException($"Invalid offset");
            }
            if (count == 0) {
                throw new Exception($"No bytes to read");
            }

            try {
                return device.ExecuteCommand(new[] {
                    READBYTE, 
                    device.I2CAddress, 
                    (byte)(offset >> 8),   // MSB
                    (byte)(offset & 0xFF), // LSB
                    count
                }, count);
            }
            catch {
                throw new Exception($"Unable to read byte # 0x{offset:X4} at {device.PortName}:{device.I2CAddress}");
            }
        }

        /// <summary>
        /// Write a byte to the EEPROM
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true" /> if <paramref name="value"/> is written to <paramref name="offset"/> </returns>
        public static bool WriteByte(Device device, UInt16 offset, byte value) {
            if (offset > (int)Ram.SpdSize.DDR5) {
                throw new IndexOutOfRangeException($"Invalid offset");
            }
            try {
                return device.ExecuteCommand(new[] {
                    WRITEBYTE, 
                    device.I2CAddress, 
                    (byte)(offset >> 8),   // MSB
                    (byte)(offset & 0xFF), // LSB
                    value
                }) == Response.SUCCESS;
            }
            catch {
                throw new Exception($"Unable to write \"0x{value:X2}\" to # 0x{offset:X4} at {device.PortName}:{device.I2CAddress}");
            }
        }

        /// <summary>
        /// Write a page to the EEPROM
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Page contents</param>
        /// <returns><see langword="true" /> if <paramref name="value"/> is written to <paramref name="offset"/> </returns>
        public static bool WriteByte(Device device, UInt16 offset, byte[] value) {
            if (offset > (int)Ram.SpdSize.DDR5) {
                throw new IndexOutOfRangeException($"Invalid offset");
            }
            if (value.Length > 16 || value.Length == 0) {
                throw new Exception($"Invalid page size ({value.Length})");
            }

            // Prepare command + data
            byte[] command = new byte[5 + value.Length];
            command[0] = WRITEPAGE;
            command[1] = device.I2CAddress;
            command[2] = (byte)(offset >> 8);   // MSB
            command[3] = (byte)(offset & 0xFF); // LSB
            command[4] = (byte)value.Length;

            Array.Copy(value, 0, command, 5, value.Length);

            try {
                return device.ExecuteCommand(command) == Response.SUCCESS;
            }
            catch {
                throw new Exception($"Unable to write page of {value.Length} byte(s) to # 0x{offset:X4} at {device.PortName}:{device.I2CAddress}");
            }
        }

        /// <summary>
        /// Write a byte to the EEPROM. The byte is written only if it's value differs from the one already saved at the same address.
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true" /> if byte read at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool UpdateByte(Device device, UInt16 offset, byte value) {
            try {
                return VerifyByte(device, offset, value) ||
                       WriteByte(device, offset, value);
            }
            catch {
                throw new Exception($"Unable to update byte # 0x{offset:X4} with \"0x{value:X2}\" at {device.PortName}:{device.I2CAddress}");
            }
        }

        /// <summary>
        /// Write a page to the EEPROM. The page is written only if it's value differs from the one already saved at the same address.
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Page contents</param>
        /// <returns><see langword="true" /> if page read at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool UpdateByte(Device device, UInt16 offset, byte[] value) {
            if (offset > (int)Ram.SpdSize.DDR5) {
                throw new IndexOutOfRangeException($"Invalid offset");
            }
            if (value.Length > 16 || value.Length == 0) {
                throw new Exception($"Invalid page size ({value.Length})");
            }

            try {
                return VerifyByte(device, offset, value) ||
                       WriteByte(device, offset, value);
            }
            catch {
                throw new Exception($"Unable to update page at # 0x{offset:X4} with \"0x{value:X2}\" at {device.PortName}:{device.I2CAddress}");
            }
        }

        /// <summary>
        /// Verifies if the offset content matches the input specified
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true" /> if byte at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool VerifyByte(Device device, UInt16 offset, byte value) {
            try {
                return ReadByte(device, offset) == value;
            }
            catch {
                throw new Exception($"Unable to verify byte # 0x{offset:X4} at {device.PortName}:{device.I2CAddress}");
            }
        }

        /// <summary>
        /// Verifies if the offset content matches the input specified
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte array</param>
        /// <returns><see langword="true" /> if bytes at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool VerifyByte(Device device, UInt16 offset, byte[] value) {
            try {
                byte[] source = ReadByte(device, offset, (UInt8)value.Length);

                for (int i = 0; i < source.Length; i++) {
                    if (source[i] != value[i]) {
                        return false;
                    }
                }

                return true;
            }
            catch {
                throw new Exception($"Unable to verify bytes # 0x{offset:X4}-0x{(offset + value.Length):X4} at {device.PortName}:{device.I2CAddress}");
            }
        }

        /// <summary>
        /// Enables software write protection on the specified EEPROM block 
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="block">Block number to be write protected</param>
        /// <returns><see langword="true" /> when the write protection has been enabled on block <paramref name="block"/> </returns>
        public static bool SetWriteProtection(Device device, UInt8 block) {
            try {
                return device.ExecuteCommand(new[] { RSWP, block, ON }) == Response.SUCCESS;
            }
            catch {
                throw new Exception($"Unable to set RSWP on {device.PortName}");
            }
        }

        /// <summary>
        /// Enables software write protection on all 4 EEPROM blocks
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <returns><see langword="true" /> when the write protection has been enabled on all available blocks</returns>
        public static bool SetWriteProtection(Device device) {
            try {
                for (UInt8 i = 0; i <= 3; i++) {
                    if (!SetWriteProtection(device, i)) {
                        return false;
                    }
                }

                return true;
            }
            catch {
                throw new Exception($"Unable to set RSWP on {device.PortName}");
            }
        }

        /// <summary>
        /// Read software write protection status
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <returns><see langword="false" /> when the EEPROM is writable, or <see langword="true" /> if the at least one block is write protected or if RSWP is not supported</returns>
        public static bool GetReversibleWriteProtection(Device device) {
            try {
                for (UInt8 i = 0; i <= 3; i++) {
                    if (device.ExecuteCommand(new[] { RSWP, i, GET }) == Response.NACK) {
                        return true;
                    }
                }

                return false;
            }
            catch {
                throw new Exception($"Unable to get RSWP status on {device.PortName}");
            }
        }

        /// <summary>
        /// Read software write protection status
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="block">Block number to be checked</param>
        /// <returns><see langword="false" /> when the block is writable, or <see langword="true" /> if the block is write protected or if RSWP is not supported</returns>
        public static bool GetReversibleWriteProtection(Device device, UInt8 block) {
            try {
                return device.ExecuteCommand(new[] { RSWP, block, GET }) == Response.NACK;
            }
            catch {
                throw new Exception($"Unable to get block {block} RSWP status on {device.PortName}");
            }
        }

        /// <summary>
        /// Clears EEPROM write protection 
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> if the write protection has been disabled</returns>
        public static bool ClearReversibleWriteProtection(Device device) {
            try {
                return device.ExecuteCommand(new[] { RSWP, DNC, OFF }) == Response.SUCCESS;
            }
            catch {
                throw new Exception($"Unable to clear RSWP on {device.PortName}");
            }
        }

        /// <summary>
        /// Sets permanent write protection on supported EEPROMs
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> when the permanent write protection is enabled</returns>
        public static bool SetPermanentWriteProtection(Device device) {
            try {
                return device.ExecuteCommand(new[] { PSWP, device.I2CAddress, ON }) == Response.SUCCESS;
            }
            catch {
                throw new Exception($"Unable to set PSWP on {device.PortName}");
            }
        }

        /// <summary>
        /// Tests if EEPROM is writable or permanently protected
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> if when PSWP has NOT been set and EEPROM is fully writable or <see langword="false" /> when PSWP is enabled</returns>
        public static bool GetPermanentWriteProtection(Device device) {
            try {
                return device.ExecuteCommand(new[] { PSWP, device.I2CAddress, GET }) == Response.ACK;
            }
            catch {
                throw new Exception($"Unable to get PSWP status on {device.PortName}");
            }
        }
    }
}
