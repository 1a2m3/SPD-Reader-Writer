/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using System;
using System.ComponentModel;

namespace SpdReaderWriterDll {
    /// <summary>
    /// Defines EEPROM class, properties, and methods to handle EEPROM operations
    /// </summary>
    public class Eeprom {

        #region SMBus

        /// <summary>
        /// Reads a single byte from the EEPROM
        /// </summary>
        /// <param name="controller">SMBus controller instance</param>
        /// <param name="offset">Byte offset</param>
        /// <returns>Byte value at <paramref name="offset"/></returns>
        public static byte ReadByte(Smbus controller, ushort offset) {
            AdjustPageAddress(controller, offset);

            if (DetectDdr5(controller)) {
                offset = (byte)((offset % 128) | Spd5Register.NVMREG);
            }

            return controller.ReadByte(controller, controller.I2CAddress, offset);
        }

        /// <summary>
        /// Reads bytes from the EEPROM
        /// </summary>
        /// <param name="controller">SMBus controller instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="count">Total number of bytes to read from <paramref name="offset"/></param>
        /// <returns>A byte array containing byte values</returns>
        public static byte[] ReadByte(Smbus controller, ushort offset, byte count) {

            if (count == 0) {
                throw new Exception($"No bytes to read");
            }

            byte[] result = new byte[count];

            for (ushort i = 0; i < count; i++) {
                result[i] = ReadByte(controller, (ushort)(i + offset));
            }

            return result;
        }

        /// <summary>
        /// Write a byte to the EEPROM
        /// </summary>
        /// <param name="controller">SMBus controller instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is written to <paramref name="offset"/> </returns>
        public static bool WriteByte(Smbus controller, ushort offset, byte value) {

            AdjustPageAddress(controller, offset);

            if (DetectDdr5(controller)) {
                offset = (byte)((offset % 128) | Spd5Register.NVMREG);
            }

            return controller.WriteByte(controller, controller.I2CAddress, offset, value);
        }

        /// <summary>
        /// Write a byte array to the EEPROM
        /// </summary>
        /// <param name="controller">SMBus controller instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte array</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is written to <paramref name="offset"/></returns>
        public static bool WriteByte(Smbus controller, ushort offset, byte[] value) {

            for (ushort i = 0; i < value.Length; i++) {
                if (!WriteByte(controller, (ushort)(i + offset), value[i])) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Write a byte to the EEPROM. The byte is written only if its value differs from the one already saved at the same address.
        /// </summary>
        /// <param name="controller">SMBus controller instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true"/> if byte read at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool UpdateByte(Smbus controller, ushort offset, byte value) {
            return VerifyByte(controller, offset, value) || WriteByte(controller, offset, value);
        }

        /// <summary>
        /// Write a byte array to the EEPROM. The page is written only if its value differs from the one already saved at the same address.
        /// </summary>
        /// <param name="controller">SMBus controller instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte array contents</param>
        /// <returns><see langword="true"/> if bytes read at <paramref name="offset"/> matches <paramref name="value"/> values</returns>
        public static bool UpdateByte(Smbus controller, ushort offset, byte[] value) {
            return VerifyByte(controller, offset, value) || WriteByte(controller, offset, value);
        }

        /// <summary>
        /// Verifies if the offset content matches the input specified
        /// </summary>
        /// <param name="controller">SMBus controller instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true"/> if bytes at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool VerifyByte(Smbus controller, ushort offset, byte value) {
            return ReadByte(controller, offset) == value;
        }

        /// <summary>
        /// Verifies if the offset content matches the input specified
        /// </summary>
        /// <param name="controller">SMBus controller instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="value">Byte array</param>
        /// <returns><see langword="true"/> if bytes at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool VerifyByte(Smbus controller, ushort offset, byte[] value) {

            byte[] source = ReadByte(controller, offset, (byte)value.Length);

            return Data.CompareByteArray(source, value);
        }

