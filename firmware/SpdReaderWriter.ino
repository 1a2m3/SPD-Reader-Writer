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

#define VERSION 20211104  // Version number (YYYYMMDD)

// RAM types
#define DDR2 (1 << 2)
#define DDR3 (1 << 3)
#define DDR4 (1 << 4)
#define DDR5 (1 << 5)

// SPD5 hub registers
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

// EEPROM page commands
#define SPA0 0x6C  // Set EE Page Address to 0 (addresses  00h to  FFh) (  0-255) (DDR4)
#define SPA1 0x6E  // Set EE Page Address to 1 (addresses 100h to 1FFh) (256-511) (DDR4)
#define RPA  0x6D  // Read EE Page Address                                        (DDR4)

// EEPROM RSWP commands
#define RPS0 0x63  // Read SWP0 status         (addresses  00h to  7Fh) (  0-127) (DDR4/DDR3/DDR2)
#define RPS1 0x69  // Read SWP1 status         (addresses  80h to  FFh) (128-255) (DDR4)
#define RPS2 0x6B  // Read SWP2 status         (addresses 100h to 17Fh) (256-383) (DDR4)
#define RPS3 0x61  // Read SWP3 status         (addresses 180h to 1FFh) (384-511) (DDR4)

#define SWP0 0x62  // Set RSWP for block 0     (addresses  00h to  7Fh) (  0-127) (DDR4/DDR3/DDR2)
#define SWP1 0x68  // Set RSWP for block 1     (addresses  80h to  FFh) (128-255) (DDR4)
#define SWP2 0x6A  // Set RSWP for block 2     (addresses 100h to 17Fh) (256-383) (DDR4)
#define SWP3 0x60  // Set RSWP for block 3     (addresses 180h to 1FFh) (384-511) (DDR4)

#define CWP  0x66  // Clear RSWP                                                  (DDR4/DDR3/DDR2)

// EEPROM PSWP commands
#define PWPB 0b0110  // PSWP Device Type Identifier Control Code (bits 7-4)       (DDR3/DDR2)

// EEPROM temperature sensor register commands
#define TSRB 0b0011 // Device select code to access Temperature Sensor registers (bits 7-4)
#define TS00 0x00   // Capability Register [RO]
#define TS01 0x01   // Configuration Register [R/W]
#define TS02 0x02   // High Limit Register [R/W]
#define TS03 0x03   // Low Limit Register [R/W]
#define TS04 0x04   // Critical Limit Register [R/W]
#define TS05 0x05   // Temperature Data Register [RO]
#define TS06 0x06   // Manufacturer ID Register [RO]
#define TS07 0x07   // Device ID/Revision Register [RO]

// EEPROM data
#define DNC 0x00  // "Do not care" byte

// Device commands
#define READBYTE     'r' // Read
#define WRITEBYTE    'w' // Write byte
#define WRITEPAGE    'g' // Write page (16 bytes)
#define SCANBUS      's' // Scan I2C bus
#define PROBEADDRESS 'a' // Address test
#define PINCONTROL   'p' // Pin control
#define RSWP         'b' // Reversible write protection control
#define PSWP         'l' // Permanent write protection control
#define NAME         'n' // Name control
#define GETVERSION   'v' // Get FW version
#define TEST         't' // Test communication
#define FEATURES     'f' // Supported RAM bitmask report
#define DDR4DETECT   '4' // DDR4 detection test
#define DDR5DETECT   '5' // DDR5 detection test
#define RETEST       'e' // Reevaluate RSWP capabilities

// Device control pins
#define OFFLINE_MODE_SWITCH 0 // Pin to toggle SPD5 offline mode
#define SA1_SWITCH          1 // Pin to toggle SA1 state
#define HIGH_VOLTAGE_SWITCH 9 // Pin to toggle VHV on SA0 pin

// Device responses
#define SUCCESS (byte) 0x01
#define ERROR   (byte) 0xFF
#define ZERO    (byte) 0x00
#define WELCOME (char) '!'

