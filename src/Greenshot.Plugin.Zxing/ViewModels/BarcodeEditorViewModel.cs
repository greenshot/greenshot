/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using Greenshot.Base.Languages;
using Greenshot.Plugin.Zxing.Drawing;
using Greenshot.Plugin.Zxing.Processing;

namespace Greenshot.Plugin.Zxing.ViewModels
{
    /// <summary>
    /// The fields of a barcode: format, content for the kind of QR code, colors and margin, with a preview and the hint or error.
    /// Works on a <see cref="ZxingModel"/>, which it changes directly. No UI toolkit here: shown by the BarcodeEditorView in the
    /// Barcode panel, the Barcode section and the window for the options of a recipe step.
    /// </summary>
    public sealed class BarcodeEditorViewModel : INotifyPropertyChanged
    {
        private static IZxingLanguage ZxingTexts => Texts.Get<IZxingLanguage>();

        private ZxingModel _model;

        /// <param name="model">the barcode to change</param>
        /// <param name="showsPreview">false: no preview is made, e.g. when the barcode itself is shown in the capture</param>
        public BarcodeEditorViewModel(ZxingModel model, bool showsPreview = true)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            ShowsPreview = showsPreview;
            Update();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// A field was changed by the user, with the name of its property. The model has the new value.
        /// </summary>
        public event EventHandler<string> FieldChanged;

        /// <summary>
        /// The barcode which is changed. Setting another one shows its values.
        /// </summary>
        public ZxingModel Model
        {
            get => _model;
            set
            {
                _model = value ?? throw new ArgumentNullException(nameof(value));
                Update();
                // All fields show the other barcode
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            }
        }

        public bool ShowsPreview { get; }

        /// <summary>
        /// The names of the formats, in the order of <see cref="ZxingBarcodeGenerator.MapFormatIndex"/>
        /// </summary>
        public IReadOnlyList<string> Formats { get; } = new[]
        {
            "QR Code", "Aztec (2D)", "Data Matrix (2D)", "PDF-417 (2D)", "Code 128 (1D)", "Code 39 (1D)", "Code 93 (1D)", "EAN-13 (1D)",
            "EAN-8 (1D)", "UPC-A (1D)", "UPC-E (1D)", "Codabar (1D)", "ITF (1D)", "MSI (1D)", "Plessey (1D)"
        };

        /// <summary>
        /// The kinds of QR codes, in the order of <see cref="ZxingModel.QrCategoryIndex"/>
        /// </summary>
        public IReadOnlyList<string> QrCategories { get; } = new[]
        {
            ZxingTexts.CategoryText, ZxingTexts.CategoryWifi, ZxingTexts.CategoryVcard, ZxingTexts.CategoryEpc, ZxingTexts.CategoryEmail,
            ZxingTexts.CategoryCalendar, ZxingTexts.CategoryPhone, ZxingTexts.CategorySms, ZxingTexts.CategoryGeo
        };

        public IReadOnlyList<string> WifiEncryptions { get; } = new[] { "WPA/WPA2", "WEP", ZxingTexts.WifiUnencrypted };

        /// <summary>
        /// The margins to choose from, in modules
        /// </summary>
        public IReadOnlyList<int> Margins { get; } = Enumerable.Range(0, 11).ToList();

