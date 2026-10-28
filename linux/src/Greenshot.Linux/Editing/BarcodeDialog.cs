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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Helpers;
using SkiaSharp;

namespace Greenshot.Linux.Editing;

/// <summary>
/// The settings of a QR code or barcode, the ZxingEditorWindow of Windows: the format, for a QR code its type and the inputs of
/// the type (WiFi, contact, SEPA transfer, ...), the colors and a preview
/// </summary>
internal static class BarcodeDialog
{
    private sealed record Input(string Key, string Label, bool Multiline = false);

    /// <summary>
    /// The inputs of a QR type, the category texts and the hint for empty inputs
    /// </summary>
    private static readonly Dictionary<string, (string Category, string Hint, Input[] Inputs)> Types = new()
    {
        ["Text"] = ("category_text", "hint_text", [new("Text", "raw_text", true)]),
        ["Wifi"] = ("category_wifi", "hint_wifi", [new("WifiSsid", "wifi_ssid"), new("WifiPassword", "wifi_password")]),
        ["BusinessCard"] = ("category_vcard", "hint_vcard",
            [new("VcardFirstName", "vcard_first_name"), new("VcardLastName", "vcard_last_name"), new("VcardPhone", "vcard_phone"),
             new("VcardEmail", "vcard_email"), new("VcardCompany", "vcard_company"), new("VcardUrl", "vcard_url")]),
        ["Payment"] = ("category_epc", "hint_epc",
            [new("EpcName", "epc_recipient"), new("EpcIban", "epc_iban"), new("EpcBic", "epc_bic"), new("EpcAmount", "epc_amount"),
             new("EpcReference", "epc_reference"), new("EpcMessage", "message")]),
        ["Email"] = ("category_email", "hint_email", [new("EmailTo", "email_to"), new("EmailSubject", "email_subject"), new("EmailBody", "email_body", true)]),
        ["CalendarEvent"] = ("category_calendar", "hint_calendar",
            [new("EventTitle", "event_title"), new("EventLocation", "event_location"), new("EventStart", "event_start"), new("EventEnd", "event_end"),
             new("EventDescription", "event_description", true)]),
        ["Phone"] = ("category_phone", "hint_phone", [new("PhoneNumber", "phone_number")]),
        ["Sms"] = ("category_sms", "hint_sms", [new("SmsNumber", "sms_number"), new("SmsMessage", "message", true)]),
        ["Geo"] = ("category_geo", "hint_geo", [new("Latitude", "geo_latitude"), new("Longitude", "geo_longitude")]),
    };

    /// <summary>
    /// The WiFi encryptions as the payload writes them
    /// </summary>
    private static readonly string[] WifiEncryptions = ["WPA", "WEP", "nopass"];

    private static string Z(string key) => Texts.Get("Zxing", key);

