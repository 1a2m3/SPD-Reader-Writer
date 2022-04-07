using System;
using System.Diagnostics;
using System.IO;
using SpdReaderWriterDll;
using SerialPortSettings = SpdReaderWriterDll.SerialDevice.SerialPortSettings;

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

            // Wait for input to prevent application from closing automatically
            if (Debugger.IsAttached || args.Length == 0) {
                Console.WriteLine("\nPress [enter] to quit.\n");
                Console.ReadLine();
            }
        }

        static void Welcome() {
            string[] header = {
                " Arduino based EEPROM SPD reader and writer ",
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "For overclockers and PC hardware enthusiasts",
                ""
            };
            foreach (string line in header) {
                Console.WriteLine(line);
            }
        }
        static void ShowHelp() {
            string[] help = {
                "",
                "Command line parameters:",
                "",
                "{0} /?",
                "{0} /find",
                "{0} /scan <PORT>",
                "{0} /read <PORT> <ADDRESS#> <filepath> /silent",
                "{0} /write <PORT> <ADDRESS#> <FILEPATH> /silent",
                "{0} /writeforce <PORT> <ADDRESS#> <FILEPATH> /silent",
                "{0} /enablewriteprotection <PORT>",
                "{0} /enablewriteprotection <PORT> <block#>",
                "{0} /disablewriteprotection <PORT>",
                "{0} /enablepermanentwriteprotection <PORT> <ADDRESS#>",
                "",
                "Parameters in CAPS are mandatory!",
                "Parameter <filepath> is optional when /read switch is used, output will be printed to console only.",
                "Switch /silent is optional, progress won't be shown with this switch.",
                "",
                "For additional help, visit: https://github.com/1a2m3/SPD-Reader-Writer",
                "                         or https://forums.evga.com/FindPost/3053544",
                "",
                "This program is free to use, but if you like it and wish to support me,",
                "I am accepting donations via systems listed below:",
                "",
                "Paypal:  https://paypal.me/mik4rt3m",
                "Bitcoin: 3Pe9VhVaUygyMFGT3pFuQ3dAghS36NPJTz",
                ""
            };

            foreach (string line in help) {
                Console.WriteLine(line, AppDomain.CurrentDomain.FriendlyName);
            }
        }

        static void ParseCommand(string[] args) {

            string mode = args[0];

            if (mode == "/?" || mode == "/help") {
                ShowHelp();
                return;
            }

            try {
                // Setup
                SerialPortSettings readerSettings = new SerialPortSettings(
                        // Baud rate
                        baudRate: 115200,
                        // Enable DTR
                        dtrEnable: true,
                        // Enable RTS
                        rtsEnable: true,
                        // Response timeout (sec.)
                        responseTimeout: 10);

                // Find
                if (mode == "/find") {
                    // Find 
                    string[] devices = new SerialDevice(readerSettings).Find();
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

                    SerialDevice reader = new SerialDevice(readerSettings, portName);

                    if (!reader.Connect()) {
                        throw new Exception($"Could not connect to the device on port {portName}.");
                    }

                    //if (reader.GetFirmwareVersion() < SpdReaderWriterDll.Settings.MINVERSION) {
                    //    throw new Exception($"The device on port {portName} requires its firmware to be updated.");
                    //}

                    //if (!reader.Test()) {
                    //	throw new Exception($"The device on port {portName} does not respond.");
                    //}

                    // Scan
                    if (mode == "/scan") {
                        byte[] addresses = reader.Scan();

                        if (addresses.Length == 0) {
                            throw new Exception("No EEPROM devices found.");
                        }

                        foreach (int location in addresses) {
                            Console.WriteLine($"Found EEPROM at address: {location}");
                        }

                        reader.Disconnect();
                        return;
                    }

                    // Test reversible write protection capabilities
                    if (mode.StartsWith("/") && mode.EndsWith("writeprotection")) {

                        if (reader.GetRamTypeSupport() < Arduino.Response.RswpSupport.DDR3) {
                            throw new Exception("Your device does not support write protection features.");
                        }

                        // Turn on write protection
                        if (mode.StartsWith("/enable") || mode.StartsWith("/set")) {

                            int[] block;

                            if (args.Length == 3) { // Block # was specified
                                try {
                                    block = new[] { Int32.Parse(args[2]) };
                                }
                                catch {
                                    throw new Exception("Block number should be specified in decimal notation.");
                                }

                            }
                            else { // No block number specified, protect all available
                                if (Spd.GetRamType(reader) == Ram.Type.DDR4) {
                                    block = new[] { 0, 1, 2, 3 };
                                }
                                else { // DDR3 + DDR2
                                    block = new[] { 0 };
                                }
                            }

                            reader.ResetAddressPins();

                            for (byte i = 0; i < block.Length; i++) {
                                if (Eeprom.SetRswp(reader, i)) {
                                    Console.WriteLine($"Block {i} is now read-only");
                                }
                                else {
                                    throw new Exception($"Unable to set write protection for block {i}. Either SA0 is not connected to HV, or the block is already read-only.");
                                }
                            }

                            return;
                        }

                        // Disable write protection
                        if (mode.StartsWith("/disable") || mode.StartsWith("/clear")) {

                            reader.PIN_SA1 = SerialDevice.Pin.State.ON;

                            if (Eeprom.ClearRswp(reader)) {
                                Console.WriteLine("Write protection successfully disabled.");
                            }
                            else {
                                throw new Exception("Unable to clear write protection");
                            }

                            reader.ResetAddressPins();

                            return;
                        }
                    }

                    byte address;

                    try {
                        address = (byte)Int32.Parse(args[2]);
                    }
                    catch {
                        throw new Exception("EEPROM address should be specified in decimal notation.");
                    }

                    reader.I2CAddress = address;
                    reader.SpdSize = Spd.GetSpdSize(reader);

                    if (!reader.ProbeAddress()) {
                        throw new Exception($"EEPROM is not present at address {reader.I2CAddress}.");
                    }

                    string filePath = (args.Length >= 4) ? args[3] : "";
                    bool silent = (args.Length >= 5 && args[4] == "/silent") ? true : false;

                    // Read SPD contents
                    if (mode == "/read") {

                        Console.Write($"Reading EEPROM at address {reader.I2CAddress} ({Spd.GetRamType(reader)})");

                        if (filePath != "") {
                            Console.WriteLine($" to {filePath}");
                        }
                        Console.WriteLine("\n");

                        int startTick = Environment.TickCount;

                        byte[] spdDump = new byte[(int)reader.SpdSize];

                        for (ushort i = 0; i < (int)reader.SpdSize; i++) {
                            spdDump[i] = Eeprom.ReadByte(reader, i);
                        }

                        for (int i = 0; i < spdDump.Length; i++) {
                            if (!silent) {
                                ConsoleDisplayByte(i, spdDump[i]);
                            }
                        }

                        Console.Write("\n\nRead {0} {1} from EEPROM at address {2} on port {3} in {4} ms",
                            spdDump.Length,
                            (spdDump.Length > 1) ? "bytes" : "byte",
                            reader.I2CAddress,
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
                            reader.I2CAddress);

                        if (inputFile.Length > (int)reader.SpdSize) {
                            throw new Exception($"File \"{filePath}\" is larger than {reader.SpdSize} bytes.");
                        }

                        int bytesWritten = 0;
                        int startTick = Environment.TickCount;
                        byte b;

                        for (int i = 0; i != inputFile.Length; i++) {
                            b = inputFile[i];
                            bool writeResult = (mode == "/writeforce")
                                ? Eeprom.WriteByte(reader, (ushort)i, inputFile[i])
                                : Eeprom.UpdateByte(reader, (ushort)i, inputFile[i]);

                            if (!writeResult) {
                                throw new Exception($"Could not write byte {i} to EEPROM at address {reader.I2CAddress} on port {reader.PortName}.");
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
                            reader.I2CAddress,
                            reader.PortName,
                            Environment.TickCount - startTick);
                        return;
                    }

                    if (mode == "/enablepermanentwriteprotection") {
                        if (Eeprom.SetPswp(reader)) {
                            Console.WriteLine($"Permanent write protection enabled on {reader.PortName}:{reader.I2CAddress}");
                        }
                        else {
                            throw new Exception($"Unable to set permanent write protection on {reader.PortName}:{reader.I2CAddress}");
                        }
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
                Console.Write("      "); // Indentation
                for (int i = 0; i < bpr; i++) {
                    Console.Write($"{i:X2} ");
                }
            }

            // Print contents
            if (pos % bpr == 0) {
                Console.Write(Environment.NewLine);
                if (showOffset) {
                    // Print row offsets
                    Console.Write("{0:X4}: ", pos);
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
