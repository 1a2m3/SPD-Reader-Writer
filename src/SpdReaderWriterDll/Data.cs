using System;
using UInt8 = System.Byte;

namespace SpdReaderWriterDll {

    /// <summary>
    /// Data class
    /// </summary>
    public class Data {

        /// <summary>
        /// Calculates CRC16/XMODEM checksum
        /// </summary>
        /// <param name="input">A byte array to be checked</param>
        /// <param name="poly">Polynomial value</param>
        /// <returns>A calculated checksum</returns>
        public static UInt16 Crc16(byte[] input, UInt16 poly) {
            UInt16[] table = new UInt16[256];
            UInt16   crc   = 0;

            for (int i = 0; i < table.Length; ++i) {

                UInt16 temp = 0;
                UInt16 a = (UInt16)(i << 8);

                for (UInt8 j = 0; j < 8; ++j) {
                    temp = (UInt16)(((temp ^ a) & 0x8000) != 0 ? (temp << 1) ^ poly : temp << 1);
                    a <<= 1;
                }

                table[i] = temp;
            }

            for (int i = 0; i < input.Length; ++i) {
                crc = (UInt16)((crc << 8) ^ table[(crc >> 8) ^ (0xFF & input[i])]);
            }

            return crc;
        }

        /// <summary>
        /// Calculates CRC8 checksum
        /// </summary>
        /// <param name="input">A byte array to be checked</param>
        /// <returns>A calculated checksum</returns>
        public static UInt16 Crc(byte[] input) {
            UInt16 crc = 0;

            for (int i = 0; i < input.Length; i++) {
                crc += input[i];
            }

            return crc;
        }

        /// <summary>
        /// Gets bit value specified at position from a byte
        /// </summary>
        /// <param name="input">Input byte to get bit value from</param>
        /// <param name="position">Bit position from 0 (LSB) to 7 (MSB)</param>
        /// <returns><see langword="true" /> if bit is set to 1 at <paramref name="position" /></returns>
        public static bool GetBit(byte input, UInt8 position) {

            if (position > 7) {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return ((input >> position) & 1) == 1;
        }

        /// <summary>
        /// Gets bit values from a byte at specified offset position
        /// </summary>
        /// <param name="input">Input byte to get a bit value from</param>
        /// <param name="position">Bit position from 0 (LSB) to 7 (MSB)</param>
        /// <param name="count">The number of bits to read</param>
        /// <returns>An array of bit values</returns>
        public static byte[] GetBits(byte input, UInt8 position, UInt8 count) {

            if (count < 1) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count > 8 || position > 7 || count > position + 1) {
                return new byte[] { 0 };
            }

            byte[] bits = new byte[count];

            for (int i = 0; i < bits.Length; i++) {
                bits[i] = (byte)(GetBit(input, (byte)(position - i)) ? 1 : 0);
            }

            return bits;
        }

        /// <summary>
        /// Sets specified bit in a byte at specified offset position
        /// </summary>
        /// <param name="input">Input byte to set bit in</param>
        /// <param name="position">Bit position to set</param>
        /// <param name="value">Boolean bit value, set <see langref="true" /> for 1, or <see langword="false" /> for 0</param>
        public static byte SetBit(byte input, UInt8 position, bool value) {

            if (position > 7) {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return (byte)(value ? input | (1 << position) : input & ~(1 << position));
        }


        /// <summary>
        /// Gets number of bits from input byte at position and converts them to a new byte
        /// </summary>
        /// <param name="input">Input byte to get bits from</param>
        /// <param name="position">Bit position from 0 (LSB) to 7 (MSB)</param>
        /// <returns>Byte matching bit pattern at <paramref name="input" /> position of all bits</returns>
        public static byte GetByteFromBits(byte input, UInt8 position) {
            return GetByteFromBits(input, position, (byte)(position + 1));
        }

        /// <summary>
        /// Gets number of bits from input byte at position and converts them to a new byte
        /// </summary>
        /// <param name="input">Input byte to get bits from</param>
        /// <param name="position">Bit position from 0 (LSB) to 7 (MSB)</param>
        /// <param name="count">The number of bits to read to the right of <paramref name="position" /> </param>
        /// <returns>Byte matching bit pattern at <paramref name="input" /> position of <paramref name="count" /> bits</returns>
        public static byte GetByteFromBits(byte input, UInt8 position, UInt8 count) {

            if (count < 1) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // Generate bit mask
            byte mask = (byte)(Math.Pow(2, count) - 1);

            // Calculate shift position for the input
            byte shift = (byte)(position - count + 1);

            // Bitwise AND shifted input and mask
            return (byte)((input >> shift) & mask);
        }

        /// <summary>
        /// Converts boolean type to UInt8
        /// </summary>
        /// <param name="input">Boolean input</param>
        /// <returns>1 if the input is <see langword="true" />, or 0, when the input is <see langword="false" /></returns>
        public static UInt8 BoolToInt(bool input) {
            return (UInt8)(input ? 1 : 0);
        }

        /// <summary>
        /// Determine if a string contains a case insensitive given substring
        /// </summary>
        /// <param name="inputString">The string to search in</param>
        /// <param name="substring">The substring to search for in the <paramref name="inputString" /></param>
        /// <returns><see langword="true" /> if <paramref name="substring" /> is part of <paramref name="inputString" /></returns>
        public static bool StringContains(string inputString, string substring) {
            return inputString.IndexOf(substring, 0, StringComparison.CurrentCultureIgnoreCase) != -1;
        }
    }
}