        public int FormatIndex { get => _model.FormatIndex; set => Set(_model.FormatIndex, value, v => _model.FormatIndex = v); }
        public int QrCategoryIndex { get => _model.QrCategoryIndex; set => Set(_model.QrCategoryIndex, value, v => _model.QrCategoryIndex = v); }
        public string RawText { get => _model.RawText; set => Set(_model.RawText, value, v => _model.RawText = v); }
        public string WifiSsid { get => _model.WifiSsid; set => Set(_model.WifiSsid, value, v => _model.WifiSsid = v); }
        public string WifiPassword { get => _model.WifiPassword; set => Set(_model.WifiPassword, value, v => _model.WifiPassword = v); }
        public int WifiEncryptionIndex { get => _model.WifiEncryptionIndex; set => Set(_model.WifiEncryptionIndex, value, v => _model.WifiEncryptionIndex = v); }
        public string VcardFirstName { get => _model.VcardFirstName; set => Set(_model.VcardFirstName, value, v => _model.VcardFirstName = v); }
        public string VcardLastName { get => _model.VcardLastName; set => Set(_model.VcardLastName, value, v => _model.VcardLastName = v); }
        public string VcardPhone { get => _model.VcardPhone; set => Set(_model.VcardPhone, value, v => _model.VcardPhone = v); }
        public string VcardEmail { get => _model.VcardEmail; set => Set(_model.VcardEmail, value, v => _model.VcardEmail = v); }
        public string VcardCompany { get => _model.VcardCompany; set => Set(_model.VcardCompany, value, v => _model.VcardCompany = v); }
        public string VcardUrl { get => _model.VcardUrl; set => Set(_model.VcardUrl, value, v => _model.VcardUrl = v); }
        public string EpcName { get => _model.EpcName; set => Set(_model.EpcName, value, v => _model.EpcName = v); }
        public string EpcIban { get => _model.EpcIban; set => Set(_model.EpcIban, value, v => _model.EpcIban = v); }
        public string EpcBic { get => _model.EpcBic; set => Set(_model.EpcBic, value, v => _model.EpcBic = v); }
        public string EpcAmount { get => _model.EpcAmount; set => Set(_model.EpcAmount, value, v => _model.EpcAmount = v); }
        public string EpcReference { get => _model.EpcReference; set => Set(_model.EpcReference, value, v => _model.EpcReference = v); }
        public string EpcMessage { get => _model.EpcMessage; set => Set(_model.EpcMessage, value, v => _model.EpcMessage = v); }
        public string EmailTo { get => _model.EmailTo; set => Set(_model.EmailTo, value, v => _model.EmailTo = v); }
        public string EmailSubject { get => _model.EmailSubject; set => Set(_model.EmailSubject, value, v => _model.EmailSubject = v); }
        public string EmailBody { get => _model.EmailBody; set => Set(_model.EmailBody, value, v => _model.EmailBody = v); }
        public string EventTitle { get => _model.EventTitle; set => Set(_model.EventTitle, value, v => _model.EventTitle = v); }
        public string EventLocation { get => _model.EventLocation; set => Set(_model.EventLocation, value, v => _model.EventLocation = v); }
        public string EventStart { get => _model.EventStart; set => Set(_model.EventStart, value, v => _model.EventStart = v); }
        public string EventEnd { get => _model.EventEnd; set => Set(_model.EventEnd, value, v => _model.EventEnd = v); }
        public string EventDescription { get => _model.EventDescription; set => Set(_model.EventDescription, value, v => _model.EventDescription = v); }
        public string PhoneNumber { get => _model.PhoneNumber; set => Set(_model.PhoneNumber, value, v => _model.PhoneNumber = v); }
        public string SmsNumber { get => _model.SmsNumber; set => Set(_model.SmsNumber, value, v => _model.SmsNumber = v); }
        public string SmsMessage { get => _model.SmsMessage; set => Set(_model.SmsMessage, value, v => _model.SmsMessage = v); }
        public string Latitude { get => _model.Latitude; set => Set(_model.Latitude, value, v => _model.Latitude = v); }
        public string Longitude { get => _model.Longitude; set => Set(_model.Longitude, value, v => _model.Longitude = v); }
        public Color ForeColor { get => _model.ForeColor; set => Set(_model.ForeColor, value, v => _model.ForeColor = v); }
        public Color BackColor { get => _model.BackColor; set => Set(_model.BackColor, value, v => _model.BackColor = v); }
        public bool RoundedDots { get => _model.RoundedDots; set => Set(_model.RoundedDots, value, v => _model.RoundedDots = v); }
        public int Margin { get => Math.Max(0, Math.Min(10, _model.Margin)); set => Set(_model.Margin, value, v => _model.Margin = v); }

        /// <summary>
        /// A QR code: it has kinds (WiFi, contact...)
        /// </summary>
        public bool IsQrCode => _model.FormatIndex == 0;

        /// <summary>
        /// A 2D code, it can have rounded dots
        /// </summary>
        public bool Is2D => ZxingBarcodeGenerator.Is2D(ZxingBarcodeGenerator.MapFormatIndex(_model.FormatIndex));

