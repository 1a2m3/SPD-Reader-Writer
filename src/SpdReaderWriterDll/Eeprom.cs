using System;
using System.Threading;
using static SpdReaderWriterDll.SerialDeviceCommand;
using static SpdReaderWriterDll.PciDevice;
using UInt8 = System.Byte;

namespace SpdReaderWriterDll {
    /// <summary>
    /// Defines EEPROM class, properties, and methods to handle EEPROM operations
    /// </summary>
    public class Eeprom {

        #region PciDevice

        /// <summary>
        /// Reads a single byte from the EEPROM
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <param name="offset">Byte offset</param>
        /// <returns>Byte value at <paramref name="offset"/></returns>
        public static byte ReadByte(PciDevice device, UInt16 offset) {

            if (offset > device.MaxSpdSize) {
                throw new IndexOutOfRangeException($"Invalid offset");
            }

            //Adjust EEPROM page address
            AdjustPageAddress(device, offset);

            // Prepare and store location information
            device.WriteWord(SMBUS_OFFSET.I2CADDRESS, (UInt16)((device.I2CAddress << 8) | (byte)offset));

            // Execute command
            device.WriteByte(SMBUS_OFFSET.COMMAND, SMBUS_COMMAND.EXEC_CMD);

            // Wait
            while (device.IsBusy()) { }

            // Return output
            return device.ReadByte(SMBUS_OFFSET.OUTPUT);
        }

