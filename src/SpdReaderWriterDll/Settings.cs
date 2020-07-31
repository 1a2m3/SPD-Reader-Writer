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
		public const int VERSION = 20200731;

		/// <summary>
		/// Minimum device's firmware version required 
		/// </summary>
		public static int MinimumVersionRequired = 20200731;
	}
}
