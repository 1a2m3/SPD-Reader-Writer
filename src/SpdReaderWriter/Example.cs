#if DEBUG

using System;
using SpdReaderWriterDll;

namespace SpdReaderWriter {
    class Example {

        public static void Example_BasicUse() {
            // Initialize the device
            Device myDevice = new Device("COM3");
            //myDevice.Connect();

            // Test if the device responds (optional)
            myDevice.Test();

            // Set EEPROM address to 80 (0x50)
            myDevice.EepromAddress = 80;

            // Make sure the address is valid (optional)
            myDevice.ProbeAddress(myDevice.EepromAddress);
            myDevice.ProbeAddress(); // You can omit the address if myDevice already has "EepromAddress" set

            // Set SPD size to DDR4's EEPROM size (512 bytes)
            myDevice.SpdSize = SpdSize.DDR4;

            // The device can also be initialized in one line, like so:
            Device myOtherDevice = new Device("COM3", 80, SpdSize.DDR4);

            // Read first byte at offset 0
            byte firstByte = Eeprom.ReadByte(myDevice, 0);

            // Read last byte (located at offset 511, not 512!)
            byte lastByte = Eeprom.ReadByte(myDevice, (ushort)(myDevice.SpdSize - 1));

            // Read the entire EEPROM (this will take a bit longer, about 1-5 seconds)
            byte[] spdDump = Eeprom.ReadByte(myDevice, 0, (int)myDevice.SpdSize);

            // When you're done, disconnect
            myDevice.Disconnect();

            // Attempt to read  once we're disconnected (this will fail)
            Eeprom.ReadByte(myDevice, 0, (int)SpdSize.DDR4);

        }

        public static void Example_TestNonConnectableDevice() {
            // Test an unreachable device to make sure all functions properly return false
            Device UnreachableDevice = new Device("COM666");
            UnreachableDevice.EepromAddress = 0x50;
            if (UnreachableDevice.IsConnected) {
                // This won't be reached
            }
            UnreachableDevice.Test(); // false
            UnreachableDevice.Scan(1, 20); // Scan an inaccessible range
        }

        public static void Example_TestRealDevice() {
            // Test a real device
            Device RealDevice = new Device("COM3");
            RealDevice.Test(); //true
            RealDevice.Scan(11, 99); //{ 80 }

            // Test a real device
            Device MyReader = new Device("COM3", 0x50, SpdSize.DDR4);
            MyReader.Test(); //true
        }

        public static void Example_DuplicateRam() {
            // Copy SPD contents from one DIMM to another
            Device source = new Device("COM3", 80, SpdSize.DDR4);
            Device destination = new Device("COM4", 82, source.SpdSize);

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

        public static void Example_FixCRC() {

            Device MyReader = new Device("COM3", 0x52, SpdSize.DDR4);

            // Read first 126 bytes
            byte[] spdHeader = Eeprom.ReadByte(MyReader, 0, 126);

            // Calculate CRC
            ushort crc = Spd.Crc16(spdHeader);

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

        public static void Example_EraseSPD() {
            // Erase SPD contents (fill with 0xff's)
            Device MyReader = new Device("COM3", 0x50, SpdSize.DDR4);
            for (ushort i = 0; i <= (int)MyReader.SpdSize; i++) {
                Eeprom.UpdateByte(MyReader, i, 0xff);
                Console.WriteLine(i.ToString());
            }
        }

        public static void ScanRange() {

            Device myDevice = new Device("COM8");

            bool[] _probes = new bool[256];

            for (int i = 0; i < 256; i++) {
                _probes[i] = myDevice.ProbeAddress(i);
            }
        }
    }
}
#endif