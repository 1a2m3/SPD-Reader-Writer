/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using SpdReaderWriterCore.Properties;
using static SpdReaderWriterCore.NativeFunctions.Advapi32;

namespace SpdReaderWriterCore.Driver {

    /// <summary>
    /// CPU-Z Kernel driver class
    /// </summary>
    public static class CpuZ {

        /// <summary>
        /// CPU-Z Driver info
        /// </summary>
        internal static KernelDriver.Info CpuZDriverInfo = new KernelDriver.Info {

            ServiceName = @"cpuz158",
            DeviceName  = @"\\.\cpuz158",
            GZipData    = Environment.Is64BitOperatingSystem
                ? Resources.Driver.cpuzx64
                : Resources.Driver.cpuzx32,

            // Setup
            Setup                 = Setup,

            // Driver info
            GetDriverVersion      = GetDriverVersion,

            // Driver functions
            ReadIoPortByte        = ReadIoPortByte,
            ReadIoPortByteEx      = ReadIoPortByteEx,
            ReadIoPortWord        = ReadIoPortWord,
            ReadIoPortWordEx      = ReadIoPortWordEx,
            ReadIoPortDword       = ReadIoPortDword,
            ReadIoPortDwordEx     = ReadIoPortDwordEx,
            WriteIoPortByte       = WriteIoPortByte,
            WriteIoPortByteEx     = WriteIoPortByteEx,
            WriteIoPortWord       = WriteIoPortWord,
            WriteIoPortWordEx     = WriteIoPortWordEx,
            WriteIoPortDword      = WriteIoPortDword,
            WriteIoPortDwordEx    = WriteIoPortDwordEx,
            ReadPciConfigByte     = ReadPciConfigByte,
            ReadPciConfigByteEx   = ReadPciConfigByteEx,
            ReadPciConfigWord     = ReadPciConfigWord,
            ReadPciConfigWordEx   = ReadPciConfigWordEx,
            ReadPciConfigDword    = ReadPciConfigDword,
            ReadPciConfigDwordEx  = ReadPciConfigDwordEx,
            WritePciConfigByte    = WritePciConfigByte,
            WritePciConfigByteEx  = WritePciConfigByteEx,
            WritePciConfigWord    = WritePciConfigWord,
            WritePciConfigWordEx  = WritePciConfigWordEx,
            WritePciConfigDword   = WritePciConfigDword,
            WritePciConfigDwordEx = WritePciConfigDwordEx,
            ReadMemoryByte        = ReadMemoryByte,
            ReadMemoryByteEx      = ReadMemoryByteEx,
            ReadMemoryWord        = ReadMemoryWord,
            ReadMemoryWordEx      = ReadMemoryWordEx,
            ReadMemoryDword       = ReadMemoryDword,
            ReadMemoryDwordEx     = ReadMemoryDwordEx
        };

        /// <summary>
        /// Set up driver environment
        /// </summary>
        /// <returns><see langword="true"/> if the function succeeds</returns>
        private static bool Setup() => AdjustPrivileges();

        /// <summary>
        /// Set up and adjust driver access privileges
        /// </summary>
        /// <returns><see langword="true"/> if privileges are successfully adjusted</returns>
        private static bool AdjustPrivileges() {

            // Value passed to SeSinglePrivilegeCheck() function as PrivilegeValue parameter in IRP_MJ_CREATE routine:
            LUID luid = new LUID {
                LowPart = 0x0A
            };

            LUID_AND_ATTRIBUTES[] newLuidAndAttributeses = {
                new LUID_AND_ATTRIBUTES {
                    Luid       = luid,
                    Attributes = LuidAttributes.SE_PRIVILEGE_ENABLED
                }
            };

            TOKEN_PRIVILEGES newPrivileges = new TOKEN_PRIVILEGES {
                Privileges     = newLuidAndAttributeses,
                PrivilegeCount = newLuidAndAttributeses.Length
            };

            TOKEN_PRIVILEGES previousPrivileges = new TOKEN_PRIVILEGES();

            if (!OpenProcessToken(
                    processHandle : Process.GetCurrentProcess().Handle,
                    desiredAccess : DesiredAccess.TOKEN_ADJUST_PRIVILEGES | 
                                    DesiredAccess.TOKEN_QUERY,
                    tokenHandle   : out IntPtr tokenHandle) &&
                !LookupPrivilegeValue(
                    lpSystemName  : null,
                    lpName        : "SeLoadDriverPrivilege",
                    lpLuid        : ref luid)) {
                return false;
            }

            bool result = AdjustTokenPrivileges(
                tokenHandle          : tokenHandle,
                disableAllPrivileges : false,
                newState             : ref newPrivileges,
                bufferLengthInBytes  : 16,
                previousState        : ref previousPrivileges,
                returnLengthInBytes  : out uint returnLength);

            return result && 
                   Marshal.GetLastWin32Error() != SystemError.ErrorNotAllAssigned;
        }

