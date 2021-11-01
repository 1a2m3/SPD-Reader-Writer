namespace SpdReaderWriterDll {
    /// <summary>
    /// Class describing digital pin states
    /// </summary>
    public struct Pin {

        /// <summary>
        /// DDR5 offline mode pin
        /// </summary>
        public const byte OFFLINE_MODE_SWITCH = 0;
        /// <summary>
        /// Slave address 1 (SA1) control pin
        /// </summary>
        public const byte SA1_SWITCH = 1;
        /// <summary>
        /// High voltage (9V) control pin
        /// </summary>
        public const byte HIGH_VOLTAGE_SWITCH = 9;

        public struct State {
            /// <summary>
            /// Name state describing condition when pin is <b>HIGH</b>
            /// </summary>
            public const bool HIGH     = true;

            /// <summary>
            /// Name state describing condition when pin is <b>LOW</b>
            /// </summary>
            public const bool LOW      = false;

            // Aliases for HIGH
            public const bool VDDSPD   = HIGH;
            public const bool PULLUP   = HIGH;
            public const bool VCC      = HIGH;
            public const bool ON       = HIGH;
            public const bool ENABLE   = HIGH;
            public const bool ENABLED  = HIGH;

            // Aliases for LOW
            public const bool VSSSPD   = LOW;
            public const bool PUSHDOWN = LOW;
            public const bool VSS      = LOW;
            public const bool GND      = LOW;
            public const bool OFF      = LOW;
            public const bool DISABLE  = LOW;
            public const bool DISABLED = LOW;
            public const bool DEFAULT  = LOW;
        }
    }


}