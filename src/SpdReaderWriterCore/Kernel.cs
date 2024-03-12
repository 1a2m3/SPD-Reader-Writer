/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using static SpdReaderWriterCore.Data;
using static SpdReaderWriterCore.Data.DataSize;
using static SpdReaderWriterCore.Driver;

namespace SpdReaderWriterCore {

    /// <summary>
    /// Kernel class
    /// </summary>
    public class Kernel {

        #region Delegates

        // Setup
        internal delegate bool SetupDelegate();

        // Controls
        internal delegate bool InstallDriverDelegate();
        internal delegate bool UninstallDriverDelegate();
        internal delegate bool StartDriverDelegate();
        internal delegate bool StopDriverDelegate();
        internal delegate bool LockHandleDelegate(bool state);

        // Info
        internal delegate int GetDriverVersionDelegate(out byte major, out byte minor, out byte revision, out byte release);

        // Read IO
        internal delegate byte ReadIoPortByteDelegate(ushort port);
        internal delegate bool ReadIoPortByteExDelegate(ushort port, out byte output);
        internal delegate ushort ReadIoPortWordDelegate(ushort port);
        internal delegate bool ReadIoPortWordExDelegate(ushort port, out ushort output);
        internal delegate uint ReadIoPortDwordDelegate(ushort port);
        internal delegate bool ReadIoPortDwordExDelegate(ushort port, out uint output);

        // Write IO
        internal delegate void WriteIoPortByteDelegate(ushort port, byte value);
        internal delegate bool WriteIoPortByteExDelegate(ushort port, byte value);
        internal delegate void WriteIoPortWordDelegate(ushort port, ushort value);
        internal delegate bool WriteIoPortWordExDelegate(ushort port, ushort value);
        internal delegate void WriteIoPortDwordDelegate(ushort port, uint value);
        internal delegate bool WriteIoPortDwordExDelegate(ushort port, uint value);

        // Read PCI
        internal delegate byte ReadPciConfigByteDelegate(byte bus, byte device, byte function, ushort offset);
        internal delegate bool ReadPciConfigByteExDelegate(byte bus, byte device, byte function, ushort offset, out byte output);
        internal delegate ushort ReadPciConfigWordDelegate(byte bus, byte device, byte function, ushort offset);
        internal delegate bool ReadPciConfigWordExDelegate(byte bus, byte device, byte function, ushort offset, out ushort output);
        internal delegate uint ReadPciConfigDwordDelegate(byte bus, byte device, byte function, ushort offset);
        internal delegate bool ReadPciConfigDwordExDelegate(byte bus, byte device, byte function, ushort offset, out uint output);

        // Write PCI
        internal delegate void WritePciConfigByteDelegate(byte bus, byte device, byte function, ushort offset, byte value);
        internal delegate bool WritePciConfigByteExDelegate(byte bus, byte device, byte function, ushort offset, byte value);
        internal delegate void WritePciConfigWordDelegate(byte bus, byte device, byte function, ushort offset, ushort value);
        internal delegate bool WritePciConfigWordExDelegate(byte bus, byte device, byte function, ushort offset, ushort value);
        internal delegate void WritePciConfigDwordDelegate(byte bus, byte device, byte function, ushort offset, uint value);
        internal delegate bool WritePciConfigDwordExDelegate(byte bus, byte device, byte function, ushort offset, uint value);

        // Read memory
        internal delegate byte ReadMemoryByteDelegate(uint address);
        internal delegate bool ReadMemoryByteDelegateEx(uint address, out byte output);
        internal delegate ushort ReadMemoryWordDelegate(uint address);
        internal delegate bool ReadMemoryWordDelegateEx(uint address, out ushort output);
        internal delegate uint ReadMemoryDwordDelegate(uint address);
        internal delegate bool ReadMemoryDwordDelegateEx(uint address, out uint output);

        #endregion

        #region IO Port

        /// <summary>
        /// Reads a register value from the specified I/O port address
        /// </summary>
        /// <typeparam name="T">Return value type</typeparam>
        /// <param name="port">I/O port address</param>
        /// <returns>Value read from the specified <paramref name="port">I/O port address</paramref></returns>
        public static T ReadIoPort<T>(ushort port) {
            ReadIoPortEx(port, out T output);
            return output;
        }

        /// <summary>
        /// Reads a register value from the specified I/O port address
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="port">I/O port address</param>
        /// <param name="output">Register value read from the specified <paramref name="port">I/O port address</paramref></param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public static bool ReadIoPortEx<T>(ushort port, out T output) {

            bool result = false;
            output = default;

