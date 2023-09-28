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
using System.ServiceProcess;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using SpdReaderWriterCore.Driver;
using static SpdReaderWriterCore.NativeFunctions;
using static SpdReaderWriterCore.NativeFunctions.Advapi32;

namespace SpdReaderWriterCore {

    /// <summary>
    /// Kernel Driver class
    /// </summary>
    public class KernelDriver {

        /// <summary>
        /// Default driver to use
        /// </summary>
        private static readonly Info DefaultDriver = CpuZ.CpuZDriverInfo;

        /// <summary>
        /// Driver info
        /// </summary>
        public static Info DriverInfo {
            get => _driverInfo;
            set {
                _driverInfo = value;

                // Driver pre-init
                if (!_driverInfo.Setup()) {
                    throw new Exception($"{_driverInfo.ServiceName} setup failed");
                }

                // Check if all functions are present
                if (_driverInfo.GetDriverVersion      == null) { throw new MissingMethodException(nameof(GetDriverVersionDelegate)); }
                if (_driverInfo.ReadIoPortByte        == null) { throw new MissingMethodException(nameof(ReadIoPortByteDelegate)); }
                if (_driverInfo.ReadIoPortByteEx      == null) { throw new MissingMethodException(nameof(ReadIoPortByteExDelegate)); }
                if (_driverInfo.ReadIoPortWord        == null) { throw new MissingMethodException(nameof(ReadIoPortWordDelegate)); }
                if (_driverInfo.ReadIoPortWordEx      == null) { throw new MissingMethodException(nameof(ReadIoPortWordExDelegate)); }
                if (_driverInfo.ReadIoPortDword       == null) { throw new MissingMethodException(nameof(ReadIoPortDwordDelegate)); }
                if (_driverInfo.ReadIoPortDwordEx     == null) { throw new MissingMethodException(nameof(ReadIoPortDwordExDelegate)); }
                if (_driverInfo.WriteIoPortByte       == null) { throw new MissingMethodException(nameof(WriteIoPortByteDelegate)); }
                if (_driverInfo.WriteIoPortByteEx     == null) { throw new MissingMethodException(nameof(WriteIoPortByteExDelegate)); }
                if (_driverInfo.WriteIoPortWord       == null) { throw new MissingMethodException(nameof(WriteIoPortWordDelegate)); }
                if (_driverInfo.WriteIoPortWordEx     == null) { throw new MissingMethodException(nameof(WriteIoPortWordExDelegate)); }
                if (_driverInfo.WriteIoPortDword      == null) { throw new MissingMethodException(nameof(WriteIoPortDwordDelegate)); }
                if (_driverInfo.WriteIoPortDwordEx    == null) { throw new MissingMethodException(nameof(WriteIoPortDwordExDelegate)); }
                if (_driverInfo.ReadPciConfigByte     == null) { throw new MissingMethodException(nameof(ReadPciConfigByteDelegate)); }
                if (_driverInfo.ReadPciConfigByteEx   == null) { throw new MissingMethodException(nameof(ReadPciConfigByteExDelegate)); }
                if (_driverInfo.ReadPciConfigWord     == null) { throw new MissingMethodException(nameof(ReadPciConfigWordDelegate)); }
                if (_driverInfo.ReadPciConfigWordEx   == null) { throw new MissingMethodException(nameof(ReadPciConfigWordExDelegate)); }
                if (_driverInfo.ReadPciConfigDword    == null) { throw new MissingMethodException(nameof(ReadPciConfigDwordDelegate)); }
                if (_driverInfo.ReadPciConfigDwordEx  == null) { throw new MissingMethodException(nameof(ReadPciConfigDwordExDelegate)); }
                if (_driverInfo.WritePciConfigByte    == null) { throw new MissingMethodException(nameof(WritePciConfigByteDelegate)); }
                if (_driverInfo.WritePciConfigByteEx  == null) { throw new MissingMethodException(nameof(WritePciConfigByteExDelegate)); }
                if (_driverInfo.WritePciConfigWord    == null) { throw new MissingMethodException(nameof(WritePciConfigWordDelegate)); }
                if (_driverInfo.WritePciConfigWordEx  == null) { throw new MissingMethodException(nameof(WritePciConfigWordExDelegate)); }
                if (_driverInfo.WritePciConfigDword   == null) { throw new MissingMethodException(nameof(WritePciConfigDwordDelegate)); }
                if (_driverInfo.WritePciConfigDwordEx == null) { throw new MissingMethodException(nameof(WritePciConfigDwordExDelegate)); }
                if (_driverInfo.ReadMemoryByte        == null) { throw new MissingMethodException(nameof(ReadMemoryByteDelegate)); }
                if (_driverInfo.ReadMemoryByteEx      == null) { throw new MissingMethodException(nameof(ReadMemoryByteDelegateEx)); }
                if (_driverInfo.ReadMemoryWord        == null) { throw new MissingMethodException(nameof(ReadMemoryWordDelegate)); }
                if (_driverInfo.ReadMemoryWordEx      == null) { throw new MissingMethodException(nameof(ReadMemoryWordDelegateEx)); }
                if (_driverInfo.ReadMemoryDword       == null) { throw new MissingMethodException(nameof(ReadMemoryDwordDelegate)); }
                if (_driverInfo.ReadMemoryDwordEx     == null) { throw new MissingMethodException(nameof(ReadMemoryDwordDelegateEx)); }
            }
        }