        /// <summary>
        /// Retrieves driver version
        /// </summary>
        /// <param name="major">Major version number</param>
        /// <param name="minor">Minor version number</param>
        /// <param name="revision">Revision number</param>
        /// <param name="release">Release number</param>
        /// <returns>Driver version</returns>
        private static uint GetDriverVersion(out byte major, out byte minor, out byte revision, out byte release) {

            uint version = 0;

            KernelDriver.DeviceIoControl(IoControlCode.CPUZ_GET_DRIVER_VERSION, 0, ref version);
            
            major    = (byte)(version >> 8);
            minor    = 0;
            revision = (byte)((version >> 4) & 0x0F);
            release  = (byte)(version & 0x0F);

            version = Data.ByteToBinaryCodedDecimal(version);

            return version;
        }

        /// <summary>
        /// CPUID driver IO control codes
        /// </summary>
        private struct IoControlCode {
            // Info
            internal const uint CPUZ_GET_DRIVER_VERSION  = 0x9C402400; // Binary coded decimal
            internal const uint CPUZ_GET_KERNEL_VERSION  = 0x9C402404; // PsGetVersion

            // IO
            internal const uint CPUZ_READ_IO_PORT_BYTE   = 0x9C402480; // __inbyte (x64) / READ_PORT_UCHAR (x32)
            internal const uint CPUZ_READ_IO_PORT_WORD   = 0x9C402484; // __inword (x64) / READ_PORT_USHORT (x32)
            internal const uint CPUZ_READ_IO_PORT_DWORD  = 0x9C402488; // __indword (x64) / READ_PORT_ULONG (x32)

            internal const uint CPUZ_WRITE_IO_PORT_BYTE  = 0x9C4024C0; // __outbyte (x64) / WRITE_PORT_UCHAR (x32)
            internal const uint CPUZ_WRITE_IO_PORT_WORD  = 0x9C4024C4; // __outword (x64) / WRITE_PORT_USHORT (x32)
            internal const uint CPUZ_WRITE_IO_PORT_DWORD = 0x9C4024C8; // __outdword (x64) / WRITE_PORT_ULONG (x32)

            // PCI
            internal const uint CPUZ_READ_PCI_CONFIG     = 0x9C402700; // HalGetBusDataByOffset
            internal const uint CPUZ_WRITE_PCI_CONFIG    = 0x9C402704; // HalSetBusDataByOffset

            // MSR
            internal const uint CPUZ_READ_MSR            = 0x9C402440; // __readmsr
            internal const uint CPUZ_WRITE_MSR           = 0x9C402444; // __writemsr

            // Memory
            internal const uint CPUZ_READ_MEMORY         = 0x9C402708; // MmMapIoSpace
        }

        /// <summary>
        /// Response codes
        /// </summary>
        private struct Status {
            internal const uint SUCCESS = 0x87654321;
        }

        #region IO

        /// <summary>
        /// Reads a byte value from an I/O port
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <returns>Byte value at specified <see cref="port"/></returns>
        private static byte ReadIoPortByte(ushort port) {
            ReadIoPortEx(port, out byte output);
            return output;
        }

        /// <summary>
        /// Reads a word value from an I/O port
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <returns>Word value at specified <see cref="port"/></returns>
        private static ushort ReadIoPortWord(ushort port) {
            ReadIoPortEx(port, out ushort output);
            return output;
        }

        /// <summary>
        /// Reads a dword value from an I/O port
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <returns>Dword value at specified <see cref="port"/></returns>
        private static uint ReadIoPortDword(ushort port) {
            ReadIoPortEx(port, out ushort output);
            return output;
        }

        /// <summary>
        /// Reads a byte value from an I/O port
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="output">Byte value at specified <see cref="port"/></param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool ReadIoPortByteEx(ushort port, out byte output) => 
            ReadIoPortEx(port, out output);

        /// <summary>
        /// Reads a word value from an I/O port
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="output">Word value at specified <see cref="port"/></param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool ReadIoPortWordEx(ushort port, out ushort output) => 
            ReadIoPortEx(port, out output);

