/*
   Arduino based DDR4 SPD reader/writer
   https://github.com/1a2m3/
*/

#include <Wire.h>
#include "SpdReaderWriterSettings.h" // Settings

// EEPROM commands
#define SPA0 0x36   // Set EE Page Address to 0 (DDR4 only)
#define SPA1 0x37   // Set EE Page Address to 1 (DDR4 only)

// RSWP commands
#define SWP0 0x31   // Set Write Protection for block 0 (addresses  00h to  7Fh) (  0-127) (DDR4 only)
#define SWP1 0x34   // Set Write Protection for block 1 (addresses  80h to  FFh) (128-255) (DDR4 only)
#define SWP2 0x35   // Set Write Protection for block 2 (addresses 100h to 17Fh) (256-383) (DDR4 only)
#define SWP3 0x30   // Set Write Protection for block 3 (addresses 180h to 1FFh) (384-511) (DDR4 only)
#define CWP  0x33   // Clear Write Protection for all 4 blocks
#define DNC  0x00   // "Do not care" byte
#define PWPB 0b0110 // PSWP command bits (7-4)

int eeAddress = 0;  // Initial EEPROM page address

void setup() {

  pinMode(HVSW,  OUTPUT);
  pinMode(SA0SW, OUTPUT);
  pinMode(SA1SW, OUTPUT);
  pinMode(SA2SW, OUTPUT);

  Wire.begin();
  setPageAddress(eeAddress); // Reset eeprom page address

  PORT.begin(BAUD_RATE);
  PORT.setTimeout(10000); // 10 sec. timeout

  while (!PORT) {} // Wait for serial monitor
}

// Turns on or off address select pins
void setAddressPin(int pin, bool state) {
  digitalWrite(pin, state);
}

// Reverses software write protection
bool clearWriteProtection() {

  digitalWrite(HVSW, HIGH); // Turn on the optocoupler
  delay(10);

  Wire.beginTransmission(CWP);
  Wire.write(DNC);
  Wire.write(DNC);
  int status = Wire.endTransmission();
  delay(10);

  digitalWrite(HVSW, LOW); // Turn off the optocoupler

  return status == 0;
}

// Enables write protection on specified block
bool setWriteProtection(uint8_t block) {

  int commands[] = {SWP0, SWP1, SWP2, SWP3};
  int cmd = (block >= 0 || block <= 3) ? commands[block] : commands[0];

  digitalWrite(HVSW, HIGH); // Turn on the optocoupler
  delay(10);

  Wire.beginTransmission(cmd);
  Wire.write(DNC);
  Wire.write(DNC);
  int status = Wire.endTransmission(); // status == 0 when SWP is enabled
  delay(10);

  digitalWrite(HVSW, LOW); // Turn off the optocoupler

  return status == 0;
}

// Sets permanent write protection on supported EEPROMs
bool setPermanentWriteProtection(uint8_t deviceAddress) {

  uint8_t cmd = (byte)(deviceAddress & 0b111) + (PWPB << 4); // Keep address bits (SA0-SA2) intact and change bits 7-4 to '0110'
  Wire.beginTransmission(cmd); 
  Wire.write(DNC);
  Wire.write(DNC);
  int status = Wire.endTransmission();
  delay(10);

  return status == 0;
}

// Reads a byte
byte readByte(uint8_t deviceAddress, uint16_t offset) {

  adjustPageAddress(offset);

  Wire.beginTransmission(deviceAddress);
  Wire.write(offset);
  Wire.endTransmission();
  Wire.requestFrom(deviceAddress, 1);

  if (Wire.available()) {
    return Wire.read();
  }
}

// Writes a byte
bool writeByte(uint8_t deviceAddress, uint16_t offset, byte data) {

  adjustPageAddress(offset);

  Wire.beginTransmission(deviceAddress);
  Wire.write(offset);
  Wire.write(data);
  int status = Wire.endTransmission();

  delay(10); // This has to go after endTransmission, not before!

  return status == 0;
}

// Sets page address to access lower or upper 256 bytes of DDR4 SPD
void setPageAddress(uint8_t pageAddress) {
  // command doesn't use the select address, all devices on the I2C bus will act simultaneously
  Wire.beginTransmission((pageAddress == 0) ? SPA0 : SPA1);
  Wire.endTransmission();
  delay(5);

  eeAddress = pageAddress;
}

