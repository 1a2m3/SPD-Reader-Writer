namespace SpdReaderWriterGUI {
	partial class formAbout {
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(formAbout));
			this.aboutButtonClose = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxAbout = new System.Windows.Forms.TextBox();
			this.paypalLink = new System.Windows.Forms.LinkLabel();
			this.btcLink = new System.Windows.Forms.LinkLabel();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// aboutButtonClose
			// 
			this.aboutButtonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.aboutButtonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.aboutButtonClose.Location = new System.Drawing.Point(397, 276);
			this.aboutButtonClose.Name = "aboutButtonClose";
			this.aboutButtonClose.Size = new System.Drawing.Size(75, 23);
			this.aboutButtonClose.TabIndex = 0;
			this.aboutButtonClose.Text = "Close";
			this.aboutButtonClose.UseVisualStyleBackColor = true;
			this.aboutButtonClose.Click += new System.EventHandler(this.aboutButtonClose_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(186, 12);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(7, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "\r\n";
			// 
			// textBoxAbout
			// 
			this.textBoxAbout.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxAbout.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxAbout.Location = new System.Drawing.Point(12, 12);
			this.textBoxAbout.Multiline = true;
			this.textBoxAbout.Name = "textBoxAbout";
			this.textBoxAbout.ReadOnly = true;
			this.textBoxAbout.Size = new System.Drawing.Size(460, 233);
			this.textBoxAbout.TabIndex = 3;
			this.textBoxAbout.Text = resources.GetString("textBoxAbout.Text");
			// 
			// paypalLink
			// 
			this.paypalLink.AutoSize = true;
			this.paypalLink.Location = new System.Drawing.Point(60, 248);
			this.paypalLink.Name = "paypalLink";
			this.paypalLink.Size = new System.Drawing.Size(94, 13);
			this.paypalLink.TabIndex = 6;
			this.paypalLink.TabStop = true;
			this.paypalLink.Text = "Donate via Paypal";
			this.paypalLink.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.paypalLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
			// 
			// btcLink
			// 
			this.btcLink.AutoSize = true;
			this.btcLink.Location = new System.Drawing.Point(60, 276);
			this.btcLink.Name = "btcLink";
			this.btcLink.Size = new System.Drawing.Size(164, 13);
			this.btcLink.TabIndex = 7;
			this.btcLink.TabStop = true;
			this.btcLink.Text = "Copy Bitcoin address to clipboard";
			this.btcLink.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btcLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel2_LinkClicked);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 248);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(42, 13);
			this.label2.TabIndex = 8;
			this.label2.Text = "Paypal:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 276);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(42, 13);
			this.label3.TabIndex = 9;
			this.label3.Text = "Bitcoin:";
			// 
			// formAbout
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.aboutButtonClose;
			this.ClientSize = new System.Drawing.Size(484, 311);
			this.ControlBox = false;
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.btcLink);
			this.Controls.Add(this.paypalLink);
			this.Controls.Add(this.textBoxAbout);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.aboutButtonClose);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "formAbout";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "About";
			this.Load += new System.EventHandler(this.formAbout_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button aboutButtonClose;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxAbout;
		private System.Windows.Forms.LinkLabel paypalLink;
		private System.Windows.Forms.LinkLabel btcLink;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
	}
}