        /// <summary>
        /// Kernel Driver Info struct
        /// </summary>
        public struct Info {
            /// <summary>
            /// Driver service name
            /// </summary>
            public string ServiceName;

            /// <summary>
            /// Driver file name
            /// </summary>
            public string FileName => $"{DriverInfo.ServiceName}{(Environment.Is64BitOperatingSystem ? "x64" : "")}.sys";

            /// <summary>
            /// NT Device name
            /// </summary>
            public string DeviceName;

            /// <summary>
            /// Driver file path
            /// </summary>
            public string FilePath => $"{Core.ParentPath}\\{DriverInfo.FileName}";

            /// <summary>
            /// Binary driver file contents
            /// </summary>
            public byte[] BinaryData => Data.Gzip(DriverInfo.GZipData, Data.GzipMethod.Decompress);

            /// <summary>
            /// Compressed driver file contents
            /// </summary>
            public byte[] GZipData;

            /// <summary>
            /// Driver specific setup procedure
            /// </summary>
            public SetupDelegate Setup { get; set; }

            public GetDriverVersionDelegate GetDriverVersion { get; set; }

            public ReadIoPortByteDelegate ReadIoPortByte { get; set; }
            public ReadIoPortByteExDelegate ReadIoPortByteEx { get; set; }
            public ReadIoPortWordDelegate ReadIoPortWord { get; set; }
            public ReadIoPortWordExDelegate ReadIoPortWordEx { get; set; }
            public ReadIoPortDwordDelegate ReadIoPortDword { get; set; }
            public ReadIoPortDwordExDelegate ReadIoPortDwordEx { get; set; }
            public WriteIoPortByteDelegate WriteIoPortByte { get; set; }
            public WriteIoPortByteExDelegate WriteIoPortByteEx { get; set; }
            public WriteIoPortWordDelegate WriteIoPortWord { get; set; }
            public WriteIoPortWordExDelegate WriteIoPortWordEx { get; set; }
            public WriteIoPortDwordDelegate WriteIoPortDword { get; set; }
            public WriteIoPortDwordExDelegate WriteIoPortDwordEx { get; set; }
            public ReadPciConfigByteDelegate ReadPciConfigByte { get; set; }
            public ReadPciConfigByteExDelegate ReadPciConfigByteEx { get; set; }
            public ReadPciConfigWordDelegate ReadPciConfigWord { get; set; }
            public ReadPciConfigWordExDelegate ReadPciConfigWordEx { get; set; }
            public ReadPciConfigDwordDelegate ReadPciConfigDword { get; set; }
            public ReadPciConfigDwordExDelegate ReadPciConfigDwordEx { get; set; }
            public WritePciConfigByteDelegate WritePciConfigByte { get; set; }
            public WritePciConfigByteExDelegate WritePciConfigByteEx { get; set; }
            public WritePciConfigWordDelegate WritePciConfigWord { get; set; }
            public WritePciConfigWordExDelegate WritePciConfigWordEx { get; set; }
            public WritePciConfigDwordDelegate WritePciConfigDword { get; set; }
            public WritePciConfigDwordExDelegate WritePciConfigDwordEx { get; set; }
            public ReadMemoryByteDelegate ReadMemoryByte { get; set; }
            public ReadMemoryByteDelegateEx ReadMemoryByteEx { get; set; }
            public ReadMemoryWordDelegate ReadMemoryWord { get; set; }
            public ReadMemoryWordDelegateEx ReadMemoryWordEx { get; set; }
            public ReadMemoryDwordDelegate ReadMemoryDword { get; set; }
            public ReadMemoryDwordDelegateEx ReadMemoryDwordEx { get; set; }
        }

