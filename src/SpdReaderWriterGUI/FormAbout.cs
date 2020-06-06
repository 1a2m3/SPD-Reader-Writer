using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace SpdReaderWriterGUI {
	public partial class formAbout : Form {
		public formAbout() {
			InitializeComponent();
		}

		private void aboutButtonClose_Click(object sender, EventArgs e) {
			this.Hide();
		}

		private void formAbout_Load(object sender, EventArgs e) {
			//SystemSounds.Beep.Play();
		}

		private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
			Clipboard.SetText("3Pe9VhVaUygyMFGT3pFuQ3dAghS36NPJTz");
			MessageBox.Show("Address copied to clipboard", "Bitcoin", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
			Process.Start("http://paypal.me/mik4rt3m");
		}
	}
}