    /// <summary>
    /// Ask for the settings; the result is a new element with them, null when cancelled
    /// </summary>
    /// <param name="current">The element to change, null to insert a new one</param>
    public static async Task<BarcodeElement?> AskAsync(Window owner, BarcodeElement? current, Func<IReadOnlyList<SKColor>> recentColors)
    {
        var model = new BarcodeElement
        {
            Format = current?.Format ?? "QR_CODE",
            QrType = current?.QrType ?? "Text",
            Values = new Dictionary<string, string>(current?.Values ?? new Dictionary<string, string>()),
            ForeColor = current?.ForeColor ?? SKColors.Black,
            BackColor = current?.BackColor ?? SKColors.White,
            RoundedDots = current?.RoundedDots ?? false,
            Margin = current?.Margin ?? 1,
        };

        var dialog = new Window
        {
            Title = Z(current == null ? "editor_title_insert" : "editor_title_edit"),
            Icon = AppIcons.Window,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var format = new ComboBox { ItemsSource = BarcodeElement.Formats, SelectedItem = model.Format, Width = 220 };
        var type = new ComboBox
        {
            ItemsSource = BarcodeElement.QrTypes.Select(t => Z(Types[t].Category)).ToList(),
            SelectedIndex = Math.Max(0, BarcodeElement.QrTypes.ToList().IndexOf(model.QrType)),
            Width = 220,
        };
        var typeRow = Ui.Row(Z("qr_type"), type);
        var inputs = new StackPanel { Spacing = 6, Width = 380 };
        var inputsTitle = new TextBlock { FontWeight = FontWeight.SemiBold };
        var preview = new Image { Width = 240, Height = 240, Stretch = Stretch.Uniform };
        var hint = Ui.Note(string.Empty);
        hint.MaxWidth = 240;
        var fore = new ColorButton(Z("foreground"), model.ForeColor, recentColors);
        var back = new ColorButton(Z("background"), model.BackColor, recentColors);
        var rounded = Ui.Check(Z("rounded_dots"), model.RoundedDots);
        var margin = new NumericUpDown { Value = model.Margin, Minimum = 0, Maximum = 20, Increment = 1, FormatString = "0", Width = 120 };
        var ok = new Button { Content = Z(current == null ? "insert" : "apply"), IsDefault = true, MinWidth = 90 };
        var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true, MinWidth = 90 };

        void UpdatePreview()
        {
            var old = preview.Source as IDisposable;
            using var bitmap = model.Render(240, model.Is2D ? 240 : 120);
            preview.Source = bitmap == null ? null : BitmapConverter.ToAvalonia(bitmap);
            old?.Dispose();
            bool isQr = model.Format == "QR_CODE";
            hint.Text = bitmap != null ? string.Empty
                : string.IsNullOrEmpty(model.Payload) ? Z(isQr ? Types[model.QrType].Hint : "hint_value")
                : $"{Z("error_generating")} {model.Format}";
            ok.IsEnabled = bitmap != null;
        }

        TextBox Field(string key, bool multiline)
        {
            var box = new TextBox
            {
                Text = model.Values.GetValueOrDefault(key) ?? string.Empty,
                AcceptsReturn = multiline,
                TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
                Height = multiline ? 80 : double.NaN,
                Width = 360,
            };
            box.TextChanged += (_, _) =>
            {
                model.Values[key] = box.Text ?? string.Empty;
                UpdatePreview();
            };
            return box;
        }

        void BuildInputs()
        {
            inputs.Children.Clear();
            bool isQr = model.Format == "QR_CODE";
            typeRow.IsVisible = isQr;
            if (!isQr)
            {
                inputsTitle.Text = Z("inputs_value");
                inputs.Children.Add(new TextBlock { Text = Z("raw_text") });
                inputs.Children.Add(Field("Text", false));
                return;
            }

            var (_, _, fields) = Types[model.QrType];
            inputsTitle.Text = Z("inputs_" + model.QrType switch
            {
                "Wifi" => "wifi",
                "BusinessCard" => "vcard",
                "Payment" => "epc",
                "Email" => "email",
                "CalendarEvent" => "calendar",
                "Phone" => "phone",
                "Sms" => "sms",
                "Geo" => "geo",
                _ => "text",
            });
            foreach (var input in fields)
            {
                inputs.Children.Add(new TextBlock { Text = Z(input.Label) });
                inputs.Children.Add(Field(input.Key, input.Multiline));
            }

            if (model.QrType == "Wifi")
            {
                var encryption = new ComboBox
                {
                    ItemsSource = new[] { "WPA/WPA2", "WEP", Z("wifi_unencrypted") },
                    SelectedIndex = Math.Max(0, Array.IndexOf(WifiEncryptions, model.Values.GetValueOrDefault("WifiEncryption") ?? "WPA")),
                    Width = 220,
                };
                encryption.SelectionChanged += (_, _) =>
                {
                    model.Values["WifiEncryption"] = WifiEncryptions[Math.Max(0, encryption.SelectedIndex)];
                    UpdatePreview();
                };
                inputs.Children.Add(new TextBlock { Text = Z("wifi_encryption") });
                inputs.Children.Add(encryption);
            }
        }

        format.SelectionChanged += (_, _) =>
        {
            model.Format = format.SelectedItem as string ?? "QR_CODE";
            BuildInputs();
            UpdatePreview();
        };
        type.SelectionChanged += (_, _) =>
        {
            model.QrType = BarcodeElement.QrTypes[Math.Max(0, type.SelectedIndex)];
            BuildInputs();
            UpdatePreview();
        };
        fore.ColorChosen += color =>
        {
            model.ForeColor = color;
            UpdatePreview();
        };
        back.ColorChosen += color =>
        {
            model.BackColor = color;
            UpdatePreview();
        };
        rounded.IsCheckedChanged += (_, _) =>
        {
            model.RoundedDots = rounded.IsChecked == true;
            UpdatePreview();
        };
        margin.ValueChanged += (_, _) =>
        {
            model.Margin = (int)(margin.Value ?? 1);
            UpdatePreview();
        };

        bool confirmed = false;
        ok.Click += (_, _) =>
        {
            confirmed = true;
            dialog.Close();
        };
        cancel.Click += (_, _) => dialog.Close();

        BuildInputs();
        UpdatePreview();
        var left = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                Ui.Row(Z("barcode_format"), format),
                typeRow,
                Ui.Section(Z("inputs"), inputsTitle, inputs),
            },
        };
        var right = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                Ui.Section(Z("preview"), new Border { Background = Brushes.LightGray, Padding = new Thickness(6), Child = preview }, hint),
                Ui.Horizontal(new TextBlock { Text = Z("foreground"), VerticalAlignment = VerticalAlignment.Center }, fore,
                    new TextBlock { Text = Z("background"), VerticalAlignment = VerticalAlignment.Center }, back),
                rounded,
                Ui.Horizontal(new TextBlock { Text = Z("margin"), VerticalAlignment = VerticalAlignment.Center }, margin),
            },
        };
        var buttons = Ui.Horizontal(ok, cancel);
        buttons.HorizontalAlignment = HorizontalAlignment.Right;
        dialog.Content = Ui.Page(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 24, Children = { left, right } }, buttons);
        await dialog.ShowDialog(owner);
        (preview.Source as IDisposable)?.Dispose();
        return confirmed ? model : null;
    }
}
