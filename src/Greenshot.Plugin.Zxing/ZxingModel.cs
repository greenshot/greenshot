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
using System.Windows.Forms;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Plugin.Zxing.Views;

namespace Greenshot.Plugin.Zxing
{
    public class ZxingModel : IDoubleClickHandler
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

        // Quiet zone margin (modules of padding around barcode)
        public int Margin { get; set; } = 1;

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

        public void OnDoubleClick(IDrawableContainer container)
        {
            Form ownerForm = null;
            if (container.Parent is Control ctrl)
            {
                ownerForm = ctrl.FindForm();
            }

            var window = new ZxingEditorWindow(this);
            if (window.ShowDialog(ownerForm) == true)
            {
                window.PopulateModel(this);
                if (container is BarcodeContainer barcodeContainer)
                {
                    barcodeContainer.RegenerateBarcode();
                    barcodeContainer.Invalidate();
                }
                else if (container is IImageContainer imageContainer && window.GeneratedBitmap != null)
                {
                    imageContainer.Image = window.GeneratedBitmap;
                    imageContainer.Width = window.GeneratedBitmap.Width;
                    imageContainer.Height = window.GeneratedBitmap.Height;
                    imageContainer.Invalidate();
                }
            }
        }
    }
}