        /// <summary>
        /// Reads a dword value from an I/O port
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="output">Dword value at specified <see cref="port"/></param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool ReadIoPortDwordEx(ushort port, out uint output) => 
            ReadIoPortEx(port, out output);

        /// <summary>
        /// Reads register value from an I/O port
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="port">I/O port address</param>
        /// <param name="output">Register value</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool ReadIoPortEx<T>(ushort port, out T output) {

            output = default;
            ReadIoPortOutput portOutput = new ReadIoPortOutput();
            ReadIoPortInput portInput   = new ReadIoPortInput {
                PortNumber = port
            };

            uint ioCtlCode;

            switch (Data.GetDataSize(output)) {
                case Data.DataSize.Byte:
                    ioCtlCode = IoControlCode.CPUZ_READ_IO_PORT_BYTE;
                    break;
                case Data.DataSize.Word:
                    ioCtlCode = IoControlCode.CPUZ_READ_IO_PORT_WORD;
                    break;
                case Data.DataSize.Dword:
                    ioCtlCode = IoControlCode.CPUZ_READ_IO_PORT_DWORD;
                    break;
                default:
                    throw new InvalidDataException($"{MethodBase.GetCurrentMethod()?.Name}:{typeof(T)}");
            }

            if (!KernelDriver.DeviceIoControl(ioCtlCode, portInput, ref portOutput)) {
                return false;
            }

            output = (T)Convert.ChangeType(portOutput.Data, Type.GetTypeCode(typeof(T)));

            return portOutput.Status == Status.SUCCESS;
        }

        /// <summary>
        /// Writes byte value to an I/O port register
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="input">Register value</param>
        private static void WriteIoPortByte(ushort port, byte input) => 
            WriteIoPortEx(port, input);

        /// <summary>
        /// Writes byte value to an I/O port register
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="input">Register value</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool WriteIoPortByteEx(ushort port, byte input) => 
            WriteIoPortEx(port, input);

        /// <summary>
        /// Writes word value to an I/O port register
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="input">Register value</param>
        private static void WriteIoPortWord(ushort port, ushort input) =>
            WriteIoPortEx(port, input);

        /// <summary>
        /// Writes word value to an I/O port register
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="input">Register value</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool WriteIoPortWordEx(ushort port, ushort input) => 
            WriteIoPortEx(port, input);

        /// <summary>
        /// Writes dword value to an I/O port register
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="input">Register value</param>
        private static void WriteIoPortDword(ushort port, uint input) => 
            WriteIoPortEx(port, input);

        /// <summary>
        /// Writes dword value to an I/O port register
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="input">Register value</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool WriteIoPortDwordEx(ushort port, uint input) => 
            WriteIoPortEx(port, input);

        /// <summary>
        /// Writes value to an I/O port register
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="port">I/O port address</param>
        /// <param name="input">Register value</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool WriteIoPortEx<T>(ushort port, T input) {

            ReadIoPortOutput portOutput = new ReadIoPortOutput();
            WriteIoPortInput portInput  = new WriteIoPortInput {
                PortNumber = port,
            };

            uint ioCtlCode;

            switch (Data.GetDataSize(input)) {
                case Data.DataSize.Byte:
                    ioCtlCode = IoControlCode.CPUZ_WRITE_IO_PORT_BYTE;
                    break;
                case Data.DataSize.Word:
                    ioCtlCode = IoControlCode.CPUZ_WRITE_IO_PORT_WORD;
                    break;
                case Data.DataSize.Dword:
                    ioCtlCode = IoControlCode.CPUZ_WRITE_IO_PORT_DWORD;
                    break;
                default:
                    throw new InvalidDataException($"{MethodBase.GetCurrentMethod()?.Name}:{typeof(T)}");
            }

            portInput.Data = (uint)Convert.ChangeType(input, TypeCode.UInt32);

            if (!KernelDriver.DeviceIoControl(ioCtlCode, portInput, ref portOutput)) {
                return false;
            }

