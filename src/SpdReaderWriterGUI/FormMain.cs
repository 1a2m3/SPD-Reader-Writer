using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using SpdReaderWriterDll;

namespace SpdReaderWriterGUI {

	public partial class formMain : Form {

		const int PROCESS_WM_READ = 0x0010;

		[DllImport("kernel32.dll")]
		public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

		[DllImport("kernel32.dll")]
		public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

		public formMain() {
			InitializeComponent();
		}

		// Create a device instance
		public Device MySpdReader;

		// Spd contents
		public byte[] SpdContents = new byte[0];

		// Loaded file
		public string currentFileName = "";

		// Strings of COM port names and EEPROM addresses found
		Stack<string> foundEeproms = new Stack<string>();
		private string[] foundDevices;
		private int foundDevicesCount = 0;

		// CRC status
		private bool crcValidChecksum;

		// Screenshot template
		Bitmap png;

		// RAM type
		private RamType rt;

		// Command line arguments
		string[] args = Environment.GetCommandLineArgs();

		private void FormMain_Load(object sender, EventArgs e) {

			Logger("Program started");

			SetStyle(ControlStyles.DoubleBuffer, true);

			populateDevices(this, new EventArgs());

			// Prepare screenshot 
			png = new Bitmap(Size.Width, Size.Height, CreateGraphics());

			// Fill top offset line
			byte[] _offsetTable = new byte[16];
			for (int i = 0; i < 16; i++) {
				_offsetTable[i] = (byte)i;
			}
			labelTopOffsetHex.Text = BinToHex(_offsetTable);

			// Open file dragged onto executable
			if (args.Length == 2 && File.Exists(args[1])) {
				openFile(args[1], null);
			}
		}

		private bool validateCrc(byte[] input) {

			if (input.Length < (int) SpdSize.DDR_SPD_SIZE) {
				return false;
			}

			byte[] _correctedSpd = fixCrc(input);

			for (int i = 0; i < input.Length; i++) {
				if (input[i] != _correctedSpd[i]) {
					return false;
				}
			}

			return true;
		}

		private byte[] fixCrc(byte[] input) {

			byte[] _spd = new byte[input.Length];

			for (int i = 0; i < _spd.Length; i++) {
				_spd[i] = input[i];
			}

			int[] headerStart = new int[0];
			int headerLength = 0;
			int crcPosition = 0;

			// Get DDR4 CRC data
			if (Eeprom.GetRamType(input) == RamType.DDR4) {
				// Read 126 bytes from headersStart positions
				headerStart = new [] { 0, 128 };
				headerLength = 126;
				crcPosition = 126;
			}

			// Get DDR3 CRC data
			if (Eeprom.GetRamType(input) == RamType.DDR3) {
				headerStart = new [] { 0 };
				// Exclude SN from CRC?
				headerLength = ((input[0] >> 7) == 1) ? 117 : 126;
			}

			// Calculate DDR3 and DDR4 CRC
			if (Eeprom.GetRamType(input) == RamType.DDR4 || Eeprom.GetRamType(input) == RamType.DDR3) {
				foreach (int _headerStart in headerStart) {
					byte[] header = new byte[headerLength];
					for (int i = 0; i < headerLength; i++) {
						header[i] = _spd[i + _headerStart];
					}

					ushort crc = Eeprom.Crc16(header);

					byte CrcLsb = (byte)(crc & 0xff);
					byte CrcMsb = (byte)(crc >> 8);

					if (_spd[_headerStart + crcPosition + 0] != CrcLsb || // MSB
						_spd[_headerStart + crcPosition + 1] != CrcMsb) { //LSB

						_spd[_headerStart + crcPosition + 0] = CrcLsb;
						_spd[_headerStart + crcPosition + 1] = CrcMsb;
					}
				}
			}

			// Calculate SDR - DDR2 CRC
			else if (Eeprom.GetRamType(input) != RamType.UNKNOWN) {
				//headerStart = new [] { 0 };
				headerLength = 63;
				crcPosition = 64;
				byte[] header = new byte[headerLength];

				for (int i = 0; i < headerLength; i++) {
					header[i] += _spd[i];
				}

				byte crc = (byte)Eeprom.Crc(header);

				if (_spd[crcPosition - 1] != crc) {
					_spd[crcPosition - 1] = crc;
				}
			}

			return _spd;
		}

