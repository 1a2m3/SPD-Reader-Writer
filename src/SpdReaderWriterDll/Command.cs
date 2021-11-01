namespace SpdReaderWriterDll {
    /// <summary>
    /// Device commands
    /// </summary>
    public struct Command {
        /// <summary>
        /// Read byte
        /// </summary>
        public const byte READBYTE     = (byte) 'r';
        /// <summary>
        /// Write byte
        /// </summary>
        public const byte WRITEBYTE    = (byte) 'w';
        /// <summary>
        /// Scan i2c bus
        /// </summary>
        public const byte SCANBUS      = (byte) 's';
        /// <summary>
        /// Probe i2c address
        /// </summary>
        public const byte PROBEADDRESS = (byte) 'a';
        /// <summary>
        /// Control pin state
        /// </summary>
        public const byte PINCONTROL   = (byte) 'p';
        /// <summary>
        /// RSWP control
        /// </summary>
        public const byte RSWP         = (byte) 'b';
        /// <summary>
        /// Enable Permanent SWP
        /// </summary>
        public const byte PSWP         = (byte) 'l';
        /// <summary>
        /// Get Firmware version
        /// </summary>
        public const byte GETVERSION   = (byte) 'v';
        /// <summary>
        /// Device Communication Test
        /// </summary>
        public const byte TESTCOMM     = (byte) 't';
        /// <summary>
        /// Report supported RAM type(s)
        /// </summary>
        public const byte RAMSUPPORT   = (byte) 'f';
        /// <summary>
        /// Device name controls
        /// </summary>
        public const byte NAME         = (byte) 'n';
        /// <summary>
        /// DDR4 detection
        /// </summary>
        public const byte DDR4DETECT   = (byte) '4';
        /// <summary>
        /// DDR5 detection
        /// </summary>
        public const byte DDR5DETECT   = (byte) '5';
        /// <summary>
        /// Suffix added to get current state
        /// </summary>
        public const byte GET          = (byte) '?';
        /// <summary>
        /// Suffix added to set state equivalent to true
        /// </summary>
        public const byte ON           = 1;
        /// <summary>
        /// Suffix added to set state equivalent to false
        /// </summary>
        public const byte OFF          = 0;
        /// <summary>
        /// "Do not care" byte
        /// </summary>
        public const byte DNC          = 0;
    }
}