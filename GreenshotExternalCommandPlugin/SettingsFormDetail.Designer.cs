/*
 * Created by SharpDevelop.
 * User: 05018038
 * Date: 04.04.2012
 * Time: 10:50
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
namespace GreenshotExternalCommandPlugin
{
	partial class SettingsFormDetail
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
			this.buttonOk = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label4 = new System.Windows.Forms.Label();
			this.button3 = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.textBox_name = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.textBox_arguments = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.textBox_commandline = new System.Windows.Forms.TextBox();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonOk
			// 
			this.buttonOk.Enabled = false;
			this.buttonOk.Location = new System.Drawing.Point(273, 140);
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.Size = new System.Drawing.Size(75, 23);
			this.buttonOk.TabIndex = 26;
			this.buttonOk.Text = "OK";
			this.buttonOk.UseVisualStyleBackColor = true;
			this.buttonOk.Click += new System.EventHandler(this.ButtonOkClick);
			// 
			// buttonCancel
			// 
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(9, 140);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 27;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.ButtonCancelClick);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.button3);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.textBox_name);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.textBox_arguments);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.textBox_commandline);
			this.groupBox1.Location = new System.Drawing.Point(10, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(339, 122);
			this.groupBox1.TabIndex = 28;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Configure";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(68, 98);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(225, 21);
			this.label4.TabIndex = 19;
			this.label4.Text = "{0} is the filename of your screenshot";
			// 
			// button3
			// 
			this.button3.Location = new System.Drawing.Point(298, 47);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(33, 23);
			this.button3.TabIndex = 18;
			this.button3.Text = "...";
			this.button3.UseVisualStyleBackColor = true;
			this.button3.Click += new System.EventHandler(this.Button3Click);
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(6, 26);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(56, 17);
			this.label3.TabIndex = 17;
			this.label3.Text = "Name";
			// 
			// textBox_name
			// 
			this.textBox_name.Location = new System.Drawing.Point(68, 23);
			this.textBox_name.Name = "textBox_name";
			this.textBox_name.Size = new System.Drawing.Size(225, 20);
			this.textBox_name.TabIndex = 12;
			this.textBox_name.TextChanged += new System.EventHandler(this.textBox_name_TextChanged);
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(6, 78);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(56, 17);
			this.label2.TabIndex = 16;
			this.label2.Text = "Arguments";
			// 
			// textBox_arguments
			// 
			this.textBox_arguments.Location = new System.Drawing.Point(68, 75);
			this.textBox_arguments.Name = "textBox_arguments";
			this.textBox_arguments.Size = new System.Drawing.Size(225, 20);
			this.textBox_arguments.TabIndex = 14;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(6, 52);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(56, 17);
			this.label1.TabIndex = 15;
			this.label1.Text = "Command";
			// 
			// textBox_commandline
			// 
			this.textBox_commandline.Location = new System.Drawing.Point(68, 49);
			this.textBox_commandline.Name = "textBox_commandline";
			this.textBox_commandline.Size = new System.Drawing.Size(225, 20);
			this.textBox_commandline.TabIndex = 13;
			this.textBox_commandline.TextChanged += new System.EventHandler(this.textBox_commandline_TextChanged);
			// 
			// SettingsFormDetail
			// 
			this.AcceptButton = this.buttonOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(360, 172);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.buttonOk);
			this.Controls.Add(this.buttonCancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "SettingsFormDetail";
			this.Text = "Configure";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOk;
		private System.Windows.Forms.TextBox textBox_commandline;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBox_arguments;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBox_name;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button button3;
	}
}
