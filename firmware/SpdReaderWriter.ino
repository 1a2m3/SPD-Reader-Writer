/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

   PS: DO NOT EDIT THIS FILE UNLESS YOU KNOW WHAT YOU ARE DOING!
   CONFIGURABLE SETTINGS ARE IN "SpdReaderWriterSettings.h" FILE

*/

#include <Wire.h>
#include <EEPROM.h>
#include "SpdReaderWriterSettings.h"  // Settings

#define VERSION 20211206 // Version number (YYYYMMDD)

// RSWP RAM support bitmasks
#define DDR5 (1 << 5) // Offline mode control
#define DDR4 (1 << 4) // VHV control
#define DDR3 (1 << 3) // VHV+SA1 controls

// SPD5 hub registers
#pragma region SPD5 hub registers
#define MEMREG 0b11111 // SPD5 internal register bitmask
#define MR0  0x00  // Device Type; Most Significant Byte
#define MR1  0x01  // Device Type; Least Significant Byte
#define MR2  0x02  // Device Revision
#define MR3  0x03  // Vendor ID Byte 0
#define MR4  0x04  // Vendor ID Byte 1
#define MR5  0x05  // Device Capability
#define MR6  0x06  // Device Write Recovery Time Capability
#define MR11 0x0B  // I2C Legacy Mode Device Configuration
#define MR12 0x0C  // Write Protection For NVM Blocks [7:0]
#define MR13 0x0D  // Write Protection for NVM Blocks [15:8]
#define MR14 0x0E  // Device Configuration - Host and Local Interface IO
#define MR15 0x0F  // Device Configuration - Local Interface Pull-up Resistors
#define MR18 0x12  // Device Configuration
#define MR19 0x13  // Clear Register MR51 Temperature Status Command
#define MR20 0x14  // Clear Register MR52 Error Status Command
#define MR48 0x30  // Device Status
#define MR51 0x33  // TS Temperature Status
#define MR52 0x34  // Hub, Thermal and NVM Error Status
#pragma endregion

// EEPROM page commands
#pragma region EEPROM page commands
#define SPA0 0x6C  // Set EE Page Address to 0 (addresses  00h to  FFh) (  0-255) (DDR4)
#define SPA1 0x6E  // Set EE Page Address to 1 (addresses 100h to 1FFh) (256-511) (DDR4)
#define RPA  0x6D  // Read EE Page Address                                        (DDR4)
#pragma endregion

// EEPROM RSWP commands
#pragma region EEPROM RSWP commands
#define RPS0 0x63  // Read SWP0 status         (addresses  00h to  7Fh) (  0-127) (DDR4/DDR3/DDR2)
#define RPS1 0x69  // Read SWP1 status         (addresses  80h to  FFh) (128-255) (DDR4)
#define RPS2 0x6B  // Read SWP2 status         (addresses 100h to 17Fh) (256-383) (DDR4)
#define RPS3 0x61  // Read SWP3 status         (addresses 180h to 1FFh) (384-511) (DDR4)

#define SWP0 0x62  // Set RSWP for block 0     (addresses  00h to  7Fh) (  0-127) (DDR4/DDR3/DDR2) *
#define SWP1 0x68  // Set RSWP for block 1     (addresses  80h to  FFh) (128-255) (DDR4)
#define SWP2 0x6A  // Set RSWP for block 2     (addresses 100h to 17Fh) (256-383) (DDR4)
#define SWP3 0x60  // Set RSWP for block 3     (addresses 180h to 1FFh) (384-511) (DDR4)           *

#define CWP  0x66  // Clear RSWP                                                  (DDR4/DDR3/DDR2) *
#pragma endregion

// EEPROM PSWP commands
#define PWPB 0b0110  // PSWP Device Type Identifier Control Code (bits 7-4)       (DDR3/DDR2)

