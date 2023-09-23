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
using System.Reflection;

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
            return $"0x{BaseAddress:X4}";
        }

        /// <summary>
        /// Reads data from an IO port register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register offset</param>
        /// <returns>Register value</returns>
        public T Read<T>(ushort offset) {

            Data.DataSize inputSize = Data.GetDataSize(typeof(T));

            if (inputSize > Data.DataSize.Dword || inputSize < Data.DataSize.Byte) {
                throw new InvalidDataException($"{MethodBase.GetCurrentMethod()?.Name}:{typeof(T)}");
            }

            object output = KernelDriver.ReadIoPort<T>((ushort)(BaseAddress + offset));
            return (T)Convert.ChangeType(output, typeof(T));
        }

        /// <summary>
        /// Reads data from an IO port register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register offset</param>
        /// <param name="output">Output reference</param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public bool Read<T>(ushort offset, out T output) => 
            KernelDriver.ReadIoPortEx(offset, out output);

        /// <summary>
        /// Writes data to an IO port register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Data value</param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public bool Write<T>(ushort offset, T value) {

            object input = value;
            Data.DataSize inputSize = Data.GetDataSize(input);

            if (inputSize > Data.DataSize.Dword || inputSize < Data.DataSize.Byte) {
                throw new InvalidDataException($"{MethodBase.GetCurrentMethod()?.Name}:{typeof(T)}");
            }

            return KernelDriver.WriteIoPortEx((ushort)(BaseAddress + offset), (T)input);
        }
    }
}