		private void fixCrcMenuItem_Click(object sender, EventArgs e) {

			if (crcValidChecksum) {
				MessageBox.Show("The CRC checksum is valid", "CRC information", MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return;
			}

			SpdContents = fixCrc(SpdContents);
			crcValidChecksum = validateCrc(SpdContents);

			string _message, _messageAdd = "";
			MessageBoxIcon _icon;

			if (crcValidChecksum) {
				displayContents(SpdContents);
				_message = "CRC has been corrected";
				_messageAdd = "\n\nYou may now save the SPD or write it";
				_icon = MessageBoxIcon.Information;
			}
			else {
				_message = "CRC could not be corrected";
				_icon = MessageBoxIcon.Error;
			}

			Logger(_message);
			MessageBox.Show($"{_message}{_messageAdd}", "CRC information", MessageBoxButtons.OK, _icon);
		}

		private void populateDevices(object sender, EventArgs e) {

			foundDevices = null;
			foundDevicesCount = 0;

			// Find Ports and addresses
			string[] _port = Device.Find();
			if (_port.Length > 0) {
				for (int i = 0; i < _port.Length; i++) {
					Device _device = new Device(_port[i]);
					foreach (int _address in _device.Scan()) {
						string _deviceId = $"{_port[i]}:{_address}";
						foundEeproms.Push(_deviceId);
						foundDevicesCount++;
						Logger($"Found available device at {_deviceId}");
					}
					_device.Disconnect();
				}
				foundDevices = new string[foundDevicesCount];
			}

			if (foundDevices != null) {
				for (int i = foundDevices.Length - 1; i >= 0; i--) {
					foundDevices[i] = foundEeproms.Pop();
				}
			}

			// Remove existing old items, except for connected devices
			ToolStripItem[] _existingDeviceItem = toolStripDeviceButton.DropDownItems.Find("FoundDevicePortAddress", false);

			if (_existingDeviceItem.Length > 0) {
				for (int i = 0; i < _existingDeviceItem.Length; i++) {
					if (!((ToolStripMenuItem)_existingDeviceItem[i]).Checked) {
						toolStripDeviceButton.DropDownItems.Remove(_existingDeviceItem[i]);
					}
				}
			}
			//toolStripDeviceButton.DropDownItems.RemoveByKey("FoundDevicePortAddress");


			// Add found devices to toolbar menu
			if (foundDevices != null && foundDevices.Length > 0) {
				for (int i = 0; i < foundDevices.Length; i++) {
					string _label = foundDevices[i];
					ToolStripMenuItem _item = new ToolStripMenuItem(_label, null, connectToDevice);
					_item.Name = "FoundDevicePortAddress";
					toolStripDeviceButton.DropDownItems.Add(_item);
				}

				toolStripDeviceSeparator.Visible = true;
			}
			else {
				// Hide separator
				toolStripDeviceSeparator.Visible = false;
				Logger("No available devices found");
			}
		}

		private void Logger(string message) {
			if (message.Length > 0) {
				String timeStamp = (DateTime.Now).ToLocalTime().ToString();
				loggerBox.Items.Add($"{timeStamp}: {message}");
				//loggerBox.SelectionMode = SelectionMode.One;
				loggerBox.SelectedIndex = loggerBox.Items.Count - 1;
				//loggerBox.SelectedIndex = -1;
				//loggerBox.SelectionMode = SelectionMode.None;
			}
		}

		private void connectToDevice(object sender, EventArgs e) {

			ToolStripMenuItem _sender = (ToolStripMenuItem)sender;

			string _port = _sender.Text.Split(':')[0];
			int _address = Int32.Parse(_sender.Text.Split(':')[1]);

			// Disconnect
			if (MySpdReader != null && MySpdReader.IsConnected) {
				if (MySpdReader.Disconnect()) {
					Logger($"{MySpdReader.PortName}:{MySpdReader.EepromAddress} disconnected");
				}

				if (!MySpdReader.IsConnected) {
					// Uncheck all devices
					ToolStripItem[] _existingDeviceItem = toolStripDeviceButton.DropDownItems.Find("FoundDevicePortAddress", false);

					if (_existingDeviceItem.Length > 0) {
						foreach (ToolStripMenuItem toolStripItem in _existingDeviceItem) {
							toolStripItem.Checked = false;
						}
					}
					//_sender.Checked = false;
				}

				// Do not reconnect if checked item was clicked
				if (MySpdReader.PortName == _port && MySpdReader.EepromAddress == _address) {
					return;
				}
			}

			// Connect
			if (MySpdReader == null || !MySpdReader.IsConnected) {
				MySpdReader = new Device(_port, _address);
				MySpdReader.SpdSize = Eeprom.GetSpdSize(MySpdReader);
				if (MySpdReader.Connect()) {
					_sender.Checked = true;
					rt = Eeprom.GetRamType(MySpdReader);
					Logger($"{_port}:{_address} connected ({rt})");
				}
			}
		}

		private void readSpdFromEeprom(object sender, EventArgs e) {

			int start = Environment.TickCount;

			// Clear viewer
			//displayContents(new byte[0]);

			SpdContents = new byte[(int)MySpdReader.SpdSize];

			statusProgressBar.Value = 0;
			statusProgressBar.Maximum = SpdContents.Length;

			// Read each byte to display status in the progress bar
			for (int i = 0; i < SpdContents.Length; i++) {
				SpdContents[i] = Eeprom.ReadByte(MySpdReader, i);
				statusProgressBar.Value = i + 1;
				Application.DoEvents();
			}

			int stop = Environment.TickCount;

			if (SpdContents.Length > 0) {
				// Reset file name 
				currentFileName = "";
				displayContents(SpdContents);
				crcValidChecksum = validateCrc(SpdContents);
				tabPageMain.Text = $"{MySpdReader.PortName}:{MySpdReader.EepromAddress}";
				if (tabControlMain.SelectedTab != tabPageMain) {
					tabPageMain.Text = $"* {tabPageMain.Text}";
				}
			}

			Logger($"Read SPD ({SpdContents.Length} bytes) from {MySpdReader.PortName}:{MySpdReader.EepromAddress} in {stop - start} ms");
		}


		private void clearWriteProtection(object sender, EventArgs e) {

			if (Eeprom.ClearWriteProtection(MySpdReader)) {
				Logger($"Write protection is cleared on {MySpdReader.PortName}:{MySpdReader.EepromAddress}");
			}
			else {
				Logger($"Could not clear write protection on {MySpdReader.PortName}:{MySpdReader.EepromAddress}");
			}
		}

		private string BinToText(byte[] input) {

			int charsPerRow = 16;
			string output = "";

			for (int i = 0; i < input.Length; i++) {

				// Add a new line, except before the first row
				if (i % charsPerRow == 0 && i > 0) {
					output += Environment.NewLine;
				}

				output += (input[i] >= 0x20 && input[i] <= 0x7E) || (input[i] >= 0xA0 && input[i] <= 0xFF)
					// Display an ASCII character
					? ((char)input[i]).ToString()
					// Display a middle dot for all other characters
					: "·";
			}

			return output;
		}

		private string BinToHex(byte[] input, int bytesPerRow = 16, int bytesGroup = 4) {

			string output = "";

			// Print contents
			for (int i = 0; i < input.Length; i++) {

				// Place new line break, but not before the first line
				if ((i & bytesPerRow - 1) == 0 && i > 0) {
					output += Environment.NewLine;
				}

				// Print byte value
				output += ($"{input[i]:X2}");

				// Print blank space between each byte, but not at the end of the line
				if (i % bytesPerRow != bytesPerRow - 1) {
					output += " ";
				}

				// Add blank space after group of bytes in each line
				if ((i + 1 & 0x0F) % bytesGroup == 0 && i % bytesPerRow != bytesPerRow - 1) {
					output += " ";
				}
			}

			return output;
		}

		private void saveToFile(object sender, EventArgs e) {

			if (SpdContents.Length == 0) {
				Logger("No SPD loaded");
				return;
			}

			SaveFileDialog saveFileDialog   = new SaveFileDialog();
			saveFileDialog.Filter           = "Binary files (*.bin)|*.bin|SPD Dumps (*.spd)|*.spd|All files (*.*)|*.*";
			saveFileDialog.FilterIndex      = 0;
			saveFileDialog.RestoreDirectory = true;
			saveFileDialog.FileName         = Eeprom.GetModuleModelName(SpdContents);

			if (saveFileDialog.ShowDialog() == DialogResult.OK && saveFileDialog.FileName != "") {
				currentFileName = saveFileDialog.FileName;
				File.WriteAllBytes(currentFileName, SpdContents);
				Logger($"Saved {SpdContents.Length} byte(s) to '{currentFileName}'");
			}
		}

		private void writeSpdToEeprom(object sender, EventArgs e) {

			DialogResult _writeConfirmation = MessageBox.Show(
				"Are you sure you want to write SPD to EEPROM?\n\nThis process is irreversible and you will not be able to restore old SPD, unless you have a backup copy.",
				"Write",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Exclamation,
				MessageBoxDefaultButton.Button2);

			if (_writeConfirmation == DialogResult.No) {
				return;
			}

			Logger("Write started");

			// Warn if CRC is invalid
			if (!crcValidChecksum) {
				DialogResult _crcConfirmation = MessageBox.Show(
					"The loaded SPD CRC is not valid!\n\nAre you sure you want to write this SPD?",
					"Warning",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Warning);
				if (_crcConfirmation == DialogResult.No) {
					Logger("Invalid CRC - write aborted by user");
					return;
				}
			}

			int start = Environment.TickCount;

			statusProgressBar.Value = 0;

			if (SpdContents.Length > 0) {

				statusProgressBar.Maximum = SpdContents.Length;

				for (int i = 0; i < SpdContents.Length; i++) {

					if (!Eeprom.UpdateByte(MySpdReader, i, SpdContents[i])) {
						string message = $"Could not write to offset 0x{i:X3} at {MySpdReader.PortName}:{MySpdReader.EepromAddress}";
						Logger(message);

						string _question = "Do you want to clear write protection?\n\nClick 'Retry' to attempt to disable write protection and try again or 'Ignore' to continue.";
						DialogResult _result = MessageBox.Show($"{message}\n\n{_question}", "Error", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Warning);
						// Yes button pressed
						if (_result == DialogResult.Retry) {
							Logger("Attempting to clear write protection");
							clearWriteProtection(this, null);
							Thread.Sleep(100);
							i--; // Decrement the value of i to retry writing the same byte after WP clear is attempted
							continue;
						}

						// Abort button pressed
						if (_result == DialogResult.Abort) {
							Logger("Could not write to EEPROM - write aborted by user");
							statusProgressBar.Value = statusProgressBar.Maximum;
							return;
						}
					}

					// Redraw interface every N bytes written
					if (i % 16 == 0) {
						statusProgressBar.Value = i;
						Application.DoEvents();
					}
				}
				statusProgressBar.Value = statusProgressBar.Maximum;

				if (currentFileName != "") {
					// Log filename if file was written to EEPROM
					Logger($"Written {currentFileName} ({SpdContents.Length} bytes) to {MySpdReader.PortName}:{MySpdReader.EepromAddress} in {Environment.TickCount - start} ms");
				}
				else {
					Logger($"Written {SpdContents.Length} byte(s) to {MySpdReader.PortName}:{MySpdReader.EepromAddress} in {Environment.TickCount - start} ms");
				}
			}
		}

		private void openFile(object sender, EventArgs e) {

			string path = "";

			// A fix to prevent opening file "Open" if it exists in the same directory where the binary is located
			if (sender.GetType() == typeof(String)) {
				path = sender.ToString();
			}

			if (!File.Exists(path)) {
				var inputFilePath = new OpenFileDialog();

				inputFilePath.Filter = "Binary files (*.bin)|*.bin|SPD Dumps (*.spd)|*.spd|All files (*.*)|*.*";
				inputFilePath.FilterIndex = 0;
				inputFilePath.RestoreDirectory = true;

				if (inputFilePath.ShowDialog() == DialogResult.OK && inputFilePath.FileName != "") {
					path = inputFilePath.FileName;
				}
				else {
					return;
				}
			}

			byte[] _SpdContents = File.ReadAllBytes(path);
			if (_SpdContents.Length > 1024) {
				string _errorMessage = "File \"{0}\" not opened, too big";
				Logger(String.Format(_errorMessage, path));
				MessageBox.Show(String.Format(_errorMessage, Path.GetFileName(path)), path, MessageBoxButtons.OK, MessageBoxIcon.Error);
				//currentFileName = "";
				//SpdContents = new byte[0];
				return;
			}
			currentFileName = path;

			if (currentFileName != "" && tabPageMain.Text != Path.GetFileName(currentFileName)) {
				tabPageMain.Text = Path.GetFileName(currentFileName);
			}

			SpdContents = _SpdContents;
			displayContents(SpdContents);
			statusProgressBar.Value = statusProgressBar.Maximum;
			crcValidChecksum = validateCrc(SpdContents);
			Logger($"Opened file '{currentFileName}'");
		}

		private void displayContents(byte[] input) {

			int bytesPerRow = 16;

			// Vertical offsets
			int vOffsetCount = input.Length / bytesPerRow;

			sideOffsets.Text = "";

			for (int i = 0; i < input.Length; i++) {
				if (i % bytesPerRow == 0 || i == 0) {
					sideOffsets.Text += $"{i:X3}";
					sideOffsets.Text += Environment.NewLine;
				}
			}

			// HEX values
			textBoxHex.Text = BinToHex(input, bytesPerRow);
			// ASCII values
			textBoxAscii.Text = BinToText(input);

			// Switch to Data view tab
			//tabControl1.SelectTab(tabPageMain);

			// Highlight tabPageMain if the tab is not active
			if (tabControlMain.SelectedTab != tabPageMain) {
				tabPageMain.ForeColor = Color.Blue; // Doesn't work, when DrawMode is set to Normal
				tabPageMain.Text = $"* {tabPageMain.Text}";
			}
		}


		private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {
			formAbout about = new formAbout();
			about.ShowDialog();
		}


		private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
			if (File.Exists(currentFileName)) {
				File.WriteAllBytes(currentFileName, SpdContents);
				Logger($"File '{currentFileName}' saved");
			}
			else {
				saveToFile(this, null);
			}
		}