// EEPROM temperature sensor register commands
#pragma region EEPROM temperature sensor register commands
#define TSRB 0b0011 // Device select code to access Temperature Sensor registers (bits 7-4)
#define TS00 0x00   // Capability Register [RO]
#define TS01 0x01   // Configuration Register [R/W]
#define TS02 0x02   // High Limit Register [R/W]
#define TS03 0x03   // Low Limit Register [R/W]
#define TS04 0x04   // Critical Limit Register [R/W]
#define TS05 0x05   // Temperature Data Register [RO]
#define TS06 0x06   // Manufacturer ID Register [RO]
#define TS07 0x07   // Device ID/Revision Register [RO]
#pragma endregion

// EEPROM data
#define DNC 0x00  // "Do not care" byte

// Device commands
#pragma region Command
#define READBYTE     'r' // Read
#define WRITEBYTE    'w' // Write byte
#define WRITEPAGE    'g' // Write page
#define SCANBUS      's' // Scan I2C bus
#define I2CCLOCK     'c' // I2C bus clock control
#define PROBEADDRESS 'a' // Address test
#define PINCONTROL   'p' // Pin control
#define RSWP         'b' // Reversible write protection control
#define PSWP         'l' // Permanent write protection control
#define NAME         'n' // Name control
#define GETVERSION   'v' // Get FW version
#define TESTCOMM     't' // Test communication
#define RSWPREPORT   'f' // Initial supported RSWP capabilities
#define RETESTRSWP   'e' // Reevaluate supported RSWP capabilities
#define DDR4DETECT   '4' // DDR4 detection test
#define DDR5DETECT   '5' // DDR5 detection test
#define FACTORYRESET '-' // Factory reset device settings
#pragma endregion

// Device pin names (SpdReaderWriterDll.Pin.Name class)
#define OFFLINE_MODE_SWITCH (uint8_t) 0 // Pin to toggle SPD5 offline mode
#define SA1_SWITCH          (uint8_t) 1 // Pin to toggle SA1 state
#define HIGH_VOLTAGE_SWITCH (uint8_t) 9 // Pin to toggle VHV on SA0 pin

// Pin states
#define ON             HIGH
#define OFF            LOW
#define ENABLE  (byte) 0x01
#define SET     (byte) 0x01
#define DISABLE (byte) 0x00
#define RESET   (byte) 0x00
#define GET     (char) '?'  // Suffix added to commands to return current state

// Device responses
#define SUCCESS  (byte) 0x01
#define ENABLED  (byte) 0x01
#define ACK      (byte) 0x01
#define ZERO     (byte) 0x00
#define DISABLED (byte) 0x00
#define NACK     (byte) 0xFF
#define ERROR    (byte) 0xFF
#define WELCOME  (char) '!'
#define UNKNOWN  (char) '?'

// Device name settings
#define NAMELENGTH 16
char deviceName[NAMELENGTH];

// Device settings
#define DEVICESETTINGS 0x20 // EEPROM location to store device settings
#define CLOCKMODE      0    // Bit position for I2C clock settings
#define FASTMODE       true
#define STDMODE        false

// Global variables
uint32_t i2cClock         = 100000L;          // Initial I2C clock
uint8_t rswpSupport       = 0;                // Bitmask representing RAM RSWP support
uint8_t eepromPageAddress = 0;                // Initial EEPROM page address
const int pins[] = { OFF_EN, SA1_EN, HV_EN }; // Configuration pins array

void setup() {

  // Config pin controls
  for (int i = 0; i <= sizeof(pins[0]); i++) {
    pinMode(pins[i], OUTPUT);
  }

  // HV controls
  pinMode(HV_EN, OUTPUT);
  pinMode(HV_FB, INPUT);

  // Initiate and join the I2C bus as a master
  Wire.begin();
   
  // Perform initial device RSWP support test
  rswpSupport = rswpSupportTest();

  // Check saved i2c clock and set mode accordingly
  if (getI2cClockMode()) {
    i2cClock = 400000;
  }   
  Wire.setClock(i2cClock);

  // Reset EEPROM page address
  setPageAddress(0);

  // Start serial data transmission
  PORT.begin(BAUD_RATE);
  PORT.setTimeout(100);  // Input timeout in ms

  // Wait for serial port connection or initialization
  while (!PORT) {}
  
  // Send a welcome byte when the device is ready
  cmdTest();
}