        /// <summary>
        /// Reset EEPROM page address
        /// </summary>
        /// <param name="controller">SMBus controller instance</param>
        public static void ResetPageAddress(Smbus controller) {
            SetPageAddress(controller, 0);
        }

        /// <summary>
        /// Adjust EEPROM page address to access lower or upper 256 bytes of the entire 512 byte EEPROM array
        /// </summary>
        /// <param name="controller">System device instance</param>
        /// <param name="eepromPageNumber">Page number</param>
        private static void SetPageAddress(Smbus controller, byte eepromPageNumber) {

            if (controller.MaxSpdSize != 0 && controller.MaxSpdSize < (ushort)Spd.DataLength.DDR4) {
                return;
            }

            if (controller.MaxSpdSize == (ushort)Spd.DataLength.DDR4 && eepromPageNumber > 1 ||
                controller.MaxSpdSize == (ushort)Spd.DataLength.DDR5 && eepromPageNumber > 15) {
                throw new ArgumentOutOfRangeException(nameof(eepromPageNumber));
            }

            if (DetectDdr5(controller)) {
                // DDR5 page
                controller.WriteByte(controller, controller.I2CAddress, Spd5Register.MEMREG & Spd5Register.MR11, eepromPageNumber);
            }
            else {
                // DDR4 page
                controller.WriteByte(controller, (byte)((EepromCommand.SPA0 >> 1) + eepromPageNumber));
            }

            PageNumber = eepromPageNumber;
        }

        /// <summary>
        /// Adjust EEPROM page number according to specified offset
        /// </summary>
        /// <param name="controller">SMBus controller instance</param>
        /// <param name="offset">Byte position</param>
        private static void AdjustPageAddress(Smbus controller, ushort offset) {

            if (controller.MaxSpdSize >= (ushort)Spd.DataLength.Minimum) {
                if (offset > controller.MaxSpdSize) {
                    throw new IndexOutOfRangeException($"Invalid offset");
                }

                if (controller.MaxSpdSize < (ushort)Spd.GetSpdSize(Spd.RamType.DDR4)) {
                    return;
                }
            }

            byte targetPage;

            if (DetectDdr5(controller)) {
                targetPage = (byte)(offset >> 7);
            }
            else {
                targetPage = (byte)(offset >> 8);
            }

            if (targetPage != PageNumber) {
                SetPageAddress(controller, targetPage);
            }
        }

        /// <summary>
        /// Detects if DDR5 is present on the controller
        /// </summary>
        /// <param name="controller">Smbus controller instance</param>
        /// <returns><see langword="true"/> if DDR5 is present on the Smbus</returns>
        public static bool DetectDdr5(Smbus controller) {
            return false;
        }

