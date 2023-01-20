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

            // The device can also be initialized in one line, like so:
            Arduino myOtherDevice = new Arduino(ReaderSettings, PortName, 80);

            // Read first byte at offset 0
            byte firstByte = Eeprom.Read(myDevice, 0);

            // Read last byte (located at offset 511, not 512!)
            byte lastByte = Eeprom.Read(myDevice, (ushort)(myDevice.DataLength - 1));

            // Read the entire EEPROM, 1 byte at a time
            byte[] spdDump = new byte[myDevice.DataLength];
            for (ushort i = 0; i < myDevice.DataLength; i++) {
                spdDump[i] = Eeprom.Read(myDevice, i);
            }

            // When you're done, disconnect
            myDevice.Disconnect();

            // Attempt to read  once we're disconnected (this will fail)
            Eeprom.Read(myDevice, 0, 1);
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
            Arduino myReader = new Arduino(ReaderSettings, PortName, 0x50);
            myReader.Test(); //true
        }

        /// <summary>
        /// Duplicate one EEPROM contents to another
        /// </summary>
        public static void Example_DuplicateRam() {
            // Copy SPD contents from one DIMM to another
            Arduino source = new Arduino(ReaderSettings, "COM1", 80);
            Arduino destination = new Arduino(ReaderSettings, "COM4", 82);

            for (ushort i = 0; i < source.DataLength; i++) {
                Eeprom.Write(destination, i, Eeprom.Read(source, i));
            }

            // Verify contents
            for (ushort i = 0; i < source.DataLength; i++) {
                if (Eeprom.Read(source, i) != Eeprom.Read(destination, i)) {
                    // Mismatched contents detected
                }
            }
        }

        /// <summary>
        /// Check and fix CRC
        /// </summary>
        public static void Example_FixCRC() {

            Arduino myReader = new Arduino(ReaderSettings, PortName, 0x52);

            // Read first 126 bytes
            byte[] spdHeader = Eeprom.Read(myReader, 0, 126);

            // Calculate CRC
            ushort crc = Data.Crc16(spdHeader, 0x1021);

            // Get LSB (byte 127) and MSB (byte 128)
            byte crcLsb = (byte)(crc & 0xFF);   // CRC LSB at 0x7E for 0-125 range or @ 0xFE for 128-253 range
            byte crcMsb = (byte)(crc >> 8);     // CRC MSB at 0x7F for 0-125 range or @ 0xFF for 128-253 range

            // Compare calculated CRC against SPD data
            if (Eeprom.Read(myReader, 0x7E, 1)[0] == crcLsb && Eeprom.Read(myReader, 0x7F, 1)[0] == crcMsb) {
                // The checksum is correct, do nothing
                return;
            }
            else {
                // Write correct values to SPD
                Eeprom.Update(myReader, 0x7E, crcLsb);
                Eeprom.Update(myReader, 0x7F, crcMsb);
            }
            // Note: you'll have to do the same for 128-253 range, checksum bytes are 0xFE and 0xFF
        }

        /// <summary>
        /// Erase SPD contents (fill with 0xFF's)
        /// </summary>
        public static void Example_EraseSPD() {
            Arduino myReader = new Arduino(ReaderSettings, PortName, 0x50);
            for (ushort i = 0; i <= myReader.DataLength; i++) {
                Eeprom.Update(myReader, i, 0xFF);
                Console.WriteLine(i.ToString());
            }
        }

        /// <summary>
        /// Scan an entire I2C bus
        /// </summary>
        public static void ScanRange() {

            Arduino myDevice = new Arduino(ReaderSettings, "COM8");

            bool[] probes = new bool[128];

            for (byte i = 0; i < probes.Length; i++) {
                probes[i] = myDevice.ProbeAddress(i);
            }
        }
    }
}
#endif