void loop() {
  // Wait for input data
  while (!PORT.available()) {}

  // Process input commands and data
  parseCommand();

  // Clear port buffer
  PORT.flush();
}

// Process input commands and data
void parseCommand() {
  if (!PORT.available()) {
    return;
  }

  char cmd = (char) PORT.read();

  switch (cmd) {

    // Read byte
    case READBYTE: 
      cmdRead();
      break;

    // Write byte
    case WRITEBYTE: 
      cmdWrite();
      break;
      
    // Write page
    case WRITEPAGE:
      cmdWritePage();
      break;

    // Scan i2c bus for addresses
    case SCANBUS: 
      cmdScanBus();
      break;

    // Probe if i2c address is valid
    case PROBEADDRESS: 
      cmdProbeBusAddress();
      break;

    case I2CCLOCK: 
      cmdI2CClock();
      break;

    // Control digital pins
    case PINCONTROL: 
      cmdPinControl();
      break;

    // RSWP controls
    case RSWP: 
      cmdRSWP();
      break;

    // PSWP controls
    case PSWP: 
      cmdPSWP();
      break;

    // Get Firmware version
    case GETVERSION: 
      cmdVersion();
      break;

    // Device Communication Test
    case TESTCOMM: 
      cmdTest();
      break;

    // Report supported RAM RSWP capabilities
    case RSWPREPORT: 
      cmdRswpReport();
      break;

    // Re-evaluate device's RSWP capabilities
    case RETESTRSWP: 
      cmdRetestRswp();
      break;

    // DDR4 detection test
    case DDR4DETECT: 
      cmdDdr4Detect();
      break;

    // DDR5 detection test
    case DDR5DETECT: 
      cmdDdr5Detect();
      break;

    // Device name controls
    case NAME: 
      cmdName();
      break;

    // Factory defaults restore
    case FACTORYRESET:
      cmdFactoryReset();
      break; 
  }
}

/*  -=  Command handlers  =-  */

void cmdRead() {

  // Data buffer
  byte buffer[4];
  PORT.readBytes(buffer, sizeof(buffer));

  // EEPROM address
  uint8_t address = buffer[0];
  // Offset address
  uint16_t offset = buffer[1] << 8 | buffer[2];
  // Byte count
  uint8_t length = buffer[3];

  // Data buffer
  byte data[length];
  // Fill the data buffer
  readByte(address, offset, length, data);

  for (int i = 0; i < length; i++) {
    PORT.write(data[i]);
  }
}

void cmdWrite() {

  // Data buffer
  byte buffer[4];
  PORT.readBytes(buffer, sizeof(buffer));

  // EEPROM address
  uint8_t address = buffer[0];
  // Offset address
  uint16_t offset = buffer[1] << 8 | buffer[2];
  // Byte value
  byte data = buffer[3];

  PORT.write(writeByte(address, offset, data) ? SUCCESS : ERROR);
}

void cmdWritePage() {
    
  // Data buffer
  byte buffer[4];
  PORT.readBytes(buffer, sizeof(buffer));
  
  // EEPROM address
  uint8_t address = buffer[0];
  // Offset address
  uint16_t offset = buffer[1] << 8 | buffer[2];
  // Bytes count
  uint8_t length = buffer[3];
  
  if (length == 0) {
    PORT.write(ERROR);
    return;
  }
  
  // Data buffer
  byte data[length];
  PORT.readBytes(data, sizeof(data));
  
  // Maximum page size is 16 bytes
  if (length > 16) {
    PORT.write(ERROR);
    return;
  }
  
  PORT.write(writePage(address, offset, length, data) ? SUCCESS : ERROR);
}

void cmdScanBus() {
  PORT.write(scanBus());
}

void cmdTest() {
  #ifdef __AVR__
  PORT.write(WELCOME);
  #else
  PORT.write(UNKNOWN);
  #endif
}

void cmdRswpReport() {
  PORT.write(rswpSupport);
}