// Pin states
#define ON      HIGH
#define OFF     LOW
#define ENABLE  1
#define DISABLE 0
//#define DEFAULT 0
#define GET    '?'  // Suffix added to commands to return current state

// Device name settings
#define NAMELENGTH 16
char deviceName[NAMELENGTH];

// Global variables
uint8_t ramSupport;                     // Bitmask representing supported RAM
uint8_t eepromPageAddress = 0;          // Initial EEPROM page address
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
  // Set I2C clock frequency
  Wire.setClock(I2C_CLOCK);

  // Perform device features test
  ramSupport = selfTest();

  // Reset EEPROM page address
  setPageAddress(0);

  // Start serial data transmission
  PORT.begin(BAUD_RATE);
  PORT.setTimeout(100);  // Input timeout in ms

  // Wait for serial port connection
  while (!PORT);

  // Send a welcome byte when the device is ready
  PORT.write(WELCOME);
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

  char cmd = PORT.read();

  switch (cmd) {

    // Read byte
    case READBYTE: cmdRead();
      break;

    // Write byte
    case WRITEBYTE: cmdWrite();
      break;
      
    // Write page
    case WRITEPAGE: cmdWritePage();
      break;

    // Scan i2c bus for addresses
    case SCANBUS: cmdScanBus();
      break;

    // Probe if i2c address is valid
    case PROBEADDRESS: cmdProbeBusAddress();
      break;

    // Control digital pins
    case PINCONTROL: cmdPinControl();
      break;

    // RSWP controls
    case RSWP: cmdRSWP();
      break;

    // PSWP controls
    case PSWP: cmdPSWP();
      break;

    // Get Firmware version
    case GETVERSION: cmdVersion();
      break;

    // Device Communication Test
    case TEST: cmdTest();
      break;

    // Report supported RAM RSWP capabilities
    case FEATURES: cmdFeaturesReport();
      break;

    // Re-evaluate device's RSWP capabilities
    case RETEST: cmdRetest();
      break;

    // DDR4 detection test
    case DDR4DETECT: cmdDdr4Detect();
      break;

    // DDR5 detection test
    case DDR5DETECT: cmdDdr5Detect();
      break;

    // Device name controls
    case NAME: cmdName();
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
  PORT.write(WELCOME);
}

void cmdFeaturesReport() {
  PORT.write(ramSupport);
}

void cmdRetest() {
  PORT.write(selfTest());
}

void cmdDdr4Detect() {
  // Data buffer for address
  byte buffer[1];
  PORT.readBytes(buffer, sizeof(buffer));

  uint8_t address = buffer[0]; // i2c address
  PORT.write(ddr4Detect(address) ? SUCCESS : ERROR);
}

void cmdDdr5Detect() {
  // Data buffer for address
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
    PORT.write(ERROR);
  }
}

