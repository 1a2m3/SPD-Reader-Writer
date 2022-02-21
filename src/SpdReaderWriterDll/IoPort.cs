using System;
using UInt8 = System.Byte;

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
        /// IO port instance description
        /// </summary>
        /// <returns>Readable IO port instance description</returns>
        public override string ToString() {
            return $"IO port {BaseAddress:X4}h";
        }

        /// <summary>
        /// IO Port base address
        /// </summary>
        public UInt16 BaseAddress;

        /// <summary>
        /// Write IO port mapped register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Register value</param>
        public void WriteByte(UInt8 offset, byte value) {
            Smbus.Driver.WriteIoPortByte((UInt16)(BaseAddress + offset), value);
        }

        /// <summary>
        /// Read IO port mapped register
        /// </summary>
        /// <param name="offset">Register offset</param>
        /// <returns>Register value</returns>
        public byte ReadByte(UInt8 offset) {
            return Smbus.Driver.ReadIoPortByte((UInt16)(BaseAddress + offset));
        }
    }
}