void cmdRetestRswp() {
  PORT.write(rswpSupportTest());
}

void cmdDdr4Detect() {
  // Data buffer for i2c address
  byte buffer[1];
  PORT.readBytes(buffer, sizeof(buffer));

  uint8_t address = buffer[0]; // i2c address
  PORT.write(ddr4Detect(address) ? SUCCESS : ERROR);
}

void cmdDdr5Detect() {
  // Data buffer for i2c address
  byte buffer[1];
  PORT.readBytes(buffer, sizeof(buffer));

  uint8_t address = buffer[0]; // i2c address
  PORT.write(ddr5Detect(address) ? SUCCESS : ERROR);
}

void cmdVersion() {
  PORT.print(VERSION);
}

void cmdName() {

  // Data buffer for command byte
  byte buffer[1];
  PORT.readBytes(buffer, sizeof(buffer));

  // Get name
  if (buffer[0] == GET) {
    String deviceName = getName();
    PORT.print(deviceName);
    // Pad the response with \0's
    if (deviceName.length() < NAMELENGTH) {
      for (int i = deviceName.length(); i < NAMELENGTH; i++) {
        PORT.write(ZERO);
      }
    }
  }
  // Set name
  else if (buffer[0] > 0 && buffer[0] <= NAMELENGTH) {
    // prepare name buffer
    byte name[buffer[0] + 1];
    // read name and put it into buffer
    PORT.readBytes(name, buffer[0]);
    // set last byte to \0 where the string ends
    name[buffer[0]] = ZERO;

    PORT.write(setName(name) ? SUCCESS : ERROR);
  }
  // Invalid command
  else {
    PORT.write(UNKNOWN);
  }
}

void cmdProbeBusAddress() {

  // Data buffer for address
  byte buffer[1];
  PORT.readBytes(buffer, sizeof(buffer));

  uint8_t address = buffer[0]; // i2c address
  PORT.write(probeBusAddress(address) ? SUCCESS : ERROR);
}

void cmdI2CClock() {

  // Data buffer for clock mode
  byte buffer[1];
  PORT.readBytes(buffer, sizeof(buffer));

  // Set I2C clock
  if (buffer[0] == FASTMODE || buffer[0] == STDMODE) {
    setI2cClockMode(buffer[0]);
    PORT.write(getI2cClockMode() == buffer[0] ? SUCCESS : ERROR);
  }
  // Get current I2C clock
  else if (buffer[0] == GET) {        
    PORT.write(getI2cClockMode());
  }
  // Unrecognized command
  else {
    PORT.write(UNKNOWN);
  }  
}

bool cmdFactoryReset() {
  PORT.write(factoryReset() ? SUCCESS : ERROR);
}

void cmdRSWP() {

  // Data buffer
  byte buffer[2];
  PORT.readBytes(buffer, sizeof(buffer));

  // Block number
  uint8_t block = buffer[0];
  // Block state
  char state = buffer[1];

  // enable RSWP
  if (state == ENABLE) {
    PORT.write(setRswp(block) ? SUCCESS : ERROR);
  }
  // clear RSWP (all blocks)
  else if (state == DISABLE) {
    PORT.write(clearRswp() ? SUCCESS : ERROR);
  }
  // get RSWP status
  else if (state == GET) {
    PORT.write(getRswp(block) ? ENABLED : DISABLED);
  }
  // unrecognized RSWP command
  else {
    PORT.write(UNKNOWN);
  }
}

void cmdPSWP() {

  // Data buffer
  byte buffer[2];
  PORT.readBytes(buffer, sizeof(buffer));

  // EEPROM address
  uint8_t address = buffer[0];
  // PSWP state
  char state = buffer[1];

  // enable PSWP
  if (state == ENABLE) {
    PORT.write(setPswp(address) ? SUCCESS : ERROR);
  }
  // read PSWP
  else if (state == GET) {
    PORT.write(getPswp(address) ? ENABLED : DISABLED);
  }
  // unknown state
  else {
    PORT.write(UNKNOWN);
  }
}

