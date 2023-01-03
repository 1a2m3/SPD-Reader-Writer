/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using System;

namespace SpdReaderWriterDll {

    /// <summary>
    /// I/O port class
    /// </summary>
    public class IoPort {

        /// <summary>
        /// New IO port instance
        /// </summary>
        public IoPort() {
            BaseAddress = 0;
        }

        /// <summary>
        /// New IO port instance
        /// </summary>
        /// <param name="address">Base address</param>
        public IoPort(ushort address) {
            BaseAddress = address;
        }

        /// <summary>
        /// IO Port base address
        /// </summary>
        public ushort BaseAddress { get; set; }

        /// <summary>
        /// IO port instance description
        /// </summary>
        /// <returns>Readable IO port instance description</returns>
        public override string ToString() {
            return $"IO port {BaseAddress:X4}h";
        }

        /// <summary>
        /// Reads data from an IO port register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register offset</param>
        /// <returns>Register value</returns>
        public T Read<T>(ushort offset) {

            object output = null;

            if (typeof(T) == typeof(byte)) {
                output = ReadByte(offset);
            }
            else if (typeof(T) == typeof(ushort)) {
                output = ReadWord(offset);
            }
            else if (typeof(T) == typeof(uint)) {
                output = ReadDword(offset);
            }

            if (output != null) {
                return (T)Convert.ChangeType(output, typeof(T));
            }

            throw new Exception("Wrong data type");
        }

        /// <summary>
        /// Writes data to an IO port register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Data value</param>
        public bool Write<T>(ushort offset, T value) {

            if (typeof(T) == typeof(byte)) {
                return WriteByte(offset, (byte)Convert.ChangeType(value, typeof(T)));
            }

            if (typeof(T) == typeof(ushort)) {
                return WriteWord(offset, (ushort)Convert.ChangeType(value, typeof(T)));
            }

            if (typeof(T) == typeof(uint)) {
                return WriteDword(offset, (uint)Convert.ChangeType(value, typeof(T)));
            }

            throw new Exception("Wrong data type");
        }

        /// <summary>
        /// Reads a byte from an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <returns>Register value</returns>
        private byte ReadByte(ushort offset) {
            return Smbus.Driver.ReadIoPortByte((ushort)(BaseAddress + offset));
        }

        /// <summary>
        /// Reads a word from an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <returns>Register value</returns>
        private ushort ReadWord(ushort offset) {
            return Smbus.Driver.ReadIoPortWord((ushort)(BaseAddress + offset));
        }

        /// <summary>
        /// Read a dword from an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <returns>Register value</returns>
        private uint ReadDword(ushort offset) {
            return Smbus.Driver.ReadIoPortDword((ushort)(BaseAddress + offset));
        }

        /// <summary>
        /// Writes a byte to an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Byte value</param>
        private bool WriteByte(ushort offset, byte value) {
            return Smbus.Driver.WriteIoPortByteEx((ushort)(BaseAddress + offset), value);
        }

        /// <summary>
        /// Writes a word to an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Word value</param>
        private bool WriteWord(ushort offset, ushort value) {
            return Smbus.Driver.WriteIoPortWordEx((ushort)(BaseAddress + offset), value);
        }

        /// <summary>
        /// Writes a dword to an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Dword value</param>
        private bool WriteDword(ushort offset, uint value) {
            return Smbus.Driver.WriteIoPortDwordEx((ushort)(BaseAddress + offset), value);
        }
    }
}