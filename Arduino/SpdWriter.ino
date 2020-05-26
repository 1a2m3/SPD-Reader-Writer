/*
   Arduino based DDR4 SPD reader/writer
*/

#include <Wire.h>
#define SERIAL_BAUD_RATE 115200

// EEPROM commands
#define SPA0 0x36 // Set EE Page Address to 0
#define SPA1 0x37 // Set EE Page Address to 1

// RSWP commands
#define SWP0 0x31  // Set Write Protection for block 0 (addresses  00h to  7Fh) (  0-127)
#define SWP1 0x34  // Set Write Protection for block 1 (addresses  80h to  FFh) (128-255)
#define SWP2 0x35  // Set Write Protection for block 2 (addresses 100h to 17Fh) (256-383)  
#define SWP3 0x30  // Set Write Protection for block 3 (addresses 180h to 1FFh) (384-511)
#define CWP  0x33  // Clear Write Protection for all 4 blocks

#define OPTCO 13   // Optocoupler switch pin

int eeAddress = 0; // EE Page address

void setup() {

  pinMode(OPTCO, OUTPUT);

  Wire.begin();
  Serial.begin(SERIAL_BAUD_RATE);
  Serial.setTimeout(10000); // 10 sec. timeout

  setPageAddress(eeAddress); // Reset eeprom page address

  while (!Serial) {} // Wait for serial monitor
}

// Reverses software write protection
bool clearWriteProtection() {

  digitalWrite(OPTCO, HIGH); // Turn on the optocoupler
  delay(10);

  Wire.beginTransmission(CWP);
  Wire.write(0x00); // "Dummy" writes
  Wire.write(0x00); // Works without them, but PDFs say these are needed
  int status = Wire.endTransmission();
  delay(10);

  digitalWrite(OPTCO, LOW); // Turn off the optocoupler

  if (status == 0) {
    return true;
  }
  return false;
}

// Enables write protection on specified block
bool setWriteProtection(uint8_t block) {

  int cmd;

  switch (block) {
    case 0:
      cmd = SWP0;
      break;
    case 1:
      cmd = SWP1;
      break;
    case 2:
      cmd = SWP2;
      break;
    case 3:
      cmd = SWP3;
      break;
    default:
      cmd = SWP0;
      block = 0;
      break;
  }

  digitalWrite(OPTCO, HIGH); // Turn on the optocoupler
  delay(10);

  Wire.beginTransmission(cmd);
  Wire.write(0x00); // "Dummy" writes
  Wire.write(0x00); // Works without them, but PDFs say these are needed
  int status = Wire.endTransmission();
  delay(10);

  digitalWrite(OPTCO, LOW); // Turn off the optocoupler

  if (status == 0) { // SWP enabled
    return true;
  }
  return false;
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
  
  if (status == 0) {
    return true;
  }
  return false;
}

// Sets page address to access lower or upper 256 bytes of DDR4 SPD
void setPageAddress(uint8_t pageAddress) {
  // command doesn't use the select address, all devices on the I2C bus will act simultaneously
  Wire.beginTransmission((pageAddress == 0) ? SPA0 : SPA1);
  delay(5);
  Wire.endTransmission();

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
  if (Wire.endTransmission() == 0) {
    return true;
  }
  return false;
}

//Command handlers

void cmdScan() {
  int startAddress = Serial.parseInt(); //First address
  int endAddress = Serial.parseInt();   //Last address

  if (startAddress > endAddress) {
    Serial.write(0);
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
      Serial.write((byte)i & 0xFF);
    }
  }
  Serial.write(0); // Send 0 to prevent application from waiting in case no devices are present
  return;
}

void cmdRead() {
  int address = Serial.parseInt(); // Device address
  int offset = Serial.parseInt();  // Offset address

  Serial.write(readByte(address, offset));
  return;
}

void cmdWrite() {
  int address = Serial.parseInt(); // Device address
  int offset = Serial.parseInt();  // Offset address
  byte data = Serial.parseInt();   // Byte value

  if (writeByte(address, offset, data)) {
    Serial.write(0); // Success
    return;
  }

  Serial.write(1); // Error
  return;
}

void cmdTest() {
  Serial.write('!');
  return;
}

void cmdProbe() {
  int address = Serial.parseInt(); // Device address

  if (probe(address)) {
    Serial.write(address); // Success
    return;
  }
  Serial.write(0); // Error
  return;
}

void cmdClearWP() {

  if (clearWriteProtection()) {
    Serial.write(0); // Success
    //Serial.println("WP cleared");
    return;
  }
  Serial.write(1); // Error
  //Serial.println("WP NOT cleared");
  return;
}

void cmdEnableWP() {

  int block = Serial.parseInt(); // Block number
  block = (block >= 0 && block <= 3) ? block : 0; // Block number can't be outside of 0-3 range

  if (setWriteProtection(block)) {
    //Serial.print("Protection enabled on block ");
    //Serial.println(block, HEX);
    Serial.write(0); // Success
    return;
  }

  //Serial.print("Nothing changed with block ");
  //Serial.println(block, HEX);
  Serial.write(1); // Error
  return;
}

void parseCommand() {
  if (!Serial.available()) {
    return;
  }

  char cmd = Serial.read();

  switch (cmd) {
    case 't': cmdTest();      // Device Communication Test
      break;
    case 's': cmdScan();      // Scan i2c bus
      break;
    case 'r': cmdRead();      // Read byte
      break;
    case 'w': cmdWrite();     // Write byte
      break;
    case 'p': cmdProbe();     // Probe i2c address
      break;
    case 'c': cmdClearWP();   // Clear reversible software write protection
      break;
    case 'e': cmdEnableWP();  // Enable write protection
      break;
  }
}

void loop() {
  parseCommand();
}