void cmdPinControl() {

  // Data buffer
  byte buffer[2];
  PORT.readBytes(buffer, sizeof(buffer));

  // Pin number
  uint8_t pin = buffer[0];
  // Pin state
  char state = buffer[1];

  // DDR5 Offline mode controls
  if (pin == OFFLINE_MODE_SWITCH) {
    // toggle Offline mode state
    if (state == ENABLE || state == DISABLE) {
      PORT.write(ddr5SetOfflineMode(state) ? SUCCESS : ERROR);
    }
    // get Offline mode state
    else if (state == GET) {
      PORT.write(ddr5GetOfflineMode() ? ON : OFF);
    }
    // Unknown state
    else {
      PORT.write(ERROR);
    }
  }
  // SA1 controls
  else if (pin == SA1_SWITCH) {
    // Toggle SA1 state
    if (state == ENABLE || state == DISABLE) {
      setConfigPin(pins[pin], state);
      PORT.write(getConfigPin(pins[pin]) == state ? SUCCESS : ERROR);
    }
    // Get SA1 state
    else if (state == GET) {
      PORT.write(getConfigPin(pins[pin]) == ON ? ON : OFF);
    }
    // Unknown state
    else {
      PORT.write(UNKNOWN);
    }
  }
  // VHV 9V controls
  else if (pin == HIGH_VOLTAGE_SWITCH) {
    // Toggle HV state
    if (state == ENABLE || state == DISABLE) {
      PORT.write(setHighVoltage(state) ? SUCCESS : ERROR);
    }
    // Get HV state
    else if (state == GET) {
      PORT.write(getHighVoltage() ? ON : OFF);
    }
    // Unknown state
    else {
      PORT.write(ERROR);
    }
  }
  // Unknown pin
  else {
    PORT.write(UNKNOWN);
  }
}

/*  -=  Read/Write functions  =-  */

// Reads bytes into data buffer
void readByte(uint8_t deviceAddress, uint16_t offset, uint8_t length, byte *data) {

  if ((deviceAddress >= 80 || deviceAddress <= 87)) {
    adjustPageAddress(deviceAddress, offset);
  }

  Wire.beginTransmission(deviceAddress);
  Wire.write((uint8_t)(offset));
  Wire.endTransmission();
  Wire.requestFrom(deviceAddress, length);

  if (Wire.available() < length) {
    for (uint8_t i = 0; i < length; i++) {
      data[i] = ERROR;
    }
    return;
  }

  // Fill data buffer
  for (uint8_t i = 0; i < length; i++) {
    while (!Wire.available()) {}
    data[i] = Wire.read();
  }
}

// Writes a byte
bool writeByte(uint8_t deviceAddress, uint16_t offset, byte data) {

  if (deviceAddress >= 80 || deviceAddress <= 87) {
    adjustPageAddress(deviceAddress, offset);
  }

  Wire.beginTransmission(deviceAddress);
  Wire.write((uint8_t)(offset));
  Wire.write(data);
  uint8_t status = Wire.endTransmission();

  delay(10);

  return status == 0;  // TODO: writing to PSWP-protected area returns true
}

// Writes a page
bool writePage(uint8_t deviceAddress, uint16_t offset, uint8_t length, byte *data) {
  
  if (deviceAddress >= 80 || deviceAddress <= 87) {
    adjustPageAddress(deviceAddress, offset);
  }
 
  Wire.beginTransmission(deviceAddress);
  Wire.write((uint8_t)(offset));
  for (uint8_t i = 0; i < length; i++) {
    Wire.write(data[i]);
  }  
  uint8_t status = Wire.endTransmission();
  
  delay(10);
  
  return status == 0;
}


/*  -=  RSWP functions  =-  */

// Sets reversible write protection on specified block
bool setRswp(uint8_t block) {

  byte commands[] = { SWP0, SWP1, SWP2, SWP3 };
  byte cmd = (block > 0 || block <= 3) ? commands[block] : commands[0];

  bool ddr4 = ddr4Detect();
  bool result;

  if (setHighVoltage(ON)) {
    setConfigPin(SA1_EN, OFF); // Required for pre-DDR4
    if (block > 0 && !ddr4) {      
      result = false;
    }
    else {
      result = probeDeviceTypeId(cmd);
    }
    resetPins();

    return result;
  }
  
  return false;
}