void cmdProbeBusAddress() {

  // Data buffer for address
  byte buffer[1];
  PORT.readBytes(buffer, sizeof(buffer));

  uint8_t address = buffer[0]; // i2c address
  PORT.write(probeBusAddress(address) ? SUCCESS : ERROR);
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
    PORT.write(setWriteProtection(block) ? SUCCESS : ERROR);
  }
  // clear RSWP (block number is ignored)
  else if (state == DISABLE) {
    PORT.write(clearWriteProtection() ? SUCCESS : ERROR);
  }
  // get RSWP status
  else if (state == GET) {
    PORT.write(getWriteProtection(block) ? SUCCESS : ERROR);
  }
  // unrecognized RSWP command
  else {
    PORT.write(ERROR);
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
    PORT.write(setPermanentWriteProtection(address) ? SUCCESS : ERROR);
  }
  // read PSWP
  else if (state == GET) {
    PORT.write(readPermanentWriteProtection(address) ? SUCCESS : ERROR);
  }
  // unknown state
  else {
    PORT.write(ERROR);
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
      PORT.write(digitalRead(pins[pin]) == state ? SUCCESS : ERROR);
    }
    // Get SA1 state
    else if (state == GET) {
      PORT.write(getConfigPin(pins[pin]) == ON ? ON : OFF);
    }
    // Unknown state
    else {
      PORT.write(ERROR);
    }
  }
  // VHV 9V controls
  else if (pin == HIGH_VOLTAGE_SWITCH) {
    // Toggle HV state
    if (state == 0 || state == 1) {
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
    PORT.write(ERROR);
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

// Writes a page (16 bytes)
bool writePage(uint8_t deviceAddress, uint16_t offset, uint8_t length, byte * data) {
  
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
bool setWriteProtection(uint8_t block) {

  byte commands[] = { SWP0, SWP1, SWP2, SWP3 };
  byte cmd = (block > 0 || block <= 3) ? commands[block] : commands[0];

  setHighVoltage(ON);
  bool result = probeDeviceTypeId(cmd);
  setHighVoltage(OFF);

  return result;
}

// Reads reversible write protection status
bool getWriteProtection(uint8_t block) {

  byte commands[] = { RPS0, RPS1, RPS2, RPS3 };
  byte cmd = (block > 0 || block <= 3) ? commands[block] : commands[0];

  setHighVoltage(OFF);

  return probeDeviceTypeId(cmd);  // true = unprotected; false = protected or rswp not supported
}

// Clears reversible software write protection
bool clearWriteProtection() {

  setHighVoltage(ON);
  bool result = probeDeviceTypeId(CWP);
  setHighVoltage(OFF);

  return result;
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
  return digitalRead(HV_EN) && digitalRead(HV_FB);
}


/*  -=  PSWP functions  =-  */

// Sets permanent write protection on supported EEPROMs
bool setPermanentWriteProtection(uint8_t deviceAddress) {

  if (ddr4Detect(deviceAddress) || ddr5Detect(deviceAddress)) {
    return ERROR;
  }

  // Keep address bits (SA0-SA2) intact and change bits 7-4 to '0110'
  uint8_t cmd = (deviceAddress & 0b111) | (PWPB << 3);

  Wire.beginTransmission(cmd);
  // Write 2 DNC bytes to force LSB to set to 0
  Wire.write(DNC);
  Wire.write(DNC);
  int status = Wire.endTransmission();
  delay(10);

  return status == 0;

  //uint8_t cmd = (deviceAddress & 0b111) << 1 | (PWPB << 4);
  //return probeDeviceTypeId(cmd << 1);
}

// Read permanent write protection status
bool readPermanentWriteProtection(uint8_t deviceAddress) {

  // Keep address bits (SA0-SA2) intact and change bits 7-4 to '0110'
  uint8_t cmd = (deviceAddress & 0b111) | (PWPB << 3);

  Wire.beginTransmission(cmd);
  // Write 1 DNC byte to force LSB to set to 1
  Wire.write(DNC);
  int status = Wire.endTransmission();
  delay(10);

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

#ifdef __AVR__

  uint8_t status = 0;

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

  // Write 2xDNC after ACK. If status is NACK (0x48), stop and return 1
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
    default: return ERROR;
  }

#endif

  // Non-AVR response
  return ERROR;
}

// Sets page address to access lower or upper 256 bytes of DDR4 SPD
void setPageAddress(uint8_t pageNumber) {

  if (pageNumber < 2) {
    probeDeviceTypeId((pageNumber == 0) ? SPA0 : SPA1);
    eepromPageAddress = pageNumber;
  }
}

// Adjusts page address according to byte offset specified
void adjustPageAddress(uint8_t address, uint16_t offset) {

  // Assume DDR4 is present, do not call ddr4Detect() for performance reasons
  if (offset < 512) {
    // Get offset MSB to see if it is below 0x100 or above 0xFF
    uint8_t page = offset >> 8;  // DDR4 page
    if (getPageAddress() != page) {
      setPageAddress(page);
    }
  }

  // Check if DDR5 is present and adjust page number and addressing mode
  if (ddr5Detect(address)) {
    // Enable 1-byte addresing mode
    setLegacyModeAddress(address, false);

    // Write page address to MR11[2:0]
    uint8_t page = offset >> 7;  // DDR5 page

    writeByte((MEMREG & address), (uint8_t)(MR11), (byte)(page));
    // TODO: TBT
  }
}

// I2C Legacy Mode Device Configuration
void setLegacyModeAddress(uint8_t address, bool twoByteAddressing) {

  writeByte((MEMREG & address), (uint8_t)(MR11), (byte)(twoByteAddressing ? (1 << 3) : 0));

  // TODO: TBT
}


/*  -=  Device name functions =-  */

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


/*  -=  I2C bus functions  =-  */

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

// Control slave address pins
bool setConfigPin(uint8_t pin, bool state) {
  digitalWrite(pin, state);
  return digitalRead(pin) == state;
}

// Get slave address pin state
bool getConfigPin(uint8_t pin) {
  return digitalRead(pin);
}

// Reset SA and HV pins
void resetPins() {
  for (int i = 0; i <= sizeof(pins[0]); i++) {
    setConfigPin(pins[i], OFF);
  }
  setHighVoltage(OFF);
}

// Toggle DDR5 offline mode
bool ddr5SetOfflineMode(bool state) {
  digitalWrite(OFF_EN, state);
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

// DDR4 detection test
bool ddr4Detect(uint8_t address) {

  if (!probeBusAddress(address) || !scanBus()) {
    return false;
  }

  bool result = true;
  bool avrCpu = true;
  byte eePage = 128;

  for (uint8_t i = 0; i <= 1; i++) {
    setPageAddress(i);
    eePage = getPageAddress(true);
    if (eePage == ERROR) { // unsupported hardware, switch to alternative methods
      avrCpu = false;
      break;
    }
    if (eePage != i && eePage != ERROR ) {
      result = false;
      break;
    }
  }

  if (avrCpu) {
    return result;
  }

  // Alternative methods for non-AVR controllers

  result = false; // presume DDR4 is not present

  // Read protection status of blocks 1-3, if at least one is unprotected, return true.
  for (uint8_t i = 1; i <= 3; i++) {
    if (getWriteProtection(i)) {
      return true;
    }
  }

  // Check for TS registers, if present, return true.
  if (probeBusAddress(TSRB << 3 | address & 0b111)) {
    return true;
  }

  // Read data from 2 pages, if contents don't match, return true.
  byte page0[16];
  byte page1[16];

  for (uint16_t i = 0; i <= 256; i += 16) {

    readByte(address, i,       16, page0); // Read page 0
    readByte(address, i + 256, 16, page1); // Read page 1

    for (uint8_t j = 0; j < 16; j++) {
      if (page0[j] != page1[j]) {
        return true;
      }
    }
  }

  return result;
}

// DDR5 detetion
bool ddr5Detect(uint8_t address) {

  if (!probeBusAddress(address) || !scanBus()) {
    return false;
  }

  bool result = false;

  // TODO: return true if MR0 is 0x51 or 0x52

  return result;
}

// Perform basic device feature test and report capabilities
byte selfTest() {

  // Reset supported RAM value
  ramSupport = 0;

  // Reset config pins and HV state
  resetPins();

  // Scan i2c bus
  if (!scanBus()) {
    // No I2C devices
    return ZERO;
  }

  // RSWP DDR5 test
  if (ddr5SetOfflineMode(ON)) {
    ramSupport |= DDR5;
  }

  // RSWP VHV test
  if (setHighVoltage(ON)) {
    ramSupport |= DDR4;

    // RSWP SA1 test
    if ((setConfigPin(SA1_EN, ON) ^ scanBus()) != (setConfigPin(SA1_EN, OFF) ^ scanBus())) {
      ramSupport |= DDR3 | DDR2;
    }
  }

  resetPins();

  return ramSupport;
}
