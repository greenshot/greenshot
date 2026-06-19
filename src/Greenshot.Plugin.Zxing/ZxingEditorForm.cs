using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;

namespace Greenshot.Plugin.Zxing
{
    public class ZxingModel : Greenshot.Base.Interfaces.Drawing.IDoubleClickHandler
    {
        public int FormatIndex { get; set; }
        public int QrCategoryIndex { get; set; }
        
        public string RawText { get; set; }

        public string WifiSsid { get; set; }
        public string WifiPassword { get; set; }
        public int WifiEncryptionIndex { get; set; }

        public string VcardFirstName { get; set; }
        public string VcardLastName { get; set; }
        public string VcardPhone { get; set; }
        public string VcardEmail { get; set; }
        public string VcardCompany { get; set; }
        public string VcardUrl { get; set; }

        public string EpcName { get; set; }
        public string EpcIban { get; set; }
        public string EpcBic { get; set; }
        public string EpcAmount { get; set; }
        public string EpcReference { get; set; }
        public string EpcMessage { get; set; }

        // Color Settings
        public Color ForeColor { get; set; } = Color.Black;
        public Color BackColor { get; set; } = Color.White;

        // Modern Style Setting
        public bool RoundedDots { get; set; } = false;

        public void OnDoubleClick(Greenshot.Base.Interfaces.Drawing.IDrawableContainer container)
        {
            if (container is Greenshot.Base.Interfaces.Drawing.IImageContainer imageContainer)
            {
                Form ownerForm = null;
                if (container.Parent is Control ctrl)
                {
                    ownerForm = ctrl.FindForm();
                }

                using (var form = new ZxingEditorForm(this))
                {
                    if (form.ShowDialog(ownerForm) == DialogResult.OK && form.GeneratedBitmap != null)
                    {
                        imageContainer.Image = form.GeneratedBitmap;
                        imageContainer.Width = form.GeneratedBitmap.Width;
                        imageContainer.Height = form.GeneratedBitmap.Height;
                        imageContainer.Invalidate();
                    }
                }
            }
        }
    }

    public class ZxingEditorForm : Form
    {
        private ZxingModel _model;

        private ComboBox cmbFormat;
        private ComboBox cmbQrCategory;
        private GroupBox grpInputs;
        private PictureBox picPreview;
        private Button btnInsert;
        private Button btnCancel;
        private Label lblStatus;

        // Color picker controls
        private Panel pnlForeColor;
        private Button btnForeColor;
        private Panel pnlBackColor;
        private Button btnBackColor;

        // Modern style controls
        private CheckBox chkRoundedDots;

        private Color foreColor = Color.Black;
        private Color backColor = Color.White;

        // Sub-panels for input categories
        private Panel pnlRawText;
        private TextBox txtRawText;

        private Panel pnlWifi;
        private TextBox txtWifiSsid;
        private TextBox txtWifiPassword;
        private ComboBox cmbWifiEncryption;

        private Panel pnlVcard;
        private TextBox txtVcardFirstName;
        private TextBox txtVcardLastName;
        private TextBox txtVcardPhone;
        private TextBox txtVcardEmail;
        private TextBox txtVcardCompany;
        private TextBox txtVcardUrl;

        private Panel pnlEpc;
        private TextBox txtEpcName;
        private TextBox txtEpcIban;
        private TextBox txtEpcBic;
        private TextBox txtEpcAmount;
        private TextBox txtEpcReference;
        private TextBox txtEpcMessage;

        public Bitmap GeneratedBitmap { get; private set; }

        public ZxingEditorForm()
        {
            InitializeComponent();
            SetupEventHandlers();
            cmbFormat.SelectedIndex = 0; // QR Code by default
            cmbQrCategory.SelectedIndex = 0; // Text/URL by default
            UpdatePreview();
        }