// Reads reversible write protection status
bool getRswp(uint8_t block) {

  byte commands[] = { RPS0, RPS1, RPS2, RPS3 };
  byte cmd = (block > 0 || block <= 3) ? commands[block] : commands[0];

  // Jedec EE1002(A), TSE2002av compliance  
  if (block == 0 && !ddr4Detect()) {
    setHighVoltage(ON);
  }

  bool status = probeDeviceTypeId(cmd); // true/ack = not protected

  resetPins();

  return !status; // true = protected or rswp not supported; false = unprotected
}

// Clears reversible software write protection
bool clearRswp() {

  if (setHighVoltage(ON)) {
    setConfigPin(SA1_EN, ON); // Required for pre-DDR4
    bool result = probeDeviceTypeId(CWP);
    resetPins();

    return result;
  }

  return false;
}

// Test RSWP support capabilities
byte rswpSupportTest() {

  // Reset supported RAM value
  rswpSupport = 0;

  // Reset config pins and HV state
  resetPins();

  // Scan i2c bus
  if (!scanBus()) {
    // No I2C devices
    return ZERO;
  }

  // Slow down I2C bus clock for accurate results 
    if (getI2cClockMode()) {
      Wire.setClock(100000);
    }

  // RSWP DDR5 test
  if (ddr5SetOfflineMode(ON)) {
    rswpSupport |= DDR5;
  }

  // RSWP VHV test
  if (setHighVoltage(ON)) {
    rswpSupport |= DDR4;

    // RSWP SA1 test
    if ((setConfigPin(SA1_EN, ON) && setConfigPin(SA1_EN, OFF)) && 
        (setConfigPin(SA1_EN, ON)  ^ scanBus()) != 
        (setConfigPin(SA1_EN, OFF) ^ scanBus())) {
      rswpSupport |= DDR3;
    }
  }

  resetPins();

  // Restore I2C clock
  setI2cClockMode(getI2cClockMode());
  
  return rswpSupport;
}


/*  -=  High Voltage (9V) functions  =-  */

// Controls HV source (set state to ON to turn on, or OFF to turn off)
bool setHighVoltage(bool state) {

  digitalWrite(HV_EN, state);
  delay(25);

  // Return operation result
  return getHighVoltage() == state;
}

// Returns HV status by reading HV_FB
bool getHighVoltage() {
  return getConfigPin(HV_EN) && getConfigPin(HV_FB);
}


/*  -=  PSWP functions  =-  */

// Sets permanent write protection on supported EEPROMs
bool setPswp(uint8_t deviceAddress) {

  if (ddr4Detect(deviceAddress) || ddr5Detect(deviceAddress)) {
    return false;
  }

  // Keep address bits (SA0-SA2) intact and change bits 7-4 to '0110'
  uint8_t cmd = (deviceAddress & 0b111) | (PWPB << 3);

  Wire.beginTransmission(cmd);
  // Write 2 DNC bytes to force LSB to set to 0
  Wire.write(DNC);
  Wire.write(DNC);
  int status = Wire.endTransmission();

  return status == 0;

  //uint8_t cmd = (deviceAddress & 0b111) << 1 | (PWPB << 4);
  //return probeDeviceTypeId(cmd << 1);
}

// Read permanent write protection status
bool getPswp(uint8_t deviceAddress) {

  // Keep address bits (SA0-SA2) intact and change bits 7-4 to '0110'
  uint8_t cmd = (deviceAddress & 0b111) | (PWPB << 3);

  Wire.beginTransmission(cmd);
  // Write 1 DNC byte to force LSB to set to 1
  Wire.write(DNC);
  int status = Wire.endTransmission();
  
  return status == 0;  // returns true if PSWP is not set

  //uint8_t cmd = (deviceAddress & 0b111) << 1 | (PWPB << 4);
  //return probeDeviceTypeId(cmd << 1 | 1);
}