        #region Delegates

        // Setup
        public delegate bool SetupDelegate();

        // Info
        public delegate uint GetDriverVersionDelegate(out byte major, out byte minor, out byte revision, out byte release);

        // Read IO
        public delegate byte ReadIoPortByteDelegate(ushort port);
        public delegate bool ReadIoPortByteExDelegate(ushort port, out byte output);
        public delegate ushort ReadIoPortWordDelegate(ushort port);
        public delegate bool ReadIoPortWordExDelegate(ushort port, out ushort output);
        public delegate uint ReadIoPortDwordDelegate(ushort port);
        public delegate bool ReadIoPortDwordExDelegate(ushort port, out uint output);

        // Write IO
        public delegate void WriteIoPortByteDelegate(ushort port, byte value);
        public delegate bool WriteIoPortByteExDelegate(ushort port, byte value);
        public delegate void WriteIoPortWordDelegate(ushort port, ushort value);
        public delegate bool WriteIoPortWordExDelegate(ushort port, ushort value);
        public delegate void WriteIoPortDwordDelegate(ushort port, uint value);
        public delegate bool WriteIoPortDwordExDelegate(ushort port, uint value);

        // Read PCI
        public delegate byte ReadPciConfigByteDelegate(byte bus, byte device, byte function, ushort offset);
        public delegate bool ReadPciConfigByteExDelegate(byte bus, byte device, byte function, ushort offset, out byte output);
        public delegate ushort ReadPciConfigWordDelegate(byte bus, byte device, byte function, ushort offset);
        public delegate bool ReadPciConfigWordExDelegate(byte bus, byte device, byte function, ushort offset, out ushort output);
        public delegate uint ReadPciConfigDwordDelegate(byte bus, byte device, byte function, ushort offset);
        public delegate bool ReadPciConfigDwordExDelegate(byte bus, byte device, byte function, ushort offset, out uint output);

        // Write PCI
        public delegate void WritePciConfigByteDelegate(byte bus, byte device, byte function, ushort offset, byte value);
        public delegate bool WritePciConfigByteExDelegate(byte bus, byte device, byte function, ushort offset, byte value);
        public delegate void WritePciConfigWordDelegate(byte bus, byte device, byte function, ushort offset, ushort value);
        public delegate bool WritePciConfigWordExDelegate(byte bus, byte device, byte function, ushort offset, ushort value);
        public delegate void WritePciConfigDwordDelegate(byte bus, byte device, byte function, ushort offset, uint value);
        public delegate bool WritePciConfigDwordExDelegate(byte bus, byte device, byte function, ushort offset, uint value);

        // Read memory
        public delegate byte ReadMemoryByteDelegate(uint address);
        public delegate bool ReadMemoryByteDelegateEx(uint address, out byte output);
        public delegate ushort ReadMemoryWordDelegate(uint address);
        public delegate bool ReadMemoryWordDelegateEx(uint address, out ushort output);
        public delegate uint ReadMemoryDwordDelegate(uint address);
        public delegate bool ReadMemoryDwordDelegateEx(uint address, out uint output);

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

