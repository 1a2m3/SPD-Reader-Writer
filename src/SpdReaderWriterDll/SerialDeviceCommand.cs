namespace SpdReaderWriterDll {
    /// <summary>
    /// Device commands
    /// </summary>
    public struct SerialDeviceCommand {
        /// <summary>
        /// Read byte
        /// </summary>
        public const byte READBYTE     = (byte) 'r';
        /// <summary>
        /// Write byte
        /// </summary>
        public const byte WRITEBYTE    = (byte) 'w';
        /// <summary>
        /// Write page
        /// </summary>
        public const byte WRITEPAGE    = (byte) 'g';
        /// <summary>
        /// Scan i2c bus
        /// </summary>
        public const byte SCANBUS      = (byte) 's';
        /// <summary>
        /// Set i2c clock 
        /// </summary>
        public const byte I2CCLOCK     = (byte) 'c';
        /// <summary>
        /// Probe i2c address
        /// </summary>
        public const byte PROBEADDRESS = (byte) 'a';
        /// <summary>
        /// Config pin state control
        /// </summary>
        public const byte PINCONTROL   = (byte) 'p';
        /// <summary>
        /// RSWP control
        /// </summary>
        public const byte RSWP         = (byte) 'b';
        /// <summary>
        /// PSWP control
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
        /// Report current RSWP RAM support
        /// </summary>
        public const byte RSWPREPORT   = (byte) 'f';
        /// <summary>
        /// Re-evaluate RSWP capabilities
        /// </summary>
        public const byte RETESTRSWP   = (byte) 'e';
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
        /// Restore device settings to default
        /// </summary>
        public const byte FACTORYRESET = (byte) '-';
        /// <summary>
        /// Suffix added to get current state
        /// </summary>
        public const byte GET          = (byte) '?';
        /// <summary>
        /// Suffix added to set state equivalent to true/on/enable etc
        /// </summary>
        public const byte ON           = 1;
        /// <summary>
        /// Suffix added to set state equivalent to false/off/disable etc
        /// </summary>
        public const byte OFF          = 0;
        /// <summary>
        /// "Do not care" byte
        /// </summary>
        public const byte DNC          = 0;
    }
}
