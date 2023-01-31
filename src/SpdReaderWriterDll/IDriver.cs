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
    /// Driver interface
    /// </summary>
    public interface IDriver {

        // Driver functions
        bool InstallDriver();
        bool RemoveDriver();
        bool StartDriver();
        bool StopDriver();

        // Driver info and status
        bool IsInstalled { get; }
        bool IsServiceRunning { get; }
        bool IsReady { get; }
    }
}