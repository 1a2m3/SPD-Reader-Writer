using System;

namespace SpdReaderWriterDll {
    
    /// <summary>
    /// Configurable settings
    /// </summary>
    public class Settings {

        /// <summary>
        /// Serial port baud rate
        /// </summary>
        public const int BAUDRATE = 115200; //115200;

        /// <summary>
        /// DLL version
        /// </summary>
        public const int DLLVERSION = 20210226;

        /// <summary>
        /// Minimum device's firmware version required 
        /// </summary>
        public static int MINVERSION = 20210212;
    }
}
