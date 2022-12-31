/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using static SpdReaderWriterDll.Spd;

namespace SpdReaderWriterDll {

    /// <summary>
    /// Interface for different SPD classes
    /// </summary>
    public interface ISpd {

        DataLength Length { get; }
        int SpdBytesUsed { get; }
        RamType DramDeviceType { get; }
        ManufacturerIdCodeData ManufacturerIdCode { get; }
        string PartNumber { get; }
        DateCodeData ModuleManufacturingDate { get; }
        //ulong DieDensity { get; }
        ulong TotalModuleCapacity { get; }
        bool CrcStatus { get; }
        bool FixCrc();
    }
}