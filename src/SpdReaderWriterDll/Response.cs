namespace SpdReaderWriterDll {
    /// <summary>
    /// Class describing different responses received from the device
    /// </summary>
    public struct Response {
        /// <summary>
        /// Boolean True response
        /// </summary>
        public const byte TRUE    = 0x01;
        /// <summary>
        /// Boolean False response
        /// </summary>
        public const byte FALSE   = 0x00;
        /// <summary>
        /// Indicates the operation has failed
        /// </summary>
        public const byte ERROR   = 0xFF;
        /// <summary>
        /// Indicates the operation was executed successfully
        /// </summary>
        public const byte SUCCESS = 0x01;
        /// <summary>
        /// A response used to indicate an error when normally a numeric non-zero answer is expected if the operation was executed successfully
        /// </summary>
        public const byte NULL    = 0;
        /// <summary>
        /// A response used to describe when SA pin is tied to VCC
        /// </summary> 
        public const byte ON      = 1;
        /// <summary>
        /// A response used to describe when SA pin is tied to GND
        /// </summary>
        public const byte OFF     = 0;
        /// <summary>
        /// A response expected from the device after executing Command.TESTCOMM command to identify the correct device
        /// </summary>
        public const char WELCOME = '!';
        /// <summary>
        /// A response indicating the command or syntax was not in a correct fromat
        /// </summary>
        public const char UNKNOWN = '?';

        // Aliases
        public const byte ACK      = SUCCESS;
        public const byte ENABLED  = TRUE;
        public const byte DISABLED = FALSE;
        public const byte NACK     = ERROR;
        public const byte NOACK    = ERROR;
        public const byte FAIL     = ERROR;
        public const byte ZERO     = NULL;
    }
}
