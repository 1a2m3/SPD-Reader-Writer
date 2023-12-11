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
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SpdReaderWriterCore {

    /// <summary>
    /// Data class which works with bytes, bits, strings, streams, and other types of data
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

            foreach (byte b in input) {
                crc ^= (ushort)(b << 8);
                for (byte i = 0; i < 8; i++) {
                    crc = (ushort)((crc << 1) ^ (GetBit(crc, 15) ? poly : 0));
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

            int bitCount = CountBits(input);
            ulong value  = Convert.ToUInt64(input) & GenerateBitmask<ulong>(bitCount);

            byte result = 0;

            for (int i = 0; i < bitCount; i++) {
                result ^= (byte)((value >> i) & 1);
            }

            return (byte)(result ^ (~(byte)parityType & 1));
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
        public static bool GetBit(object input, int position) {

            if (!IsNumeric(input)) {
                throw new InvalidDataException(nameof(input));
            }

            long value = Convert.ToInt64(input) & GenerateBitmask<long>(CountBits(input));

            return ((value >> position) & 1) == 1;
        }

        /// <summary>
        /// Sets specified bit in a byte at specified offset position
        /// </summary>
        /// <typeparam name="T">Input data type</typeparam>
        /// <param name="input">Input data to set bit in</param>
        /// <param name="position">Bit position to set</param>
        /// <param name="value">Boolean bit value, set <see langword="true"/> for <value>1</value>, or <see langword="false"/> for <value>0</value></param>
        /// <returns>Updated data value</returns>
        public static T SetBit<T>(T input, int position, bool value) {

            if (!IsNumeric(input)) {
                throw new InvalidDataException(nameof(input));
            }

            if (position > CountBits(input)) {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            int mask = 1 << position;
            ulong inputData = Convert.ToUInt64((T)Convert.ChangeType(input, typeof(T)));

            return (T)Convert.ChangeType(
                value
                    ? inputData | (uint)mask
                    : inputData & (ulong)~mask
                , typeof(T));
        }


        /// <summary>
        /// Gets bits from input data at specified position and converts them to a new value of the same type
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="input">Input data to get bits from</param>
        /// <param name="position">Bit position</param>
        /// <returns>Value matching bit pattern starting at <paramref name="input"/> position till LSB</returns>
        public static T SubByte<T>(T input, int position) {

            if (!IsNumeric(input)) {
                throw new InvalidDataException(nameof(input));
            }

            return SubByte(input, position, position + 1);
        }

        /// <summary>
        /// Gets bits from input data at specified position and converts them to a new value of the same type
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="input">Input data to get bits from</param>
        /// <param name="position">Bit position</param>
        /// <param name="count">The number of bits to read to the right of <paramref name="position"/> </param>
        /// <returns>Value matching bit pattern at <paramref name="input"/> position of <paramref name="count"/> bits</returns>
        public static T SubByte<T>(T input, int position, int count) {

            if (input == null) {
                throw new ArgumentNullException(nameof(input));
            }

            if (!IsNumeric(input)) {
                throw new InvalidDataException(nameof(input));
            }

            if (count < 1) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (position > CountBits(input)) {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            if (position + 1 < count) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // Convert input
            object inputData = Convert.ChangeType(input, typeof(T));

            // Generate bit mask
            object mask = Convert.ChangeType(GenerateBitmask<T>(count), typeof(T));

            // Calculate shift position for the input
            int shift = position - count + 1;

            // Bitwise AND shifted input and mask
            object result = null;

            if (typeof(T) == typeof(byte)) {
                result = ((byte)inputData >> shift) & (byte)mask;
            }
            else if (typeof(T) == typeof(sbyte)) {
                result = ((sbyte)inputData >> shift) & (sbyte)mask;
            }
            else if (typeof(T) == typeof(short)) {
                result = ((short)inputData >> shift) & (short)mask;
            }
            else if (typeof(T) == typeof(ushort)) {
                result = ((ushort)inputData >> shift) & (ushort)mask;
            }
            else if (typeof(T) == typeof(int)) {
                result = ((int)inputData >> shift) & (int)mask;
            }
            else if (typeof(T) == typeof(uint)) {
                result = ((uint)inputData >> shift) & (uint)mask;
            }
            else if (typeof(T) == typeof(long)) {
                result = ((long)inputData >> shift) & (long)mask;
            }
            else if (typeof(T) == typeof(ulong)) {
                result = ((ulong)inputData >> shift) & (ulong)mask;
            }

            if (result != null) {
                return (T)Convert.ChangeType(result, typeof(T));
            }

            throw new InvalidDataException($"{MethodBase.GetCurrentMethod()?.Name}:{typeof(T)}");
        }

        /// <summary>
        /// Counts number of bits in input data
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="input">Input data</param>
        /// <returns>Number of bits in input data</returns>
        public static int CountBits<T>(T input) {

            if (!IsNumeric(input)) {
                throw new InvalidDataException(nameof(input));
            }

            try {
                return Marshal.SizeOf(input) * 8;
            }
            catch {
                return 0;
            }
        }

        /// <summary>
        /// Counts number of bytes in input type 
        /// </summary>
        /// <param name="input">Input type</param>
        /// <returns>Number of bits in input type</returns>
        public static int CountBytes(Type input) {

            if (input == typeof(bool)) {
                return 1;
            }

            return Marshal.SizeOf(input);
        }

        /// <summary>
        /// Generates bitmask
        /// </summary>
        /// <param name="count">Number of bits</param>
        /// <returns>Bitmask with a number of bits specified in <paramref name="count"/> parameter</returns>
        public static T GenerateBitmask<T>(int count) {
            return (T)Convert.ChangeType(Math.Pow(2, count) - 1, typeof(T));
        }

        /// <summary>
        /// Converts boolean value to a number
        /// </summary>
        /// <param name="input">Boolean input</param>
        /// <returns><value>1</value> if the input is <see langword="true"/>, or <value>0</value>, when the input is <see langword="false"/></returns>
        public static T BoolToNum<T>(bool input) {
            return (T)Convert.ChangeType(input ? 1 : 0, typeof(T));
        }

        /// <summary>
        /// Indicates whether the value of input number is an even number
        /// </summary>
        /// <param name="input">Input integer</param>
        /// <returns><see langword="true"/> if <param name="input"/> is an even number, or <see langword="false"/> if it is an odd number</returns>
        public static bool IsEven<T>(T input) {

            if (!IsNumeric(input)) {
                throw new InvalidDataException(nameof(input));
            }

            return !GetBit(input, 0);
        }

        /// <summary>
        /// Indicates whether the value of input number is an odd number
        /// </summary>
        /// <param name="input">Input integer</param>
        /// <returns><see langword="true"/> if <param name="input"/> is an odd number, or <see langword="false"/> if it is an even number</returns>
        public static bool IsOdd<T>(T input) {

            if (!IsNumeric(input)) {
                throw new InvalidDataException(nameof(input));
            }

            return GetBit(input, 0);
        }

        /// <summary>
        /// Rounds the <see cref="input"/> number to the nearest even number
        /// </summary>
        /// <param name="input">Input number</param>
        /// <param name="dir">Rounding direction</param>
        /// <returns>Closest even number to <see cref="input"/></returns>
        public static int ToEven<T>(T input, Direction dir) {

            if (!IsNumeric(input)) {
                throw new InvalidDataException(nameof(input));
            }

            return Convert.ToInt32(input) + (IsEven(input) ? 0 : (int)dir);
        }

        /// <summary>
        /// Rounds the <see cref="input"/> number to the nearest odd number
        /// </summary>
        /// <param name="input">Input number</param>
        /// <param name="dir">Rounding direction</param>
        /// <returns>Closest odd number to <see cref="input"/></returns>
        public static int ToOdd<T>(T input, Direction dir) {

            if (!IsNumeric(input)) {
                throw new InvalidDataException(nameof(input));
            }

            return Convert.ToInt32(input) + (IsOdd(input) ? 0 : (int)dir);
        }

        /// <summary>
        /// Rounding direction
        /// </summary>
        public enum Direction {
            Greater = +1,
            Lower   = -1,
            Up      = +1,
            Down    = -1,
        }

        /// <summary>
        /// Determines whether an input data is a number
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="input">Input data</param>
        /// <returns><see langword="true"/> if <paramref name="input"/> is a number</returns>
        public static bool IsNumeric<T>(T input) {
            return IsNumeric(Type.GetTypeCode(input.GetType()));
        }

        /// <summary>
        /// Determines whether an input data type code is a numeric type
        /// </summary>
        /// <param name="input">Input data type code</param>
        /// <returns><see langword="true"/> if <paramref name="input"/> is a numeric type</returns>
        public static bool IsNumeric(TypeCode input) {
            switch (input) {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets input data size in bytes
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="input">Input data</param>
        /// <returns>Input data size in bytes</returns>
        public static DataSize GetDataSize<T>(T input) =>
            input == null
                ? DataSize.Null
                : GetDataSize(Type.GetTypeCode(input.GetType()));

        /// <summary>
        /// Gets input data size in bytes
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Input data size in bytes</returns>
        public static DataSize GetDataSize(Type type) =>
            type == null 
                ? DataSize.Null 
                : GetDataSize(Type.GetTypeCode(type));

        /// <summary>
        /// Gets input data size in bytes
        /// </summary>
        /// <param name="t">Type code</param>
        /// <returns>Input data size in bytes</returns>
        public static DataSize GetDataSize(TypeCode t) {

            if (!Enum.IsDefined(typeof(TypeCode), t))
                return DataSize.Null;

            switch (t) {
                case TypeCode.UInt64:
                case TypeCode.Int64:
                    return DataSize.Qword;
                case TypeCode.UInt32:
                case TypeCode.Int32:
                    return DataSize.Dword;
                case TypeCode.UInt16:
                case TypeCode.Int16:
                    return DataSize.Word;
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Char:
                    return DataSize.Byte;
                case TypeCode.Empty:
                    return DataSize.Null;
                default:
                    return (DataSize)Marshal.SizeOf(t);
            }
        }

        /// <summary>
        /// Data size
        /// </summary>
        public enum DataSize {
            Qword = 8,
            Dword = 4,
            Word  = 2,
            Byte  = 1,
            Null  = 0,
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

            return ('A' <= (input & ~0x20) && (input & ~0x20) <= 'F') ||
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
        /// Converts byte array to hex string
        /// </summary>
        /// <param name="input">Input byte array</param>
        /// <returns>Non separated hexadecimal string</returns>
        public static string BytesToHexString(byte[] input) {

            StringBuilder hex = new StringBuilder(input.Length * 2);

            foreach (byte b in input) {
                hex.Append($"{b:X2}");
            }

            return hex.ToString();
        }

        /// <summary>
        /// Compresses any data or decompresses Gzip data
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
        /// Converts array to string
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="input">Input array</param>
        /// <returns>Text string from <paramref name="input"/></returns>
        public static string BytesToString<T>(T[] input) {

            StringBuilder sbOutput = new StringBuilder();

            for (int i = 0; i < input.Length; i++) {
                char c = (char)Convert.ChangeType(input[i], typeof(char));
                sbOutput.Append(c.ToString());
            }

            return sbOutput.ToString();
        }

        /// <summary>
        /// Checks if input data is within ASCII range
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="input">Input symbol</param>
        /// <returns><see langword="true"/> if <see cref="input"/> is within ASCII range</returns>
        public static bool IsAscii<T>(T input) {
            if (input == null) {
                throw new NullReferenceException(nameof(input));
            }

            byte b = (byte)Convert.ChangeType(input, typeof(byte));

            return 0x20 <= b && b <= 0x7E;
        }

        /// <summary>
        /// Checks if input string is within ASCII range
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns><see langword="true"/> if <see cref="input"/> contains ASCII characters</returns>
        public static bool IsAscii(string input) {
            foreach (byte b in Encoding.ASCII.GetBytes(input)) {
                if (!IsAscii(b)) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Removes non ASCII symbols from the string
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns>ASCII-only string</returns>
        public static string TrimNonAscii(string input) {

            StringBuilder output = new StringBuilder(input.Length);

            foreach (char c in Encoding.Default.GetBytes(input)) {
                if (IsAscii(c)) {
                    output.Append(c);
                }
            }

            return output.ToString();
        }

        /// <summary>
        /// Converts input to a Binary Coded Decimal (BCD)
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="input">Input data</param>
        /// <returns>Binary Coded Decimal</returns>
        /// <example><value>0x138</value> is converted to <value>138</value></example>
        public static T ByteToBinaryCodedDecimal<T>(T input) {

            if (!IsNumeric(input)) {
                throw new InvalidDataException(nameof(input));
            }

            int dataSize = Marshal.SizeOf(typeof(T));
            ulong output = default;

            for (int i = 0; i < dataSize; i++) {
                output += (ulong)((((ulong)Convert.ChangeType(input, Type.GetTypeCode(typeof(ulong))) >> (4 * i)) & 0xF) * Math.Pow(10, i));
            }

            return (T)Convert.ChangeType(output, Type.GetTypeCode(typeof(T)));
        }

        /// <summary>
        /// Converts Binary Coded Decimal (BCD) to a byte
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="input">Binary Coded Decimal</param>
        /// <returns>Binary Coded Decimal Byte</returns>
        /// <example><value>314</value> is converted to <value>0x314</value></example>
        public static T BinaryCodedDecimalToByte<T>(T input) {

            if (!IsNumeric(input)) {
                throw new InvalidDataException(nameof(input));
            }

            ulong number = (ulong)Convert.ChangeType(input, Type.GetTypeCode(typeof(ulong)));

            if (number > 0x9999_9999_9999_9999) {
                throw new ArgumentOutOfRangeException(nameof(input));
            }

            int digits = 0;

            while (number / (ulong)Math.Pow(10, digits) > 0) {
                digits++;
            }

            ulong output = default;

            for (int i = digits; i >= 0; i--) {
                ulong divisor = (ulong)Math.Pow(10, i);
                ulong quotient = number / divisor;
                number -= quotient * divisor;
                output |= quotient << i * 4;
            }

            return (T)Convert.ChangeType(output, typeof(T));
        }

        /// <summary>
        /// Returns a consecutive array of numbers based on input criteria
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="start">First number in array</param>
        /// <param name="stop">Last number in array</param>
        /// <param name="step">Number interval</param>
        /// <returns>A consecutive array of numbers between <see cref="start"/> and <see cref="stop"/> with an interval of <see cref="step"/></returns>
        public static T[] ConsecutiveArray<T>(int start, int stop, int step) {

            Queue<T> numbers = new Queue<T>();

            int i = start;

            if (start < stop) {
                do {
                    numbers.Enqueue((T)Convert.ChangeType(i, typeof(T)));
                    i += step;
                } while (i <= stop);
            }
            else {
                do {
                    numbers.Enqueue((T)Convert.ChangeType(i, typeof(T)));
                    i -= step;
                } while (i >= stop);
            }

            return numbers.ToArray();
        }

        /// <summary>
        /// Returns an array populated with the same value
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="element">Element value</param>
        /// <param name="count">Number of elements</param>
        /// <returns>An array of the same <paramref name="element"/> <paramref name="count"/> number of times</returns>
        public static T[] RepetitiveArray<T>(T element, int count) {
            T[] array = new T[count];

            for (int i = 0; i < array.Length; ++i) {
                array[i] = element;
            }

            return array;
        }

        /// <summary>
        /// Returns an array of random bytes
        /// </summary>
        /// <param name="count">Number of bytes in the array</param>
        /// <returns>An array of random bytes</returns>
        public static byte[] RandomArray(int count) {

            byte[] array = new byte[count];

            using (RNGCryptoServiceProvider rg = new RNGCryptoServiceProvider()) {
                rg.GetBytes(array);
            }

            return array;
        }

        /// <summary>
        /// Returns an array of random bytes within specified range
        /// </summary>
        /// <param name="count">Number of bytes in the array</param>
        /// <param name="min">Minimum byte value</param>
        /// <param name="max">Maximum byte value</param>
        /// <returns>An array of random bytes whose values are within <paramref name="min"/> and <paramref name="max"/> values</returns>
        public static byte[] RandomArray(uint count, int min, int max) {

            if (min > max) {
                throw new ArgumentOutOfRangeException($"{nameof(min)} cannot be greater than {nameof(max)}");
            }

            if (max < min) {
                throw new ArgumentOutOfRangeException($"{nameof(max)} cannot be less than {nameof(min)}");
            }

            byte[] array = new byte[count];
            byte r;

            for (int i = 0; i < count;) {

                r = RandomArray(1)[0];

                if (r < min || r > max) {
                    continue;
                }

                array[i] = r;
                i++;
            }

            return array;
        }

        /// <summary>
        /// Returns first index of source array matching input pattern array
        /// </summary>
        /// <param name="source">Source byte array</param>
        /// <param name="pattern">Matching pattern</param>
        /// <returns>First index of <paramref name="source"/> array matching pattern array</returns>
        public static int FindArray<T>(T[] source, T[] pattern) {
            return FindArray(source, pattern, 0);
        }

        /// <summary>
        /// Returns first index of source array matching input pattern array starting from specified position
        /// </summary>
        /// <param name="source">Source byte array</param>
        /// <param name="pattern">Matching pattern</param>
        /// <param name="start">Starting position</param>
        /// <returns>First index of <paramref name="source"/> array matching <paramref name="pattern"/> array from <paramref name="start"/> position</returns>
        public static int FindArray<T>(T[] source, T[] pattern, int start) {

            if (source == null) {
                throw new NullReferenceException(nameof(source));
            }

            if (pattern == null) {
                throw new NullReferenceException(nameof(pattern));
            }

            if (pattern.Length > source.Length) {
                throw new ArgumentOutOfRangeException($"{nameof(pattern)} cannot be greater than {nameof(source)}");
            }

            int maxFirstCharSlot = source.Length - pattern.Length + 1;

            for (int i = start; i < maxFirstCharSlot; i++) {
                // Compare only first byte
                if (!source[i].Equals(pattern[0])) {
                    continue;
                }

                // First byte match found, now try to match the rest of the pattern in reverse
                for (int j = pattern.Length - 1; j >= 1; j--) {
                    if (!source[i + j].Equals(pattern[j])) {
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
        /// Checks if source array contains input pattern at the specified offset
        /// </summary>
        /// <param name="source">Source array</param>
        /// <param name="pattern">Matching pattern array</param>
        /// <param name="offset">Source array offset</param>
        /// <returns><see langword="true"/> if <see cref="pattern"/> is present in <see cref="source"/> at <see cref="offset"/></returns>
        public static bool MatchArray<T>(T[] source, T[] pattern, int offset) {

            if (source == null) {
                throw new NullReferenceException(nameof(source));
            }

            if (pattern == null) {
                throw new NullReferenceException(nameof(pattern));
            }

            if (pattern.Length > source.Length) {
                throw new ArgumentOutOfRangeException();
            }

            if (pattern.Length + offset > source.Length) {
                throw new IndexOutOfRangeException(nameof(offset));
            }

            T[] sourcePart = new T[pattern.Length];

            Array.Copy(
                sourceArray      : source,
                sourceIndex      : offset,
                destinationArray : sourcePart,
                destinationIndex : 0,
                length           : pattern.Length);

            return CompareArray(sourcePart, pattern);
        }

        /// <summary>
        /// Compares two arrays
        /// </summary>
        /// <param name="a1">First array</param>
        /// <param name="a2">Second array</param>
        /// <returns><see langword="true"/> if both arrays are equal</returns>
        public static bool CompareArray<T1, T2>(T1[] a1, T2[] a2) {

            if (a1 == null) {
                throw new ArgumentNullException(nameof(a1));
            }

            if (a2 == null) {
                throw new ArgumentNullException(nameof(a2));
            }

            if (typeof(T1) != typeof(T2)) {
                return false;
            }

            if (a1.Length != a2.Length) {
                return false;
            }

            if (a1.Length == a2.Length) {
                int i = 0;
                while (i < a1.Length) {
                    if (!a1[i].Equals(a2[i])) {
                        return false;
                    }
                    i++;
                }
            }

            return true;
        }

        /// <summary>
        /// Trims array
        /// </summary>
        /// <param name="input">Input array</param>
        /// <param name="newSize">New array size</param>
        /// <param name="trimPosition">Trim position</param>
        /// <returns>Trimmed array</returns>
        public static T[] TrimArray<T>(T[] input, int newSize, TrimPosition trimPosition) {

            if (input == null) {
                throw new ArgumentNullException(nameof(input));
            }

            if (newSize == input.Length) {
                return input;
            }

            if (newSize > input.Length) {
                throw new ArgumentOutOfRangeException($"{nameof(newSize)} cannot be greater than {nameof(input)}");
            }

            T[] newArray = new T[newSize];

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

        /// <summary>
        /// Extracts part of a larger array
        /// </summary>
        /// <param name="input">Input array</param>
        /// <param name="start">New array start position</param>
        /// <param name="length">New array length</param>
        /// <returns></returns>
        public static T[] SubArray<T>(T[] input, uint start, uint length) {
            if (input == null) {
                throw new ArgumentNullException(nameof(input));
            }

            if (start + length > input.Length) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (start == 0 && length == input.Length) {
                return input;
            }

            T[] output = new T[length];

            for (int i = 0; i < length; i++) {
                output[i] = input[i + start];
            }

            return output;
        }

        /// <summary>
        /// Merges two arrays into one array
        /// </summary>
        /// <param name="a1">First array</param>
        /// <param name="a2">Second array</param>
        /// <returns>Merged array</returns>
        public static T[] MergeArray<T>(T[] a1, T[] a2) {

            if (a1 == null) {
                throw new NullReferenceException(nameof(a1));
            }

            if (a2 == null) {
                throw new NullReferenceException(nameof(a2));
            }

            if (a1.GetType() != a2.GetType()) {
                throw new DataException();
            }

            if (a1.Length == 0) {
                return a2;
            }

            if (a2.Length == 0) {
                return a1;
            }

            T[] newArray = new T[a1.Length + a2.Length];

            Array.Copy(a1, newArray, a1.Length);
            Array.Copy(a2, 0, newArray, a1.Length, a2.Length);

            return newArray;
        }

        /// <summary>
        /// Reverses array elements
        /// </summary>
        /// <param name="input">Input array</param>
        /// <returns>Reversed array</returns>
        public static T[] ReverseArray<T>(T[] input) {

            if (input == null) {
                throw new ArgumentNullException(nameof(input));
            }

            if (input.Length == 0) {
                return input;
            }

            T[] newArray = new T[input.Length];

            for (int i = 0; i < input.Length; i++) {
                newArray[i] = input[input.Length - 1 - i];
            }

            return newArray;
        }

        /// <summary>
        /// Checks if input array contains specified element
        /// </summary>
        /// <param name="input">Input array</param>
        /// <param name="item">Element to look for</param>
        /// <returns><see langword="true"/> if <paramref name="input"/> contains <paramref name="item"/></returns>
        public static bool ArrayContains<T>(T[] input, T item) {

            if (input == null) {
                throw new NullReferenceException(nameof(input));
            }

            if (item == null) {
                throw new NullReferenceException(nameof(item));
            }

            if (input.Length == 0) {
                return false;
            }

            foreach (T member in input) {
                if (member.Equals(item)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Converts object array to data type array
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="input">Input array</param>
        /// <returns>Data type array</returns>
        public static T[] ConvertObjectArray<T>(object input) {

            if (input == null) {
                throw new NullReferenceException(nameof(input));
            }

            Queue<object> arrayQueue = new Queue<object>(); // Queue for nested arrays
            Queue<T> outputQueue     = new Queue<T>();

            void AddItem(object item) {
                if (item.GetType().IsArray) {
                    arrayQueue.Enqueue(item);
                }
                else {
                    outputQueue.Enqueue((T)Convert.ChangeType(item, typeof(T)));
                }
            }

            foreach (object item in (Array)input) {
                AddItem(item);
            }

            while (arrayQueue.Count > 0) {
                foreach (object item in (IEnumerable<T>)arrayQueue.Dequeue()) {
                    AddItem(item);
                }
            }

            return outputQueue.ToArray();
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
    }
}