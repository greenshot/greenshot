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
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Helpers;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.Recipes.Ai;

namespace Greenshot.Linux.Settings;

/// <summary>
/// The AI tools tab: switching AI tools on, the programs which may use Greenshot, what they may do and the applications
/// they never see. Saved with OK, except "Ask again", which takes effect right away like on Windows.
/// </summary>
internal sealed class AiToolsSettingsPage(CoreSettings settings, List<Action> apply)
{
    private readonly List<string> _allowed = settings.AiToolsAllowedClients.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
    private readonly List<string> _excluded = settings.AiToolsExcludedProcesses.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
    private readonly StackPanel _allowedList = new() { Spacing = 4 };
    private readonly StackPanel _deniedList = new() { Spacing = 4 };
    private readonly StackPanel _excludedList = new() { Spacing = 4 };
    private Control? _deniedSection;

    private static string S(string key) => Texts.Label(Texts.Settings(key));

    public Control Create()
    {
        var enabled = Ui.Check(S("aitools_enabled"), settings.AiToolsEnabled);
        apply.Add(() => settings.AiToolsEnabled = enabled.IsChecked == true);
        var proposals = Ui.Check(S("aitools_proposals"), settings.AiToolsAllowRecipeProposals);
        apply.Add(() => settings.AiToolsAllowRecipeProposals = proposals.IsChecked == true);
        var notify = Ui.Check(S("aitools_notify"), settings.AiToolsNotifyOnCapture);
        apply.Add(() => settings.AiToolsNotifyOnCapture = notify.IsChecked == true);
        apply.Add(() =>
        {
            settings.AiToolsAllowedClients = _allowed;
            settings.AiToolsExcludedProcesses = _excluded;
        });

        _deniedSection = Ui.Section(S("aitools_denied"), Ui.Note(S("aitools_denied_description")), _deniedList);
        RefreshAllowed();
        RefreshDenied();
        RefreshExcluded();

        return Ui.Page(
            Ui.Section(S("aitools_access"), enabled, Ui.Note(Texts.Linux("ai_enabled_description")), McpSetup()),
            Ui.Section(S("aitools_programs"), Ui.Note(S("aitools_allowed")), _allowedList),
            _deniedSection,
            Ui.Section(S("aitools_permissions"), proposals, Ui.Note(S("aitools_proposals_description")), notify),
            Ui.Section(S("aitools_excluded"), ExcludedNotes(), _excludedList, AddExcludedRow()));
    }

    /// <summary>
    /// The command for the AI tool's MCP configuration
    /// </summary>
    private static Control McpSetup()
    {
        string server = Path.Combine(AppContext.BaseDirectory, AiToolCaller.McpServerFileName);
        if (!File.Exists(server))
        {
            return Ui.Note(Texts.Linux("ai_mcp_missing", AppContext.BaseDirectory));
        }

        var command = new TextBox { Text = server, IsReadOnly = true };
        return new StackPanel { Spacing = 4, Children = { Ui.Note(Texts.Linux("ai_mcp_setup")), command } };
    }

    private static Control ExcludedNotes()
    {
        var notes = new StackPanel { Spacing = 4, Children = { Ui.Note(Texts.Linux("ai_excluded_description")) } };
        if (RecipeServices.Compositor is { KnowsWindows: false } compositor)
        {
            notes.Children.Add(new TextBlock
            {
                Text = Texts.Linux("ai_excluded_unsupported", compositor.Name),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = Brushes.DarkOrange,
            });
        }

        return notes;
    }

    private static Control ItemRow(string text, params Button[] buttons)
    {
        var row = new DockPanel();
        var actions = Ui.Horizontal(buttons);
        DockPanel.SetDock(actions, Dock.Right);
        row.Children.Add(actions);
        row.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });
        ToolTip.SetTip(row, text);
        return row;
    }

    private void RefreshAllowed()
    {
        _allowedList.Children.Clear();
        if (_allowed.Count == 0)
        {
            _allowedList.Children.Add(Ui.Note(S("aitools_none")));
        }

        foreach (string client in _allowed.ToList())
        {
            var remove = new Button { Content = "✕" };
            ToolTip.SetTip(remove, S("aitools_remove"));
            remove.Click += (_, _) =>
            {
                _allowed.Remove(client);
                RefreshAllowed();
            };
            _allowedList.Children.Add(ItemRow(client, remove));
        }
    }

    private void RefreshDenied()
    {
        _deniedList.Children.Clear();
        var denied = AiToolAccess.GetDeniedClients().Where(c => !_allowed.Contains(c)).ToList();
        if (_deniedSection != null)
        {
            _deniedSection.IsVisible = denied.Count > 0;
        }

        foreach (string client in denied)
        {
            var allow = new Button { Content = S("aitools_denied_allow") };
            allow.Click += (_, _) =>
            {
                // Saved with OK; until then it is still refused
                _allowed.Add(client);
                apply.Add(() => AiToolAccess.ForgetDenied(client));
                RefreshAllowed();
                RefreshDenied();
            };
            var askAgain = new Button { Content = S("aitools_denied_askagain") };
            askAgain.Click += (_, _) =>
            {
                AiToolAccess.ForgetDenied(client);
                RefreshDenied();
            };
            _deniedList.Children.Add(ItemRow(client, allow, askAgain));
        }
    }

    private void RefreshExcluded()
    {
        _excludedList.Children.Clear();
        foreach (string application in _excluded.ToList())
        {
            var remove = new Button { Content = "✕" };
            ToolTip.SetTip(remove, S("aitools_remove"));
            remove.Click += (_, _) =>
            {
                _excluded.Remove(application);
                RefreshExcluded();
            };
            _excludedList.Children.Add(ItemRow(application, remove));
        }
    }

    private Control AddExcludedRow()
    {
        var name = new TextBox { Width = 260, PlaceholderText = "keepassxc" };
        ToolTip.SetTip(name, Texts.Linux("ai_excluded_add_tooltip"));
        var add = new Button { Content = S("aitools_add") };
        void Add()
        {
            string value = name.Text?.Trim() ?? string.Empty;
            if (value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                value = value[..^4];
            }

            if (value.Length > 0 && value.IndexOf(',') < 0 && !_excluded.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                _excluded.Add(value);
                RefreshExcluded();
            }

            name.Text = string.Empty;
        }

        add.Click += (_, _) => Add();
        name.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Add();
                e.Handled = true;
            }
        };
        return Ui.Horizontal(name, add);
    }
}
