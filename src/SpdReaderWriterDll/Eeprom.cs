using System;
using UInt8 = System.Byte;

namespace SpdReaderWriterDll {
    /// <summary>
    /// Defines EEPROM class, properties, and methods to handle EEPROM operations
    /// </summary>
    public class Eeprom {

        #region SMbus

        /// <summary>
        /// Reads a single byte from the EEPROM
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <param name="offset">Byte offset</param>
        /// <returns>Byte value at <paramref name="offset"/></returns>
        public static byte ReadByte(Smbus device, UInt16 offset) {

            if (offset > device.MaxSpdSize) {
                throw new IndexOutOfRangeException($"Invalid offset");
            }

            if (device.MaxSpdSize >= (UInt16)(Spd.GetSpdSize(Ram.Type.DDR4))) {
                AdjustPageAddress(device, offset);
            }
            
            return Smbus.ReadByte(device, device.I2CAddress, offset);
        }
        
        /// <summary>
        /// Reads bytes from the EEPROM
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="count">Total number of bytes to read from <paramref name="offset" /></param>
        /// <returns>A byte array containing byte values</returns>
        public static byte[] ReadByte(Smbus device, UInt16 offset, UInt8 count) {

            if (count == 0) {
                throw new Exception($"No bytes to read");
            }

            byte[] result = new byte[count];

            for (UInt16 i = 0; i < count; i++) {
                result[i] = ReadByte(device, (UInt16)(i + offset));
            }

            return result;
        }

        /// <summary>
        /// Write a byte to the EEPROM
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true" /> if <paramref name="value"/> is written to <paramref name="offset"/> </returns>
        public static bool WriteByte(Smbus device, UInt16 offset, byte value) {

            if (offset > device.MaxSpdSize) {
                throw new IndexOutOfRangeException($"Invalid offset");
            }

            if (device.MaxSpdSize >= (UInt16)(Spd.GetSpdSize(Ram.Type.DDR4))) {
                AdjustPageAddress(device, offset);
            }

            return Smbus.WriteByte(device, device.I2CAddress, offset, value);
        }

        /// <summary>
        /// Write a byte array to the EEPROM
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte array</param>
        /// <returns><see langword="true" /> if <paramref name="value"/> is written to <paramref name="offset"/></returns>
        public static bool WriteByte(Smbus device, UInt16 offset, byte[] value) {

            for (UInt16 i = 0; i < value.Length; i++) {
                if (!WriteByte(device, (UInt16)(i + offset), value[i])) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Write a byte to the EEPROM. The byte is written only if its value differs from the one already saved at the same address.
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true" /> if byte read at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool UpdateByte(Smbus device, UInt16 offset, byte value) {
            return VerifyByte(device, offset, value) || WriteByte(device, offset, value);
        }

        /// <summary>
        /// Write a byte array to the EEPROM. The page is written only if its value differs from the one already saved at the same address.
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte array contents</param>
        /// <returns><see langword="true" /> if bytes read at <paramref name="offset"/> matches <paramref name="value"/> values</returns>
        public static bool UpdateByte(Smbus device, UInt16 offset, byte[] value) {
            return VerifyByte(device, offset, value) || WriteByte(device, offset, value);
        }

        /// <summary>
        /// Verifies if the offset content matches the input specified
        /// </summary>
        /// <param name="device">SMB device instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true" /> if bytes at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool VerifyByte(Smbus device, UInt16 offset, byte value) {
            return ReadByte(device, offset) == value;
        }

        /// <summary>
        /// Verifies if the offset content matches the input specified
        /// </summary>
        /// <param name="device">SMB device instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="value">Byte array</param>
        /// <returns><see langword="true" /> if bytes at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool VerifyByte(Smbus device, UInt16 offset, byte[] value) {

            byte[] source = ReadByte(device, offset, (UInt8)value.Length);

            for (int i = 0; i < source.Length; i++) {
                if (source[i] != value[i]) {
                    return false;
                }
            }

            return true;
        }
        
        /// <summary>
        /// EEPROM local page number
        /// </summary>
        private static byte _eepromPageNumber;
        
