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
        /// Executing Assembly Product Version
        /// </summary>
        public static string AssemblyVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
    }
}