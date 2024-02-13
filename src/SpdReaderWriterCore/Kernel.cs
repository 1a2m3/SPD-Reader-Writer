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
        internal delegate uint GetDriverVersionDelegate(out byte major, out byte minor, out byte revision, out byte release);

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

            if (ReadIoPortEx(port, out T output)) {
                return output;
            }

            throw new InvalidDataException($"{MethodBase.GetCurrentMethod()?.Name}:{typeof(T)}");
        }

        /// <summary>
        /// Reads a register value from the specified I/O port address
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="port">I/O port address</param>
        /// <param name="output">Register value read from the specified <paramref name="port">I/O port address</paramref></param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public static bool ReadIoPortEx<T>(ushort port, out T output) {

            object outputData = null;
            bool result       = false;
            output            = default;

            if (Data.GetDataSize(typeof(T)) == Data.DataSize.Byte) {
                result = Driver.DriverInfo.ReadIoPortByteEx(port, out byte buffer);
                outputData = buffer;
            }
            else if (Data.GetDataSize(typeof(T)) == Data.DataSize.Word) {
                result = Driver.DriverInfo.ReadIoPortWordEx(port, out ushort buffer);
                outputData = buffer;
            }
            else if (Data.GetDataSize(typeof(T)) == Data.DataSize.Dword) {
                result = Driver.DriverInfo.ReadIoPortDwordEx(port, out uint buffer);
                outputData = buffer;
            }
            
            if (outputData != null) {
                output = (T)outputData;
            }

            return result;
        }

        /// <summary>
        /// Writes data to an I/O port register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="port">Register offset</param>
        /// <param name="value">Data value</param>
        public static void WriteIoPort<T>(ushort port, T value) =>
            WriteIoPortEx(port, value);

        /// <summary>
        /// Writes data to an I/O port register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="port">Register offset</param>
        /// <param name="value">Data value</param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public static bool WriteIoPortEx<T>(ushort port, T value) {

            object input = value;

            switch (Data.GetDataSize(input)) {
                case Data.DataSize.Byte:
                    return Driver.DriverInfo.WriteIoPortByteEx(port, (byte)input);
                case Data.DataSize.Word:
                    return Driver.DriverInfo.WriteIoPortWordEx(port, (ushort)input);
                case Data.DataSize.Dword:
                    return Driver.DriverInfo.WriteIoPortDwordEx(port, (uint)input);
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

            T outputData = default;

            switch (Data.GetDataSize(typeof(T))) {
                case Data.DataSize.Byte:
                case Data.DataSize.Word:
                case Data.DataSize.Dword:
                    ReadPciConfigEx(bus, device, function, offset, out outputData);
                    break;
                default:
                    throw new InvalidDataException($"{MethodBase.GetCurrentMethod()?.Name}:{typeof(T)}");
            }

            return (T)Convert.ChangeType(outputData, Type.GetTypeCode(typeof(T)));
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

            object outputData = null;
            bool result       = false;
            output            = default;

            if (Data.GetDataSize(typeof(T)) == Data.DataSize.Byte) {
                result = Driver.DriverInfo.ReadPciConfigByteEx(bus, device, function, offset, out byte buffer);
                outputData = buffer;
            }
            else if (Data.GetDataSize(typeof(T)) == Data.DataSize.Word) {
                result = Driver.DriverInfo.ReadPciConfigWordEx(bus, device, function, offset, out ushort buffer);
                outputData = buffer;
            }
            else if (Data.GetDataSize(typeof(T)) == Data.DataSize.Dword) {
                result = Driver.DriverInfo.ReadPciConfigDwordEx(bus, device, function, offset, out uint buffer);
                outputData = buffer;
            }

            if (outputData != null) {
                output = (T)outputData;
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
        /// <param name="value">Input value</param>
        public static void WritePciConfig<T>(byte bus, byte device, byte function, ushort offset, T value) => 
            WritePciConfigEx(bus, device, function, offset, value);

        /// <summary>
        /// Writes to PCI register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="bus">PCI bus number</param>
        /// <param name="device">PCI device number</param>
        /// <param name="function">PCI function number</param>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Input value</param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public static bool WritePciConfigEx<T>(byte bus, byte device, byte function, ushort offset, T value) {

            object input = value;

            switch (Data.GetDataSize(typeof(T))) {
                case Data.DataSize.Byte:
                    return Driver.DriverInfo.WritePciConfigByteEx(bus, device, function, offset, (byte)input);
                case Data.DataSize.Word:
                    return Driver.DriverInfo.WritePciConfigWordEx(bus, device, function, offset, (ushort)input);
                case Data.DataSize.Dword:
                    return Driver.DriverInfo.WritePciConfigDwordEx(bus, device, function, offset, (uint)input);
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

            if (ReadMemoryEx(address, out T output)) {
                return (T)Convert.ChangeType(output, Type.GetTypeCode(typeof(T)));
            }

            throw new InvalidDataException($"{MethodBase.GetCurrentMethod()?.Name}:{typeof(T)}");
        }

        /// <summary>
        /// Reads data from physical memory
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="address">Memory address</param>
        /// <param name="output">Output data reference</param>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        public static bool ReadMemoryEx<T>(uint address, out T output) {

            object outputData = null;
            bool result       = false;
            output            = default;

            if (Data.GetDataSize(typeof(T)) == Data.DataSize.Byte) {
                result = Driver.DriverInfo.ReadMemoryByteEx(address, out byte buffer);
                outputData = buffer;
            }
            else if (Data.GetDataSize(typeof(T)) == Data.DataSize.Word) {
                result = Driver.DriverInfo.ReadMemoryWordEx(address, out ushort buffer);
                outputData = buffer;
            }
            else if (Data.GetDataSize(typeof(T)) == Data.DataSize.Dword) {
                result = Driver.DriverInfo.ReadMemoryDwordEx(address, out uint buffer);
                outputData = buffer;
            }

            if (outputData != null) {
                output = (T)outputData;
            }

            return result;
        }

        #endregion
    }
}