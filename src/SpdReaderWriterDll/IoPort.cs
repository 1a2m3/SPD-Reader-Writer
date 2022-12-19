/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

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
        /// Reads a byte from an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <returns>Register value</returns>
        public byte ReadByte(ushort offset) {
            return Smbus.Driver.ReadIoPortByte((ushort)(BaseAddress + offset));
        }

        /// <summary>
        /// Reads a word from an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <returns>Register value</returns>
        public ushort ReadWord(ushort offset) {
            return Smbus.Driver.ReadIoPortWord((ushort)(BaseAddress + offset));
        }

        /// <summary>
        /// Read a dword from an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <returns>Register value</returns>
        public uint ReadDword(ushort offset) {
            return Smbus.Driver.ReadIoPortDword((ushort)(BaseAddress + offset));
        }

        /// <summary>
        /// Writes a byte to an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Byte value</param>
        public void WriteByte(ushort offset, byte value) {
            Smbus.Driver.WriteIoPortByte((ushort)(BaseAddress + offset), value);
        }

        /// <summary>
        /// Writes a word to an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Word value</param>
        public void WriteWord(ushort offset, ushort value) {
            Smbus.Driver.WriteIoPortWord((ushort)(BaseAddress + offset), value);
        }

        /// <summary>
        /// Writes a dword to an IO port register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Dword value</param>
        public void WriteDword(ushort offset, uint value) {
            Smbus.Driver.WriteIoPortDword((ushort)(BaseAddress + offset), value);
        }
    }
}