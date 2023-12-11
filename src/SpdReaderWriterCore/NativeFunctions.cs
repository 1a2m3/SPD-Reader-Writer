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
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Microsoft.Win32.SafeHandles;

namespace SpdReaderWriterCore {
    public static class NativeFunctions {

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
            /// <param name="hTemplateFile">A valid handle to a template file with the <see cref="FileAccess.GenericRead"/> access right.</param>
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
            /// Generic Access Rights
            /// </summary>
            [Flags]
            public enum FileAccess : uint {
                /// <summary>
                /// Read access
                /// </summary>
                GenericRead  = 0x80000000,

                /// <summary>
                /// Write access
                /// </summary>
                GenericWrite = 0x40000000
            }

            /// <summary>
            /// Closes an open object handle.
            /// </summary>
            /// <param name="hObject">A valid handle to an open object.</param>
            /// <returns>If the function succeeds, the return value is <see langword="true"/>.</returns>
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr hObject);

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
        }

        /// <summary>
        /// Advanced Windows 32 Base API
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
            public static extern IntPtr OpenSCManager(
                string machineName,
                string databaseName,
                ServiceAccessRights dwAccess);

            /// <summary>
            /// Service Security and Access Rights for the Service Control Manager
            /// </summary>
            [Flags]
            public enum ServiceAccessRights : uint {
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
            public static extern IntPtr CreateService(
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
            /// Starts a service.
            /// </summary>
            /// <param name="hService">A handle to the service.</param>
            /// <param name="dwNumServiceArgs">The number of strings in the lpServiceArgVectors array.</param>
            /// <param name="lpServiceArgVectors">The null-terminated strings to be passed to the ServiceMain function for the service as arguments.</param>
            /// <returns></returns>
            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool StartService(
                IntPtr hService,
                uint dwNumServiceArgs,
                string[] lpServiceArgVectors);

            /// <summary>
            /// The severity of the error, and action taken, if this service fails to start
            /// </summary>
            public enum ErrorControl : uint {

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
            /// System Error Codes returned by <see cref="Marshal.GetLastWin32Error"/>
            /// </summary>
            public struct SystemError {
                /// <summary>
                /// The operation completed successfully
                /// </summary>
                public const int Success                      = 0x00;

                /// <summary>
                /// The system cannot find the file specified.
                /// </summary>
                public const int ErrorFileNotFound            = 0x02;

                /// <summary>
                /// The system cannot find the path specified.
                /// </summary>
                public const int ErrorPathNotFound            = 0x03;

                /// <summary>
                /// Access is denied.
                /// </summary>
                public const int ErrorAccessDenied            = 0x05;

                /// <summary>
                /// The handle is invalid.
                /// </summary>
                public const int ErrorInvalidHandle           = 0x06;

                /// <summary>
                /// %1 is not a valid Win32 application.
                /// </summary>
                public const int ErrorBadExeFormat            = 0xC1;

                /// <summary>
                /// An instance of the service is already running.
                /// </summary>
                public const int ServiceAlreadyRunning        = 0x420;

                /// <summary>
                /// The specified service does not exist as an installed service.
                /// </summary>
                public const int ErrorServiceDoesNotExist     = 0x424;

                /// <summary>
                /// The service cannot accept control messages at this time.
                /// </summary>
                public const int ErrorServiceCannotAcceptCtrl = 0x425;

                /// <summary>
                /// The specified service already exists.
                /// </summary>
                public const int ErrorServiceExists           = 0x431;

                /// <summary>
                /// Not all privileges or groups referenced are assigned to the caller.
                /// </summary>
                public const int ErrorNotAllAssigned          = 0x514;
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
            /// <param name="hSCManager">A handle to the service control manager database.</param>
            /// <param name="lpServiceName">The name of the service to be opened</param>
            /// <param name="dwDesiredAccess">The access to the service.</param>
            /// <returns>If the function succeeds, the return value is a handle to the service. If the function fails, the return value is <see cref="IntPtr.Zero"/>.</returns>
            [DllImport("advapi32.dll", EntryPoint = "OpenServiceW", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, ServiceRights dwDesiredAccess);

            /// <summary>
            /// Specific access rights for a service
            /// </summary>
            [Flags]
            public enum ServiceRights : uint {

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
            public struct ServiceStatus {
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
            [Flags]
            public enum ServiceStatusServiceType : uint {

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
            public enum ServiceStatusCurrentState : uint {

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
            public enum ServiceStatusControlsAccepted : uint {

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
            /// Handles to service control manager objects are returned by the <see cref="OpenSCManager"/> function,
            /// and handles to service objects are returned by either the <see cref="OpenService"/> or <see cref="CreateService"/> function.</param>
            /// <returns><see langword="true"/> if the function succeeds</returns>
            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseServiceHandle(IntPtr hSCObject);

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
                StandardRightsRequired = 0x000F0000,

                StandardRightsRead     = 0x00020000,

                /// <summary>
                /// Required to attach a primary token to a process.
                /// </summary>
                TokenAssignPrimary     = 0x0001,

                /// <summary>
                /// Required to duplicate an access token.
                /// </summary>
                TokenDuplicate         = 0x0002,

                /// <summary>
                /// Required to attach an impersonation access token to a process.
                /// </summary>
                TokenImpersonate       = 0x0004,

                /// <summary>
                /// Required to query an access token.
                /// </summary>
                TokenQuery             = 0x0008,

                /// <summary>
                /// Required to query the source of an access token.
                /// </summary>
                TokenQuerySource       = 0x0010,

                /// <summary>
                /// Required to enable or disable the privileges in an access token.
                /// </summary>
                TokenAdjustPrivileges  = 0x0020,


                /// <summary>
                /// Required to adjust the attributes of the groups in an access token.
                /// </summary>
                TokenAdjustGroups      = 0x0040,

                /// <summary>
                /// Required to change the default owner, primary group, or DACL of an access token.
                /// </summary>
                TokenAdjustDefault     = 0x0080,

                /// <summary>
                /// Required to adjust the session ID of an access token. The SE_TCB_NAME privilege is required.
                /// </summary>
                TokenAdjustSessionid   = 0x0100,

                /// <summary>
                /// Combines <see cref ="StandardRightsRead"/> and <see cref="TokenQuery"/>.
                /// </summary>
                TokenRead              = StandardRightsRead |
                                          TokenQuery,
                /// <summary>
                /// Combines all possible access rights for a token.
                /// </summary>
                TokenAllAccess         = StandardRightsRequired |
                                         TokenAssignPrimary |
                                         TokenDuplicate |
                                         TokenImpersonate |
                                         TokenQuery |
                                         TokenQuerySource |
                                         TokenAdjustPrivileges |
                                         TokenAdjustGroups |
                                         TokenAdjustDefault |
                                         TokenAdjustSessionid
            }

            /// <summary>
            /// The LookupPrivilegeValue function retrieves the locally unique identifier (<see cref="LUID"/>) used on a specified system to locally represent the specified privilege name.
            /// </summary>
            /// <param name="lpSystemName">The name of the system on which the privilege name is retrieved.
            /// If a null string is specified, the function attempts to find the privilege name on the local system.</param>
            /// <param name="lpName">The name of the privilege.</param>
            /// <param name="lpLuid">A pointer to a variable that receives the <see cref="LUID"/> by which the privilege is known on the system specified by the <paramref name="lpSystemName"/> parameter.</param>
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
            /// Enables or disables privileges in the specified access token.
            /// </summary>
            /// <param name="tokenHandle">A handle to the access token that contains the privileges to be modified.</param>
            /// <param name="disableAllPrivileges">Specifies whether the function disables all of the token's privileges.</param>
            /// <param name="newState">A pointer to a <see cref="TokenPrivileges"/> structure that specifies an array of privileges and their attributes.</param>
            /// <param name="bufferLengthInBytes">Specifies the size, in bytes, of the buffer pointed to by the <paramref name="previousState"/> parameter.</param>
            /// <param name="previousState">A pointer to a buffer that the function fills with a <see cref="TokenPrivileges"/> structure that contains the previous state of any privileges that the function modifies.</param>
            /// <param name="returnLengthInBytes">A pointer to a variable that receives the required size, in bytes, of the buffer pointed to by the <paramref name="previousState"/> parameter.</param>
            /// <returns></returns>
            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AdjustTokenPrivileges(IntPtr tokenHandle,
                [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
                ref TokenPrivileges newState,
                uint bufferLengthInBytes,
                [Optional] ref TokenPrivileges previousState,
                [Optional] out uint returnLengthInBytes);

            /// <summary>
            /// Contains information about a set of privileges for an access token.
            /// </summary>
            public struct TokenPrivileges {
                /// <summary>
                /// This must be set to the number of entries in the <see cref="TokenPrivileges.Privileges"/> array.
                /// </summary>
                public int PrivilegeCount;

                /// <summary>
                /// Specifies an array of <see cref="LuidAndAttributes"/> structures.
                /// </summary>
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
                public LuidAndAttributes[] Privileges;
            }

            /// <summary>
            /// Represents a locally unique identifier (<see cref="LUID"/>) and its attributes.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct LuidAndAttributes {
                /// <summary>
                /// Specifies an <see cref="LUID"/> value
                /// </summary>
                public LUID Luid;

                /// <summary>
                /// Specifies attributes of the <see cref="LUID"/>
                /// </summary>
                public LuidAttributes Attributes;
            }

            /// <summary>
            /// Privilege attributes
            /// </summary>
            [Flags]
            public enum LuidAttributes : uint {
                SePrivilegeEnabledByDefault = 0x00000001,
                SePrivilegeEnabled          = 0x00000002,
                SePrivilegeRemoved          = 0x00000004,
                SePrivilegeUsedForAccess    = 0x80000000,
                SePrivilegeValidAttributes  = SePrivilegeEnabledByDefault | 
                                              SePrivilegeEnabled | 
                                              SePrivilegeRemoved | 
                                              SePrivilegeUsedForAccess
            }
        }
    }
}