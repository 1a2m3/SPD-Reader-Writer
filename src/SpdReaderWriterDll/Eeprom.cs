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
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte offset</param>
        /// <returns>Byte value at <paramref name="offset"/></returns>
        public static byte ReadByte(Smbus smbus, ushort offset) {
            AdjustPageAddress(smbus, offset);

            if (DetectDdr5(smbus)) {
                offset = (byte)((offset % 128) | Spd5Register.NVMREG);
            }

            return smbus.ReadByte(smbus, smbus.I2CAddress, offset);
        }

        /// <summary>
        /// Reads bytes from the EEPROM
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="count">Total number of bytes to read from <paramref name="offset"/></param>
        /// <returns>A byte array containing byte values</returns>
        public static byte[] ReadByte(Smbus smbus, ushort offset, int count) {

            if (count == 0) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            byte[] result = new byte[count];

            for (ushort i = 0; i < count; i++) {
                result[i] = ReadByte(smbus, (ushort)(i + offset));
            }

            return result;
        }

        /// <summary>
        /// Writes a byte to the EEPROM
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is written to <paramref name="offset"/> </returns>
        public static bool WriteByte(Smbus smbus, ushort offset, byte value) {

            AdjustPageAddress(smbus, offset);

            if (DetectDdr5(smbus)) {
                offset = (byte)((offset % 128) | Spd5Register.NVMREG);
            }

            return smbus.WriteByte(smbus, smbus.I2CAddress, offset, value);
        }

        /// <summary>
        /// Writes a byte array to the EEPROM
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte array</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is written to <paramref name="offset"/></returns>
        public static bool WriteByte(Smbus smbus, ushort offset, byte[] value) {

            for (ushort i = 0; i < value.Length; i++) {
                if (!WriteByte(smbus, (ushort)(i + offset), value[i])) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Writes a byte to the EEPROM. The byte is written only if its value differs from the one already saved at the same address.
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true"/> if byte read at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool UpdateByte(Smbus smbus, ushort offset, byte value) {
            return VerifyByte(smbus, offset, value) || WriteByte(smbus, offset, value);
        }

        /// <summary>
        /// Writes a byte array to the EEPROM. The page is written only if its value differs from the one already saved at the same address.
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte array contents</param>
        /// <returns><see langword="true"/> if bytes read at <paramref name="offset"/> matches <paramref name="value"/> values</returns>
        public static bool UpdateByte(Smbus smbus, ushort offset, byte[] value) {
            return VerifyByte(smbus, offset, value) || WriteByte(smbus, offset, value);
        }

        /// <summary>
        /// Verifies if the offset content matches the input specified
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true"/> if bytes at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool VerifyByte(Smbus smbus, ushort offset, byte value) {
            return ReadByte(smbus, offset) == value;
        }

        /// <summary>
        /// Verifies if the offset content matches the input specified
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="value">Byte array</param>
        /// <returns><see langword="true"/> if bytes at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool VerifyByte(Smbus smbus, ushort offset, byte[] value) {

            byte[] source = ReadByte(smbus, offset, value.Length);

            return Data.CompareArray(source, value);
        }

        /// <summary>
        /// Reset EEPROM page address
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        public static void ResetPageAddress(Smbus smbus) {
            SetPageAddress(smbus, 0);
        }

        /// <summary>
        /// Adjust EEPROM page address to access lower or upper 256 bytes of the entire 512 byte EEPROM array
        /// </summary>
        /// <param name="smbus">System device instance</param>
        /// <param name="eepromPageNumber">Page number</param>
        private static void SetPageAddress(Smbus smbus, byte eepromPageNumber) {

            if (smbus.MaxSpdSize != 0 && smbus.MaxSpdSize < (ushort)Spd.DataLength.DDR4) {
                return;
            }

            if (smbus.MaxSpdSize == (ushort)Spd.DataLength.DDR4 && eepromPageNumber > 1 ||
                smbus.MaxSpdSize == (ushort)Spd.DataLength.DDR5 && eepromPageNumber > 15) {
                throw new ArgumentOutOfRangeException(nameof(eepromPageNumber));
            }

            if (DetectDdr5(smbus)) {
                // DDR5 page
                smbus.WriteByte(smbus, smbus.I2CAddress, Spd5Register.MR11, eepromPageNumber);
            }
            else {
                // DDR4 page
                smbus.WriteByte(smbus, (byte)((EepromCommand.SPA0 >> 1) + eepromPageNumber));
            }

            PageNumber = eepromPageNumber;
        }

        /// <summary>
        /// Adjust EEPROM page number according to specified offset
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte position</param>
        private static void AdjustPageAddress(Smbus smbus, ushort offset) {

            if (smbus.MaxSpdSize >= (ushort)Spd.DataLength.Minimum) {
                if (offset > smbus.MaxSpdSize) {
                    throw new IndexOutOfRangeException($"Invalid offset");
                }

                if (smbus.MaxSpdSize < (ushort)Spd.GetSpdSize(Spd.RamType.DDR4)) {
                    return;
                }
            }

            byte targetPage;

            if (DetectDdr5(smbus)) {
                targetPage = (byte)(offset >> 7);
            }
            else {
                targetPage = (byte)(offset >> 8);
            }

            if (targetPage != PageNumber) {
                SetPageAddress(smbus, targetPage);
            }
        }

        /// <summary>
        /// Detects if DDR5 is present on the controller
        /// </summary>
        /// <param name="smbus">Smbus controller instance</param>
        /// <returns><see langword="true"/> if DDR5 is present on the Smbus</returns>
        public static bool DetectDdr5(Smbus smbus) {
            return smbus.ProbeAddress((byte)(LidCode.Pmic0 << 3 | (Spd5Register.LocalHid & smbus.I2CAddress)));
        }

        /// <summary>
        /// Read software write protection status
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="block">Block number to be checked</param>
        /// <returns><see langword="true"/> if the block is write protected or <see langword="false"/> when the block is writable</returns>
        public static bool GetRswp(Smbus smbus, byte block) {

            byte[] rswpCmd = {
                EepromCommand.RPS0,
                EepromCommand.RPS1,
                EepromCommand.RPS2,
                EepromCommand.RPS3,
            };

            block = block > 3 ? (byte)0 : block;

            try {
                return !smbus.ReadByte(smbus, (byte)(rswpCmd[block] >> 1));
            }
            catch {
                return true;
            }
        }

        /// <summary>
        /// Enables software write protection on the specified EEPROM block 
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="block">Block number to be write protected</param>
        /// <returns><see langword="true"/> when the write protection has been enabled on block <paramref name="block"/></returns>
        public static bool SetRswp(Smbus smbus, byte block) {

            if (smbus.MaxSpdSize == (ushort)Spd.DataLength.Minimum && block >= 1) {
                throw new ArgumentOutOfRangeException(nameof(block));
            }

            if (smbus.MaxSpdSize == (ushort)Spd.DataLength.DDR4 && block >= 4) {
                throw new ArgumentOutOfRangeException(nameof(block));
            }

            if (DetectDdr5(smbus)) {

                byte memReg;
                byte currentValue; // Existing RSWP value
                byte updatedValue; // Updated RSWP value

                if (block <= 7) {
                    memReg       = Spd5Register.MR12;
                    currentValue = smbus.ReadByte(smbus, smbus.I2CAddress, memReg);
                    updatedValue = Data.SetBit(currentValue, block, true);
                }
                else if (block <= 15) {
                    memReg       = Spd5Register.MR13;
                    currentValue = smbus.ReadByte(smbus, smbus.I2CAddress, memReg);
                    updatedValue = Data.SetBit(currentValue, block - 8, true);
                }
                else {
                    throw new ArgumentOutOfRangeException($"Wrong block # specified");
                }

                return smbus.WriteByte(smbus, smbus.I2CAddress, memReg, (byte)(currentValue | updatedValue));
            }

            byte[] commands = {
                EepromCommand.SWP0,
                EepromCommand.SWP1,
                EepromCommand.SWP2,
                EepromCommand.SWP3,
            };

            return smbus.WriteByte(smbus, (byte)(commands[block] >> 1));
        }

        /// <summary>
        /// Clears EEPROM write protection 
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <returns><see langword="true"/> if RSWP has been disabled</returns>
        public static bool ClearRswp(Smbus smbus) {

            if (DetectDdr5(smbus)) {
                return smbus.WriteByte(smbus, smbus.I2CAddress, Spd5Register.MR12, 0x00) &&
                       smbus.WriteByte(smbus, smbus.I2CAddress, Spd5Register.MR13, 0x00);
            }

            return smbus.WriteByte(smbus, EepromCommand.CWP >> 1);
        }

        /// <summary>
        /// Tests if EEPROM is writable or permanently protected
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <returns><see langword="true"/> if PSWP is enabled or <see langword="false"/> if PSWP has NOT been set and EEPROM is writable</returns>
        public static bool GetPswp(Smbus smbus) {
            return !smbus.ReadByte(smbus, (byte)((EepromCommand.PWPB << 3) | (smbus.I2CAddress & 0b111)));
        }

        #endregion

        #region Arduino

        /// <summary>
        /// Reads a single byte from the EEPROM
        /// </summary>
        /// <param name="arduino">SPD reader/writer device instance</param>
        /// <param name="offset">Byte offset</param>
        /// <returns>Byte value at <paramref name="offset"/></returns>
        public static byte ReadByte(Arduino arduino, ushort offset) {
            if (offset > (int)Spd.DataLength.DDR5 && arduino.DetectDdr5(arduino.I2CAddress)) {
                throw new IndexOutOfRangeException($"Invalid DDR5 offset");
            }

            try {
                return arduino.ExecuteCommand(new[] {
                    Arduino.Command.READBYTE,
                    arduino.I2CAddress,
                    (byte)(offset >> 8), // MSB
                    (byte)offset,        // LSB
                    (byte)1 });
            }
            catch {
                throw new Exception($"Unable to read byte 0x{offset:X4} at {arduino.PortName}:{arduino.I2CAddress}");
            }
        }

        /// <summary>
        /// Reads bytes from the EEPROM
        /// </summary>
        /// <param name="arduino">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position to start reading from</param>
        /// <param name="count">Total number of bytes to read from <paramref name="offset"/> </param>
        /// <returns>A byte array containing byte values</returns>
        public static byte[] ReadByte(Arduino arduino, ushort offset, byte count) {

            if (offset > (int)Spd.DataLength.DDR5 && arduino.DetectDdr5(arduino.I2CAddress)) {
                throw new IndexOutOfRangeException($"Invalid DDR5 offset");
            }

            if (count == 0) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            try {
                return arduino.ExecuteCommand(new[] {
                    Arduino.Command.READBYTE,
                    arduino.I2CAddress,
                    (byte)(offset >> 8), // MSB
                    (byte)offset,        // LSB
                    count
                }, count);
            }
            catch {
                throw new Exception($"Unable to read byte # 0x{offset:X4} at {arduino.PortName}:{arduino.I2CAddress}");
            }
        }

        /// <summary>
        /// Write a byte to the EEPROM
        /// </summary>
        /// <param name="arduino">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is written to <paramref name="offset"/> </returns>
        public static bool WriteByte(Arduino arduino, ushort offset, byte value) {
            if (offset > (int)Spd.DataLength.DDR5 && arduino.DetectDdr5(arduino.I2CAddress)) {
                throw new IndexOutOfRangeException($"Invalid DDR5 offset");
            }

            try {
                return arduino.ExecuteCommand(new[] {
                    Arduino.Command.WRITEBYTE,
                    arduino.I2CAddress,
                    (byte)(offset >> 8), // MSB
                    (byte)offset,        // LSB
                    value
                }) == Arduino.Response.SUCCESS;
            }
            catch {
                throw new Exception($"Unable to write \"0x{value:X2}\" to # 0x{offset:X4} at {arduino.PortName}:{arduino.I2CAddress}");
            }
        }

        /// <summary>
        /// Write a page to the EEPROM
        /// </summary>
        /// <param name="arduino">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Page contents</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is written to <paramref name="offset"/> </returns>
        public static bool WriteByte(Arduino arduino, ushort offset, byte[] value) {

            if (offset > (int)Spd.DataLength.DDR5 && arduino.DetectDdr5(arduino.I2CAddress)) {
                throw new IndexOutOfRangeException($"Invalid DDR5 offset");
            }

            if (value.Length > 16 || value.Length == 0) {
                throw new ArgumentOutOfRangeException($"Invalid page size ({value.Length})");
            }

            // Prepare command
            byte[] command = {
                Arduino.Command.WRITEPAGE,
                arduino.I2CAddress,
                (byte)(offset >> 8), // MSB
                (byte)offset,        // LSB
                (byte)value.Length
            };

            try {
                return arduino.ExecuteCommand(Data.MergeArray(command, value)) == Arduino.Response.SUCCESS;
            }
            catch {
                throw new Exception($"Unable to write page of {value.Length} byte(s) to # 0x{offset:X4} at {arduino.PortName}:{arduino.I2CAddress}");
            }
        }

        /// <summary>
        /// Write a byte to the EEPROM. The byte is written only if its value differs from the one already saved at the same address.
        /// </summary>
        /// <param name="arduino">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true"/> if byte read at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool UpdateByte(Arduino arduino, ushort offset, byte value) {
            try {
                return VerifyByte(arduino, offset, value) ||
                       WriteByte(arduino, offset, value);
            }
            catch {
                throw new Exception($"Unable to update byte # 0x{offset:X4} with \"0x{value:X2}\" at {arduino.PortName}:{arduino.I2CAddress}");
            }
        }

        /// <summary>
        /// Write a page to the EEPROM. The page is written only if its value differs from the one already saved at the same address.
        /// </summary>
        /// <param name="arduino">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Page contents</param>
        /// <returns><see langword="true"/> if page read at <paramref name="offset"/> matches <paramref name="value"/> values</returns>
        public static bool UpdateByte(Arduino arduino, ushort offset, byte[] value) {

            if (arduino.DetectDdr5(arduino.I2CAddress) && offset > (int)Spd.DataLength.DDR5) {
                throw new IndexOutOfRangeException($"Invalid DDR5 offset");
            }

            if (value.Length > 16 || value.Length == 0) {
                throw new ArgumentOutOfRangeException($"Invalid page size ({value.Length})");
            }

            try {
                return VerifyByte(arduino, offset, value) ||
                       WriteByte(arduino, offset, value);
            }
            catch {
                throw new Exception($"Unable to update page at # 0x{offset:X4} with \"0x{value:X2}\" at {arduino.PortName}:{arduino.I2CAddress}");
            }
        }

        /// <summary>
        /// Verifies if the offset content matches the input specified
        /// </summary>
        /// <param name="arduino">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true"/> if byte at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool VerifyByte(Arduino arduino, ushort offset, byte value) {
            try {
                return ReadByte(arduino, offset) == value;
            }
            catch {
                throw new Exception($"Unable to verify byte # 0x{offset:X4} at {arduino.PortName}:{arduino.I2CAddress}");
            }
        }

        /// <summary>
        /// Verifies if the offset content matches the input specified
        /// </summary>
        /// <param name="arduino">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte array</param>
        /// <returns><see langword="true"/> if bytes at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool VerifyByte(Arduino arduino, ushort offset, byte[] value) {
            try {
                byte[] source = ReadByte(arduino, offset, (byte)value.Length);

                return Data.CompareArray(source, value);
            }
            catch {
                throw new Exception($"Unable to verify bytes # 0x{offset:X4}-0x{offset + value.Length:X4} at {arduino.PortName}:{arduino.I2CAddress}");
            }
        }

        /// <summary>
        /// Enables software write protection on the specified EEPROM block 
        /// </summary>
        /// <param name="arduino">SPD reader/writer device instance</param>
        /// <param name="block">Block number to be write protected</param>
        /// <returns><see langword="true"/> when the write protection has been enabled on block <paramref name="block"/></returns>
        public static bool SetRswp(Arduino arduino, byte block) {
            try {
                return arduino.ExecuteCommand(new[] { Arduino.Command.RSWP, block, Arduino.Command.ON }) == Arduino.Response.SUCCESS;
            }
            catch {
                throw new Exception($"Unable to set RSWP on {arduino.PortName}");
            }
        }

        /// <summary>
        /// Read software write protection status
        /// </summary>
        /// <param name="arduino">SPD reader/writer device instance</param>
        /// <param name="block">Block number to be checked</param>
        /// <returns><see langword="true"/> if the block is write protected (or RSWP is not supported) or <see langword="false"/> when the block is writable</returns>
        public static bool GetRswp(Arduino arduino, byte block) {
            try {
                return arduino.ExecuteCommand(new[] { Arduino.Command.RSWP, block, Arduino.Command.GET }) == Arduino.Response.ENABLED;
            }
            catch {
                throw new Exception($"Unable to get block {block} RSWP status on {arduino.PortName}");
            }
        }

        /// <summary>
        /// Clears EEPROM write protection 
        /// </summary>
        /// <param name="arduino">Device instance</param>
        /// <returns><see langword="true"/> if the write protection has been disabled</returns>
        public static bool ClearRswp(Arduino arduino) {
            try {
                return arduino.ExecuteCommand(new[] { Arduino.Command.RSWP, Data.BoolToNum<byte>(false), Arduino.Command.OFF }) == Arduino.Response.SUCCESS;
            }
            catch {
                throw new Exception($"Unable to clear RSWP on {arduino.PortName}");
            }
        }

        /// <summary>
        /// Sets permanent write protection on supported EEPROMs
        /// </summary>
        /// <param name="arduino">Device instance</param>
        /// <returns><see langword="true"/> when the permanent write protection is enabled</returns>
        public static bool SetPswp(Arduino arduino) {
            try {
                return arduino.ExecuteCommand(new[] { Arduino.Command.PSWP, arduino.I2CAddress, Arduino.Command.ON }) == Arduino.Response.SUCCESS;
            }
            catch {
                throw new Exception($"Unable to set PSWP on {arduino.PortName}");
            }
        }

        /// <summary>
        /// Tests if EEPROM is writable or permanently protected
        /// </summary>
        /// <param name="arduino">Device instance</param>
        /// <returns><see langword="true"/> when PSWP is enabled or <see langword="false"/> if when PSWP has NOT been set and EEPROM is writable</returns>
        public static bool GetPswp(Arduino arduino) {
            try {
                return arduino.ExecuteCommand(new[] { Arduino.Command.PSWP, arduino.I2CAddress, Arduino.Command.GET }) == Arduino.Response.ENABLED;
            }
            catch {
                throw new Exception($"Unable to get PSWP status on {arduino.PortName}");
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
        public struct Spd5Register {
            // SPD5 NVM location bitmask
            internal const byte NVMREG = 0b10000000;

            // Local device type HID behind SPD5 Hub device
            internal const byte LocalHid = 0b111;

            // I2C Legacy Mode Device Configuration
            internal const byte MR11 = 11;

            // Write Protection For NVM Blocks [7:0]
            public const byte MR12   = 12;

            // Write Protection For NVM Blocks [15:8]
            public const byte MR13   = 13;

            // Device Status
            public const byte MR48   = 48;
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