/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

namespace SpdReaderWriterCore {

    /// <summary>
    /// Device interface
    /// </summary>
    public interface IDevice {

        byte I2CAddress { get; set; }
        bool IsConnected { get; }
        ushort MaxSpdSize { get; }

        byte[] Addresses { get; }

        bool Connect();
        bool Disconnect();

        byte[] Scan();

        bool GetOfflineMode();
        bool ProbeAddress(byte address);
    }
}