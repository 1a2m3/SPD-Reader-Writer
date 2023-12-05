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
using System.Security.Principal;
using SpdReaderWriterCore;
using SpdReaderWriterCore.Properties;

namespace SpdReaderWriter {
    class Program {

        /// <summary>
        /// Default Arduino serial port settings
        /// </summary>
        public static Arduino.SerialPortSettings ReaderSettings = new Arduino.SerialPortSettings(
            // Baud rate
            baudRate: 115200,
            // Enable DTR
            dtrEnable: true,
            // Enable RTS
            rtsEnable: true,
            // Response timeout (sec.)
            timeout: 10);

        /// <summary>
        /// SMBus instance
        /// </summary>
        public static Smbus Smbus;

        /// <summary>
        /// Arduino instance
        /// </summary>
        public static Arduino Arduino = new Arduino(ReaderSettings);

        /// <summary>
        /// Command line arguments
        /// </summary>
        public static string[] Args;

        /// <summary>
        /// Display SPD contents in color
        /// </summary>
        public static bool ShowColor;

        /// <summary>
        /// Silent state
        /// </summary>
        public static bool Silent;

        /// <summary>
        /// Input or output file path
        /// </summary>
        public static string FilePath = "";

        static void Main(string[] args) {

            Args = args;

            Silent = Data.ArrayContains(Args, "/silent");
            ShowColor = !Data.ArrayContains(Args, "/nocolor");
            FilePath = Args.Length >= 4 && !Args[3].Contains("/") ? Args[3] : "";

            if (IsAdmin()) {
                Smbus = new Smbus();
            }

            Welcome();

            if (args.Length > 0) {
#if DEBUG
                // Display command line arguments in console title
                if (Debugger.IsAttached) {
                    Console.Title = $"{AppDomain.CurrentDomain.FriendlyName} ";
                    foreach (string cmd in args) {
                        Console.Title += $"{cmd} ";
                    }
                }
#endif
                ParseCommand();
            }
            else {
                ShowHelp();
            }

            // Wait for input to prevent application from closing automatically
            if (!Debugger.IsAttached && args.Length != 0) return;
            Console.WriteLine("\nPress [enter] to quit.\n");
            Console.ReadLine();
        }

