#if DEBUG

using System;
using System.IO.Ports;
using SpdReaderWriterDll;

namespace SpdReaderWriter {
    class Example {

        /// <summary>
        /// Serial port settings
        /// </summary>
        public static Device.SerialPortSettings readerSettings = new Device.SerialPortSettings(
            // Baud rate
            115200,
            // Enable DTR
            true,
            // Enable RTS
            true,
            // Data bits
            8,
            // Handshake
            Handshake.None,
            // New line
            "\n",
            // Parity
            Parity.None,
            // Stop bits
            StopBits.One,
            // Use event handler
            true,
            // Response timeout (sec.)
            10);

        /// <summary>
        /// Serial Port name
        /// </summary>
        public static string portName = "COM3";

        /// <summary>
        /// Basic use
        /// </summary>
        public static void Example_BasicUse() {
            // Initialize the device
            Device myDevice = new Device(readerSettings, portName);
            //myDevice.Connect();

            // Test if the device responds (optional)
            myDevice.Test();

            // Set EEPROM address to 80 (0x50)
            myDevice.I2CAddress = 80;

            // Make sure the address is valid (optional)
            myDevice.ProbeAddress(myDevice.I2CAddress);
            myDevice.ProbeAddress(); // You can omit the address if myDevice already has "I2CAddress" set

            // Set SPD size to DDR4's EEPROM size (512 bytes)
            myDevice.SpdSize = Ram.SpdSize.DDR4;

            // The device can also be initialized in one line, like so:
            Device myOtherDevice = new Device(readerSettings, portName, 80, Ram.SpdSize.DDR4);

            // Read first byte at offset 0
            byte firstByte = Eeprom.ReadByte(myDevice, 0);

            // Read last byte (located at offset 511, not 512!)
            byte lastByte = Eeprom.ReadByte(myDevice, (ushort)(myDevice.SpdSize - 1));

            // Read the entire EEPROM, 1 byte at a time
            byte[] spdDump = new byte[(int)myDevice.SpdSize];
            for (ushort i = 0; i < (int)myDevice.SpdSize; i++) {
                spdDump[i] = Eeprom.ReadByte(myDevice, i);
            }

            // When you're done, disconnect
            myDevice.Disconnect();

            // Attempt to read  once we're disconnected (this will fail)
            Eeprom.ReadByte(myDevice, 0, 1);
        }

        /// <summary>
        /// Test an unreachable device to make sure all functions properly return false
        /// </summary>
        public static void Example_TestNonConnectableDevice() {
            Device UnreachableDevice = new Device(readerSettings,"COM666");
            UnreachableDevice.I2CAddress = 0x50;
            if (UnreachableDevice.IsConnected) {
                // This won't be reached
            }
            UnreachableDevice.Test(); // false
        }

        /// <summary>
        /// Test and scan devices' I2C bus
        /// </summary>
        public static void Example_TestRealDevice() {
            // Test a real device
            Device RealDevice = new Device(readerSettings, portName);
            RealDevice.Test(); //true
            RealDevice.Scan(); //{ 80 }

            // Test a real device
            Device MyReader = new Device(readerSettings, portName, 0x50, Ram.SpdSize.DDR4);
            MyReader.Test(); //true
        }

        /// <summary>
        /// Duplicate one EEPROM contents to another
        /// </summary>
        public static void Example_DuplicateRam() {
            // Copy SPD contents from one DIMM to another
            Device source = new Device(readerSettings, "COM1", 80, Ram.SpdSize.DDR4);
            Device destination = new Device(readerSettings, "COM4", 82, source.SpdSize);

            for (ushort i = 0; i < (int)source.SpdSize; i++) {
                Eeprom.WriteByte(destination, i, Eeprom.ReadByte(source, i));
            }

            // Verify contents
            for (ushort i = 0; i < (int)source.SpdSize; i++) {
                if (Eeprom.ReadByte(source, i) != Eeprom.ReadByte(destination, i)) {
                    // Mismatched contents detected
                }
            }
        }

        /// <summary>
        /// Check and fix CRC
        /// </summary>
        public static void Example_FixCRC() {

            Device MyReader = new Device(readerSettings, portName, 0x52, Ram.SpdSize.DDR4);

            // Read first 126 bytes
            byte[] spdHeader = Eeprom.ReadByte(MyReader, 0, 126);

            // Calculate CRC
            ushort crc = Spd.Crc16(spdHeader, 0x1021);

            // Get LSB (byte 127) and MSB (byte 128)
            byte CrcLsb = (byte)(crc & 0xff);   // CRC LSB at 0x7e for 0-125 range or @ 0xfe for 128-253 range
            byte CrcMsb = (byte)(crc >> 8);     // CRC MSB at 0x7f for 0-125 range or @ 0xff for 128-253 range

            // Compare calculated CRC against SPD data
            if (Eeprom.ReadByte(MyReader, 0x7e, 1)[0] == CrcLsb && Eeprom.ReadByte(MyReader, 0x7f, 1)[0] == CrcMsb) {
                // The checksum is correct, do nothing
                return;
            }
            else {
                // Write correct values to SPD
                Eeprom.UpdateByte(MyReader, 0x7e, CrcLsb);
                Eeprom.UpdateByte(MyReader, 0x7f, CrcMsb);
            }
            // Note: you'll have to do the same for 128-253 range, checksum bytes are 0xfe and 0xff
        }

        /// <summary>
        /// Erase SPD contents (fill with 0xff's)
        /// </summary>
        public static void Example_EraseSPD() {
            Device MyReader = new Device(readerSettings, portName, 0x50, Ram.SpdSize.DDR4);
            for (ushort i = 0; i <= (int)MyReader.SpdSize; i++) {
                Eeprom.UpdateByte(MyReader, i, 0xff);
                Console.WriteLine(i.ToString());
            }
        }

        /// <summary>
        /// Scan an entire I2C bus
        /// </summary>
        public static void ScanRange() {

            Device myDevice = new Device(readerSettings, "COM8");

            bool[] _probes = new bool[128];

            for (byte i = 0; i < 128; i++) {
                _probes[i] = myDevice.ProbeAddress(i);
            }
        }
    }
}
#endif
