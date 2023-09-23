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
    /// Platform Vendor ID
    /// </summary>
    public enum VendorId : ushort {
        AMD    = 0x1022,
        ATI    = 0x1002, // Former ATI vendor ID which is now owned by AMD, who has 2 vendor IDs
        Intel  = 0x8086,
        Nvidia = 0x10DE,
        SiS    = 0x1039,
        VIA    = 0x1106,
    }
}