            object outputData;
            bool result;

            if (Data.GetDataSize(typeof(T)) == Data.DataSize.Byte) {
                result = DriverInfo.ReadIoPortByteEx(port, out byte buffer);
                outputData = buffer;
            }
            else if (Data.GetDataSize(typeof(T)) == Data.DataSize.Word) {
                result = DriverInfo.ReadIoPortWordEx(port, out ushort buffer);
                outputData = buffer;
            }
            else if (Data.GetDataSize(typeof(T)) == Data.DataSize.Dword) {
                result = DriverInfo.ReadIoPortDwordEx(port, out uint buffer);
                outputData = buffer;
            }
            else {
                throw new InvalidDataException($"{MethodBase.GetCurrentMethod()?.Name}:{typeof(T)}");
            }

            output = (T)outputData;
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

            if (Data.GetDataSize(input) == Data.DataSize.Byte) {
                return DriverInfo.WriteIoPortByteEx(port, (byte)input);
            }

            if (Data.GetDataSize(input) == Data.DataSize.Word) {
                return DriverInfo.WriteIoPortWordEx(port, (ushort)input);
            }

            if (Data.GetDataSize(input) == Data.DataSize.Dword) {
                return DriverInfo.WriteIoPortDwordEx(port, (uint)input);
            }

            return false;
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

            object output = null;

            if (Data.GetDataSize(typeof(T)) == Data.DataSize.Byte) {
                output = DriverInfo.ReadPciConfigByte(bus, device, function, offset);
            }
            else if (Data.GetDataSize(typeof(T)) == Data.DataSize.Word) {
                output = DriverInfo.ReadPciConfigWord(bus, device, function, offset);
            }
            else if (Data.GetDataSize(typeof(T)) == Data.DataSize.Dword) {
                output = DriverInfo.ReadPciConfigDword(bus, device, function, offset);
            }

            if (output != null) {
                return (T)Convert.ChangeType(output, Type.GetTypeCode(typeof(T)));
            }

            throw new InvalidDataException($"{MethodBase.GetCurrentMethod()?.Name}:{typeof(T)}");
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

            object outputData;
            bool result;

            if (Data.GetDataSize(typeof(T)) == Data.DataSize.Byte) {
                result = DriverInfo.ReadPciConfigByteEx(bus, device, function, offset, out byte buffer);
                outputData = buffer;
            }
            else if (Data.GetDataSize(typeof(T)) == Data.DataSize.Word) {
                result = DriverInfo.ReadPciConfigWordEx(bus, device, function, offset, out ushort buffer);
                outputData = buffer;
            }
            else if (Data.GetDataSize(typeof(T)) == Data.DataSize.Dword) {
                result = DriverInfo.ReadPciConfigDwordEx(bus, device, function, offset, out uint buffer);
                outputData = buffer;
            }
            else {
                throw new InvalidDataException($"{MethodBase.GetCurrentMethod()?.Name}:{typeof(T)}");
            }

            output = (T)outputData;
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

            if (Data.GetDataSize(typeof(T)) == Data.DataSize.Byte) {
                return DriverInfo.WritePciConfigByteEx(bus, device, function, offset, (byte)input);
            }

            if (Data.GetDataSize(typeof(T)) == Data.DataSize.Word) {
                return DriverInfo.WritePciConfigWordEx(bus, device, function, offset, (ushort)input);
            }
            if (Data.GetDataSize(typeof(T)) == Data.DataSize.Dword) {
                return DriverInfo.WritePciConfigDwordEx(bus, device, function, offset, (uint)input);
            }

            throw new InvalidDataException($"{MethodBase.GetCurrentMethod()?.Name}:{typeof(T)}");
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
            
            object outputData;
            bool result;

