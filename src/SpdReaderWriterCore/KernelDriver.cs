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
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Microsoft.Win32.SafeHandles;

namespace SpdReaderWriterCore {

    /// <summary>
    /// Kernel Driver class
    /// </summary>
    public static class KernelDriver {

        /// <summary>
        /// Kernel Driver Info struct
        /// </summary>
        public struct KernelDriverInfo {
            /// <summary>
            /// Driver service name
            /// </summary>
            public string Name;
            /// <summary>
            /// Binary file path
            /// </summary>
            public string Path;
        }

        /// <summary>
        /// Windows NT Kernel BASE API
        /// </summary>
        public static class NtBaseApi {

            /// <summary>
            /// Creates or opens a file or I/O device.
            /// </summary>
            /// <param name="lpFileName">The name of the file or device to be created or opened.</param>
            /// <param name="dwDesiredAccess">The requested access to the file or device, which can be of <see cref="FileAccess"/> values.</param>
            /// <param name="dwShareMode">The requested sharing mode of the file or device, which can be of <see cref="FileShare"/> values.</param>
            /// <param name="lpSecurityAttributes">A pointer to optional SECURITY_ATTRIBUTES structure or IntPtr.Zero</param>
            /// <param name="dwCreationDisposition">An action to take on a file or device that exists or does not exist. For devices other than files, this parameter is usually set to <see cref="FileMode.Open"/>.</param>
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
            /// <param name="dwIoControlCode">The control code for the operation. This value identifies the specific operation to be performed and the type of device on which to perform it.</param>
            /// <param name="lpInBuffer">A pointer to the input buffer that contains the data required to perform the operation.</param>
            /// <param name="nInBufferSize">The size of the input buffer, in bytes.</param>
            /// <param name="lpOutBuffer">A pointer to the output buffer that is to receive the data returned by the operation.</param>
            /// <param name="nOutBufferSize">The size of the output buffer, in bytes.</param>
            /// <param name="lpBytesReturned">A pointer to a variable that receives the size of the data stored in the output buffer, in bytes.</param>
            /// <param name="lpOverlapped">A pointer to an OVERLAPPED structure.</param>
            /// <returns>If the operation completes successfully, the return value is nonzero (<see lang="true"/>). If the operation fails or is pending, the return value is zero.</returns>
            [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool DeviceIoControl(
                SafeFileHandle hDevice,
                uint dwIoControlCode,
                [MarshalAs(UnmanagedType.AsAny)][In] object lpInBuffer,
                uint nInBufferSize,
                [MarshalAs(UnmanagedType.AsAny)][Out] object lpOutBuffer,
                uint nOutBufferSize,
                out uint lpBytesReturned,
                IntPtr lpOverlapped);

            /// <summary>
            /// Defines a new IO Control Code
            /// </summary>
            /// <param name="deviceType">Identifies the device type.</param>
            /// <param name="function">Identifies the function to be performed by the driver.</param>
            /// <param name="method">Indicates how the system will pass data between the caller of <see cref="DeviceIoControl"/> and the driver that handles the IRP. Use one of the <see cref="IoctlMethod"/> constants.</param>
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
                METHOD_BUFFERED,

                /// <summary>
                /// Specifies the direct I/O method, which is typically used for writing large amounts of data, using DMA or PIO, that must be transferred quickly.
                /// </summary>
                METHOD_IN_DIRECT,

                /// <summary>
                /// Specifies the direct I/O method, which is typically used for reading large amounts of data, using DMA or PIO, that must be transferred quickly.
                /// </summary>
                METHOD_OUT_DIRECT,

                /// <summary>
                /// Specifies neither buffered nor direct I/O. The I/O manager does not provide any system buffers or MDLs.
                /// </summary>
                METHOD_NEITHER,
            }

            /// <summary>
            /// Indicates the type of access that a caller must request when opening the file object that represents the device.
            /// </summary>
            internal enum IoctlAccess : byte {

                /// <summary>
                /// The I/O manager sends the IRP for any caller that has a handle to the file object that represents the target device object.
                /// </summary>
                FILE_ANY_ACCESS,

                /// <summary>
                /// The I/O manager sends the IRP only for a caller with read access rights, allowing the underlying device driver to transfer data from the device to system memory.
                /// </summary>
                FILE_READ_DATA,

                /// <summary>
                /// The I/O manager sends the IRP only for a caller with write access rights, allowing the underlying device driver to transfer data from system memory to its device.
                /// </summary>
                FILE_WRITE_DATA,

                /// <summary>
                /// The caller must have both read and write access rights
                /// </summary>
                FILE_READ_WRITE_DATA = FILE_READ_DATA | FILE_WRITE_DATA,
            }

            /// <summary>
            /// Retrieves the calling thread's last-error code value.
            /// </summary>
            /// <returns>Calling thread's last-error code</returns>
            [DllImport("kernel32.dll")]
            internal static extern ushort GetLastError();
        }

