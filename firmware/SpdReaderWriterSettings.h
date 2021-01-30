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
#define PORT        Serial   // Communications Port, default is "Serial". Change to "SerialUSB" for native USB Arduinos (Leonardo, Micro, Due, Yun, etc)
#define BAUD_RATE   115200   // Serial port baud rate, must match program's serial baud rate
#define I2CCLOCK    100000   // I2C communication clock frequency, default value is 100000. Set to 400000 to enable Fast Mode. Some processors also support 10000 (low speed mode), 1000000 (fast mode plus) and 3400000 (high speed mode). 

/* -= Pins config =- */
#define HVSW         9       // High Voltage (9V) switch pin number
#define HVDET        6       // High Voltage (9V) detector pin number
#define SA0SW       A0       // SA0 select pin number
#define SA1SW       A1       // SA1 select pin number
#define SA2SW       A2       // SA2 select pin number
