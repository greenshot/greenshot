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
using Dapplo.Ini;
using Greenshot.Base.Drawing;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Editor.Drawing;

namespace Greenshot.Plugin.Zxing;

public class ZxingPlugin : IGreenshotPlugin, IRecipeStepProvider, IRecipeDrawableProvider, IRecipeStepSchemaProvider
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ZxingPlugin));
    private static IZxingConfiguration _config;
    private ZxingCaptureProcessor _captureProcessor;
    private ZxingEditorPlugin _editorPlugin;
    private ZxingHotspotTransformer _hotspotTransformer;

    public string Name => "Zxing";

    public bool IsConfigurable => true;

    public void RegisterConfiguration(IniConfig iniConfig)
    {
        var section = new ZxingConfigurationImpl();
        iniConfig.AddSection(section);
        _config = section;
    }

    public void RegisterServices(IServiceLocator serviceLocator)
    {
        _captureProcessor = new ZxingCaptureProcessor(_config);
        _editorPlugin = new ZxingEditorPlugin(_config);
        _hotspotTransformer = new ZxingHotspotTransformer();
        serviceLocator.AddService<IProcessor>(_captureProcessor);
        serviceLocator.AddService<IEditorPlugin>(_editorPlugin);
        serviceLocator.AddService<IFeatureHotspotTransformer>(_hotspotTransformer);
        serviceLocator.AddService<IDestination>(new ZxingQrDestination());
        serviceLocator.AddService<IRecipeStepProvider>(this);
        serviceLocator.AddService<IRecipeDrawableProvider>(this);
        StepRegistry.Instance.RegisterProvider(this);
        RecipeDrawableRegistry.Instance.RegisterProvider(this);
    }

    /// <summary>
    /// Registers drawable factories provided by the ZXing plugin for recipe drawables.
    /// </summary>
    /// <param name="registry">The recipe drawable registry.</param>
    public void RegisterDrawables(IRecipeDrawableRegistry registry)
    {
        if (registry == null) return;

        registry.RegisterDrawableFactory("QRCode", (surface, p, ctx) => CreateQrDrawable(surface, p, ctx), ScaleOptions.Rational);
        registry.RegisterDrawableFactory("Barcode", (surface, p, ctx) => CreateQrDrawable(surface, p, ctx), ScaleOptions.Default);
        registry.RegisterDrawableConfigurator("QRCode", (p, owner) => ConfigureZxingDrawable(p, owner));
        registry.RegisterDrawableConfigurator("Barcode", (p, owner) => ConfigureZxingDrawable(p, owner));
    }

    /// <summary>
    /// Registers recipe step factories provided by the ZXing plugin.
    /// </summary>
    /// <param name="registry">The step registry.</param>
    public void RegisterSteps(IStepRegistry registry)
    {
        if (registry == null) return;

        registry.RegisterStepFactory("BarcodeScan", config => new ZxingStep(config));
    }

    /// <summary>
    /// Returns the JSON Schema fragment describing the drawables provided by this extension.
    /// </summary>
    public string GetDrawableSchemaJson()
    {
        return @"{
  ""definitions"": {
    ""ZxingQrDrawable"": {
      ""type"": ""object"",
      ""properties"": {
        ""type"": { ""type"": ""string"", ""enum"": [""QRCode"", ""Barcode""] },
        ""qrType"": { 
          ""type"": ""string"", 
          ""enum"": [""Text"", ""Link"", ""WiFi"", ""BusinessCard"", ""Payment"", ""Email"", ""CalendarEvent"", ""Phone"", ""Sms"", ""Geo""] 
        },
        ""text"": { ""type"": ""string"" },
        ""foreColor"": { ""type"": ""string"" },
        ""backColor"": { ""type"": ""string"" },
        ""roundedDots"": { ""type"": ""boolean"" },
        ""margin"": { ""type"": ""integer"", ""minimum"": 0 },
        ""wifiSsid"": { ""type"": ""string"" },
        ""wifiPassword"": { ""type"": ""string"" },
        ""wifiEncryption"": { ""type"": ""string"", ""enum"": [""WPA"", ""WEP"", ""nopass""] },
        ""vcardFirstName"": { ""type"": ""string"" },
        ""vcardLastName"": { ""type"": ""string"" },
        ""vcardCompany"": { ""type"": ""string"" },
        ""vcardEmail"": { ""type"": ""string"" },
        ""vcardPhone"": { ""type"": ""string"" },
        ""vcardUrl"": { ""type"": ""string"" },
        ""epcName"": { ""type"": ""string"" },
        ""epcIban"": { ""type"": ""string"" },
        ""epcBic"": { ""type"": ""string"" },
        ""epcAmount"": { ""type"": ""string"" },
        ""epcReference"": { ""type"": ""string"" },
        ""epcMessage"": { ""type"": ""string"" },
        ""emailTo"": { ""type"": ""string"" },
        ""emailSubject"": { ""type"": ""string"" },
        ""emailBody"": { ""type"": ""string"" },
        ""eventTitle"": { ""type"": ""string"" },
        ""eventLocation"": { ""type"": ""string"" },
        ""eventStart"": { ""type"": ""string"" },
        ""eventEnd"": { ""type"": ""string"" },
        ""eventDescription"": { ""type"": ""string"" },
        ""phoneNumber"": { ""type"": ""string"" },
        ""smsNumber"": { ""type"": ""string"" },
        ""smsMessage"": { ""type"": ""string"" },
        ""latitude"": { ""type"": ""string"" },
        ""longitude"": { ""type"": ""string"" }
      }
    }
  }
}";
    }

    /// <summary>
    /// Returns the JSON Schema fragment describing custom capture steps provided by this extension.
    /// </summary>
    public string GetStepSchemaJson()
    {
        return @"{
  ""definitions"": {
    ""BarcodeScanStep"": {
      ""type"": ""object"",
      ""properties"": {
        ""stepType"": { ""type"": ""string"", ""const"": ""BarcodeScan"" },
        ""parameters"": {
          ""type"": ""object"",
          ""properties"": {
            ""tryHarder"": { ""type"": ""boolean"" },
            ""pureBarcode"": { ""type"": ""boolean"" },
            ""autoRotate"": { ""type"": ""boolean"" }
          }
        }
      }
    }
  }
}";
    }

    /// <summary>
    /// Creates an IDrawableContainer representing a QR code or barcode.
    /// </summary>
    public static IDrawableContainer CreateQrDrawable(ISurface surface, Dictionary<string, object> p, CaptureFlowContext context)
    {
        if (surface == null) return null;

        string qrType = GetString(p, "QrType") ?? GetString(p, "Category");
        string payload = null;
        int qrCategoryIndex = 0;

        if (string.Equals(qrType, "Payment", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(qrType, "Epc", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(qrType, "Sepa", StringComparison.OrdinalIgnoreCase))
        {
            qrCategoryIndex = 3;
            payload = FormatEpcPayload(p);
        }
        else if (string.Equals(qrType, "BusinessCard", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(qrType, "Contact", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(qrType, "vCard", StringComparison.OrdinalIgnoreCase))
        {
            qrCategoryIndex = 2;
            payload = FormatVcardPayload(p);
        }
        else if (string.Equals(qrType, "WiFi", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(qrType, "Network", StringComparison.OrdinalIgnoreCase))
        {
            qrCategoryIndex = 1;
            payload = FormatWifiPayload(p);
        }
        else if (string.Equals(qrType, "Email", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(qrType, "Mail", StringComparison.OrdinalIgnoreCase))
        {
            qrCategoryIndex = 4;
            payload = FormatEmailPayload(p);
        }
        else if (string.Equals(qrType, "CalendarEvent", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(qrType, "Calendar", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(qrType, "Event", StringComparison.OrdinalIgnoreCase))
        {
            qrCategoryIndex = 5;
            payload = FormatCalendarPayload(p);
        }
        else if (string.Equals(qrType, "Phone", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(qrType, "Tel", StringComparison.OrdinalIgnoreCase))
        {
            qrCategoryIndex = 6;
            payload = FormatPhonePayload(p);
        }
        else if (string.Equals(qrType, "Sms", StringComparison.OrdinalIgnoreCase))
        {
            qrCategoryIndex = 7;
            payload = FormatSmsPayload(p);
        }
        else if (string.Equals(qrType, "Geo", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(qrType, "Location", StringComparison.OrdinalIgnoreCase))
        {
            qrCategoryIndex = 8;
            payload = FormatGeoPayload(p);
        }
        else
        {
            // Default: Text / Link / URL
            qrCategoryIndex = 0;
            payload = GetString(p, "Text");
        }

        if (string.IsNullOrEmpty(payload))
        {
            payload = "https://getgreenshot.org";
        }

        string formatStr = GetString(p, "Format") ?? "QR_CODE";
        var format = ZxingBarcodeGenerator.MapBarcodeFormat(formatStr);

        Color foreColor = GetColor(p, "ForeColor", Color.Black);
        Color backColor = GetColor(p, "BackColor", Color.White);
        bool rounded = GetBool(p, "RoundedDots", false);
        int margin = GetInt(p, "Margin", 1);

        int? requestedW = null;
        int? requestedH = null;
        int size = GetInt(p, "Size", 0);
        if (p.TryGetValue("Width", out var wVal) && int.TryParse(wVal?.ToString(), out int w) && w > 0)
        {
            requestedW = w;
        }
        else if (size > 0)
        {
            requestedW = size;
        }

        if (p.TryGetValue("Height", out var hVal) && int.TryParse(hVal?.ToString(), out int h) && h > 0)
        {
            requestedH = h;
        }
        else if (size > 0)
        {
            requestedH = size;
        }

        int formatIndex = 0;
        if (format != ZXing.BarcodeFormat.QR_CODE)
        {
            formatIndex = ZxingEditorForm.MapFormatToIndex(format);
        }

        var model = new ZxingModel
        {
            FormatIndex = formatIndex,
            QrCategoryIndex = qrCategoryIndex,
            RawText = payload,
            ForeColor = foreColor,
            BackColor = backColor,
            RoundedDots = rounded,
            Margin = margin,
            WifiSsid = GetString(p, "WifiSsid"),
            WifiPassword = GetString(p, "WifiPassword"),
            WifiEncryptionIndex = string.Equals(GetString(p, "WifiEncryption"), "WEP", StringComparison.OrdinalIgnoreCase) ? 1 :
                                  string.Equals(GetString(p, "WifiEncryption"), "nopass", StringComparison.OrdinalIgnoreCase) ? 2 : 0,
            VcardFirstName = GetString(p, "VcardFirstName"),
            VcardLastName = GetString(p, "VcardLastName"),
            VcardPhone = GetString(p, "VcardPhone"),
            VcardEmail = GetString(p, "VcardEmail"),
            VcardCompany = GetString(p, "VcardCompany"),
            VcardUrl = GetString(p, "VcardUrl"),
            EpcName = GetString(p, "EpcName"),
            EpcIban = GetString(p, "EpcIban"),
            EpcBic = GetString(p, "EpcBic"),
            EpcAmount = GetString(p, "EpcAmount"),
            EpcReference = GetString(p, "EpcReference"),
            EpcMessage = GetString(p, "EpcMessage"),
            EmailTo = GetString(p, "EmailTo") ?? GetString(p, "EmailAddress"),
            EmailSubject = GetString(p, "EmailSubject"),
            EmailBody = GetString(p, "EmailBody"),
            EventTitle = GetString(p, "EventTitle"),
            EventLocation = GetString(p, "EventLocation"),
            EventStart = GetString(p, "EventStart"),
            EventEnd = GetString(p, "EventEnd"),
            EventDescription = GetString(p, "EventDescription"),
            PhoneNumber = GetString(p, "PhoneNumber"),
            SmsNumber = GetString(p, "SmsNumber"),
            SmsMessage = GetString(p, "SmsMessage"),
            Latitude = GetString(p, "Latitude") ?? GetString(p, "GeoLat"),
            Longitude = GetString(p, "Longitude") ?? GetString(p, "GeoLon")
        };

        var container = new BarcodeContainer(surface, model, margin);

        bool is2D = (format == ZXing.BarcodeFormat.QR_CODE ||
                     format == ZXing.BarcodeFormat.AZTEC ||
                     format == ZXing.BarcodeFormat.DATA_MATRIX ||
                     format == ZXing.BarcodeFormat.PDF_417);

        int initW;
        int initH;
        if (size > 0)
        {
            initW = size;
            initH = size;
        }
        else if (requestedW.HasValue || requestedH.HasValue)
        {
            if (is2D)
            {
                int side = (requestedW.HasValue && requestedH.HasValue)
                    ? Math.Min(requestedW.Value, requestedH.Value)
                    : (requestedW ?? requestedH.Value);
                initW = side;
                initH = side;
            }
            else
            {
                initW = requestedW ?? 250;
                initH = requestedH ?? 80;
            }
        }
        else
        {
            initW = is2D ? 160 : 250;
            initH = is2D ? 160 : 80;
        }

        container.Width = initW;
        container.Height = initH;
        container.RegenerateBarcode();

        return container;
    }

    public static string FormatWifiPayload(Dictionary<string, object> p)
    {
        string wifiSsid = GetString(p, "WifiSsid") ?? "";
        string wifiPass = GetString(p, "WifiPassword") ?? "";
        string wifiEnc = GetString(p, "WifiEncryption") ?? "WPA";
        return $"WIFI:S:{wifiSsid};T:{wifiEnc};P:{wifiPass};;";
    }

    public static string FormatVcardPayload(Dictionary<string, object> p)
    {
        string firstName = GetString(p, "VcardFirstName") ?? "";
        string lastName = GetString(p, "VcardLastName") ?? "";
        string phone = GetString(p, "VcardPhone") ?? "";
        string email = GetString(p, "VcardEmail") ?? "";
        string org = GetString(p, "VcardCompany") ?? "";
        string url = GetString(p, "VcardUrl") ?? "";
        return "BEGIN:VCARD\r\nVERSION:3.0\r\n" +
               $"N:{lastName};{firstName}\r\n" +
               $"FN:{firstName} {lastName}\r\n" +
               $"ORG:{org}\r\n" +
               $"TEL;TYPE=CELL:{phone}\r\n" +
               $"EMAIL:{email}\r\n" +
               $"URL:{url}\r\nEND:VCARD";
    }

    public static string FormatEpcPayload(Dictionary<string, object> p)
    {
        string iban = GetString(p, "EpcIban") ?? "";
        string name = GetString(p, "EpcName") ?? "";
        string bic = GetString(p, "EpcBic") ?? "";
        string amount = GetString(p, "EpcAmount") ?? "";
        string formattedAmount = string.Empty;
        if (double.TryParse(amount, out double amt))
        {
            formattedAmount = string.Format(System.Globalization.CultureInfo.InvariantCulture, "EUR{0:F2}", amt);
        }
        else if (!string.IsNullOrEmpty(amount))
        {
            formattedAmount = amount.StartsWith("EUR", StringComparison.OrdinalIgnoreCase) ? amount : $"EUR{amount}";
        }
        string reference = GetString(p, "EpcReference") ?? "";
        string message = GetString(p, "EpcMessage") ?? "";

        return "BCD\n002\n1\nSCT\n" +
               $"{bic}\n{name}\n{iban}\n{formattedAmount}\n\n{reference}\n{message}\n";
    }

    public static string FormatEmailPayload(Dictionary<string, object> p)
    {
        string to = GetString(p, "EmailTo") ?? GetString(p, "EmailAddress") ?? "";
        string subject = GetString(p, "EmailSubject");
        string body = GetString(p, "EmailBody");

        string mailto = $"mailto:{to}";
        var query = new List<string>();
        if (!string.IsNullOrEmpty(subject)) query.Add($"subject={Uri.EscapeDataString(subject)}");
        if (!string.IsNullOrEmpty(body)) query.Add($"body={Uri.EscapeDataString(body)}");
        if (query.Count > 0) mailto += "?" + string.Join("&", query);
        return mailto;
    }

    public static string FormatCalendarPayload(Dictionary<string, object> p)
    {
        string title = GetString(p, "EventTitle") ?? "";
        string location = GetString(p, "EventLocation") ?? "";
        string start = GetString(p, "EventStart") ?? "";
        string end = GetString(p, "EventEnd") ?? "";
        string desc = GetString(p, "EventDescription") ?? "";

        return "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\n" +
               $"SUMMARY:{title}\r\n" +
               $"LOCATION:{location}\r\n" +
               $"DESCRIPTION:{desc}\r\n" +
               (!string.IsNullOrEmpty(start) ? $"DTSTART:{start}\r\n" : "") +
               (!string.IsNullOrEmpty(end) ? $"DTEND:{end}\r\n" : "") +
               "END:VEVENT\r\nEND:VCALENDAR";
    }

    public static string FormatPhonePayload(Dictionary<string, object> p)
    {
        string number = GetString(p, "PhoneNumber") ?? "";
        return $"tel:{number}";
    }

    public static string FormatSmsPayload(Dictionary<string, object> p)
    {
        string number = GetString(p, "SmsNumber") ?? "";
        string message = GetString(p, "SmsMessage") ?? "";
        return $"smsto:{number}:{message}";
    }

    public static string FormatGeoPayload(Dictionary<string, object> p)
    {
        string lat = GetString(p, "Latitude") ?? GetString(p, "GeoLat") ?? "0";
        string lon = GetString(p, "Longitude") ?? GetString(p, "GeoLon") ?? "0";
        return $"geo:{lat},{lon}";
    }

    public bool Start()
    {
        return true;
    }

    public void Shutdown()
    {
        Log.Debug("ZXing plugin shutdown.");
    }

    public void Configure()
    {
        new Forms.ZxingSettingsWindow(_config).ShowDialog();
    }

    /// <summary>
    /// Configures a barcode or QR code drawable using the ZxingEditorForm dialog.
    /// </summary>
    public static bool ConfigureZxingDrawable(IDictionary<string, object> p, object owner)
    {
        if (p == null) return false;

        var model = new ZxingModel();

        string qrType = GetString(p, "QrType") ?? GetString(p, "Category");
        if (string.Equals(qrType, "WiFi", StringComparison.OrdinalIgnoreCase)) model.QrCategoryIndex = 1;
        else if (string.Equals(qrType, "BusinessCard", StringComparison.OrdinalIgnoreCase) || string.Equals(qrType, "vCard", StringComparison.OrdinalIgnoreCase)) model.QrCategoryIndex = 2;
        else if (string.Equals(qrType, "Payment", StringComparison.OrdinalIgnoreCase) || string.Equals(qrType, "Epc", StringComparison.OrdinalIgnoreCase)) model.QrCategoryIndex = 3;
        else if (string.Equals(qrType, "Email", StringComparison.OrdinalIgnoreCase)) model.QrCategoryIndex = 4;
        else if (string.Equals(qrType, "CalendarEvent", StringComparison.OrdinalIgnoreCase) || string.Equals(qrType, "Calendar", StringComparison.OrdinalIgnoreCase)) model.QrCategoryIndex = 5;
        else if (string.Equals(qrType, "Phone", StringComparison.OrdinalIgnoreCase)) model.QrCategoryIndex = 6;
        else if (string.Equals(qrType, "Sms", StringComparison.OrdinalIgnoreCase)) model.QrCategoryIndex = 7;
        else if (string.Equals(qrType, "Geo", StringComparison.OrdinalIgnoreCase)) model.QrCategoryIndex = 8;
        else model.QrCategoryIndex = 0;

        model.RawText = GetString(p, "Text");
        model.RoundedDots = GetBool(p, "RoundedDots", false);

        model.WifiSsid = GetString(p, "WifiSsid");
        model.WifiPassword = GetString(p, "WifiPassword");
        string enc = GetString(p, "WifiEncryption");
        model.WifiEncryptionIndex = string.Equals(enc, "WEP", StringComparison.OrdinalIgnoreCase) ? 1 :
                                    string.Equals(enc, "nopass", StringComparison.OrdinalIgnoreCase) ? 2 : 0;

        model.VcardFirstName = GetString(p, "VcardFirstName");
        model.VcardLastName = GetString(p, "VcardLastName");
        model.VcardCompany = GetString(p, "VcardCompany");
        model.VcardEmail = GetString(p, "VcardEmail");
        model.VcardPhone = GetString(p, "VcardPhone");
        model.VcardUrl = GetString(p, "VcardUrl");

        model.EpcName = GetString(p, "EpcName");
        model.EpcIban = GetString(p, "EpcIban");
        model.EpcBic = GetString(p, "EpcBic");
        model.EpcAmount = GetString(p, "EpcAmount") ?? "10.00";
        model.EpcReference = GetString(p, "EpcReference");
        model.EpcMessage = GetString(p, "EpcMessage");

        model.EmailTo = GetString(p, "EmailTo");
        model.EmailSubject = GetString(p, "EmailSubject");
        model.EmailBody = GetString(p, "EmailBody");

        model.EventTitle = GetString(p, "EventTitle");
        model.EventLocation = GetString(p, "EventLocation");
        model.EventStart = GetString(p, "EventStart");
        model.EventEnd = GetString(p, "EventEnd");
        model.EventDescription = GetString(p, "EventDescription");

        model.PhoneNumber = GetString(p, "PhoneNumber");

        model.SmsNumber = GetString(p, "SmsNumber");
        model.SmsMessage = GetString(p, "SmsMessage");

        model.Latitude = GetString(p, "Latitude");
        model.Longitude = GetString(p, "Longitude");

        model.ForeColor = GetColor(p, "ForeColor", Color.Black);
        model.BackColor = GetColor(p, "BackColor", Color.White);
        model.Margin = GetInt(p, "Margin", 1);

        IWin32Window win32Owner = owner as IWin32Window;
        if (win32Owner == null && owner is System.Windows.Window wpfWin)
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(wpfWin);
            win32Owner = new Win32WindowWrapper(helper.Handle);
        }

        using var form = new ZxingEditorForm(model);
        var result = win32Owner != null ? form.ShowDialog(win32Owner) : form.ShowDialog();

        if (result == DialogResult.OK)
        {
            string newQrType = model.QrCategoryIndex switch
            {
                1 => "WiFi",
                2 => "BusinessCard",
                3 => "Payment",
                4 => "Email",
                5 => "CalendarEvent",
                6 => "Phone",
                7 => "Sms",
                8 => "Geo",
                _ => "Text"
            };
            p["QrType"] = newQrType;
            p["Text"] = model.RawText;
            p["RoundedDots"] = model.RoundedDots;
            p["Margin"] = model.Margin;

            p["WifiSsid"] = model.WifiSsid;
            p["WifiPassword"] = model.WifiPassword;
            p["WifiEncryption"] = model.WifiEncryptionIndex == 1 ? "WEP" : model.WifiEncryptionIndex == 2 ? "nopass" : "WPA";

            p["VcardFirstName"] = model.VcardFirstName;
            p["VcardLastName"] = model.VcardLastName;
            p["VcardCompany"] = model.VcardCompany;
            p["VcardEmail"] = model.VcardEmail;
            p["VcardPhone"] = model.VcardPhone;
            p["VcardUrl"] = model.VcardUrl;

            p["EpcName"] = model.EpcName;
            p["EpcIban"] = model.EpcIban;
            p["EpcBic"] = model.EpcBic;
            p["EpcAmount"] = model.EpcAmount;
            p["EpcReference"] = model.EpcReference;
            p["EpcMessage"] = model.EpcMessage;

            p["EmailTo"] = model.EmailTo;
            p["EmailSubject"] = model.EmailSubject;
            p["EmailBody"] = model.EmailBody;

            p["EventTitle"] = model.EventTitle;
            p["EventLocation"] = model.EventLocation;
            p["EventStart"] = model.EventStart;
            p["EventEnd"] = model.EventEnd;
            p["EventDescription"] = model.EventDescription;

            p["PhoneNumber"] = model.PhoneNumber;

            p["SmsNumber"] = model.SmsNumber;
            p["SmsMessage"] = model.SmsMessage;

            p["Latitude"] = model.Latitude;
            p["Longitude"] = model.Longitude;

            p["ForeColor"] = ColorTranslator.ToHtml(model.ForeColor);
            p["BackColor"] = ColorTranslator.ToHtml(model.BackColor);

            return true;
        }

        return false;
    }

    private class Win32WindowWrapper : IWin32Window
    {
        public IntPtr Handle { get; }
        public Win32WindowWrapper(IntPtr handle) => Handle = handle;
    }

    public void Dispose()
    {
    }

    #region Helpers

    private static string GetString(IDictionary<string, object> p, string key)
    {
        if (p != null && p.TryGetValue(key, out var val) && val != null)
        {
            return val.ToString();
        }
        return null;
    }

    private static int GetInt(IDictionary<string, object> p, string key, int defaultValue = 0)
    {
        if (p != null && p.TryGetValue(key, out var val) && val != null)
        {
            if (int.TryParse(val.ToString(), out int i)) return i;
            if (double.TryParse(val.ToString(), out double d)) return (int)Math.Round(d);
        }
        return defaultValue;
    }

    private static bool GetBool(IDictionary<string, object> p, string key, bool defaultValue = false)
    {
        if (p != null && p.TryGetValue(key, out var val) && val != null)
        {
            if (bool.TryParse(val.ToString(), out bool b)) return b;
        }
        return defaultValue;
    }

    private static Color GetColor(IDictionary<string, object> p, string key, Color fallback)
    {
        if (p != null && p.TryGetValue(key, out var val) && val != null)
        {
            if (val is Color c) return c;
            string s = val.ToString();
            if (!string.IsNullOrWhiteSpace(s))
            {
                try
                {
                    return ColorTranslator.FromHtml(s);
                }
                catch
                {
                    var named = Color.FromName(s);
                    return named.IsKnownColor ? named : fallback;
                }
            }
        }
        return fallback;
    }

    #endregion
}
