namespace GreenshotQiniuPlugin
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.gslblAccessKey = new GreenshotPlugin.Controls.GreenshotLabel();
            this.gstbAccessKey = new GreenshotPlugin.Controls.GreenshotTextBox();
            this.gstbSecretKey = new GreenshotPlugin.Controls.GreenshotTextBox();
            this.gslblSecretKey = new GreenshotPlugin.Controls.GreenshotLabel();
            this.gstbScope = new GreenshotPlugin.Controls.GreenshotTextBox();
            this.gslblScope = new GreenshotPlugin.Controls.GreenshotLabel();
            this.buttonCancel = new GreenshotPlugin.Controls.GreenshotButton();
            this.buttonOK = new GreenshotPlugin.Controls.GreenshotButton();
            this.label_upload_format = new GreenshotPlugin.Controls.GreenshotLabel();
            this.combobox_uploadimageformat = new GreenshotPlugin.Controls.GreenshotComboBox();
            this.gstbDefaultDomain = new GreenshotPlugin.Controls.GreenshotTextBox();
            this.gslblDefaultDomain = new GreenshotPlugin.Controls.GreenshotLabel();
            this.gstbImageNamePrefix = new GreenshotPlugin.Controls.GreenshotTextBox();
            this.gslblImageNamePrefix = new GreenshotPlugin.Controls.GreenshotLabel();
            this.SuspendLayout();
            // 
            // gslblAccessKey
            // 
            this.gslblAccessKey.AutoSize = true;
            this.gslblAccessKey.Location = new System.Drawing.Point(18, 48);
            this.gslblAccessKey.Name = "gslblAccessKey";
            this.gslblAccessKey.Size = new System.Drawing.Size(65, 12);
            this.gslblAccessKey.TabIndex = 0;
            this.gslblAccessKey.Text = "Access Key";
            // 
            // gstbAccessKey
            // 
            this.gstbAccessKey.Location = new System.Drawing.Point(137, 48);
            this.gstbAccessKey.Name = "gstbAccessKey";
            this.gstbAccessKey.PropertyName = "AccessKey";
            this.gstbAccessKey.SectionName = "Qiniu";
            this.gstbAccessKey.Size = new System.Drawing.Size(241, 21);
            this.gstbAccessKey.TabIndex = 1;
            // 
            // gstbSecretKey
            // 
            this.gstbSecretKey.Location = new System.Drawing.Point(137, 79);
            this.gstbSecretKey.Name = "gstbSecretKey";
            this.gstbSecretKey.PropertyName = "SecretKey";
            this.gstbSecretKey.SectionName = "Qiniu";
            this.gstbSecretKey.Size = new System.Drawing.Size(241, 21);
            this.gstbSecretKey.TabIndex = 3;
            // 
            // gslblSecretKey
            // 
            this.gslblSecretKey.AutoSize = true;
            this.gslblSecretKey.Location = new System.Drawing.Point(18, 82);
            this.gslblSecretKey.Name = "gslblSecretKey";
            this.gslblSecretKey.Size = new System.Drawing.Size(65, 12);
            this.gslblSecretKey.TabIndex = 2;
            this.gslblSecretKey.Text = "Secret Key";
            // 
            // gstbScope
            // 
            this.gstbScope.Location = new System.Drawing.Point(137, 111);
            this.gstbScope.Name = "gstbScope";
            this.gstbScope.PropertyName = "Scope";
            this.gstbScope.SectionName = "Qiniu";
            this.gstbScope.Size = new System.Drawing.Size(241, 21);
            this.gstbScope.TabIndex = 5;
            // 
            // gslblScope
            // 
            this.gslblScope.AutoSize = true;
            this.gslblScope.Location = new System.Drawing.Point(18, 114);
            this.gslblScope.Name = "gslblScope";
            this.gslblScope.Size = new System.Drawing.Size(35, 12);
            this.gslblScope.TabIndex = 4;
            this.gslblScope.Text = "Scope";
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.LanguageKey = "CANCEL";
            this.buttonCancel.Location = new System.Drawing.Point(294, 208);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 7;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.LanguageKey = "OK";
            this.buttonOK.Location = new System.Drawing.Point(213, 208);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 6;
            this.buttonOK.Text = "Ok";
            this.buttonOK.UseVisualStyleBackColor = true;
            // 
            // label_upload_format
            // 
            this.label_upload_format.LanguageKey = "Qiniu.label_upload_format";
            this.label_upload_format.Location = new System.Drawing.Point(18, 15);
            this.label_upload_format.Name = "label_upload_format";
            this.label_upload_format.Size = new System.Drawing.Size(83, 20);
            this.label_upload_format.TabIndex = 15;
            this.label_upload_format.Text = "Image format";
            // 
            // combobox_uploadimageformat
            // 
            this.combobox_uploadimageformat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.combobox_uploadimageformat.FormattingEnabled = true;
            this.combobox_uploadimageformat.Location = new System.Drawing.Point(137, 12);
            this.combobox_uploadimageformat.Name = "combobox_uploadimageformat";
            this.combobox_uploadimageformat.PropertyName = "UploadFormat";
            this.combobox_uploadimageformat.SectionName = "Qiniu";
            this.combobox_uploadimageformat.Size = new System.Drawing.Size(241, 20);
            this.combobox_uploadimageformat.TabIndex = 13;
            // 
            // gstbDefaultDomain
            // 
            this.gstbDefaultDomain.Location = new System.Drawing.Point(137, 144);
            this.gstbDefaultDomain.Name = "gstbDefaultDomain";
            this.gstbDefaultDomain.PropertyName = "DefaultDomain";
            this.gstbDefaultDomain.SectionName = "Qiniu";
            this.gstbDefaultDomain.Size = new System.Drawing.Size(241, 21);
            this.gstbDefaultDomain.TabIndex = 17;
            // 
            // gslblDefaultDomain
            // 
            this.gslblDefaultDomain.AutoSize = true;
            this.gslblDefaultDomain.Location = new System.Drawing.Point(18, 147);
            this.gslblDefaultDomain.Name = "gslblDefaultDomain";
            this.gslblDefaultDomain.Size = new System.Drawing.Size(89, 12);
            this.gslblDefaultDomain.TabIndex = 16;
            this.gslblDefaultDomain.Text = "Default Domain";
            // 
            // gstbImageNamePrefix
            // 
            this.gstbImageNamePrefix.Location = new System.Drawing.Point(137, 178);
            this.gstbImageNamePrefix.Name = "gstbImageNamePrefix";
            this.gstbImageNamePrefix.PropertyName = "ImageNamePrefix";
            this.gstbImageNamePrefix.SectionName = "Qiniu";
            this.gstbImageNamePrefix.Size = new System.Drawing.Size(241, 21);
            this.gstbImageNamePrefix.TabIndex = 19;
            // 
            // gslblImageNamePrefix
            // 
            this.gslblImageNamePrefix.AutoSize = true;
            this.gslblImageNamePrefix.Location = new System.Drawing.Point(18, 181);
            this.gslblImageNamePrefix.Name = "gslblImageNamePrefix";
            this.gslblImageNamePrefix.Size = new System.Drawing.Size(107, 12);
            this.gslblImageNamePrefix.TabIndex = 18;
            this.gslblImageNamePrefix.Text = "Image Name Prefix";
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(396, 243);
            this.Controls.Add(this.gstbImageNamePrefix);
            this.Controls.Add(this.gslblImageNamePrefix);
            this.Controls.Add(this.gstbDefaultDomain);
            this.Controls.Add(this.gslblDefaultDomain);
            this.Controls.Add(this.label_upload_format);
            this.Controls.Add(this.combobox_uploadimageformat);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.gstbScope);
            this.Controls.Add(this.gslblScope);
            this.Controls.Add(this.gstbSecretKey);
            this.Controls.Add(this.gslblSecretKey);
            this.Controls.Add(this.gstbAccessKey);
            this.Controls.Add(this.gslblAccessKey);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Qiniu image upload settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private GreenshotPlugin.Controls.GreenshotLabel gslblAccessKey;
        private GreenshotPlugin.Controls.GreenshotTextBox gstbAccessKey;
        private GreenshotPlugin.Controls.GreenshotTextBox gstbSecretKey;
        private GreenshotPlugin.Controls.GreenshotLabel gslblSecretKey;
        private GreenshotPlugin.Controls.GreenshotTextBox gstbScope;
        private GreenshotPlugin.Controls.GreenshotLabel gslblScope;
        private GreenshotPlugin.Controls.GreenshotButton buttonCancel;
        private GreenshotPlugin.Controls.GreenshotButton buttonOK;
        private GreenshotPlugin.Controls.GreenshotLabel label_upload_format;
        private GreenshotPlugin.Controls.GreenshotComboBox combobox_uploadimageformat;
        private GreenshotPlugin.Controls.GreenshotTextBox gstbDefaultDomain;
        private GreenshotPlugin.Controls.GreenshotLabel gslblDefaultDomain;
        private GreenshotPlugin.Controls.GreenshotTextBox gstbImageNamePrefix;
        private GreenshotPlugin.Controls.GreenshotLabel gslblImageNamePrefix;
    }
}