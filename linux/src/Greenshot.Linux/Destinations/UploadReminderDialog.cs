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

using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Helpers;
using Greenshot.Linux.Plugins.CustomUploader;

namespace Greenshot.Linux.Destinations;

/// <summary>
/// Before an upload whose uploads can't be deleted (the reminder of the import): the capture, where it goes, Upload or Cancel
/// </summary>
internal static class UploadReminderDialog
{
    public static Task<UploadReminderAnswer> AskAsync(Capture capture, string uploader, string server) =>
        Dispatcher.UIThread.InvokeAsync(() => ShowAsync(capture, uploader, server));

    private static Task<UploadReminderAnswer> ShowAsync(Capture capture, string uploader, string server)
    {
        var result = new TaskCompletionSource<UploadReminderAnswer>();
        var thumbnail = BitmapConverter.ToAvalonia(capture.Image);
        var window = new Window
        {
            Title = string.Format(Texts.Get("CustomUploader", "reminder_title"), uploader),
            Icon = AppIcons.Window,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };
        var stop = Ui.Check(string.Format(Texts.Get("CustomUploader", "reminder_stop"), uploader), false);
        var upload = new Button { Content = Texts.Get("CustomUploader", "reminder_upload") };
        var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true, IsDefault = true };
        upload.Click += (_, _) =>
        {
            result.TrySetResult(stop.IsChecked == true ? UploadReminderAnswer.UploadAndStopReminding : UploadReminderAnswer.Upload);
            window.Close();
        };
        cancel.Click += (_, _) => window.Close();
        window.Closed += (_, _) =>
        {
            thumbnail.Dispose();
            result.TrySetResult(UploadReminderAnswer.Cancel);
        };
        var buttons = Ui.Horizontal(upload, cancel);
        buttons.HorizontalAlignment = HorizontalAlignment.Right;
        window.Content = Ui.Page(
            new Image { Source = thumbnail, MaxWidth = 360, MaxHeight = 220, Stretch = Stretch.Uniform },
            new TextBlock { Text = string.Format(Texts.Get("CustomUploader", "reminder_message"), server), TextWrapping = TextWrapping.Wrap, MaxWidth = 420 },
            stop,
            buttons);
        window.Show();
        window.Activate();
        return result.Task;
    }
}