            if (Data.GetDataSize(typeof(T)) == Data.DataSize.Byte) {
                result = DriverInfo.ReadMemoryByteEx(address, out byte buffer);
                outputData = buffer;
            }
            else if (Data.GetDataSize(typeof(T)) == Data.DataSize.Word) {
                result = DriverInfo.ReadMemoryWordEx(address, out ushort buffer);
                outputData = buffer;
            }
            else if (Data.GetDataSize(typeof(T)) == Data.DataSize.Dword) {
                result = DriverInfo.ReadMemoryDwordEx(address, out uint buffer);
                outputData = buffer;
            }
            else {
                throw new InvalidDataException($"{MethodBase.GetCurrentMethod()?.Name}:{typeof(T)}");
            }

            output = (T)outputData;
            return result;
        }

        #endregion

        /// <summary>
        /// Retrieves driver version
        /// </summary>
        /// <param name="major">Major version number</param>
        /// <param name="minor">Minor version number</param>
        /// <param name="revision">Revision number</param>
        /// <param name="release">Release number</param>
        /// <returns>Driver version</returns>
        public static uint GetDriverVersion(out byte major, out byte minor, out byte revision, out byte release) {
            return DriverInfo.GetDriverVersion(out major, out minor, out revision, out release);
        }

        /// <summary>
        /// Starts kernel driver
        /// </summary>
        /// <returns></returns>
        public static bool Start() {
            DriverInfo = DefaultDriver;
            return Initialize();
        }

        /// <summary>
        /// Stops kernel driver
        /// </summary>
        /// <returns></returns>
        public static bool Stop() {
            Dispose();
            return !IsRunning;
        }

        /// <summary>
        /// Initializes driver
        /// </summary>
        /// <returns><see langword="true"/> if driver file is successfully initialized</returns>
        private static bool Initialize() {

            if (IsInstalled && IsRunning) {
                _disposeOnExit = false;
                return true;
            }

            if (!(IsInstalled || InstallDriver())) {
                throw new Exception($"Unable to install {DriverInfo.ServiceName} service");
            }

            if (!(IsRunning || StartDriver())) {
                throw new Exception($"Unable to start {DriverInfo.ServiceName} service");
            }

            return IsInstalled && IsRunning;
        }

        /// <summary>
        /// Deinitializes kernel driver instance
        /// </summary>
        public static void Dispose() {

            _driverHandle.Close();

            if (_disposeOnExit) {
                StopDriver();
                RemoveDriver(deleteFile: false);
            }

            CloseServiceHandle(_serviceHandle);
            CloseServiceHandle(ManagerHandle);
        }

        /// <summary>
        /// Extracts driver file from resources and saves it to a local file
        /// </summary>
        /// <returns><see langword="true"/> if driver file is successfully extracted</returns>
        private static bool ExtractDriver() {

            if (!(File.Exists(DriverInfo.FilePath) && 
                  Data.CompareArray(DriverInfo.BinaryData, File.ReadAllBytes(DriverInfo.FilePath)))) {

                // Save driver to local file
                try {
                    File.WriteAllBytes(DriverInfo.FilePath, DriverInfo.BinaryData);
                }
                catch {
                    return false;
                }
            }

            return File.Exists(DriverInfo.FilePath) && 
                   Data.CompareArray(DriverInfo.BinaryData, File.ReadAllBytes(DriverInfo.FilePath));
        }

        /// <summary>
        /// Installs kernel driver
        /// </summary>
        /// <returns><see langword="true"/> if the driver is successfully installed</returns>
        private static bool InstallDriver() {

            if (IsInstalled) {
                return true;
            }

            if (!ExtractDriver()) {
                return false;
            }

            if (ManagerHandle == IntPtr.Zero) {
                return false;
            }

            _serviceHandle = CreateService(
                hSCManager       : ManagerHandle,
                lpServiceName    : DriverInfo.ServiceName,
                lpDisplayName    : DriverInfo.ServiceName,
                dwDesiredAccess  : ServiceAccessRights.SC_MANAGER_ALL_ACCESS,
                dwServiceType    : ServiceType.KernelDriver,
                dwStartType      : ServiceStartMode.Manual,
                dwErrorControl   : ErrorControl.Normal,
                lpBinaryPathName : DriverInfo.FilePath);

            if (_serviceHandle == IntPtr.Zero) {
                return false;
            }

            CloseServiceHandle(_serviceHandle);

            return true;
        }

