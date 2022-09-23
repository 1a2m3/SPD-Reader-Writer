/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using UInt8 = System.Byte;

namespace SpdReaderWriterDll {

    /// <summary>
    /// Data class which works with bytes, bits, streams, and other types of data
    /// </summary>
    public class Data {

        /// <summary>
        /// Calculates CRC16/XMODEM checksum
        /// </summary>
        /// <param name="input">A byte array to be checked</param>
        /// <param name="poly">Polynomial value</param>
        /// <returns>A calculated checksum</returns>
        public static UInt16 Crc16(byte[] input, UInt16 poly) {

            UInt16 crc = 0;

            for (UInt16 i = 0; i < input.Length; i++) {

                crc ^= (UInt16)(input[i] << 8);

                for (UInt8 j = 0; j < 8; j++) {
                    crc = (UInt16)((crc & 0x8000) != 0 ? (crc << 1) ^ poly : crc << 1);
                }
            }

            return crc;
        }

        /// <summary>
        /// Calculates CRC8 checksum
        /// </summary>
        /// <param name="input">A byte array to be checked</param>
        /// <returns>A calculated checksum</returns>
        public static UInt8 Crc(byte[] input) {

            UInt8 crc = 0;

            foreach (byte b in input) {
                crc += b;
            }

            return crc;
        }

        /// <summary>
        /// Calculates parity bit
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="parityType">Parity type</param>
        /// <returns>Parity bit</returns>
        public static byte GetParity(object input, Parity parityType) {

            int bitCount = Marshal.SizeOf(input) * 8;
            UInt64 value = Convert.ToUInt64(input) & (UInt64)(Math.Pow(2, bitCount) - 1);

            byte result = 0;

            for (int i = 0; i < bitCount; i++) {
                result ^= (byte)((value >> i) & 0x01);
            }

            return (byte)(result ^ (~(byte)parityType & 0x01));
        }

        /// <summary>
        /// Parity type
        /// </summary>
        public enum Parity : byte {
            Odd  = 0,
            Even = 1
        }