        /// <summary>
        /// Which fields are shown: the kind of QR code, a plain value for the other formats
        /// </summary>
        private int InputKind => IsQrCode ? _model.QrCategoryIndex : 0;

        public bool ShowsText => InputKind is < 1 or > 8;
        public bool ShowsWifi => InputKind == 1;
        public bool ShowsVcard => InputKind == 2;
        public bool ShowsEpc => InputKind == 3;
        public bool ShowsEmail => InputKind == 4;
        public bool ShowsCalendar => InputKind == 5;
        public bool ShowsPhone => InputKind == 6;
        public bool ShowsSms => InputKind == 7;
        public bool ShowsGeo => InputKind == 8;

        /// <summary>
        /// The heading of the fields
        /// </summary>
        public string InputsHeader
        {
            get
            {
                if (!IsQrCode)
                {
                    return ZxingTexts.InputsValue;
                }

                return _model.QrCategoryIndex switch
                {
                    0 => ZxingTexts.InputsText,
                    1 => ZxingTexts.InputsWifi,
                    2 => ZxingTexts.InputsVcard,
                    3 => ZxingTexts.InputsEpc,
                    4 => ZxingTexts.InputsEmail,
                    5 => ZxingTexts.InputsCalendar,
                    6 => ZxingTexts.InputsPhone,
                    7 => ZxingTexts.InputsSms,
                    8 => ZxingTexts.InputsGeo,
                    _ => ZxingTexts.Inputs
                };
            }
        }

        /// <summary>
        /// The fields make a barcode: something to encode, which the format can encode
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// What is missing, or why the barcode can't be made, empty when it can
        /// </summary>
        public string StatusText { get; private set; } = string.Empty;

        /// <summary>
        /// The status is an error (the format can't encode it), not a hint
        /// </summary>
        public bool StatusIsError { get; private set; }

        /// <summary>
        /// The barcode as it looks, null when it can't be made or <see cref="ShowsPreview"/> is false
        /// </summary>
        public Bitmap Preview { get; private set; }

        /// <summary>
        /// The text which is encoded
        /// </summary>
        public string Payload => _model.GetPayloadString();

        private void Set<T>(T current, T value, Action<T> apply, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(current, value))
            {
                return;
            }

            apply(value);
            Update();
            // The field and everything which depends on it
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            FieldChanged?.Invoke(this, propertyName);
        }

        /// <summary>
        /// Check the fields and make the preview
        /// </summary>
        private void Update()
        {
            var oldPreview = Preview;
            Preview = null;
            IsValid = false;
            StatusIsError = false;
            StatusText = string.Empty;

            string payload = Payload;
            if (string.IsNullOrEmpty(payload))
            {
                StatusText = RequiredInputHint();
            }
            else
            {
                var format = ZxingBarcodeGenerator.MapFormatIndex(_model.FormatIndex);
                bool is2D = ZxingBarcodeGenerator.Is2D(format);
                try
                {
                    if (ShowsPreview)
                    {
                        Preview = ZxingBarcodeGenerator.Generate(payload, format, _model.ForeColor, _model.BackColor, _model.RoundedDots, is2D ? 210 : 330, is2D ? 210 : 100, Margin);
                        IsValid = Preview != null;
                    }
                    else
                    {
                        IsValid = ZxingBarcodeGenerator.CreateLayout(payload, format, _model.ForeColor, _model.BackColor, _model.RoundedDots, 1, 1, Margin) != null;
                    }
                }
                catch (Exception ex)
                {
                    StatusIsError = true;
                    StatusText = ZxingTexts.ErrorGenerating + "\n" + ex.Message;
                }
            }

            oldPreview?.Dispose();
        }

        private string RequiredInputHint()
        {
            if (!IsQrCode)
            {
                return ZxingTexts.HintValue;
            }

            return _model.QrCategoryIndex switch
            {
                0 => ZxingTexts.HintText,
                1 => ZxingTexts.HintWifi,
                2 => ZxingTexts.HintVcard,
                3 => ZxingTexts.HintEpc,
                4 => ZxingTexts.HintEmail,
                5 => ZxingTexts.HintCalendar,
                6 => ZxingTexts.HintPhone,
                7 => ZxingTexts.HintSms,
                8 => ZxingTexts.HintGeo,
                _ => ZxingTexts.HintDefault
            };
        }
    }
}
