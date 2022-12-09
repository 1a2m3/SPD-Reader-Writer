/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

namespace SpdReaderWriterDll {

    /// <summary>
    /// Driver interface
    /// </summary>
    public interface IDriver {
        bool IsInstalled { get; }
        bool IsServiceRunning { get; }
        bool IsHandleOpen { get; }
        bool IsValid { get; }
        bool IsReady { get; }
    }
}