		private void timerInterfaceUpdater_Tick(object sender, EventArgs e) {

			// Enable or disable EEPROM toolbar buttons and menus depending on device state
			bool _deviceConnectionEstablished   = MySpdReader != null && MySpdReader.IsConnected;
			bool _eepromWriteable               = _deviceConnectionEstablished && SpdContents.Length != 0;
			bool _progressBarActive             = statusProgressBar.Value == statusProgressBar.Minimum || statusProgressBar.Value == statusProgressBar.Maximum;
			readEeprom_button.Enabled           = _deviceConnectionEstablished && _progressBarActive;
			readToolStripMenuItem.Enabled       = _deviceConnectionEstablished && _progressBarActive;
			disconnectToolStripMenuItem.Enabled = _deviceConnectionEstablished;
			testToolStripMenuItem.Enabled       = _deviceConnectionEstablished;
			clearToolStripMenuItem.Enabled      = _deviceConnectionEstablished && _progressBarActive && rt == RamType.DDR4;
			enableToolStripMenuItem.Enabled     = _deviceConnectionEstablished && _progressBarActive && rt == RamType.DDR4;
			enableRswpButton.Enabled            = _deviceConnectionEstablished && _progressBarActive && rt == RamType.DDR4;
			clearRswpButton.Enabled             = _deviceConnectionEstablished && _progressBarActive && rt == RamType.DDR4;
			writeEeprom_button.Enabled          = _eepromWriteable && _progressBarActive;
			writeToolStripMenuItem.Enabled      = _eepromWriteable && _progressBarActive;
			refreshToolStripMenuItem.Enabled    = !_deviceConnectionEstablished;

			// Enable or disable file operations
			bool _spdLoaded                  = SpdContents.Length != 0;
			toolSaveFile_button.Enabled      = _spdLoaded;
			//crcDropdownMenu.Enabled        = toolSaveFile_button.Enabled;
			saveToolStripMenuItem.Enabled    = _spdLoaded;
			saveasToolStripMenuItem.Enabled  = _spdLoaded;
			copyHexToolStripMenuItem.Enabled = _spdLoaded;

			// CRC status
			if (_spdLoaded && (Eeprom.GetRamType(SpdContents) != RamType.UNKNOWN)) {
				statusBarCrcStatus.Visible = statusProgressBar.Value == statusProgressBar.Maximum;
				statusBarCrcStatus.Text         = (crcValidChecksum) ? "CRC OK" : "CRC Error";
				statusBarCrcStatus.ForeColor    = (crcValidChecksum) ? Color.FromArgb(128, 255, 128) : Color.White;
				statusBarCrcStatus.BackColor    = (crcValidChecksum) ? Color.FromArgb(255, 0, 64, 0) : Color.FromArgb(192, 255, 0, 0);
				fixCrcToolStripMenuItem.Enabled = !crcValidChecksum && _spdLoaded;
			}
			else {
				// Hide CRC status for non DDR4 RAM
				statusBarCrcStatus.Visible = false;
			}

			// RAM type
			if (_spdLoaded) {
				toolStripStatusRamType.Text = $"{Eeprom.GetRamType(SpdContents)}"; //, {Eeprom.GetModuleModelName(SpdContents)}
				toolStripStatusRamType.Visible = true;
			}

			// Status progress bar (hide when value is 0 or maximum)
			//statusProgressBar.Visible = (statusProgressBar.Value > 0 && statusProgressBar.Value < statusProgressBar.Maximum);
			statusProgressBar.Visible = !_progressBarActive;

			// Connection Status
			statusBarConnectionStatus.Enabled   = _deviceConnectionEstablished;
			statusBarConnectionStatus.ForeColor = (_deviceConnectionEstablished) ? Color.Navy : SystemColors.Control;
			statusBarConnectionStatus.Text      = (_deviceConnectionEstablished) ? $"Connected to {MySpdReader.PortName}:{MySpdReader.EepromAddress}" : "Not connected";
			toolStripDeviceButton.Text          = (_deviceConnectionEstablished) ? $"{MySpdReader.PortName}:{MySpdReader.EepromAddress}" : "Device";

			// Toolbar device button
			toolStripDeviceButton.Text        = (_deviceConnectionEstablished) ? $"{MySpdReader.PortName}:{MySpdReader.EepromAddress}" : "Device";
			toolStripDeviceButton.ToolTipText = (_deviceConnectionEstablished) ? $"Connected to {MySpdReader.PortName}:{MySpdReader.EepromAddress}" : "Select device port and address";

			// Split container looks
			splitContainerViewer.Panel1.MinimumSize = labelTopOffsetHex.GetPreferredSize(new Size());
			if (splitContainerViewer.SplitterDistance < splitContainerViewer.Panel1.MinimumSize.Width) {
				splitContainerViewer.SplitterDistance = splitContainerViewer.Panel1.MinimumSize.Width;
			}
			splitContainerViewer.Panel2.MinimumSize = labelTopOffsetAscii.GetPreferredSize(new Size());
			if (splitContainerViewer.SplitterDistance > splitContainerViewer.Panel1.MinimumSize.Width + splitContainerViewer.Panel2.MinimumSize.Width) {
				splitContainerViewer.SplitterDistance = splitContainerViewer.Panel1.MinimumSize.Width + splitContainerViewer.Panel2.MinimumSize.Width;
			}

			// Main tab label
			if (currentFileName != "" && tabPageMain.Text != Path.GetFileName(currentFileName)) {
				//tabPageMain.Text = Path.GetFileName(currentFileName);
			}

			// Main tab color
			//if (tabControlMain.SelectedTab == tabPageMain) {
			//	tabPageMain.ForeColor = SystemColors.ControlText;
			//}

			// Disable Save log button if log is empty
			buttonSaveLog.Enabled = loggerBox.Items.Count > 0;

			// ASCII textbox size to match ascii offset box width
			//Size asciiBoxSize = textBoxAscii.Size;
			//asciiBoxSize.Width = labelTopOffsetAscii.Size.Width;
		}

