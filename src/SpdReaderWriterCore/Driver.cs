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
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using static SpdReaderWriterCore.Data;
using static SpdReaderWriterCore.Kernel;
using static SpdReaderWriterCore.NativeFunctions;
using static SpdReaderWriterCore.NativeFunctions.Advapi32;
using static SpdReaderWriterCore.NativeFunctions.Advapi32.SystemError;
using static SpdReaderWriterCore.NativeFunctions.Kernel32;

namespace SpdReaderWriterCore {
    /// <summary>
    /// Driver class
    /// </summary>
    public class Driver {

        #region Delegates

        /// <summary>
        /// Driver initialization and setup procedure
        /// </summary>
        /// <returns></returns>
        internal delegate bool SetupDelegate();

        /// <summary>
        /// Driver installation procedure
        /// </summary>
        /// <returns><see langword="true"/> if driver is successfully installed</returns>
        internal delegate bool InstallDriverDelegate();

        /// <summary>
        /// Driver uninstallation procedure
        /// </summary>
        /// <returns><see langword="true"/> if driver is successfully uninstalled</returns>
        internal delegate bool UninstallDriverDelegate();

        /// <summary>
        /// Driver service start procedure
        /// </summary>
        /// <returns><see langword="true"/> if driver is successfully started</returns>
        internal delegate bool StartDriverDelegate();

        /// <summary>
        /// Driver service stop procedure
        /// </summary>
        /// <returns><see langword="true"/> if driver is successfully stopped</returns>
        internal delegate bool StopDriverDelegate();

        /// <summary>
        /// Driver handle locking procedure
        /// </summary>
        /// <param name="state">Driver handle state</param>
        /// <returns><see langword="true"/> if driver handle state is successfully set</returns>
        internal delegate bool LockHandleDelegate(bool state);

        /// <summary>
        /// Driver version info procedure
        /// </summary>
        /// <param name="major">Major version number</param>
        /// <param name="minor">Minor version number</param>
        /// <param name="revision">Revision number</param>
        /// <param name="release">Release number</param>
        /// <returns>Encoded version number</returns>
        internal delegate int GetDriverVersionDelegate(out byte major, out byte minor, out byte revision, out byte release);

        #endregion

        /// <summary>
        /// Default driver to use
        /// </summary>
        private static Info DefaultDriver {
            get {
                //return SpdReaderWriterCore.Drivers.CpuId.CpuZ.CpuZInfo;

                string className = "SpdReaderWriterCore.Drivers.CpuId.CpuZ";
                string fieldName = "CpuZInfo";

                try {
                    return GetFieldValue<Info>(className, fieldName);
                }
                catch (MissingFieldException) {
                    foreach (Info fieldValue in GetFieldValues<Info>(className)) {
                        if (fieldValue.Name == fieldName) {
                            return fieldValue;
                        }
                    }
                }

                return new Info();
            }
        }

        /// <summary>
        /// Driver info
        /// </summary>
        public static Info DriverInfo {
            get => _driverInfo;
            set {
                _driverInfo = value;

                int i = 0;
                ulong methodStatusMask = 0;

                // Check if all functions are present
                //if (_driverInfo.Setup               == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.GetDriverVersion      == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadIoPortByte        == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadIoPortByteEx      == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadIoPortWord        == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadIoPortWordEx      == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadIoPortDword       == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadIoPortDwordEx     == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.WriteIoPortByte       == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.WriteIoPortByteEx     == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.WriteIoPortWord       == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.WriteIoPortWordEx     == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.WriteIoPortDword      == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.WriteIoPortDwordEx    == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadPciConfigByte     == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadPciConfigByteEx   == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadPciConfigWord     == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadPciConfigWordEx   == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadPciConfigDword    == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadPciConfigDwordEx  == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.WritePciConfigByte    == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.WritePciConfigByteEx  == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.WritePciConfigWord    == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.WritePciConfigWordEx  == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.WritePciConfigDword   == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.WritePciConfigDwordEx == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadMemoryByte        == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadMemoryByteEx      == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadMemoryWord        == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadMemoryWordEx      == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadMemoryDword       == null) { SetBit(ref methodStatusMask, i, true); } i++;
                if (_driverInfo.ReadMemoryDwordEx     == null) { SetBit(ref methodStatusMask, i, true); } i++;

                if (methodStatusMask > 0) {
                    throw new MissingMethodException($"{_driverInfo} {nameof(MissingMethodException)}: 0x{methodStatusMask:X16}");
                }
            }
        }

