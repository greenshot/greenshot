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
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Helpers;
using Greenshot.Linux.Recipes.Ai;

namespace Greenshot.Linux.Ai;

/// <summary>
/// The question whether an AI tool may use Greenshot, asked the first time it connects
/// </summary>
internal static class AiConsentDialog
{
    /// <summary>
    /// The buttons only react after this time
    /// </summary>
    private static readonly TimeSpan ArmDelay = TimeSpan.FromSeconds(1);

    public static Task<bool> AskAsync(AiToolClient client, CancellationToken cancellationToken) =>
        Dispatcher.UIThread.InvokeAsync(() => ShowAsync(client)).WaitAsync(cancellationToken);

    /// <summary>
    /// On top, so the question doesn't end up behind the AI tool's window. It doesn't take the keyboard, has no default button,
    /// ignores Escape and its buttons only react after a moment: the user is probably typing in the AI tool, and a keystroke
    /// meant for it must not answer the question.
    /// </summary>
    private static Task<bool> ShowAsync(AiToolClient client)
    {
        var result = new TaskCompletionSource<bool>();
        var window = new Window
        {
            Title = Texts.Linux("ai_consent_title"),
            Icon = AppIcons.Window,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            Topmost = true,
            ShowActivated = false,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };
        var allow = new Button { Content = Texts.Linux("ai_consent_allow"), IsEnabled = false };
        var deny = new Button { Content = Texts.Linux("ai_consent_deny"), IsEnabled = false };
        string answer = "closed the question without an answer";
        allow.Click += (_, _) =>
        {
            answer = "clicked \"Allow\"";
            result.TrySetResult(true);
            window.Close();
        };
        deny.Click += (_, _) =>
        {
            answer = "clicked \"Don't Allow\"";
            window.Close();
        };
        window.Closed += (_, _) =>
        {
            Log.Info($"AI tool access for {client}: the user {answer}.");
            result.TrySetResult(false);
        };
        DispatcherTimer.RunOnce(() =>
        {
            allow.IsEnabled = true;
            deny.IsEnabled = true;
        }, ArmDelay);

        var buttons = Ui.Horizontal(allow, deny);
        buttons.HorizontalAlignment = HorizontalAlignment.Right;
        TextBlock Paragraph(string text, FontWeight weight = FontWeight.Normal) =>
            new() { Text = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 460, FontWeight = weight };
        window.Content = Ui.Page(
            Paragraph(Texts.Linux("ai_consent_message", client.DisplayName)),
            Paragraph(Texts.Linux("ai_consent_program", client.Identity)),
            Paragraph(Texts.Linux("ai_consent_note")),
            Paragraph(Texts.Linux("ai_consent_question", client.DisplayName), FontWeight.SemiBold),
            buttons);
        window.Show();
        return result.Task;
    }
}
