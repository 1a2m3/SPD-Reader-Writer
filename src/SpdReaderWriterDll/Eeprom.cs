using System;
using UInt8   = System.Byte;

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
        /// <returns>Byte value at <paramref name="offset"/> </returns>
        public static byte ReadByte(Device device, UInt16 offset) {
            return device.ExecuteCommand($"{Command.READBYTE} {device.I2CAddress} {offset}");
        }

        /// <summary>
        /// Reads bytes from the EEPROM
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position to start reading from</param>
        /// <param name="count">Total number of bytes to read from <paramref name="offset" /> </param>
        /// <returns>A byte array containing byte values</returns>
        public static byte[] ReadByte(Device device, UInt16 offset, int count) {
            
            byte[] output = new byte[count];

            for (UInt16 i = 0; i < count; i++) {
                output[i] = ReadByte(device, (UInt16)(i + offset));
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
        public static bool WriteByte(Device device, UInt16 offset, byte value) {
            return device.ExecuteCommand($"{Command.WRITEBYTE} {device.I2CAddress} {offset} {value}") == Response.SUCCESS;
        }

        /// <summary>
        /// Write a byte to the EEPROM. The byte is written only if it's value differs from the one already saved at the same address.
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true" /> if byte read at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool UpdateByte(Device device, UInt16 offset, byte value) {
            return VerifyByte(device, offset, value) || WriteByte(device, offset, value);
        }

        /// <summary>
        /// Verifies if the offset content matches the input specified
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="offset">Byte position</param>
        /// <param name="value">Byte value</param>
        /// <returns><see langword="true" /> if byte at <paramref name="offset"/> matches <paramref name="value"/> value</returns>
        public static bool VerifyByte(Device device, UInt16 offset, byte value) {
            return ReadByte(device, offset) == value;
        }

        /// <summary>
        /// Enables software write protection on the specified EEPROM block 
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="block">Block number to be write protected</param>
        /// <returns><see langword="true" /> when the write protection has been enabled on block <paramref name="block"/> </returns>
        public static bool SetWriteProtection(Device device, int block) {
            return device.ExecuteCommand($"{Command.SETREVERSIBLESWP} {block}") == Response.SUCCESS; 
        }

        /// <summary>
        /// Enables software write protection on all 4 EEPROM blocks
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <returns><see langword="true" /> when the write protection has been enabled on all available blocks</returns>
        public static bool SetWriteProtection(Device device) {

            for (int i = 0; i <= 3; i++) {
                if (!SetWriteProtection(device, i)) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Read software write protection status
        /// </summary>
        /// <param name="device">SPD reader/writer device instance</param>
        /// <param name="block">Block number to be checked</param>
        /// <returns><see langword="true" /> when the block is writable, or <see langword="false" /> if the block is write protected or if RSWP is not supported</returns>
        public static bool IsWriteProtectionEnabled(Device device, int block) {
            return device.ExecuteCommand($"{Command.GETREVERSIBLESWP} {block}") == Response.ACK;
        }

        /// <summary>
        /// Clears EEPROM write protection 
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> if the write protection has been disabled</returns>
        public static bool ClearWriteProtection(Device device) {
            return device.ExecuteCommand($"{Command.CLEARSWP}") == Response.SUCCESS;
        }

        /// <summary>
        /// Sets permanent write protection on supported EEPROMs
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> when the permanent write protection is enabled</returns>
        public static bool SetPermanentWriteProtection(Device device) {
            return device.ExecuteCommand($"{Command.SETPERMANENTSWP} {device.I2CAddress}") == Response.SUCCESS; 
        }

        /// <summary>
        /// Tests if EEPROM is writable or permanently protected
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns><see langword="true" /> if when PSWP has NOT been set and EEPROM is fully writable or <see langword="false" /> when PSWP is enabled</returns>
        public static bool IsPermanentWriteProtectionEnabled(Device device) {
            return device.ExecuteCommand($"{Command.GETPSWP} {device.I2CAddress}") == Response.NACK;
        }
    }
}