        /// <summary>
        /// Kernel Driver Info struct
        /// </summary>
        public struct Info {

            /// <summary>
            /// Driver name
            /// </summary>
            public string Name;

            /// <summary>
            /// Info string
            /// </summary>
            /// <returns>Service name</returns>
            public override string ToString() =>
                !string.IsNullOrEmpty(Name)
                    ? Name
                    : !string.IsNullOrEmpty(ServiceName)
                        ? ServiceName
                        : "?";

            /// <summary>
            /// Driver service name
            /// </summary>
            public string ServiceName;

            /// <summary>
            /// Driver file name
            /// </summary>
            public string FileName;

            /// <summary>
            /// NT Device name
            /// </summary>
            public string DeviceName;

            /// <summary>
            /// Driver file path
            /// </summary>
            public string FilePath;

            /// <summary>
            /// Binary driver file contents
            /// </summary>
            internal byte[] BinaryData;

            /// <summary>
            /// Driver specific initialization and setup procedure
            /// </summary>
            internal SetupDelegate Setup { get; set; }
            internal InstallDriverDelegate InstallDriver { get; set; }
            internal UninstallDriverDelegate UninstallDriver { get; set; }
            internal StartDriverDelegate StartDriver { get; set; }
            internal StopDriverDelegate StopDriver { get; set; }
            internal LockHandleDelegate LockHandle { get; set; }

            internal GetDriverVersionDelegate GetDriverVersion { get; set; }

            internal ReadIoPortByteDelegate ReadIoPortByte { get; set; }
            internal ReadIoPortByteExDelegate ReadIoPortByteEx { get; set; }
            internal ReadIoPortWordDelegate ReadIoPortWord { get; set; }
            internal ReadIoPortWordExDelegate ReadIoPortWordEx { get; set; }
            internal ReadIoPortDwordDelegate ReadIoPortDword { get; set; }
            internal ReadIoPortDwordExDelegate ReadIoPortDwordEx { get; set; }
            internal WriteIoPortByteDelegate WriteIoPortByte { get; set; }
            internal WriteIoPortByteExDelegate WriteIoPortByteEx { get; set; }
            internal WriteIoPortWordDelegate WriteIoPortWord { get; set; }
            internal WriteIoPortWordExDelegate WriteIoPortWordEx { get; set; }
            internal WriteIoPortDwordDelegate WriteIoPortDword { get; set; }
            internal WriteIoPortDwordExDelegate WriteIoPortDwordEx { get; set; }
            internal ReadPciConfigByteDelegate ReadPciConfigByte { get; set; }
            internal ReadPciConfigByteExDelegate ReadPciConfigByteEx { get; set; }
            internal ReadPciConfigWordDelegate ReadPciConfigWord { get; set; }
            internal ReadPciConfigWordExDelegate ReadPciConfigWordEx { get; set; }
            internal ReadPciConfigDwordDelegate ReadPciConfigDword { get; set; }
            internal ReadPciConfigDwordExDelegate ReadPciConfigDwordEx { get; set; }
            internal WritePciConfigByteDelegate WritePciConfigByte { get; set; }
            internal WritePciConfigByteExDelegate WritePciConfigByteEx { get; set; }
            internal WritePciConfigWordDelegate WritePciConfigWord { get; set; }
            internal WritePciConfigWordExDelegate WritePciConfigWordEx { get; set; }
            internal WritePciConfigDwordDelegate WritePciConfigDword { get; set; }
            internal WritePciConfigDwordExDelegate WritePciConfigDwordEx { get; set; }
            internal ReadMemoryByteDelegate ReadMemoryByte { get; set; }
            internal ReadMemoryByteDelegateEx ReadMemoryByteEx { get; set; }
            internal ReadMemoryWordDelegate ReadMemoryWord { get; set; }
            internal ReadMemoryWordDelegateEx ReadMemoryWordEx { get; set; }
            internal ReadMemoryDwordDelegate ReadMemoryDword { get; set; }
            internal ReadMemoryDwordDelegateEx ReadMemoryDwordEx { get; set; }
        }

