namespace Greenshot.Forms {
	partial class TitleDialog {
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
			this.textboxTitle = new System.Windows.Forms.TextBox();
			this.btnOK = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// textboxTitle
			// 
			this.textboxTitle.Location = new System.Drawing.Point(13, 13);
			this.textboxTitle.Name = "textboxTitle";
			this.textboxTitle.Size = new System.Drawing.Size(360, 20);
			this.textboxTitle.TabIndex = 0;
			this.textboxTitle.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textboxTitle_KeyPress);
			// 
			// btnOK
			// 
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(126, 42);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(75, 23);
			this.btnOK.TabIndex = 1;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			// 
			// TitleDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(390, 77);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.textboxTitle);
			this.Name = "TitleDialog";
			this.Text = "Image editor title";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox textboxTitle;
		private System.Windows.Forms.Button btnOK;
	}
}