            switch (GetDataSize(typeof(T))) {
                case Byte: {
                    result = DriverInfo.ReadIoPortByteEx(port, out byte buffer);
                    output = ConvertTo<T>(buffer);
                    break;
                }
                case Word: {
                    result = DriverInfo.ReadIoPortWordEx(port, out ushort buffer);
                    output = ConvertTo<T>(buffer);
                    break;
                }
                case Dword: {
                    result = DriverInfo.ReadIoPortDwordEx(port, out uint buffer);
                    output = ConvertTo<T>(buffer);
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Writes data to an I/O port register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="port">Register offset</param>
        /// <param name="input">Input data value</param>
        public static void WriteIoPort<T>(ushort port, T input) =>
            WriteIoPortEx(port, input);

        /// <summary>
        /// Writes data to an I/O port register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="port">Register offset</param>
        /// <param name="input">Input data value</param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public static bool WriteIoPortEx<T>(ushort port, T input) {

            switch (GetDataSize(typeof(T))) {
                case Byte:
                    return DriverInfo.WriteIoPortByteEx(port, ConvertTo<byte>(input));
                case Word:
                    return DriverInfo.WriteIoPortWordEx(port, ConvertTo<ushort>(input));
                case Dword:
                    return DriverInfo.WriteIoPortDwordEx(port, ConvertTo<uint>(input));
                default:
                    return false;
            }
        }

        #endregion

        #region PCI

        /// <summary>
        /// Reads PCI register value from the specified location
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="bus">PCI bus number</param>
        /// <param name="device">PCI device number</param>
        /// <param name="function">PCI function number</param>
        /// <param name="offset">Register offset</param>
        /// <returns>PCI register value</returns>
        public static T ReadPciConfig<T>(byte bus, byte device, byte function, ushort offset) {
            ReadPciConfigEx(bus, device, function, offset, out T output);
            return output;
        }

        /// <summary>
        /// Reads PCI register value from the specified location
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="bus">PCI bus number</param>
        /// <param name="device">PCI device number</param>
        /// <param name="function">PCI function number</param>
        /// <param name="offset">Register offset</param>
        /// <param name="output">PCI register value</param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public static bool ReadPciConfigEx<T>(byte bus, byte device, byte function, ushort offset, out T output) {

            bool result = false;
            output = default;

            switch (GetDataSize(typeof(T))) {
                case Byte: {
                    result = DriverInfo.ReadPciConfigByteEx(bus, device, function, offset, out byte buffer);
                    output = ConvertTo<T>(buffer);
                    break;
                }
                case Word: {
                    result = DriverInfo.ReadPciConfigWordEx(bus, device, function, offset, out ushort buffer);
                    output = ConvertTo<T>(buffer);
                    break;
                }
                case Dword: {
                    result = DriverInfo.ReadPciConfigDwordEx(bus, device, function, offset, out uint buffer);
                    output = ConvertTo<T>(buffer);
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Writes to PCI register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="bus">PCI bus number</param>
        /// <param name="device">PCI device number</param>
        /// <param name="function">PCI function number</param>
        /// <param name="offset">Register offset</param>
        /// <param name="input">Input data value</param>
        public static void WritePciConfig<T>(byte bus, byte device, byte function, ushort offset, T input) => 
            WritePciConfigEx(bus, device, function, offset, input);

        /// <summary>
        /// Writes to PCI register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="bus">PCI bus number</param>
        /// <param name="device">PCI device number</param>
        /// <param name="function">PCI function number</param>
        /// <param name="offset">Register offset</param>
        /// <param name="input">Input value</param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public static bool WritePciConfigEx<T>(byte bus, byte device, byte function, ushort offset, T input) {

            switch (GetDataSize(typeof(T))) {
                case Byte:
                    return DriverInfo.WritePciConfigByteEx(bus, device, function, offset, ConvertTo<byte>(input));
                case Word:
                    return DriverInfo.WritePciConfigWordEx(bus, device, function, offset, ConvertTo<ushort>(input));
                case Dword:
                    return DriverInfo.WritePciConfigDwordEx(bus, device, function, offset, ConvertTo<uint>(input));
                default:
                    return false;
            }
        }

        #endregion

        #region Memory

        /// <summary>
        /// Reads data from physical memory
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="address">Memory address</param>
        /// <returns>Data at specified memory address</returns>
        public static T ReadMemory<T>(uint address) {
            ReadMemoryEx(address, out T output);
            return output;
        }

        /// <summary>
        /// Reads data from physical memory
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="address">Memory address</param>
        /// <param name="output">Output data reference</param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public static bool ReadMemoryEx<T>(uint address, out T output) {

            bool result = false;
            output = default;

            switch (GetDataSize(typeof(T))) {
                case Byte: {
                    result = DriverInfo.ReadMemoryByteEx(address, out byte buffer);
                    output = ConvertTo<T>(buffer);
                    break;
                }
                case Word: {
                    result = DriverInfo.ReadMemoryWordEx(address, out ushort buffer);
                    output = ConvertTo<T>(buffer);
                    break;
                }
                case Dword: {
                    result = DriverInfo.ReadMemoryDwordEx(address, out uint buffer);
                    output = ConvertTo<T>(buffer);
                    break;
                }
            }

            return result;
        }

        #endregion
    }
}