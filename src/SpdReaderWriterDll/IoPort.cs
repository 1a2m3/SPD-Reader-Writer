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
        public IoPort(UInt16 address) {
            BaseAddress = address;
        }

        /// <summary>
        /// IO Port base address
        /// </summary>
        public UInt16 BaseAddress { get; set; }

        /// <summary>
        /// IO port instance description
        /// </summary>
        /// <returns>Readable IO port instance description</returns>
        public override string ToString() {
            return $"IO port {BaseAddress:X4}h";
        }

        /// <summary>
        /// Reads a byte from an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <returns>Register value</returns>
        public byte ReadByte(UInt16 offset) {
            return Smbus.Driver.ReadIoPortByte((UInt16)(BaseAddress + offset));
        }

        /// <summary>
        /// Reads a word from an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <returns>Register value</returns>
        public UInt16 ReadWord(UInt16 offset) {
            return Smbus.Driver.ReadIoPortWord((UInt16)(BaseAddress + offset));
        }

        /// <summary>
        /// Read a dword from an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <returns>Register value</returns>
        public UInt32 ReadDword(UInt16 offset) {
            return Smbus.Driver.ReadIoPortDword((UInt16)(BaseAddress + offset));
        }

        /// <summary>
        /// Writes a byte to an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Byte value</param>
        public void WriteByte(UInt16 offset, byte value) {
            Smbus.Driver.WriteIoPortByte((UInt16)(BaseAddress + offset), value);
        }

        /// <summary>
        /// Writes a word to an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Word value</param>
        public void WriteWord(UInt16 offset, UInt16 value) {
            Smbus.Driver.WriteIoPortWord((UInt16)(BaseAddress + offset), value);
        }

        /// <summary>
        /// Writes a dword to an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Dword value</param>
        public void WriteDword(UInt16 offset, UInt32 value) {
            Smbus.Driver.WriteIoPortDword((UInt16)(BaseAddress + offset), value);
        }
    }
}