        /// <summary>
        /// Deletes kernel driver service
        /// </summary>
        /// <returns><see langword="true"/> if driver is successfully deleted</returns>
        private static bool RemoveDriver() {

            if (ManagerHandle == IntPtr.Zero) {
                return false;
            }

            _serviceHandle = OpenService(ManagerHandle, DriverInfo.ServiceName, ServiceRights.ServiceAllAccess);

            if (_serviceHandle == IntPtr.Zero) {
                return false;
            }

            return DeleteService(_serviceHandle) &&
                   CloseServiceHandle(_serviceHandle);
        }

        /// <summary>
        /// Deletes kernel driver service and driver file
        /// </summary>
        /// <param name="deleteFile">Set to <see langword="true"/> to delete driver file, or <see langword="false"/> to keep it</param>
        /// <returns><see langword="true"/> if the driver service and the driver file are successfully deleted</returns>
        private static bool RemoveDriver(bool deleteFile) {

            if (!deleteFile) {
                return RemoveDriver();
            }

            if (!RemoveDriver()) {
                return false;
            }

            try {
                File.Delete(DriverInfo.FilePath);
                return !File.Exists(DriverInfo.FilePath);
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Starts kernel driver
        /// </summary>
        /// <returns><see langword="true"/> if driver is successfully started</returns>
        private static bool StartDriver() {

            if (!IsInstalled) {
                return false;
            }

            if (IsRunning) {
                return true;
            }

            try {
                _service = new ServiceController(DriverInfo.ServiceName);

                if (_service.Status == ServiceControllerStatus.Running) {
                    return true;
                }

                _service.Start();
                _service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(Timeout));
                return _service.Status == ServiceControllerStatus.Running;
            }
            catch {
                try {
                    return _service.Status == ServiceControllerStatus.Running;
                }
                catch {
                    return false;
                }
            }
        }

        /// <summary>
        /// Stops kernel driver
        /// </summary>
        /// <returns><see langword="true"/> if driver is successfully stopped</returns>
        private static bool StopDriver() {

            _service = new ServiceController(DriverInfo.ServiceName);

            if (_service.Status != ServiceControllerStatus.Stopped) {
                _service.Stop();
            }

            _service.Refresh();

            return _service.Status == ServiceControllerStatus.Stopped ||
                   _service.Status == ServiceControllerStatus.StopPending;
        }

        /// <summary>
        /// Opens driver handle
        /// </summary>
        /// <returns><see langword="true"/> if driver handle is successfully opened</returns>
        private static bool OpenDriverHandle(out IntPtr handle) {

            int status = 0;
            handle = IntPtr.Zero;

            _sw.Restart();

            while (_sw.ElapsedMilliseconds < Timeout) {
                handle = Kernel32.CreateFile(
                    lpFileName            : DriverInfo.DeviceName,
                    dwDesiredAccess       : Kernel32.FileAccess.GenericRead,
                    dwShareMode           : FileShare.Read,
                    lpSecurityAttributes  : IntPtr.Zero,
                    dwCreationDisposition : FileMode.Open,
                    dwFlagsAndAttributes  : 0);

                status = Marshal.GetLastWin32Error();

                if (status != SystemError.ErrorSuccess) {
                    Thread.Sleep(1);
                    continue;
                }

                _sw.Stop();
                break;
            }

            return status == SystemError.ErrorSuccess;
        }

        /// <summary>
        /// Closes kernel driver
        /// </summary>
        /// <returns><see langword="true"/> if driver handle is successfully closed</returns>
        private static bool CloseDriverHandle() {
            
            _driverHandle.Close();
            return !IsHandleOpen;
        }

        /// <summary>
        /// Describes driver installation state
        /// </summary>
        public static bool IsInstalled {
            get {
                try {
                    if (_service == null) {
                        _service = new ServiceController(DriverInfo.ServiceName);
                    }
                    _service.Refresh();
                    return _service.ServiceType == ServiceType.KernelDriver;
                }
                catch {
                    return false;
                }
            }
        }

        /// <summary>
        /// Describes driver running state
        /// </summary>
        public static bool IsRunning {
            get {
                try {
                    if (_service == null) {
                        _service = new ServiceController(DriverInfo.ServiceName);
                    }
                    _service.Refresh();
                    return _service.Status == ServiceControllerStatus.Running;
                }
                catch {
                    return false;
                }
            }
        }

        /// <summary>
        /// Describes driver handle open state
        /// </summary>
        private static bool IsHandleOpen => _driverHandle != null && !_driverHandle.IsClosed;

        /// <summary>
        /// Writes data to device
        /// </summary>
        /// <param name="ioControlCode">IOCTL code</param>
        /// <param name="inputData">Input parameters</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        public static bool DeviceIoControl(uint ioControlCode, object inputData) {
            object _ = null;
            return DeviceIoControl(ioControlCode, inputData, ref _);
        }