        /// <summary>
        /// Reset EEPROM page address
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        public static void ResetPageAddress(Smbus device) {
            SetPageAddress(device, 0);
        }

        /// <summary>
        /// Adjust EEPROM page address to access lower or upper 256 bytes of the entire 512 byte EEPROM array
        /// </summary>
        /// <param name="device">System device instance</param>
        /// <param name="eepromPageNumber">Page number</param>
        private static void SetPageAddress(Smbus device, UInt8 eepromPageNumber) {

            if (device.MaxSpdSize < (UInt16)Ram.SpdSize.DDR4) {
                return;
            }

            if (eepromPageNumber > 1) {
                throw new ArgumentOutOfRangeException(nameof(eepromPageNumber));
            }

            Smbus.WriteByte(device, (byte)((EepromCommand.SPA0 >> 1) + eepromPageNumber));
            _eepromPageNumber = eepromPageNumber;
        }

        /// <summary>
        /// Gets currently selected EEPROM page number
        /// </summary>
        /// <returns>Last set EEPROM page number</returns>
        private static UInt8 GetPageAddress() {
            return _eepromPageNumber;
        }

        /// <summary>
        /// Gets currently selected EEPROM page number from hardware
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <returns>Currently selected EEPROM page number</returns>
        private static UInt8 GetPageAddress(Smbus device) {

            if (device.MaxSpdSize < (UInt16)Ram.SpdSize.DDR4) {
                return 0;
            }

            return (byte)(Smbus.ReadByte(device, EepromCommand.RPA >> 1) ? 0 : 1);
        }

        /// <summary>
        /// Adjust EEPROM page number according to specified offset
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <param name="offset">Byte position</param>
        private static void AdjustPageAddress(Smbus device, UInt16 offset) {
            byte targetPage = (byte)(offset >> 8);

            if (targetPage != GetPageAddress()) {
                SetPageAddress(device, targetPage);
            }
        }

