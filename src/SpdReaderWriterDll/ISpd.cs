/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using System;

namespace SpdReaderWriterDll {

    /// <summary>
    /// Interface for different SPD classes
    /// </summary>
    public interface ISpd {

        int SpdBytesUsed { get; }
        Spd.RamType DramDeviceType { get; }
        Spd.DateCodeData ModuleManufacturingDate { get; }
        UInt32 DieDensity { get; }
        UInt32 TotalModuleCapacity { get; }

        string ToString();

    }
}