        /// <summary>
        /// Reads bytes from the EEPROM
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="count">Total number of bytes to read from <paramref name="offset" /></param>
        /// <returns>A byte array containing byte values</returns>
        public static byte[] ReadByte(PciDevice device, UInt16 offset, UInt8 count) {

            if (offset > device.MaxSpdSize) {
                throw new IndexOutOfRangeException($"Invalid offset");
            }

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
        public static bool WriteByte(PciDevice device, UInt16 offset, byte value) {

            if (offset > device.MaxSpdSize) {
                throw new IndexOutOfRangeException($"Invalid offset");
            }

            //Adjust EEPROM page address
            AdjustPageAddress(device, offset);

            // Prepare and store location information
            device.WriteWord(SMBUS_OFFSET.I2CADDRESS, (UInt16)(((device.I2CAddress | SMBUS_COMMAND.WRITE) << 8) | (byte)offset));

            // Store byte value to be written
            device.WriteByte(SMBUS_OFFSET.INPUT, value);

            // Execute command 
            device.WriteByte(SMBUS_OFFSET.COMMAND, SMBUS_COMMAND.EXEC_CMD);

            // Wait
            Thread.Sleep(10);
            while (device.IsBusy()) { }

            // Return result
            return !device.GetError();
        }

        /// <summary>
        /// Write a byte array to the EEPROM
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte array</param>
        /// <returns><see langword="true" /> if <paramref name="value"/> is written to <paramref name="offset"/></returns>
        public static bool WriteByte(PciDevice device, UInt16 offset, byte[] value) {

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
        public static bool UpdateByte(PciDevice device, UInt16 offset, byte value) {
            return VerifyByte(device, offset, value) || WriteByte(device, offset, value);
        }

        /// <summary>
        /// Write a byte array to the EEPROM. The page is written only if its value differs from the one already saved at the same address.
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte array contents</param>
        /// <returns><see langword="true" /> if bytes read at <paramref name="offset"/> matches <paramref name="value"/> values</returns>
        public static bool UpdateByte(PciDevice device, UInt16 offset, byte[] value) {
            return VerifyByte(device, offset, value) || WriteByte(device, offset, value);
        }

        /// <summary>
        /// Verifies if the offset content matches the input specified
        /// </summary>
        /// <param name="device">SMB device instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true" /> if bytes at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool VerifyByte(PciDevice device, UInt16 offset, byte value) {
            return ReadByte(device, offset) == value;
        }

        /// <summary>
        /// Verifies if the offset content matches the input specified
        /// </summary>
        /// <param name="device">SMB device instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="value">Byte array</param>
        /// <returns><see langword="true" /> if bytes at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool VerifyByte(PciDevice device, UInt16 offset, byte[] value) {

            byte[] source = ReadByte(device, offset, (UInt8)value.Length);

            for (int i = 0; i < source.Length; i++) {
                if (source[i] != value[i]) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Reset EEPROM page address
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        public static void ResetPageAddress(PciDevice device) {
            SetPageAddress(device, 0);
        }

        /// <summary>
        /// Adjust EEPROM page address to access lower or upper 256 bytes of the entire 512 byte EEPROM array
        /// </summary>
        /// <param name="device">System device instance</param>
        /// <param name="eepromPageNumber">Page number</param>
        private static void SetPageAddress(PciDevice device, UInt8 eepromPageNumber) {

            if (eepromPageNumber > 1) {
                throw new ArgumentOutOfRangeException(nameof(eepromPageNumber));
            }

            byte cmd = (byte)(((eepromPageNumber == 0 ? EEPROM_COMMAND.SPA0 : EEPROM_COMMAND.SPA1) >> 1) | SMBUS_COMMAND.WRITE);

            device.WriteByte(SMBUS_OFFSET.I2CADDRESS, cmd);
            device.WriteByte(SMBUS_OFFSET.COMMAND, SMBUS_COMMAND.EXEC_CMD);

            while (device.IsBusy()) { }

            device.EepromPageNumber = eepromPageNumber;
        }

        /// <summary>
        /// Gets currently selected EEPROM page number
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <returns>Currently selected EEPROM page number</returns>
        private static UInt8 GetPageAddress(PciDevice device) {

            device.WriteByte(SMBUS_OFFSET.I2CADDRESS, EEPROM_COMMAND.RPA >> 1 | SMBUS_COMMAND.READ);
            device.WriteByte(SMBUS_OFFSET.COMMAND, SMBUS_COMMAND.EXEC_CMD | SMBUS_COMMAND.MOD_NEXT); // command 0x0E works too

            while (device.IsBusy()) { }

            device.EepromPageNumber = (byte)(device.GetError() ? 1 : 0);

            return device.EepromPageNumber;
        }

        /// <summary>
        /// Adjust EEPROM page number according to specified offset
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <param name="offset">Byte position</param>
        private static void AdjustPageAddress(PciDevice device, UInt16 offset) {
            byte targetPage = (byte)(offset >> 8);

            if (targetPage != GetPageAddress(device)) {
                SetPageAddress(device, targetPage);
            }
        }

        /// <summary>
        /// Read software write protection status
        /// </summary>
        /// <param name="device">SMBus device instance</param>
        /// <returns><see langword="true" /> if some blocks are write protected or <see langword="false" /> when all blocks are writable</returns>
        public static bool GetRswp(PciDevice device) {
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
        public static bool GetRswp(PciDevice device, UInt8 block) {

            byte[] eepromBlock = { 
                EEPROM_COMMAND.RPS0, 
                EEPROM_COMMAND.RPS1, 
                EEPROM_COMMAND.RPS2, 
                EEPROM_COMMAND.RPS3,
            };

            block = block > 3 ? (byte)0 : block;

            device.WriteByte(SMBUS_OFFSET.I2CADDRESS, (byte)(eepromBlock[block] >> 1));
            device.WriteByte(SMBUS_OFFSET.COMMAND, SMBUS_COMMAND.EXEC_CMD);

            while (device.IsBusy()) { }

            return device.GetError();
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
        public static byte[] ReadByte(SerialDevice device, UInt16 offset, UInt8 count) {
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
        public static bool WriteByte(SerialDevice device, UInt16 offset, byte value) {
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
        public static bool WriteByte(SerialDevice device, UInt16 offset, byte[] value) {
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
                    if (device.ExecuteCommand(new[] { RSWP, i, GET }) == Response.ENABLED) {
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
                return device.ExecuteCommand(new[] { RSWP, block, GET }) == Response.ENABLED;
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
        public static bool SetPswp(SerialDevice device) {
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
        /// <returns><see langword="true" /> when PSWP is enabled or <see langword="false" /> if when PSWP has NOT been set and EEPROM is writable</returns>
        public static bool GetPswp(SerialDevice device) {
            try {
                return device.ExecuteCommand(new[] { PSWP, device.I2CAddress, GET }) == Response.ENABLED;
            }
            catch {
                throw new Exception($"Unable to get PSWP status on {device.PortName}");
            }
        }

        #endregion
    }
}
