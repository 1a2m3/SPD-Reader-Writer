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
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace SpdReaderWriterDll {

    /// <summary>
    /// Data class which works with bytes, bits, streams, and other types of data
    /// </summary>
    public class Data {

        /// <summary>
        /// Calculates CRC16 checksum
        /// </summary>
        /// <param name="input">A byte array to be checked</param>
        /// <param name="poly">Polynomial value</param>
        /// <returns>A calculated checksum</returns>
        public static ushort Crc16(byte[] input, ushort poly) {

            ushort crc = 0;

            for (ushort i = 0; i < input.Length; i++) {

                crc ^= (ushort)(input[i] << 8);

                for (byte j = 0; j < 8; j++) {
                    crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ poly : crc << 1);
                }
            }

            return crc;
        }

        /// <summary>
        /// Calculates CRC8 checksum
        /// </summary>
        /// <param name="input">A byte array to be checked</param>
        /// <returns>A calculated checksum</returns>
        public static byte Crc(byte[] input) {

            byte crc = 0;

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
            ulong value  = Convert.ToUInt64(input) & (ulong)(Math.Pow(2, bitCount) - 1);

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
        public static bool GetBit(object input, byte position) {

            int bitCount = Marshal.SizeOf(input) * 8;
            ulong value  = Convert.ToUInt64(input) & (ulong)(Math.Pow(2, bitCount) - 1);

            return ((value >> position) & 1) == 1;
        }

        /// <summary>
        /// Gets bit values from a byte at specified offset position
        /// </summary>
        /// <param name="input">Input byte to get a bit value from</param>
        /// <param name="position">Bit position from 0 (LSB) to 7 (MSB)</param>
        /// <param name="count">The number of bits to read</param>
        /// <returns>An array of bit values</returns>
        public static byte[] GetBits(byte input, byte position, byte count) {

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
        public static byte SetBit(byte input, byte position, bool value) {

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
        public static byte SubByte(byte input, byte position) {
            return SubByte(input, position, (byte)(position + 1));
        }

        /// <summary>
        /// Gets number of bits from input byte at position and converts them to a new byte
        /// </summary>
        /// <param name="input">Input byte to get bits from</param>
        /// <param name="position">Bit position from 0 (LSB) to 7 (MSB)</param>
        /// <param name="count">The number of bits to read to the right of <paramref name="position"/> </param>
        /// <returns>Byte matching bit pattern at <paramref name="input"/> position of <paramref name="count"/> bits</returns>
        public static byte SubByte(byte input, byte position, byte count) {

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
        /// Converts boolean type to byte
        /// </summary>
        /// <param name="input">Boolean input</param>
        /// <returns>1 if the input is <see langword="true"/>, or 0, when the input is <see langword="false"/></returns>
        public static byte BoolToNum(bool input) {
            return (byte)(input ? 1 : 0);
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
                int.Parse(input, System.Globalization.NumberStyles.AllowHexSpecifier);
                return true;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Determines if input char is a HEX
        /// </summary>
        /// <param name="input">Input char to validate</param>
        /// <returns><see langword="true"/> if <paramref name="input"/> is in a HEX format</returns>
        public static bool ValidateHex(char input) {

            return ('A' <= char.ToUpper(input) && char.ToUpper(input) <= 'F') ||
                   ('0' <= input && input <= '9');
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
        /// Compresses or decompresses Gzip data
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="method">Gzip method</param>
        /// <returns>Data byte array</returns>
        public static byte[] Gzip(byte[] input, GzipMethod method) {
            return method == GzipMethod.Compress ? CompressGzip(input) : DecompressGzip(input);
        }

        /// <summary>
        /// Gzip Data Methods
        /// </summary>
        public enum GzipMethod {
            Compress,
            Decompress
        }

        /// <summary>
        /// Allows to extract compressed contents header
        /// </summary>
        /// <param name="input">GZip contents byte array</param>
        /// <param name="outputSize">Number of bytes to read</param>
        /// <returns>Decompressed header byte array</returns>
        public static byte[] GzipPeek(byte[] input, int outputSize) {
            return DecompressGzip(input, outputSize, true);
        }

        /// <summary>
        /// Compresses contents to GZip
        /// </summary>
        /// <param name="input">Contents byte array</param>
        /// <returns>Compressed byte array</returns>
        private static byte[] CompressGzip(byte[] input) {

            using (MemoryStream inputStream = new MemoryStream(input), outputStream = new MemoryStream()) {
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

        /// <summary>
        /// Decompresses GZip contents
        /// </summary>
        /// <param name="input">GZip contents byte array</param>
        /// <returns>Decompressed byte array</returns>
        private static byte[] DecompressGzip(byte[] input) {
            return DecompressGzip(input, 16384, false);
        }

        /// <summary>
        /// Decompresses GZip contents
        /// </summary>
        /// <param name="input">GZip contents byte array</param>
        /// <param name="bufferSize">Buffer size</param>
        /// <param name="peek">When set to <see langword="true"/> to read header only, or <see langword="false"/> to get full data</param>
        /// <returns>Decompressed byte array</returns>
        private static byte[] DecompressGzip(byte[] input, int bufferSize, bool peek) {

            using (MemoryStream outputStream = new MemoryStream(), inputStream = new MemoryStream(input)) {
                using (GZipStream zipStream = new GZipStream(inputStream, CompressionMode.Decompress)) {

                    byte[] buffer = new byte[bufferSize];
                    int count;

                    do {
                        count = zipStream.Read(buffer, 0, buffer.Length);
                        if (count > 0) {
                            outputStream.Write(buffer, 0, count);
                            if (peek) {
                                break;
                            }
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
            return Encoding.Default.GetString(input).Trim();
        }

        /// <summary>
        /// Converts char array to string
        /// </summary>
        /// <param name="input">Input char array</param>
        /// <returns>Text string from <paramref name="input"/></returns>
        public static string BytesToString(char[] input) {

            StringBuilder sbOutput = new StringBuilder();

            // Process ASCII printable characters only
            foreach (char c in input) {
                sbOutput.Append(IsAscii(c) ? c.ToString() : "");
            }

            return sbOutput.ToString();
        }

        /// <summary>
        /// Checks if input character is within ASCII range
        /// </summary>
        /// <param name="input">Input character</param>
        /// <returns><see langword="true"/> if <see cref="input"/> is within ASCII range</returns>
        public static bool IsAscii(char input) {
            return 0x20 <= input && input <= 0x7E;
        }

        /// <summary>
        /// Checks if input byte value is within ASCII range
        /// </summary>
        /// <param name="input">Input byte</param>
        /// <returns><see langword="true"/> if <see cref="input"/> value is within ASCII range</returns>
        public static bool IsAscii(byte input) {
            return IsAscii((char)input);
        }

        /// <summary>
        /// Converts byte to a Binary Coded Decimal (BCD)
        /// </summary>
        /// <param name="input">Input byte</param>
        /// <returns>Binary Coded Decimal</returns>
        /// <example>0x38 is converted to 38</example>
        public static byte ByteToBinaryCodedDecimal(byte input) {
            return (byte)((input & 0x0F) + ((input >> 4) & 0x0F) * 10);
        }

        /// <summary>
        /// Converts Binary Coded Decimal (BCD) to a byte
        /// </summary>
        /// <param name="input">Binary Coded Decimal</param>
        /// <returns>Binary Coded Decimal Byte</returns>
        /// <example>14 is converted to 0x14</example>
        public static byte BinaryCodedDecimalToByte(byte input) {

            if (input > 99) {
                throw new ArgumentOutOfRangeException(nameof(input));
            }

            byte tens = (byte)(input / 10);
            byte ones = (byte)(input - tens * 10);

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

        /// <summary>
        /// Returns first index of matching array bytes in the source array
        /// </summary>
        /// <param name="source">Source array</param>
        /// <param name="pattern">Matching pattern</param>
        /// <returns>First index of matching array bytes</returns>
        public static int FindArray(byte[] source, byte[] pattern) {
            
            int maxFirstCharSlot = source.Length - pattern.Length + 1;
            
            for (int i = 0; i < maxFirstCharSlot; i++) {
                // Compare only first byte
                if (source[i] != pattern[0]) {
                    continue;
                }

                // First byte match found, now try to match the rest of the pattern in reverse
                for (int j = pattern.Length - 1; j >= 1; j--) {
                    if (source[i + j] != pattern[j]) {
                        break;
                    }
                    if (j == 1) {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets description attribute of an Enum member
        /// </summary>
        /// <param name="e">Enum member</param>
        /// <returns>Enum member description or name, if description attribute is missing</returns>
        public static string GetEnumDescription(Enum e) {

            string name = e.ToString();
            object[] descriptionAttributes = e.GetType().GetMember(name)[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

            return descriptionAttributes.Length <= 0 ? name : ((DescriptionAttribute)descriptionAttributes[0]).Description;
        }

        /// <summary>
        /// Indicates whether the value of input integer object is an even number
        /// </summary>
        /// <param name="input">Input integer</param>
        /// <returns><see langword="true"/> if <param name="input"/> is an even number, or <see langword="false"/> if it is an odd number</returns>
        public static bool IsEven(int input) {
            return (input & 1) == 0;
        }

        /// <summary>
        /// Indicates whether the value of input integer object is an odd number
        /// </summary>
        /// <param name="input">Input integer</param>
        /// <returns><see langword="true"/> if <param name="input"/> is an odd number, or <see langword="false"/> if it is an even number</returns>
        public static bool IsOdd(int input) {
            return (input & 1) == 1;
        }

        /// <summary>
        /// Return the closest even integer that is greater than or equal to <see cref="input"/>
        /// </summary>
        /// <param name="input">Input integer</param>
        /// <returns>Closest even number that is greater than or equal to <see cref="input"/></returns>
        public static int EvenUp(int input) {
            return IsEven(input) ? input : input + 1;
        }

        /// <summary>
        /// Compares two byte arrays
        /// </summary>
        /// <param name="a1">First byte array</param>
        /// <param name="b1">Second byte array</param>
        /// <returns><see langword="true"/> if both arrays are equal</returns>
        public static bool CompareByteArray(byte[] a1, byte[] b1) {

            if (a1 == b1) {
                return true;
            }

            if (a1?.Length == b1?.Length) {
                int i = 0;
                while (i < a1.Length && a1[i] == b1[i]) {
                    i++;
                }
                if (i == a1.Length) {
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Trims byte array
        /// </summary>
        /// <param name="input">Input byte array</param>
        /// <param name="newSize">New byte array size</param>
        /// <param name="trimPosition">Trim position</param>
        /// <returns>Trimmed byte array</returns>
        public static byte[] TrimByteArray(byte[] input, int newSize, TrimPosition trimPosition) {

            if (input == null) {
                throw new ArgumentNullException(nameof(input));
            }

            if (newSize == input.Length) {
                return input;
            }

            if (newSize > input.Length) {
                throw new IndexOutOfRangeException($"{nameof(newSize)} cannot be greater than {nameof(input)} length");
            }

            byte[] newArray = new byte[newSize];

            if (trimPosition == TrimPosition.End) {
                Array.Copy(
                    sourceArray      : input, 
                    destinationArray : newArray, 
                    length           : newSize);
            }
            else {
                Array.Copy(
                    sourceArray      : input,
                    sourceIndex      : input.Length - newSize,
                    destinationArray : newArray,
                    destinationIndex : 0,
                    length           : newSize);
            }

            return newArray;
        }

        /// <summary>
        /// Array trim position
        /// </summary>
        public enum TrimPosition {
            Start,
            End
        }
    }
}