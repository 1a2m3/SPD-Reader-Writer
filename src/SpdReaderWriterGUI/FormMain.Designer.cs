namespace SpdReaderWriterGUI {
	partial class formMain {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(formMain));
			this.splitContainerViewer = new System.Windows.Forms.SplitContainer();
			this.textBoxHex = new System.Windows.Forms.TextBox();
			this.labelTopOffsetHex = new System.Windows.Forms.Label();
			this.textBoxAscii = new System.Windows.Forms.TextBox();
			this.labelTopOffsetAscii = new System.Windows.Forms.Label();
			this.menuStripMain = new System.Windows.Forms.MenuStrip();
			this.fIleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveasToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.fixCrcToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyHexToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deviceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.testToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.disconnectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.eEPROMToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.readToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.writeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.enableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.websiteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.donateViaPaypalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.statusBarConnectionStatus = new System.Windows.Forms.ToolStripStatusLabel();
			this.statusBarCrcStatus = new System.Windows.Forms.ToolStripStatusLabel();
			this.statusProgressBar = new System.Windows.Forms.ToolStripProgressBar();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripMain = new System.Windows.Forms.ToolStrip();
			this.toolOpenFile = new System.Windows.Forms.ToolStripButton();
			this.toolSaveFile_button = new System.Windows.Forms.ToolStripButton();
			this.toolStripDeviceButton = new System.Windows.Forms.ToolStripDropDownButton();
			this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripDeviceSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.readEeprom_button = new System.Windows.Forms.ToolStripButton();
			this.writeEeprom_button = new System.Windows.Forms.ToolStripButton();
			this.clearRswpButton = new System.Windows.Forms.ToolStripButton();
			this.enableRswpButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripButtonScreenshot = new System.Windows.Forms.ToolStripButton();
			this.toolStripButtonAbout = new System.Windows.Forms.ToolStripButton();
			this.timerInterfaceUpdater = new System.Windows.Forms.Timer(this.components);
			this.loggerBox = new System.Windows.Forms.ListBox();
			this.sideOffsets = new System.Windows.Forms.TextBox();
			this.tabControlMain = new System.Windows.Forms.TabControl();
			this.tabPageMain = new System.Windows.Forms.TabPage();
			this.tabPageLog = new System.Windows.Forms.TabPage();
			this.buttonSaveLog = new System.Windows.Forms.Button();
			this.buttonClearLog = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.splitContainerViewer)).BeginInit();
			this.splitContainerViewer.Panel1.SuspendLayout();
			this.splitContainerViewer.Panel2.SuspendLayout();
			this.splitContainerViewer.SuspendLayout();
			this.menuStripMain.SuspendLayout();
			this.statusStrip.SuspendLayout();
			this.toolStripMain.SuspendLayout();
			this.tabControlMain.SuspendLayout();
			this.tabPageMain.SuspendLayout();
			this.tabPageLog.SuspendLayout();
			this.SuspendLayout();
			// 
			// splitContainerViewer
			// 
			this.splitContainerViewer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainerViewer.Location = new System.Drawing.Point(49, 3);
			this.splitContainerViewer.Name = "splitContainerViewer";
			// 
			// splitContainerViewer.Panel1
			// 
			this.splitContainerViewer.Panel1.AutoScroll = true;
			this.splitContainerViewer.Panel1.Controls.Add(this.textBoxHex);
			this.splitContainerViewer.Panel1.Controls.Add(this.labelTopOffsetHex);
			// 
			// splitContainerViewer.Panel2
			// 
			this.splitContainerViewer.Panel2.Controls.Add(this.textBoxAscii);
			this.splitContainerViewer.Panel2.Controls.Add(this.labelTopOffsetAscii);
			this.splitContainerViewer.Size = new System.Drawing.Size(574, 589);
			this.splitContainerViewer.SplitterDistance = 395;
			this.splitContainerViewer.SplitterWidth = 3;
			this.splitContainerViewer.TabIndex = 16;
			this.splitContainerViewer.TabStop = false;
			// 
			// textBoxHex
			// 
			this.textBoxHex.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxHex.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxHex.Font = new System.Drawing.Font("Consolas", 10.25F);
			this.textBoxHex.Location = new System.Drawing.Point(0, 17);
			this.textBoxHex.Multiline = true;
			this.textBoxHex.Name = "textBoxHex";
			this.textBoxHex.Size = new System.Drawing.Size(395, 572);
			this.textBoxHex.TabIndex = 9;
			this.textBoxHex.TabStop = false;
			// 
			// labelTopOffsetHex
			// 
			this.labelTopOffsetHex.AutoSize = true;
			this.labelTopOffsetHex.BackColor = System.Drawing.Color.Transparent;
			this.labelTopOffsetHex.Dock = System.Windows.Forms.DockStyle.Top;
			this.labelTopOffsetHex.Enabled = false;
			this.labelTopOffsetHex.Font = new System.Drawing.Font("Consolas", 10.25F);
			this.labelTopOffsetHex.ForeColor = System.Drawing.SystemColors.ControlText;
			this.labelTopOffsetHex.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.labelTopOffsetHex.Location = new System.Drawing.Point(0, 0);
			this.labelTopOffsetHex.Margin = new System.Windows.Forms.Padding(0);
			this.labelTopOffsetHex.Name = "labelTopOffsetHex";
			this.labelTopOffsetHex.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
			this.labelTopOffsetHex.Size = new System.Drawing.Size(26, 17);
			this.labelTopOffsetHex.TabIndex = 7;
			this.labelTopOffsetHex.Text = "00";
			this.labelTopOffsetHex.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			// 
			// textBoxAscii
			// 
			this.textBoxAscii.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxAscii.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxAscii.Font = new System.Drawing.Font("Consolas", 10.25F);
			this.textBoxAscii.Location = new System.Drawing.Point(0, 17);
			this.textBoxAscii.Multiline = true;
			this.textBoxAscii.Name = "textBoxAscii";
			this.textBoxAscii.Size = new System.Drawing.Size(176, 572);
			this.textBoxAscii.TabIndex = 16;
			// 
			// labelTopOffsetAscii
			// 
			this.labelTopOffsetAscii.Dock = System.Windows.Forms.DockStyle.Top;
			this.labelTopOffsetAscii.Enabled = false;
			this.labelTopOffsetAscii.Font = new System.Drawing.Font("Consolas", 10.25F);
			this.labelTopOffsetAscii.ForeColor = System.Drawing.SystemColors.ControlText;
			this.labelTopOffsetAscii.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.labelTopOffsetAscii.Location = new System.Drawing.Point(0, 0);
			this.labelTopOffsetAscii.Name = "labelTopOffsetAscii";
			this.labelTopOffsetAscii.Size = new System.Drawing.Size(176, 17);
			this.labelTopOffsetAscii.TabIndex = 15;
			this.labelTopOffsetAscii.Text = "0123456789ABCDEF";
			this.labelTopOffsetAscii.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			// 
			// menuStripMain
			// 
			this.menuStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fIleToolStripMenuItem,
            this.editToolStripMenuItem,
            this.deviceToolStripMenuItem,
            this.eEPROMToolStripMenuItem,
            this.helpToolStripMenuItem});
			this.menuStripMain.Location = new System.Drawing.Point(0, 0);
			this.menuStripMain.Name = "menuStripMain";
			this.menuStripMain.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
			this.menuStripMain.Size = new System.Drawing.Size(634, 24);
			this.menuStripMain.TabIndex = 11;
			this.menuStripMain.Text = "menuStrip1";
			// 
			// fIleToolStripMenuItem
			// 
			this.fIleToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveasToolStripMenuItem});
			this.fIleToolStripMenuItem.Name = "fIleToolStripMenuItem";
			this.fIleToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fIleToolStripMenuItem.Text = "&File";
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.openToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
			this.openToolStripMenuItem.Text = "&Open...";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.openFile);
			// 
			// saveToolStripMenuItem
			// 
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
			this.saveToolStripMenuItem.Text = "&Save";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
			// 
			// saveasToolStripMenuItem
			// 
			this.saveasToolStripMenuItem.Name = "saveasToolStripMenuItem";
			this.saveasToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt) 
            | System.Windows.Forms.Keys.S)));
			this.saveasToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
			this.saveasToolStripMenuItem.Text = "Save &as...";
			this.saveasToolStripMenuItem.Click += new System.EventHandler(this.saveToFile);
			// 
			// editToolStripMenuItem
			// 
			this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fixCrcToolStripMenuItem,
            this.copyHexToolStripMenuItem});
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
			this.editToolStripMenuItem.Text = "&Edit";
			// 
			// fixCrcToolStripMenuItem
			// 
			this.fixCrcToolStripMenuItem.Name = "fixCrcToolStripMenuItem";
			this.fixCrcToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F4)));
			this.fixCrcToolStripMenuItem.Size = new System.Drawing.Size(228, 22);
			this.fixCrcToolStripMenuItem.Text = "&Fix CRC";
			this.fixCrcToolStripMenuItem.Click += new System.EventHandler(this.fixCrcMenuItem_Click);
			// 
			// copyHexToolStripMenuItem
			// 
			this.copyHexToolStripMenuItem.Name = "copyHexToolStripMenuItem";
			this.copyHexToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt) 
            | System.Windows.Forms.Keys.C)));
			this.copyHexToolStripMenuItem.Size = new System.Drawing.Size(228, 22);
			this.copyHexToolStripMenuItem.Text = "&Copy HEX values";
			this.copyHexToolStripMenuItem.Click += new System.EventHandler(this.copyHexMenuItem_Click);
			// 
			// deviceToolStripMenuItem
			// 
			this.deviceToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.testToolStripMenuItem,
            this.disconnectToolStripMenuItem});
			this.deviceToolStripMenuItem.Name = "deviceToolStripMenuItem";
			this.deviceToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
			this.deviceToolStripMenuItem.Text = "&Device";
			// 
			// testToolStripMenuItem
			// 
			this.testToolStripMenuItem.Name = "testToolStripMenuItem";
			this.testToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
			this.testToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
			this.testToolStripMenuItem.Text = "&Test";
			this.testToolStripMenuItem.Click += new System.EventHandler(this.testToolStripMenuItem_Click);
			// 
			// disconnectToolStripMenuItem
			// 
			this.disconnectToolStripMenuItem.Name = "disconnectToolStripMenuItem";
			this.disconnectToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.disconnectToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
			this.disconnectToolStripMenuItem.Text = "&Disconnect";
			this.disconnectToolStripMenuItem.Click += new System.EventHandler(this.disconnectToolStripMenuItem_Click);
			// 
			// eEPROMToolStripMenuItem
			// 
			this.eEPROMToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.readToolStripMenuItem,
            this.writeToolStripMenuItem,
            this.toolStripSeparator5,
            this.clearToolStripMenuItem,
            this.enableToolStripMenuItem});
			this.eEPROMToolStripMenuItem.Name = "eEPROMToolStripMenuItem";
			this.eEPROMToolStripMenuItem.Size = new System.Drawing.Size(65, 20);
			this.eEPROMToolStripMenuItem.Text = "EEP&ROM";
			// 
			// readToolStripMenuItem
			// 
			this.readToolStripMenuItem.Name = "readToolStripMenuItem";
			this.readToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
			this.readToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
			this.readToolStripMenuItem.Text = "&Read";
			this.readToolStripMenuItem.Click += new System.EventHandler(this.readSpdFromEeprom);
			// 
			// writeToolStripMenuItem
			// 
			this.writeToolStripMenuItem.Name = "writeToolStripMenuItem";
			this.writeToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.W)));
			this.writeToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
			this.writeToolStripMenuItem.Text = "&Write";
			this.writeToolStripMenuItem.Click += new System.EventHandler(this.writeSpdToEeprom);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(206, 6);
			// 
			// clearToolStripMenuItem
			// 
			this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
			this.clearToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F7;
			this.clearToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
			this.clearToolStripMenuItem.Text = "Clear Write Protection";
			this.clearToolStripMenuItem.Click += new System.EventHandler(this.clearWriteProtection);
			// 
			// enableToolStripMenuItem
			// 
			this.enableToolStripMenuItem.Name = "enableToolStripMenuItem";
			this.enableToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F8;
			this.enableToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
			this.enableToolStripMenuItem.Text = "Set Write Protection";
			this.enableToolStripMenuItem.Click += new System.EventHandler(this.enableWriteProtectionMenuItem_Click);
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem,
            this.websiteToolStripMenuItem,
            this.donateViaPaypalToolStripMenuItem});
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.helpToolStripMenuItem.Text = "&Help";
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F1;
			this.aboutToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
			this.aboutToolStripMenuItem.Text = "About";
			this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
			// 
			// websiteToolStripMenuItem
			// 
			this.websiteToolStripMenuItem.Name = "websiteToolStripMenuItem";
			this.websiteToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
			this.websiteToolStripMenuItem.Text = "Visit &website";
			this.websiteToolStripMenuItem.Click += new System.EventHandler(this.websiteToolStripMenuItem_Click);
			// 
			// donateViaPaypalToolStripMenuItem
			// 
			this.donateViaPaypalToolStripMenuItem.Name = "donateViaPaypalToolStripMenuItem";
			this.donateViaPaypalToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
			this.donateViaPaypalToolStripMenuItem.Text = "Donate via Paypal";
			this.donateViaPaypalToolStripMenuItem.Click += new System.EventHandler(this.donateViaPaypalToolStripMenuItem_Click);
			// 
			// statusStrip
			// 
			this.statusStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Visible;
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusBarConnectionStatus,
            this.statusBarCrcStatus,
            this.statusProgressBar});
			this.statusStrip.Location = new System.Drawing.Point(0, 689);
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.Size = new System.Drawing.Size(634, 22);
			this.statusStrip.TabIndex = 13;
			this.statusStrip.Text = "statusStrip1";
			// 
			// statusBarConnectionStatus
			// 
			this.statusBarConnectionStatus.AutoSize = false;
			this.statusBarConnectionStatus.Name = "statusBarConnectionStatus";
			this.statusBarConnectionStatus.Size = new System.Drawing.Size(160, 17);
			this.statusBarConnectionStatus.Text = "Not connected";
			// 
			// statusBarCrcStatus
			// 
			this.statusBarCrcStatus.AutoSize = false;
			this.statusBarCrcStatus.Name = "statusBarCrcStatus";
			this.statusBarCrcStatus.Size = new System.Drawing.Size(75, 17);
			this.statusBarCrcStatus.Text = "CRC status";
			this.statusBarCrcStatus.Click += new System.EventHandler(this.statusBarCrcStatus_DoubleClick);
			// 
			// statusProgressBar
			// 
			this.statusProgressBar.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.statusProgressBar.AutoSize = false;
			this.statusProgressBar.Name = "statusProgressBar";
			this.statusProgressBar.Size = new System.Drawing.Size(75, 16);
			this.statusProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 38);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(6, 38);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 38);
			// 
			// toolStripMain
			// 
			this.toolStripMain.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolOpenFile,
            this.toolSaveFile_button,
            this.toolStripSeparator1,
            this.toolStripDeviceButton,
            this.toolStripSeparator3,
            this.readEeprom_button,
            this.writeEeprom_button,
            this.toolStripSeparator2,
            this.clearRswpButton,
            this.enableRswpButton,
            this.toolStripSeparator4,
            this.toolStripButtonScreenshot,
            this.toolStripButtonAbout});
			this.toolStripMain.Location = new System.Drawing.Point(0, 24);
			this.toolStripMain.Name = "toolStripMain";
			this.toolStripMain.Padding = new System.Windows.Forms.Padding(4, 0, 4, 2);
			this.toolStripMain.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
			this.toolStripMain.Size = new System.Drawing.Size(634, 40);
			this.toolStripMain.TabIndex = 12;
			this.toolStripMain.Text = "toolStrip1";
			// 
			// toolOpenFile
			// 
			this.toolOpenFile.Image = ((System.Drawing.Image)(resources.GetObject("toolOpenFile.Image")));
			this.toolOpenFile.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolOpenFile.Name = "toolOpenFile";
			this.toolOpenFile.Size = new System.Drawing.Size(40, 35);
			this.toolOpenFile.Text = "Open";
			this.toolOpenFile.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.toolOpenFile.ToolTipText = "Open file";
			this.toolOpenFile.Click += new System.EventHandler(this.openFile);
			// 
			// toolSaveFile_button
			// 
			this.toolSaveFile_button.Image = ((System.Drawing.Image)(resources.GetObject("toolSaveFile_button.Image")));
			this.toolSaveFile_button.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolSaveFile_button.Name = "toolSaveFile_button";
			this.toolSaveFile_button.Size = new System.Drawing.Size(35, 35);
			this.toolSaveFile_button.Text = "Save";
			this.toolSaveFile_button.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.toolSaveFile_button.ToolTipText = "Save file";
			this.toolSaveFile_button.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
			// 
			// toolStripDeviceButton
			// 
			this.toolStripDeviceButton.AutoSize = false;
			this.toolStripDeviceButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshToolStripMenuItem,
            this.toolStripDeviceSeparator});
			this.toolStripDeviceButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDeviceButton.Image")));
			this.toolStripDeviceButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripDeviceButton.Name = "toolStripDeviceButton";
			this.toolStripDeviceButton.Size = new System.Drawing.Size(75, 35);
			this.toolStripDeviceButton.Text = "Device";
			this.toolStripDeviceButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.toolStripDeviceButton.ToolTipText = "Select device port and address";
			// 
			// refreshToolStripMenuItem
			// 
			this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
			this.refreshToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.refreshToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
			this.refreshToolStripMenuItem.Text = "Refresh list";
			this.refreshToolStripMenuItem.Click += new System.EventHandler(this.populateDevices);
			// 
			// toolStripDeviceSeparator
			// 
			this.toolStripDeviceSeparator.Name = "toolStripDeviceSeparator";
			this.toolStripDeviceSeparator.Size = new System.Drawing.Size(147, 6);
			// 
			// readEeprom_button
			// 
			this.readEeprom_button.Image = ((System.Drawing.Image)(resources.GetObject("readEeprom_button.Image")));
			this.readEeprom_button.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.readEeprom_button.Name = "readEeprom_button";
			this.readEeprom_button.Size = new System.Drawing.Size(37, 35);
			this.readEeprom_button.Text = "Read";
			this.readEeprom_button.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.readEeprom_button.ToolTipText = "Read SPD from EEPROM";
			this.readEeprom_button.Click += new System.EventHandler(this.readSpdFromEeprom);
			// 
			// writeEeprom_button
			// 
			this.writeEeprom_button.Image = ((System.Drawing.Image)(resources.GetObject("writeEeprom_button.Image")));
			this.writeEeprom_button.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.writeEeprom_button.Name = "writeEeprom_button";
			this.writeEeprom_button.Size = new System.Drawing.Size(39, 35);
			this.writeEeprom_button.Text = "Write";
			this.writeEeprom_button.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.writeEeprom_button.ToolTipText = "Write SPD to EEPROM";
			this.writeEeprom_button.Click += new System.EventHandler(this.writeSpdToEeprom);
			// 
			// clearRswpButton
			// 
			this.clearRswpButton.Image = ((System.Drawing.Image)(resources.GetObject("clearRswpButton.Image")));
			this.clearRswpButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.clearRswpButton.Name = "clearRswpButton";
			this.clearRswpButton.Size = new System.Drawing.Size(59, 35);
			this.clearRswpButton.Text = "Clear WP";
			this.clearRswpButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.clearRswpButton.ToolTipText = "Clear Reversible Software Write Protection";
			this.clearRswpButton.Click += new System.EventHandler(this.clearWriteProtection);
			// 
			// enableRswpButton
			// 
			this.enableRswpButton.Image = ((System.Drawing.Image)(resources.GetObject("enableRswpButton.Image")));
			this.enableRswpButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.enableRswpButton.Name = "enableRswpButton";
			this.enableRswpButton.Size = new System.Drawing.Size(48, 35);
			this.enableRswpButton.Text = "Set WP";
			this.enableRswpButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.enableRswpButton.ToolTipText = "Set Reversible Software Write Protection";
			this.enableRswpButton.Click += new System.EventHandler(this.enableWriteProtectionMenuItem_Click);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(6, 38);
			// 
			// toolStripButtonScreenshot
			// 
			this.toolStripButtonScreenshot.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonScreenshot.Image")));
			this.toolStripButtonScreenshot.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonScreenshot.Name = "toolStripButtonScreenshot";
			this.toolStripButtonScreenshot.Size = new System.Drawing.Size(69, 35);
			this.toolStripButtonScreenshot.Text = "Screenshot";
			this.toolStripButtonScreenshot.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.toolStripButtonScreenshot.Click += new System.EventHandler(this.toolStripButtonScreenshot_Click);
			this.toolStripButtonScreenshot.MouseEnter += new System.EventHandler(this.prepareScreenShot);
			// 
			// toolStripButtonAbout
			// 
			this.toolStripButtonAbout.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonAbout.Image")));
			this.toolStripButtonAbout.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonAbout.Name = "toolStripButtonAbout";
			this.toolStripButtonAbout.Size = new System.Drawing.Size(44, 35);
			this.toolStripButtonAbout.Text = "About";
			this.toolStripButtonAbout.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.toolStripButtonAbout.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
			// 
			// timerInterfaceUpdater
			// 
			this.timerInterfaceUpdater.Enabled = true;
			this.timerInterfaceUpdater.Tick += new System.EventHandler(this.timerInterfaceUpdater_Tick);
			// 
			// loggerBox
			// 
			this.loggerBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.loggerBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.loggerBox.Font = new System.Drawing.Font("Consolas", 9.75F);
			this.loggerBox.IntegralHeight = false;
			this.loggerBox.ItemHeight = 15;
			this.loggerBox.Location = new System.Drawing.Point(0, 0);
			this.loggerBox.Name = "loggerBox";
			this.loggerBox.ScrollAlwaysVisible = true;
			this.loggerBox.Size = new System.Drawing.Size(626, 560);
			this.loggerBox.TabIndex = 0;
			this.loggerBox.TabStop = false;
			this.loggerBox.UseTabStops = false;
			// 
			// sideOffsets
			// 
			this.sideOffsets.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.sideOffsets.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.sideOffsets.Enabled = false;
			this.sideOffsets.Font = new System.Drawing.Font("Consolas", 10.25F);
			this.sideOffsets.ForeColor = System.Drawing.SystemColors.ControlText;
			this.sideOffsets.Location = new System.Drawing.Point(-7, 20);
			this.sideOffsets.MaxLength = 1024;
			this.sideOffsets.Multiline = true;
			this.sideOffsets.Name = "sideOffsets";
			this.sideOffsets.ReadOnly = true;
			this.sideOffsets.Size = new System.Drawing.Size(50, 575);
			this.sideOffsets.TabIndex = 8;
			this.sideOffsets.Text = "000";
			this.sideOffsets.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// tabControlMain
			// 
			this.tabControlMain.Controls.Add(this.tabPageMain);
			this.tabControlMain.Controls.Add(this.tabPageLog);
			this.tabControlMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControlMain.HotTrack = true;
			this.tabControlMain.ItemSize = new System.Drawing.Size(100, 22);
			this.tabControlMain.Location = new System.Drawing.Point(0, 64);
			this.tabControlMain.Margin = new System.Windows.Forms.Padding(0);
			this.tabControlMain.Name = "tabControlMain";
			this.tabControlMain.Padding = new System.Drawing.Point(15, 5);
			this.tabControlMain.SelectedIndex = 0;
			this.tabControlMain.Size = new System.Drawing.Size(634, 625);
			this.tabControlMain.SizeMode = System.Windows.Forms.TabSizeMode.FillToRight;
			this.tabControlMain.TabIndex = 17;
			// 
			// tabPageMain
			// 
			this.tabPageMain.BackColor = System.Drawing.SystemColors.Control;
			this.tabPageMain.Controls.Add(this.splitContainerViewer);
			this.tabPageMain.Controls.Add(this.sideOffsets);
			this.tabPageMain.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
			this.tabPageMain.ForeColor = System.Drawing.SystemColors.ControlText;
			this.tabPageMain.Location = new System.Drawing.Point(4, 26);
			this.tabPageMain.Name = "tabPageMain";
			this.tabPageMain.Size = new System.Drawing.Size(626, 595);
			this.tabPageMain.TabIndex = 0;
			this.tabPageMain.Text = "Data viewer";
			// 
			// tabPageLog
			// 
			this.tabPageLog.Controls.Add(this.buttonSaveLog);
			this.tabPageLog.Controls.Add(this.buttonClearLog);
			this.tabPageLog.Controls.Add(this.loggerBox);
			this.tabPageLog.Location = new System.Drawing.Point(4, 26);
			this.tabPageLog.Name = "tabPageLog";
			this.tabPageLog.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageLog.Size = new System.Drawing.Size(626, 595);
			this.tabPageLog.TabIndex = 1;
			this.tabPageLog.Text = "Program log";
			this.tabPageLog.ToolTipText = "View log";
			// 
			// buttonSaveLog
			// 
			this.buttonSaveLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonSaveLog.AutoSize = true;
			this.buttonSaveLog.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.buttonSaveLog.Location = new System.Drawing.Point(430, 566);
			this.buttonSaveLog.Name = "buttonSaveLog";
			this.buttonSaveLog.Size = new System.Drawing.Size(107, 23);
			this.buttonSaveLog.TabIndex = 2;
			this.buttonSaveLog.Text = "Save to file...";
			this.buttonSaveLog.UseVisualStyleBackColor = true;
			this.buttonSaveLog.Click += new System.EventHandler(this.buttonSaveLog_Click);
			// 
			// buttonClearLog
			// 
			this.buttonClearLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonClearLog.AutoSize = true;
			this.buttonClearLog.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.buttonClearLog.Location = new System.Drawing.Point(543, 566);
			this.buttonClearLog.Name = "buttonClearLog";
			this.buttonClearLog.Size = new System.Drawing.Size(75, 23);
			this.buttonClearLog.TabIndex = 1;
			this.buttonClearLog.Text = "Clear";
			this.buttonClearLog.UseVisualStyleBackColor = true;
			this.buttonClearLog.Click += new System.EventHandler(this.buttonClearLog_Click);
			// 
			// formMain
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(634, 711);
			this.Controls.Add(this.tabControlMain);
			this.Controls.Add(this.statusStrip);
			this.Controls.Add(this.toolStripMain);
			this.Controls.Add(this.menuStripMain);
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStripMain;
			this.Name = "formMain";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "DDR4 SPD Reader/Writer";
			this.MaximumSizeChanged += new System.EventHandler(this.formResized);
			this.Load += new System.EventHandler(this.FormMain_Load);
			this.ResizeEnd += new System.EventHandler(this.formResized);
			this.SizeChanged += new System.EventHandler(this.formResized);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.formMain_DragDrop);
			this.splitContainerViewer.Panel1.ResumeLayout(false);
			this.splitContainerViewer.Panel1.PerformLayout();
			this.splitContainerViewer.Panel2.ResumeLayout(false);
			this.splitContainerViewer.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainerViewer)).EndInit();
			this.splitContainerViewer.ResumeLayout(false);
			this.menuStripMain.ResumeLayout(false);
			this.menuStripMain.PerformLayout();
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
			this.toolStripMain.ResumeLayout(false);
			this.toolStripMain.PerformLayout();
			this.tabControlMain.ResumeLayout(false);
			this.tabPageMain.ResumeLayout(false);
			this.tabPageMain.PerformLayout();
			this.tabPageLog.ResumeLayout(false);
			this.tabPageLog.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.MenuStrip menuStripMain;
		private System.Windows.Forms.ToolStripMenuItem fIleToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem deviceToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem disconnectToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem testToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem eEPROMToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem readToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem writeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem fixCrcToolStripMenuItem;
		private System.Windows.Forms.StatusStrip statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel statusBarConnectionStatus;
		private System.Windows.Forms.ToolStripMenuItem copyHexToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveasToolStripMenuItem;
		private System.Windows.Forms.ToolStripStatusLabel statusBarCrcStatus;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripButton toolOpenFile;
		private System.Windows.Forms.ToolStripButton toolSaveFile_button;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripDropDownButton toolStripDeviceButton;
		private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripDeviceSeparator;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripButton readEeprom_button;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton enableRswpButton;
		private System.Windows.Forms.ToolStripButton clearRswpButton;
		private System.Windows.Forms.ToolStrip toolStripMain;
		private System.Windows.Forms.Timer timerInterfaceUpdater;
		private System.Windows.Forms.ToolStripButton writeEeprom_button;
		private System.Windows.Forms.ToolStripProgressBar statusProgressBar;
		private System.Windows.Forms.ToolStripButton toolStripButtonAbout;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem enableToolStripMenuItem;
		private System.Windows.Forms.ListBox loggerBox;
		private System.Windows.Forms.TextBox sideOffsets;
		private System.Windows.Forms.SplitContainer splitContainerViewer;
		private System.Windows.Forms.Label labelTopOffsetAscii;
		private System.Windows.Forms.TabControl tabControlMain;
		private System.Windows.Forms.TabPage tabPageMain;
		private System.Windows.Forms.TabPage tabPageLog;
		private System.Windows.Forms.Button buttonClearLog;
		private System.Windows.Forms.Button buttonSaveLog;
		private System.Windows.Forms.ToolStripMenuItem websiteToolStripMenuItem;
		private System.Windows.Forms.ToolStripButton toolStripButtonScreenshot;
		private System.Windows.Forms.ToolStripMenuItem donateViaPaypalToolStripMenuItem;
		private System.Windows.Forms.TextBox textBoxAscii;
		private System.Windows.Forms.TextBox textBoxHex;
		private System.Windows.Forms.Label labelTopOffsetHex;
	}
}