		private void enableWriteProtectionMenuItem_Click(object sender, EventArgs e) {

			DialogResult _protectConfirmation = MessageBox.Show(
				"You won't be able to write to EEPROM again until the protection is cleared.\n\nAre you sure you want to enable write protection?\n\n",
				"Write protection",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Exclamation,
				MessageBoxDefaultButton.Button2);

			if (_protectConfirmation == DialogResult.No) {
				return;
			}

			for (int i = 0; i <= 3; i++) {
				if (Eeprom.SetWriteProtection(MySpdReader, i)) {
					Logger($"Write protection is set on block {i}");
				}
				else {
					Logger($"Could not set write protection on block {i}");
				}
			}
		}

		private void disconnectToolStripMenuItem_Click(object sender, EventArgs e) {
			if (MySpdReader != null && MySpdReader.IsConnected) {
				if (MySpdReader.Disconnect()) {
					Logger($"{MySpdReader.PortName}:{MySpdReader.EepromAddress} disconnected");

					// Remove existing old items
					ToolStripItem[] _tollbarDevices = toolStripDeviceButton.DropDownItems.Find("FoundDevicePortAddress", true);
					foreach (ToolStripMenuItem item in _tollbarDevices) {
						if (item.Checked) {
							item.Checked = false;
						}
					}
				}
			}
		}

