/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Microsoft.Win32.SafeHandles;
using SpdReaderWriterCore.Properties;
using static SpdReaderWriterCore.KernelDriver;

namespace SpdReaderWriterCore.Driver {

    /// <summary>
    /// WinRing0 Kernel driver class
    /// </summary>
    public class WinRing0 : IDisposable, IDriver {

        /// <summary>
        /// Describes driver installation state
        /// </summary>
        public bool IsInstalled => CheckDriver();

        /// <summary>
        /// Describes driver running state
        /// </summary>
        public bool IsServiceRunning {
            get {
                try {
                    _sc?.Refresh();
                    return IsInstalled && _sc?.Status == ServiceControllerStatus.Running;
                }
                catch {
                    return false;
                }
            }
        }

        /// <summary>
        /// Describes driver handle open state
        /// </summary>
        private bool IsHandleOpen => _deviceHandle != null && !_deviceHandle.IsClosed;

        /// <summary>
        /// Describes driver handle valid state
        /// </summary>
        private bool IsValid => IsInstalled && _deviceHandle != null && !_deviceHandle.IsInvalid;

        /// <summary>
        /// Describes driver ready state
        /// </summary>
        public bool IsReady => IsValid && IsHandleOpen;

        /// <summary>
        /// Initializes kernel driver
        /// </summary>
        public WinRing0() {

            if (IsInstalled && IsServiceRunning) {
                _disposeOnExit = false;
            }
            else {
                if (!(IsInstalled || InstallDriver())) {
                    throw new Exception("Unable to install driver service");
                }

                if (!(IsServiceRunning || StartDriver())) {
                    throw new Exception("Unable to start driver service");
                }
            }

            if (!(IsHandleOpen || OpenDriverHandle())) {
                throw new Exception("Unable to open driver handle");
            }
        }

        /// <summary>
        /// Kernel driver destructor
        /// </summary>
        ~WinRing0() {
            Dispose();
        }

        /// <summary>
        /// Deinitializes kernel driver instance
        /// </summary>
        public void Dispose() {
            CloseDriverHandle();

            int refCount = 0;

            DeviceIoControl(IoControlCode.WR0_GET_REFCOUNT, null, ref refCount);

            if (_disposeOnExit && refCount <= 1) {
                StopDriver();
                RemoveDriver(deleteFile: false);
            }

            _deviceHandle = null;

            Win32BaseApi.CloseServiceHandle(_servicePtr);
            Win32BaseApi.CloseServiceHandle(_managerPtr);
        }

        /// <summary>
        /// Extracts driver file from resources and saves it to a local file at <see cref="_fileName"/>
        /// </summary>
        /// <returns><see langref="true"/> if driver file is successfully extracted</returns>
        private bool ExtractDriver() {

            // Read applicable driver from resources depending on OS platform
            byte[] driverFileContents = Data.Gzip(Environment.Is64BitOperatingSystem
                    ? Resources.Driver.WinRing0x64_sys
                    : Resources.Driver.WinRing0_sys,
                Data.GzipMethod.Decompress);

            if (!(File.Exists(_fileName) && Data.CompareArray(driverFileContents, File.ReadAllBytes(_fileName)))) {

                // Save driver to local file
                try {
                    File.WriteAllBytes(_fileName, driverFileContents);
                }
                catch {
                    return false;
                }
            }

            return File.Exists(_fileName) && Data.CompareArray(driverFileContents, File.ReadAllBytes(_fileName));
        }

        /// <summary>
        /// Installs kernel driver
        /// </summary>
        /// <returns><see langref="true"/> if the driver is successfully installed</returns>
        public bool InstallDriver() {

            if (!ExtractDriver()) {
                return false;
            }

            if (_managerPtr == IntPtr.Zero) {
                return false;
            }

            _servicePtr = Win32BaseApi.CreateService(
                hSCManager       : _managerPtr,
                lpServiceName    : Name,
                lpDisplayName    : Name,
                dwDesiredAccess  : Win32BaseApi.ServiceAccessRights.SC_MANAGER_ALL_ACCESS,
                dwServiceType    : ServiceType.KernelDriver,
                dwStartType      : ServiceStartMode.Automatic,
                dwErrorControl   : Win32BaseApi.ErrorControl.SERVICE_ERROR_NORMAL,
                lpBinaryPathName : _fileName);

            if (_servicePtr == IntPtr.Zero) {
                return false;
            }

            Win32BaseApi.CloseServiceHandle(_servicePtr);

            return true;
        }

        /// <summary>
        /// Deletes kernel driver
        /// </summary>
        /// <returns><see langref="true"/> if driver is successfully deleted</returns>
        public bool RemoveDriver() {

            if (_managerPtr == IntPtr.Zero) {
                return false;
            }

            _servicePtr = Win32BaseApi.OpenService(_managerPtr, Name, Win32BaseApi.ServiceRights.SERVICE_ALL_ACCESS);

            if (_servicePtr == IntPtr.Zero) {
                return false;
            }

            return Win32BaseApi.DeleteService(_servicePtr) &&
                   Win32BaseApi.CloseServiceHandle(_servicePtr);
        }