        /// <summary>
        /// Read software write protection status
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <returns><see langword="true" /> if some blocks are write protected or <see langword="false" /> when all blocks are writable</returns>
        public static bool GetRswp(Smbus device) {
            for (UInt8 i = 0; i <= 3; i++) {
                if (GetRswp(device, i)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Read software write protection status
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <param name="block">Block number to be checked</param>
        /// <returns><see langword="true" /> if the block is write protected or <see langword="false" /> when the block is writable</returns>
        public static bool GetRswp(Smbus device, UInt8 block) {

            byte[] rswpCmd = {
                EepromCommand.RPS0,
                EepromCommand.RPS1,
                EepromCommand.RPS2,
                EepromCommand.RPS3,
            };

            block = block > 3 ? (byte)0 : block;

            try {
                return !Smbus.ReadByte(device, (byte)(rswpCmd[block] >> 1));
            }
            catch {
                return true;
            }
        }

        #endregion

        #region SerialDevice

        /// <summary>
        /// Reads a single byte from the EEPROM
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="offset">Byte offset</param>
        /// <returns>Byte value at <paramref name="offset"/></returns>
        public static byte ReadByte(SerialDevice device, UInt16 offset) {
            if (offset > (int)Ram.SpdSize.DDR5) {
                throw new IndexOutOfRangeException($"Invalid offset");
            }

            try {
                return device.ExecuteCommand(new[] {
                    Command.READBYTE, 
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
        public static byte[] ReadByte(SerialDevice device, UInt16 offset, UInt8 count) {
            if (offset > (int)Ram.SpdSize.DDR5) {
                throw new IndexOutOfRangeException($"Invalid offset");
            }
            if (count == 0) {
                throw new Exception($"No bytes to read");
            }

            try {
                return device.ExecuteCommand(new[] {
                    Command.READBYTE, 
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
        public static bool WriteByte(SerialDevice device, UInt16 offset, byte value) {
            if (offset > (int)Ram.SpdSize.DDR5) {
                throw new IndexOutOfRangeException($"Invalid offset");
            }
            try {
                return device.ExecuteCommand(new[] {
                    Command.WRITEBYTE, 
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
        public static bool WriteByte(SerialDevice device, UInt16 offset, byte[] value) {
            if (offset > (int)Ram.SpdSize.DDR5) {
                throw new IndexOutOfRangeException($"Invalid offset");
            }
            if (value.Length > 16 || value.Length == 0) {
                throw new Exception($"Invalid page size ({value.Length})");
            }

            // Prepare command + data
            byte[] command = new byte[5 + value.Length];
            command[0] = Command.WRITEPAGE;
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
        /// Write a byte to the EEPROM. The byte is written only if its value differs from the one already saved at the same address.
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true" /> if byte read at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool UpdateByte(SerialDevice device, UInt16 offset, byte value) {
            try {
                return VerifyByte(device, offset, value) ||
                       WriteByte(device, offset, value);
            }
            catch {
                throw new Exception($"Unable to update byte # 0x{offset:X4} with \"0x{value:X2}\" at {device.PortName}:{device.I2CAddress}");
            }
        }

        /// <summary>
        /// Write a page to the EEPROM. The page is written only if its value differs from the one already saved at the same address.
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Page contents</param>
        /// <returns><see langword="true" /> if page read at <paramref name="offset"/> matches <paramref name="value"/> values</returns>
        public static bool UpdateByte(SerialDevice device, UInt16 offset, byte[] value) {
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
        public static bool VerifyByte(SerialDevice device, UInt16 offset, byte value) {
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
        public static bool VerifyByte(SerialDevice device, UInt16 offset, byte[] value) {
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
        public static bool SetRswp(SerialDevice device, UInt8 block) {
            try {
                return device.ExecuteCommand(new[] { Command.RSWP, block, Command.ON }) == Response.SUCCESS;
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
        public static bool SetRswp(SerialDevice device) {
            try {
                for (UInt8 i = 0; i <= 3; i++) {
                    if (!SetRswp(device, i)) {
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
        /// <returns><see langword="true" /> if the at least one block is write protected (or RSWP is not supported), or <see langword="false" /> when the EEPROM is writable</returns>
        public static bool GetRswp(SerialDevice device) {
            try {
                for (UInt8 i = 0; i <= 3; i++) {
                    if (device.ExecuteCommand(new[] { Command.RSWP, i, Command.GET }) == Response.ENABLED) {
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
        /// <returns><see langword="true" /> if the block is write protected (or RSWP is not supported) or <see langword="false" /> when the block is writable</returns>
        public static bool GetRswp(SerialDevice device, UInt8 block) {
            try {
                return device.ExecuteCommand(new[] { Command.RSWP, block, Command.GET }) == Response.ENABLED;
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
        public static bool ClearRswp(SerialDevice device) {
            try {
                return device.ExecuteCommand(new[] { Command.RSWP, Command.DNC, Command.OFF }) == Response.SUCCESS;
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
        public static bool SetPswp(SerialDevice device) {
            try {
                return device.ExecuteCommand(new[] { Command.PSWP, device.I2CAddress, Command.ON }) == Response.SUCCESS;
            }
            catch {
                throw new Exception($"Unable to set PSWP on {device.PortName}");
            }
        }

        /// <summary>
        /// Tests if EEPROM is writable or permanently protected
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> when PSWP is enabled or <see langword="false" /> if when PSWP has NOT been set and EEPROM is writable</returns>
        public static bool GetPswp(SerialDevice device) {
            try {
                return device.ExecuteCommand(new[] { Command.PSWP, device.I2CAddress, Command.GET }) == Response.ENABLED;
            }
            catch {
                throw new Exception($"Unable to get PSWP status on {device.PortName}");
            }
        }

        #endregion

        /// <summary>
        /// DDR4 EEPROM commands
        /// </summary>
        public struct EepromCommand {
            // DDR4 Page commands
            public const byte SPA0 = 0x6C;
            public const byte SPA1 = 0x6E;
            public const byte RPA  = 0x6D;

            // DDR4 RSWP commands
            public const byte RPS0 = 0x63;
            public const byte RPS1 = 0x69;
            public const byte RPS2 = 0x6B;
            public const byte RPS3 = 0x61;
        }
    }
}
