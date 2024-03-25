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
using SpdReaderWriterCore;

namespace SpdReaderWriter {
    class Program {

        /// <summary>
        /// Default Arduino serial port settings
        /// </summary>
        public static Arduino.SerialPortParameters PortParameters = new Arduino.SerialPortParameters(
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
        public static Smbus smbus;

        /// <summary>
        /// Arduino instance
        /// </summary>
        public static Arduino arduino = new Arduino(PortParameters);

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

            ShowBanner();

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

        static void ShowBanner() {
            string[] header = {
                "   SPD-RW - EEPROM SPD reader and writer",
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "Version {0}",
                "(C) 2021-2023 A213M",
                ""
            };
            foreach (string line in header) {
                Console.WriteLine(line, Data.FileVersion(Global.ParentDirectory + "\\" + Global.ExeName));
            }
        }
        static void ShowHelp() {
            string[] help = {
                "",
                "Command line parameters:",
                "",
                "{0} /?",
                "{0} /find",
                "{0} /find <all|arduino <baudrate>|smbus>",
                "{0} /scan <PORTNAME<:baudrate>>",
                "{0} /scan <SMBUS#>",
                "{0} /read <PORTNAME<:baudrate>> <ADDRESS#> <filepath> /silent /nocolor",
                "{0} /read <SMBUS#> <ADDRESS#> <filepath> /silent /nocolor",
                "{0} /write <PORTNAME<:baudrate>> <ADDRESS#|ALL> <FILEPATH> /silent /nocolor",
                "{0} /writeforce <PORTNAME<:baudrate>> <ADDRESS#|ALL> <FILEPATH> /silent /nocolor",
                "{0} /firmware <FILEPATH>",
                "{0} /enablewriteprotection <PORTNAME<:baudrate>> <ADDRESS#|ALL> <block#>",
                "{0} /disablewriteprotection <PORTNAME<:baudrate>> <ADDRESS#|ALL>",
                "{0} /enablepermanentwriteprotection <PORTNAME<:baudrate>> <ADDRESS#|ALL>",
                "{0} /readpci <BUS#>:<DEVICE#>:<FUNCTION#> <size|256|1024> <filepath>",
                "{0} /readio <PORT#> <size> <filepath>",
                "{0} /readmem <ADDRESS#> <size> <filepath>",                
                "",
                "Parameters in CAPS are mandatory!",
                "All numbers must be specified in decimal format",
                "If baud rate is not specified, default value will be used - {1}",
                "Parameter <filepath> is optional when /read switch is used, output will be printed to console only.",
                "Switch /silent is optional, progress won't be shown with this switch.",
                "Switch /nocolor is optional, use to show data contents in monochrome",
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
                Console.WriteLine(line, AppDomain.CurrentDomain.FriendlyName, PortParameters.BaudRate);
            }
        }

        static void ParseCommand() {

            string mode = Args[0].ToLower();

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
                    case "/enablerswp":
                    case "/setrswp":
                    case "/rswp":
                        EnableRswp();
                        break;

                    case "/disablewriteprotection":
                    case "/clearwriteprotection":
                    case "/clearrswp":
                    case "/clearwrp":
                    case "/cwp":
                        ClearRswp();
                        break;

                    case "/read":
                    case "/readSpd":
                        ReadSpd();
                        break;

                    case "/write":
                    case "/writeSpd":
                    case "/writeforce":
                        WriteSpd();
                        break;

                    case "/enablepermanentwriteprotection":
                    case "/setpermanentwriteprotection":
                    case "/enablepswp":
                    case "/pswp":
                        EnablePswp();
                        break;

                    case "/firmware":
                    case "/savefirmware":
                        SaveFirmware();
                        break;

                    case "/readeeprom":
                        ReadEeprom();
                        break;

                    case "/readpci":
                        ReadPci();
                        break;

                    case "/readmemory":
                    case "/readmem":
                        ReadMemory();
                        break;

                    case "/readioport":
                    case "/readio":
                        ReadIoPort();
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
                if (arduino != null && arduino.IsConnected) {
                    arduino.Disconnect();
                }

                if (Global.IsAdmin()) {
                    Driver.Stop();
                    Driver.Dispose();
                }
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
            Resources.Firmware.FirmwareFile.SaveTo(destinationDir);
            Resources.Firmware.FirmwareSettingsFile.SaveTo(destinationDir);

            File.SetAttributes($"{destinationDir}\\{Resources.Firmware.FirmwareFile.Name}", FileAttributes.ReadOnly);

            Console.WriteLine($"Firmware files saved to {destinationDir}");
        }

        /// <summary>
        /// Enables RSWP on EEPROM
        /// </summary>
        private static void EnablePswp() {

            Connect();

            foreach (byte i2CAddress in Args[2].ToLower() == "all" ? arduino.Scan() : new[] { Data.StringToNum<byte>(Args[2]) }) {

                arduino.I2CAddress = i2CAddress;

                if (Eeprom.SetPswp(arduino)) {
                    Console.WriteLine($"Permanent write protection enabled on {arduino.PortName}:{arduino.I2CAddress}");
                }
                else {
                    throw new Exception($"Unable to set permanent write protection on {arduino.PortName}:{arduino.I2CAddress}");
                }
            }
        }

        /// <summary>
        /// Writes SPD data
        /// </summary>
        private static void WriteSpd() {

            string mode = Args[0].ToLower();
            
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

            foreach (byte i2CAddress in Args[2].ToLower() == "all" ? arduino.Scan() : new[] { Data.StringToNum<byte>(Args[2]) }) {

                arduino.I2CAddress = i2CAddress;

                Console.WriteLine(
                    "Writing \"{0}\" ({1} {2}) to EEPROM at address {3}\n",
                    FilePath,
                    inputFile.Length,
                    inputFile.Length > 1 ? "bytes" : "byte",
                    arduino.I2CAddress);

                if (inputFile.Length > arduino.MaxSpdSize) {
                    throw new Exception($"File \"{FilePath}\" is larger than {arduino.MaxSpdSize} bytes.");
                }

                int bytesWritten = 0;
                int startTick = Environment.TickCount;

                if (!Spd.ValidateSpd(inputFile)) {
                    throw new Exception($"Incorrect SPD file");
                }

                for (ushort i = 0; i < inputFile.Length; i++) {
                    byte b = inputFile[i];
                    bool writeResult = mode == "/writeforce"
                        ? Eeprom.Write(arduino, i, b)
                        : Eeprom.Update(arduino, i, b);

                    if (!writeResult) {
                        throw new Exception($"Could not write byte {i} to EEPROM at address {arduino.I2CAddress} on port {arduino.PortName}.");
                    }

                    bytesWritten++;
                }

                if (!Silent) {
                    ConsoleDisplayData(inputFile);
                }

                Console.WriteLine(
                    "\n\nWritten {0} {1} to EEPROM at address {2} on port {3} in {4} ms",
                    bytesWritten,
                    bytesWritten > 1 ? "bytes" : "byte",
                    arduino.I2CAddress,
                    arduino.PortName,
                    Environment.TickCount - startTick);
            }
        }

        /// <summary>
        /// Reads SPD data
        /// </summary>
        private static void ReadSpd() {

            byte i2CAddress = Data.StringToNum<byte>(Args[2]);
            byte[] spdDump = new byte[0];
            string name;

            Console.Write($"Reading EEPROM at address {i2CAddress}");

            if (FilePath.Length > 0) {
                Console.WriteLine($" to {FilePath}");
            }
            Console.WriteLine("\n");

            int startTick = Environment.TickCount;

            if (Args[1].ToUpper().StartsWith("COM")) {

                Connect();

                name = arduino.ToString();
                arduino.I2CAddress = i2CAddress;

                for (ushort i = 0; i < arduino.MaxSpdSize; i += 32) {
                    spdDump = Data.MergeArray(spdDump, Eeprom.Read(arduino, i, 32));
                }
            }
            else {
                if (!Global.IsAdmin()) {
                    throw new AccessViolationException("Administrative privileges required");
                }

                smbus = new Smbus(Data.StringToNum<byte>(Args[1]));

                if (smbus.ProbeAddress(i2CAddress)) {
                    smbus.I2CAddress = i2CAddress;
                }
                else {
                    throw new AccessViolationException($"Address {i2CAddress} not found on bus {smbus.BusNumber}");
                }

                name = $"{smbus} ({smbus.BusNumber})";

                for (ushort i = 0; i < smbus.MaxSpdSize; i += 32) {
                    spdDump = Data.MergeArray(spdDump, Eeprom.Read(smbus, i, 32));
                }
            }

            int endTick = Environment.TickCount;

            if (!Silent) {
                ConsoleDisplayData(spdDump);
            }

            Console.Write("\n\nRead {0} {1} from EEPROM at address {2} on {3} in {4} ms",
                spdDump.Length,
                spdDump.Length > 1 ? "bytes" : "byte",
                i2CAddress,
                name,
                endTick - startTick);

            if (FilePath.Length > 0) {
                try {
                    TrySaveOutput(spdDump);
                }
                catch {
                    throw new Exception($"Unable to write to {FilePath}");
                }

                Console.Write($" to file \"{FilePath}\"");
            }
        }

        /// <summary>
        /// Reads data from Arduino's internal EEPROM
        /// </summary>
        private static void ReadEeprom() {

            ushort offset = Data.StringToNum<ushort>(Args[2]);
            ushort length = Data.StringToNum<ushort>(Args[3]);

            Connect();

            byte[] data = arduino.ReadEeprom(offset, length);

            ConsoleDisplayData(data);
        }

        private static void ReadPci() {

            if (!Global.IsAdmin()) {
                throw new AccessViolationException("Administrative privileges required");
            }
            Driver.Start();

            string[] pciAddress = Args[1].Split(':');

            byte pciBus = Data.StringToNum<byte>(pciAddress[0]);
            byte pciDev = Data.StringToNum<byte>(pciAddress[1]);
            byte pciFunc = Data.StringToNum<byte>(pciAddress[2]);

            ushort length = (ushort)(Args.Length > 2 ? Data.StringToNum<ushort>(Args[2]) : 256);

            Console.WriteLine($"Reading PCI config space at {Args[1]}\n");

            byte[] data = new byte[length];

            for (ushort i = 0; i < data.Length; i++) {
                data[i] = Kernel.ReadPciConfig<byte>(pciBus, pciDev, pciFunc, i);
            }

            Driver.Stop();

            if (!Silent) {
                ConsoleDisplayData(data);
            }

            TrySaveOutput(data);
        }

        /// <summary>
        /// Reads data from I/O port space
        /// </summary>
        private static void ReadIoPort() {
            if (!Global.IsAdmin()) {
                throw new AccessViolationException("Administrative privileges required");
            }
            Driver.Start();

            ushort address = Data.StringToNum<ushort>(Args[1]);
            ushort length = (ushort)(Args.Length > 2 ? Data.StringToNum<ushort>(Args[2]) : 256);

            Console.WriteLine($"Reading IO port at address {Args[1]}\n");

            byte[] data = new byte[length];

            for (int i = 0; i < data.Length; i++) {
                data[i] = Kernel.ReadIoPort<byte>((ushort)(address + i));
            }

            Driver.Stop();

            if (!Silent) {
                ConsoleDisplayData(data);
            }

            TrySaveOutput(data);
        }

        /// <summary>
        /// Reads data from system memory
        /// </summary>
        private static void ReadMemory() {

            if (!Global.IsAdmin()) {
                throw new AccessViolationException("Administrative privileges required");
            }

            Driver.Start();

            uint address = Data.StringToNum<uint>(Args[1]);
            ushort length = (ushort)(Args.Length > 2 ? Data.StringToNum<ushort>(Args[2]) : 256);

            Console.WriteLine($"Reading memory at address {Args[1]}\n");

            byte[] data = new byte[length];

            for (int i = 0; i < data.Length; i++) {
                data[i] = Kernel.ReadMemory<byte>((uint)(address + i));
            }

            Driver.Stop();

            if (!Silent) {
                ConsoleDisplayData(data);
            }

            TrySaveOutput(data);
        }

        /// <summary>
        /// Clears RSWP on EEPROM
        /// </summary>
        private static void ClearRswp() {

            Connect();

            byte[] i2CAddresses;

            if (Args.Length > 2) {
                i2CAddresses = Args[2].ToLower() == "all" ? arduino.Scan() : new[] { Data.StringToNum<byte>(Args[2]) };
            }
            else {
                i2CAddresses = arduino.Scan();
            }

            foreach (byte i2CAddress in i2CAddresses) {

                arduino.I2CAddress = i2CAddress;

                if (Eeprom.ClearRswp(arduino)) {
                    Console.WriteLine("Write protection successfully disabled.");
                }
                else {
                    throw new Exception("Unable to clear write protection");
                }
            }
        }

        /// <summary>
        /// Sets RSWP on EEPROM
        /// </summary>
        private static void EnableRswp() {
            int[] blocks;

            Connect();

            foreach (byte i2CAddress in Args[2].ToLower() == "all"
                         ? arduino.Scan()
                         : new[] { Data.StringToNum<byte>(Args[2]) }) {

                arduino.I2CAddress = i2CAddress;

                Spd.RamType ramType = Spd.GetRamType(arduino);

                if (Args.Length == 4) { // Block # was specified

                    blocks = new[] { Data.StringToNum<int>(Args[3]) };

                    if (blocks[0] > 15 || blocks[0] < 0 ||
                        (blocks[0] > 3 && ramType == Spd.RamType.DDR4) ||
                        (blocks[0] > 0 && ramType != Spd.RamType.DDR4 && ramType != Spd.RamType.DDR5)) {
                        throw new ArgumentOutOfRangeException("Incorrect block number specified");
                    }
                }
                else { // No block number specified, protect all available

                    int totalBlocks = 1; // DDR3 + DDR2

                    switch (ramType) {
                        case Spd.RamType.DDR5:
                            totalBlocks = 16;
                            break;
                        case Spd.RamType.DDR4:
                            totalBlocks = 4;
                            break;
                    }

                    blocks = Data.ConsecutiveArray<int>(0, totalBlocks - 1);
                }

                foreach (byte b in blocks) {
                    Console.WriteLine(Eeprom.SetRswp(arduino, b)
                        ? $"Block {b} is now read-only"
                        : $"Unable to set write protection for block {b}. Either SA0 is not connected to HV, or the block is already read-only.");
                }
            }
        }

        /// <summary>
        /// Scans Arduino or SMBus for I2C addresses
        /// </summary>
        private static void ScanDevice() {
            if (Args.Length == 2) {

                byte[] addresses;
                string name;

                if (Args[1].ToUpper().StartsWith("COM")) {

                    Connect();

                    addresses = arduino.Scan();
                    name = $"port {arduino.PortName}";
                }
                else {
                    if (!Global.IsAdmin()) {
                        throw new AccessViolationException("Administrative privileges required");
                    }

                    smbus = new Smbus();

                    int i = Data.StringToNum<int>(Args[1]);

                    if (i > smbus.FindBus().Length - 1) {
                        throw new ArgumentOutOfRangeException($"SMBus number {i} not available");
                    }

                    smbus.BusNumber = (byte)i;
                    addresses = smbus.Scan();
                    name = $"SMBus {smbus.BusNumber}";

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
            string portName = Args[1].ToUpper();

            if (!portName.StartsWith("COM", StringComparison.CurrentCulture)) {
                throw new ArgumentException("Port name should start with \"COM\" followed by a number.");
            }

            // Get baud rate
            if (portName.Contains(":")) {
                string[] portParams = portName.Split(':');
                if (portParams.Length == 2) {
                    portName = portParams[0].Trim();
                    PortParameters.BaudRate = Data.StringToNum<int>(portParams[1]);
                }
                else {
                    throw new ArgumentException("Incorrect use of port arguments");
                }
            }

            arduino = new Arduino(portName, PortParameters);

            // Establish connection
            if (!arduino.Connect()) {
                throw new Exception($"Could not connect to the device on port {portName}.");
            }

            // Check FW version
            if (arduino.FirmwareVersion < Arduino.RequiredFirmwareVersion) {
                throw new Exception($"The device on port {portName} requires its firmware to be updated.");
            }
        }

        /// <summary>
        /// Looks for Arduino devices or available SMBuses
        /// </summary>
        private static void FindDevice() {

            if (Args.Length == 1 || (Args.Length >= 2 && Args[1].ToLower() == "all")) {
                FindArduino();
                if (Global.IsAdmin()) {
                    FindSmbus();
                }
            }
            else if (Args.Length >= 2) {
                switch (Args[1].ToLower()) {
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

            if (Args.Length >= 3) {
                PortParameters.BaudRate = Data.StringToNum<int>(Args[2]);
            }

            if (PortParameters.BaudRate == 0) {
                throw new ArgumentException("Incorrect baud rate parameter");
            }

            Arduino[] devices = Arduino.Find(PortParameters);
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

            if (!Global.IsAdmin()) {
                throw new AccessViolationException("Administrative privileges required");
            }

            smbus = new Smbus();

            try {
                foreach (byte bus in smbus.SmBuses) {
                    Console.WriteLine($"Found SMBus # {bus} ({smbus})");
                }
            }
            catch {
                Console.WriteLine("No SMBus found");
            }
        }

        /// <summary>
        /// Saves input data to a file if path is provided
        /// </summary>
        /// <param name="data">Input data</param>
        static bool TrySaveOutput(byte[] data) {
            if (FilePath != "") {
                try {
                    File.WriteAllBytes(FilePath, data);
                    return true;
                }
                catch {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Prints byte array in a grid pattern
        /// </summary>
        /// <param name="input">Input byte array</param>
        static void ConsoleDisplayData(byte[] input) => 
            ConsoleDisplayData(input, ShowColor);

        /// <summary>
        /// Prints byte array in a grid pattern
        /// </summary>
        /// <param name="input">Input byte array</param>
        /// <param name="color">Set to <see langword="true"/> to display colored output, or <see langword="false"/> to disable colors</param>
        static void ConsoleDisplayData(byte[] input, bool color) {
            for (int i = 0; i < input.Length; i++) {
                ConsoleDisplayByte(i, input[i], 16, true, color);
            }
        }

        /// <summary>
        /// Prints bytes in a grid pattern
        /// </summary>
        /// <param name="pos">Byte offset</param>
        /// <param name="b">Byte value</param>
        /// <param name="bpr">Bytes per row</param>
        /// <param name="showOffset">Show or hide offsets on top and at the beginning of each line</param>
        /// <param name="color">Set to <see langword="true"/> to display colored output, or <see langword="false"/> to disable colors</param>
        static void ConsoleDisplayByte(int pos, byte b, int bpr = 16, bool showOffset = true, bool color = true) {

            // Colors sorted in rainbow order
            ConsoleColor[] consoleColors = {
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
                // Print byte values on black background, so the output looks readable in cmd and in powershell
                Console.BackgroundColor = ConsoleColor.Black;
                // Set color 
                Console.ForegroundColor = consoleColors[b >> 4];
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
    }
}