        /// <summary>
        /// Advanced Windows 32 Base API (services)
        /// </summary>
        public static class Win32BaseApi {

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
            internal static IntPtr OpenSCManager(ServiceAccessRights dwAccess) {
                return OpenSCManager(null, null, dwAccess);
            }

            /// <summary>
            /// Service Security and Access Rights for the Service Control Manager
            /// </summary>
            internal enum ServiceAccessRights : uint {
                SC_MANAGER_ALL_ACCESS         = 0xF003F,
                SC_MANAGER_CREATE_SERVICE     = 0x00002,
                SC_MANAGER_CONNECT            = 0x00001,
                SC_MANAGER_ENUMERATE_SERVICE  = 0x00004,
                SC_MANAGER_LOCK               = 0x00008,
                SC_MANAGER_MODIFY_BOOT_CONFIG = 0x00020,
                SC_MANAGER_QUERY_LOCK_STATUS  = 0x00010,
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
                SERVICE_ERROR_IGNORE   = 0x00000000,

                /// <summary>
                /// The startup program logs the error in the event log but continues the startup operation
                /// </summary>
                SERVICE_ERROR_NORMAL   = 0x00000001,

                /// <summary>
                /// The startup program logs the error in the event log.
                /// If the last-known-good configuration is being started, the startup operation continues.
                /// Otherwise, the system is restarted with the last-known-good configuration.
                /// </summary>
                SERVICE_ERROR_SEVERE   = 0x00000002,

                /// <summary>
                /// The startup program logs the error in the event log, if possible
                /// </summary>
                SERVICE_ERROR_CRITICAL = 0x00000003,
            }

            /// <summary>
            /// Windows error codes returned by <see cref="Marshal.GetHRForLastWin32Error"/>
            /// </summary>
            internal enum WinError {
                /// <summary>
                /// The operation completed successfully
                /// </summary>
                NO_ERROR                = unchecked((int)0x80070000),
                /// <summary>
                /// The specified service already exists
                /// </summary>
                SERVICE_EXISTS          = unchecked((int)0x80070431),
                /// <summary>
                /// An instance of the service is already running
                /// </summary>
                SERVICE_ALREADY_RUNNING = unchecked((int)0x80070420),
            }

            /// <summary>
            /// Marks the specified service for deletion from the service control manager database.
            /// </summary>
            /// <param name="hService">A handle to the service.
            /// This handle is returned by the <see cref="OpenService"/> or <see cref="CreateService"/> function, and it must have the DELETE access right.</param>
            /// <returns><see langref="true"/> if the function succeeds</returns>
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
            internal enum ServiceRights : uint {

                /// <summary>
                /// Required to call the QueryServiceConfig and QueryServiceConfig2 functions to query the service configuration.
                /// </summary>
                SERVICE_QUERY_CONFIG         = 0x00001,

                /// <summary>
                /// Required to call the ChangeServiceConfig or ChangeServiceConfig2 function to change the service configuration. 
                /// </summary>
                SERVICE_CHANGE_CONFIG        = 0x00002,

                /// <summary>
                /// Required to call the QueryServiceStatus or QueryServiceStatusEx function to ask the service control manager about the status of the service.
                /// </summary>
                SERVICE_QUERY_STATUS         = 0x00004,

                /// <summary>
                /// Required to call the EnumDependentServices function to enumerate all the services dependent on the service.
                /// </summary>
                SERVICE_ENUMERATE_DEPENDENTS = 0x00008,

                /// <summary>
                /// Required to call the <see cref="StartService"/> function to start the service.
                /// </summary>
                SERVICE_START                = 0x00010,

                /// <summary>
                /// Required to call the <see cref="ControlService"/> function to stop the service.
                /// </summary>
                SERVICE_STOP                 = 0x00020,

                /// <summary>
                /// Required to call the <see cref="ControlService"/> function to pause or continue the service.
                /// </summary>
                SERVICE_PAUSE_CONTINUE       = 0x00040,

                /// <summary>
                /// Required to call the <see cref="ControlService"/> function to ask the service to report its status immediately.
                /// </summary>
                SERVICE_INTERROGATE          = 0x00080,

                /// <summary>
                /// Required to call the <see cref="ControlService"/> function to specify a user-defined control code.
                /// </summary>
                SERVICE_USER_DEFINED_CONTROL = 0x00100,

                /// <summary>
                /// The right to delete the object.
                /// </summary>
                DELETE                       = 0x10000,

                /// <summary>
                /// The right to read the information in the object's security descriptor, not including the information in the system access control list (SACL).
                /// </summary>
                READ_CONTROL                 = 0x20000,

                /// <summary>
                /// The right to modify the discretionary access control list (DACL) in the object's security descriptor.
                /// </summary>
                WRITE_DAC                    = 0x40000,

