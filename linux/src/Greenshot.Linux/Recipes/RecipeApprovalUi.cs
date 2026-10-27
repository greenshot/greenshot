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
using Avalonia.Threading;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Helpers;
using Greenshot.Linux.Recipes.Approval;
using Greenshot.Linux.Recipes.Pipeline;
using Greenshot.Linux.Recipes.Triggers;
using Greenshot.Linux.Recipes.ViewModels;

using Greenshot.Linux.Recipes;

namespace Greenshot.Linux.RecipeUi;

/// <summary>
/// The approval window of recipe files, like the RecipeApprovalWindow of Windows: what the recipe does (described by Greenshot),
/// a switch per trigger, a permission per kind of gated action, the changes, or the validation errors. It is modal and
/// synchronous like the WPF window, with a nested frame of the dispatcher.
/// </summary>
internal sealed class RecipeApprovalUi : IRecipeApprovalUi
{
    private static string T(string key, params object?[] args) => Texts.Get("Recipe", key, args);

    public ApprovalResult ShowApproval(RecipeApprovalRequest request)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            return Dispatcher.UIThread.Invoke(() => ShowApproval(request));
        }

        var window = new ApprovalWindow(request);
        RunModal(window);
        return window.Result!;
    }

    public void ShowValidationError(string filePath, RecipeValidationResult validation, CaptureRecipe recipe, string rawErrorMessage)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var request = new RecipeApprovalRequest
            {
                FilePath = filePath,
                Recipe = recipe,
                Validation = validation ?? new RecipeValidationResult(),
            };
            if ((validation == null || validation.Errors.Count == 0) && !string.IsNullOrWhiteSpace(rawErrorMessage))
            {
                request.Validation.AddError(rawErrorMessage);
            }
            else if (request.Validation.Errors.Count == 0)
            {
                request.Validation.AddError(T("approval_validation_unknown"));
            }

            new ApprovalWindow(request).Show();
        });
    }

    /// <summary>
    /// Show the window and wait for it on the UI thread, like ShowDialog of WPF
    /// </summary>
    private static void RunModal(Window window)
    {
        var frame = new DispatcherFrame();
        window.Closed += (_, _) => frame.Continue = false;
        window.Show();
        window.Activate();
        Dispatcher.UIThread.PushFrame(frame);
    }

    private sealed class ApprovalWindow : Window
    {
        private readonly RecipeApprovalRequest _request;
        private readonly List<(string Key, CheckBox Check, bool DisabledInRecipe, string Title)> _triggers = new();
        private readonly List<(RecipeGateType Gate, CheckBox Check)> _gates = new();
        private readonly Button _approve = new() { IsDefault = true };
        private readonly CheckBox _openInEditor = new();
        private readonly RecipeApprovalMode _mode;

        public ApprovalResult? Result { get; private set; }

        public ApprovalWindow(RecipeApprovalRequest request)
        {
            _request = request;
            var recipe = request.Recipe;
            var validation = request.Validation;
            Icon = AppIcons.Window;
            Width = 720;
            Height = 760;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;

            var previous = request.PreviousRecord;
            bool invalid = validation != null && !validation.IsValid;
            string title, description, badge;
            if (invalid)
            {
                _mode = RecipeApprovalMode.ValidationError;
                title = T(request.Extension != null ? "approval_ext_validation_failed" : "approval_validation_failed");
                description = T(request.Extension != null ? "approval_ext_validation_description" : "approval_validation_description");
                badge = T("approval_badge_validation");
            }
            else if (request.IsAiProposal)
            {
                _mode = RecipeApprovalMode.NewRecipe;
                title = T(request.ReplacedRecipe != null ? "approval_ai_title_change" : "approval_ai_title_new");
                description = T("approval_ai_description");
                badge = T("approval_badge_ai");
            }
            else if (request.IsOwnEdit)
            {
                _mode = previous != null ? RecipeApprovalMode.Modified : RecipeApprovalMode.NewRecipe;
                title = T("approval_own_title");
                description = T("approval_own_description", string.Join(" ", request.OwnEditReasons));
                badge = T("approval_badge_own");
            }
            else if (previous != null && !string.Equals(previous.Sha256Hash, request.ContentHash, StringComparison.OrdinalIgnoreCase))
            {
                _mode = RecipeApprovalMode.Modified;
                title = T(request.Extension != null ? "approval_ext_modified_title" : "approval_modified_title");
                description = T(request.Extension != null ? "approval_ext_modified_description" : "approval_modified_description");
                badge = T("approval_badge_modified");
            }
            else if (previous != null)
            {
                _mode = RecipeApprovalMode.ReVerify;
                title = T(request.Extension != null ? "approval_ext_review_title" : "approval_review_title");
                description = T(request.Extension != null ? "approval_ext_review_description" : "approval_review_description");
                badge = T("approval_badge_approved");
            }
            else
            {
                _mode = RecipeApprovalMode.NewRecipe;
                title = T(request.Extension != null ? "approval_ext_new_title" : "approval_new_title");
                description = T(request.Extension != null ? "approval_ext_new_description" : "approval_new_description");
                badge = T(request.Extension != null ? "approval_badge_ext_new" : "approval_badge_new");
            }

            bool readOnly = request.IsReadOnly && !invalid;
            if (readOnly)
            {
                title = T(request.Extension != null ? "approval_ext_details_title" : "approval_details_title");
                description = request.Extension != null ? T("approval_ext_details_description")
                    : string.IsNullOrEmpty(request.FilePath) ? T("approval_details_builtin_description") : T("approval_details_description");
                if (string.IsNullOrEmpty(request.FilePath))
                {
                    badge = T("approval_badge_builtin");
                }
            }

            Title = $"Greenshot — {title}";
            var page = new StackPanel { Margin = new Thickness(18), Spacing = 12 };
            page.Children.Add(Header(title, description, badge, invalid));

            // The recipe
            string name = recipe?.Name ?? (File.Exists(request.FilePath) ? Path.GetFileName(request.FilePath) : T("approval_unnamed_recipe"));
            var info = new StackPanel { Spacing = 4 };
            info.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(recipe?.Version) ? name : $"{name}  v{recipe.Version}", FontSize = 16, FontWeight = FontWeight.SemiBold });
            info.Children.Add(Wrapped(string.IsNullOrWhiteSpace(recipe?.Description) ? T(invalid ? "approval_failed_validation" : "approval_no_description") : recipe.Description));
            info.Children.Add(Small($"{T("approval_recipe_id")} {recipe?.Id ?? T("approval_unknown")}"));
            info.Children.Add(Small($"{T("approval_file_path")} {(string.IsNullOrEmpty(request.FilePath) ? T("approval_builtin_path") : request.FilePath)}"));
            if (!string.IsNullOrEmpty(request.ContentHash))
            {
                info.Children.Add(Small($"SHA-256: {request.ContentHash}"));
            }

            if (previous != null && _mode is RecipeApprovalMode.Modified or RecipeApprovalMode.ReVerify)
            {
                info.Children.Add(Small($"{T("approval_previously_approved")} {previous.ApprovedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}"));
            }

            page.Children.Add(Card(info));

            if (request.IsAiProposal)
            {
                var ai = new StackPanel { Spacing = 4 };
                ai.Children.Add(Bold(T("approval_ai_written")));
                ai.Children.Add(Small(string.IsNullOrWhiteSpace(request.ProposedByPath) ? request.ProposedByName : $"{request.ProposedByName} ({request.ProposedByPath})"));
                if (!string.IsNullOrWhiteSpace(request.AiRequest))
                {
                    ai.Children.Add(Small(T("approval_ai_request")));
                    ai.Children.Add(Wrapped(request.AiRequest));
                }

                if (!string.IsNullOrWhiteSpace(request.AiExplanation))
                {
                    ai.Children.Add(Small(T("approval_ai_explanation")));
                    ai.Children.Add(Wrapped(request.AiExplanation));
                }

                ai.Children.Add(Small(T("approval_ai_check")));
                page.Children.Add(Card(ai));
            }

            if (invalid)
            {
                var errors = new StackPanel { Spacing = 6 };
                errors.Children.Add(Bold(T("approval_validation_info")));
                foreach (var error in validation!.Errors)
                {
                    var item = RecipeValidationErrorViewModel.Create(error);
                    errors.Children.Add(new TextBlock { Text = "• " + item.ErrorMessage, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.IndianRed });
                    if (!string.IsNullOrEmpty(item.DiagnosticHint))
                    {
                        errors.Children.Add(Small($"   {T("approval_hint")} {item.DiagnosticHint}"));
                    }
                }

                page.Children.Add(Card(errors));
            }
            else
            {
                if (request.Extension != null)
                {
                    var reach = new StackPanel { Spacing = 4 };
                    reach.Children.Add(Bold(T("approval_ext_reach_title")));
                    reach.Children.Add(Small(T("approval_ext_reach_text")));
                    foreach (var line in request.ExtensionReach ?? [])
                    {
                        reach.Children.Add(Wrapped("• " + line));
                    }

                    page.Children.Add(Card(reach));
                }
                else
                {
                    page.Children.Add(Card(TriggersPanel(recipe, previous, readOnly)));
                }

                page.Children.Add(Card(StepsPanel(recipe)));
                if (ChangesPanel(recipe, previous) is { } changes)
                {
                    page.Children.Add(Card(changes));
                }

                if (validation is { HasGatedActions: true })
                {
                    page.Children.Add(Card(GatesPanel(validation, previous?.GetApproval(recipe?.Id), readOnly)));
                }
            }

            page.Children.Add(Buttons(invalid, readOnly));
            Content = new ScrollViewer { Content = page };
            UpdateApproveEnabled();
            Closed += (_, _) => Result ??= null;
        }

        private static Control Header(string title, string description, string badge, bool error)
        {
            var header = new StackPanel { Spacing = 6 };
            header.Children.Add(new Border
            {
                Background = error ? Brushes.IndianRed : new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock { Text = badge, Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeight.Bold },
            });
            header.Children.Add(new TextBlock { Text = title, FontSize = 20, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
            header.Children.Add(Wrapped(description));
            return header;
        }

        private Control TriggersPanel(CaptureRecipe? recipe, RecipeTrustRecord? previous, bool readOnly)
        {
            var panel = new StackPanel { Spacing = 6 };
            panel.Children.Add(Bold(T("approval_what_starts_it")));
            var triggers = recipe == null ? [] : RecipeDescriber.DescribeTriggers(recipe);
            if (triggers.Count == 0)
            {
                panel.Children.Add(Small("ℹ " + T("approval_trigger_manual")));
                return panel;
            }

            bool startSwitchedOff = _request.IsAiProposal || _request.StartSwitchedOff;
            var previousApproval = previous?.GetApproval(recipe?.Id);
            var suggested = _request.SuggestedApproval;
            bool keepPrevious = suggested != null || previous != null && _mode != RecipeApprovalMode.Modified;
            var basis = suggested ?? previousApproval;
            bool defaultOn = suggested != null || !startSwitchedOff;
            foreach (var trigger in triggers)
            {
                string label = trigger.IsDisabled ? T("approval_trigger_disabled", trigger.Label) : trigger.Label;
                var check = Ui.Check(label, keepPrevious && basis != null ? basis.IsTriggerApproved(trigger.Key) : defaultOn, trigger.Risk);
                check.IsEnabled = !readOnly;
                _triggers.Add((trigger.Key, check, trigger.IsDisabled, label));
                panel.Children.Add(check);
                if (!string.IsNullOrEmpty(trigger.Risk))
                {
                    panel.Children.Add(Small("   " + trigger.Risk));
                }
            }

            if (!readOnly)
            {
                panel.Children.Add(Small(defaultOn ? T("approval_triggers_on_hint") : "⚠ " + T("approval_triggers_off_hint")));
            }

            return panel;
        }

        private static Control StepsPanel(CaptureRecipe? recipe)
        {
            var panel = new StackPanel { Spacing = 4 };
            panel.Children.Add(Bold(T("approval_what_it_does")));
            panel.Children.Add(Small(T("approval_described_by")));
            if (recipe?.Nodes == null)
            {
                return panel;
            }

            foreach (var line in RecipeDescriber.DescribeSteps(recipe))
            {
                string icon = line.Risk switch
                {
                    RecipeGateType.NetworkAccess => "🌐",
                    RecipeGateType.ExternalCommand => "⚙",
                    RecipeGateType.FileSystemAccess => "📁",
                    RecipeGateType.Custom => "⚠",
                    _ => "▶",
                };
                var text = new TextBlock
                {
                    Text = $"{icon} {line.Text}",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(line.IsDetail ? 24 : 0, 0, 0, 0),
                    FontWeight = line.Risk.HasValue && !line.IsDetail ? FontWeight.SemiBold : FontWeight.Normal,
                };
                if (line.Risk.HasValue)
                {
                    text.Foreground = Brushes.DarkOrange;
                }

                panel.Children.Add(text);
            }

            return panel;
        }

        private Control? ChangesPanel(CaptureRecipe? recipe, RecipeTrustRecord? previous)
        {
            var previousRecipe = _request.ReplacedRecipe;
            string? previousContent = _request.PreviousContent;
            if (previousRecipe == null && _mode == RecipeApprovalMode.Modified && !string.IsNullOrEmpty(previous?.ApprovedContent))
            {
                previousContent ??= previous.ApprovedContent;
                try
                {
                    previousRecipe = RecipeSerializer.DeserializeList(previous.ApprovedContent, validate: false)
                        .FirstOrDefault(r => string.Equals(r.Id, recipe?.Id, StringComparison.OrdinalIgnoreCase));
                }
                catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException or NotSupportedException)
                {
                    previousRecipe = null;
                }
            }

            string? title = _request.ReplacedRecipe != null
                ? T(_request.ReplacesBuiltIn ? "approval_changes_replaces_builtin" : "approval_changes_recipe", _request.ReplacedRecipe.Name)
                : _mode == RecipeApprovalMode.Modified ? T(previousRecipe != null ? "approval_changes_since" : "approval_changes_unknown") : null;
            if (title == null)
            {
                return null;
            }

            var panel = new StackPanel { Spacing = 4 };
            panel.Children.Add(Bold(title));
            if (previousRecipe != null && recipe != null)
            {
                var changes = RecipeDescriber.DescribeChanges(previousRecipe, recipe);
                foreach (var change in changes.Count == 0 ? [T("approval_no_change")] : changes)
                {
                    panel.Children.Add(Wrapped("• " + change));
                }
            }

            if (!string.IsNullOrEmpty(previousContent) && !string.IsNullOrEmpty(_request.Content))
            {
                string diff = RecipeTextDiff.ToUnifiedText(previousContent, _request.Content);
                var show = new Button { Content = T("approval_view_changes") };
                show.Click += (_, _) => ShowText(T("approval_changes_overlay_title"), diff);
                panel.Children.Add(show);
            }

            return panel;
        }

        private Control GatesPanel(RecipeValidationResult validation, RecipeApproval? previousApproval, bool readOnly)
        {
            var panel = new StackPanel { Spacing = 6 };
            panel.Children.Add(Bold(T("approval_permission_title")));
            panel.Children.Add(Small(T("approval_permission_text")));
            bool startSwitchedOff = _request.IsAiProposal || _request.StartSwitchedOff;
            foreach (var group in validation.GatedActions.GroupBy(a => a.GateType).OrderBy(g => g.Key))
            {
                bool isChecked = _mode == RecipeApprovalMode.ReVerify && previousApproval != null
                    ? previousApproval.IsGateAllowed(group.Key)
                    : _request.IsOwnEdit && previousApproval?.IsGateAllowed(group.Key) == true ||
                      !startSwitchedOff && group.Key != RecipeGateType.ExternalCommand && group.Key != RecipeGateType.Custom;
                var check = Ui.Check(GateQuestion(group.Key), isChecked);
                check.IsEnabled = !readOnly;
                check.IsCheckedChanged += (_, _) => UpdateApproveEnabled();
                _gates.Add((group.Key, check));
                panel.Children.Add(check);
                if (GateExplanation(group.Key) is { } explanation)
                {
                    panel.Children.Add(Small("   " + explanation));
                }

                foreach (var target in group.Select(a => "• " + RecipeDescriber.DescribeGatedAction(a)).Distinct())
                {
                    panel.Children.Add(new TextBlock { Text = target, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(24, 0, 0, 0), FontSize = 12 });
                }
            }

            return panel;
        }

        private static string GateQuestion(RecipeGateType gate) => gate switch
        {
            RecipeGateType.ExternalCommand => T("approval_gate_external_command"),
            RecipeGateType.NetworkAccess => T("approval_gate_network"),
            RecipeGateType.FileSystemAccess => T("approval_gate_files"),
            _ => T("approval_gate_custom"),
        };

        private static string? GateExplanation(RecipeGateType gate) => gate switch
        {
            RecipeGateType.ExternalCommand => T("approval_gate_external_command_explanation"),
            RecipeGateType.NetworkAccess => T("approval_gate_network_explanation"),
            RecipeGateType.FileSystemAccess => T("approval_gate_files_explanation"),
            _ => null,
        };

        private Control Buttons(bool invalid, bool readOnly)
        {
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
            var viewJson = new Button { Content = T("approval_view_json") };
            viewJson.Click += (_, _) => ShowText(T("approval_json_title"), _request.Content
                ?? (File.Exists(_request.FilePath) ? File.ReadAllText(_request.FilePath) : _request.Recipe != null ? RecipeSerializer.Serialize(_request.Recipe) : T("approval_json_unavailable")));
            buttons.Children.Add(viewJson);
            if (!string.IsNullOrEmpty(_request.FilePath) && File.Exists(_request.FilePath))
            {
                var folder = new Button { Content = Texts.Linux("open_folder") };
                folder.Click += (_, _) => ProcessRunner.StartDetached("xdg-open", Path.GetDirectoryName(_request.FilePath)!);
                buttons.Children.Add(folder);
                if (invalid)
                {
                    var edit = new Button { Content = T("approval_edit_file") };
                    edit.Click += (_, _) => ProcessRunner.StartDetached("xdg-open", _request.FilePath);
                    buttons.Children.Add(edit);
                }
            }

            if (invalid || readOnly)
            {
                var close = new Button { Content = T(readOnly ? "approval_done" : "approval_done"), IsDefault = true, IsCancel = true };
                close.Click += (_, _) => Close();
                buttons.Children.Add(close);
                return buttons;
            }

            if (_request.IsAiProposal)
            {
                _openInEditor.Content = T("approval_open_in_editor");
                buttons.Children.Insert(0, _openInEditor);
            }

            _approve.Content = _mode switch
            {
                RecipeApprovalMode.ReVerify => T("approval_btn_save_changes"),
                RecipeApprovalMode.Modified when !_request.IsOwnEdit => T("approval_btn_approve_changes"),
                _ when _request.IsAiProposal => T("approval_btn_save_recipe"),
                _ when _request.IsOwnEdit => T("approval_btn_approve_save"),
                _ => T("approval_btn_approve_enable"),
            };
            _approve.Click += async (_, _) => await ApproveAsync();
            buttons.Children.Add(_approve);
            if (_mode == RecipeApprovalMode.ReVerify)
            {
                var keep = new Button { Content = T("approval_btn_keep_as_is"), IsCancel = true };
                ToolTip.SetTip(keep, T("approval_keep_tooltip"));
                keep.Click += (_, _) => Close();
                buttons.Children.Add(keep);
                var revoke = new Button { Content = T("approval_btn_revoke") };
                revoke.Click += async (_, _) =>
                {
                    if (await RecipeWindows.ChooseAsync(this, T("approval_btn_revoke"), T("approval_revoke_question", _request.Recipe?.Name),
                            [T("approval_btn_revoke_short"), T("approval_btn_keep")]) == 0)
                    {
                        Result = ApprovalResult.Revoked;
                        Close();
                    }
                };
                buttons.Children.Add(revoke);
            }
            else
            {
                var reject = new Button { Content = T("approval_btn_reject"), IsCancel = true };
                reject.Click += (_, _) => Close();
                buttons.Children.Add(reject);
            }

            return buttons;
        }

        private void UpdateApproveEnabled() => _approve.IsEnabled = _gates.All(g => g.Check.IsChecked == true);

        private async Task ApproveAsync()
        {
            if (_gates.Any(g => g.Check.IsChecked != true))
            {
                return;
            }

            // A trigger left off is easy to miss: say so before it is approved like that
            var leftOff = _triggers.Where(t => t.Check.IsChecked != true && !t.DisabledInRecipe).Select(t => "• " + t.Title).ToList();
            if (leftOff.Count > 0)
            {
                string text = T(leftOff.Count == 1 ? "approval_left_off_one" : "approval_left_off_many") + "\n\n" + string.Join("\n", leftOff) + "\n\n" + T("approval_left_off_later");
                if (await RecipeWindows.ChooseAsync(this, T("approval_left_off_title"), text, [T("approval_btn_approve"), T("approval_btn_go_back")]) != 0)
                {
                    return;
                }
            }

            var approval = new RecipeApproval
            {
                RecipeId = _request.Recipe?.Id,
                ApprovedTriggers = _triggers.Where(t => t.Check.IsChecked == true).Select(t => t.Key).ToList(),
                AllowedGates = _gates.Where(g => g.Check.IsChecked == true).Select(g => g.Gate).ToList(),
                ReplacesBuiltIn = _request.ReplacesBuiltIn,
            };
            Result = new ApprovalResult(approval, _openInEditor.IsChecked == true);
            Close();
        }

        private void ShowText(string title, string text)
        {
            var box = new TextBox { Text = text, IsReadOnly = true, AcceptsReturn = true, FontFamily = new FontFamily("monospace"), TextWrapping = TextWrapping.NoWrap };
            var copy = new Button { Content = T("approval_copy_clipboard") };
            copy.Click += async (_, _) => await SystemClipboard.SetTextAsync(text);
            var viewer = new Window
            {
                Title = title,
                Icon = AppIcons.Window,
                Width = 760,
                Height = 600,
                Content = new DockPanel { Margin = new Thickness(8), Children = { copy, box } },
            };
            DockPanel.SetDock(copy, Dock.Bottom);
            viewer.ShowDialog(this);
        }

        private static Border Card(Control child) => new()
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Child = child,
        };

        private static TextBlock Bold(string text) => new() { Text = text, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };

        private static TextBlock Wrapped(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };

        private static TextBlock Small(string text) => Ui.Note(text);
    }
}