            return portOutput.Status == Status.SUCCESS;
        }

        /// <summary>
        /// IO Port input for reading from an I/O port
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 4)]
        private struct ReadIoPortInput {
            [FieldOffset(0)] internal ushort PortNumber;
        }

        /// <summary>
        /// IO Port output when reading from an I/O port
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct ReadIoPortOutput {
            [FieldOffset(0)] internal readonly uint Data;
            [FieldOffset(4)] internal readonly uint Status;
        }

        /// <summary>
        /// IO Port input for writing to an I/O port
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct WriteIoPortInput {
            [FieldOffset(0)] internal uint PortNumber;
            [FieldOffset(4)] internal uint Data;
        }

        #endregion

        #region PCI

        /// <summary>
        /// Reads byte value from the specified PCI device register
        /// </summary>
        /// <param name="bus">PCI bus number</param>
        /// <param name="device">PCI device number</param>
        /// <param name="function">PCI function number</param>
        /// <param name="offset">Register location</param>
        /// <returns>Register byte value</returns>
        private static byte ReadPciConfigByte(byte bus, byte device, byte function, ushort offset) {
            ReadPciConfigByteEx(bus, device, function, offset, out byte output);
            return output;
        }

        /// <summary>
        /// Reads word value from the specified PCI device register
        /// </summary>
        /// <param name="bus">PCI bus number</param>
        /// <param name="device">PCI device number</param>
        /// <param name="function">PCI function number</param>
        /// <param name="offset">Register location</param>
        /// <returns>Register word value</returns>
        private static ushort ReadPciConfigWord(byte bus, byte device, byte function, ushort offset) {
            ReadPciConfigWordEx(bus, device, function, offset, out ushort output);
            return output;
        }

        /// <summary>
        /// Reads dword value from the specified PCI device register
        /// </summary>
        /// <param name="bus">PCI bus number</param>
        /// <param name="device">PCI device number</param>
        /// <param name="function">PCI function number</param>
        /// <param name="offset">Register location</param>
        /// <returns>Register dword value</returns>
        private static uint ReadPciConfigDword(byte bus, byte device, byte function, ushort offset) {
            ReadPciConfigDwordEx(bus, device, function, offset, out uint output);
            return output;
        }

        /// <summary>
        /// Reads byte value from the specified PCI device register
        /// </summary>
        /// <param name="bus">PCI bus number</param>
        /// <param name="device">PCI device number</param>
        /// <param name="function">PCI function number</param>
        /// <param name="offset">Register location</param>
        /// <param name="output">Byte output reference</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool ReadPciConfigByteEx(byte bus, byte device, byte function, ushort offset, out byte output) =>
            ReadPciConfigEx(bus, device, function, offset, out output);

        /// <summary>
        /// Reads word value from the specified PCI device register
        /// </summary>
        /// <param name="bus">PCI bus number</param>
        /// <param name="device">PCI device number</param>
        /// <param name="function">PCI function number</param>
        /// <param name="offset">Register location</param>
        /// <param name="output">Word output reference</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool ReadPciConfigWordEx(byte bus, byte device, byte function, ushort offset, out ushort output) => 
            ReadPciConfigEx(bus, device, function, offset, out output);

        /// <summary>
        /// Reads dword value from the specified PCI device register
        /// </summary>
        /// <param name="bus">PCI bus number</param>
        /// <param name="device">PCI device number</param>
        /// <param name="function">PCI function number</param>
        /// <param name="offset">Register location</param>
        /// <param name="output">Dword output reference</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool ReadPciConfigDwordEx(byte bus, byte device, byte function, ushort offset, out uint output) => 
            ReadPciConfigEx(bus, device, function, offset, out output);

        /// <summary>
        /// Reads register value from the specified PCI device 
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="bus">PCI bus number</param>
        /// <param name="device">PCI device number</param>
        /// <param name="function">PCI function number</param>
        /// <param name="offset">Register location</param>
        /// <param name="output">Output reference</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static unsafe bool ReadPciConfigEx<T>(byte bus, byte device, byte function, ushort offset, out T output) {

            output = default;
            uint data = 0;
            uint bytesRead = default;

            Data.DataSize outputSize = Data.GetDataSize(output);

            // Default values
            switch (outputSize) {
                case Data.DataSize.Byte:
                    output = (T)Convert.ChangeType(byte.MaxValue, Type.GetTypeCode(typeof(T)));
                    break;
                case Data.DataSize.Word:
                    output = (T)Convert.ChangeType(ushort.MaxValue, Type.GetTypeCode(typeof(T)));
                    break;
                case Data.DataSize.Dword:
                    output = (T)Convert.ChangeType(uint.MaxValue, Type.GetTypeCode(typeof(T)));
                    break;
            }

            // Check offset alignment
            if (offset % (byte)outputSize != 0) {
                return false;
            }

            PciConfigInput pciInput = new PciConfigInput {
                Bus      = bus,
                Device   = device,
                Function = function,
                Offset   = offset,
                Data     = (IntPtr)(&data),
                Size     = (byte)outputSize
            };

            if (!KernelDriver.DeviceIoControl(IoControlCode.CPUZ_READ_PCI_CONFIG, pciInput, ref bytesRead)) {
                return false;
            }

            output = (T)Convert.ChangeType(data, Type.GetTypeCode(typeof(T)));

            return bytesRead == (byte)outputSize;
        }

        /// <summary>
        /// Writes byte to the specified PCI configuration space
        /// </summary>
        /// <param name="bus">Bus number</param>
        /// <param name="device">Device number</param>
        /// <param name="function">Function number</param>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Byte vale</param>
        private static void WritePciConfigByte(byte bus, byte device, byte function, ushort offset, byte value) => 
            WritePciConfigByteEx(bus, device, function, offset, value);

        /// <summary>
        /// Writes word to the specified PCI configuration space
        /// </summary>
        /// <param name="bus">Bus number</param>
        /// <param name="device">Device number</param>
        /// <param name="function">Function number</param>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Word vale</param>
        private static void WritePciConfigWord(byte bus, byte device, byte function, ushort offset, ushort value) => 
            WritePciConfigWordEx(bus, device, function, offset, value);

        /// <summary>
        /// Writes dword to the specified PCI configuration space
        /// </summary>
        /// <param name="bus">Bus number</param>
        /// <param name="device">Device number</param>
        /// <param name="function">Function number</param>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Dword vale</param>
        private static void WritePciConfigDword(byte bus, byte device, byte function, ushort offset, uint value) => 
            WritePciConfigDwordEx(bus, device, function, offset, value);

        /// <summary>
        /// Writes byte to the specified PCI configuration space
        /// </summary>
        /// <param name="bus">Bus number</param>
        /// <param name="device">Device number</param>
        /// <param name="function">Function number</param>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Byte vale</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool WritePciConfigByteEx(byte bus, byte device, byte function, ushort offset, byte value) => 
            WritePciConfigEx(bus, device, function, offset, value);

        /// <summary>
        /// Writes word to the specified PCI configuration space
        /// </summary>
        /// <param name="bus">Bus number</param>
        /// <param name="device">Device number</param>
        /// <param name="function">Function number</param>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Word vale</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool WritePciConfigWordEx(byte bus, byte device, byte function, ushort offset, ushort value) => 
            WritePciConfigEx(bus, device, function, offset, value);

        /// <summary>
        /// Writes dword to the specified PCI configuration space
        /// </summary>
        /// <param name="bus">Bus number</param>
        /// <param name="device">Device number</param>
        /// <param name="function">Function number</param>
        /// <param name="offset">Register offset</param>
        /// <param name="value">Dword vale</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool WritePciConfigDwordEx(byte bus, byte device, byte function, ushort offset, uint value) => 
            WritePciConfigEx(bus, device, function, offset, value);

        /// <summary>
        /// Writes data to the specified PCI configuration space
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="bus">Bus number</param>
        /// <param name="device">Device number</param>
        /// <param name="function">Function number</param>
        /// <param name="offset">Register offset</param>
        /// <param name="input">Input value</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static unsafe bool WritePciConfigEx<T>(byte bus, byte device, byte function, ushort offset, T input) {

            byte inputSize = (byte)Data.GetDataSize(typeof(T));

            // Check offset alignment
            if (offset % inputSize != 0) {
                return false;
            }

            uint data = (uint)Convert.ChangeType(input, Type.GetTypeCode(typeof(uint)));

            uint bytesWritten = default;

            PciConfigInput pciInput = new PciConfigInput {
                Bus      = bus,
                Device   = device,
                Function = function,
                Offset   = offset,
                Data     = (IntPtr)(&data),
                Size     = inputSize
            };

            if (!KernelDriver.DeviceIoControl(IoControlCode.CPUZ_WRITE_PCI_CONFIG, pciInput, ref bytesWritten)) {
                return false;
            }

            return bytesWritten == inputSize;
        }

        /// <summary>
        /// PCI config input data
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 28)]
        private struct PciConfigInput {
            [FieldOffset(0)] internal byte Bus;
            [FieldOffset(4)] internal byte Device;
            [FieldOffset(8)] internal byte Function;
            [FieldOffset(12)] internal ushort Offset;
            [FieldOffset(24)] internal IntPtr Data;
            [FieldOffset(16)] internal byte Size;
        }

        #endregion

        #region Memory

        /// <summary>
        /// Reads byte value from memory
        /// </summary>
        /// <param name="address">Memory address</param>
        /// <returns>Byte value at specified address</returns>
        private static byte ReadMemoryByte(uint address) => 
            ReadMemoryEx(address, out byte output) ? output : byte.MaxValue;

        /// <summary>
        /// Reads byte value from memory
        /// </summary>
        /// <param name="address">Memory address</param>
        /// <param name="output">Output byte reference</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool ReadMemoryByteEx(uint address, out byte output) => 
            ReadMemoryEx(address, out output);

        /// <summary>
        /// Reads word value from memory
        /// </summary>
        /// <param name="address">Memory address</param>
        /// <returns>Word value at specified address</returns>
        private static ushort ReadMemoryWord(uint address) => 
            ReadMemoryEx(address, out ushort output) ? output : ushort.MaxValue;

        /// <summary>
        /// Reads word value from memory
        /// </summary>
        /// <param name="address">Memory address</param>
        /// <param name="output">Output word reference</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool ReadMemoryWordEx(uint address, out ushort output) => 
            ReadMemoryEx(address, out output);

        /// <summary>
        /// Reads dword value from memory
        /// </summary>
        /// <param name="address">Memory address</param>
        /// <returns>Dword value at specified address</returns>
        private static uint ReadMemoryDword(uint address) => 
            ReadMemoryEx(address, out uint output) ? output : uint.MaxValue;

        /// <summary>
        /// Reads dword value from memory
        /// </summary>
        /// <param name="address">Memory address</param>
        /// <param name="output">Output dword reference</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        private static bool ReadMemoryDwordEx(uint address, out uint output) => 
            ReadMemoryEx(address, out output);

        /// <summary>
        /// Reads data from memory
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="address">Memory address</param>
        /// <param name="output">Data value</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        /// <remarks>CPUZ reads memory at 4 byte boundaries and returns 32-bit DWORD values only, regardless of address and output data type specified.
        /// The function converts values to specified data type from the output data returned by CPUZ,
        /// taking output data type size and out of dword alignment into account.</remarks>
        private static bool ReadMemoryEx<T>(uint address, out T output) {

            output = default;
            byte outputSize = (byte)Data.GetDataSize(output);

            ReadMemoryOutput[] memoryOutput = new ReadMemoryOutput[2];
            ReadMemoryInput[]  memoryInput  = new ReadMemoryInput[2];

            byte dwordSize = (byte)Data.DataSize.Dword;

            // Align address to 4 byte boundary, in case the future driver doesn't do that
            memoryInput[0].LowerAddress = address / dwordSize * dwordSize;
            
            if (!KernelDriver.DeviceIoControl(IoControlCode.CPUZ_READ_MEMORY, memoryInput[0], ref memoryOutput[0])) {
                return false;
            }

            // Dword boundary alignment check
            byte shift = (byte)(address % dwordSize);

            // Dword boundary overlap check
            if (outputSize + shift > dwordSize && shift + outputSize - dwordSize > 0) {

                // Next dword address for out of boundary data
                memoryInput[1].LowerAddress = memoryInput[0].LowerAddress + dwordSize; 

                // Read the next memory address
                if (!KernelDriver.DeviceIoControl(IoControlCode.CPUZ_READ_MEMORY, memoryInput[1], ref memoryOutput[1])) {
                    return false;
                }
            }

            memoryOutput[0].Data = memoryOutput[0].Data >> (shift * 8) | 
                                   memoryOutput[1].Data << (32 - shift * 8);

            switch (Data.GetDataSize(output)) {
                case Data.DataSize.Byte:
                    output = (T)Convert.ChangeType(
                        value    : (byte)memoryOutput[0].Data, 
                        typeCode : Type.GetTypeCode(typeof(T)));
                    break;
                case Data.DataSize.Word:
                    output = (T)Convert.ChangeType(
                        value    : (ushort)memoryOutput[0].Data, 
                        typeCode : Type.GetTypeCode(typeof(T)));
                    break;
                case Data.DataSize.Dword:
                    output = (T)Convert.ChangeType(
                        value    : memoryOutput[0].Data, 
                        typeCode : Type.GetTypeCode(typeof(T)));
                    break;
                default:
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Read memory input data
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct ReadMemoryInput {
            [FieldOffset(0)] internal uint LowerAddress;
            [FieldOffset(4)] internal uint UpperAddress; // Ignored, limiting memory space access to 32-bit addressing only
        }

        /// <summary>
        /// Read memory output data
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 4)]
        private struct ReadMemoryOutput {
            [FieldOffset(0)] internal uint Data;
        }

        #endregion
    }
}