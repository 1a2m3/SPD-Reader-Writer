/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using System.ComponentModel;

namespace SpdReaderWriterDll {

    /// <summary>
    /// RAM class
    /// </summary>
    public class Ram {

        /// <summary>
        /// Defines basic memory type byte value
        /// </summary>
        public enum Type {
            UNKNOWN       = 0x00,
            SDRAM         = 0x04,
            DDR           = 0x07,
            DDR2          = 0x08,
            [Description("DDR2 FB DIMM")]
            DDR2_FB_DIMM  = 0x09,
            DDR3          = 0x0B,
            LPDDR3        = 0x0F,
            DDR4          = 0x0C,
            DDR4E         = 0x0E,
            LPDDR4        = 0x10,
            LPDDR4X       = 0x11,
            DDR5          = 0x12,
            LPDDR5        = 0x13,
            [Description("DDR5 NVDIMM-P")]
            DDR5_NVDIMM_P = 0x14,
        }

        /// <summary>
        /// Defines SPD sizes
        /// </summary>
        public enum SpdSize {
            UNKNOWN       = 0,
            MINIMUM       = 256,
            SDRAM         = 256,
            DDR           = 256,
            DDR2          = 256,
            DDR2_FB_DIMM  = 256,
            DDR3          = 256,
            DDR4          = 512,
            DDR5          = 1024,
        }
    }
}