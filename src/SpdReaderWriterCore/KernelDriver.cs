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
using static SpdReaderWriterCore.NativeFunctions;
using static SpdReaderWriterCore.NativeFunctions.Advapi32;
using static SpdReaderWriterCore.NativeFunctions.Kernel32;

namespace SpdReaderWriterCore {

    /// <summary>
    /// Kernel Driver class
    /// </summary>
    public class KernelDriver {

        /// <summary>
        /// Default driver to use
        /// </summary>
        private static readonly Info DefaultDriver = GetDriverInfo("SpdReaderWriterCore.Driver.CpuZ", "CpuZInfo");

        /// <summary>
        /// Gets driver info
        /// </summary>
        /// <param name="driverClass">Driver class</param>
        /// <param name="fieldName">Driver info field name</param>
        /// <returns>Driver info</returns>
        private static Info GetDriverInfo(string driverClass, string fieldName) {

            Type driverType = Type.GetType(driverClass);

            if (driverType == null) {
                return new Info();
            }

            BindingFlags fieldFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            FieldInfo driverInfoField = driverType.GetField(fieldName, fieldFlags);

            if (driverInfoField == null) {
                foreach (FieldInfo fieldInfo in driverType.GetFields(fieldFlags)) {
                    if (fieldInfo.FieldType != DriverInfo.GetType()) {
                        continue;
                    }

                    driverInfoField = fieldInfo;
                    break;
                }
            }

            if (driverInfoField == null) {
                return new Info();
            }

            object driverInfoValue = driverInfoField.GetValue(null);

            return driverInfoValue is Info driverInfo
                ? driverInfo
                : new Info();
        }

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
                if (_driverInfo.Setup                 == null) { throw new MissingMethodException(typeof(SetupDelegate).ToString()); }
                if (_driverInfo.GetDriverVersion      == null) { throw new MissingMethodException(typeof(GetDriverVersionDelegate).ToString()); }
                if (_driverInfo.ReadIoPortByte        == null) { throw new MissingMethodException(typeof(ReadIoPortByteDelegate).ToString()); }
                if (_driverInfo.ReadIoPortByteEx      == null) { throw new MissingMethodException(typeof(ReadIoPortByteExDelegate).ToString()); }
                if (_driverInfo.ReadIoPortWord        == null) { throw new MissingMethodException(typeof(ReadIoPortWordDelegate).ToString()); }
                if (_driverInfo.ReadIoPortWordEx      == null) { throw new MissingMethodException(typeof(ReadIoPortWordExDelegate).ToString()); }
                if (_driverInfo.ReadIoPortDword       == null) { throw new MissingMethodException(typeof(ReadIoPortDwordDelegate).ToString()); }
                if (_driverInfo.ReadIoPortDwordEx     == null) { throw new MissingMethodException(typeof(ReadIoPortDwordExDelegate).ToString()); }
                if (_driverInfo.WriteIoPortByte       == null) { throw new MissingMethodException(typeof(WriteIoPortByteDelegate).ToString()); }
                if (_driverInfo.WriteIoPortByteEx     == null) { throw new MissingMethodException(typeof(WriteIoPortByteExDelegate).ToString()); }
                if (_driverInfo.WriteIoPortWord       == null) { throw new MissingMethodException(typeof(WriteIoPortWordDelegate).ToString()); }
                if (_driverInfo.WriteIoPortWordEx     == null) { throw new MissingMethodException(typeof(WriteIoPortWordExDelegate).ToString()); }
                if (_driverInfo.WriteIoPortDword      == null) { throw new MissingMethodException(typeof(WriteIoPortDwordDelegate).ToString()); }
                if (_driverInfo.WriteIoPortDwordEx    == null) { throw new MissingMethodException(typeof(WriteIoPortDwordExDelegate).ToString()); }
                if (_driverInfo.ReadPciConfigByte     == null) { throw new MissingMethodException(typeof(ReadPciConfigByteDelegate).ToString()); }
                if (_driverInfo.ReadPciConfigByteEx   == null) { throw new MissingMethodException(typeof(ReadPciConfigByteExDelegate).ToString()); }
                if (_driverInfo.ReadPciConfigWord     == null) { throw new MissingMethodException(typeof(ReadPciConfigWordDelegate).ToString()); }
                if (_driverInfo.ReadPciConfigWordEx   == null) { throw new MissingMethodException(typeof(ReadPciConfigWordExDelegate).ToString()); }
                if (_driverInfo.ReadPciConfigDword    == null) { throw new MissingMethodException(typeof(ReadPciConfigDwordDelegate).ToString()); }
                if (_driverInfo.ReadPciConfigDwordEx  == null) { throw new MissingMethodException(typeof(ReadPciConfigDwordExDelegate).ToString()); }
                if (_driverInfo.WritePciConfigByte    == null) { throw new MissingMethodException(typeof(WritePciConfigByteDelegate).ToString()); }
                if (_driverInfo.WritePciConfigByteEx  == null) { throw new MissingMethodException(typeof(WritePciConfigByteExDelegate).ToString()); }
                if (_driverInfo.WritePciConfigWord    == null) { throw new MissingMethodException(typeof(WritePciConfigWordDelegate).ToString()); }
                if (_driverInfo.WritePciConfigWordEx  == null) { throw new MissingMethodException(typeof(WritePciConfigWordExDelegate).ToString()); }
                if (_driverInfo.WritePciConfigDword   == null) { throw new MissingMethodException(typeof(WritePciConfigDwordDelegate).ToString()); }
                if (_driverInfo.WritePciConfigDwordEx == null) { throw new MissingMethodException(typeof(WritePciConfigDwordExDelegate).ToString()); }
                if (_driverInfo.ReadMemoryByte        == null) { throw new MissingMethodException(typeof(ReadMemoryByteDelegate).ToString()); }
                if (_driverInfo.ReadMemoryByteEx      == null) { throw new MissingMethodException(typeof(ReadMemoryByteDelegateEx).ToString()); }
                if (_driverInfo.ReadMemoryWord        == null) { throw new MissingMethodException(typeof(ReadMemoryWordDelegate).ToString()); }
                if (_driverInfo.ReadMemoryWordEx      == null) { throw new MissingMethodException(typeof(ReadMemoryWordDelegateEx).ToString()); }
                if (_driverInfo.ReadMemoryDword       == null) { throw new MissingMethodException(typeof(ReadMemoryDwordDelegate).ToString()); }
                if (_driverInfo.ReadMemoryDwordEx     == null) { throw new MissingMethodException(typeof(ReadMemoryDwordDelegateEx).ToString()); }
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
            internal string FileName;