        /// <summary>
        /// Gets bit value specified at position from a byte
        /// </summary>
        /// <param name="input">Input byte to get bit value from</param>
        /// <param name="position">Bit position from 0 (LSB) to 7 (MSB)</param>
        /// <returns><see langword="true"/> if bit is set to 1 at <paramref name="position"/></returns>
        public static bool GetBit(object input, UInt8 position) {

            int bitCount = Marshal.SizeOf(input) * 8;
            UInt64 value = Convert.ToUInt64(input) & (UInt64)(Math.Pow(2, bitCount) - 1);

            return ((value >> position) & 1) == 1;
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
        /// <param name="value">Boolean bit value, set <see langref="true"/> for 1, or <see langword="false"/> for 0</param>
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
        /// <returns>Byte matching bit pattern at <paramref name="input"/> position of all bits</returns>
        public static byte SubByte(byte input, UInt8 position) {
            return SubByte(input, position, (byte)(position + 1));
        }

        /// <summary>
        /// Gets number of bits from input byte at position and converts them to a new byte
        /// </summary>
        /// <param name="input">Input byte to get bits from</param>
        /// <param name="position">Bit position from 0 (LSB) to 7 (MSB)</param>
        /// <param name="count">The number of bits to read to the right of <paramref name="position"/> </param>
        /// <returns>Byte matching bit pattern at <paramref name="input"/> position of <paramref name="count"/> bits</returns>
        public static byte SubByte(byte input, UInt8 position, UInt8 count) {

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
        /// <returns>1 if the input is <see langword="true"/>, or 0, when the input is <see langword="false"/></returns>
        public static UInt8 BoolToNum(bool input) {
            return (UInt8)(input ? 1 : 0);
        }

        /// <summary>
        /// Determines if a string contains a case insensitive given substring
        /// </summary>
        /// <param name="inputString">The string to search in</param>
        /// <param name="substring">The substring to search for in the <paramref name="inputString"/></param>
        /// <returns><see langword="true"/> if <paramref name="substring"/> is part of <paramref name="inputString"/></returns>
        public static bool StringContains(string inputString, string substring) {
            return inputString.IndexOf(substring, 0, StringComparison.CurrentCultureIgnoreCase) != -1;
        }

        /// <summary>
        /// Determines if input string contains HEX values (0-9,A-F)
        /// </summary>
        /// <param name="input">Input string to validate</param>
        /// <returns><see langword="true"/> if <paramref name="input"/> is in a HEX format</returns>
        public static bool ValidateHex(string input) {
            try {
                uint.Parse(input, System.Globalization.NumberStyles.AllowHexSpecifier);
                return true;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Converts hex string to byte
        /// </summary>
        /// <param name="input">Hex string</param>
        /// <returns>Byte value of input hex string</returns>
        public static byte HexStringToByte(string input) {
            return Convert.ToByte(input, 16);
        }

        /// <summary>
        /// Compresses contents to GZip
        /// </summary>
        /// <param name="input">Contents byte array</param>
        /// <returns>Compressed byte array</returns>
        public static byte[] CompressGzip(byte[] input) {
            
            using (MemoryStream inputStream = new MemoryStream(input)) {
                using (MemoryStream outputStream = new MemoryStream()) {
                    using (GZipStream zipStream = new GZipStream(outputStream, CompressionMode.Compress)) {

                        byte[] buffer = new byte[16384];
                        int count;

                        do {
                            count = inputStream.Read(buffer, 0, buffer.Length);
                            zipStream.Write(buffer, 0, count);

                        } while (count > 0);
                    }

                    return outputStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Decompresses GZip contents
        /// </summary>
        /// <param name="input">GZip contents byte array</param>
        /// <returns>Decompressed byte array</returns>
        public static byte[] DecompressGzip(byte[] input) {

            using (MemoryStream outputStream = new MemoryStream()) {
                using (GZipStream zipStream = new GZipStream(new MemoryStream(input), CompressionMode.Decompress)) {

                    byte[] buffer = new byte[16384];
                    int count;

                    do {
                        count = zipStream.Read(buffer, 0, buffer.Length);
                        if (count > 0) {
                            outputStream.Write(buffer, 0, count);
                        }
                    } while (count > 0);
                }

                return outputStream.ToArray();
            }
        }

        /// <summary>
        /// Converts byte array to string
        /// </summary>
        /// <param name="input">Input byte array</param>
        /// <returns>Text string from <paramref name="input"/></returns>
        public static string BytesToString(byte[] input) {
            return System.Text.Encoding.Default.GetString(input).Trim();
        }

        /// <summary>
        /// Converts char array to string
        /// </summary>
        /// <param name="input">Input char array</param>
        /// <returns>Text string from <paramref name="input"/></returns>
        public static string BytesToString(char[] input) {

            string output = "";

            // Process ASCII printable characters only
            foreach (char b in input) {
                if (0x20 <= b && b <= 0x7E) {
                    output += b.ToString();
                }
            }

            return output;
        }

        /// <summary>
        /// Converts byte to a Binary Coded Decimal (BCD)
        /// </summary>
        /// <param name="input">Input byte</param>
        /// <returns>Binary Coded Decimal</returns>
        /// <example>0x38 is converted to 38</example>
        public static UInt8 ByteToBinaryCodedDecimal(byte input) {
            return (UInt8)((input & 0x0F) + ((input >> 4) & 0x0F) * 10);
        }

        /// <summary>
        /// Converts Binary Coded Decimal (BCD) to a byte
        /// </summary>
        /// <param name="input">Binary Coded Decimal</param>
        /// <returns>Binary Coded Decimal Byte</returns>
        /// <example>14 is converted to 0x14</example>
        public static byte BinaryCodedDecimalToByte(UInt8 input) {
            if (input > 99) {
                throw new ArgumentOutOfRangeException(nameof(input));
            }

            UInt8 tens = (UInt8)(input / 10);
            UInt8 ones = (UInt8)(input - tens * 10);

            return (byte)((ones & 0xF) | (tens << 4));
        }

        /// <summary>
        /// Returns a consecutive array of bytes based on input criteria
        /// </summary>
        /// <param name="start">First number in array</param>
        /// <param name="stop">Last number in array</param>
        /// <param name="step">Number interval</param>
        /// <returns>A consecutive array of bytes starting from <see cref="start"/> till <see cref="stop"/> with an interval of <see cref="step"/></returns>
        public static byte[] ConsecutiveArray(byte start, byte stop, byte step) {

            Queue<byte> numbers = new Queue<byte>();

            int i = start;
            do {
                numbers.Enqueue((byte)i);
                i += step;
            } while (i <= stop);
            
            return numbers.ToArray();
        }

        /// <summary>
        /// Returns a consecutive array of words based on input criteria
        /// </summary>
        /// <param name="start">First number in array</param>
        /// <param name="stop">Last number in array</param>
        /// <param name="step">Number interval</param>
        /// <returns>A consecutive array of words starting from <see cref="start"/> till <see cref="stop"/> with an interval of <see cref="step"/></returns>
        public static short[] ConsecutiveArray(short start, short stop, short step) {

            Queue<short> numbers = new Queue<short>();

            int i = start;
            do {
                numbers.Enqueue((short)i);
                i += step;
            } while (i <= stop);

            return numbers.ToArray();
        }
    }
}