        /// <summary>
        /// Retrieves driver version
        /// </summary>
        /// <param name="major">Major version number</param>
        /// <param name="minor">Minor version number</param>
        /// <param name="revision">Revision number</param>
        /// <param name="release">Release number</param>
        /// <returns>Driver version</returns>
        public static int GetDriverVersion(out byte major, out byte minor, out byte revision, out byte release) => 
            DriverInfo.GetDriverVersion(out major, out minor, out revision, out release);

        /// <summary>
        /// Starts kernel driver
        /// </summary>
        /// <returns><see langword="true"/> if driver service starts successfully</returns>
        public static bool Start() => Initialize();

        /// <summary>
        /// Stops kernel driver
        /// </summary>
        /// <returns><see langword="true"/> if driver service stops successfully</returns>
        public static bool Stop() => StopDriver();

        /// <summary>
        /// Initializes driver
        /// </summary>
        /// <returns><see langword="true"/> if driver file is successfully initialized</returns>
        private static bool Initialize() {

            DriverInfo = DefaultDriver;

            if (DriverInfo.Setup != null) {
                return DriverInfo.Setup();
            }

            if (OpenDriverHandle(out IntPtr handle)) {
                CloseHandle(handle);
                return true;
            }

            if (!(IsInstalled || InstallDriver())) {
                throw new Exception($"Unable to install {DriverInfo.ServiceName} service");
            }

            if (!(IsRunning || StartDriver())) {
                throw new Exception($"Unable to start {DriverInfo.ServiceName} service");
            }

            return true;
        }

        /// <summary>
        /// Deinitializes kernel driver instance
        /// </summary>
        public static void Dispose() {

            if (DriverInfo.UninstallDriver != null) {
                DriverInfo.UninstallDriver();
                return;
            }

            _driverHandle?.Close();

            if (_disposeOnExit) {
                Stop();
                RemoveDriver(deleteFile: false);
            }

            CloseServiceHandle(Manager);
        }

        /// <summary>
        /// Extracts driver file from resources and saves it to a local file
        /// </summary>
        /// <returns><see langword="true"/> if driver file is successfully extracted</returns>
        private static bool ExtractDriver() {

            if (!(File.Exists(DriverInfo.FilePath) &&
                  CompareArray(DriverInfo.BinaryData, File.ReadAllBytes(DriverInfo.FilePath)))) {

                // Save driver to local file
                try {
                    File.WriteAllBytes(DriverInfo.FilePath, DriverInfo.BinaryData);
                }
                catch {
                    return false;
                }
            }

            return File.Exists(DriverInfo.FilePath) &&
                   CompareArray(DriverInfo.BinaryData, File.ReadAllBytes(DriverInfo.FilePath));
        }

        /// <summary>
        /// Installs kernel driver
        /// </summary>
        /// <returns><see langword="true"/> if the driver is successfully installed</returns>
        private static bool InstallDriver() {

            if (DriverInfo.InstallDriver != null) {
                return DriverInfo.InstallDriver();
            }

            if (IsInstalled) {
                return true;
            }

            if (!ExtractDriver()) {
                return false;
            }

            if (Manager == IntPtr.Zero) {
                return false;
            }

            _serviceHandle = CreateService(
                hSCManager       : Manager,
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

            if (DriverInfo.UninstallDriver != null) {
                return DriverInfo.UninstallDriver();
            }

            if (Manager == IntPtr.Zero) {
                return false;
            }

            _serviceHandle = OpenService(Manager, DriverInfo.ServiceName, ServiceRights.ServiceAllAccess);

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

            bool result = RemoveDriver();

            if (result && deleteFile) {
                try {
                    File.Delete(DriverInfo.FilePath);
                }
                finally {
                    result = !File.Exists(DriverInfo.FilePath);
                }
            }

            return result;
        }

        /// <summary>
        /// Starts kernel driver
        /// </summary>
        /// <returns><see langword="true"/> if driver is successfully started</returns>
        private static bool StartDriver() {

            if (DriverInfo.StartDriver != null) {
                return DriverInfo.StartDriver();
            }

            if (IsRunning) {
                return true;
            }

            if (IsInstalled) {
                try {
                    Service.Start();
                    Service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(Timeout));
                }
                catch {
                    return IsRunning;
                }
            }

            return IsRunning;
        }

