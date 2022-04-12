using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using SpdReaderWriterDll;
using SpdReaderWriterDll.Properties;
using SerialPortSettings = SpdReaderWriterDll.Arduino.SerialPortSettings;

namespace SpdReaderWriter {
    class Program {

        /// <summary>
        /// Default Arduino serial port settings
        /// </summary>
        public static SerialPortSettings ReaderSettings = new SerialPortSettings(
            // Baud rate
            baudRate: 115200,
            // Enable DTR
            dtrEnable: true,
            // Enable RTS
            rtsEnable: true,
            // Response timeout (sec.)
            responseTimeout: 10);

        /// <summary>
        /// SMBus instance
        /// </summary>
        public static Smbus Smbus;

        /// <summary>
        /// Arduino instance
        /// </summary>
        public static Arduino Reader = new Arduino(ReaderSettings);

        /// <summary>
        /// Command line arguments
        /// </summary>
        public static string[] Args;

        static void Main(string[] args) {

            Program.Args = args;

            try {
                if (IsAdmin()) {
                    Smbus = new Smbus();
                }
            }
            catch {
                // Do nothing
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
            if (Debugger.IsAttached || args.Length == 0) {
                Console.WriteLine("\nPress [enter] to quit.\n");
                Console.ReadLine();
            }
        }

        static void Welcome() {
            string[] header = {
                "    SPDRW - EEPROM SPD reader and writer",
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
                "{0} /find <all|arduino|smbus>",
                "{0} /scan <PORTNAME>",
                "{0} /scan <SMBUS#>",
                "{0} /read <PORTNAME> <ADDRESS#> <filepath> /silent",
                "{0} /read <SMBUS#> <ADDRESS#> <filepath> /silent",
                "{0} /write <PORTNAME> <ADDRESS#> <FILEPATH> /silent",
                "{0} /writeforce <PORTNAME> <ADDRESS#> <FILEPATH> /silent",
                "{0} /firmware <FILEPATH>",
                "{0} /enablewriteprotection <PORTNAME>",
                "{0} /enablewriteprotection <PORTNAME> <block#>",
                "{0} /disablewriteprotection <PORTNAME>",
                "{0} /enablepermanentwriteprotection <PORTNAME> <ADDRESS#>",
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
                        SaveFirmware();
                        break;

                    default:
                        Console.WriteLine("Unknown command line parameters.\n");
                        ShowHelp();
                        break;
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
            finally {
                if (Reader.IsConnected) {
                    Reader.Disconnect();
                }
            }
        }

        /// <summary>
        /// Saves firmware files to a directory
        /// </summary>
        private static void SaveFirmware() {

            if (Args.Length < 2 || Args[1].Length < 1) {
                throw new Exception($"No destination path specified");
            }

            string destinationDir = Args[1] + "\\SpdReaderWriter";

            Directory.CreateDirectory(destinationDir);
            File.WriteAllText(
                path     : destinationDir + "\\SpdReaderWriter.ino",
                contents : Data.BytesToString(Data.DecompressGzip(Resources.SpdReaderWriter_ino)));

            File.WriteAllText(
                path     : destinationDir + "\\SpdReaderWriterSettings.h",
                contents : Data.BytesToString(Data.DecompressGzip(Resources.SpdReaderWriterSettings_h)));

            File.SetAttributes(destinationDir + "\\SpdReaderWriter.ino", FileAttributes.ReadOnly);

            Console.WriteLine($"Firmware files saved to {destinationDir}");
        }

        /// <summary>
        /// Enables RSWP on EEPROM
        /// </summary>
        private static void EnablePswp() {

            Connect();
            if (Eeprom.SetPswp(Reader)) {
                Console.WriteLine($"Permanent write protection enabled on {Reader.PortName}:{Reader.I2CAddress}");
            }
            else {
                throw new Exception($"Unable to set permanent write protection on {Reader.PortName}:{Reader.I2CAddress}");
            }
        }

        /// <summary>
        /// Writes data to EEPROM
        /// </summary>
        private static void WriteEeprom() {

            string mode = Args[0];
            string filePath = Args.Length >= 4 ? Args[3] : "";
            bool silent = Args.Length >= 5 && Args[4] == "/silent";
            byte i2CAddress = (byte)Int32.Parse(Args[2]);

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

            Connect();

            Reader.I2CAddress = i2CAddress;

            Console.WriteLine(
                "Writing \"{0}\" ({1} {2}) to EEPROM at address {3}\n",
                filePath,
                inputFile.Length,
                inputFile.Length > 1 ? "bytes" : "byte",
                Reader.I2CAddress);

            if (inputFile.Length > (int)Spd.GetSpdSize(Reader)) {
                throw new Exception($"File \"{filePath}\" is larger than {Reader.SpdSize} bytes.");
            }

            int bytesWritten = 0;
            int startTick = Environment.TickCount;
            byte b;

            for (int i = 0; i != inputFile.Length; i++) {
                b = inputFile[i];
                bool writeResult = mode == "/writeforce"
                    ? Eeprom.WriteByte(Reader, (ushort)i, b)
                    : Eeprom.UpdateByte(Reader, (ushort)i, b);

                if (!writeResult) {
                    throw new Exception($"Could not write byte {i} to EEPROM at address {Reader.I2CAddress} on port {Reader.PortName}.");
                }

                bytesWritten++;

                if (!silent) {
                    ConsoleDisplayByte(i, b);
                }
            }
            Reader.Disconnect();

            Console.WriteLine(
                "\n\nWritten {0} {1} to EEPROM at address {2} on port {3} in {4} ms",
                bytesWritten,
                bytesWritten > 1 ? "bytes" : "byte",
                Reader.I2CAddress,
                Reader.PortName,
                Environment.TickCount - startTick);
        }

        /// <summary>
        /// Reads data from EEPROM
        /// </summary>
        private static void ReadEeprom() {
            string filePath = Args.Length >= 4 ? Args[3] : "";
            bool silent = Args.Length >= 5 && Args[4] == "/silent";
            byte i2CAddress = (byte)Int32.Parse(Args[2]);
            byte[] spdDump = new byte[0];
            string name;

            Console.Write($"Reading EEPROM at address {i2CAddress}");

            if (filePath != "") {
                Console.WriteLine($" to {filePath}");
            }
            Console.WriteLine("\n");

            int startTick = Environment.TickCount;

            if (Args[1].StartsWith("COM")) {

                Connect();

                name = Reader.ToString();
                Reader.I2CAddress = i2CAddress;
                spdDump = new byte[(int)Spd.GetSpdSize(Reader)];

                for (ushort i = 0; i < spdDump.Length; i++) {
                    spdDump[i] = Eeprom.ReadByte(Reader, i);
                }

                Reader.Disconnect();
            }
            else {
                spdDump = new byte[Smbus.MaxSpdSize];
                Smbus.BusNumber = (byte)Int32.Parse(Args[1]);
                Smbus.I2CAddress = i2CAddress;
                name = $"{Smbus} ({Smbus.BusNumber})";

                for (ushort i = 0; i < spdDump.Length; i++) {
                    spdDump[i] = Eeprom.ReadByte(Smbus, i);
                }
            }

            if (!silent) {
                for (int i = 0; i < spdDump.Length; i++) {
                    ConsoleDisplayByte(i, spdDump[i]);
                }
            }

            Console.Write("\n\nRead {0} {1} from EEPROM at address {2} on {3} in {4} ms",
                spdDump.Length,
                spdDump.Length > 1 ? "bytes" : "byte",
                i2CAddress,
                name,
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
        }

        /// <summary>
        /// Clears RSWP on EEPROM
        /// </summary>
        private static void DisableRswp() {

            Connect();

            Reader.PIN_SA1 = Arduino.Pin.State.ON;

            if (Eeprom.ClearRswp(Reader)) {
                Console.WriteLine("Write protection successfully disabled.");
            }
            else {
                throw new Exception("Unable to clear write protection");
            }

            Reader.ResetAddressPins();
        }

        /// <summary>
        /// Sets RSWP on EEPROM
        /// </summary>
        private static void EnableRswp() {
            int[] block;

            Connect();

            if (Args.Length == 3) { // Block # was specified
                try {
                    block = new[] { Int32.Parse(Args[2]) };
                }
                catch {
                    throw new Exception("Block number should be specified in decimal notation.");
                }

            }
            else { // No block number specified, protect all available
                if (Spd.GetRamType(Reader) == Ram.Type.DDR4) {
                    block = new[] { 0, 1, 2, 3 };
                }
                else { // DDR3 + DDR2
                    block = new[] { 0 };
                }
            }

            Reader.ResetAddressPins();

            for (byte i = 0; i < block.Length; i++) {
                if (Eeprom.SetRswp(Reader, i)) {
                    Console.WriteLine($"Block {i} is now read-only");
                }
                else {
                    Console.WriteLine($"Unable to set write protection for block {i}. Either SA0 is not connected to HV, or the block is already read-only.");
                }
            }
            Reader.Disconnect();
        }

        /// <summary>
        /// Scans Arduino or SMBus for I2C addresses
        /// </summary>
        private static void ScanDevice() {
            if (Args.Length != 2) {
                //todo
            }
            else {

                byte[] addresses = new byte[0];

                if (Args[1].StartsWith("COM")) {

                    Connect();

                    addresses = Reader.Scan();

                    Reader.Disconnect();
                }
                else {
                    int i = -1;
                    Int32.TryParse(Args[1], out i);
                    if (i != -1) {
                        addresses = Smbus.Scan();
                    }
                }

                if (addresses.Length == 0) {
                    throw new Exception("No EEPROM devices found.");
                }

                foreach (int location in addresses) {
                    Console.WriteLine($"Found EEPROM at address: {location}");
                }
            }
        }

        /// <summary>
        /// Establishes a connection with Arduino
        /// </summary>
        private static void Connect() {
            // Init
            string portName = Args[1];

            if (!portName.StartsWith("COM")) {
                throw new Exception("Port name should start with \"COM\" followed by a number.");
            }

            Reader = new Arduino(ReaderSettings, portName);

            // Establish connection
            if (!Reader.Connect()) {
                throw new Exception($"Could not connect to the device on port {portName}.");
            }

            // Check FW version
            string firmwareFile = Data.BytesToString(Data.DecompressGzip(SpdReaderWriterDll.Properties.Resources.SpdReaderWriter_ino));
            if (Reader.GetFirmwareVersion() <
                Int32.Parse(firmwareFile.Split(new string[] { "#define VERSION " }, StringSplitOptions.None)[1].Split(' ')[0].Trim())) {
                throw new Exception($"The device on port {portName} requires its firmware to be updated.");
            }

            if (!Reader.Test()) {
                throw new Exception($"The device on port {portName} does not respond.");
            }
        }

        /// <summary>
        /// Looks for Arduino devices or available SMBuses
        /// </summary>
        private static void FindDevice() {

            if (Args.Length == 1 || Args.Length == 2 && Args[1] == "all") {
                FindArduino();
                FindSmbus();
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
            string[] devices = new Arduino(ReaderSettings).Find();
            if (devices.Length > 0) {
                foreach (string portName in devices) {
                    Console.WriteLine($"Found Arduino on Serial Port: {portName}\n");
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

            ConsoleColor defaultForeColor = Console.ForegroundColor;   // Text Color
            ConsoleColor defaultBackColor = Console.BackgroundColor;   // Background Color

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
            Console.ForegroundColor = defaultForeColor;
            // Reset background color
            Console.BackgroundColor = defaultBackColor;
        }

        /// <summary>
        /// Detects if administrative privileges are present
        /// </summary>
        /// <returns><see langref="true"/> if administrative privileges are present</returns>
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