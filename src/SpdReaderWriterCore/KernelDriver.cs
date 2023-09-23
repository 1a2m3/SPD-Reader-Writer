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
                    throw new Exception($"{_driverInfo.Name} setup failed");
                }

                // Check if all functions are present
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

                Initialize();
            }
        }

        /// <summary>
        /// Kernel Driver Info struct
        /// </summary>
        public struct Info {
            /// <summary>
            /// Driver service name
            /// </summary>
            public string Name;

            /// <summary>
            /// Driver file name
            /// </summary>
            public string FileName => $"{DriverInfo.Name}.sys";

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
        }

        #region Delegates

        // Setup
        public delegate bool SetupDelegate();

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

        /// <summary>
        /// Starts kernel driver
        /// </summary>
        /// <returns></returns>
        public static bool Start() {

            DriverInfo = DefaultDriver;
            return IsReady;
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

            _serviceController = new ServiceController(DriverInfo.Name);

            if (IsInstalled && IsRunning) {
                _disposeOnExit = false;
            }
            else {
                if (!(IsInstalled || InstallDriver())) {
                    throw new Exception($"Unable to install {DriverInfo.Name} service");
                }

                if (!(IsRunning || StartDriver())) {
                    throw new Exception($"Unable to start {DriverInfo.Name} service");
                }
            }

            return true;
        }

        /// <summary>
        /// Deinitializes kernel driver instance
        /// </summary>
        public static void Dispose() {

            CloseDriverHandle();

            if (_disposeOnExit) {
                StopDriver();
                RemoveDriver(deleteFile: false);
            }

            _driverHandle = null;

            Advapi32.CloseServiceHandle(_serviceHandle);
            Advapi32.CloseServiceHandle(ManagerPtr);
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

            return File.Exists(DriverInfo.FilePath) && Data.CompareArray(DriverInfo.BinaryData, File.ReadAllBytes(DriverInfo.FilePath));
        }

        /// <summary>
        /// Installs kernel driver
        /// </summary>
        /// <returns><see langword="true"/> if the driver is successfully installed</returns>
        private static bool InstallDriver() {

            if (!ExtractDriver()) {
                return false;
            }

            if (ManagerPtr == IntPtr.Zero) {
                return false;
            }

            _serviceHandle = Advapi32.CreateService(
                hSCManager       : ManagerPtr,
                lpServiceName    : DriverInfo.Name,
                lpDisplayName    : DriverInfo.Name,
                dwDesiredAccess  : Advapi32.ServiceAccessRights.SC_MANAGER_ALL_ACCESS, //(Advapi32.ServiceAccessRights)0xF01FF,
                dwServiceType    : ServiceType.KernelDriver,
                dwStartType      : ServiceStartMode.Manual,
                dwErrorControl   : Advapi32.ErrorControl.Normal,
                lpBinaryPathName : DriverInfo.FilePath);

            if (_serviceHandle == IntPtr.Zero) {
                return false;
            }

            Advapi32.CloseServiceHandle(_serviceHandle);

            return true;
        }

        /// <summary>
        /// Deletes kernel driver service
        /// </summary>
        /// <returns><see langword="true"/> if driver is successfully deleted</returns>
        private static bool RemoveDriver() {

            if (ManagerPtr == IntPtr.Zero) {
                return false;
            }

            _serviceHandle = Advapi32.OpenService(ManagerPtr, DriverInfo.Name, Advapi32.ServiceRights.ServiceAllAccess);

            if (_serviceHandle == IntPtr.Zero) {
                return false;
            }

            return Advapi32.DeleteService(_serviceHandle) &&
                   Advapi32.CloseServiceHandle(_serviceHandle);
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

            try {
                if (_serviceController.Status == ServiceControllerStatus.Running) {
                    return true;
                }

                _serviceController.Start();
                _serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(Timeout));
                return _serviceController.Status == ServiceControllerStatus.Running;
            }
            catch {
                try {
                    return _serviceController.Status == ServiceControllerStatus.Running;
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

            try {
                if (_serviceController.Status != ServiceControllerStatus.Stopped) {

                    _serviceController.Stop();

                    // Wait for Stopped or StopPending
                    _sw.Restart();

                    while (_sw.ElapsedMilliseconds < Timeout) {

                        _serviceController.Refresh();

                        if (_serviceController.Status == ServiceControllerStatus.Stopped ||
                            _serviceController.Status == ServiceControllerStatus.StopPending) {
                            return true;
                        }
                    }
                }

                return _serviceController.Status == ServiceControllerStatus.Stopped ||
                       _serviceController.Status == ServiceControllerStatus.StopPending;
            }
            catch {
                try {
                    return _serviceController.Status == ServiceControllerStatus.Stopped ||
                           _serviceController.Status == ServiceControllerStatus.StopPending;
                }
                catch {
                    return true;
                }
            }
            finally {
                _sw.Stop();
            }
        }

        /// <summary>
        /// Checks if the driver is installed
        /// </summary>
        /// <returns><see langword="true"/> if the driver is installed</returns>
        private static bool CheckDriver() {

            try {
                _serviceController = new ServiceController(DriverInfo.Name);

                return _serviceController?.ServiceType == ServiceType.KernelDriver && 
                       _serviceController?.DisplayName == DriverInfo.Name;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Opens driver handle
        /// </summary>
        /// <returns><see langword="true"/> if driver handle is successfully opened</returns>
        private static bool OpenDriverHandle() {

            IntPtr driverHandle = IntPtr.Zero;

            _sw.Restart();

            while (_sw.ElapsedMilliseconds < Timeout) {
                driverHandle = Kernel32.CreateFile(
                    lpFileName            : DriverInfo.DeviceName,
                    dwDesiredAccess       : FileAccess.Read,
                    dwShareMode           : FileShare.Read,
                    lpSecurityAttributes  : IntPtr.Zero,
                    dwCreationDisposition : FileMode.Open,
                    dwFlagsAndAttributes  : 0);

                if ((uint)driverHandle == InvalidHandle || 
                    driverHandle == IntPtr.Zero) {
                    Thread.Sleep(1);
                }
                else {
                    _sw.Stop();
                    break;
                }
            }

            int error = Marshal.GetLastWin32Error();

            if ((uint)driverHandle == InvalidHandle) {
                //throw new AccessViolationException($"Could not open {DefaultDriver.DeviceName} handle");
                return false;
            }

            _driverHandle = new SafeFileHandle(driverHandle, true);

            if (_driverHandle.IsInvalid) {
                CloseDriverHandle();
            }

            return IsValid;
        }

        /// <summary>
        /// Closes kernel driver
        /// </summary>
        /// <returns><see langword="true"/> if driver handle is successfully closed</returns>
        private static bool CloseDriverHandle() {
            if (IsHandleOpen && !IsHandleLocked) {
                _driverHandle.Close();
                _driverHandle.Dispose();
            }

            return !IsHandleOpen && !IsHandleLocked;
        }

        /// <summary>
        /// Locks driver handle, preventing it from being closed with <see cref="CloseDriverHandle"/>
        /// </summary>
        public static void LockDriverHandle() {
            _handleLock = true;
        }

        /// <summary>
        /// Releases driver handle lock, allowing it to be closed with <see cref="CloseDriverHandle"/>
        /// </summary>
        public static void UnlockDriverHandle() {
            _handleLock = false;
        }

        /// <summary>
        /// Describes driver installation state
        /// </summary>
        public static bool IsInstalled => CheckDriver();

        /// <summary>
        /// Describes driver running state
        /// </summary>
        public static bool IsRunning {
            get {
                try {
                    _serviceController?.Refresh();
                    return IsInstalled && _serviceController?.Status == ServiceControllerStatus.Running;
                }
                catch {
                    return false;
                }
            }
        }

        /// <summary>
        /// Describes driver handle lock state
        /// </summary>
        public static bool IsHandleLocked => _handleLock;

        /// <summary>
        /// Describes driver handle valid state
        /// </summary>
        public static bool IsValid => IsInstalled;

        /// <summary>
        /// Describes driver ready state
        /// </summary>
        public static bool IsReady => IsValid && IsRunning;

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

            if (!IsReady) {
                return false;
            }

            uint inputSize = (uint)(inputData == null ? 0 : Marshal.SizeOf(inputData));
            uint returnedLength = default;
            object outputBuffer = outputData;

            if (!(IsHandleOpen || OpenDriverHandle())) {
                throw new Exception($"Unable to open {DriverInfo.Name} handle");
            }

            bool result = Kernel32.DeviceIoControl(
                hDevice         : _driverHandle,
                dwIoControlCode : ioControlCode,
                lpInBuffer      : inputData,
                nInBufferSize   : inputSize,
                lpOutBuffer     : outputBuffer,
                nOutBufferSize  : (uint)Marshal.SizeOf(outputBuffer),
                lpBytesReturned : out returnedLength,
                lpOverlapped    : IntPtr.Zero);

            outputData = (T)outputBuffer;

            CloseDriverHandle();

            return result;
        }

        /// <summary>
        /// Windows NT Kernel BASE API
        /// </summary>
        public static class Kernel32 {

            /// <summary>
            /// Creates or opens a file or I/O device.
            /// </summary>
            /// <param name="lpFileName">The name of the file or device to be created or opened.</param>
            /// <param name="dwDesiredAccess">The requested access to the file or device, which can be of <see cref="FileAccess"/> values.</param>
            /// <param name="dwShareMode">The requested sharing mode of the file or device, which can be of <see cref="FileShare"/> values.</param>
            /// <param name="lpSecurityAttributes">A pointer to optional SECURITY_ATTRIBUTES structure or IntPtr.Zero</param>
            /// <param name="dwCreationDisposition">An action to take on a file or device that exists or does not exist.
            /// For devices other than files, this parameter is usually set to <see cref="FileMode.Open"/>.</param>
            /// <param name="dwFlagsAndAttributes">The file or device attributes and flags.</param>
            /// <param name="hTemplateFile">A valid handle to a template file with the <see cref="FileAccess.Read"/> access right.</param>
            /// <returns>If the function succeeds, the return value is an open handle to the specified file, device, named pipe, or mail slot.</returns>
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr CreateFile(
                [MarshalAs(UnmanagedType.LPTStr)] string lpFileName,
                [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
                [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
                [Optional] IntPtr lpSecurityAttributes,
                [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
                [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
                [Optional] IntPtr hTemplateFile);

            /// <summary>
            /// Sends a control code directly to a specified device driver, causing the corresponding device to perform the corresponding operation.
            /// </summary>
            /// <param name="hDevice">A handle to the device on which the operation is to be performed.</param>
            /// <param name="dwIoControlCode">The control code for the operation.
            /// This value identifies the specific operation to be performed and the type of device on which to perform it.</param>
            /// <param name="lpInBuffer">A pointer to the input buffer that contains the data required to perform the operation.</param>
            /// <param name="nInBufferSize">The size of the input buffer, in bytes.</param>
            /// <param name="lpOutBuffer">A pointer to the output buffer that is to receive the data returned by the operation.</param>
            /// <param name="nOutBufferSize">The size of the output buffer, in bytes.</param>
            /// <param name="lpBytesReturned">A pointer to a variable that receives the size of the data stored in the output buffer, in bytes.</param>
            /// <param name="lpOverlapped">A pointer to an OVERLAPPED structure.</param>
            /// <returns>If the operation completes successfully, the return value is nonzero (<see langword="true"/>).
            /// If the operation fails or is pending, the return value is zero.</returns>
            [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool DeviceIoControl(
                SafeFileHandle hDevice,
                uint dwIoControlCode,
                [MarshalAs(UnmanagedType.AsAny)][In] object lpInBuffer,
                uint nInBufferSize,
                [MarshalAs(UnmanagedType.AsAny)][Out] object lpOutBuffer,
                uint nOutBufferSize,
                [Optional] out uint lpBytesReturned,
                [Optional] IntPtr lpOverlapped);

            /// <summary>
            /// Defines a new IO Control Code
            /// </summary>
            /// <param name="deviceType">Identifies the device type.</param>
            /// <param name="function">Identifies the function to be performed by the driver.</param>
            /// <param name="method">Indicates how the system will pass data between the caller of <see cref="DeviceIoControl"/>
            /// and the driver that handles the IRP. Use one of the <see cref="IoctlMethod"/> constants.</param>
            /// <param name="access">Indicates the type of access that a caller must request when opening the file object that represents the device.</param>
            /// <returns>An I/O control code</returns>
            internal static uint CTL_CODE(uint deviceType, uint function, IoctlMethod method, IoctlAccess access) {
                return (deviceType << 16) | ((uint)access << 14) | (uint)((ushort)(function) << 2) | (uint)method;
            }

            /// <summary>
            /// Indicates how the system will pass data between the caller of <see cref="DeviceIoControl"/> and the driver that handles the IRP.
            /// </summary>
            internal enum IoctlMethod : uint {

                /// <summary>
                /// Specifies the buffered I/O method, which is typically used for transferring small amounts of data per request. 
                /// </summary>
                Buffered,

                /// <summary>
                /// Specifies the direct I/O method, which is typically used for writing large amounts of data, using DMA or PIO, that must be transferred quickly.
                /// </summary>
                InDirect,

                /// <summary>
                /// Specifies the direct I/O method, which is typically used for reading large amounts of data, using DMA or PIO, that must be transferred quickly.
                /// </summary>
                OutDirect,

                /// <summary>
                /// Specifies neither buffered nor direct I/O. The I/O manager does not provide any system buffers or MDLs.
                /// </summary>
                Neither,
            }

            /// <summary>
            /// Indicates the type of access that a caller must request when opening the file object that represents the device.
            /// </summary>
            internal enum IoctlAccess : byte {

                /// <summary>
                /// The I/O manager sends the IRP for any caller that has a handle to the file object that represents the target device object.
                /// </summary>
                AnyAccess,

                /// <summary>
                /// The I/O manager sends the IRP only for a caller with read access rights, allowing the underlying device driver to transfer data from the device to system memory.
                /// </summary>
                ReadData,

                /// <summary>
                /// The I/O manager sends the IRP only for a caller with write access rights, allowing the underlying device driver to transfer data from system memory to its device.
                /// </summary>
                WriteData,

                /// <summary>
                /// The caller must have both read and write access rights
                /// </summary>
                ReadWriteData = ReadData | WriteData,
            }
        }

        /// <summary>
        /// Advanced Windows 32 Base API (services)
        /// </summary>
        public static class Advapi32 {

            /// <summary>
            /// Establishes a connection to the service control manager on <paramref name="machineName"/> and opens the specified service control manager <paramref name="databaseName"/>.
            /// </summary>
            /// <param name="machineName">The name of the target computer</param>
            /// <param name="databaseName">The name of the service control manager database</param>
            /// <param name="dwAccess">The access to the service control manager</param>
            /// <returns>If the function succeeds, the return value is a handle to the specified service control manager database. If the function fails, the return value is NULL</returns>
            [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern IntPtr OpenSCManager(
                string machineName,
                string databaseName,
                ServiceAccessRights dwAccess);

            /// <summary>
            /// Establishes a connection to the service control manager on local computer and opens the specified service control manager SERVICES_ACTIVE_DATABASE database.
            /// </summary>
            /// <param name="dwAccess">The access to the service control manager</param>
            /// <returns>If the function succeeds, the return value is a handle to the specified service control manager database. If the function fails, the return value is NULL</returns>
            internal static IntPtr OpenSCManager(ServiceAccessRights dwAccess) => OpenSCManager(null, null, dwAccess);

            /// <summary>
            /// Service Security and Access Rights for the Service Control Manager
            /// </summary>
            [Flags]
            internal enum ServiceAccessRights : uint {
                SC_MANAGER_ALL_ACCESS = 0xF003F
            }

            /// <summary>
            /// Creates an NT service object and adds it to the specified service control manager database
            /// </summary>
            /// <param name="hSCManager">A handle to the service control manager database</param>
            /// <param name="lpServiceName">The name of the service to install</param>
            /// <param name="lpDisplayName">The display name to be used by user interface programs to identify the service</param>
            /// <param name="dwDesiredAccess">The access to the service. For a list of values, see <see cref="ServiceAccessRights"/> values.</param>
            /// <param name="dwServiceType">The service type. This parameter can be one of the <see cref="ServiceType"/> values</param>
            /// <param name="dwStartType">The service start options. This parameter can be one of the <see cref="ServiceStartMode"/> values</param>
            /// <param name="dwErrorControl">The severity of the error, and action taken, if this service fails to start. This parameter can be one of the <see cref="ErrorControl"/> values</param>
            /// <param name="lpBinaryPathName">The fully qualified path to the service binary file</param>
            /// <param name="lpLoadOrderGroup">The names of the load ordering group of which this service is a member</param>
            /// <param name="lpdwTagId">A pointer to a variable that receives a tag value that is unique in the group specified in the <paramref name="lpLoadOrderGroup"/> parameter</param>
            /// <param name="lpDependencies">A pointer to a double null-terminated array of null-separated names of services or load ordering groups that the system must start before this service</param>
            /// <param name="lpServiceStartName">The name of the account under which the service should run</param>
            /// <param name="lpPassword">The password to the account name specified by the <paramref name="lpServiceStartName"/> parameter</param>
            /// <returns>If the function succeeds, the return value is a handle to the service.</returns>
            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            internal static extern IntPtr CreateService(
                IntPtr hSCManager,
                string lpServiceName,
                string lpDisplayName,
                ServiceAccessRights dwDesiredAccess,
                ServiceType dwServiceType,
                ServiceStartMode dwStartType,
                ErrorControl dwErrorControl,
                string lpBinaryPathName,
                [Optional] string lpLoadOrderGroup,
                [Optional] string lpdwTagId,
                [Optional] string lpDependencies,
                [Optional] string lpServiceStartName,
                [Optional] string lpPassword);

            /// <summary>
            /// The severity of the error, and action taken, if this service fails to start
            /// </summary>
            internal enum ErrorControl : uint {

                /// <summary>
                /// The startup program ignores the error and continues the startup operation
                /// </summary>
                Ignore   = 0x00000000,

                /// <summary>
                /// The startup program logs the error in the event log but continues the startup operation
                /// </summary>
                Normal   = 0x00000001,

                /// <summary>
                /// The startup program logs the error in the event log.
                /// If the last-known-good configuration is being started, the startup operation continues.
                /// Otherwise, the system is restarted with the last-known-good configuration.
                /// </summary>
                Severe   = 0x00000002,

                /// <summary>
                /// The startup program logs the error in the event log, if possible
                /// </summary>
                Critical = 0x00000003,
            }

            /// <summary>
            /// Windows error codes returned by <see cref="Marshal.GetHRForLastWin32Error"/>
            /// </summary>
            internal enum WinError {
                /// <summary>
                /// The operation completed successfully
                /// </summary>
                NoError               = unchecked((int)0x80070000),
                /// <summary>
                /// The specified service already exists
                /// </summary>
                ServiceExists         = unchecked((int)0x80070431),
                /// <summary>
                /// An instance of the service is already running
                /// </summary>
                ServiceAlreadyRunning = unchecked((int)0x80070420),
            }

            /// <summary>
            /// Marks the specified service for deletion from the service control manager database.
            /// </summary>
            /// <param name="hService">A handle to the service.
            /// This handle is returned by the <see cref="OpenService"/> or <see cref="CreateService"/> function, and it must have the DELETE access right.</param>
            /// <returns><see langword="true"/> if the function succeeds</returns>
            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeleteService(IntPtr hService);

            /// <summary>
            /// Opens an existing service.
            /// </summary>
            /// <param name="hSCManager"></param>
            /// <param name="lpServiceName"></param>
            /// <param name="dwDesiredAccess"></param>
            /// <returns>If the function succeeds, the return value is a handle to the service. If the function fails, the return value is <see cref="IntPtr.Zero"/>.</returns>
            [DllImport("advapi32.dll", EntryPoint = "OpenServiceW", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, ServiceRights dwDesiredAccess);

            /// <summary>
            /// Specific access rights for a service
            /// </summary>
            [Flags]
            internal enum ServiceRights : uint {

                /// <summary>
                /// Required to call the QueryServiceConfig and QueryServiceConfig2 functions to query the service configuration.
                /// </summary>
                ServiceQueryConfig         = 0x00001,

                /// <summary>
                /// Required to call the ChangeServiceConfig or ChangeServiceConfig2 function to change the service configuration. 
                /// </summary>
                ServiceChangeConfig        = 0x00002,

                /// <summary>
                /// Required to call the QueryServiceStatus or QueryServiceStatusEx function to ask the service control manager about the status of the service.
                /// </summary>
                ServiceQueryStatus         = 0x00004,

                /// <summary>
                /// Required to call the EnumDependentServices function to enumerate all the services dependent on the service.
                /// </summary>
                ServiceEnumerateDependents = 0x00008,

                /// <summary>
                /// Required to call the StartService function to start the service.
                /// </summary>
                ServiceStart               = 0x00010,

                /// <summary>
                /// Required to call the ControlService function to stop the service.
                /// </summary>
                ServiceStop                = 0x00020,

                /// <summary>
                /// Required to call the ControlService function to pause or continue the service.
                /// </summary>
                ServicePauseContinue       = 0x00040,

                /// <summary>
                /// Required to call the ControlService function to ask the service to report its status immediately.
                /// </summary>
                ServiceInterrogate         = 0x00080,

                /// <summary>
                /// Required to call the ControlService function to specify a user-defined control code.
                /// </summary>
                ServiceUserDefinedControl  = 0x00100,

                /// <summary>
                /// The right to delete the object.
                /// </summary>
                Delete                     = 0x10000,

                /// <summary>
                /// The right to read the information in the object's security descriptor, not including the information in the system access control list (SACL).
                /// </summary>
                ReadControl                = 0x20000,

                /// <summary>
                /// The right to modify the discretionary access control list (DACL) in the object's security descriptor.
                /// </summary>
                WriteDac                   = 0x40000,

                /// <summary>
                /// The right to change the owner in the object's security descriptor.
                /// </summary>
                WriteOwner                 = 0x80000,

                /// <summary>
                /// Combines DELETE, READ_CONTROL, WRITE_DAC, and WRITE_OWNER access.
                /// </summary>
                StandardRightsRequired     = Delete | ReadControl | WriteDac | WriteOwner,

                /// <summary>
                /// Includes <see cref="StandardRightsRequired"/> in addition to all access rights in this table.
                /// </summary>
                ServiceAllAccess           = StandardRightsRequired | 
                                             ServiceQueryConfig | 
                                             ServiceChangeConfig | 
                                             ServiceQueryStatus | 
                                             ServiceEnumerateDependents | 
                                             ServiceStart | 
                                             ServiceStop | 
                                             ServicePauseContinue | 
                                             ServiceInterrogate | 
                                             ServiceUserDefinedControl
            }

            /// <summary>
            /// Contains status information for a service.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            internal struct ServiceStatus {
                public ServiceStatusServiceType dwServiceType;
                public ServiceStatusCurrentState dwCurrentState;
                public ServiceStatusControlsAccepted dwControlsAccepted;
                public uint dwWin32ExitCode;
                public uint dwServiceSpecificExitCode;
                public uint dwCheckPoint;
                public uint dwWaitHint;
            }

            /// <summary>
            /// The type of service for <see cref="ServiceStatus"/>.
            /// </summary>
            internal enum ServiceStatusServiceType : uint {

                /// <summary>
                /// The service is a device driver. 
                /// </summary>
                KernelDriver      = 0x00000001,

                /// <summary>
                /// The service is a file system driver. 
                /// </summary>
                FileSystemDriver  = 0x00000002,

                /// <summary>
                /// The service runs in its own process. 
                /// </summary>
                Win32OwnProcess   = 0x00000010,

                /// <summary>
                /// The service shares a process with other services. 
                /// </summary>
                Win32ShareProcess = 0x00000020,

                /// <summary>
                /// The service runs in its own process under the logged-on user account. 
                /// </summary>
                UserOwnProcess    = 0x00000050,

                /// <summary>
                /// The service shares a process with one or more other services that run under the logged-on user account. 
                /// </summary>
                UserShareProcess  = 0x00000060,
            }

            /// <summary>
            /// The current state of the service for <see cref="ServiceStatus"/>. 
            /// </summary>
            internal enum ServiceStatusCurrentState : uint {

                /// <summary>
                /// The service is not running. 
                /// </summary>
                Stopped         = 0x00000001,

                /// <summary>
                /// The service is starting. 
                /// </summary>
                StartPending    = 0x00000002,

                /// <summary>
                /// The service is stopping. 
                /// </summary>
                StopPending     = 0x00000003,

                /// <summary>
                /// The service is running. 
                /// </summary>
                Running         = 0x00000004,

                /// <summary>
                /// The service continue is pending. 
                /// </summary>
                ContinuePending = 0x00000005,

                /// <summary>
                /// The service pause is pending. 
                /// </summary>
                PausePending    = 0x00000006,

                /// <summary>
                /// The service is paused.
                /// </summary>
                Paused          = 0x00000007,
            }

            /// <summary>
            /// The control codes the service accepts and processes in its handler function for <see cref="ServiceStatus"/>.
            /// </summary>
            [Flags]
            internal enum ServiceStatusControlsAccepted : uint {

                /// <summary>
                /// The service can be stopped.
                /// </summary>
                Stop          = 0x00000001,

                /// <summary>
                /// The service can be paused and continued.
                /// </summary>
                PauseContinue = 0x00000002,

                /// <summary>
                /// The service is notified when system shutdown occurs.
                /// </summary>
                Shutdown      = 0x00000004,

                /// <summary>
                /// The service can reread its startup parameters without being stopped and restarted.
                /// </summary>
                Paramchange   = 0x00000008,

                /// <summary>
                /// The service is a network component that can accept changes in its binding without being stopped and restarted.
                /// </summary>
                Netbindchange = 0x00000010,

                /// <summary>
                /// The service can perform preshutdown tasks.
                /// </summary>
                Preshutdown   = 0x00000100,
            }

            /// <summary>
            /// Closes a handle to a service control manager or service object
            /// </summary>
            /// <param name="hSCObject">A handle to the service control manager object or the service object to close.
            /// Handles to service control manager objects are returned by the <see cref="OpenSCManager(ServiceAccessRights)"/> function,
            /// and handles to service objects are returned by either the <see cref="OpenService"/> or <see cref="CreateService"/> function.</param>
            /// <returns><see langword="true"/> if the function succeeds</returns>
            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseServiceHandle(IntPtr hSCObject);

            /// <summary>
            /// The OpenProcessToken function opens the access token associated with a process
            /// </summary>
            /// <param name="processHandle">A handle to the process whose access token is opened. The process must have the PROCESS_QUERY_LIMITED_INFORMATION access permission.</param>
            /// <param name="desiredAccess">Specifies an access mask that specifies the requested types of access to the access token.</param>
            /// <param name="tokenHandle">A pointer to a handle that identifies the newly opened access token when the function returns.</param>
            /// <returns><see langword="true"/> if the operation succeeds</returns>
            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool OpenProcessToken(IntPtr processHandle, DesiredAccess desiredAccess, out IntPtr tokenHandle);

            /// <summary>
            /// Access Rights for Access-Token Objects
            /// </summary>
            [Flags]
            public enum DesiredAccess {
                STANDARD_RIGHTS_REQUIRED = 0x000F0000,
                STANDARD_RIGHTS_READ     = 0x00020000,
                /// <summary>
                /// Required to attach a primary token to a process.
                /// </summary>
                TOKEN_ASSIGN_PRIMARY     = 0x0001,
                /// <summary>
                /// Required to duplicate an access token.
                /// </summary>
                TOKEN_DUPLICATE          = 0x0002,
                /// <summary>
                /// Required to attach an impersonation access token to a process.
                /// </summary>
                TOKEN_IMPERSONATE        = 0x0004,
                /// <summary>
                /// Required to query an access token.
                /// </summary>
                TOKEN_QUERY              = 0x0008,
                /// <summary>
                /// Required to query the source of an access token.
                /// </summary>
                TOKEN_QUERY_SOURCE       = 0x0010,
                /// <summary>
                /// Required to enable or disable the privileges in an access token.
                /// </summary>
                TOKEN_ADJUST_PRIVILEGES  = 0x0020,
                /// <summary>
                /// Required to adjust the attributes of the groups in an access token.
                /// </summary>
                TOKEN_ADJUST_GROUPS      = 0x0040,
                /// <summary>
                /// Required to change the default owner, primary group, or DACL of an access token.
                /// </summary>
                TOKEN_ADJUST_DEFAULT     = 0x0080,
                /// <summary>
                /// Required to adjust the session ID of an access token. The SE_TCB_NAME privilege is required.
                /// </summary>
                TOKEN_ADJUST_SESSIONID   = 0x0100,
                /// <summary>
                /// Combines <see cref="STANDARD_RIGHTS_READ"/> and <see cref="TOKEN_QUERY"/>.
                /// </summary>
                TOKEN_READ               = STANDARD_RIGHTS_READ | 
                                           TOKEN_QUERY,
                /// <summary>
                /// Combines all possible access rights for a token.
                /// </summary>
                TOKEN_ALL_ACCESS         = STANDARD_RIGHTS_REQUIRED | 
                                           TOKEN_ASSIGN_PRIMARY |
                                           TOKEN_DUPLICATE |
                                           TOKEN_IMPERSONATE | 
                                           TOKEN_QUERY | 
                                           TOKEN_QUERY_SOURCE |
                                           TOKEN_ADJUST_PRIVILEGES | 
                                           TOKEN_ADJUST_GROUPS | 
                                           TOKEN_ADJUST_DEFAULT |
                                           TOKEN_ADJUST_SESSIONID
            }

            /// <summary>
            /// The LookupPrivilegeValue function retrieves the locally unique identifier (LUID) used on a specified system to locally represent the specified privilege name.
            /// </summary>
            /// <param name="lpSystemName">The name of the system on which the privilege name is retrieved.
            /// If a null string is specified, the function attempts to find the privilege name on the local system.</param>
            /// <param name="lpName">The name of the privilege.</param>
            /// <param name="lpLuid">A pointer to a variable that receives the LUID by which the privilege is known on the system specified by the <paramref name="lpSystemName"/> parameter.</param>
            /// <returns></returns>
            [DllImport("advapi32.dll")]
            public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref LUID lpLuid);

            /// <summary>
            /// An opaque structure that specifies a 64-bit value that is guaranteed to be unique on the local machine.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public struct LUID {
                public uint LowPart;
                public uint HighPart;
            }

            /// <summary>
            /// The AdjustTokenPrivileges function enables or disables privileges in the specified access token.
            /// </summary>
            /// <param name="tokenHandle">A handle to the access token that contains the privileges to be modified.</param>
            /// <param name="disableAllPrivileges">Specifies whether the function disables all of the token's privileges.</param>
            /// <param name="newState">A pointer to a <see cref="TOKEN_PRIVILEGES"/> structure that specifies an array of privileges and their attributes.</param>
            /// <param name="bufferLengthInBytes">Specifies the size, in bytes, of the buffer pointed to by the <paramref name="previousState"/> parameter.</param>
            /// <param name="previousState">A pointer to a buffer that the function fills with a <see cref="TOKEN_PRIVILEGES"/> structure that contains the previous state of any privileges that the function modifies.</param>
            /// <param name="returnLengthInBytes">A pointer to a variable that receives the required size, in bytes, of the buffer pointed to by the <paramref name="previousState"/> parameter.</param>
            /// <returns></returns>
            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AdjustTokenPrivileges(IntPtr tokenHandle,
                [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
                ref TOKEN_PRIVILEGES newState,
                uint bufferLengthInBytes,
                [Optional] ref TOKEN_PRIVILEGES previousState,
                [Optional] out uint returnLengthInBytes);

            /// <summary>
            /// Contains information about a set of privileges for an access token.
            /// </summary>
            public struct TOKEN_PRIVILEGES {
                /// <summary>
                /// This must be set to the number of entries in the <see cref="TOKEN_PRIVILEGES.Privileges"/> array.
                /// </summary>
                public int PrivilegeCount;

                /// <summary>
                /// Specifies an array of <see cref="LUID_AND_ATTRIBUTES"/> structures.
                /// </summary>
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
                public LUID_AND_ATTRIBUTES[] Privileges;
            }

            /// <summary>
            /// Represents a locally unique identifier (<see cref="LUID"/>) and its attributes.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct LUID_AND_ATTRIBUTES {
                /// <summary>
                /// Specifies an <see cref="LUID"/> value
                /// </summary>
                public LUID Luid;

                /// <summary>
                /// Specifies attributes of the LUID
                /// </summary>
                public LuidAttributes Attributes;
            }

            /// <summary>
            /// Privilege attributes
            /// </summary>
            [Flags]
            public enum LuidAttributes : uint {
                SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001,
                SE_PRIVILEGE_ENABLED            = 0x00000002,
                SE_PRIVILEGE_REMOVED            = 0x00000004,
                SE_PRIVILEGE_USED_FOR_ACCESS    = 0x80000000,
                SE_PRIVILEGE_VALID_ATTRIBUTES   = SE_PRIVILEGE_ENABLED_BY_DEFAULT |
                                                  SE_PRIVILEGE_ENABLED |
                                                  SE_PRIVILEGE_REMOVED |
                                                  SE_PRIVILEGE_USED_FOR_ACCESS
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
        private static ServiceController _serviceController;

        /// <summary>
        /// Service control manager
        /// </summary>
        private static IntPtr ManagerPtr => Advapi32.OpenSCManager(dwAccess: Advapi32.ServiceAccessRights.SC_MANAGER_ALL_ACCESS);

        /// <summary>
        /// Service object
        /// </summary>
        private static IntPtr _serviceHandle;

        /// <summary>
        /// IO device driver handle
        /// </summary>
        private static SafeFileHandle _driverHandle;

        /// <summary>
        /// Driver handle lock flag
        /// </summary>
        private static bool _handleLock;

        /// <summary>
        /// Invalid handle alias
        /// </summary>
        private static uint InvalidHandle => uint.MaxValue;
    }
}