// Get currently selected page address
int getPageAddress() {
  return eeAddress;
}

// Adjusts page address according to byte offset specified
void adjustPageAddress(uint16_t offset) {
  if (offset <= 0xFF && getPageAddress() != 0) {
    setPageAddress(0);
  }
  if (offset > 0xFF && getPageAddress() != 1) {
    setPageAddress(1);
  }
}

// Tests I2C bus for connected devices
bool probe(uint8_t address) {
  Wire.beginTransmission(address);

  return Wire.endTransmission() == 0;
}

//Command handlers

void cmdScan() {
  int startAddress = PORT.parseInt(); //First address
  int endAddress   = PORT.parseInt(); //Last address

  if (startAddress > endAddress) {
    PORT.write((byte)0);
    return;
  }

  /*
    Limit scanning range from 0x50 (0b1010000) to 0x57 (0b1010111)
    because it causes the SPD EEPROM to switch pages once we  try to access devices 0x36 (SPA0) or 0x37 (SPA1)
    Also it can trigger write protection when devices 0x30 (SWP3), 0x31 (SWP0), 0x34 (SWP1), or 0x35 (SWP2) are accessed and SA0 is connected to HV source
  */

  startAddress = (startAddress < 0x50) ? 0x50 : startAddress;
  endAddress = (endAddress > 0x57) ? 0x57 : endAddress;

  for (int i = startAddress; i <= endAddress; i++) {
    if (probe(i)) {
      PORT.write((byte)i);
    }
  }
  PORT.write((byte)0); // Send 0 to prevent application from waiting in case no devices are present
}

void cmdRead() {
  int address = PORT.parseInt(); // Device address
  int offset  = PORT.parseInt(); // Offset address

  PORT.write((byte)readByte(address, offset));
}

void cmdWrite() {
  int address = PORT.parseInt(); // Device address
  int offset  = PORT.parseInt(); // Offset address
  byte data   = PORT.parseInt(); // Byte value

  if (writeByte(address, offset, data)) {
    PORT.write((byte)0); // Success
  }

  PORT.write((byte)1); // Error
}

void cmdTest() {
  PORT.write((byte)'!');
}

void cmdProbe() {
  int address = PORT.parseInt(); // Device address

  if (probe(address)) {
    PORT.write((byte)address); // Success
    return;
  }
  PORT.write((byte)0); // Error
}

void cmdClearWP() {

  if (clearWriteProtection()) {
    PORT.write((byte)0); // Success
    //PORT.println("WP cleared");
    return;
  }
  PORT.write((byte)1); // Error
  //PORT.println("WP NOT cleared");
}

void cmdEnableWP() {

  int block = PORT.parseInt(); // Block number
  block = (block >= 0 && block <= 3) ? block : 0; // Block number can't be outside of 0-3 range

  if (setWriteProtection(block)) {
    //PORT.print("Protection enabled on block ");
    //PORT.println(block, HEX);
    PORT.write((byte)0); // Success
    return;
  }

  //PORT.print("Nothing changed with block ");
  //PORT.println(block, HEX);
  PORT.write((byte)1); // Error
}

void cmdEnablePSWP() {
  int address = PORT.parseInt(); // Device address

  if (setPermanentWriteProtection(address)) {
    PORT.write((byte)0); // Success
    return;
  }
  PORT.write((byte)1); // Error
}

void cmdSetAddress() {
  int address = PORT.parseInt();

  setAddressPin(SA0SW, (address >> 0) & 0b1);
  setAddressPin(SA1SW, (address >> 1) & 0b1);
  setAddressPin(SA2SW, (address >> 2) & 0b1);

}

void parseCommand() {
  if (!PORT.available()) {
    return;
  }

  char cmd = PORT.read();

  switch (cmd) {
    case 't': cmdTest();       // Device Communication Test
      break;
    case 's': cmdScan();       // Scan i2c bus
      break;
    case 'r': cmdRead();       // Read byte
      break;
    case 'w': cmdWrite();      // Write byte
      break;
    case 'p': cmdProbe();      // Probe i2c address
      break;
    case 'c': cmdClearWP();    // Clear reversible software write protection
      break;
    case 'e': cmdEnableWP();   // Enable write protection
      break;
    case 'x': cmdEnablePSWP(); // Enable permanent write protection
      break;
    case 'a': cmdSetAddress(); // Set EEPROM address
      break;
  }
}

void loop() {
  parseCommand();
}