        /// <summary>
        /// Reads data from device
        /// </summary>
        /// <typeparam name="T">Output data type</typeparam>
        /// <param name="ioControlCode">IOCTL code</param>
        /// <param name="inputData">Input parameters</param>
        /// <param name="outputData">Output data returned by the driver</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        public static bool DeviceIoControl<T>(uint ioControlCode, object inputData, ref T outputData) {

            uint inputSize = (uint)(inputData == null ? 0 : Marshal.SizeOf(inputData));
            object outputBuffer = outputData;
            IntPtr deviceHandle = default;

            _sw.Restart();

            while (_sw.ElapsedMilliseconds < Timeout && !OpenDriverHandle(out deviceHandle)) {
                KeepAlive();
            }

            _sw.Stop();

            if (_sw.ElapsedMilliseconds >= Timeout) {
                throw new Exception($"Unable to open {DriverInfo.ServiceName} handle");
            }

            if (_driverHandle == null ||
                _driverHandle.IsClosed ||
                _driverHandle.IsInvalid) {
                _driverHandle = new SafeFileHandle(deviceHandle, true);
            }

            if (_driverHandle.IsInvalid) {
                return false;
            }

            bool result = Kernel32.DeviceIoControl(
                hDevice         : _driverHandle,
                dwIoControlCode : ioControlCode,
                lpInBuffer      : inputData,
                nInBufferSize   : inputSize,
                lpOutBuffer     : outputBuffer,
                nOutBufferSize  : (uint)Marshal.SizeOf(outputBuffer),
                lpBytesReturned : out uint returnedLength,
                lpOverlapped    : IntPtr.Zero);

            if (result) {
                outputData = (T)outputBuffer;
            }

            CloseDriverHandle();

            return result;
        }

        /// <summary>
        /// Ensures the driver keeps running
        /// </summary>
        private static void KeepAlive() {
            if (!IsInstalled) {
                InstallDriver();
            }

            if (!IsRunning) {
                StartDriver();
            }
        }

        /// <summary>
        /// Service operation timeout
        /// </summary>
        private const int Timeout = 1000;

        /// <summary>
        /// Performance and timeout monitor
        /// </summary>
        private static Stopwatch _sw = new Stopwatch();

        /// <summary>
        /// Driver info instance
        /// </summary>
        private static Info _driverInfo;

        /// <summary>
        /// Indicates whether the driver service should be stopped and deleted on exit
        /// </summary>
        private static bool _disposeOnExit = true;

        /// <summary>
        /// Service controller for the driver
        /// </summary>
        private static ServiceController _service;

        /// <summary>
        /// Service control manager
        /// </summary>
        private static IntPtr ManagerHandle => OpenSCManager(dwAccess: ServiceAccessRights.SC_MANAGER_ALL_ACCESS);

        /// <summary>
        /// Service object
        /// </summary>
        private static IntPtr _serviceHandle;

        /// <summary>
        /// IO device driver handle
        /// </summary>
        private static SafeFileHandle _driverHandle;
    }
}