        /// <summary>
        /// Stops kernel driver
        /// </summary>
        /// <returns><see langword="true"/> if driver is successfully stopped</returns>
        private static bool StopDriver() {

            if (DriverInfo.StopDriver != null) {
                return DriverInfo.StopDriver();
            }

            if (IsRunning) {
                try {
                    Service.Stop();
                }
                catch {
                    return !IsRunning;
                }
            }

            return !IsRunning;
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
        /// Opens driver handle
        /// </summary>
        /// <returns><see langword="true"/> if driver handle is successfully opened</returns>
        private static bool OpenDriverHandle(out IntPtr handle) {

            int status = -1;
            handle = IntPtr.Zero;

            _sw.Restart();

            try {
                while (_sw.ElapsedMilliseconds < Timeout) {
                    handle = CreateFile(
                        lpFileName            : DriverInfo.DeviceName,
                        dwDesiredAccess       : Kernel32.FileAccess.GenericRead,
                        dwShareMode           : FileShare.Read,
                        lpSecurityAttributes  : IntPtr.Zero,
                        dwCreationDisposition : FileMode.Open,
                        dwFlagsAndAttributes  : 0);

                    status = Marshal.GetLastWin32Error();

                    switch (status) {
                        case ErrorFileNotFound:
                            return false;
                        case ErrorAccessDenied:
                            Thread.Sleep(10);
                            continue;
                    }

                    break;
                }
            }
            finally {
                _sw.Stop();
            }

            return status == Success;
        }

        /// <summary>
        /// Closes kernel driver
        /// </summary>
        /// <returns><see langword="true"/> if driver handle is successfully closed</returns>
        private static bool CloseDriverHandle() {
            _driverHandle?.Close();

            return !IsHandleOpen;
        }

        /// <summary>
        /// Locks driver object handle to prevent opening new handles
        /// </summary>
        internal static bool LockHandle() => LockHandle(true);

        /// <summary>
        /// Unlocks driver object handle to allow opening new handles
        /// </summary>
        internal static bool UnlockHandle() => LockHandle(false);

        /// <summary>
        /// Sets handle lock status
        /// </summary>
        /// <param name="state">Handle lock state</param>
        /// <returns><see langword="true"/> if driver handle lock is successfully set</returns>
        internal static bool LockHandle(bool state) => DriverInfo.LockHandle(state);

        /// <summary>
        /// Describes driver installation state
        /// </summary>
        public static bool IsInstalled {
            get {
                try {
                    return Service?.ServiceName == DriverInfo.ServiceName &&
                           Service?.ServiceType == ServiceType.KernelDriver;
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
                    return IsInstalled &&
                           Service?.Status == ServiceControllerStatus.Running;
                }
                catch {
                    return false;
                }
            }
        }

        /// <summary>
        /// Describes driver handle open state
        /// </summary>
        private static bool IsHandleOpen => _driverHandle != null &&
                                            !_driverHandle.IsInvalid &&
                                            !_driverHandle.IsClosed;

        /// <summary>
        /// Establishes a connection to the service control manager on local computer and opens the specified service control manager SERVICES_ACTIVE_DATABASE database.
        /// </summary>
        /// <param name="dwAccess">The access to the service control manager</param>
        /// <returns>If the function succeeds, the return value is a handle to the specified service control manager database. If the function fails, the return value is NULL</returns>
        private static IntPtr OpenSCManager(ServiceAccessRights dwAccess) => Advapi32.OpenSCManager(null, null, dwAccess);

        /// <summary>
        /// Reads data from device
        /// </summary>
        /// <typeparam name="T">Output data type</typeparam>
        /// <param name="ioControlCode">IOCTL code</param>
        /// <param name="inputData">Input parameters</param>
        /// <param name="outputData">Output data returned by the driver</param>
        /// <returns><see langword="true"/> if the operation succeeds</returns>
        internal static bool DeviceIoControl<T>(uint ioControlCode, object inputData, ref T outputData) {

            bool result;

            lock (_driverLock) {

                uint inputSize = (uint)GetDataSize<uint>(inputData);
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

                if (_driverHandle == null || _driverHandle.IsClosed) {
                    _driverHandle = new SafeFileHandle(deviceHandle, true);
                }

                if (_driverHandle.IsInvalid) {
                    return false;
                }

                result = Kernel32.DeviceIoControl(
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
            }

            return result;
        }

        /// <summary>
        /// Defines a new IO Control Code
        /// </summary>
        /// <param name="deviceType">Identifies the device type.</param>
        /// <param name="function">Identifies the function to be performed by the driver.</param>
        /// <param name="method">Indicates how the system will pass data between the caller of <see cref="Kernel32.DeviceIoControl"/> and the driver that handles the IRP.
        /// Use one of the <see cref="IoctlMethod"/> constants.</param>
        /// <param name="access">Indicates the type of access that a caller must request when opening the file object that represents the device.</param>
        /// <returns>An I/O control code</returns>
        internal static uint CTL_CODE(uint deviceType, ushort function, IoctlMethod method, IoctlAccess access) {
            return (deviceType << 16) | ((uint)access << 14) | (ushort)(function << 2) | (byte)method;
        }

        /// <summary>
        /// Indicates how the system will pass data between the caller of <see cref="DeviceIoControl"/> and the driver that handles the IRP.
        /// </summary>
        internal enum IoctlMethod : byte {

            /// <summary>
            /// Specifies the buffered I/O method, which is typically used for transferring small amounts of data per request. 
            /// </summary>
            Buffered = 0,

            /// <summary>
            /// Specifies the direct I/O method, which is typically used for writing large amounts of data, using DMA or PIO, that must be transferred quickly.
            /// </summary>
            InDirect = 1,

            /// <summary>
            /// Specifies the direct I/O method, which is typically used for reading large amounts of data, using DMA or PIO, that must be transferred quickly.
            /// </summary>
            OutDirect = 2,

            /// <summary>
            /// Specifies neither buffered nor direct I/O. The I/O manager does not provide any system buffers or MDLs.
            /// </summary>
            Neither = 3,
        }

        /// <summary>
        /// Indicates the type of access that a caller must request when opening the file object that represents the device.
        /// </summary>
        [Flags]
        internal enum IoctlAccess : byte {

            /// <summary>
            /// The I/O manager sends the IRP for any caller that has a handle to the file object that represents the target device object.
            /// </summary>
            AnyAccess = 0,

            /// <summary>
            /// The I/O manager sends the IRP only for a caller with read access rights, allowing the underlying device driver to transfer data from the device to system memory.
            /// </summary>
            ReadData = 1,

            /// <summary>
            /// The I/O manager sends the IRP only for a caller with write access rights, allowing the underlying device driver to transfer data from system memory to its device.
            /// </summary>
            WriteData = 2,

            /// <summary>
            /// The caller must have both read and write access rights
            /// </summary>
            ReadWriteData = ReadData | WriteData,
        }

        /// <summary>
        /// Driver object lock to prevent multiple threads from acquiring the lock
        /// </summary>
        private static object _driverLock = new object();

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
        private static ServiceController Service => DriverInfo.ServiceName != null ? new ServiceController(DriverInfo.ServiceName) : null;

        /// <summary>
        /// Service control manager
        /// </summary>
        private static IntPtr Manager => OpenSCManager(ServiceAccessRights.SC_MANAGER_ALL_ACCESS);

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