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

namespace SpdReaderWriterCore {
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
        public static byte Read(Smbus smbus, ushort offset) {
            AdjustPageAddress(smbus, offset);
            AdjustOffset(smbus, ref offset);

            return smbus.ReadByte(smbus.I2CAddress, offset);
        }

        /// <summary>
        /// Reads bytes from the EEPROM
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="count">Total number of bytes to read from <paramref name="offset"/></param>
        /// <returns>A byte array containing byte values</returns>
        public static byte[] Read(Smbus smbus, ushort offset, int count) {

            if (count == 0) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            byte[] result = new byte[count];

            for (ushort i = 0; i < count; i++) {
                result[i] = Read(smbus, (ushort)(i + offset));
            }

            return result;
        }

        /// <summary>
        /// Writes a byte to the EEPROM
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is written to <paramref name="offset"/></returns>
        public static bool Write(Smbus smbus, ushort offset, byte value) {

            AdjustPageAddress(smbus, offset);
            AdjustOffset(smbus, ref offset);

            return smbus.WriteByte(smbus.I2CAddress, offset, value);
        }

        /// <summary>
        /// Writes a byte array to the EEPROM
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte array</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is written to <paramref name="offset"/></returns>
        public static bool Write(Smbus smbus, ushort offset, byte[] value) {

            for (ushort i = 0; i < value.Length; i++) {
                if (!Write(smbus, (ushort)(i + offset), value[i])) {
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
        /// <returns><see langword="true"/> if byte read at <paramref name="offset"/> matches <paramref name="value"/></returns>
        public static bool Update(Smbus smbus, ushort offset, byte value) {
            return Verify(smbus, offset, value) || Write(smbus, offset, value);
        }

        /// <summary>
        /// Writes a byte array to the EEPROM. The page is written only if its value differs from the one already saved at the same address.
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte array contents</param>
        /// <returns><see langword="true"/> if bytes read at <paramref name="offset"/> matches <paramref name="value"/></returns>
        public static bool Update(Smbus smbus, ushort offset, byte[] value) {
            return Verify(smbus, offset, value) || Write(smbus, offset, value);
        }

        /// <summary>
        /// Verifies if the offset content matches the input specified
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true"/> if bytes at <paramref name="offset"/> matches <paramref name="value"/></returns>
        public static bool Verify(Smbus smbus, ushort offset, byte value) {
            return Read(smbus, offset) == value;
        }

        /// <summary>
        /// Verifies if the offset content matches the input specified
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="value">Byte array</param>
        /// <returns><see langword="true"/> if bytes at <paramref name="offset"/> matches <paramref name="value"/></returns>
        public static bool Verify(Smbus smbus, ushort offset, byte[] value) {

            byte[] source = Read(smbus, offset, value.Length);

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

            if (smbus.MaxSpdSize == Spd.DataLength.Minimum) {
                return;
            }
            
            if (smbus.MaxSpdSize == Spd.DataLength.DDR4 && eepromPageNumber > 1 ||
                smbus.MaxSpdSize == Spd.DataLength.DDR5 && eepromPageNumber > 15) {
                throw new ArgumentOutOfRangeException(nameof(eepromPageNumber));
            }

            if (smbus.IsDdr5Present) {
                // DDR5 page
                smbus.WriteByte(smbus.I2CAddress, Spd5Register.MR11, eepromPageNumber);
            }
            else {
                // DDR4 page
                smbus.WriteByte((byte)((EepromCommand.SPA0 >> 1) + eepromPageNumber));
            }

            PageNumber = eepromPageNumber;
        }

        /// <summary>
        /// Adjust EEPROM page number according to specified offset
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte position</param>
        private static void AdjustPageAddress(Smbus smbus, ushort offset) {

            if (smbus.MaxSpdSize >= Spd.DataLength.Minimum) {
                if (offset > smbus.MaxSpdSize) {
                    throw new IndexOutOfRangeException("Invalid offset");
                }

                // Page adjustment isn't needed for pre-DDR4
                if (smbus.MaxSpdSize < Spd.DataLength.DDR4) {
                    return;
                }
            }

            byte targetPage = (byte)(offset >> (smbus.IsDdr5Present ? 7 : 8));

            if (targetPage != PageNumber) {
                SetPageAddress(smbus, targetPage);
            }
        }

        /// <summary>
        /// Adjust EEPROM offset value depending on RAM type
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte position</param>
        private static void AdjustOffset(Smbus smbus, ref ushort offset) {
            offset = smbus.IsDdr5Present ? (byte)((offset % 128) | Spd5Register.NVMREG) : offset;
        }

        /// <summary>
        /// Performs write protection test on the specified EEPROM offset
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <param name="offset">Byte position</param>
        /// <returns><see langword="true"/> if byte at <paramref name="offset"/> is writable</returns>
        public static bool WriteTest(Smbus smbus, ushort offset) {
            byte o = Read(smbus, offset);
            return Write(smbus, offset, (byte)(o ^ 0xFF)) && 
                   Write(smbus, offset, o);
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

            try {
                return smbus.IsDdr5Present
                    ? Data.GetBit(smbus.ReadByte(smbus.I2CAddress, (ushort)(Spd5Register.MR12 + Data.BoolToNum<byte>(block >= 8))), block % 8)
                    : !WriteTest(smbus, (ushort)((block > 3 ? 0 : block) * 128));
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

            if ((smbus.MaxSpdSize == Spd.DataLength.Minimum && block >= 1) ||
                (smbus.MaxSpdSize == Spd.DataLength.DDR4 && block >= 4) ||
                (smbus.MaxSpdSize == Spd.DataLength.DDR5 && block >= 16)) {
                throw new ArgumentOutOfRangeException(nameof(block));
            }

            if (smbus.IsDdr5Present) {

                byte memReg       = (byte)(Spd5Register.MR12 + Data.BoolToNum<byte>(Data.GetBit(block, 3))); // (block >= 8)
                byte currentValue = smbus.ReadByte(smbus.I2CAddress, memReg); // Existing RSWP value
                byte updatedValue = Data.SetBit(currentValue, block & 0b111, true); // Updated RSWP value

                return smbus.WriteByte(smbus.I2CAddress, memReg, (byte)(currentValue | updatedValue));
            }

            byte[] commands = {
                EepromCommand.SWP0,
                EepromCommand.SWP1,
                EepromCommand.SWP2,
                EepromCommand.SWP3,
            };

            return smbus.WriteByte((byte)(commands[block] >> 1));
        }

        /// <summary>
        /// Clears EEPROM write protection 
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <returns><see langword="true"/> if RSWP has been disabled</returns>
        public static bool ClearRswp(Smbus smbus) {

            return smbus.IsDdr5Present
                ? smbus.WriteByte(smbus.I2CAddress, Spd5Register.MR12, 0x00) &&
                  smbus.WriteByte(smbus.I2CAddress, Spd5Register.MR13, 0x00) &&
                  smbus.ReadByte(smbus.I2CAddress, Spd5Register.MR12) == 0x00 &&
                  smbus.ReadByte(smbus.I2CAddress, Spd5Register.MR13) == 0x00
                : smbus.WriteByte(EepromCommand.CWP >> 1);
        }

        /// <summary>
        /// Tests if EEPROM is writable or permanently protected
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <returns><see langword="true"/> if PSWP is enabled or <see langword="false"/> if PSWP has NOT been set and EEPROM is writable</returns>
        public static bool GetPswp(Smbus smbus) {
            return !smbus.ReadByte((byte)((EepromCommand.PWPB << 3) | (smbus.I2CAddress & 0b111)));
        }

        /// <summary>
        /// Checks DDR5 offline mode state
        /// </summary>
        /// <param name="smbus">SMBus controller instance</param>
        /// <returns><see langword="true"/> if DDR5 is in offline mode</returns>
        public static bool GetOfflineMode(Smbus smbus) {
            return smbus.IsDdr5Present && Data.GetBit(smbus.ReadByte(Spd5Register.MR48), 2);
        }

        #endregion

        #region Arduino

        /// <summary>
        /// Reads a single byte from the EEPROM
        /// </summary>
        /// <param name="arduino">SPD reader/writer device instance</param>
        /// <param name="offset">Byte offset</param>
        /// <returns>Byte value at <paramref name="offset"/></returns>
        public static byte Read(Arduino arduino, ushort offset) {

            if (!CheckOffset(arduino, offset)) {
                throw new IndexOutOfRangeException("Invalid offset");
            }

            try {
                return arduino.ExecuteCommand<byte>(
                    Arduino.Command.ReadByte,
                    arduino.I2CAddress,
                    (byte)(offset >> 8), // MSB
                    (byte)offset,        // LSB
                    1);
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
        public static byte[] Read(Arduino arduino, ushort offset, byte count) {

            if (count == 0) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (!CheckOffset(arduino, offset)) {
                throw new IndexOutOfRangeException("Invalid offset");
            }

            try {
                return arduino.ExecuteCommand<byte[]>(
                    Arduino.Command.ReadByte,
                    arduino.I2CAddress,
                    (byte)(offset >> 8), // MSB
                    (byte)offset,        // LSB
                    count);
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
        /// <returns><see langword="true"/> if <paramref name="value"/> is written to <paramref name="offset"/></returns>
        public static bool Write(Arduino arduino, ushort offset, byte value) {

            if (!CheckOffset(arduino, offset)) {
                return false;
            }

            try {
                return arduino.ExecuteCommand<bool>(
                    (byte)Arduino.Command.WriteByte,
                    arduino.I2CAddress,
                    (byte)(offset >> 8), // MSB
                    (byte)offset,        // LSB
                    value);
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
        /// <returns><see langword="true"/> if <paramref name="value"/> is written to <paramref name="offset"/></returns>
        public static bool Write(Arduino arduino, ushort offset, byte[] value) {

            if (!CheckOffset(arduino, offset) || !CheckPageSize(value)) {
                return false;
            }

            // Prepare command
            byte[] command = {
                (byte) Arduino.Command.WritePage,
                arduino.I2CAddress,
                (byte)(offset >> 8), // MSB
                (byte)offset,        // LSB
                (byte)value.Length
            };

            try {
                return arduino.ExecuteCommand<bool>(Data.MergeArray(command, value));
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
        /// <returns><see langword="true"/> if byte read at <paramref name="offset"/> matches <paramref name="value"/></returns>
        public static bool Update(Arduino arduino, ushort offset, byte value) {
            try {
                return Verify(arduino, offset, value) || Write(arduino, offset, value);
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
        /// <returns><see langword="true"/> if page read at <paramref name="offset"/> matches <paramref name="value"/></returns>
        public static bool Update(Arduino arduino, ushort offset, byte[] value) {

            if (!CheckOffset(arduino, offset) || !CheckPageSize(value)) {
                return false;
            }

            try {
                return Verify(arduino, offset, value) || Write(arduino, offset, value);
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
        /// <returns><see langword="true"/> if byte at <paramref name="offset"/> matches <paramref name="value"/></returns>
        public static bool Verify(Arduino arduino, ushort offset, byte value) {
            try {
                return Read(arduino, offset) == value;
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
        /// <returns><see langword="true"/> if bytes at <paramref name="offset"/> matches <paramref name="value"/></returns>
        public static bool Verify(Arduino arduino, ushort offset, byte[] value) {
            try {
                byte[] source = Read(arduino, offset, (byte)value.Length);

                return Data.CompareArray(source, value);
            }
            catch {
                throw new Exception($"Unable to verify bytes # 0x{offset:X4}-0x{offset + value.Length:X4} at {arduino.PortName}:{arduino.I2CAddress}");
            }
        }

        /// <summary>
        /// Performs write protection test on the specified EEPROM offset
        /// </summary>
        /// <param name="arduino">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <returns><see langword="true"/> if byte at <paramref name="offset"/> is writable</returns>
        public static bool WriteTest(Arduino arduino, ushort offset) {
            try {
                return arduino.ExecuteCommand<bool>(
                    Arduino.Command.WriteTest,
                    arduino.I2CAddress,
                    (byte)(offset >> 8), // MSB
                    (byte)offset);       // LSB
            }
            catch {
                throw new Exception($"Unable to perform offset # 0x{offset:X4} write test at {arduino.PortName}:{arduino.I2CAddress}");
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
                return arduino.ExecuteCommand<bool>(Arduino.Command.Rswp, arduino.I2CAddress, block, true);
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
                return arduino.ExecuteCommand<bool>(Arduino.Command.Rswp, arduino.I2CAddress, block, Arduino.Command.Get);
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
                return arduino.ExecuteCommand<bool>(Arduino.Command.Rswp, arduino.I2CAddress, 0, false);
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
                return arduino.ExecuteCommand<bool>(Arduino.Command.Pswp, arduino.I2CAddress, true);
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
                return arduino.ExecuteCommand<bool>(Arduino.Command.Pswp, arduino.I2CAddress, Arduino.Command.Get);
            }
            catch {
                throw new Exception($"Unable to get PSWP status on {arduino.PortName}");
            }
        }

        /// <summary>
        /// Validates offset
        /// </summary>
        /// <param name="arduino">Device instance</param>
        /// <param name="offset">Byte position</param>
        /// <returns><see langword="true"/> if <paramref name="offset"/> is within <paramref name="arduino"/>'s EEPROM</returns>
        private static bool CheckOffset(Arduino arduino, ushort offset) {
            return offset <= arduino.MaxSpdSize;
        }
        
        /// <summary>
        /// Validates page size
        /// </summary>
        /// <param name="value">Page contents</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> length is within page size</returns>
        private static bool CheckPageSize(byte[] value) {
            return 0 < value.Length && value.Length <= 16;
        }

        #endregion

        /// <summary>
        /// EEPROM or SPD5 hub page number
        /// </summary>
        private static byte PageNumber { get; set; }

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
            public const byte LocalHid = 0b111;

            // I2C Legacy Mode Device Configuration
            public const byte MR11     = 11;

            // Write Protection For NVM Blocks [7:0]
            public const byte MR12     = 12;

            // Write Protection For NVM Blocks [15:8]
            public const byte MR13     = 13;

            // Device Status
            public const byte MR48     = 48;
        }

        /// <summary>
        /// Device LID Codes
        /// </summary>
        internal struct LidCode {
            // EEPROM & SPD5 hub
            internal const byte Eeprom = 0b1010;

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
        public static bool ValidateEepromAddress(int address) {
            return address >> 3 == LidCode.Eeprom;
        }

        /// <summary>
        /// Checks if input address is a valid PMIC address
        /// </summary>
        /// <param name="address">Input address</param>
        /// <returns><see langword="true"/> if <paramref name="address"/> is a valid PMIC address</returns>
        public static bool ValidatePmicAddress(int address) {
            return address >> 3 == LidCode.Pmic0 ||
                   address >> 3 == LidCode.Pmic1 ||
                   address >> 3 == LidCode.Pmic2;
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