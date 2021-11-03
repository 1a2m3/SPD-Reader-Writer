using System.ComponentModel;

namespace SpdReaderWriterDll {
    /// <summary>
    /// RAM class
    /// </summary>
    public class Ram {
        /// <summary>
        /// Defines RAM Type (SPD byte 0x02)
        /// </summary>
        public enum Type {
            UNKNOWN      = 0x00,
            SDRAM        = 0x04,
            DDR          = 0x07,
            DDR2         = 0x08,
            [Description("DDR2 FB DIMM")]
            DDR2_FB_DIMM = 0x09,
            DDR3         = 0x0B,
            DDR4         = 0x0C,
            DDR5         = 0x12,
        }

        /// <summary>
        /// Defines SPD sizes
        /// </summary>
        public enum SpdSize {
            UNKNOWN      = 0,
            SDRAM        = 256,
            DDR          = 256,
            DDR2         = 256,
            DDR2_FB_DIMM = 256,
            DDR3         = 256,
            DDR4         = 512,
            DDR5         = 1024,
        }

        /// <summary>
        /// Device supported RAM types
        /// </summary>
        public struct BitMask {
            /// <summary>
            /// Value describing <value>DDR2</value> support
            /// </summary>
            public const byte DDR2 = 1 << 2;

            /// <summary>
            /// Value describing <value>DDR3</value> support
            /// </summary>
            public const byte DDR3 = 1 << 3;

            /// <summary>
            /// Value describing <value>DDR4</value> support
            /// </summary>
            public const byte DDR4 = 1 << 4;

            /// <summary>
            /// Value describing <value>DDR5</value> support
            /// </summary>
            public const byte DDR5 = 1 << 5;
        }
    }
}
