/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

#if DEBUG

using System;
using SpdReaderWriterDll;

namespace SpdReaderWriter {
    class Example {

        /// <summary>
        /// Serial port settings
        /// </summary>
        public static Arduino.SerialPortSettings ReaderSettings = new Arduino.SerialPortSettings(
            // Baud rate
            115200,
            // Enable DTR
            true,
            // Enable RTS
            true,
            // Response timeout (sec.)
            10);

        /// <summary>
        /// Serial Port name
        /// </summary>
        public static string PortName = "COM3";

        /// <summary>
        /// Basic use
        /// </summary>
        public static void Example_BasicUse() {
            // Initialize the device
            Arduino myDevice = new Arduino(ReaderSettings, PortName);
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
            Arduino myOtherDevice = new Arduino(ReaderSettings, PortName, 80, Ram.SpdSize.DDR4);

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
            Arduino unreachableDevice = new Arduino(ReaderSettings,"COM666");
            unreachableDevice.I2CAddress = 0x50;
            if (unreachableDevice.IsConnected) {
                // This won't be reached
            }
            unreachableDevice.Test(); // false
        }

        /// <summary>
        /// Test and scan devices' I2C bus
        /// </summary>
        public static void Example_TestRealDevice() {
            // Test a real device
            Arduino realDevice = new Arduino(ReaderSettings, PortName);
            realDevice.Test(); //true
            realDevice.Scan(); //{ 80 }

            // Test a real device
            Arduino myReader = new Arduino(ReaderSettings, PortName, 0x50, Ram.SpdSize.DDR4);
            myReader.Test(); //true
        }

        /// <summary>
        /// Duplicate one EEPROM contents to another
        /// </summary>
        public static void Example_DuplicateRam() {
            // Copy SPD contents from one DIMM to another
            Arduino source = new Arduino(ReaderSettings, "COM1", 80, Ram.SpdSize.DDR4);
            Arduino destination = new Arduino(ReaderSettings, "COM4", 82, source.SpdSize);

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

            Arduino myReader = new Arduino(ReaderSettings, PortName, 0x52, Ram.SpdSize.DDR4);

            // Read first 126 bytes
            byte[] spdHeader = Eeprom.ReadByte(myReader, 0, 126);

            // Calculate CRC
            ushort crc = Data.Crc16(spdHeader, 0x1021);

            // Get LSB (byte 127) and MSB (byte 128)
            byte crcLsb = (byte)(crc & 0xFF);   // CRC LSB at 0x7E for 0-125 range or @ 0xFE for 128-253 range
            byte crcMsb = (byte)(crc >> 8);     // CRC MSB at 0x7F for 0-125 range or @ 0xFF for 128-253 range

            // Compare calculated CRC against SPD data
            if (Eeprom.ReadByte(myReader, 0x7e, 1)[0] == crcLsb && Eeprom.ReadByte(myReader, 0x7f, 1)[0] == crcMsb) {
                // The checksum is correct, do nothing
                return;
            }
            else {
                // Write correct values to SPD
                Eeprom.UpdateByte(myReader, 0x7e, crcLsb);
                Eeprom.UpdateByte(myReader, 0x7f, crcMsb);
            }
            // Note: you'll have to do the same for 128-253 range, checksum bytes are 0xfe and 0xFF
        }

        /// <summary>
        /// Erase SPD contents (fill with 0xFF's)
        /// </summary>
        public static void Example_EraseSPD() {
            Arduino myReader = new Arduino(ReaderSettings, PortName, 0x50, Ram.SpdSize.DDR4);
            for (ushort i = 0; i <= (int)myReader.SpdSize; i++) {
                Eeprom.UpdateByte(myReader, i, 0xFF);
                Console.WriteLine(i.ToString());
            }
        }

        /// <summary>
        /// Scan an entire I2C bus
        /// </summary>
        public static void ScanRange() {

            Arduino myDevice = new Arduino(ReaderSettings, "COM8");

            bool[] probes = new bool[128];

            for (byte i = 0; i < 128; i++) {
                probes[i] = myDevice.ProbeAddress(i);
            }
        }
    }
}
#endif