using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SpdReaderWriterDll;

namespace SpdReaderWriter {
	class Program {
		static void Main(string[] args) {

			Welcome();

			if (args.Length > 0) {
#if DEBUG
				// Display command line arguments in console title
				if (Debugger.IsAttached) {
					Console.Title = ($"{AppDomain.CurrentDomain.FriendlyName} ");
					foreach (string cmd in args) {
						Console.Title += ($"{cmd} ");
					}
				}
#endif
				ParseCommand(args);
			}
			else {
				ShowHelp();
			}

#if DEBUG
			// Wait for input to prevent application from closing automatically when debugging
			if (Debugger.IsAttached) {
				Console.WriteLine("\nPress [enter] to quit.\n");
				Console.ReadLine();
			}
#endif
		}

		static void Welcome() {
			string[] header = {
				"Welcome to DDR4 SPD reader/writer",
				"~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
				"For PC enthusiasts & overclockers"
			};
			foreach (string line in header) {
				Console.WriteLine(line);
			}
		}
		static void ShowHelp() {
			string[] help = {
				"Command line parameters:",
				"",
				"{0} /help",
				"{0} /find",
				"{0} /scan <PORT>",
				"{0} /read <PORT> <ADDRESS#> <filepath> /silent",
				"{0} /write <PORT> <ADDRESS#> <FILEPATH> /silent",
				"{0} /writeforce <PORT> <ADDRESS#> <FILEPATH> /silent",
				"{0} /enablewriteprotection <PORT>",
				"{0} /enablewriteprotection <PORT> <block#>",
				"{0} /disablewriteprotection <PORT>",
				"",
				"Parameters in CAPS are mandatory!",
				"Parameter <filepath> is optional when /read switch is used, output will be printed to console only.",
				"Switch /silent is optional, progress won't be shown with this switch.",
				"",
				"For additional help, visit: https://github.com/1a2m3/ or https://forums.evga.com/FindPost/3053544",
				"",
				"This program is free to use, but if you like it and wish to support me, I am accepting donations via systems listed below:",
				"Paypal:  http://paypal.me/mik4rt3m",
				"Bitcoin: 3Pe9VhVaUygyMFGT3pFuQ3dAghS36NPJTz",
				""
			};

			foreach (string line in help) {
				Console.WriteLine(line, AppDomain.CurrentDomain.FriendlyName);
			}
		}

		static void ParseCommand(string[] args) {

			string mode = args[0];

			if (mode == "/help") {
				ShowHelp();
				return;
			}

			try {
				// Find
				if (mode == "/find") {
					// Find 
					string[] devices = Device.Find();
					if (devices.Length > 0) {
						foreach (string portName in devices) {
							Console.WriteLine($"Found Device on Serial Port: {portName}\n");
						}
					}
					else {
						throw new Exception("Nothing found");
					}
					return;
				}

				// Other functions that require additional parameters
				if (mode != "/find" && args.Length >= 2) {

					// Init
					string portName = args[1];

					if (!portName.StartsWith("COM")) {
						throw new Exception("Port name should start with \"COM\" followed by a number.");
					}

					Device reader = new Device(portName);

					if (!reader.Connect()) {
						throw new Exception($"Could not connect to the device on port {portName}.");
					}

					//if (!reader.Test()) {
					//	throw new Exception($"The device on port {portName} does not respond.");
					//}

					// Scan
					if (mode == "/scan") {
						int[] addresses = reader.Scan();

						if (addresses.Length == 0) {
							throw new Exception("No EEPROM devices found.");
						}

						foreach (int location in addresses) {
							Console.WriteLine($"Found EEPROM at address: {location}");
						}

						reader.Disconnect();
						return;
					}

					// Turn on write protection
					if (mode == "/enablewriteprotection") {

						Stack<int> block = new Stack<int>();

						if (args.Length == 3) { // Block # was specified
							try {
								block.Push(Int32.Parse(args[2]));
							}
							catch {
								throw new Exception("Block number should be specified in decimal notation.");
							}
						}
						else { // No block number specified, protect all
							for (int i = 3; i != -1; i--) { // Push from 3 to 0, so that the stack pops in correct numeric order
								block.Push(i);
							}
						}

						while (block.Count > 0) {
							int blocknumber = block.Pop();
							if (Eeprom.SetWriteProtection(reader, blocknumber)) {
								Console.WriteLine($"Block {blocknumber} is now read-only");
							}
							else {
								throw new Exception($"Unable to set write protection for block {blocknumber}. Either SA0 is not connected to HV, or the block is already read-only.");
							}
						}

						return;
					}

					// Disable write protection
					if (mode == "/disablewriteprotection") {

						if (Eeprom.ClearWriteProtection(reader)) {
							Console.WriteLine("Write protection successfully disabled.");
						}
						else {
							throw new Exception("Unable to clear write protection");
						}

						return;
					}

					int address;

					try {
						address = Int32.Parse(args[2]);
					}
					catch {
						throw new Exception("EEPROM address should be specified in decimal notation.");
					}

					reader.EepromAddress = address;
					reader.SpdSize = SpdSize.DDR4_SPD_SIZE;

					if (!reader.Probe()) {
						throw new Exception($"EEPROM is not present at address {reader.EepromAddress}.");
					}

					string filePath = (args.Length >= 4) ? args[3] : "";
					bool silent = (args.Length >= 5 && args[4] == "/silent") ? true : false;

					// Read SPD contents
					if (mode == "/read") {

						Console.WriteLine(Eeprom.GetRamType(reader));

						Console.Write($"Reading EEPROM at address {reader.EepromAddress}");

						if (filePath != "") {
							Console.WriteLine($" to {filePath}");
						}
						Console.WriteLine("\n");

						int startTick = Environment.TickCount;

						byte[] spdDump = Eeprom.ReadByte(reader, 0, (int)reader.SpdSize);

						for (int i = 0; i < spdDump.Length; i++) {
							if (!silent) {
								ConsoleDisplayByte(i, spdDump[i]);
							}
						}

						Console.Write("\n\nRead {0} {1} from EEPROM at address {2} on port {3} in {4} ms",
							spdDump.Length,
							(spdDump.Length > 1) ? "bytes" : "byte",
							reader.EepromAddress,
							reader.PortName,
							Environment.TickCount - startTick
							);

						if (filePath != "") {
							try {
								File.WriteAllBytes(filePath, spdDump);
							}
							catch {
								throw new Exception($"Unable to write to {filePath}");
							}
							Console.Write($" to file \"{filePath}\"");
						}

						reader.Disconnect();
						return;
					}

					// Write contents to EEPROM
					if (mode.StartsWith("/write")) {

						if (filePath.Length < 1) {
							throw new Exception("File path is mandatory for write mode.");
						}

						if (!File.Exists(filePath)) {
							throw new Exception($"File \"{filePath}\" not found.");
						}

						byte[] inputFile;
						try {
							inputFile = File.ReadAllBytes(filePath);
						}
						catch {
							throw new Exception($"Unable to read {filePath}");
						}

						Console.WriteLine(
							"Writing \"{0}\" ({1} {2}) to EEPROM at address {3}\n",
							filePath,
							inputFile.Length,
							(inputFile.Length > 1) ? "bytes" : "byte",
							reader.EepromAddress);

						if (inputFile.Length > (int)reader.SpdSize) {
							throw new Exception($"File \"{filePath}\" is larger than {reader.SpdSize} bytes.");
						}

						int bytesWritten = 0;
						int startTick = Environment.TickCount;
						byte b;

						for (int i = 0; i != inputFile.Length; i++) {
							b = inputFile[i];
							bool writeResult = mode == ("/writeforce")
								? Eeprom.WriteByte(reader, i, inputFile[i])
								: Eeprom.UpdateByte(reader, i, inputFile[i]);

							if (!writeResult) {
								throw new Exception($"Could not write byte {i} to EEPROM at address {reader.EepromAddress} on port {reader.PortName}.");
							}

							bytesWritten++;

							if (!silent) {
								ConsoleDisplayByte(i, b);
							}
						}
						reader.Disconnect();

						Console.WriteLine(
							"\n\nWritten {0} {1} to EEPROM at address {2} on port {3} in {4} ms",
							bytesWritten,
							(bytesWritten > 1) ? "bytes" : "byte",
							reader.EepromAddress,
							reader.PortName,
							Environment.TickCount - startTick);
						return;
					}
				}
			}
			catch (Exception e) {
				//Console.ForegroundColor = ConsoleColor.Red;
#if DEBUG
				Console.WriteLine($"{e}\n");
#else
				Console.WriteLine($"{e.Message}\n");
#endif
				
				//Console.ForegroundColor = ConsoleColor.Gray;
				return;
			}

			Console.WriteLine("Unknown command line parameters.\n");
			ShowHelp();
		}

