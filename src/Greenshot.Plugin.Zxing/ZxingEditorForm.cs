using System;
using System.Collections.Generic;
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

        // Email Settings
        public string EmailTo { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }

        // Calendar Event Settings
        public string EventTitle { get; set; }
        public string EventLocation { get; set; }
        public string EventStart { get; set; }
        public string EventEnd { get; set; }
        public string EventDescription { get; set; }

        // Phone Settings
        public string PhoneNumber { get; set; }

        // SMS Settings
        public string SmsNumber { get; set; }
        public string SmsMessage { get; set; }

        // Geo Location Settings
        public string Latitude { get; set; }
        public string Longitude { get; set; }

        // Color Settings
        public Color ForeColor { get; set; } = Color.Black;
        public Color BackColor { get; set; } = Color.White;

        // Modern Style Setting
        public bool RoundedDots { get; set; } = false;

        public string GetPayloadString()
        {
            if (FormatIndex != 0)
            {
                return RawText ?? string.Empty;
            }

            switch (QrCategoryIndex)
            {
                case 0: // Text/URL
                    return RawText ?? string.Empty;

                case 1: // WiFi
                    string enc = WifiEncryptionIndex == 1 ? "WEP" : (WifiEncryptionIndex == 2 ? "nopass" : "WPA");
                    return $"WIFI:S:{WifiSsid};T:{enc};P:{WifiPassword};;";

                case 2: // vCard
                    return "BEGIN:VCARD\r\nVERSION:3.0\r\n" +
                           $"N:{VcardLastName};{VcardFirstName}\r\n" +
                           $"FN:{VcardFirstName} {VcardLastName}\r\n" +
                           $"ORG:{VcardCompany}\r\n" +
                           $"TEL;TYPE=CELL:{VcardPhone}\r\n" +
                           $"EMAIL:{VcardEmail}\r\n" +
                           $"URL:{VcardUrl}\r\nEND:VCARD";

                case 3: // EPC transaction data
                    string formattedAmount = string.Empty;
                    if (double.TryParse(EpcAmount, out double amt))
                    {
                        formattedAmount = string.Format(System.Globalization.CultureInfo.InvariantCulture, "EUR{0:F2}", amt);
                    }
                    else if (!string.IsNullOrEmpty(EpcAmount))
                    {
                        formattedAmount = EpcAmount.StartsWith("EUR", StringComparison.OrdinalIgnoreCase) ? EpcAmount : $"EUR{EpcAmount}";
                    }

                    return "BCD\n" +
                           "002\n" +
                           "1\n" +
                           "SCT\n" +
                           $"{EpcBic}\n" +
                           $"{EpcName}\n" +
                           $"{EpcIban}\n" +
                           $"{formattedAmount}\n" +
                           "\n" +
                           $"{EpcReference}\n" +
                           $"{EpcMessage}\n";

                case 4: // Email
                    string mailto = $"mailto:{EmailTo}";
                    var query = new List<string>();
                    if (!string.IsNullOrEmpty(EmailSubject)) query.Add($"subject={Uri.EscapeDataString(EmailSubject)}");
                    if (!string.IsNullOrEmpty(EmailBody)) query.Add($"body={Uri.EscapeDataString(EmailBody)}");
                    if (query.Count > 0) mailto += "?" + string.Join("&", query);
                    return mailto;

                case 5: // Calendar Event
                    return "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\n" +
                           $"SUMMARY:{EventTitle}\r\n" +
                           $"LOCATION:{EventLocation}\r\n" +
                           $"DESCRIPTION:{EventDescription}\r\n" +
                           (!string.IsNullOrEmpty(EventStart) ? $"DTSTART:{EventStart}\r\n" : "") +
                           (!string.IsNullOrEmpty(EventEnd) ? $"DTEND:{EventEnd}\r\n" : "") +
                           "END:VEVENT\r\nEND:VCALENDAR";

                case 6: // Phone
                    return $"tel:{PhoneNumber}";

                case 7: // SMS
                    return $"smsto:{SmsNumber}:{SmsMessage}";

                case 8: // Geo Location
                    return $"geo:{Latitude},{Longitude}";

                default:
                    return RawText ?? string.Empty;
            }
        }

        public void OnDoubleClick(Greenshot.Base.Interfaces.Drawing.IDrawableContainer container)
        {
            Form ownerForm = null;
            if (container.Parent is Control ctrl)
            {
                ownerForm = ctrl.FindForm();
            }

            using (var form = new ZxingEditorForm(this))
            {
                if (form.ShowDialog(ownerForm) == DialogResult.OK)
                {
                    form.PopulateModel(this);
                    if (container is BarcodeContainer barcodeContainer)
                    {
                        barcodeContainer.RegenerateBarcode();
                        barcodeContainer.Invalidate();
                    }
                    else if (container is Greenshot.Base.Interfaces.Drawing.IImageContainer imageContainer && form.GeneratedBitmap != null)
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

        private Panel pnlEmail;
        private TextBox txtEmailTo;
        private TextBox txtEmailSubject;
        private TextBox txtEmailBody;

        private Panel pnlCalendar;
        private TextBox txtEventTitle;
        private TextBox txtEventLocation;
        private TextBox txtEventStart;
        private TextBox txtEventEnd;
        private TextBox txtEventDescription;

        private Panel pnlPhone;
        private TextBox txtPhoneNumber;

        private Panel pnlSms;
        private TextBox txtSmsNumber;
        private TextBox txtSmsMessage;

        private Panel pnlGeo;
        private TextBox txtLatitude;
        private TextBox txtLongitude;

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

            txtEmailTo.Text = model.EmailTo;
            txtEmailSubject.Text = model.EmailSubject;
            txtEmailBody.Text = model.EmailBody;

            txtEventTitle.Text = model.EventTitle;
            txtEventLocation.Text = model.EventLocation;
            txtEventStart.Text = model.EventStart;
            txtEventEnd.Text = model.EventEnd;
            txtEventDescription.Text = model.EventDescription;

            txtPhoneNumber.Text = model.PhoneNumber;

            txtSmsNumber.Text = model.SmsNumber;
            txtSmsMessage.Text = model.SmsMessage;

            txtLatitude.Text = model.Latitude;
            txtLongitude.Text = model.Longitude;

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
            cmbQrCategory.Items.Add("Email");
            cmbQrCategory.Items.Add("Calendar Event");
            cmbQrCategory.Items.Add("Phone Call");
            cmbQrCategory.Items.Add("SMS");
            cmbQrCategory.Items.Add("Geo Location");

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

            // Panel 5: Email Message
            pnlEmail = new Panel { Dock = DockStyle.Fill, Visible = false };
            yOffset = 10;
            pnlEmail.Controls.Add(new Label { Text = "To Address:", Location = new Point(10, yOffset), Width = 100 });
            txtEmailTo = new TextBox { Location = new Point(120, yOffset - 3), Width = 230 };
            pnlEmail.Controls.Add(txtEmailTo);

            yOffset += 40;
            pnlEmail.Controls.Add(new Label { Text = "Subject:", Location = new Point(10, yOffset), Width = 100 });
            txtEmailSubject = new TextBox { Location = new Point(120, yOffset - 3), Width = 230 };
            pnlEmail.Controls.Add(txtEmailSubject);

            yOffset += 40;
            pnlEmail.Controls.Add(new Label { Text = "Body:", Location = new Point(10, yOffset), Width = 100 });
            txtEmailBody = new TextBox { Location = new Point(10, yOffset + 25), Width = 340, Height = 120, Multiline = true, ScrollBars = ScrollBars.Vertical };
            pnlEmail.Controls.Add(txtEmailBody);
            grpInputs.Controls.Add(pnlEmail);

            // Panel 6: Calendar Event
            pnlCalendar = new Panel { Dock = DockStyle.Fill, Visible = false };
            yOffset = 10;
            pnlCalendar.Controls.Add(new Label { Text = "Title / Summary:", Location = new Point(10, yOffset), Width = 100 });
            txtEventTitle = new TextBox { Location = new Point(120, yOffset - 3), Width = 230 };
            pnlCalendar.Controls.Add(txtEventTitle);

            yOffset += 40;
            pnlCalendar.Controls.Add(new Label { Text = "Location:", Location = new Point(10, yOffset), Width = 100 });
            txtEventLocation = new TextBox { Location = new Point(120, yOffset - 3), Width = 230 };
            pnlCalendar.Controls.Add(txtEventLocation);

            yOffset += 40;
            pnlCalendar.Controls.Add(new Label { Text = "Start (UTC):", Location = new Point(10, yOffset), Width = 100 });
            txtEventStart = new TextBox { Location = new Point(120, yOffset - 3), Width = 230 };
            pnlCalendar.Controls.Add(txtEventStart);

            yOffset += 40;
            pnlCalendar.Controls.Add(new Label { Text = "End (UTC):", Location = new Point(10, yOffset), Width = 100 });
            txtEventEnd = new TextBox { Location = new Point(120, yOffset - 3), Width = 230 };
            pnlCalendar.Controls.Add(txtEventEnd);

            yOffset += 40;
            pnlCalendar.Controls.Add(new Label { Text = "Description:", Location = new Point(10, yOffset), Width = 100 });
            txtEventDescription = new TextBox { Location = new Point(120, yOffset - 3), Width = 230 };
            pnlCalendar.Controls.Add(txtEventDescription);
            grpInputs.Controls.Add(pnlCalendar);

            // Panel 7: Phone Call
            pnlPhone = new Panel { Dock = DockStyle.Fill, Visible = false };
            yOffset = 10;
            pnlPhone.Controls.Add(new Label { Text = "Phone Number:", Location = new Point(10, yOffset), Width = 100 });
            txtPhoneNumber = new TextBox { Location = new Point(120, yOffset - 3), Width = 230 };
            pnlPhone.Controls.Add(txtPhoneNumber);
            grpInputs.Controls.Add(pnlPhone);

            // Panel 8: SMS Message
            pnlSms = new Panel { Dock = DockStyle.Fill, Visible = false };
            yOffset = 10;
            pnlSms.Controls.Add(new Label { Text = "Recipient Number:", Location = new Point(10, yOffset), Width = 110 });
            txtSmsNumber = new TextBox { Location = new Point(130, yOffset - 3), Width = 220 };
            pnlSms.Controls.Add(txtSmsNumber);

            yOffset += 40;
            pnlSms.Controls.Add(new Label { Text = "Message:", Location = new Point(10, yOffset), Width = 100 });
            txtSmsMessage = new TextBox { Location = new Point(10, yOffset + 25), Width = 340, Height = 130, Multiline = true, ScrollBars = ScrollBars.Vertical };
            pnlSms.Controls.Add(txtSmsMessage);
            grpInputs.Controls.Add(pnlSms);

            // Panel 9: Geo Location
            pnlGeo = new Panel { Dock = DockStyle.Fill, Visible = false };
            yOffset = 10;
            pnlGeo.Controls.Add(new Label { Text = "Latitude:", Location = new Point(10, yOffset), Width = 100 });
            txtLatitude = new TextBox { Location = new Point(120, yOffset - 3), Width = 230 };
            pnlGeo.Controls.Add(txtLatitude);

            yOffset += 40;
            pnlGeo.Controls.Add(new Label { Text = "Longitude:", Location = new Point(10, yOffset), Width = 100 });
            txtLongitude = new TextBox { Location = new Point(120, yOffset - 3), Width = 230 };
            pnlGeo.Controls.Add(txtLongitude);
            grpInputs.Controls.Add(pnlGeo);
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

            txtEmailTo.TextChanged += (s, e) => UpdatePreview();
            txtEmailSubject.TextChanged += (s, e) => UpdatePreview();
            txtEmailBody.TextChanged += (s, e) => UpdatePreview();

            txtEventTitle.TextChanged += (s, e) => UpdatePreview();
            txtEventLocation.TextChanged += (s, e) => UpdatePreview();
            txtEventStart.TextChanged += (s, e) => UpdatePreview();
            txtEventEnd.TextChanged += (s, e) => UpdatePreview();
            txtEventDescription.TextChanged += (s, e) => UpdatePreview();

            txtPhoneNumber.TextChanged += (s, e) => UpdatePreview();

            txtSmsNumber.TextChanged += (s, e) => UpdatePreview();
            txtSmsMessage.TextChanged += (s, e) => UpdatePreview();

            txtLatitude.TextChanged += (s, e) => UpdatePreview();
            txtLongitude.TextChanged += (s, e) => UpdatePreview();

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
                case 4:
                    ShowPanel(pnlEmail);
                    grpInputs.Text = "Email Message Details";
                    break;
                case 5:
                    ShowPanel(pnlCalendar);
                    grpInputs.Text = "Calendar Event (iCal) Details";
                    break;
                case 6:
                    ShowPanel(pnlPhone);
                    grpInputs.Text = "Phone Call Details";
                    break;
                case 7:
                    ShowPanel(pnlSms);
                    grpInputs.Text = "SMS Text Message Details";
                    break;
                case 8:
                    ShowPanel(pnlGeo);
                    grpInputs.Text = "Geo Location Details";
                    break;
            }
        }

        private void ShowPanel(Panel panelToShow)
        {
            pnlRawText.Visible = (panelToShow == pnlRawText);
            pnlWifi.Visible = (panelToShow == pnlWifi);
            pnlVcard.Visible = (panelToShow == pnlVcard);
            pnlEpc.Visible = (panelToShow == pnlEpc);
            pnlEmail.Visible = (panelToShow == pnlEmail);
            pnlCalendar.Visible = (panelToShow == pnlCalendar);
            pnlPhone.Visible = (panelToShow == pnlPhone);
            pnlSms.Visible = (panelToShow == pnlSms);
            pnlGeo.Visible = (panelToShow == pnlGeo);
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
                bool is2D = (selectedFormat == BarcodeFormat.QR_CODE || 
                             selectedFormat == BarcodeFormat.AZTEC || 
                             selectedFormat == BarcodeFormat.DATA_MATRIX || 
                             selectedFormat == BarcodeFormat.PDF_417);

                int targetW = is2D ? 230 : 350;
                int targetH = is2D ? 230 : 100;

                var bmp = ZxingBarcodeGenerator.Generate(payload, selectedFormat, foreColor, backColor, chkRoundedDots.Checked, targetW, targetH);
                picPreview.Image = bmp;
                GeneratedBitmap = bmp;
                btnInsert.Enabled = (bmp != null);
            }
            catch (Exception ex)
            {
                picPreview.Image = null;
                GeneratedBitmap = null;
                btnInsert.Enabled = false;
                lblStatus.Text = "Error generating barcode:\n" + ex.Message;
            }
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
                        formattedAmount = string.Format(System.Globalization.CultureInfo.InvariantCulture, "EUR{0:F2}", amt);
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

                case 4: // Email
                    string mailto = $"mailto:{txtEmailTo.Text}";
                    var query = new List<string>();
                    if (!string.IsNullOrEmpty(txtEmailSubject.Text)) query.Add($"subject={Uri.EscapeDataString(txtEmailSubject.Text)}");
                    if (!string.IsNullOrEmpty(txtEmailBody.Text)) query.Add($"body={Uri.EscapeDataString(txtEmailBody.Text)}");
                    if (query.Count > 0) mailto += "?" + string.Join("&", query);
                    return mailto;

                case 5: // Calendar Event
                    return "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\n" +
                           $"SUMMARY:{txtEventTitle.Text}\r\n" +
                           $"LOCATION:{txtEventLocation.Text}\r\n" +
                           $"DESCRIPTION:{txtEventDescription.Text}\r\n" +
                           (!string.IsNullOrEmpty(txtEventStart.Text) ? $"DTSTART:{txtEventStart.Text}\r\n" : "") +
                           (!string.IsNullOrEmpty(txtEventEnd.Text) ? $"DTEND:{txtEventEnd.Text}\r\n" : "") +
                           "END:VEVENT\r\nEND:VCALENDAR";

                case 6: // Phone
                    return $"tel:{txtPhoneNumber.Text}";

                case 7: // SMS
                    return $"smsto:{txtSmsNumber.Text}:{txtSmsMessage.Text}";

                case 8: // Geo Location
                    return $"geo:{txtLatitude.Text},{txtLongitude.Text}";

                default:
                    return string.Empty;
            }
        }

        public static BarcodeFormat MapFormatIndex(int index)
        {
            switch (index)
            {
                case 1: return BarcodeFormat.AZTEC;
                case 2: return BarcodeFormat.DATA_MATRIX;
                case 3: return BarcodeFormat.PDF_417;
                case 4: return BarcodeFormat.CODE_128;
                case 5: return BarcodeFormat.CODE_39;
                case 6: return BarcodeFormat.CODE_93;
                case 7: return BarcodeFormat.EAN_13;
                case 8: return BarcodeFormat.EAN_8;
                case 9: return BarcodeFormat.UPC_A;
                case 10: return BarcodeFormat.UPC_E;
                case 11: return BarcodeFormat.CODABAR;
                case 12: return BarcodeFormat.ITF;
                case 13: return BarcodeFormat.MSI;
                case 14: return BarcodeFormat.PLESSEY;
                default: return BarcodeFormat.QR_CODE;
            }
        }

        public static int MapFormatToIndex(BarcodeFormat format)
        {
            switch (format)
            {
                case BarcodeFormat.AZTEC: return 1;
                case BarcodeFormat.DATA_MATRIX: return 2;
                case BarcodeFormat.PDF_417: return 3;
                case BarcodeFormat.CODE_128: return 4;
                case BarcodeFormat.CODE_39: return 5;
                case BarcodeFormat.CODE_93: return 6;
                case BarcodeFormat.EAN_13: return 7;
                case BarcodeFormat.EAN_8: return 8;
                case BarcodeFormat.UPC_A: return 9;
                case BarcodeFormat.UPC_E: return 10;
                case BarcodeFormat.CODABAR: return 11;
                case BarcodeFormat.ITF: return 12;
                case BarcodeFormat.MSI: return 13;
                case BarcodeFormat.PLESSEY: return 14;
                default: return 0;
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

            model.EmailTo = txtEmailTo.Text;
            model.EmailSubject = txtEmailSubject.Text;
            model.EmailBody = txtEmailBody.Text;

            model.EventTitle = txtEventTitle.Text;
            model.EventLocation = txtEventLocation.Text;
            model.EventStart = txtEventStart.Text;
            model.EventEnd = txtEventEnd.Text;
            model.EventDescription = txtEventDescription.Text;

            model.PhoneNumber = txtPhoneNumber.Text;

            model.SmsNumber = txtSmsNumber.Text;
            model.SmsMessage = txtSmsMessage.Text;

            model.Latitude = txtLatitude.Text;
            model.Longitude = txtLongitude.Text;

            model.ForeColor = foreColor;
            model.BackColor = backColor;
            
            model.RoundedDots = chkRoundedDots.Checked;
        }
    }
}