        static void Welcome() {
            string[] header = {
                "   SPD-RW - EEPROM SPD reader and writer",
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "Version {0}",
                "(C) 2021-2023 A213M",
                ""
            };
            foreach (string line in header) {
                Console.WriteLine(line, FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).ProductVersion);
            }
        }
        static void ShowHelp() {
            string[] help = {
                "",
                "Command line parameters:",
                "",
                "{0} /?",
                "{0} /find",
                "{0} /find <all|arduino|smbus>",
                "{0} /scan <PORTNAME>",
                "{0} /scan <SMBUS#>",
                "{0} /read <PORTNAME> <ADDRESS#> <filepath> /silent /nocolor",
                "{0} /read <SMBUS#> <ADDRESS#> <filepath> /silent /nocolor",
                "{0} /write <PORTNAME> <ADDRESS#> <FILEPATH> /silent /nocolor",
                "{0} /writeforce <PORTNAME> <ADDRESS#> <FILEPATH> /silent /nocolor",
                "{0} /firmware <FILEPATH>",
                "{0} /enablewriteprotection <PORTNAME> <ADDRESS#>",
                "{0} /enablewriteprotection <PORTNAME> <ADDRESS#> <block#>",
                "{0} /disablewriteprotection <PORTNAME> <ADDRESS#>",
                "{0} /enablepermanentwriteprotection <PORTNAME> <ADDRESS#>",
                "",
                "Parameters in CAPS are mandatory!",
                "All numbers must be specified in decimal format",
                "Parameter <filepath> is optional when /read switch is used, output will be printed to console only.",
                "Switch /silent is optional, progress won't be shown with this switch.",
                "Switch /nocolor is optional, use to show SPD contents in monochrome",
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

        static void ParseCommand() {

            string mode = Args[0];

            try {
                switch (mode) {
                    case "/?":
                    case "/help":
                        ShowHelp();
                        break;

                    case "/find":
                        FindDevice();
                        break;

                    case "/scan":
                        ScanDevice();
                        break;

                    case "/enablewriteprotection":
                    case "/setwriteprotection":
                        EnableRswp();
                        break;

                    case "/disablewriteprotection":
                    case "/clearwriteprotection":
                        DisableRswp();
                        break;

                    case "/read":
                        ReadEeprom();
                        break;

                    case "/write":
                        WriteEeprom();
                        break;

                    case "/enablepermanentwriteprotection":
                    case "/setpermanentwriteprotection":
                        EnablePswp();
                        break;

                    case "/firmware":
                    case "/savefirmware":
                        SaveFirmware();
                        break;
                    default:
                        Console.WriteLine("Unknown command line parameters.\n");
                        ShowHelp();
                        break;
                }
            }
            catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
#if DEBUG
                Console.WriteLine($"{e}\n");
#else
                Console.WriteLine($"{e.Message}\n");
#endif
                Console.ResetColor();
            }
            finally {
                Arduino.Disconnect();
            }
        }

        /// <summary>
        /// Saves firmware files to a directory
        /// </summary>
        private static void SaveFirmware() {

            if (Args.Length < 2 || Args[1].Length < 1) {
                throw new ArgumentException($"No destination path specified");
            }

            string destinationDir = Args[1] + "\\SpdReaderWriter";

            Directory.CreateDirectory(destinationDir);
            File.WriteAllText(
                path     : destinationDir + "\\SpdReaderWriter.ino",
                contents : Data.BytesToString(Data.Gzip(Resources.Firmware.SpdReaderWriter_ino, Data.GzipMethod.Decompress)));

            File.WriteAllText(
                path     : destinationDir + "\\SpdReaderWriterSettings.h",
                contents : Data.BytesToString(Data.Gzip(Resources.Firmware.SpdReaderWriterSettings_h, Data.GzipMethod.Decompress)));

            File.SetAttributes(destinationDir + "\\SpdReaderWriter.ino", FileAttributes.ReadOnly);

            Console.WriteLine($"Firmware files saved to {destinationDir}");
        }

        /// <summary>
        /// Enables RSWP on EEPROM
        /// </summary>
        private static void EnablePswp() {

            Connect();

            if (Eeprom.SetPswp(Arduino)) {
                Console.WriteLine($"Permanent write protection enabled on {Arduino.PortName}:{Arduino.I2CAddress}");
            }
            else {
                throw new Exception($"Unable to set permanent write protection on {Arduino.PortName}:{Arduino.I2CAddress}");
            }
        }

        /// <summary>
        /// Writes data to EEPROM
        /// </summary>
        private static void WriteEeprom() {

            string mode = Args[0];
            byte i2CAddress = (byte)int.Parse(Args[2]);

            if (FilePath.Length < 1) {
                throw new ArgumentException("File path is mandatory for write mode.");
            }

            if (!File.Exists(FilePath)) {
                throw new FileNotFoundException($"File \"{FilePath}\" not found.");
            }

            byte[] inputFile;
            try {
                inputFile = File.ReadAllBytes(FilePath);
            }
            catch {
                throw new FileLoadException($"Unable to read {FilePath}");
            }

            Connect();

            Arduino.I2CAddress = i2CAddress;

            Console.WriteLine(
                "Writing \"{0}\" ({1} {2}) to EEPROM at address {3}\n",
                FilePath,
                inputFile.Length,
                inputFile.Length > 1 ? "bytes" : "byte",
                Arduino.I2CAddress);

            if (inputFile.Length > Arduino.MaxSpdSize) {
                throw new Exception($"File \"{FilePath}\" is larger than {Arduino.MaxSpdSize} bytes.");
            }

            int bytesWritten = 0;
            int startTick = Environment.TickCount;

            if (!Spd.ValidateSpd(inputFile)) {
                throw new Exception($"Incorrect SPD file");
            }

            for (ushort i = 0; i < inputFile.Length; i++) {
                byte b = inputFile[i];
                bool writeResult = mode == "/writeforce"
                    ? Eeprom.Write(Arduino, i, b)
                    : Eeprom.Update(Arduino, i, b);

                if (!writeResult) {
                    throw new Exception($"Could not write byte {i} to EEPROM at address {Arduino.I2CAddress} on port {Arduino.PortName}.");
                }

                bytesWritten++;

                if (!Silent) {
                    ConsoleDisplayByte(i, b, 16, true, ShowColor);
                }
            }

            Arduino.Disconnect();

            Console.WriteLine(
                "\n\nWritten {0} {1} to EEPROM at address {2} on port {3} in {4} ms",
                bytesWritten,
                bytesWritten > 1 ? "bytes" : "byte",
                Arduino.I2CAddress,
                Arduino.PortName,
                Environment.TickCount - startTick);
        }

        /// <summary>
        /// Reads data from EEPROM
        /// </summary>
        private static void ReadEeprom() {

            byte i2CAddress = (byte)int.Parse(Args[2]);
            byte[] spdDump = new byte[0];
            string name;

            Console.Write($"Reading EEPROM at address {i2CAddress}");

            if (FilePath.Length > 0) {
                Console.WriteLine($" to {FilePath}");
            }
            Console.WriteLine("\n");

            int startTick = Environment.TickCount;

            if (Args[1].StartsWith("COM")) {

                Connect();

                name = Arduino.ToString();
                Arduino.I2CAddress = i2CAddress;

                for (ushort i = 0; i < Arduino.MaxSpdSize; i += 32) {
                    spdDump = Data.MergeArray(spdDump, Eeprom.Read(Arduino, i, 32));
                }

                Arduino.Disconnect();
            }
            else {
                if (!IsAdmin()) {
                    throw new AccessViolationException("Administrative privileges required");
                }

                Smbus.BusNumber = (byte)int.Parse(Args[1]);
                Smbus.I2CAddress = i2CAddress;
                name = $"{Smbus} ({Smbus.BusNumber})";

                for (ushort i = 0; i < Smbus.MaxSpdSize; i += 32) {
                    spdDump = Data.MergeArray(spdDump, Eeprom.Read(Smbus, i, 32));
                }
            }

            int endTick = Environment.TickCount;

            if (!Silent) {
                for (int i = 0; i < spdDump.Length; i++) {
                    ConsoleDisplayByte(i, spdDump[i], 16, true, ShowColor);
                }
            }

            Console.Write("\n\nRead {0} {1} from EEPROM at address {2} on {3} in {4} ms",
                spdDump.Length,
                spdDump.Length > 1 ? "bytes" : "byte",
                i2CAddress,
                name,
                endTick - startTick);

            if (FilePath.Length > 0) {
                try {
                    File.WriteAllBytes(FilePath, spdDump);
                }
                catch {
                    throw new Exception($"Unable to write to {FilePath}");
                }
                Console.Write($" to file \"{FilePath}\"");
            }
        }

        /// <summary>
        /// Clears RSWP on EEPROM
        /// </summary>
        private static void DisableRswp() {

            Connect();

            if (Eeprom.ClearRswp(Arduino)) {
                Console.WriteLine("Write protection successfully disabled.");
            }
            else {
                throw new Exception("Unable to clear write protection");
            }
        }

        /// <summary>
        /// Sets RSWP on EEPROM
        /// </summary>
        private static void EnableRswp() {
            int[] block;
            byte i2CAddress = (byte)int.Parse(Args[2]);

            Connect();
            Arduino.I2CAddress = i2CAddress;

            Spd.RamType ramType = Spd.GetRamType(Arduino);

            if (Args.Length == 4) { // Block # was specified
                try {
                    block = new[] { int.Parse(Args[3]) };
                }
                catch {
                    throw new ArgumentException("Block number should be specified in decimal notation.");
                }

                if (block[0] > 15 || block[0] < 0 ||
                    (block[0] > 3 && ramType == Spd.RamType.DDR4) ||
                    (block[0] > 0 && ramType != Spd.RamType.DDR4 && ramType != Spd.RamType.DDR5)) {
                    throw new ArgumentOutOfRangeException("Incorrect block number specified");
                }

            }
            else { // No block number specified, protect all available

                int totalBlocks;

                if (ramType == Spd.RamType.DDR5) {
                    totalBlocks = 16;
                }
                else if (ramType == Spd.RamType.DDR4) {
                    totalBlocks = 4;
                }
                else { // DDR3 + DDR2
                    totalBlocks = 1;
                }

                block = Data.ConsecutiveArray<int>(0, totalBlocks - 1, 1);
            }

            for (byte i = 0; i < block.Length; i++) {
                Console.WriteLine(Eeprom.SetRswp(Arduino, i)
                    ? $"Block {i} is now read-only"
                    : $"Unable to set write protection for block {i}. Either SA0 is not connected to HV, or the block is already read-only.");
            }

            Arduino.Disconnect();
        }

        /// <summary>
        /// Scans Arduino or SMBus for I2C addresses
        /// </summary>
        private static void ScanDevice() {
            if (Args.Length == 2) {

                byte[] addresses;
                string name;

                if (Args[1].StartsWith("COM")) {

                    Connect();

                    addresses = Arduino.Scan();
                    name = $"port {Arduino.PortName}";

                    Arduino.Disconnect();
                }
                else {
                    if (!IsAdmin()) {
                        throw new AccessViolationException("Administrative privileges required");
                    }

                    int i = -1;
                    if (int.TryParse(Args[1], out i) && i != -1) {
                        if (i > Smbus.FindBus().Length - 1) {
                            throw new ArgumentOutOfRangeException("SMBus number not available");
                        }
                        Smbus.BusNumber = (byte)i;
                        addresses = Smbus.Scan();
                        name = $"SMBus {Smbus.BusNumber}";
                    }
                    else {
                        throw new Exception("SMBus number should be specified in decimal notation.");
                    }
                }

                if (addresses.Length == 0) {
                    throw new Exception("No EEPROM devices found.");
                }

                foreach (int location in addresses) {
                    Console.WriteLine($"Found EEPROM on {name} at address: {location}");
                }
            }
            else {
                throw new ArgumentException($"Incorrect use of arguments");
            }
        }

        /// <summary>
        /// Establishes a connection with Arduino
        /// </summary>
        private static void Connect() {
            // Init
            string portName = Args[1];

            if (!portName.StartsWith("COM", StringComparison.CurrentCulture)) {
                throw new ArgumentException("Port name should start with \"COM\" followed by a number.");
            }

            Arduino = new Arduino(ReaderSettings, portName);

            // Establish connection
            if (!Arduino.Connect()) {
                throw new Exception($"Could not connect to the device on port {portName}.");
            }

            // Check FW version
            if (Arduino.FirmwareVersion < Arduino.RequiredFirmwareVersion) {
                throw new Exception($"The device on port {portName} requires its firmware to be updated.");
            }

            if (!Arduino.Test()) {
                throw new Exception($"The device on port {portName} does not respond.");
            }
        }

        /// <summary>
        /// Looks for Arduino devices or available SMBuses
        /// </summary>
        private static void FindDevice() {

            if (Args.Length == 1 || Args.Length == 2 && Args[1] == "all") {
                FindArduino();
                if (IsAdmin()) {
                    FindSmbus();
                }
            }
            else if (Args.Length == 2) {
                switch (Args[1]) {
                    case "arduino":
                        FindArduino();
                        break;
                    case "smbus":
                        FindSmbus();
                        break;
                }
            }
        }

        /// <summary>
        /// Looks for Arduino devices
        /// </summary>
        private static void FindArduino() {
            Arduino[] devices = Arduino.Find(ReaderSettings);
            if (devices.Length > 0) {
                foreach (Arduino arduinoPortName in devices) {
                    Console.WriteLine($"Found Arduino on Serial Port: {arduinoPortName}\n");
                }
            }
            else {
                Console.WriteLine("No Arduinos found");
            }
        }

        /// <summary>
        /// Looks for available SMBuses
        /// </summary>
        private static void FindSmbus() {

            if (!IsAdmin()) {
                throw new AccessViolationException("Administrative privileges required");
            }

            try {
                foreach (byte bus in Smbus.FindBus()) {
                    Console.WriteLine($"Found SMBus # {bus} ({Smbus})");
                }
            }
            catch {
                Console.WriteLine("No SMBus found");
            }
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

            // Colors sorted in rainbow order
            ConsoleColor[] colors = {
                ConsoleColor.DarkGray,
                ConsoleColor.Gray,
                ConsoleColor.DarkRed,
                ConsoleColor.Red,
                ConsoleColor.Yellow,
                ConsoleColor.DarkYellow,
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

            // Reset colors
            Console.ResetColor();
        }

        /// <summary>
        /// Detects if administrative privileges are present
        /// </summary>
        /// <returns><see langword="true"/> if administrative privileges are present</returns>
        private static bool IsAdmin() {

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