        /// <summary>
        /// Deletes kernel driver and driver file
        /// </summary>
        /// <param name="deleteFile">Set to <see langref="true"/> to delete driver file, or <see langref="false"/> to keep it</param>
        /// <returns><see langref="true"/> if the driver service and the driver file are successfully deleted</returns>
        private bool RemoveDriver(bool deleteFile) {
            if (!deleteFile) {
                return RemoveDriver();
            }

            if (!RemoveDriver()) {
                return false;
            }

            try {
                File.Delete(_fileName);
                return !File.Exists(_fileName);
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Starts kernel driver
        /// </summary>
        /// <returns><see langref="true"/> if driver is successfully started</returns>
        public bool StartDriver() {

            if (!IsInstalled) {
                return false;
            }

            try {
                if (_sc.Status == ServiceControllerStatus.Running) {
                    return true;
                }

                _sc.Start();
                _sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(TIMEOUT));
                return _sc.Status == ServiceControllerStatus.Running;
            }
            catch {
                try {
                    return _sc.Status == ServiceControllerStatus.Running;
                }
                catch {
                    return false;
                }
            }
        }

        /// <summary>
        /// Stops kernel driver
        /// </summary>
        /// <returns><see langref="true"/> if driver is successfully stopped</returns>
        public bool StopDriver() {

            try {
                _sc = new ServiceController(Name);

                if (_sc.Status != ServiceControllerStatus.Stopped) {

                    _sc.Stop();

                    // Wait for Stopped or StopPending
                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    while (sw.ElapsedMilliseconds < TIMEOUT) {

                        _sc.Refresh();

                        if (_sc.Status == ServiceControllerStatus.Stopped ||
                            _sc.Status == ServiceControllerStatus.StopPending) {
                            return true;
                        }
                    }
                }

                return _sc.Status == ServiceControllerStatus.Stopped ||
                       _sc.Status == ServiceControllerStatus.StopPending;
            }
            catch {
                try {
                    _sc = new ServiceController(Name);
                    return _sc.Status == ServiceControllerStatus.Stopped ||
                           _sc.Status == ServiceControllerStatus.StopPending;
                }
                catch {
                    return true;
                }
            }
        }

        /// <summary>
        /// Checks if the driver is installed
        /// </summary>
        /// <returns><see langref="true"/> if the driver is installed</returns>
        private static bool CheckDriver() {

            try {
                return _sc?.ServiceType == ServiceType.KernelDriver && _sc?.DisplayName == Name;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Opens driver handle
        /// </summary>
        /// <returns><see langref="true"/> if driver handle is successfully opened</returns>
        private bool OpenDriverHandle() {

            IntPtr driverHandle = NtBaseApi.CreateFile(
                lpFileName            : $@"\\.\{Name}",
                dwDesiredAccess       : FileAccess.ReadWrite,
                dwShareMode           : FileShare.ReadWrite,
                lpSecurityAttributes  : IntPtr.Zero,
                dwCreationDisposition : FileMode.Open,
                dwFlagsAndAttributes  : FileAttributes.Normal);

            _deviceHandle = new SafeFileHandle(driverHandle, true);

            if (_deviceHandle.IsInvalid) {
                CloseDriverHandle();
            }

            return IsValid;
        }

        /// <summary>
        /// Closes kernel driver
        /// </summary>
        /// <returns><see langref="true"/> if driver handle is successfully closed</returns>
        private bool CloseDriverHandle() {
            if (IsHandleOpen) {
                _deviceHandle.Close();
                _deviceHandle.Dispose();
            }

            return !IsHandleOpen;
        }

        /// <summary>
        /// Retrieves driver version
        /// </summary>
        /// <param name="major">Major version number</param>
        /// <param name="minor">Minor version number</param>
        /// <param name="revision">Revision number</param>
        /// <param name="release">Release number</param>
        /// <returns>Driver version</returns>
        public uint GetDriverVersion(ref byte major, ref byte minor, ref byte revision, ref byte release) {

            uint output = default;

            DeviceIoControl(IoControlCode.WR0_GET_DRIVER_VERSION, null, ref output);

            major    = (byte)(output >> 24);
            minor    = (byte)(output >> 16);
            revision = (byte)(output >> 8);
            release  = (byte)output;

            return output;
        }

        /// <summary>
        /// Reads data from device
        /// </summary>
        /// <typeparam name="T">Output data type</typeparam>
        /// <param name="ioControlCode">IOCTL code</param>
        /// <param name="inputData">Input parameters</param>
        /// <param name="outputData">Output data returned by the driver</param>
        /// <returns><see lang="true"/> if the operation succeeds</returns>
        private static bool DeviceIoControl<T>(uint ioControlCode, object inputData, ref T outputData) {

            if (!_isReady) {
                return false;
            }

            uint inputSize      = (uint)(inputData == null ? 0 : Marshal.SizeOf(inputData));
            uint returnedLength = default;
            object outputBuffer = outputData;

            bool result = NtBaseApi.DeviceIoControl(
                hDevice         : _deviceHandle,
                dwIoControlCode : ioControlCode,
                lpInBuffer      : inputData,
                nInBufferSize   : inputSize,
                lpOutBuffer     : outputBuffer,
                nOutBufferSize  : (uint)Marshal.SizeOf(outputBuffer),
                lpBytesReturned : out returnedLength,
                lpOverlapped    : IntPtr.Zero);

            outputData = (T)outputBuffer;

            return result;
        }

        /// <summary>
        /// Writes data to device
        /// </summary>
        /// <param name="ioControlCode">IOCTL code</param>
        /// <param name="inputData">Input parameters</param>
        /// <returns><see lang="true"/> if the operation succeeds</returns>
        private static bool DeviceIoControl(uint ioControlCode, object inputData) {

            if (!_isReady) {
                return false;
            }

            uint inputSize      = (uint)(inputData == null ? 0 : Marshal.SizeOf(inputData));
            uint returnedLength = default;

            return NtBaseApi.DeviceIoControl(
                hDevice         : _deviceHandle,
                dwIoControlCode : ioControlCode,
                lpInBuffer      : inputData,
                nInBufferSize   : inputSize,
                lpOutBuffer     : null,
                nOutBufferSize  : 0,
                lpBytesReturned : out returnedLength,
                lpOverlapped    : IntPtr.Zero);
        }

        #region PCI device

        /// <summary>
        /// Converts PCI Bus Number, Device Number, and Function Number to PCI Device Address
        /// </summary>
        /// <param name="bus">PCI Bus Number</param>
        /// <param name="dev">PCI Device Number</param>
        /// <param name="func">PCI Function Number</param>
        /// <returns>PCI Device Address</returns>
        public static uint PciBusDevFunc(uint bus, uint dev, uint func) {
            return ((bus & 0xFF) << 8) | ((dev & 0x1F) << 3) | (func & 0x07);
        }

        /// <summary>
        /// Converts PCI Device Address to PCI Bus Number
        /// </summary>
        /// <param name="address">PCI Device Address</param>
        /// <returns>PCI Bus Number</returns>
        public static byte PciGetBus(uint address) {
            return (byte)((address >> 8) & 0xFF);
        }

        /// <summary>
        /// Converts PCI Device Address to PCI Device Number
        /// </summary>
        /// <param name="address">PCI Device Address</param>
        /// <returns>PCI Device Number</returns>
        public static byte PciGetDev(uint address) {
            return (byte)((address >> 3) & 0x1F);
        }

        /// <summary>
        /// Converts PCI Device Address to PCI Function Number
        /// </summary>
        /// <param name="address">PCI Device Address</param>
        /// <returns>PCI Function Number</returns>
        public static byte PciGetFunc(uint address) {
            return (byte)(address & 0x07);
        }

        /// <summary>
        /// Sets the maximum PCI bus index to scan by <see cref="FindPciDeviceByIdArray(ushort,ushort,ushort)"/>
        /// and <see cref="FindPciDeviceByClass(byte,byte,byte,ushort)"/>
        /// </summary>
        /// <param name="max">Maximum PCI bus index to scan</param>
        public void SetPciMaxBusIndex(byte max) {
            gPciNumberOfBus = max;
        }

        /// <summary>
        /// Finds PCI device matching Vendor ID and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <returns>PCI Device Address matching input <paramref name="vendorId">Vendor ID</paramref> and <paramref name="deviceId">Device ID</paramref></returns>
        public uint FindPciDeviceById(ushort vendorId, ushort deviceId) {
            return FindPciDeviceById(vendorId, deviceId, 0);
        }

        /// <summary>
        /// Finds PCI device matching Vendor ID and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <param name="index">Device index to find</param>
        /// <returns>PCI Device Address matching input <paramref name="vendorId">Vendor ID</paramref> and <paramref name="deviceId">Device ID</paramref></returns>
        public uint FindPciDeviceById(ushort vendorId, ushort deviceId, ushort index) {

            if (index > gPciNumberOfBus * gPciNumberOfDevice * gPciNumberOfFunction) {
                throw new ArgumentOutOfRangeException();
            }

            uint pciAddress = uint.MaxValue;
            
            if (vendorId == ushort.MinValue || vendorId == ushort.MaxValue) {
                return pciAddress;
            }

            uint[] device = FindPciDeviceByIdArray(vendorId, deviceId);

            return device.Length >= index + 1 ? device[index] : pciAddress;
        }

        /// <summary>
        /// Finds PCI devices matching Vendor ID and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <returns>An array of PCI Device Addresses matching input <paramref name="vendorId">Vendor ID</paramref> and <paramref name="deviceId">Device ID</paramref></returns>
        public uint[] FindPciDeviceByIdArray(ushort vendorId, ushort deviceId) {
            return FindPciDeviceByIdArray(vendorId, deviceId, (ushort)(gPciNumberOfBus * gPciNumberOfDevice * gPciNumberOfFunction));
        }

        /// <summary>
        /// Finds PCI devices matching Vendor ID and Device ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="deviceId">Device ID</param>
        /// <param name="maxCount">Maximum number of devices to find</param>
        /// <returns>An array of PCI Device Addresses matching input <paramref name="vendorId">Vendor ID</paramref> and <paramref name="deviceId">Device ID</paramref></returns>
        public uint[] FindPciDeviceByIdArray(ushort vendorId, ushort deviceId, ushort maxCount) {

            if (maxCount > gPciNumberOfBus * gPciNumberOfDevice * gPciNumberOfFunction || maxCount == 0) {
                throw new ArgumentOutOfRangeException();
            }

            uint count = 0;

            if (vendorId == default || deviceId == default) {
                return new uint[0];
            }

            Queue<uint> result = new Queue<uint>();

            for (ushort bus = 0; bus <= gPciNumberOfBus; bus++) {
                for (byte dev = 0; dev < gPciNumberOfDevice; dev++) {

                    ushort devId = ReadPciConfigWord(PciBusDevFunc(bus, dev, 0), 0x00);

                    if (devId == ushort.MinValue || devId == ushort.MaxValue) {
                        continue;
                    }

                    for (byte func = 0; func < gPciNumberOfFunction; func++) {

                        if (ReadPciConfigDword(PciBusDevFunc(bus, dev, func), 0x00) !=
                            (uint)(vendorId | (deviceId << 16))) {
                            continue;
                        }

                        result.Enqueue(PciBusDevFunc(bus, dev, func));

                        if (++count == maxCount) {
                            return result.ToArray();
                        }
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Finds PCI device by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <param name="programIf">Program Interface</param>
        /// <returns>PCI Device Address matching input <paramref name="baseClass"/>, <paramref name="subClass"/>, and <paramref name="programIf"/></returns>
        public uint FindPciDeviceByClass(byte baseClass, byte subClass, byte programIf) {
            return FindPciDeviceByClass(baseClass, subClass, programIf, 0);
        }

        /// <summary>
        /// Finds PCI device by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <param name="programIf">Program Interface</param>
        /// <param name="index">Device index to find</param>
        /// <returns>PCI Device Address matching input <paramref name="baseClass"/>, <paramref name="subClass"/>, and <paramref name="programIf"/></returns>
        public uint FindPciDeviceByClass(byte baseClass, byte subClass, byte programIf, ushort index) {

            if (index > gPciNumberOfBus * gPciNumberOfDevice * gPciNumberOfFunction) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            uint pciAddress = uint.MaxValue;

            uint[] device = FindPciDeviceByClassArray(baseClass, subClass, programIf);

            return device.Length >= index + 1 ? device[index] : pciAddress;
        }

        /// <summary>
        /// Finds PCI devices by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <param name="programIf">Program Interface</param>
        /// <returns>An array of PCI Device Addresses matching input <paramref name="baseClass"/>, <paramref name="subClass"/>, and <paramref name="programIf"/></returns>
        public uint[] FindPciDeviceByClassArray(byte baseClass, byte subClass, byte programIf) {
            return FindPciDeviceByClassArray(baseClass, subClass, programIf, (ushort)(gPciNumberOfBus * gPciNumberOfDevice * gPciNumberOfFunction));
        }

        /// <summary>
        /// Finds PCI devices by Device Class
        /// </summary>
        /// <param name="baseClass">Base Class</param>
        /// <param name="subClass">Sub Class</param>
        /// <param name="programIf">Program Interface</param>
        /// <param name="maxCount">Maximum number of devices to find</param>
        /// <returns>An array of PCI Device Addresses matching input <paramref name="baseClass"/>, <paramref name="subClass"/>, and <paramref name="programIf"/></returns>
        public uint[] FindPciDeviceByClassArray(byte baseClass, byte subClass, byte programIf, ushort maxCount) {

            if (maxCount > gPciNumberOfBus * gPciNumberOfDevice * gPciNumberOfFunction) {
                throw new ArgumentOutOfRangeException(nameof(maxCount));
            }

            if (maxCount == 0) {
                return new uint[0];
            }

            uint count = 0;

            Queue<uint> result = new Queue<uint>();

            for (ushort bus = 0; bus <= gPciNumberOfBus; bus++) {
                for (byte dev = 0; dev < gPciNumberOfDevice; dev++) {

                    ushort devId = ReadPciConfigWord(PciBusDevFunc(bus, dev, 0), 0x00);

                    if (devId == ushort.MinValue || devId == ushort.MaxValue) {
                        continue;
                    }

                    for (byte func = 0; func < gPciNumberOfFunction; func++) {

                        if ((ReadPciConfigDword(PciBusDevFunc(bus, dev, func), 0x08) & 0xFFFFFF00) !=
                            (uint)(baseClass << 24 | subClass << 16 | programIf << 8)) {
                            continue;
                        }

                        result.Enqueue(PciBusDevFunc(bus, dev, func));

                        if (++count == maxCount) {
                            return result.ToArray();
                        }
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Reads a byte value from the specified PCI configuration address
        /// </summary>
        /// <param name="pciAddress">PCI device address</param>
        /// <param name="regAddress">Configuration register address</param>
        /// <returns>Returns a byte value read from the specified PCI configuration address</returns>
        public byte ReadPciConfigByte(uint pciAddress, uint regAddress) {

            ReadPciConfigByteEx(pciAddress, regAddress, out byte output);

            return output;
        }

        /// <summary>
        /// Reads a byte value from the specified PCI configuration address
        /// </summary>
        /// <param name="pciAddress">PCI device address</param>
        /// <param name="regAddress">Configuration register address</param>
        /// <param name="output">Byte value read from the specified PCI configuration address</param>
        /// <returns><see lang="true"/> if the function succeeds</returns>
        public bool ReadPciConfigByteEx(uint pciAddress, uint regAddress, out byte output) {

            output = byte.MaxValue;

            ReadPciConfigInput pciData = new ReadPciConfigInput {
                PciAddress = pciAddress,
                PciOffset  = regAddress,
            };

            return DeviceIoControl(IoControlCode.WR0_READ_PCI_CONFIG, pciData, ref output);
        }

        /// <summary>
        /// Reads a ushort value from the specified PCI configuration address
        /// </summary>
        /// <param name="pciAddress">PCI device address</param>
        /// <param name="regAddress">Configuration register address</param>
        /// <returns>Returns a ushort value read from the specified PCI configuration address.</returns>
        public ushort ReadPciConfigWord(uint pciAddress, uint regAddress) {

            ReadPciConfigWordEx(pciAddress, regAddress, out ushort output);

            return output;
        }

        /// <summary>
        /// Reads a ushort value from the specified PCI configuration address
        /// </summary>
        /// <param name="pciAddress">PCI device address</param>
        /// <param name="regAddress">Configuration register address</param>
        /// <param name="output">ushort value read from the specified PCI configuration address.</param>
        /// <returns><see lang="true"/> if the function succeeds</returns>
        public bool ReadPciConfigWordEx(uint pciAddress, uint regAddress, out ushort output) {

            output = ushort.MaxValue;

            ReadPciConfigInput pciData = new ReadPciConfigInput {
                PciAddress = pciAddress,
                PciOffset  = regAddress
            };

            return DeviceIoControl(IoControlCode.WR0_READ_PCI_CONFIG, pciData, ref output);
        }

        /// <summary>
        /// Reads a uint value from the specified PCI configuration address
        /// </summary>
        /// <param name="pciAddress">PCI device address</param>
        /// <param name="regAddress">Configuration register address</param>
        /// <returns>Returns a uint value read from the specified PCI configuration address.</returns>
        public uint ReadPciConfigDword(uint pciAddress, uint regAddress) {

            ReadPciConfigDwordEx(pciAddress, regAddress, out uint output);

            return output;
        }

        /// <summary>
        /// Reads a uint value from the specified PCI configuration address
        /// </summary>
        /// <param name="pciAddress">PCI device address</param>
        /// <param name="regAddress">Configuration register address</param>
        /// <param name="output">uint value read from the specified PCI configuration address.</param>
        /// <returns><see lang="true"/> if the function succeeds</returns>
        public bool ReadPciConfigDwordEx(uint pciAddress, uint regAddress, out uint output) {

            output = uint.MaxValue;

            ReadPciConfigInput pciData = new ReadPciConfigInput {
                PciAddress = pciAddress,
                PciOffset  = regAddress
            };

            return DeviceIoControl(IoControlCode.WR0_READ_PCI_CONFIG, pciData, ref output);
        }

        /// <summary>
        /// Writes a byte value to the specified PCI configuration address
        /// </summary>
        /// <param name="pciAddress">PCI device address</param>
        /// <param name="regAddress">Configuration register address</param>
        /// <param name="value">Byte value to write to the configuration register</param>
        public void WritePciConfigByte(uint pciAddress, uint regAddress, byte value) {
            WritePciConfigByteEx(pciAddress, regAddress, value);
        }

        /// <summary>
        /// Writes a byte value to the specified PCI configuration address
        /// </summary>
        /// <param name="pciAddress">PCI device address</param>
        /// <param name="regAddress">Configuration register address</param>
        /// <param name="value">Byte value to write to the configuration register</param>
        /// <returns><see lang="true"/> if the function succeeds</returns>
        public bool WritePciConfigByteEx(uint pciAddress, uint regAddress, byte value) {

            WritePciConfigInputByte pciData = new WritePciConfigInputByte {
                PciAddress = pciAddress,
                PciOffset  = regAddress,
                Data       = value
            };

            return DeviceIoControl(IoControlCode.WR0_WRITE_PCI_CONFIG, pciData);
        }

        /// <summary>
        /// Writes a ushort value to the specified PCI configuration address
        /// </summary>
        /// <param name="pciAddress">PCI device address</param>
        /// <param name="regAddress">Configuration register address</param>
        /// <param name="value">ushort value to write to the configuration register</param>
        public void WritePciConfigWord(uint pciAddress, uint regAddress, ushort value) {
            WritePciConfigWordEx(pciAddress, regAddress, value);
        }

        /// <summary>
        /// Writes a ushort value to the specified PCI configuration address
        /// </summary>
        /// <param name="pciAddress">PCI device address</param>
        /// <param name="regAddress">Configuration register address</param>
        /// <param name="value">Word value to write to the configuration register</param>
        /// <returns><see lang="true"/> if the function succeeds</returns>
        public bool WritePciConfigWordEx(uint pciAddress, uint regAddress, ushort value) {

            // Check ushort boundary alignment
            if ((regAddress & 1) != 0) {
                return false;
            }

            WritePciConfigInputWord pciData = new WritePciConfigInputWord {
                PciAddress = pciAddress,
                PciOffset  = regAddress,
                Data       = value
            };

            return DeviceIoControl(IoControlCode.WR0_WRITE_PCI_CONFIG, pciData);
        }

        /// <summary>
        /// Writes a uint value to the specified PCI configuration address
        /// </summary>
        /// <param name="pciAddress">PCI device address</param>
        /// <param name="regAddress">Configuration register address</param>
        /// <param name="value">uint value to write to the configuration register</param>
        public void WritePciConfigDword(uint pciAddress, uint regAddress, uint value) {
            WritePciConfigDwordEx(pciAddress, regAddress, value);
        }

        /// <summary>
        /// Writes a uint value to the specified PCI configuration address
        /// </summary>
        /// <param name="pciAddress">PCI device address</param>
        /// <param name="regAddress">Configuration register address</param>
        /// <param name="value">uint value to write to the configuration register</param>
        /// <returns><see lang="true"/> if the function succeeds</returns>
        public bool WritePciConfigDwordEx(uint pciAddress, uint regAddress, uint value) {

            // Check uint boundary alignment
            if ((regAddress & 3) != 0) {
                return false;
            }

            WritePciConfigInputDword pciData = new WritePciConfigInputDword {
                PciAddress = pciAddress,
                PciOffset  = regAddress,
                Data       = value
            };

            return DeviceIoControl(IoControlCode.WR0_WRITE_PCI_CONFIG, pciData);
        }

        /// <summary>
        /// PCI address and register offset used by <see cref="DeviceIoControl"/> for reading from PCI device (OlsIoctl.h OLS_READ_PCI_CONFIG_INPUT)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ReadPciConfigInput {
            public uint PciAddress;
            public uint PciOffset;
        }

        /// <summary>
        /// PCI address, register offset, and byte value used by <see cref="DeviceIoControl"/> for writing bytes to PCI device
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WritePciConfigInputByte {
            public uint PciAddress;
            public uint PciOffset;
            public byte Data;
        }

        /// <summary>
        /// PCI address, register offset, and word value used by <see cref="DeviceIoControl"/> for writing words to PCI device
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WritePciConfigInputWord {
            public uint   PciAddress;
            public uint   PciOffset;
            public ushort Data;
        }

        /// <summary>
        /// PCI address, register offset, and dword value used by <see cref="DeviceIoControl"/> for writing dwords to PCI device
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WritePciConfigInputDword {
            public uint PciAddress;
            public uint PciOffset;
            public uint Data;
        }

        /// <summary>
        /// Maximum number of PCI buses assigned by <see cref="SetPciMaxBusIndex"/>
        /// </summary>
        private byte gPciNumberOfBus = 255;

        /// <summary>
        /// Maximum number of PCI devices per bus
        /// </summary>
        private const byte gPciNumberOfDevice = 32;

        /// <summary>
        /// Maximum number of PCI functions per device
        /// </summary>
        private const byte gPciNumberOfFunction = 8;

        #endregion

        #region IO Port

        /// <summary>
        /// Reads a byte value from the specified I/O port address
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <returns>Byte value read from the specified <paramref name="port">I/O port address</paramref></returns>
        public byte ReadIoPortByte(ushort port) {

            ReadIoPortByteEx(port, out byte output);

            return output;
        }

        /// <summary>
        /// Reads a byte value from the specified I/O port address
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="output">Byte value read from the specified <paramref name="port">I/O port address</paramref></param>
        /// <returns><see lang="true"/> if the function succeeds</returns>
        public bool ReadIoPortByteEx(ushort port, out byte output) {

            output = byte.MaxValue;

            ReadIoPortInput portData = new ReadIoPortInput {
                PortNumber = port
            };

            return DeviceIoControl(IoControlCode.WR0_READ_IO_PORT_BYTE, portData, ref output);
        }

        /// <summary>
        /// Reads a ushort value from the specified I/O port address
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <returns>ushort value read from the specified <paramref name="port">I/O port address</paramref></returns>
        public ushort ReadIoPortWord(ushort port) {

            ReadIoPortWordEx(port, out ushort output);

            return output;
        }

        /// <summary>
        /// Reads a ushort value from the specified I/O port address
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="output">ushort value read from the specified <paramref name="port">I/O port address</paramref></param>
        /// <returns><see lang="true"/> if the function succeeds</returns>
        public bool ReadIoPortWordEx(ushort port, out ushort output) {

            output = ushort.MaxValue;

            ReadIoPortInput portData = new ReadIoPortInput {
                PortNumber = port
            };

            return DeviceIoControl(IoControlCode.WR0_READ_IO_PORT_WORD, portData, ref output);
        }

        /// <summary>
        /// Reads a uint value from the specified I/O port address
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <returns>uint value read from the specified <paramref name="port">I/O port address</paramref></returns>
        public uint ReadIoPortDword(ushort port) {

            ReadIoPortDwordEx(port, out uint output);

            return output;
        }

        /// <summary>
        /// Reads a uint value from the specified I/O port address
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="output">uint value read from the specified <paramref name="port">I/O port address</paramref></param>
        /// <returns><see lang="true"/> if the function succeeds</returns>
        public bool ReadIoPortDwordEx(ushort port, out uint output) {

            output = uint.MaxValue;

            ReadIoPortInput portData = new ReadIoPortInput {
                PortNumber = port
            };

            return DeviceIoControl(IoControlCode.WR0_READ_IO_PORT_DWORD, portData, ref output);
        }

        /// <summary>
        /// Writes a byte value to the specified I/O port address
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="value">Byte value to write to the port</param>
        public void WriteIoPortByte(ushort port, byte value) {
            WriteIoPortByteEx(port, value);
        }

        /// <summary>
        /// Writes a byte value to the specified I/O port address
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="value">Byte value to write to the port</param>
        /// <returns><see lang="true"/> if the function succeeds</returns>
        public bool WriteIoPortByteEx(ushort port, byte value) {

            WriteIoPortInput portData = new WriteIoPortInput {
                PortNumber = port,
                Data       = value
            };

            return DeviceIoControl(IoControlCode.WR0_WRITE_IO_PORT_BYTE, portData);
        }

        /// <summary>
        /// Writes a ushort value to the specified I/O port address
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="value">ushort value to write to the port</param>
        public void WriteIoPortWord(ushort port, ushort value) {
            WriteIoPortWordEx(port, value);
        }

        /// <summary>
        /// Writes a ushort value to the specified I/O port address
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="value">ushort value to write to the port</param>
        /// <returns><see lang="true"/> if the function succeeds</returns>
        public bool WriteIoPortWordEx(ushort port, ushort value) {

            WriteIoPortInput portData = new WriteIoPortInput {
                PortNumber = port,
                Data       = value
            };

            return DeviceIoControl(IoControlCode.WR0_WRITE_IO_PORT_WORD, portData);
        }

        /// <summary>
        /// Writes a uint value to the specified I/O port address
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="value">uint value to write to the port</param>
        public void WriteIoPortDword(ushort port, uint value) {
            WriteIoPortDwordEx(port, value);
        }

        /// <summary>
        /// Writes a uint value to the specified I/O port address
        /// </summary>
        /// <param name="port">I/O port address</param>
        /// <param name="value">uint value to write to the port</param>
        /// <returns><see lang="true"/> if the function succeeds</returns>
        public bool WriteIoPortDwordEx(ushort port, uint value) {

            WriteIoPortInput portData = new WriteIoPortInput {
                PortNumber = port,
                Data       = value
            };

            return DeviceIoControl(IoControlCode.WR0_WRITE_IO_PORT_DWORD, portData);
        }

        /// <summary>
        /// IO Port address used by <see cref="DeviceIoControl"/> for reading from an I/O port
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ReadIoPortInput {
            public uint PortNumber;
        }

        /// <summary>
        /// IO Port address and value used by <see cref="DeviceIoControl"/> for writing to an I/O port
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WriteIoPortInput {
            public uint PortNumber;
            public uint Data;
        }

        #endregion

        /// <summary>
        /// Driver and service name
        /// </summary>
        private const string Name = "WinRing0_1_2_0"; // WinRing0_1_2_0

        /// <summary>
        /// Service operation timeout
        /// </summary>
        private const int TIMEOUT = 1000;

        /// <summary>
        /// IO device handle
        /// </summary>
        private static SafeFileHandle _deviceHandle;

        /// <summary>
        /// Service controller for the driver
        /// </summary>
        private static ServiceController _sc = new ServiceController(Name);

        /// <summary>
        /// Driver ready state
        /// </summary>
        private static bool _isReady => _deviceHandle != null && !_deviceHandle.IsInvalid && !_deviceHandle.IsClosed;

        /// <summary>
        /// Service control manager pointer
        /// </summary>
        private IntPtr _managerPtr = Win32BaseApi.OpenSCManager(dwAccess: Win32BaseApi.ServiceAccessRights.SC_MANAGER_ALL_ACCESS);

        /// <summary>
        /// Service object pointer
        /// </summary>
        private IntPtr _servicePtr;

        /// <summary>
        /// Path to driver file
        /// </summary>
        private static string _fileName => $"{Core.ParentPath}\\spdrwdrv.sys";

        /// <summary>
        /// Indicates whether the driver service should be stopped and deleted on exit
        /// </summary>
        private bool _disposeOnExit = true;

        /// <summary>
        /// WinRing0 IO control codes
        /// </summary>
        internal struct IoControlCode {
            /// <summary>
            /// Winring0 Device type code
            /// </summary>
            public static readonly uint WR0_DEVICE_TYPE = 0x9C40;     // 40000

            public static uint WR0_GET_DRIVER_VERSION   = 0x9C402000; // CTL_CODE(function: 0x800, access: IOCTL_ACCESS.FILE_ANY_ACCESS);
            public static uint WR0_GET_REFCOUNT         = 0x9C402004; // CTL_CODE(function: 0x801, access: IOCTL_ACCESS.FILE_ANY_ACCESS);
            public static uint WR0_READ_MSR             = 0x9C402084; // CTL_CODE(function: 0x821, access: IOCTL_ACCESS.FILE_ANY_ACCESS);
            public static uint WR0_WRITE_MSR            = 0x9C402088; // CTL_CODE(function: 0x822, access: IOCTL_ACCESS.FILE_ANY_ACCESS);
            public static uint WR0_READ_PMC             = 0x9C40208C; // CTL_CODE(function: 0x823, access: IOCTL_ACCESS.FILE_ANY_ACCESS);
            public static uint WR0_HALT                 = 0x9C402090; // CTL_CODE(function: 0x824, access: IOCTL_ACCESS.FILE_ANY_ACCESS);
            public static uint WR0_READ_IO_PORT         = 0x9C4060C4; // CTL_CODE(function: 0x831, access: IOCTL_ACCESS.FILE_READ_DATA); 
            public static uint WR0_WRITE_IO_PORT        = 0x9C40A0C8; // CTL_CODE(function: 0x832, access: IOCTL_ACCESS.FILE_WRITE_DATA);
            public static uint WR0_READ_IO_PORT_BYTE    = 0x9C4060CC; // CTL_CODE(function: 0x833, access: IOCTL_ACCESS.FILE_READ_DATA); 
            public static uint WR0_READ_IO_PORT_WORD    = 0x9C4060D0; // CTL_CODE(function: 0x834, access: IOCTL_ACCESS.FILE_READ_DATA); 
            public static uint WR0_READ_IO_PORT_DWORD   = 0x9C4060D4; // CTL_CODE(function: 0x835, access: IOCTL_ACCESS.FILE_READ_DATA); 
            public static uint WR0_WRITE_IO_PORT_BYTE   = 0x9C40A0D8; // CTL_CODE(function: 0x836, access: IOCTL_ACCESS.FILE_WRITE_DATA);
            public static uint WR0_WRITE_IO_PORT_WORD   = 0x9C40A0DC; // CTL_CODE(function: 0x837, access: IOCTL_ACCESS.FILE_WRITE_DATA);
            public static uint WR0_WRITE_IO_PORT_DWORD  = 0x9C40A0E0; // CTL_CODE(function: 0x838, access: IOCTL_ACCESS.FILE_WRITE_DATA);
            public static uint WR0_READ_PCI_CONFIG      = 0x9C406144; // CTL_CODE(function: 0x851, access: IOCTL_ACCESS.FILE_READ_DATA); 
            public static uint WR0_WRITE_PCI_CONFIG     = 0x9C40A148; // CTL_CODE(function: 0x852, access: IOCTL_ACCESS.FILE_WRITE_DATA);
        }
    }
}