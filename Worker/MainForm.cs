using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace Worker {
	public class MainForm : System.Windows.Forms.Form {
		private System.Windows.Forms.TextBox txtLog;
		#region Codice generato dal wizard
		private System.ComponentModel.Container components = null;

		public MainForm() {
			InitializeComponent();
		}

		protected override void Dispose( bool disposing ) {
			if( disposing ) {
				if(components != null) {
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.txtLog = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// txtLog
			// 
			this.txtLog.AcceptsReturn = true;
			this.txtLog.AcceptsTab = true;
			this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtLog.Location = new System.Drawing.Point(0, 0);
			this.txtLog.Multiline = true;
			this.txtLog.Name = "txtLog";
			this.txtLog.ReadOnly = true;
			this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.txtLog.Size = new System.Drawing.Size(592, 342);
			this.txtLog.TabIndex = 0;
			this.txtLog.WordWrap = false;
			// 
			// MainForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(592, 342);
			this.Controls.Add(this.txtLog);
			this.Name = "MainForm";
			this.Text = "MainForm";
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
		#endregion

		private MainService _service = null;
		private bool _canLog = false;

		public MainForm(MainService service) {
			InitializeComponent();
			_service = service;
			_service.StartServer(this);
		}

		private void UpdateText(string text) {
			if (txtLog.Lines.Length > 100)
				txtLog.Clear();
			var msg = string.Format("{0}\t{1}\r\n", DateTime.Now, text);
			txtLog.AppendText(msg);
			txtLog.Select(txtLog.TextLength, 0);
			txtLog.ScrollToCaret();
		}

		private List<string> _buffered = new List<string>();

		public void Log(string message) {
			if (_canLog)
				txtLog.Invoke(new UpdateTextCallback(this.UpdateText), new object[] { message });
			else
				_buffered.Add(message);
		}

		private void FlushLog() {
			foreach (string msg in _buffered)
				UpdateText(msg);
			_buffered.Clear();
		}

		private delegate void UpdateTextCallback(string text);

		private void MainForm_Load(object sender, EventArgs e) {
			_canLog = true;
			FlushLog();
		}

	}
}