            /// <summary>
            /// NT Device name
            /// </summary>
            internal string DeviceName;

            /// <summary>
            /// Driver file path
            /// </summary>
            internal string FilePath;

            /// <summary>
            /// Binary driver file contents
            /// </summary>
            internal byte[] BinaryData;

            /// <summary>
            /// Driver specific setup procedure
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
            bool result = false;
            output = default;

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
                    return DriverInfo.WriteIoPortByteEx(port, (byte)input);
                case Data.DataSize.Word:
                    return DriverInfo.WriteIoPortWordEx(port, (ushort)input);
                case Data.DataSize.Dword:
                    return DriverInfo.WriteIoPortDwordEx(port, (uint)input);
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

            object outputData = null;

            if (Data.GetDataSize(typeof(T)) == Data.DataSize.Byte) {
                outputData = DriverInfo.ReadPciConfigByte(bus, device, function, offset);
            }
            else if (Data.GetDataSize(typeof(T)) == Data.DataSize.Word) {
                outputData = DriverInfo.ReadPciConfigWord(bus, device, function, offset);
            }
            else if (Data.GetDataSize(typeof(T)) == Data.DataSize.Dword) {
                outputData = DriverInfo.ReadPciConfigDword(bus, device, function, offset);
            }

            if (outputData != null) {
                return (T)Convert.ChangeType(outputData, Type.GetTypeCode(typeof(T)));
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

            object outputData = null;
            bool result = false;
            output = default;

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
                    return DriverInfo.WritePciConfigByteEx(bus, device, function, offset, (byte)input);
                case Data.DataSize.Word:
                    return DriverInfo.WritePciConfigWordEx(bus, device, function, offset, (ushort)input);
                case Data.DataSize.Dword:
                    return DriverInfo.WritePciConfigDwordEx(bus, device, function, offset, (uint)input);
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
            bool result = false;
            output = default;

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

            if (outputData != null) {
                output = (T)outputData;
            }

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

            if (DriverInfo.Setup()) {
                return true;
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

            if (RemoveDriver() && !deleteFile) {
                return true;
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

            if (IsInstalled && Service.Status != ServiceControllerStatus.Stopped) {
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
                        case SystemError.ErrorFileNotFound:
                            return false;
                        case SystemError.ErrorAccessDenied:
                            Thread.Sleep(10);
                            continue;
                    }

                    break;
                }
            }
            finally {
                _sw.Stop();
            }

            return status == SystemError.Success;
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
        internal static void LockHandle() => LockHandle(true);

        /// <summary>
        /// Unlocks driver object handle to allow opening new handles
        /// </summary>
        internal static void UnlockHandle() => LockHandle(false);

        /// <summary>
        /// Sets handle lock status
        /// </summary>
        /// <param name="state">Handle lock state</param>
        /// <returns><see langword="true"/> if driver handle lock is successfully set</returns>
        internal static bool LockHandle(bool state) {
            return DriverInfo.LockHandle(state);
        }

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
                    return Service?.Status == ServiceControllerStatus.Running;
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
        public static IntPtr OpenSCManager(ServiceAccessRights dwAccess) => Advapi32.OpenSCManager(null, null, dwAccess);

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
            Buffered  = 0,

            /// <summary>
            /// Specifies the direct I/O method, which is typically used for writing large amounts of data, using DMA or PIO, that must be transferred quickly.
            /// </summary>
            InDirect  = 1,

            /// <summary>
            /// Specifies the direct I/O method, which is typically used for reading large amounts of data, using DMA or PIO, that must be transferred quickly.
            /// </summary>
            OutDirect = 2,

            /// <summary>
            /// Specifies neither buffered nor direct I/O. The I/O manager does not provide any system buffers or MDLs.
            /// </summary>
            Neither   = 3,
        }

        /// <summary>
        /// Indicates the type of access that a caller must request when opening the file object that represents the device.
        /// </summary>
        [Flags]
        internal enum IoctlAccess : byte {

            /// <summary>
            /// The I/O manager sends the IRP for any caller that has a handle to the file object that represents the target device object.
            /// </summary>
            AnyAccess     = 0,

            /// <summary>
            /// The I/O manager sends the IRP only for a caller with read access rights, allowing the underlying device driver to transfer data from the device to system memory.
            /// </summary>
            ReadData      = 1,

            /// <summary>
            /// The I/O manager sends the IRP only for a caller with write access rights, allowing the underlying device driver to transfer data from system memory to its device.
            /// </summary>
            WriteData     = 2,

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