        /// <summary>
        /// Read software write protection status
        /// </summary>
        /// <param name="controller">SMBus controller instance</param>
        /// <returns><see langword="true"/> if some blocks are write protected or <see langword="false"/> when all blocks are writable</returns>
        public static bool GetRswp(Smbus controller) {

            for (byte i = 0; i <= 3; i++) {
                if (GetRswp(controller, i)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Read software write protection status
        /// </summary>
        /// <param name="controller">SMBus controller instance</param>
        /// <param name="block">Block number to be checked</param>
        /// <returns><see langword="true"/> if the block is write protected or <see langword="false"/> when the block is writable</returns>
        public static bool GetRswp(Smbus controller, byte block) {

            byte[] rswpCmd = {
                EepromCommand.RPS0,
                EepromCommand.RPS1,
                EepromCommand.RPS2,
                EepromCommand.RPS3,
            };

            block = block > 3 ? (byte)0 : block;

            try {
                return !controller.ReadByte(controller, (byte)(rswpCmd[block] >> 1));
            }
            catch {
                return true;
            }
        }

        /// <summary>
        /// Enables software write protection on all EEPROM blocks
        /// </summary>
        /// <param name="controller">SMBus controller instance</param>
        /// <returns><see langword="true"/> when the write protection has been enabled on all blocks</returns>
        public static bool SetRswp(Smbus controller) {

            for (byte i = 0; i <= 3; i++) {
                if (SetRswp(controller, i)) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Enables software write protection on the specified EEPROM block 
        /// </summary>
        /// <param name="controller">SMBus controller instance</param>
        /// <param name="block">Block number to be write protected</param>
        /// <returns><see langword="true"/> when the write protection has been enabled on block <paramref name="block"/></returns>
        public static bool SetRswp(Smbus controller, byte block) {

            if (controller.MaxSpdSize == (ushort)Spd.DataLength.Minimum && block >= 1) {
                throw new ArgumentOutOfRangeException(nameof(block));
            }

            if (controller.MaxSpdSize == (ushort)Spd.DataLength.DDR4 && block >= 4) {
                throw new ArgumentOutOfRangeException(nameof(block));
            }

            byte[] commands = {
                EepromCommand.SWP0,
                EepromCommand.SWP1,
                EepromCommand.SWP2,
                EepromCommand.SWP3,
            };

            return controller.WriteByte(controller, (byte)(commands[block] >> 1));
        }

        /// <summary>
        /// Clears EEPROM write protection 
        /// </summary>
        /// <param name="controller">SMBus controller instance</param>
        /// <returns><see langword="true"/> if RSWP has been disabled</returns>
        public static bool ClearRswp(Smbus controller) {
            return controller.WriteByte(controller, EepromCommand.CWP >> 1);
        }

        /// <summary>
        /// Tests if EEPROM is writable or permanently protected
        /// </summary>
        /// <param name="controller">SMBus controller instance</param>
        /// <returns><see langword="true"/> if PSWP is enabled or <see langword="false"/> if PSWP has NOT been set and EEPROM is writable</returns>
        public static bool GetPswp(Smbus controller) {
            return !controller.ReadByte(controller, (byte)((EepromCommand.PWPB << 3) | (controller.I2CAddress & 0b111)));
        }

        #endregion

        #region Arduino

        /// <summary>
        /// Reads a single byte from the EEPROM
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="offset">Byte offset</param>
        /// <returns>Byte value at <paramref name="offset"/></returns>
        public static byte ReadByte(Arduino device, ushort offset) {
            if (offset > (int)Spd.DataLength.DDR5 && device.DetectDdr5(device.I2CAddress)) {
                throw new IndexOutOfRangeException($"Invalid DDR5 offset");
            }

            try {
                return device.ExecuteCommand(new[] {
                    Arduino.Command.READBYTE,
                    device.I2CAddress,
                    (byte)(offset >> 8),   // MSB
                    (byte)(offset & 0xFF), // LSB
                    (byte)1 });
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
        /// <param name="count">Total number of bytes to read from <paramref name="offset"/> </param>
        /// <returns>A byte array containing byte values</returns>
        public static byte[] ReadByte(Arduino device, ushort offset, byte count) {
            if (offset > (int)Spd.DataLength.DDR5 && device.DetectDdr5(device.I2CAddress)) {
                throw new IndexOutOfRangeException($"Invalid DDR5 offset");
            }

            if (count == 0) {
                throw new Exception($"No bytes to read");
            }

            try {
                return device.ExecuteCommand(new[] {
                    Arduino.Command.READBYTE,
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
        /// <returns><see langword="true"/> if <paramref name="value"/> is written to <paramref name="offset"/> </returns>
        public static bool WriteByte(Arduino device, ushort offset, byte value) {
            if (offset > (int)Spd.DataLength.DDR5 && device.DetectDdr5(device.I2CAddress)) {
                throw new IndexOutOfRangeException($"Invalid DDR5 offset");
            }

            try {
                return device.ExecuteCommand(new[] {
                    Arduino.Command.WRITEBYTE,
                    device.I2CAddress,
                    (byte)(offset >> 8),   // MSB
                    (byte)(offset & 0xFF), // LSB
                    value
                }) == Arduino.Response.SUCCESS;
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
        /// <returns><see langword="true"/> if <paramref name="value"/> is written to <paramref name="offset"/> </returns>
        public static bool WriteByte(Arduino device, ushort offset, byte[] value) {
            if (offset > (int)Spd.DataLength.DDR5 && device.DetectDdr5(device.I2CAddress)) {
                throw new IndexOutOfRangeException($"Invalid DDR5 offset");
            }

            if (value.Length > 16 || value.Length == 0) {
                throw new Exception($"Invalid page size ({value.Length})");
            }

            // Prepare command + data
            byte[] command = new byte[5 + value.Length];
            command[0] = Arduino.Command.WRITEPAGE;
            command[1] = device.I2CAddress;
            command[2] = (byte)(offset >> 8);   // MSB
            command[3] = (byte)(offset & 0xFF); // LSB
            command[4] = (byte)value.Length;

            Array.Copy(value, 0, command, 5, value.Length);

            try {
                return device.ExecuteCommand(command) == Arduino.Response.SUCCESS;
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
        /// <returns><see langword="true"/> if byte read at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool UpdateByte(Arduino device, ushort offset, byte value) {
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
        /// <returns><see langword="true"/> if page read at <paramref name="offset"/> matches <paramref name="value"/> values</returns>
        public static bool UpdateByte(Arduino device, ushort offset, byte[] value) {
            if (device.DetectDdr5(device.I2CAddress) && offset > (int)Spd.DataLength.DDR5) {
                throw new IndexOutOfRangeException($"Invalid DDR5 offset");
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
        /// <returns><see langword="true"/> if byte at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool VerifyByte(Arduino device, ushort offset, byte value) {
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
        /// <returns><see langword="true"/> if bytes at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool VerifyByte(Arduino device, ushort offset, byte[] value) {
            try {
                byte[] source = ReadByte(device, offset, (byte)value.Length);

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
        /// <returns><see langword="true"/> when the write protection has been enabled on block <paramref name="block"/></returns>
        public static bool SetRswp(Arduino device, byte block) {
            try {
                return device.ExecuteCommand(new[] { Arduino.Command.RSWP, block, Arduino.Command.ON }) == Arduino.Response.SUCCESS;
            }
            catch {
                throw new Exception($"Unable to set RSWP on {device.PortName}");
            }
        }

        /// <summary>
        /// Enables software write protection on all 4 EEPROM blocks
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <returns><see langword="true"/> when the write protection has been enabled on all available blocks</returns>
        public static bool SetRswp(Arduino device) {
            try {
                for (byte i = 0; i <= 3; i++) {
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
        /// <returns><see langword="true"/> if the at least one block is write protected (or RSWP is not supported), or <see langword="false"/> when the EEPROM is writable</returns>
        public static bool GetRswp(Arduino device) {
            try {
                for (byte i = 0; i <= 3; i++) {
                    if (device.ExecuteCommand(new[] { Arduino.Command.RSWP, i, Arduino.Command.GET }) == Arduino.Response.ENABLED) {
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
        /// <returns><see langword="true"/> if the block is write protected (or RSWP is not supported) or <see langword="false"/> when the block is writable</returns>
        public static bool GetRswp(Arduino device, byte block) {
            try {
                return device.ExecuteCommand(new[] { Arduino.Command.RSWP, block, Arduino.Command.GET }) == Arduino.Response.ENABLED;
            }
            catch {
                throw new Exception($"Unable to get block {block} RSWP status on {device.PortName}");
            }
        }

        /// <summary>
        /// Clears EEPROM write protection 
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true"/> if the write protection has been disabled</returns>
        public static bool ClearRswp(Arduino device) {
            try {
                return device.ExecuteCommand(new[] { Arduino.Command.RSWP, Arduino.Command.DNC, Arduino.Command.OFF }) == Arduino.Response.SUCCESS;
            }
            catch {
                throw new Exception($"Unable to clear RSWP on {device.PortName}");
            }
        }

        /// <summary>
        /// Sets permanent write protection on supported EEPROMs
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true"/> when the permanent write protection is enabled</returns>
        public static bool SetPswp(Arduino device) {
            try {
                return device.ExecuteCommand(new[] { Arduino.Command.PSWP, device.I2CAddress, Arduino.Command.ON }) == Arduino.Response.SUCCESS;
            }
            catch {
                throw new Exception($"Unable to set PSWP on {device.PortName}");
            }
        }

        /// <summary>
        /// Tests if EEPROM is writable or permanently protected
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true"/> when PSWP is enabled or <see langword="false"/> if when PSWP has NOT been set and EEPROM is writable</returns>
        public static bool GetPswp(Arduino device) {
            try {
                return device.ExecuteCommand(new[] { Arduino.Command.PSWP, device.I2CAddress, Arduino.Command.GET }) == Arduino.Response.ENABLED;
            }
            catch {
                throw new Exception($"Unable to get PSWP status on {device.PortName}");
            }
        }

        #endregion
        
        /// <summary>
        /// EEPROM or SPD5 hub page number
        /// </summary>
        public static byte PageNumber { get; set; }

        /// <summary>
        /// DDR4 EEPROM commands
        /// </summary>
        internal struct EepromCommand {
            // DDR4 Page commands
            internal const byte SPA0 = 0x6C;
            internal const byte SPA1 = 0x6E;
            internal const byte RPA  = 0x6D;

            // DDR4 RSWP commands
            internal const byte RPS0 = 0x63;
            internal const byte RPS1 = 0x69;
            internal const byte RPS2 = 0x6B;
            internal const byte RPS3 = 0x61;

            internal const byte SWP0 = 0x62;
            internal const byte SWP1 = 0x68;
            internal const byte SWP2 = 0x6A;
            internal const byte SWP3 = 0x60;

            internal const byte CWP  = 0x66;

            // PSWP bitmask
            internal const byte PWPB = 0b0110;
        }

        /// <summary>
        /// DDR5 SPD5 hub registers
        /// </summary>
        internal struct Spd5Register {
            // SPD5 internal register bitmask
            internal const byte MEMREG = 0b11111;

            // SPD5 NVM location bitmask
            internal const byte NVMREG = 0b10000000;

            // Local device type HID behind SPD5 Hub device
            internal const byte LocalHid = 0b111;

            // I2C Legacy Mode Device Configuration
            internal const byte MR11 = 11;
            // Write Protection For NVM Blocks [7:0]
            internal const byte MR12 = 12;
            // Write Protection For NVM Blocks [15:8]
            internal const byte MR13 = 13;
        }

        /// <summary>
        /// DDR5 Slave Device LID Codes
        /// </summary>
        internal struct LidCode {
            // SPD5 hub
            internal const byte SpdHub = 0b1010;

            // Registering Clock Driver
            internal const byte Rcd    = 0b1011;

            // Power Management ICs
            internal const byte Pmic0  = 0b1001;
            internal const byte Pmic1  = 0b1000;
            internal const byte Pmic2  = 0b1100;

            // Temperature sensors
            internal const byte Ts0    = 0b0010;
            internal const byte Ts1    = 0b0110;
        }

        /// <summary>
        /// Checks if input address is a valid EEPROM address
        /// </summary>
        /// <param name="address">Input address</param>
        /// <returns><see langword="true"/> if <paramref name="address"/> is a valid EEPROM address between 0x50 and 0x57</returns>
        public static bool ValidateEepromAddress(byte address) {
            return address >> 3 == 0b1010;
        }

        /// <summary>
        /// EEPROM Write protection types
        /// </summary>
        public enum WriteProtectionType {
            /// <summary>
            /// Unprotected
            /// </summary>
            [Description("Unprotected")]
            None,
            /// <summary>
            /// Permanent software write protection
            /// </summary>
            [Description("Permanent software write protection")]
            PSWP,
            /// <summary>
            /// Reversible software write protection
            /// </summary>
            [Description("Reversible software write protection")]
            RSWP,
            /// <summary>
            /// Hardware write protection
            /// </summary>
            [Description("Hardware write protection")] 
            HWP,
            /// <summary>
            /// BIOS SPD write disable
            /// </summary>
            [Description("BIOS SPD write disable")] 
            SPDWD,
        }
    }
}