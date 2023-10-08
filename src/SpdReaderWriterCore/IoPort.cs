/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

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
        public T Read<T>(ushort offset) => 
            KernelDriver.ReadIoPort<T>((ushort)(BaseAddress + offset));

        /// <summary>
        /// Reads data from an IO port register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register offset</param>
        /// <param name="output">Output reference</param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public bool ReadEx<T>(ushort offset, out T output) => 
            KernelDriver.ReadIoPortEx((ushort)(BaseAddress + offset), out output);

        /// <summary>
        /// Writes data to an IO port register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Data value</param>
        public void Write<T>(ushort offset, T value) => 
            KernelDriver.WriteIoPortEx((ushort)(BaseAddress + offset), value);

        /// <summary>
        /// Writes data to an IO port register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Data value</param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public bool WriteEx<T>(ushort offset, T value) => 
            KernelDriver.WriteIoPortEx((ushort)(BaseAddress + offset), value);
    }
}