		private void testToolStripMenuItem_Click(object sender, EventArgs e) {
			if (MySpdReader != null && MySpdReader.IsConnected) {
				string _message;
				MessageBoxIcon _icon;
				if (MySpdReader.Test()) {
					_message = $"Device {MySpdReader.PortName}:{MySpdReader.EepromAddress} is OK";
					_icon = MessageBoxIcon.Information;
				}
				else {
					_message = $"Device {MySpdReader.PortName}:{MySpdReader.EepromAddress} does not respond";
					_icon = MessageBoxIcon.Exclamation;
				}
				Logger(_message);
				MessageBox.Show(_message, "Test", MessageBoxButtons.OK, _icon);
			}
		}

		private void copyHexMenuItem_Click(object sender, EventArgs e) {

			// Remove blank spaces and new lines
			Clipboard.SetText(textBoxHex.Text.Replace(" ", String.Empty).Replace(Environment.NewLine, String.Empty));
		}
		

		private void buttonClearLog_Click(object sender, EventArgs e) {
			while (loggerBox.Items.Count > 0) {
				loggerBox.Items.RemoveAt(0);
			}
		}

		private void buttonSaveLog_Click(object sender, EventArgs e) {
			if (loggerBox.Items.Count > 0) {
				SaveFileDialog saveFileDialog = new SaveFileDialog();
				saveFileDialog.Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log|All files (*.*)|*.*";
				saveFileDialog.FilterIndex = 0;
				saveFileDialog.RestoreDirectory = true;
				saveFileDialog.FileName = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}_{(DateTime.Now).Hour}{(DateTime.Now).Minute}{(DateTime.Now).Second}{(DateTime.Now).Millisecond}";

				if (saveFileDialog.ShowDialog() == DialogResult.OK && saveFileDialog.FileName != "") {
					string[] log = new string[loggerBox.Items.Count];

					for (int i = 0; i < log.Length; i++) {
						log[i] = loggerBox.Items[i].ToString();
					}

					File.WriteAllLines(saveFileDialog.FileName, log);
					Logger($"Log saved to file '{saveFileDialog.FileName}'");
				}
			}
		}

