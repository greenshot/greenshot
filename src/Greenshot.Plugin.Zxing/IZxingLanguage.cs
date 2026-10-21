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

using System.ComponentModel;
using Dapplo.Ini.Internationalization.Attributes;

namespace Greenshot.Plugin.Zxing
{
    /// <summary>
    /// The texts of the [Zxing] section of greenshot.zxing.{ietf}.ini, the English text is shown per property.
    /// Generated from the language files, add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("Zxing", ModuleName = "zxing")]
    public interface IZxingLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Insert QR / Barcode
        /// </summary>
        string EditorTitleInsert { get; }

        /// <summary>
        /// Edit QR / Barcode
        /// </summary>
        string EditorTitleEdit { get; }

        /// <summary>
        /// Insert QR / Barcode...
        /// </summary>
        string EditorMenu { get; }

        /// <summary>
        /// Barcode Format:
        /// </summary>
        string BarcodeFormat { get; }

        /// <summary>
        /// QR Type:
        /// </summary>
        string QrType { get; }

        /// <summary>
        /// Inputs
        /// </summary>
        string Inputs { get; }

        /// <summary>
        /// Enter plain text or URL:
        /// </summary>
        string RawText { get; }

        /// <summary>
        /// Network Name (SSID):
        /// </summary>
        string WifiSsid { get; }

        /// <summary>
        /// Password:
        /// </summary>
        string WifiPassword { get; }

        /// <summary>
        /// Encryption Type:
        /// </summary>
        string WifiEncryption { get; }

        /// <summary>
        /// Unencrypted (None)
        /// </summary>
        string WifiUnencrypted { get; }

        /// <summary>
        /// First Name:
        /// </summary>
        string VcardFirstName { get; }

        /// <summary>
        /// Last Name:
        /// </summary>
        string VcardLastName { get; }

        /// <summary>
        /// Phone:
        /// </summary>
        string VcardPhone { get; }

        /// <summary>
        /// Email:
        /// </summary>
        string VcardEmail { get; }

        /// <summary>
        /// Company:
        /// </summary>
        string VcardCompany { get; }

        /// <summary>
        /// URL:
        /// </summary>
        string VcardUrl { get; }

        /// <summary>
        /// Recipient:
        /// </summary>
        string EpcRecipient { get; }

        /// <summary>
        /// IBAN:
        /// </summary>
        string EpcIban { get; }

        /// <summary>
        /// BIC (optional):
        /// </summary>
        string EpcBic { get; }

        /// <summary>
        /// Amount (EUR):
        /// </summary>
        string EpcAmount { get; }

        /// <summary>
        /// Reference:
        /// </summary>
        string EpcReference { get; }

        /// <summary>
        /// Message:
        /// </summary>
        string Message { get; }

        /// <summary>
        /// To:
        /// </summary>
        string EmailTo { get; }

        /// <summary>
        /// Subject:
        /// </summary>
        string EmailSubject { get; }

        /// <summary>
        /// Body:
        /// </summary>
        string EmailBody { get; }

        /// <summary>
        /// Title:
        /// </summary>
        string EventTitle { get; }

        /// <summary>
        /// Location:
        /// </summary>
        string EventLocation { get; }

        /// <summary>
        /// Start (UTC):
        /// </summary>
        string EventStart { get; }

        /// <summary>
        /// End (UTC):
        /// </summary>
        string EventEnd { get; }

        /// <summary>
        /// Description:
        /// </summary>
        string EventDescription { get; }

        /// <summary>
        /// Phone Number:
        /// </summary>
        string PhoneNumber { get; }

        /// <summary>
        /// Recipient Number:
        /// </summary>
        string SmsNumber { get; }

        /// <summary>
        /// Latitude:
        /// </summary>
        string GeoLatitude { get; }

        /// <summary>
        /// Longitude:
        /// </summary>
        string GeoLongitude { get; }

        /// <summary>
        /// Real-time Preview
        /// </summary>
        string Preview { get; }

        /// <summary>
        /// Foreground
        /// </summary>
        string Foreground { get; }

        /// <summary>
        /// Background
        /// </summary>
        string Background { get; }

        /// <summary>
        /// Rounded Dots
        /// </summary>
        string RoundedDots { get; }

        /// <summary>
        /// Margin:
        /// </summary>
        string Margin { get; }

        /// <summary>
        /// Insert
        /// </summary>
        string Insert { get; }

        /// <summary>
        /// Apply
        /// </summary>
        string Apply { get; }

        /// <summary>
        /// Text / URL
        /// </summary>
        string CategoryText { get; }

        /// <summary>
        /// WiFi Network
        /// </summary>
        string CategoryWifi { get; }

        /// <summary>
        /// vCard Contact
        /// </summary>
        string CategoryVcard { get; }

        /// <summary>
        /// EPC SEPA Transfer
        /// </summary>
        string CategoryEpc { get; }

        /// <summary>
        /// Email
        /// </summary>
        string CategoryEmail { get; }

        /// <summary>
        /// Calendar Event
        /// </summary>
        string CategoryCalendar { get; }

        /// <summary>
        /// Phone Call
        /// </summary>
        string CategoryPhone { get; }

        /// <summary>
        /// SMS
        /// </summary>
        string CategorySms { get; }

        /// <summary>
        /// Geo Location
        /// </summary>
        string CategoryGeo { get; }

        /// <summary>
        /// Text / Number Value
        /// </summary>
        string InputsValue { get; }

        /// <summary>
        /// Raw Text or URL Details
        /// </summary>
        string InputsText { get; }

        /// <summary>
        /// WiFi Network Connection Details
        /// </summary>
        string InputsWifi { get; }

        /// <summary>
        /// vCard Contact Card Details
        /// </summary>
        string InputsVcard { get; }

        /// <summary>
        /// EPC SEPA Credit Transfer Details
        /// </summary>
        string InputsEpc { get; }

        /// <summary>
        /// Email Message Details
        /// </summary>
        string InputsEmail { get; }

        /// <summary>
        /// Calendar Event (iCal) Details
        /// </summary>
        string InputsCalendar { get; }

        /// <summary>
        /// Phone Call Details
        /// </summary>
        string InputsPhone { get; }

        /// <summary>
        /// SMS Text Message Details
        /// </summary>
        string InputsSms { get; }

        /// <summary>
        /// Geo Location Details
        /// </summary>
        string InputsGeo { get; }

        /// <summary>
        /// Error generating barcode:
        /// </summary>
        string ErrorGenerating { get; }

        /// <summary>
        /// Please enter a value to generate the barcode.
        /// </summary>
        string HintValue { get; }

        /// <summary>
        /// Please enter text or a URL.
        /// </summary>
        string HintText { get; }

        /// <summary>
        /// Please enter network name (SSID).
        /// </summary>
        string HintWifi { get; }

        /// <summary>
        /// Please enter contact name or details.
        /// </summary>
        string HintVcard { get; }

        /// <summary>
        /// Please enter recipient name and IBAN.
        /// </summary>
        string HintEpc { get; }

        /// <summary>
        /// Please enter recipient email address.
        /// </summary>
        string HintEmail { get; }

        /// <summary>
        /// Please enter event title.
        /// </summary>
        string HintCalendar { get; }

        /// <summary>
        /// Please enter a phone number.
        /// </summary>
        string HintPhone { get; }

        /// <summary>
        /// Please enter recipient number.
        /// </summary>
        string HintSms { get; }

        /// <summary>
        /// Please enter latitude and longitude coordinates.
        /// </summary>
        string HintGeo { get; }

        /// <summary>
        /// Please enter required input on the left.
        /// </summary>
        string HintDefault { get; }
    }
}
