namespace SpdReaderWriterDll {
    /// <summary>
    /// Class describing different responses received from the device
    /// </summary>
    public struct Response {
        /// <summary>
        /// Indicates the operation was executed successfully
        /// </summary>
        public const byte SUCCESS         = 0x01;
        /// <summary>
        /// Indicates the operation has failed
        /// </summary>
        public const byte ERROR           = 0xFF;
        /// <summary>
        /// A response used to indicate an error when normally a numeric non-zero answer is expected if the operation was executed successfully
        /// </summary>
        public const byte NULL            = 0;
        /// <summary>
        /// A response used to describe when SA pin is tied to VCC
        /// </summary>
        public const byte ON              = 1;
        /// <summary>
        /// A response used to describe when SA pin is tied to GND
        /// </summary>
        public const byte OFF             = 0;
        /// <summary>
        /// A response expected from the device after executing Command.TESTCOMM command to identify the correct device
        /// </summary>
        public const char WELCOME         = '!';
        /// <summary>
        /// Bitmask value indicating offline mode control is OK
        /// </summary>
        public const byte OFFLINE_TEST_OK = 0b0001;
        /// <summary>
        /// Bitmask value indicating offline mode control is N/A
        /// </summary>
        public const byte OFFLINE_TEST_NA = OFFLINE_TEST_OK << 4;
        /// <summary>
        /// Bitmask value indicating SA1 control is OK
        /// </summary>
        public const byte SA1_TEST_OK     = 0b0010;
        /// <summary>
        /// Bitmask value indicating SA1 control is N/A
        /// </summary>
        public const byte SA1_TEST_NA     = SA1_TEST_OK << 4;
        /// <summary>
        /// Bitmask value indicating HIGH_VOLTAGE_SWITCH control is OK
        /// </summary>
        public const byte VHV_TEST_OK     = 0b1000;
        /// <summary>
        /// Bitmask value indicating HIGH_VOLTAGE_SWITCH control is N/A
        /// </summary>
        public const byte VHV_TEST_NA     = VHV_TEST_OK << 4;


        // Aliases
        public const byte ACK   = SUCCESS;
        public const byte NACK  = ERROR;
        public const byte NOACK = ERROR;
        public const byte FAIL  = ERROR;
        public const byte ZERO  = NULL;
    }
}