		private void websiteToolStripMenuItem_Click(object sender, EventArgs e) {
			Process.Start("https://github.com/1a2m3/");
		}

		private void toolStripButtonScreenshot_Click(object sender, EventArgs e) {

			SaveFileDialog _saveScreenshot = new SaveFileDialog();

			_saveScreenshot.Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*";
			_saveScreenshot.FilterIndex = 0;
			_saveScreenshot.RestoreDirectory = true;
			_saveScreenshot.FileName = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}_{(DateTime.Now).Hour}{(DateTime.Now).Minute}{(DateTime.Now).Second}{(DateTime.Now).Millisecond}";

			if (_saveScreenshot.ShowDialog() == DialogResult.OK && _saveScreenshot.FileName != "") {
				png.Save(_saveScreenshot.FileName, ImageFormat.Png);
				Logger($"Screenshot saved to file '{_saveScreenshot.FileName}'");
			}
		}


		// This function creates a screenshot the moment the mouse enters the 'screenshot' button, to prevent from screenshotting tooltip
		private void prepareScreenShot(object sender, EventArgs e) {
			//((ToolStripButton) sender).ToolTipText = "";
			Graphics.FromImage(png).CopyFromScreen(Location.X, Location.Y, 0, 0, Size);
		}

		private void formResized(object sender, EventArgs e) {
			// If the form got resized, change the size of screenshot bitmap
			png = new Bitmap(Size.Width, Size.Height, CreateGraphics());
		}

		private void donateViaPaypalToolStripMenuItem_Click(object sender, EventArgs e) {
			Process.Start("http://paypal.me/mik4rt3m");
		}

		private void statusBarCrcStatus_DoubleClick(object sender, EventArgs e) {
			if (!crcValidChecksum) {
				DialogResult _dr = MessageBox.Show("The CRC is not valid.\n\nDo you want to correct it?", "CRC status",
					MessageBoxButtons.YesNo, MessageBoxIcon.Question);
				if (_dr == DialogResult.Yes) {
					fixCrcMenuItem_Click(this, new EventArgs());
				}
			}
		}

		private void tabControlMain_SelectedIndexChanged(object sender, EventArgs e) {
			if (tabControlMain.SelectedTab == tabPageMain && tabPageMain.Text.StartsWith("* ")) {
				tabPageMain.Text = tabPageMain.Text.Replace("* ", "");
			}
		}

		private void importThaiphoonBurnerDumpToolStripMenuItem_Click(object sender, EventArgs e) {

		

			MessageBox.Show("Thaiphoon Burner not running.", sender.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}

		
	}
}