/*  -=  EEPROM Page functions  =-  */

// Get active DDR4 page address
uint8_t getPageAddress(bool lowLevel = false) {

  if (!lowLevel) {
    return eepromPageAddress;
  }

//#ifdef __AVR__

  uint8_t status = ERROR;

  // Send start condition
  TWCR = _BV(TWEN) | _BV(TWINT) | _BV(TWEA) | _BV(TWSTA);

  // Wait for TWINT flag set
  while (!(TWCR & (_BV(TWINT)))) {}

  // Wait for start
  while ((TWSR & 0xF8) != 0x08) {}

  // Load RPA command into data register
  TWDR = RPA;

  // Transmit address
  TWCR = _BV(TWEN) | _BV(TWEA) | _BV(TWINT);

  // Wait to transmit address
  while (!(TWCR & (_BV(TWINT)))) {}

  // Check status (0x40 = ACK = page 0; 0x48 = NACK = page 1)
  status = (TWSR & 0xF8);

  // Write 2xDNC after control byte
  if (status == 0x40) {
    for (int i = 0; i < 2; i++) {
      TWDR = DNC;
      TWCR = _BV(TWEN) | _BV(TWEA) | _BV(TWINT);
      while (!(TWCR & (_BV(TWINT)))) {}
    }
  }

  // Send stop condition
  TWCR = _BV(TWEN) | _BV(TWINT) | _BV(TWEA) | _BV(TWSTO);

  // Return the result
  switch (status) {
    case 0x40: return 0;
    case 0x48: return 1;
    default: return status;
  }

//#endif

  // Non-AVR response
  return ERROR;
}

// Sets page address to access lower or upper 256 bytes of DDR4 SPD
bool setPageAddress(uint8_t pageNumber) {

  if (pageNumber < 2) {
    probeDeviceTypeId((pageNumber == 0) ? SPA0 : SPA1);
    eepromPageAddress = pageNumber;
    return true;
  }
  return false;
}

// Adjusts page address according to byte offset specified
void adjustPageAddress(uint8_t address, uint16_t offset) {

  uint8_t page;

  // Assume DDR4 is present
  if (offset <= 256) {
    page = offset >> 8;  // DDR4 page
    if (getPageAddress() != page) {
      setPageAddress(page);
    }
  }

  // Check if DDR5 is present and adjust page number and addressing mode
  if (ddr5Detect(address)) {
    // Enable 1-byte addresing mode
    setLegacyModeAddress(address, false);

    // Write page address to MR11[2:0]
    page = offset >> 7;  // DDR5 page

    writeByte((MEMREG & address), (uint8_t)(MR11), (byte)(page));
    // TODO: TBT
  }
}

// I2C Legacy Mode Device Configuration
void setLegacyModeAddress(uint8_t address, bool twoByteAddressing) {

  writeByte((MEMREG & address), (uint8_t)(MR11), (byte)(twoByteAddressing ? (1 << 3) : 0));

  // TODO: TBT
}


/*  -=  Device settings functions =-  */

// Assign a new name
bool setName(String name) {

  for (uint8_t i = 0; i < name.length(); i++) {
    EEPROM.update(i, name[i]);
  }
  EEPROM.update(name.length(), ZERO);

  return name == getName();
}

// Get device's name
String getName() {

  char deviceNameChar[NAMELENGTH + 1];

  for (uint8_t i = 0; i < NAMELENGTH; i++) {
    deviceNameChar[i] = EEPROM.read(i);
  }
  // set last byte to zero
  deviceNameChar[NAMELENGTH] = ZERO;

  return deviceNameChar;
}

// Read device settings
bool getSettings(byte name) {
  return bitRead(EEPROM.read(DEVICESETTINGS), name);
}

// Save device settings
bool saveSettings(byte name, byte value) {

  byte currentSettings = EEPROM.read(DEVICESETTINGS);
  EEPROM.update(DEVICESETTINGS, bitWrite(currentSettings, name, value));
}


