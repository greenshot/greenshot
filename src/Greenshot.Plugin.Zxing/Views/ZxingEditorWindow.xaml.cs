/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Greenshot.Base.Core;
using Greenshot.Base.Languages;
using Greenshot.Base.Wpf;
using Greenshot.Editor.Views;
using Greenshot.Plugin.Zxing.Drawing;
using Greenshot.Plugin.Zxing.Processing;
using ZXing;
using Color = System.Drawing.Color;
using WpfColor = System.Windows.Media.Color;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace Greenshot.Plugin.Zxing.Views
{
    public partial class ZxingEditorWindow : Window
    {
        private static IZxingLanguage ZxingTexts => Texts.Get<IZxingLanguage>();

        private readonly ZxingModel _model;
        private Color _foreColor = Color.Black;
        private Color _backColor = Color.White;
        private bool _isInitializing = true;

        public Bitmap GeneratedBitmap { get; private set; }

        public ZxingEditorWindow() : this(null)
        {
        }

        public ZxingEditorWindow(ZxingModel model)
        {
            _model = model;
            InitializeComponent();

            try
            {
                var icon = GreenshotResources.GetGreenshotIcon();
                if (icon != null)
                {
                    Icon = icon.ToBitmapSource();
                }
            }
            catch
            {
                // Ignore icon loading error
            }

            InitializeDropdowns();

            if (_model != null)
            {
                BtnInsert.Content = ZxingTexts.Apply;
                Title = ZxingTexts.EditorTitleEdit;
                LoadFromModel(_model);
            }
            else
            {
                Title = ZxingTexts.EditorTitleInsert;
                CmbFormat.SelectedIndex = 0; // QR Code by default
                CmbQrCategory.SelectedIndex = 0; // Text / URL by default
                CmbMargin.SelectedIndex = 1; // Margin 1 by default
            }

            _isInitializing = false;
            UpdateCategoryVisibility();
            UpdatePreview();
        }

        private void InitializeDropdowns()
        {
            CmbFormat.Items.Add("QR Code");
            CmbFormat.Items.Add("Aztec (2D)");
            CmbFormat.Items.Add("Data Matrix (2D)");
            CmbFormat.Items.Add("PDF-417 (2D)");
            CmbFormat.Items.Add("Code 128 (1D)");
            CmbFormat.Items.Add("Code 39 (1D)");
            CmbFormat.Items.Add("Code 93 (1D)");
            CmbFormat.Items.Add("EAN-13 (1D)");
            CmbFormat.Items.Add("EAN-8 (1D)");
            CmbFormat.Items.Add("UPC-A (1D)");
            CmbFormat.Items.Add("UPC-E (1D)");
            CmbFormat.Items.Add("Codabar (1D)");
            CmbFormat.Items.Add("ITF (1D)");
            CmbFormat.Items.Add("MSI (1D)");
            CmbFormat.Items.Add("Plessey (1D)");

            CmbQrCategory.Items.Add(ZxingTexts.CategoryText);
            CmbQrCategory.Items.Add(ZxingTexts.CategoryWifi);
            CmbQrCategory.Items.Add(ZxingTexts.CategoryVcard);
            CmbQrCategory.Items.Add(ZxingTexts.CategoryEpc);
            CmbQrCategory.Items.Add(ZxingTexts.CategoryEmail);
            CmbQrCategory.Items.Add(ZxingTexts.CategoryCalendar);
            CmbQrCategory.Items.Add(ZxingTexts.CategoryPhone);
            CmbQrCategory.Items.Add(ZxingTexts.CategorySms);
            CmbQrCategory.Items.Add(ZxingTexts.CategoryGeo);

            CmbWifiEncryption.Items.Add("WPA/WPA2");
            CmbWifiEncryption.Items.Add("WEP");
            CmbWifiEncryption.Items.Add(ZxingTexts.WifiUnencrypted);
            CmbWifiEncryption.SelectedIndex = 0;

            for (int i = 0; i <= 10; i++)
            {
                CmbMargin.Items.Add(i.ToString());
            }
        }

        private void LoadFromModel(ZxingModel model)
        {
            if (model.FormatIndex >= 0 && model.FormatIndex < CmbFormat.Items.Count)
                CmbFormat.SelectedIndex = model.FormatIndex;
            if (model.QrCategoryIndex >= 0 && model.QrCategoryIndex < CmbQrCategory.Items.Count)
                CmbQrCategory.SelectedIndex = model.QrCategoryIndex;

            TxtRawText.Text = model.RawText ?? string.Empty;

            TxtWifiSsid.Text = model.WifiSsid ?? string.Empty;
            TxtWifiPassword.Text = model.WifiPassword ?? string.Empty;
            if (model.WifiEncryptionIndex >= 0 && model.WifiEncryptionIndex < CmbWifiEncryption.Items.Count)
                CmbWifiEncryption.SelectedIndex = model.WifiEncryptionIndex;

            TxtVcardFirstName.Text = model.VcardFirstName ?? string.Empty;
            TxtVcardLastName.Text = model.VcardLastName ?? string.Empty;
            TxtVcardPhone.Text = model.VcardPhone ?? string.Empty;
            TxtVcardEmail.Text = model.VcardEmail ?? string.Empty;
            TxtVcardCompany.Text = model.VcardCompany ?? string.Empty;
            TxtVcardUrl.Text = model.VcardUrl ?? string.Empty;

            TxtEpcName.Text = model.EpcName ?? string.Empty;
            TxtEpcIban.Text = model.EpcIban ?? string.Empty;
            TxtEpcBic.Text = model.EpcBic ?? string.Empty;
            TxtEpcAmount.Text = model.EpcAmount ?? string.Empty;
            TxtEpcReference.Text = model.EpcReference ?? string.Empty;
            TxtEpcMessage.Text = model.EpcMessage ?? string.Empty;

            TxtEmailTo.Text = model.EmailTo ?? string.Empty;
            TxtEmailSubject.Text = model.EmailSubject ?? string.Empty;
            TxtEmailBody.Text = model.EmailBody ?? string.Empty;

            TxtEventTitle.Text = model.EventTitle ?? string.Empty;
            TxtEventLocation.Text = model.EventLocation ?? string.Empty;
            TxtEventStart.Text = model.EventStart ?? string.Empty;
            TxtEventEnd.Text = model.EventEnd ?? string.Empty;
            TxtEventDescription.Text = model.EventDescription ?? string.Empty;

            TxtPhoneNumber.Text = model.PhoneNumber ?? string.Empty;

            TxtSmsNumber.Text = model.SmsNumber ?? string.Empty;
            TxtSmsMessage.Text = model.SmsMessage ?? string.Empty;

            TxtLatitude.Text = model.Latitude ?? string.Empty;
            TxtLongitude.Text = model.Longitude ?? string.Empty;

            _foreColor = model.ForeColor;
            _backColor = model.BackColor;
            UpdateColorSwatches();

            ChkRoundedDots.IsChecked = model.RoundedDots;

            int margin = Math.Max(0, Math.Min(10, model.Margin));
            CmbMargin.SelectedIndex = margin;
        }

        public void PopulateModel(ZxingModel model)
        {
            if (model == null) return;
            model.FormatIndex = CmbFormat.SelectedIndex;
            model.QrCategoryIndex = CmbQrCategory.SelectedIndex;

            model.RawText = TxtRawText.Text;

            model.WifiSsid = TxtWifiSsid.Text;
            model.WifiPassword = TxtWifiPassword.Text;
            model.WifiEncryptionIndex = CmbWifiEncryption.SelectedIndex;

            model.VcardFirstName = TxtVcardFirstName.Text;
            model.VcardLastName = TxtVcardLastName.Text;
            model.VcardPhone = TxtVcardPhone.Text;
            model.VcardEmail = TxtVcardEmail.Text;
            model.VcardCompany = TxtVcardCompany.Text;
            model.VcardUrl = TxtVcardUrl.Text;

            model.EpcName = TxtEpcName.Text;
            model.EpcIban = TxtEpcIban.Text;
            model.EpcBic = TxtEpcBic.Text;
            model.EpcAmount = TxtEpcAmount.Text;
            model.EpcReference = TxtEpcReference.Text;
            model.EpcMessage = TxtEpcMessage.Text;

            model.EmailTo = TxtEmailTo.Text;
            model.EmailSubject = TxtEmailSubject.Text;
            model.EmailBody = TxtEmailBody.Text;

            model.EventTitle = TxtEventTitle.Text;
            model.EventLocation = TxtEventLocation.Text;
            model.EventStart = TxtEventStart.Text;
            model.EventEnd = TxtEventEnd.Text;
            model.EventDescription = TxtEventDescription.Text;

            model.PhoneNumber = TxtPhoneNumber.Text;

            model.SmsNumber = TxtSmsNumber.Text;
            model.SmsMessage = TxtSmsMessage.Text;

            model.Latitude = TxtLatitude.Text;
            model.Longitude = TxtLongitude.Text;

            model.ForeColor = _foreColor;
            model.BackColor = _backColor;

            model.RoundedDots = ChkRoundedDots.IsChecked == true;
            model.Margin = CmbMargin.SelectedIndex >= 0 ? CmbMargin.SelectedIndex : 1;
        }

        public bool? ShowDialog(System.Windows.Forms.IWin32Window owner)
        {
            if (owner != null && owner.Handle != IntPtr.Zero)
            {
                new WindowInteropHelper(this) { Owner = owner.Handle };
            }
            return ShowDialog();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnInsert_Click(object sender, RoutedEventArgs e)
        {
            if (_model != null)
            {
                PopulateModel(_model);
            }
            DialogResult = true;
            Close();
        }

        private void CmbFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            UpdateCategoryVisibility();
            UpdatePreview();
        }

        private void CmbQrCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            UpdateCategoryVisibility();
            UpdatePreview();
        }

        private void InputControl_Changed(object sender, EventArgs e)
        {
            if (_isInitializing) return;
            UpdatePreview();
        }

        private void UpdateCategoryVisibility()
        {
            int formatIdx = CmbFormat.SelectedIndex >= 0 ? CmbFormat.SelectedIndex : 0;
            bool isQr = formatIdx == 0;
            RowQrCategory.Visibility = isQr ? Visibility.Visible : Visibility.Collapsed;

            BarcodeFormat selectedFormat = MapFormatIndex(formatIdx);
            bool is2D = (selectedFormat == BarcodeFormat.QR_CODE ||
                         selectedFormat == BarcodeFormat.AZTEC ||
                         selectedFormat == BarcodeFormat.DATA_MATRIX ||
                         selectedFormat == BarcodeFormat.PDF_417);
            ChkRoundedDots.Visibility = is2D ? Visibility.Visible : Visibility.Collapsed;

            // Hide all input panels first
            PnlRawText.Visibility = Visibility.Collapsed;
            PnlWifi.Visibility = Visibility.Collapsed;
            PnlVcard.Visibility = Visibility.Collapsed;
            PnlEpc.Visibility = Visibility.Collapsed;
            PnlEmail.Visibility = Visibility.Collapsed;
            PnlCalendar.Visibility = Visibility.Collapsed;
            PnlPhone.Visibility = Visibility.Collapsed;
            PnlSms.Visibility = Visibility.Collapsed;
            PnlGeo.Visibility = Visibility.Collapsed;

            if (!isQr)
            {
                PnlRawText.Visibility = Visibility.Visible;
                GrpInputs.Header = ZxingTexts.InputsValue;
                return;
            }

            switch (CmbQrCategory.SelectedIndex)
            {
                case 0:
                    PnlRawText.Visibility = Visibility.Visible;
                    GrpInputs.Header = ZxingTexts.InputsText;
                    break;
                case 1:
                    PnlWifi.Visibility = Visibility.Visible;
                    GrpInputs.Header = ZxingTexts.InputsWifi;
                    break;
                case 2:
                    PnlVcard.Visibility = Visibility.Visible;
                    GrpInputs.Header = ZxingTexts.InputsVcard;
                    break;
                case 3:
                    PnlEpc.Visibility = Visibility.Visible;
                    GrpInputs.Header = ZxingTexts.InputsEpc;
                    break;
                case 4:
                    PnlEmail.Visibility = Visibility.Visible;
                    GrpInputs.Header = ZxingTexts.InputsEmail;
                    break;
                case 5:
                    PnlCalendar.Visibility = Visibility.Visible;
                    GrpInputs.Header = ZxingTexts.InputsCalendar;
                    break;
                case 6:
                    PnlPhone.Visibility = Visibility.Visible;
                    GrpInputs.Header = ZxingTexts.InputsPhone;
                    break;
                case 7:
                    PnlSms.Visibility = Visibility.Visible;
                    GrpInputs.Header = ZxingTexts.InputsSms;
                    break;
                case 8:
                    PnlGeo.Visibility = Visibility.Visible;
                    GrpInputs.Header = ZxingTexts.InputsGeo;
                    break;
                default:
                    PnlRawText.Visibility = Visibility.Visible;
                    GrpInputs.Header = ZxingTexts.Inputs;
                    break;
            }
        }

        private void BtnForeColor_Click(object sender, RoutedEventArgs e)
        {
            var colorPicker = new ColorPickerWindow
            {
                Owner = this,
                SelectedColor = _foreColor
            };
            if (colorPicker.ShowDialog() == true)
            {
                _foreColor = colorPicker.SelectedColor;
                UpdateColorSwatches();
                UpdatePreview();
            }
        }

        private void BtnBackColor_Click(object sender, RoutedEventArgs e)
        {
            var colorPicker = new ColorPickerWindow
            {
                Owner = this,
                SelectedColor = _backColor
            };
            if (colorPicker.ShowDialog() == true)
            {
                _backColor = colorPicker.SelectedColor;
                UpdateColorSwatches();
                UpdatePreview();
            }
        }

        private void UpdateColorSwatches()
        {
            BdrForeColor.Background = new WpfSolidColorBrush(WpfColor.FromArgb(_foreColor.A, _foreColor.R, _foreColor.G, _foreColor.B));
            BdrBackColor.Background = new WpfSolidColorBrush(WpfColor.FromArgb(_backColor.A, _backColor.R, _backColor.G, _backColor.B));
        }

        private void UpdatePreview()
        {
            TxtStatus.Text = string.Empty;
            TxtStatus.Foreground = ThemeManager.Instance.MutedBrush;
            string payload = GetPayloadString();

            if (string.IsNullOrEmpty(payload))
            {
                ImgPreview.Source = null;
                GeneratedBitmap = null;
                BtnInsert.IsEnabled = false;
                TxtStatus.Text = GetRequiredInputHint();
                return;
            }

            int formatIdx = CmbFormat.SelectedIndex >= 0 ? CmbFormat.SelectedIndex : 0;
            BarcodeFormat selectedFormat = MapFormatIndex(formatIdx);
            try
            {
                bool is2D = (selectedFormat == BarcodeFormat.QR_CODE ||
                             selectedFormat == BarcodeFormat.AZTEC ||
                             selectedFormat == BarcodeFormat.DATA_MATRIX ||
                             selectedFormat == BarcodeFormat.PDF_417);

                int targetW = is2D ? 210 : 330;
                int targetH = is2D ? 210 : 100;
                int margin = CmbMargin.SelectedIndex >= 0 ? CmbMargin.SelectedIndex : 1;

                var bmp = ZxingBarcodeGenerator.Generate(payload, selectedFormat, _foreColor, _backColor, ChkRoundedDots.IsChecked == true, targetW, targetH, margin);
                ImgPreview.Source = bmp?.ToBitmapSource();
                GeneratedBitmap = bmp;
                BtnInsert.IsEnabled = (bmp != null);
            }
            catch (Exception ex)
            {
                ImgPreview.Source = null;
                GeneratedBitmap = null;
                BtnInsert.IsEnabled = false;
                TxtStatus.Foreground = ThemeManager.Instance.ErrorBrush;
                TxtStatus.Text = ZxingTexts.ErrorGenerating + "\n" + ex.Message;
            }
        }

        private string GetRequiredInputHint()
        {
            if (CmbFormat.SelectedIndex != 0)
            {
                return ZxingTexts.HintValue;
            }

            switch (CmbQrCategory.SelectedIndex)
            {
                case 0: return ZxingTexts.HintText;
                case 1: return ZxingTexts.HintWifi;
                case 2: return ZxingTexts.HintVcard;
                case 3: return ZxingTexts.HintEpc;
                case 4: return ZxingTexts.HintEmail;
                case 5: return ZxingTexts.HintCalendar;
                case 6: return ZxingTexts.HintPhone;
                case 7: return ZxingTexts.HintSms;
                case 8: return ZxingTexts.HintGeo;
                default: return ZxingTexts.HintDefault;
            }
        }

        public string GetPayloadString()
        {
            if (CmbFormat.SelectedIndex != 0)
            {
                return TxtRawText.Text ?? string.Empty;
            }

            switch (CmbQrCategory.SelectedIndex)
            {
                case 0: // Text/URL
                    return TxtRawText.Text ?? string.Empty;

                case 1: // WiFi
                    if (string.IsNullOrWhiteSpace(TxtWifiSsid.Text)) return string.Empty;
                    string enc = "WPA";
                    if (CmbWifiEncryption.SelectedIndex == 1) enc = "WEP";
                    else if (CmbWifiEncryption.SelectedIndex == 2) enc = "nopass";
                    return $"WIFI:S:{TxtWifiSsid.Text};T:{enc};P:{TxtWifiPassword.Text};;";

                case 2: // vCard
                    if (string.IsNullOrWhiteSpace(TxtVcardFirstName.Text) &&
                        string.IsNullOrWhiteSpace(TxtVcardLastName.Text) &&
                        string.IsNullOrWhiteSpace(TxtVcardCompany.Text) &&
                        string.IsNullOrWhiteSpace(TxtVcardPhone.Text) &&
                        string.IsNullOrWhiteSpace(TxtVcardEmail.Text))
                    {
                        return string.Empty;
                    }
                    return "BEGIN:VCARD\r\nVERSION:3.0\r\n" +
                           $"N:{TxtVcardLastName.Text};{TxtVcardFirstName.Text}\r\n" +
                           $"FN:{TxtVcardFirstName.Text} {TxtVcardLastName.Text}".Trim() + "\r\n" +
                           $"ORG:{TxtVcardCompany.Text}\r\n" +
                           $"TEL;TYPE=CELL:{TxtVcardPhone.Text}\r\n" +
                           $"EMAIL:{TxtVcardEmail.Text}\r\n" +
                           $"URL:{TxtVcardUrl.Text}\r\nEND:VCARD";

                case 3: // EPC transaction data
                    if (string.IsNullOrWhiteSpace(TxtEpcIban.Text) && string.IsNullOrWhiteSpace(TxtEpcName.Text))
                    {
                        return string.Empty;
                    }
                    string formattedAmount = string.Empty;
                    if (double.TryParse(TxtEpcAmount.Text, out double amt))
                    {
                        formattedAmount = string.Format(CultureInfo.InvariantCulture, "EUR{0:F2}", amt);
                    }
                    else if (!string.IsNullOrEmpty(TxtEpcAmount.Text))
                    {
                        formattedAmount = TxtEpcAmount.Text.StartsWith("EUR", StringComparison.OrdinalIgnoreCase) ? TxtEpcAmount.Text : $"EUR{TxtEpcAmount.Text}";
                    }

                    return "BCD\n" +
                           "002\n" +
                           "1\n" +
                           "SCT\n" +
                           $"{TxtEpcBic.Text}\n" +
                           $"{TxtEpcName.Text}\n" +
                           $"{TxtEpcIban.Text}\n" +
                           $"{formattedAmount}\n" +
                           "\n" +
                           $"{TxtEpcReference.Text}\n" +
                           $"{TxtEpcMessage.Text}\n";

                case 4: // Email
                    if (string.IsNullOrWhiteSpace(TxtEmailTo.Text) &&
                        string.IsNullOrWhiteSpace(TxtEmailSubject.Text) &&
                        string.IsNullOrWhiteSpace(TxtEmailBody.Text))
                    {
                        return string.Empty;
                    }
                    string mailto = $"mailto:{TxtEmailTo.Text}";
                    var query = new List<string>();
                    if (!string.IsNullOrEmpty(TxtEmailSubject.Text)) query.Add($"subject={Uri.EscapeDataString(TxtEmailSubject.Text)}");
                    if (!string.IsNullOrEmpty(TxtEmailBody.Text)) query.Add($"body={Uri.EscapeDataString(TxtEmailBody.Text)}");
                    if (query.Count > 0) mailto += "?" + string.Join("&", query);
                    return mailto;

                case 5: // Calendar Event
                    if (string.IsNullOrWhiteSpace(TxtEventTitle.Text))
                    {
                        return string.Empty;
                    }
                    return "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\n" +
                           $"SUMMARY:{TxtEventTitle.Text}\r\n" +
                           $"LOCATION:{TxtEventLocation.Text}\r\n" +
                           $"DESCRIPTION:{TxtEventDescription.Text}\r\n" +
                           (!string.IsNullOrEmpty(TxtEventStart.Text) ? $"DTSTART:{TxtEventStart.Text}\r\n" : "") +
                           (!string.IsNullOrEmpty(TxtEventEnd.Text) ? $"DTEND:{TxtEventEnd.Text}\r\n" : "") +
                           "END:VEVENT\r\nEND:VCALENDAR";

                case 6: // Phone
                    if (string.IsNullOrWhiteSpace(TxtPhoneNumber.Text)) return string.Empty;
                    return $"tel:{TxtPhoneNumber.Text}";

                case 7: // SMS
                    if (string.IsNullOrWhiteSpace(TxtSmsNumber.Text)) return string.Empty;
                    return $"smsto:{TxtSmsNumber.Text}:{TxtSmsMessage.Text}";

                case 8: // Geo Location
                    if (string.IsNullOrWhiteSpace(TxtLatitude.Text) && string.IsNullOrWhiteSpace(TxtLongitude.Text)) return string.Empty;
                    return $"geo:{TxtLatitude.Text},{TxtLongitude.Text}";

                default:
                    return TxtRawText.Text ?? string.Empty;
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

        private static BarcodeFormat MapBarcodeFormat(string name)
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
    }
}
