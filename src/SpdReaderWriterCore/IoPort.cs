/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using System;
using System.IO;

namespace SpdReaderWriterCore {

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
                output = Smbus.Driver.ReadIoPortByte((ushort)(BaseAddress + offset));
            }
            else if (typeof(T) == typeof(ushort)) {
                output = Smbus.Driver.ReadIoPortWord((ushort)(BaseAddress + offset));
            }
            else if (typeof(T) == typeof(uint)) {
                output = Smbus.Driver.ReadIoPortDword((ushort)(BaseAddress + offset));
            }

            if (output != null) {
                return (T)Convert.ChangeType(output, typeof(T));
            }

            throw new InvalidDataException(nameof(T));
        }

        /// <summary>
        /// Writes data to an IO port register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Data value</param>
        /// <returns><see lang="true"/> if the function succeeds</returns>
        public bool Write<T>(ushort offset, T value) {

            object input = Convert.ChangeType(value, typeof(T));

            if (typeof(T) == typeof(byte)) {
                return Smbus.Driver.WriteIoPortByteEx((ushort)(BaseAddress + offset), (byte)input);
            }

            if (typeof(T) == typeof(ushort)) {
                return Smbus.Driver.WriteIoPortWordEx((ushort)(BaseAddress + offset), (ushort)input);
            }

            if (typeof(T) == typeof(uint)) {
                return Smbus.Driver.WriteIoPortDwordEx((ushort)(BaseAddress + offset), (uint)input);
            }

            throw new InvalidDataException(nameof(T));
        }
    }
}