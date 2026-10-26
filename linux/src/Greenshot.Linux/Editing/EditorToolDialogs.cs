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
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Barcodes;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Core.Ocr;
using Greenshot.Linux.Editor;
using Greenshot.Linux.Helpers;
using SkiaSharp;

namespace Greenshot.Linux.Editing;

/// <summary>
/// The dialogs of the editor tools which need more than the property bar: redacting text and inserting a QR code
/// </summary>
internal static class EditorToolDialogs
{
    /// <summary>
    /// "Redact Text": recognize the text of the image once, search it and cover the matches with the obfuscate effect
    /// </summary>
    public static async Task RedactTextAsync(Window owner, EditorController controller, CoreSettings settings)
    {
        var dialog = new Window
        {
            Title = Texts.Editor("obfuscate_text_title"),
            Icon = AppIcons.Window,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var search = new TextBox { Width = 320 };
        var regex = Ui.Check(Texts.Editor("obfuscate_text_regex"), false);
        var caseSensitive = Ui.Check(Texts.Editor("obfuscate_text_case_sensitive"), false);
        var status = Ui.Note(string.Empty);
        var searchButton = new Button { Content = Texts.Editor("obfuscate_text_search_button") };
        var apply = new Button { Content = Texts.Editor("obfuscate_text_apply"), IsDefault = true, IsEnabled = false };
        var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true };
        IReadOnlyList<OcrLine>? lines = null;
        IReadOnlyList<SKRect> matches = [];

        async Task SearchAsync()
        {
            if (!TesseractOcr.IsAvailable)
            {
                status.Text = Texts.Linux("ocr_missing");
                return;
            }

            try
            {
                if (lines == null)
                {
                    status.Text = Texts.Core("accessible_loading");
                    using var image = controller.Surface.Flatten();
                    var languages = TesseractOcr.ChooseLanguages(settings.OcrLanguages, Texts.Current, await TesseractOcr.GetLanguagesAsync());
                    lines = await TesseractOcr.RecognizeAsync(image, languages);
                }

                if (lines.Count == 0)
                {
                    status.Text = Texts.Editor("obfuscate_text_no_text");
                    return;
                }

                matches = TextRedaction.FindMatches(lines, search.Text ?? string.Empty, regex.IsChecked == true, caseSensitive.IsChecked == true);
                status.Text = Texts.Editor("obfuscate_text_matches", matches.Count);
                apply.IsEnabled = matches.Count > 0;
            }
            catch (ArgumentException)
            {
                status.Text = Texts.Editor("obfuscate_text_invalid_regex");
            }
            catch (InvalidOperationException ex)
            {
                Log.Error("Text recognition failed", ex);
                status.Text = ex.Message;
            }
        }

        searchButton.Click += async (_, _) => await SearchAsync();
        search.KeyDown += async (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter && !apply.IsEnabled)
            {
                e.Handled = true;
                await SearchAsync();
            }
        };
        search.TextChanged += (_, _) => apply.IsEnabled = false;
        apply.Click += (_, _) =>
        {
            TextRedaction.Apply(controller, matches);
            dialog.Close();
        };
        cancel.Click += (_, _) => dialog.Close();

        var buttons = Ui.Horizontal(apply, cancel);
        buttons.HorizontalAlignment = HorizontalAlignment.Right;
        dialog.Content = Ui.Page(
            Ui.Horizontal(new TextBlock { Text = Texts.Editor("obfuscate_text_search"), VerticalAlignment = VerticalAlignment.Center }, search, searchButton),
            regex,
            caseSensitive,
            status,
            buttons);
        dialog.Opened += (_, _) => search.Focus();
        await dialog.ShowDialog(owner);
    }

    /// <summary>
    /// A QR code for a text or link, inserted as image element
    /// </summary>
    public static async Task InsertQrCodeAsync(Window owner, EditorController controller)
    {
        var dialog = new Window
        {
            Title = Texts.Linux("insert_qr"),
            Icon = AppIcons.Window,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var text = new TextBox { Width = 360, Height = 90, AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var size = new NumericUpDown { Value = 200, Minimum = 50, Maximum = 2000, Increment = 50, FormatString = "0", Width = 140 };
        var insert = new Button { Content = Texts.Linux("ok"), IsDefault = false };
        var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true };
        insert.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(text.Text))
            {
                return;
            }

            using var code = Barcodes.CreateQrCode(text.Text, (int)(size.Value ?? 200));
            controller.PasteImage(code);
            dialog.Close();
        };
        cancel.Click += (_, _) => dialog.Close();
        var buttons = Ui.Horizontal(insert, cancel);
        buttons.HorizontalAlignment = HorizontalAlignment.Right;
        dialog.Content = Ui.Page(
            new TextBlock { Text = Texts.Linux("insert_qr_text") },
            text,
            Ui.Row(Texts.Editor("resize_width"), size),
            buttons);
        dialog.Opened += (_, _) => text.Focus();
        await dialog.ShowDialog(owner);
    }
}
