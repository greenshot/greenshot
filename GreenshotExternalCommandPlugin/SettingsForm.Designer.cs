/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2014 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
namespace ExternalCommand {
	partial class SettingsForm {
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
		private void InitializeComponent() {
			this.buttonCancel = new GreenshotPlugin.Controls.GreenshotButton();
			this.buttonOk = new GreenshotPlugin.Controls.GreenshotButton();
			this.listView1 = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.button_new = new GreenshotPlugin.Controls.GreenshotButton();
			this.button_delete = new GreenshotPlugin.Controls.GreenshotButton();
			this.button_edit = new GreenshotPlugin.Controls.GreenshotButton();
			this.SuspendLayout();
			// 
			// buttonCancel
			// 
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.LanguageKey = "CANCEL";
			this.buttonCancel.Location = new System.Drawing.Point(275, 144);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 11;
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// buttonOk
			// 
			this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOk.LanguageKey = "OK";
			this.buttonOk.Location = new System.Drawing.Point(275, 173);
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.Size = new System.Drawing.Size(75, 23);
			this.buttonOk.TabIndex = 10;
			this.buttonOk.UseVisualStyleBackColor = true;
			this.buttonOk.Click += new System.EventHandler(this.ButtonOkClick);
			// 
			// listView1
			// 
			this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
			this.listView1.FullRowSelect = true;
			this.listView1.Location = new System.Drawing.Point(13, 13);
			this.listView1.MultiSelect = false;
			this.listView1.Name = "listView1";
			this.listView1.Size = new System.Drawing.Size(255, 183);
			this.listView1.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.listView1.TabIndex = 5;
			this.listView1.UseCompatibleStateImageBehavior = false;
			this.listView1.View = System.Windows.Forms.View.Details;
			this.listView1.SelectedIndexChanged += new System.EventHandler(this.ListView1ItemSelectionChanged);
			this.listView1.DoubleClick += new System.EventHandler(this.ListView1DoubleClick);
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Name";
			this.columnHeader1.Width = 226;
			// 
			// button_new
			// 
			this.button_new.LanguageKey = "externalcommand.settings_new";
			this.button_new.Location = new System.Drawing.Point(275, 13);
			this.button_new.Name = "button_new";
			this.button_new.Size = new System.Drawing.Size(75, 23);
			this.button_new.TabIndex = 1;
			this.button_new.UseVisualStyleBackColor = true;
			this.button_new.Click += new System.EventHandler(this.ButtonAddClick);
			// 
			// button_delete
			// 
			this.button_delete.LanguageKey = "externalcommand.settings_delete";
			this.button_delete.Location = new System.Drawing.Point(274, 71);
			this.button_delete.Name = "button_delete";
			this.button_delete.Size = new System.Drawing.Size(75, 23);
			this.button_delete.TabIndex = 3;
			this.button_delete.UseVisualStyleBackColor = true;
			this.button_delete.Click += new System.EventHandler(this.ButtonDeleteClick);
			// 
			// button_edit
			// 
			this.button_edit.Enabled = false;
			this.button_edit.LanguageKey = "externalcommand.settings_edit";
			this.button_edit.Location = new System.Drawing.Point(275, 42);
			this.button_edit.Name = "button_edit";
			this.button_edit.Size = new System.Drawing.Size(75, 23);
			this.button_edit.TabIndex = 2;
			this.button_edit.UseVisualStyleBackColor = true;
			this.button_edit.Click += new System.EventHandler(this.ButtonEditClick);
			// 
			// SettingsForm
			// 
			this.AcceptButton = this.buttonOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(365, 208);
			this.Controls.Add(this.button_edit);
			this.Controls.Add(this.button_delete);
			this.Controls.Add(this.button_new);
			this.Controls.Add(this.listView1);
			this.Controls.Add(this.buttonOk);
			this.Controls.Add(this.buttonCancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.LanguageKey = "externalcommand.settings_title";
			this.Name = "SettingsForm";
			this.ResumeLayout(false);

		}
		private GreenshotPlugin.Controls.GreenshotButton button_edit;
		private GreenshotPlugin.Controls.GreenshotButton button_delete;
		private GreenshotPlugin.Controls.GreenshotButton button_new;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ListView listView1;
		private GreenshotPlugin.Controls.GreenshotButton buttonOk;
		private GreenshotPlugin.Controls.GreenshotButton buttonCancel;
		
	}
}