        public ZxingEditorForm(ZxingModel model) : this()
        {
            _model = model;
            btnInsert.Text = "Apply";
            this.Text = "Edit QR / Barcode";
            
            // Populate form fields from the model
            cmbFormat.SelectedIndex = model.FormatIndex;
            cmbQrCategory.SelectedIndex = model.QrCategoryIndex;
            
            txtRawText.Text = model.RawText;
            
            txtWifiSsid.Text = model.WifiSsid;
            txtWifiPassword.Text = model.WifiPassword;
            cmbWifiEncryption.SelectedIndex = model.WifiEncryptionIndex;
            
            txtVcardFirstName.Text = model.VcardFirstName;
            txtVcardLastName.Text = model.VcardLastName;
            txtVcardPhone.Text = model.VcardPhone;
            txtVcardEmail.Text = model.VcardEmail;
            txtVcardCompany.Text = model.VcardCompany;
            txtVcardUrl.Text = model.VcardUrl;
            
            txtEpcName.Text = model.EpcName;
            txtEpcIban.Text = model.EpcIban;
            txtEpcBic.Text = model.EpcBic;
            txtEpcAmount.Text = model.EpcAmount;
            txtEpcReference.Text = model.EpcReference;
            txtEpcMessage.Text = model.EpcMessage;

            foreColor = model.ForeColor;
            backColor = model.BackColor;
            pnlForeColor.BackColor = foreColor;
            pnlBackColor.BackColor = backColor;

            chkRoundedDots.Checked = model.RoundedDots;
            
            UpdatePreview();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(720, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Insert QR / Barcode";

            // Format Selection
            Label lblFormat = new Label { Text = "Barcode Format:", Location = new Point(20, 20), Width = 100 };
            cmbFormat = new ComboBox { Location = new Point(130, 18), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFormat.Items.Add("QR Code");
            cmbFormat.Items.Add("Aztec (2D)");
            cmbFormat.Items.Add("Data Matrix (2D)");
            cmbFormat.Items.Add("PDF-417 (2D)");
            cmbFormat.Items.Add("Code 128 (1D)");
            cmbFormat.Items.Add("Code 39 (1D)");
            cmbFormat.Items.Add("Code 93 (1D)");
            cmbFormat.Items.Add("EAN-13 (1D)");
            cmbFormat.Items.Add("EAN-8 (1D)");
            cmbFormat.Items.Add("UPC-A (1D)");
            cmbFormat.Items.Add("UPC-E (1D)");
            cmbFormat.Items.Add("Codabar (1D)");
            cmbFormat.Items.Add("ITF (1D)");
            cmbFormat.Items.Add("MSI (1D)");
            cmbFormat.Items.Add("Plessey (1D)");

            // QR Category Selection (only visible when QR Code is selected)
            Label lblQrCategory = new Label { Text = "Category:", Location = new Point(20, 55), Width = 100, Name = "lblQrCategory" };
            cmbQrCategory = new ComboBox { Location = new Point(130, 53), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Name = "cmbQrCategory" };
            cmbQrCategory.Items.Add("Text / URL");
            cmbQrCategory.Items.Add("WiFi Network");
            cmbQrCategory.Items.Add("vCard Contact");
            cmbQrCategory.Items.Add("EPC SEPA Transfer");

            // GroupBox for dynamic input fields
            grpInputs = new GroupBox { Text = "Inputs", Location = new Point(20, 95), Size = new Size(380, 310) };

            // Dynamic Panels Setup
            SetupInputPanels();

            // Preview Side
            GroupBox grpPreview = new GroupBox { Text = "Real-time Preview", Location = new Point(420, 20), Size = new Size(260, 230) };
            picPreview = new PictureBox { Location = new Point(15, 20), Size = new Size(230, 195), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
            grpPreview.Controls.Add(picPreview);

            // Color Selection Panels and Buttons
            pnlForeColor = new Panel { Location = new Point(420, 265), Size = new Size(25, 25), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.Black };
            btnForeColor = new Button { Text = "Foreground", Location = new Point(450, 265), Width = 95, Height = 25 };
            
            pnlBackColor = new Panel { Location = new Point(560, 265), Size = new Size(25, 25), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            btnBackColor = new Button { Text = "Background", Location = new Point(590, 265), Width = 90, Height = 25 };

            // Checkbox for rounded dots
            chkRoundedDots = new CheckBox { Text = "Rounded Dots (Modern 2D style)", Location = new Point(420, 300), AutoSize = true };

            // Status label
            lblStatus = new Label { Location = new Point(420, 325), Size = new Size(260, 35), ForeColor = Color.Red, Font = new Font("Segoe UI", 9, FontStyle.Regular) };

            // Buttons
            btnInsert = new Button { Text = "Insert", Location = new Point(420, 370), Width = 110, Height = 35, DialogResult = DialogResult.OK, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnCancel = new Button { Text = "Cancel", Location = new Point(570, 370), Width = 110, Height = 35, DialogResult = DialogResult.Cancel };

            // Add all controls
            this.Controls.Add(lblFormat);
            this.Controls.Add(cmbFormat);
            this.Controls.Add(lblQrCategory);
            this.Controls.Add(cmbQrCategory);
            this.Controls.Add(grpInputs);
            this.Controls.Add(grpPreview);
            this.Controls.Add(pnlForeColor);
            this.Controls.Add(btnForeColor);
            this.Controls.Add(pnlBackColor);
            this.Controls.Add(btnBackColor);
            this.Controls.Add(chkRoundedDots);
            this.Controls.Add(lblStatus);
            this.Controls.Add(btnInsert);
            this.Controls.Add(btnCancel);
            
            this.AcceptButton = btnInsert;
            this.CancelButton = btnCancel;
        }

        private void SetupInputPanels()
        {
            // Panel 1: Raw Text
            pnlRawText = new Panel { Dock = DockStyle.Fill };
            pnlRawText.Controls.Add(new Label { Text = "Enter plain text or URL:", Location = new Point(10, 15), Width = 200 });
            txtRawText = new TextBox { Location = new Point(10, 40), Width = 340, Height = 220, Multiline = true, ScrollBars = ScrollBars.Vertical, Text = "https://getgreenshot.org" };
            pnlRawText.Controls.Add(txtRawText);
            grpInputs.Controls.Add(pnlRawText);

            // Panel 2: WiFi
            pnlWifi = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlWifi.Controls.Add(new Label { Text = "Network Name (SSID):", Location = new Point(10, 15), Width = 150 });
            txtWifiSsid = new TextBox { Location = new Point(10, 35), Width = 340 };
            pnlWifi.Controls.Add(new Label { Text = "Password:", Location = new Point(10, 75), Width = 150 });
            txtWifiPassword = new TextBox { Location = new Point(10, 95), Width = 340, PasswordChar = '*' };
            pnlWifi.Controls.Add(new Label { Text = "Encryption Type:", Location = new Point(10, 135), Width = 150 });
            cmbWifiEncryption = new ComboBox { Location = new Point(10, 155), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbWifiEncryption.Items.Add("WPA/WPA2");
            cmbWifiEncryption.Items.Add("WEP");
            cmbWifiEncryption.Items.Add("Unencrypted (None)");
            cmbWifiEncryption.SelectedIndex = 0;
            pnlWifi.Controls.Add(txtWifiSsid);
            pnlWifi.Controls.Add(txtWifiPassword);
            pnlWifi.Controls.Add(cmbWifiEncryption);
            grpInputs.Controls.Add(pnlWifi);

            // Panel 3: vCard Contact Details
            pnlVcard = new Panel { Dock = DockStyle.Fill, Visible = false };
            int yOffset = 10;
            pnlVcard.Controls.Add(new Label { Text = "First Name:", Location = new Point(10, yOffset), Width = 80 });
            txtVcardFirstName = new TextBox { Location = new Point(100, yOffset - 3), Width = 250 };
            pnlVcard.Controls.Add(txtVcardFirstName);

            yOffset += 40;
            pnlVcard.Controls.Add(new Label { Text = "Last Name:", Location = new Point(10, yOffset), Width = 80 });
            txtVcardLastName = new TextBox { Location = new Point(100, yOffset - 3), Width = 250 };
            pnlVcard.Controls.Add(txtVcardLastName);

            yOffset += 40;
            pnlVcard.Controls.Add(new Label { Text = "Phone:", Location = new Point(10, yOffset), Width = 80 });
            txtVcardPhone = new TextBox { Location = new Point(100, yOffset - 3), Width = 250 };
            pnlVcard.Controls.Add(txtVcardPhone);

            yOffset += 40;
            pnlVcard.Controls.Add(new Label { Text = "Email:", Location = new Point(10, yOffset), Width = 80 });
            txtVcardEmail = new TextBox { Location = new Point(100, yOffset - 3), Width = 250 };
            pnlVcard.Controls.Add(txtVcardEmail);

            yOffset += 40;
            pnlVcard.Controls.Add(new Label { Text = "Company:", Location = new Point(10, yOffset), Width = 80 });
            txtVcardCompany = new TextBox { Location = new Point(100, yOffset - 3), Width = 250 };
            pnlVcard.Controls.Add(txtVcardCompany);

            yOffset += 40;
            pnlVcard.Controls.Add(new Label { Text = "URL:", Location = new Point(10, yOffset), Width = 80 });
            txtVcardUrl = new TextBox { Location = new Point(100, yOffset - 3), Width = 250 };
            pnlVcard.Controls.Add(txtVcardUrl);
            grpInputs.Controls.Add(pnlVcard);

            // Panel 4: EPC Transaction Data
            pnlEpc = new Panel { Dock = DockStyle.Fill, Visible = false };
            yOffset = 10;
            pnlEpc.Controls.Add(new Label { Text = "Recipient Name:", Location = new Point(10, yOffset), Width = 100 });
            txtEpcName = new TextBox { Location = new Point(120, yOffset - 3), Width = 230 };
            pnlEpc.Controls.Add(txtEpcName);

            yOffset += 40;
            pnlEpc.Controls.Add(new Label { Text = "IBAN:", Location = new Point(10, yOffset), Width = 100 });
            txtEpcIban = new TextBox { Location = new Point(120, yOffset - 3), Width = 230 };
            pnlEpc.Controls.Add(txtEpcIban);

            yOffset += 40;
            pnlEpc.Controls.Add(new Label { Text = "BIC (optional):", Location = new Point(10, yOffset), Width = 100 });
            txtEpcBic = new TextBox { Location = new Point(120, yOffset - 3), Width = 230 };
            pnlEpc.Controls.Add(txtEpcBic);

            yOffset += 40;
            pnlEpc.Controls.Add(new Label { Text = "Amount (EUR):", Location = new Point(10, yOffset), Width = 100 });
            txtEpcAmount = new TextBox { Location = new Point(120, yOffset - 3), Width = 230, Text = "10.00" };
            pnlEpc.Controls.Add(txtEpcAmount);

            yOffset += 40;
            pnlEpc.Controls.Add(new Label { Text = "Reference:", Location = new Point(10, yOffset), Width = 100 });
            txtEpcReference = new TextBox { Location = new Point(120, yOffset - 3), Width = 230 };
            pnlEpc.Controls.Add(txtEpcReference);

            yOffset += 40;
            pnlEpc.Controls.Add(new Label { Text = "Message:", Location = new Point(10, yOffset), Width = 100 });
            txtEpcMessage = new TextBox { Location = new Point(120, yOffset - 3), Width = 230 };
            pnlEpc.Controls.Add(txtEpcMessage);
            grpInputs.Controls.Add(pnlEpc);
        }

        private void SetupEventHandlers()
        {
            cmbFormat.SelectedIndexChanged += (s, e) =>
            {
                bool isQr = cmbFormat.SelectedIndex == 0;
                cmbQrCategory.Visible = isQr;
                this.Controls["lblQrCategory"].Visible = isQr;
                
                // Show rounded dots option only for 2D barcodes
                BarcodeFormat selectedFormat = MapBarcodeFormat(cmbFormat.Text);
                bool is2D = (selectedFormat == BarcodeFormat.QR_CODE || 
                             selectedFormat == BarcodeFormat.AZTEC || 
                             selectedFormat == BarcodeFormat.DATA_MATRIX || 
                             selectedFormat == BarcodeFormat.PDF_417);
                chkRoundedDots.Visible = is2D;

                // Switch panel based on selection
                if (!isQr)
                {
                    ShowPanel(pnlRawText);
                    grpInputs.Text = "Text / Number Value";
                }
                else
                {
                    SwitchQrCategory();
                }
                UpdatePreview();
            };

            cmbQrCategory.SelectedIndexChanged += (s, e) =>
            {
                SwitchQrCategory();
                UpdatePreview();
            };

            btnForeColor.Click += (s, e) =>
            {
                using (var cd = new ColorDialog { Color = foreColor })
                {
                    if (cd.ShowDialog(this) == DialogResult.OK)
                    {
                        foreColor = cd.Color;
                        pnlForeColor.BackColor = foreColor;
                        UpdatePreview();
                    }
                }
            };

            btnBackColor.Click += (s, e) =>
            {
                using (var cd = new ColorDialog { Color = backColor })
                {
                    if (cd.ShowDialog(this) == DialogResult.OK)
                    {
                        backColor = cd.Color;
                        pnlBackColor.BackColor = backColor;
                        UpdatePreview();
                    }
                }
            };

            chkRoundedDots.CheckedChanged += (s, e) => UpdatePreview();

            // Hook text change events to trigger real-time preview updates
            txtRawText.TextChanged += (s, e) => UpdatePreview();
            txtWifiSsid.TextChanged += (s, e) => UpdatePreview();
            txtWifiPassword.TextChanged += (s, e) => UpdatePreview();
            cmbWifiEncryption.SelectedIndexChanged += (s, e) => UpdatePreview();

            txtVcardFirstName.TextChanged += (s, e) => UpdatePreview();
            txtVcardLastName.TextChanged += (s, e) => UpdatePreview();
            txtVcardPhone.TextChanged += (s, e) => UpdatePreview();
            txtVcardEmail.TextChanged += (s, e) => UpdatePreview();
            txtVcardCompany.TextChanged += (s, e) => UpdatePreview();
            txtVcardUrl.TextChanged += (s, e) => UpdatePreview();

            txtEpcName.TextChanged += (s, e) => UpdatePreview();
            txtEpcIban.TextChanged += (s, e) => UpdatePreview();
            txtEpcBic.TextChanged += (s, e) => UpdatePreview();
            txtEpcAmount.TextChanged += (s, e) => UpdatePreview();
            txtEpcReference.TextChanged += (s, e) => UpdatePreview();
            txtEpcMessage.TextChanged += (s, e) => UpdatePreview();

            btnInsert.Click += (s, e) =>
            {
                if (_model != null)
                {
                    SaveToModel(_model);
                }
            };
        }

        private void SwitchQrCategory()
        {
            switch (cmbQrCategory.SelectedIndex)
            {
                case 0:
                    ShowPanel(pnlRawText);
                    grpInputs.Text = "Raw Text or URL Details";
                    break;
                case 1:
                    ShowPanel(pnlWifi);
                    grpInputs.Text = "WiFi Network Connection Details";
                    break;
                case 2:
                    ShowPanel(pnlVcard);
                    grpInputs.Text = "vCard Contact Card Details";
                    break;
                case 3:
                    ShowPanel(pnlEpc);
                    grpInputs.Text = "EPC SEPA Credit Transfer Transaction Details";
                    break;
            }
        }

        private void ShowPanel(Panel panelToShow)
        {
            pnlRawText.Visible = (panelToShow == pnlRawText);
            pnlWifi.Visible = (panelToShow == pnlWifi);
            pnlVcard.Visible = (panelToShow == pnlVcard);
            pnlEpc.Visible = (panelToShow == pnlEpc);
        }

        private void UpdatePreview()
        {
            lblStatus.Text = string.Empty;
            string payload = GetPayloadString();

            if (string.IsNullOrEmpty(payload))
            {
                picPreview.Image = null;
                GeneratedBitmap = null;
                btnInsert.Enabled = false;
                return;
            }

            BarcodeFormat selectedFormat = MapBarcodeFormat(cmbFormat.Text);
            try
            {
                var hints = new Dictionary<EncodeHintType, object>
                {
                    { EncodeHintType.CHARACTER_SET, "UTF-8" },
                    { EncodeHintType.MARGIN, 4 }
                };

                var writer = new MultiFormatWriter();
                var matrix = writer.encode(payload, selectedFormat, 0, 0, hints);

                var bmp = RenderMatrix(matrix, foreColor, backColor, chkRoundedDots.Checked, selectedFormat);
                picPreview.Image = bmp;
                GeneratedBitmap = bmp;
                btnInsert.Enabled = true;
            }
            catch (Exception ex)
            {
                picPreview.Image = null;
                GeneratedBitmap = null;
                btnInsert.Enabled = false;
                lblStatus.Text = "Error generating barcode:\n" + ex.Message;
            }
        }

        private Bitmap RenderMatrix(BitMatrix matrix, Color fore, Color back, bool rounded, BarcodeFormat format)
        {
            int width = matrix.Width;
            int height = matrix.Height;
            
            bool is2D = (format == BarcodeFormat.QR_CODE || 
                         format == BarcodeFormat.AZTEC || 
                         format == BarcodeFormat.DATA_MATRIX || 
                         format == BarcodeFormat.PDF_417);
                         
            int targetWidth = is2D ? 230 : 350;
            int targetHeight = is2D ? 230 : 100;
            
            float scaleX = (float)targetWidth / width;
            float scaleY = (float)targetHeight / height;
            
            if (is2D)
            {
                float minScale = Math.Min(scaleX, scaleY);
                scaleX = minScale;
                scaleY = minScale;
                targetWidth = (int)Math.Round(width * scaleX);
                targetHeight = (int)Math.Round(height * scaleY);
            }
            
            var bmp = new Bitmap(targetWidth, targetHeight);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(back);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                using (var brush = new SolidBrush(fore))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            if (matrix[x, y])
                            {
                                float px = x * scaleX;
                                float py = y * scaleY;
                                
                                if (rounded && is2D)
                                {
                                    bool isFinder = false;
                                    if (format == BarcodeFormat.QR_CODE)
                                    {
                                        int margin = 4;
                                        if (x >= margin && x < margin + 7 && y >= margin && y < margin + 7) isFinder = true;
                                        else if (x >= width - margin - 7 && x < width - margin && y >= margin && y < margin + 7) isFinder = true;
                                        else if (x >= margin && x < margin + 7 && y >= height - margin - 7 && y < height - margin) isFinder = true;
                                    }
                                    
                                    if (isFinder)
                                    {
                                        g.FillRectangle(brush, px, py, scaleX, scaleY);
                                    }
                                    else
                                    {
                                        float sizeX = scaleX - 0.5f;
                                        float sizeY = scaleY - 0.5f;
                                        g.FillEllipse(brush, px + 0.25f, py + 0.25f, sizeX, sizeY);
                                    }
                                }
                                else
                                {
                                    g.FillRectangle(brush, px, py, scaleX, scaleY);
                                }
                            }
                        }
                    }
                }
            }
            
            return bmp;
        }

        private string GetPayloadString()
        {
            if (cmbFormat.SelectedIndex != 0)
            {
                // Simple raw text for general barcodes
                return txtRawText.Text;
            }

            // QR Code specific formatting
            switch (cmbQrCategory.SelectedIndex)
            {
                case 0: // Text/URL
                    return txtRawText.Text;

                case 1: // WiFi
                    string enc = "WPA";
                    if (cmbWifiEncryption.SelectedIndex == 1) enc = "WEP";
                    else if (cmbWifiEncryption.SelectedIndex == 2) enc = "nopass";
                    return $"WIFI:S:{txtWifiSsid.Text};T:{enc};P:{txtWifiPassword.Text};;";

                case 2: // vCard
                    return "BEGIN:VCARD\r\n" +
                           "VERSION:3.0\r\n" +
                           $"N:{txtVcardLastName.Text};{txtVcardFirstName.Text}\r\n" +
                           $"FN:{txtVcardFirstName.Text} {txtVcardLastName.Text}\r\n" +
                           $"ORG:{txtVcardCompany.Text}\r\n" +
                           $"TEL;TYPE=CELL:{txtVcardPhone.Text}\r\n" +
                           $"EMAIL:{txtVcardEmail.Text}\r\n" +
                           $"URL:{txtVcardUrl.Text}\r\n" +
                           "END:VCARD";

                case 3: // EPC transaction data
                    string formattedAmount = string.Empty;
                    if (double.TryParse(txtEpcAmount.Text, out double amt))
                    {
                        formattedAmount = $"EUR{amt:F2}";
                    }
                    else if (!string.IsNullOrEmpty(txtEpcAmount.Text))
                    {
                        formattedAmount = $"EUR{txtEpcAmount.Text}";
                    }

                    return "BCD\n" +
                           "002\n" +
                           "1\n" +
                           "SCT\n" +
                           $"{txtEpcBic.Text}\n" +
                           $"{txtEpcName.Text}\n" +
                           $"{txtEpcIban.Text}\n" +
                           $"{formattedAmount}\n" +
                           "\n" + // Empty purpose code
                           $"{txtEpcReference.Text}\n" +
                           $"{txtEpcMessage.Text}\n";

                default:
                    return string.Empty;
            }
        }

        private BarcodeFormat MapBarcodeFormat(string name)
        {
            switch (name)
            {
                case "Aztec (2D)": return BarcodeFormat.AZTEC;
                case "Data Matrix (2D)": return BarcodeFormat.DATA_MATRIX;
                case "PDF-417 (2D)": return BarcodeFormat.PDF_417;
                case "Code 128 (1D)": return BarcodeFormat.CODE_128;
                case "Code 39 (1D)": return BarcodeFormat.CODE_39;
                case "Code 93 (1D)": return BarcodeFormat.CODE_93;
                case "EAN-13 (1D)": return BarcodeFormat.EAN_13;
                case "EAN-8 (1D)": return BarcodeFormat.EAN_8;
                case "UPC-A (1D)": return BarcodeFormat.UPC_A;
                case "UPC-E (1D)": return BarcodeFormat.UPC_E;
                case "Codabar (1D)": return BarcodeFormat.CODABAR;
                case "ITF (1D)": return BarcodeFormat.ITF;
                case "MSI (1D)": return BarcodeFormat.MSI;
                case "Plessey (1D)": return BarcodeFormat.PLESSEY;
                default: return BarcodeFormat.QR_CODE;
            }
        }

        public void PopulateModel(ZxingModel model)
        {
            SaveToModel(model);
        }

        private void SaveToModel(ZxingModel model)
        {
            model.FormatIndex = cmbFormat.SelectedIndex;
            model.QrCategoryIndex = cmbQrCategory.SelectedIndex;
            
            model.RawText = txtRawText.Text;
            
            model.WifiSsid = txtWifiSsid.Text;
            model.WifiPassword = txtWifiPassword.Text;
            model.WifiEncryptionIndex = cmbWifiEncryption.SelectedIndex;
            
            model.VcardFirstName = txtVcardFirstName.Text;
            model.VcardLastName = txtVcardLastName.Text;
            model.VcardPhone = txtVcardPhone.Text;
            model.VcardEmail = txtVcardEmail.Text;
            model.VcardCompany = txtVcardCompany.Text;
            model.VcardUrl = txtVcardUrl.Text;
            
            model.EpcName = txtEpcName.Text;
            model.EpcIban = txtEpcIban.Text;
            model.EpcBic = txtEpcBic.Text;
            model.EpcAmount = txtEpcAmount.Text;
            model.EpcReference = txtEpcReference.Text;
            model.EpcMessage = txtEpcMessage.Text;

            model.ForeColor = foreColor;
            model.BackColor = backColor;
            
            model.RoundedDots = chkRoundedDots.Checked;
        }
    }
}