		/// <summary>
		/// Prints bytes in a grid pattern
		/// </summary>
		/// <param name="pos">Byte offset</param>
		/// <param name="b">Byte value</param>
		/// <param name="bpr">Bytes per row</param>
		/// <param name="showOffset">Show or hide offsets on top and at the beginning of each line</param>
		/// <param name="color">Set to true to display colored output, or false to disable colors</param>
		static void ConsoleDisplayByte(int pos, byte b, int bpr = 16, bool showOffset = true, bool color = true) {

			ConsoleColor _defaultForeColor = Console.ForegroundColor;   // Text Color
			ConsoleColor _defaultBackColor = Console.BackgroundColor;   // Background Color

			// Colors sorted in rainbow order
			ConsoleColor[] colors = {
				ConsoleColor.DarkGray,
				ConsoleColor.Gray,
				ConsoleColor.Red,
				ConsoleColor.DarkRed,
				ConsoleColor.DarkYellow,
				ConsoleColor.Yellow,
				ConsoleColor.Green,
				ConsoleColor.DarkGreen,
				ConsoleColor.DarkCyan,
				ConsoleColor.Cyan,
				ConsoleColor.Blue,
				ConsoleColor.DarkBlue,
				ConsoleColor.DarkMagenta,
				ConsoleColor.Magenta,
				ConsoleColor.White,
				ConsoleColor.Gray,
			};

			// Print top row (offsets)
			if (pos == 0 && showOffset) {
				Console.Write("     "); // Indentation
				for (int i = 0; i < bpr; i++) {
					Console.Write($"{i:x2} ");
				}
			}

			// Print contents
			if (pos % bpr == 0) {
				Console.Write(Environment.NewLine);
				if (showOffset) {
					// Print row offsets
					Console.Write("{0:x3}: ", pos);
				}
			}

			// Set colors
			if (color) {
				// Print byte values on black background, so the output looks good in cmd and in powershell
				Console.BackgroundColor = ConsoleColor.Black;
				// Set color 
				Console.ForegroundColor = colors[b >> 4];
			}
			// Print byte value
			Console.Write($"{b:X2}");

			// Print blank space between each byte, but not at the end of the line
			if (pos % bpr != bpr - 1) {
				Console.Write(" ");
			}

			// Reset foreground (text) color
			Console.ForegroundColor = _defaultForeColor;
			// Reset background color
			Console.BackgroundColor = _defaultBackColor;
		}
	}
}
