/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

   You can edit values in this file according to your specific hardware configuration

*/

/* -= Communication settings =- */
#define PORT        Serial               // Communications Port, default is "Serial". Change to "SerialUSB" for native USB Arduinos (Leonardo, Micro, Due, Yun, etc)
#define BAUD_RATE   115200               // Serial port baud rate, default value is "115200". Must match program's serial baud rate.
#define I2C_CLOCK   100000               // I2C communication clock frequency, default value is "100000". Set to "400000" to enable Fast Mode.

/* -= Pins config =- */
#define HV_SW        9                   // High Voltage (9V) switch pin    (DDR4/DDR3/DDR2)
#define HV_FB        6                   // High Voltage (9V) feedback pin  (DDR4/DDR3/DDR2)
#define OFF_SW      A0                   // Offline mode switch pin         (DDR5)
#define SA1_SW      A1                   // SA1 switch pin                  (DDR3/DDR2)

/* -= RAM support =- */
#define RAM_SUPPORT DDR2|DDR3|DDR4|DDR5  // Enter pipe-separated RAM defines to enable specific RAM support. Default value is "DDR2|DDR3|DDR4|DDR5". (SDRAM and DDR are supported by default). To leave SDRAM and DDR support only, enter "DEFAULT"