                /// <summary>
                /// The right to change the owner in the object's security descriptor.
                /// </summary>
                WRITE_OWNER                  = 0x80000,

                /// <summary>
                /// Combines DELETE, READ_CONTROL, WRITE_DAC, and WRITE_OWNER access.
                /// </summary>
                STANDARD_RIGHTS_REQUIRED     = DELETE | READ_CONTROL | WRITE_DAC | WRITE_OWNER,

                /// <summary>
                /// Includes <see cref="STANDARD_RIGHTS_REQUIRED"/> in addition to all access rights in this table.
                /// </summary>
                SERVICE_ALL_ACCESS           = STANDARD_RIGHTS_REQUIRED |
                                               SERVICE_QUERY_CONFIG |
                                               SERVICE_CHANGE_CONFIG |
                                               SERVICE_QUERY_STATUS |
                                               SERVICE_ENUMERATE_DEPENDENTS |
                                               SERVICE_START |
                                               SERVICE_STOP |
                                               SERVICE_PAUSE_CONTINUE |
                                               SERVICE_INTERROGATE |
                                               SERVICE_USER_DEFINED_CONTROL
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
                /// The service is a file system driver. 
                /// </summary>
                SERVICE_FILE_SYSTEM_DRIVER  = 0x00000002,

                /// <summary>
                /// The service is a device driver. 
                /// </summary>
                SERVICE_KERNEL_DRIVER       = 0x00000001,

                /// <summary>
                /// The service runs in its own process. 
                /// </summary>
                SERVICE_WIN32_OWN_PROCESS   = 0x00000010,

                /// <summary>
                /// The service shares a process with other services. 
                /// </summary>
                SERVICE_WIN32_SHARE_PROCESS = 0x00000020,

                /// <summary>
                /// The service runs in its own process under the logged-on user account. 
                /// </summary>
                SERVICE_USER_OWN_PROCESS    = 0x00000050,

                /// <summary>
                /// The service shares a process with one or more other services that run under the logged-on user account. 
                /// </summary>
                SERVICE_USER_SHARE_PROCESS  = 0x00000060,
            }

            /// <summary>
            /// The current state of the service for <see cref="ServiceStatus"/>. 
            /// </summary>
            internal enum ServiceStatusCurrentState : uint {

                /// <summary>
                /// The service continue is pending. 
                /// </summary>
                SERVICE_CONTINUE_PENDING = 0x00000005,

                /// <summary>
                /// The service pause is pending. 
                /// </summary>
                SERVICE_PAUSE_PENDING    = 0x00000006,

                /// <summary>
                /// The service is paused.
                /// </summary>
                SERVICE_PAUSED           = 0x00000007,

                /// <summary>
                /// The service is running. 
                /// </summary>
                SERVICE_RUNNING          = 0x00000004,

                /// <summary>
                /// The service is starting. 
                /// </summary>
                SERVICE_START_PENDING    = 0x00000002,

                /// <summary>
                /// The service is stopping. 
                /// </summary>
                SERVICE_STOP_PENDING     = 0x00000003,

                /// <summary>
                /// The service is not running. 
                /// </summary>
                SERVICE_STOPPED          = 0x00000001,
            }

            /// <summary>
            /// The control codes the service accepts and processes in its handler function for <see cref="ServiceStatus"/>.
            /// </summary>
            internal enum ServiceStatusControlsAccepted : uint {

                /// <summary>
                /// The service is a network component that can accept changes in its binding without being stopped and restarted.
                /// </summary>
                SERVICE_ACCEPT_NETBINDCHANGE  = 0x00000010,

                /// <summary>
                /// The service can reread its startup parameters without being stopped and restarted.
                /// </summary>
                SERVICE_ACCEPT_PARAMCHANGE    = 0x00000008,

                /// <summary>
                /// The service can be paused and continued.
                /// </summary>
                SERVICE_ACCEPT_PAUSE_CONTINUE = 0x00000002,

                /// <summary>
                /// The service can perform preshutdown tasks.
                /// </summary>
                SERVICE_ACCEPT_PRESHUTDOWN    = 0x00000100,

                /// <summary>
                /// The service is notified when system shutdown occurs.
                /// </summary>
                SERVICE_ACCEPT_SHUTDOWN       = 0x00000004,

                /// <summary>
                /// The service can be stopped.
                /// </summary>
                SERVICE_ACCEPT_STOP           = 0x00000001,
            }

            /// <summary>
            /// Closes a handle to a service control manager or service object
            /// </summary>
            /// <param name="hSCObject">A handle to the service control manager object or the service object to close.
            /// Handles to service control manager objects are returned by the <see cref="OpenSCManager(string,string,ServiceAccessRights)"/> function,
            /// and handles to service objects are returned by either the <see cref="OpenService"/> or <see cref="CreateService"/> function.</param>
            /// <returns><see langref="true"/> if the function succeeds</returns>
            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseServiceHandle(IntPtr hSCObject);
        }
    }
}