/*  -=  I2C bus functions  =-  */

// Set I2C bus clock mode
bool setI2cClockMode(bool mode) {
  saveSettings(CLOCKMODE, mode ? FASTMODE : STDMODE);
  i2cClock = mode ? 400000 : 100000;
  Wire.setClock(i2cClock);

  return getI2cClockMode() == mode;
}

// Gets saved I2C clock mode (true=fast mode, false=std mode)
bool getI2cClockMode() {
  return getSettings(CLOCKMODE);
}

// Scans I2C bus range 80-87
byte scanBus() {

  byte response = ZERO;

  for (uint8_t i = 0; i <= 7; i++) {
    if (probeBusAddress(i + 80)) {
      response |= ((byte)1 << i);
    }
  }

  return response;
}

// Control config pins
bool setConfigPin(uint8_t pin, bool state) {
  digitalWrite(pin, state);
  if (pin == SA1_SWITCH) {
    delay(5);
  }

  return getConfigPin(pin) == state;
}

// Get config pin state
bool getConfigPin(uint8_t pin) {
  // SA1 state check
  if (pin == SA1_SWITCH) {
    byte _a1 = 0b11001100; // valid addresses bitmask when SA1 is high: 82-83, 86-87
    return (digitalRead(pin) ? ((scanBus() & _a1)) : (scanBus() & ~_a1));
  }

  return digitalRead(pin);
}

// Reset config pins
void resetPins() {
  for (int i = 0; i <= sizeof(pins[0]); i++) {
    setConfigPin(pins[i], OFF);
  }  
}

// Toggle DDR5 offline mode
bool ddr5SetOfflineMode(bool state) {
  setConfigPin(OFF_EN, state);  
  if (state) {
    // Set SDR-DDR4 to address 82-83 to avoid conflicts
    setConfigPin(SA1_EN, state);
  }  

  return ddr5GetOfflineMode() == state;
}

bool ddr5GetOfflineMode() {

  // TODO: read MR48:2
  return false;
}

// Tests if device address is present on I2C bus
bool probeBusAddress(uint8_t address) {
  Wire.beginTransmission(address);
  return Wire.endTransmission() == 0;
}

// Tests if device select code returns ACK (true), or NACK (false)
bool probeDeviceTypeId(uint8_t deviceSelectCode) {

  uint8_t status = 0;

  // Check the LSB of DSC, if it is 0 (write), then we need to write DNC address + DNC data
  bool writeBit = (deviceSelectCode & 1) == 0;

  // Wire library uses 7 bit address, so we strip the LSB from the DSC by bitshifting right by 1
  uint8_t cmd = deviceSelectCode >> 1;

  Wire.beginTransmission(cmd);
  if (writeBit) {
    Wire.write(DNC);
    Wire.write(DNC);
  }
  status = Wire.endTransmission();

  if (writeBit) {
    return status == 0;
  }

  return Wire.requestFrom(cmd, (uint8_t)1) > 0; // true when ACK is received after control byte
}

// DDR4 detection test (address)
bool ddr4Detect(uint8_t address) {
  if (address == 0) {
    return ddr4Detect();
  }
  
  return probeBusAddress(address) && ddr4Detect();
}

// DDR4 detection test (generic)
bool ddr4Detect() {
  // Only SPA0 is tested, RPA returns NACK after SPA1 regardless of RAM type
  return setPageAddress(0) && getPageAddress(true) == 0;
}

// DDR5 detection test
bool ddr5Detect(uint8_t address) {

  if (!probeBusAddress(address) || !scanBus()) {
    return false;
  }

  bool result = false;

  // TODO: return true if MR0 is 0x51 or 0x52

  return result;
}

// Restores device's default settings
bool factoryReset() {
  for (uint8_t i = 0; i <= 32; i++) {
    EEPROM.update(i, ZERO);
  }
  for (uint8_t i = 0; i <= 32; i++) {
    if (EEPROM.read(i) != ZERO) {
      return false;
    }
  }
  return true;
}
