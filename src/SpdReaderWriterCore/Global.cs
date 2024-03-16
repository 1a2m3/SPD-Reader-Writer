/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;

namespace SpdReaderWriterCore {
    /// <summary>
    /// Core class
    /// </summary>
    public class Core {

        /// <summary>
        /// Executing assembly path
        /// </summary>
        public static string ParentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <summary>
        /// Executing assembly name
        /// </summary>
        public static string AssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;

        /// <summary>
        /// Current process name
        /// </summary>
        public static string ProcessName = Process.GetCurrentProcess().ProcessName;

        /// <summary>
        /// DLL File Version
        /// </summary>
        public static string CoreFileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;

        /// <summary>
        /// DLL Product Version
        /// </summary>
        public static string CoreProductVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

        /// <summary>
        /// Executing program file version
        /// </summary>
        public static string ExecutingProgramFileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()?.Location ?? string.Empty).FileVersion;

        /// <summary>
        /// Executing program product version
        /// </summary>
        public static string ExecutingProgramProductVersion = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()?.Location ?? string.Empty).ProductVersion;

        /// <summary>
        /// Detects if administrative privileges are present
        /// </summary>
        /// <returns><see langword="true"/> if administrative privileges are present</returns>
        public static bool IsAdmin() {

            try {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch {
                return false;
            }
        }
    }
}