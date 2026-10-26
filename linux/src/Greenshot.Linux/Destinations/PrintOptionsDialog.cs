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
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Helpers;

namespace Greenshot.Linux.Destinations;

/// <summary>
/// The print options before printing (OutputPrintPromptOptions), the PrintOptionsWindow of Windows.
/// "Do not ask again" stores the options as the defaults.
/// </summary>
internal sealed class PrintOptionsDialog : Window
{
    private readonly TaskCompletionSource<PrintOptions?> _result = new();

    private PrintOptionsDialog(CoreSettings settings, PrintOptions options)
    {
        Title = Texts.Core("printoptions_title");
        Icon = AppIcons.Window;
        SizeToContent = SizeToContent.WidthAndHeight;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Topmost = true;

        var shrink = Ui.Check(Texts.Core("printoptions_allowshrink"), options.AllowShrink);
        var enlarge = Ui.Check(Texts.Core("printoptions_allowenlarge"), options.AllowEnlarge);
        var rotate = Ui.Check(Texts.Core("printoptions_allowrotate"), options.AllowRotate);
        var center = Ui.Check(Texts.Core("printoptions_allowcenter"), options.Center);
        var footer = Ui.Check(Texts.Core("printoptions_timestamp"), options.Footer != null);
        var color = new RadioButton { Content = Texts.Core("printoptions_printcolor"), GroupName = "print-colors", IsChecked = !options.Grayscale && !options.Monochrome };
        var grayscale = new RadioButton { Content = Texts.Core("printoptions_printgrayscale"), GroupName = "print-colors", IsChecked = options.Grayscale && !options.Monochrome };
        var monochrome = new RadioButton { Content = Texts.Core("printoptions_printmonochrome"), GroupName = "print-colors", IsChecked = options.Monochrome };
        var inverted = Ui.Check(Texts.Core("printoptions_inverted"), options.Inverted);
        var dontAsk = Ui.Check(Texts.Core("printoptions_dontaskagain"), false);

        var ok = new Button { Content = Texts.Linux("ok"), IsDefault = true, MinWidth = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
        ok.Click += (_, _) =>
        {
            var chosen = new PrintOptions(shrink.IsChecked == true, enlarge.IsChecked == true, rotate.IsChecked == true, center.IsChecked == true,
                inverted.IsChecked == true, grayscale.IsChecked == true, monochrome.IsChecked == true,
                footer.IsChecked == true ? options.Footer ?? string.Empty : null);
            settings.OutputPrintAllowShrink = chosen.AllowShrink;
            settings.OutputPrintAllowEnlarge = chosen.AllowEnlarge;
            settings.OutputPrintAllowRotate = chosen.AllowRotate;
            settings.OutputPrintCenter = chosen.Center;
            settings.OutputPrintInverted = chosen.Inverted;
            settings.OutputPrintGrayscale = chosen.Grayscale;
            settings.OutputPrintMonochrome = chosen.Monochrome;
            settings.OutputPrintFooter = chosen.Footer != null;
            if (dontAsk.IsChecked == true)
            {
                settings.OutputPrintPromptOptions = false;
            }

            settings.Save();
            _result.TrySetResult(chosen);
            Close();
        };
        var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true, MinWidth = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
        cancel.Click += (_, _) => Close();
        Closed += (_, _) => _result.TrySetResult(null);

        var buttons = Ui.Horizontal(ok, cancel);
        buttons.HorizontalAlignment = HorizontalAlignment.Right;
        var content = Ui.Page(
            Ui.Section(Texts.Core("printoptions_layout"), shrink, enlarge, rotate, center, footer),
            Ui.Section(Texts.Core("printoptions_colors"), color, grayscale, monochrome, inverted),
            dontAsk,
            buttons);
        content.MinWidth = 380;
        Content = content;
    }

    public static Task<PrintOptions?> AskAsync(CoreSettings settings, PrintOptions options)
    {
        var dialog = new PrintOptionsDialog(settings, options);
        dialog.Show();
        dialog.Activate();
        return dialog._result.Task;
    }
}
