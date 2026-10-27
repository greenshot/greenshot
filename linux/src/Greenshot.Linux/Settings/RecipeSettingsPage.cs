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

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Helpers;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.Recipes.Approval;
using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Steps;

namespace Greenshot.Linux.Settings;

/// <summary>
/// The Recipes tab of Windows: the automatic steps (border, caption, drop shadow, ...) with their switch, options and where they
/// are used, the options of the recipes, which recipes are changed, and the approved recipe files
/// </summary>
internal sealed class RecipeSettingsPage(Window owner, IReadOnlyList<IDestination> destinations, IRecipeEditorService editor, List<Action> apply)
{
    private static string T(string key, params object?[] args) => Texts.Settings(key, args);

    private readonly List<OptionGroup> _groups = new();
    private readonly StackPanel _changes = new() { Spacing = 4 };
    private IReadOnlyList<CaptureRecipe> _recipes = [];

    public Control Create()
    {
        var page = Ui.Page(Ui.Note(T("recipes_description")));
        var manager = RecipeServices.Manager;
        if (manager == null)
        {
            return page;
        }

        page.Children.Add(GeneralSection(manager));

        _recipes = manager.GetAllRecipes().Where(r => r != null).OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        foreach (var extension in manager.GetAllExtensions().Where(e => e != null).OrderBy(e => e.Extends?.Order ?? 0).ThenBy(e => e.Id, StringComparer.OrdinalIgnoreCase))
        {
            var group = new OptionGroup(this, extension, _recipes.Where(r => RecipeComposer.CanExtend(extension, r)).ToList());
            _groups.Add(group);
            page.Children.Add(group.Create());
        }

        foreach (var recipe in _recipes.Where(r => r.HasOptions))
        {
            var group = new OptionGroup(this, recipe, null);
            _groups.Add(group);
            page.Children.Add(group.Create());
        }

        page.Children.Add(new Separator());
        page.Children.Add(Ui.Section(T("recipes_changes"), _changes));
        UpdateChanges();
        page.Children.Add(new Separator());
        page.Children.Add(ApprovalsSection(manager));
        apply.Add(() =>
        {
            foreach (var group in _groups)
            {
                group.Save();
            }
        });
        return page;
    }

    /// <summary>
    /// Recipes on or off and the recipe files, the settings of the Recipe Editor plugin of Windows, and its two windows
    /// </summary>
    private Control GeneralSection(IRecipeManager manager)
    {
        var configuration = new RecipeConfiguration();
        var enabled = Ui.Check(T("expert_enablerecipes"), configuration.Enabled);
        var files = new TextBox { Text = configuration.RecipeFiles ?? string.Empty, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MinWidth = 420 };
        var managerButton = new Button { Content = Texts.WithoutAccessKey(Texts.Core("contextmenu_managerecipes")) };
        managerButton.Click += (_, _) => editor.OpenRecipeManager();
        var editorButton = new Button { Content = Texts.WithoutAccessKey(Texts.Core("contextmenu_recipeeditor")) };
        editorButton.Click += (_, _) => editor.OpenEditor();
        apply.Add(() =>
        {
            bool reload = configuration.Enabled != (enabled.IsChecked == true) || (configuration.RecipeFiles ?? string.Empty) != (files.Text ?? string.Empty).Trim();
            configuration.Enabled = enabled.IsChecked == true;
            configuration.RecipeFiles = (files.Text ?? string.Empty).Trim();
            if (reload)
            {
                manager.ReloadRecipes();
            }
        });
        return Ui.Section(Texts.Settings("recipes"),
            enabled,
            new TextBlock { Text = Texts.Get("RecipeEditor", "config_recipe_files"), Margin = new Thickness(0, 6, 0, 0) },
            files,
            new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, Children = { managerButton, editorButton } });
    }

    /// <summary>
    /// One line per combination of automatic steps, with the recipes they change
    /// </summary>
    private void UpdateChanges()
    {
        var extensionGroups = _groups.Where(g => g.Extension != null).ToList();
        RecipeExtensionSettings SettingsOf(RecipeExtension extension) => extensionGroups.First(g => g.Extension == extension).CurrentSettings();
        _changes.Children.Clear();
        var combinations = _recipes
            .Select(r => (Recipe: r, Extensions: string.Join(", ", RecipeComposer.FindApplicableExtensions(r, extensionGroups.Select(g => g.Extension!), SettingsOf)
                .Select(e => RecipeText.Translate(e.Name ?? e.Id)))))
            .Where(c => c.Extensions.Length > 0)
            .GroupBy(c => c.Extensions)
            .ToList();
        if (combinations.Count == 0)
        {
            _changes.Children.Add(Ui.Note(T("recipes_changes_none")));
        }

        foreach (var combination in combinations)
        {
            _changes.Children.Add(new TextBlock
            {
                Text = $"{combination.Key}: {string.Join(", ", combination.Select(c => c.Recipe.Name ?? c.Recipe.Id))}",
                TextWrapping = TextWrapping.Wrap,
            });
        }
    }

    /// <summary>
    /// The recipe files and what was approved for them: details, review, revoke (Settings > AI tools > Approved recipes of Windows)
    /// </summary>
    private Control ApprovalsSection(IRecipeManager manager)
    {
        var list = new StackPanel { Spacing = 6 };
        void Refresh()
        {
            list.Children.Clear();
            var files = manager.GetAllRecipes().Where(r => !string.IsNullOrEmpty(r.FilePath)).Select(r => (r.Id, Name: r.Name ?? r.Id, Path: r.FilePath))
                .Concat(manager.GetAllExtensions().Where(e => !string.IsNullOrEmpty(e.FilePath)).Select(e => (e.Id, Name: RecipeText.Translate(e.Name ?? e.Id), Path: e.FilePath)))
                .OrderBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            if (files.Count == 0)
            {
                list.Children.Add(Ui.Note(Texts.Linux("recipe_no_files")));
            }

            foreach (var file in files)
            {
                var record = RecipeTrustStore.GetTrustRecord(file.Path);
                var details = new Button { Content = T("recipeapprovals_details") };
                details.Click += (_, _) => manager.ShowRecipeDetails(file.Id);
                var review = new Button { Content = T("recipeapprovals_review") };
                review.Click += (_, _) =>
                {
                    manager.ReviewApproval(file.Id);
                    Refresh();
                };
                var revoke = new Button { Content = T("recipeapprovals_revoke") };
                revoke.Click += async (_, _) =>
                {
                    if (await RecipeUi.RecipeWindows.ChooseAsync(owner, T("recipeapprovals_revoke"), Texts.Get("Recipe", "approval_revoke_question", file.Name),
                            [Texts.Get("Recipe", "approval_btn_revoke_short"), Texts.Get("Recipe", "approval_btn_keep")]) == 0)
                    {
                        if (manager is RecipeManager recipeManager)
                        {
                            recipeManager.RevokeApproval(file.Id, file.Path);
                        }

                        Refresh();
                    }
                };
                var text = new StackPanel { Spacing = 2 };
                text.Children.Add(new TextBlock { Text = file.Name, FontWeight = FontWeight.SemiBold });
                text.Children.Add(Ui.Note(file.Path));
                if (record != null)
                {
                    text.Children.Add(Ui.Note($"{Texts.Get("Recipe", "approval_previously_approved")} {record.ApprovedAt.ToLocalTime():yyyy-MM-dd HH:mm}"));
                }

                list.Children.Add(new StackPanel { Spacing = 4, Children = { text, Ui.Horizontal(details, review, revoke) } });
            }
        }

        Refresh();
        return Ui.Section(T("recipeapprovals"), Ui.Note(T("recipeapprovals_description")), list);
    }

    /// <summary>
    /// The options of a recipe or an automatic step, like the RecipeOptionGroupViewModel of Windows
    /// </summary>
    private sealed class OptionGroup
    {
        private readonly RecipeSettingsPage _page;
        private readonly FlowDefinition _definition;
        private readonly List<(RecipeOption Option, Func<object?> Value, Action<object?> Set, Control Control)> _items = new();
        private readonly Dictionary<string, bool> _useIn;
        private readonly Dictionary<string, bool> _onlyDestinations = new(StringComparer.OrdinalIgnoreCase);
        private readonly IReadOnlyList<CaptureRecipe> _extensible;
        private readonly TextBlock _scope = new() { VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        private CheckBox? _switch;

        public OptionGroup(RecipeSettingsPage page, FlowDefinition definition, IReadOnlyList<CaptureRecipe>? extensible)
        {
            _page = page;
            _definition = definition;
            _extensible = extensible ?? [];
            _useIn = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (Extension != null)
            {
                // Checked: the extension changes the recipe; new recipes get it too, so only the unchecked ones are stored
                var stored = RecipeExtensionSettings.FromStore(Extension);
                foreach (var recipe in _extensible)
                {
                    _useIn[recipe.Id] = (stored.ApplyToAll || stored.OnlyRecipes.Contains(recipe.Id)) && !stored.ExceptRecipes.Contains(recipe.Id);
                }

                if (Extension.SlotName == RecipeSlots.BeforeDestination)
                {
                    foreach (var destination in page.Destinations)
                    {
                        _onlyDestinations[destination.Designation] = stored.OnlyDestinations.Count == 0 || stored.OnlyDestinations.Contains(destination.Designation);
                    }
                }
            }
        }

        public RecipeExtension? Extension => _definition as RecipeExtension;

        public Control Create()
        {
            var body = new StackPanel { Spacing = 8, Margin = new Thickness(24, 0, 0, 0) };
            var header = new StackPanel { Spacing = 4 };
            string name = RecipeText.Translate(_definition.Name ?? _definition.Id);
            var options = (_definition.Options ?? []).Where(o => o != null).ToList();
            var switchOption = Extension == null ? null
                : options.FirstOrDefault(o => o.Type == ContractDataType.Boolean && string.Equals(o.Key, RecipeExtension.EnabledOptionKey, StringComparison.OrdinalIgnoreCase));
            if (switchOption != null)
            {
                _switch = Ui.Check(name, RecipeOptionStore.GetValue(_definition, switchOption) is true);
                _switch.FontWeight = FontWeight.SemiBold;
                _switch.IsCheckedChanged += (_, _) =>
                {
                    body.IsVisible = _switch.IsChecked == true;
                    _page.UpdateChanges();
                };
                _items.Add((switchOption, () => _switch.IsChecked == true, v => _switch.IsChecked = v is true, _switch));
                header.Children.Add(_switch);
            }
            else
            {
                header.Children.Add(new TextBlock { Text = name, FontWeight = FontWeight.SemiBold, FontSize = 15 });
            }

            if (RecipeText.Translate(_definition.Description) is { Length: > 0 } description)
            {
                header.Children.Add(Ui.Note(description));
            }

            foreach (var option in options.Where(o => o != switchOption))
            {
                body.Children.Add(OptionControl(option));
            }

            // An option switched by another one (EnabledWhen) can only be changed while that one is on
            foreach (var item in _items.Where(i => !string.IsNullOrWhiteSpace(i.Option.EnabledWhen)))
            {
                var controller = _items.FirstOrDefault(i => string.Equals(i.Option.Key, item.Option.EnabledWhen, StringComparison.OrdinalIgnoreCase));
                if (controller.Control is CheckBox check)
                {
                    item.Control.IsEnabled = check.IsChecked == true;
                    check.IsCheckedChanged += (_, _) => item.Control.IsEnabled = check.IsChecked == true;
                }
            }

            if (Extension != null && _useIn.Count > 0)
            {
                var change = new Button { Content = T("recipes_change") };
                change.Click += async (_, _) => await ChangeScopeAsync();
                UpdateScope();
                body.Children.Add(Ui.Horizontal(new TextBlock { Text = T("recipes_usedfor"), VerticalAlignment = VerticalAlignment.Center }, _scope, change));
            }

            var reset = new Button { Content = T("recipes_reset") };
            reset.Click += (_, _) => Reset();
            body.Children.Add(reset);
            body.IsVisible = _switch == null || _switch.IsChecked == true;
            return new StackPanel { Spacing = 6, Children = { header, body } };
        }

        private Control OptionControl(RecipeOption option)
        {
            object? value = RecipeOptionStore.GetValue(_definition, option);
            Control control;
            Func<object?> get;
            Action<object?> set;
            string label = option.DisplayLabel;
            switch (option.Type)
            {
                case ContractDataType.Boolean:
                    var check = Ui.Check(label, value is true, option.DisplayDescription);
                    check.IsCheckedChanged += (_, _) => _page.UpdateChanges();
                    _items.Add((option, () => check.IsChecked == true, v => check.IsChecked = v is true, check));
                    return check;
                case ContractDataType.Integer or ContractDataType.Decimal:
                    var number = new NumericUpDown
                    {
                        Value = Convert.ToDecimal(value ?? 0, CultureInfo.InvariantCulture),
                        Minimum = option.Min ?? decimal.MinValue,
                        Maximum = option.Max ?? decimal.MaxValue,
                        FormatString = option.Type == ContractDataType.Integer ? "0" : "0.##",
                        Increment = option.Type == ContractDataType.Integer ? 1 : 0.1m,
                        Width = 160,
                    };
                    control = number;
                    get = () => option.Type == ContractDataType.Integer ? (object)(int)(number.Value ?? 0) : (double)(number.Value ?? 0);
                    set = v => number.Value = Convert.ToDecimal(v ?? 0, CultureInfo.InvariantCulture);
                    break;
                case ContractDataType.Enum:
                    var choices = option.Choices?.Where(c => c != null).ToList() ?? [];
                    var combo = new ComboBox { ItemsSource = choices.Select(c => c.DisplayLabel).ToList(), MinWidth = 160 };
                    combo.SelectedIndex = Math.Max(0, choices.FindIndex(c => string.Equals(c.Value, value as string, StringComparison.OrdinalIgnoreCase)));
                    control = combo;
                    get = () => choices.Count == 0 ? null : choices[Math.Max(0, combo.SelectedIndex)].Value;
                    set = v => combo.SelectedIndex = Math.Max(0, choices.FindIndex(c => string.Equals(c.Value, v as string, StringComparison.OrdinalIgnoreCase)));
                    break;
                case ContractDataType.Color:
                    var colorText = new TextBox { Text = value as string ?? string.Empty, Width = 120 };
                    var swatch = new Border { Width = 24, Height = 24, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3) };
                    void UpdateSwatch()
                    {
                        var c = RecipeColors.Parse(colorText.Text, SkiaSharp.SKColors.Transparent);
                        swatch.Background = new SolidColorBrush(Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue));
                    }

                    colorText.TextChanged += (_, _) => UpdateSwatch();
                    UpdateSwatch();
                    control = Ui.Horizontal(colorText, swatch);
                    get = () => colorText.Text;
                    set = v => colorText.Text = v as string ?? string.Empty;
                    break;
                default:
                    var text = new TextBox { Text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty, Width = 320 };
                    control = text;
                    get = () => text.Text;
                    set = v => text.Text = Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty;
                    break;
            }

            _items.Add((option, get, set, control));
            string? range = option.Type is ContractDataType.Integer or ContractDataType.Decimal && (option.Min.HasValue || option.Max.HasValue)
                ? $"{option.Min?.ToString(CultureInfo.CurrentCulture) ?? "…"} – {option.Max?.ToString(CultureInfo.CurrentCulture) ?? "…"}"
                : null;
            return Ui.Row(label, control, string.Join("\n", new[] { option.DisplayDescription, range }.Where(t => !string.IsNullOrWhiteSpace(t))));
        }

        public RecipeExtensionSettings CurrentSettings() => new()
        {
            Enabled = _switch == null || _switch.IsChecked == true,
            ApplyToAll = true,
            ExceptRecipes = new HashSet<string>(_useIn.Where(u => !u.Value).Select(u => u.Key), StringComparer.OrdinalIgnoreCase),
            // All destinations checked: stored as "all", so destinations added later get it too
            OnlyDestinations = _onlyDestinations.Count == 0 || _onlyDestinations.Values.All(v => v)
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(_onlyDestinations.Where(d => d.Value).Select(d => d.Key), StringComparer.OrdinalIgnoreCase),
        };

        private void UpdateScope()
        {
            int used = _useIn.Values.Count(v => v);
            string captures = used == _useIn.Count ? T("recipes_scope_allcaptures") : used == 0 ? T("recipes_scope_nocaptures") : T("recipes_scope_somecaptures", used, _useIn.Count);
            if (_onlyDestinations.Count > 0)
            {
                var names = _onlyDestinations.Where(d => d.Value).Select(d => _page.Destinations.FirstOrDefault(x => x.Designation == d.Key)?.DisplayName ?? d.Key).ToList();
                captures += " → " + (names.Count == _onlyDestinations.Count ? T("recipes_scope_alldestinations") : string.Join(", ", names));
            }

            _scope.Text = captures;
        }

        /// <summary>
        /// The captures (recipes) and the destinations side by side, like the RecipeExtensionScopeWindow of Windows
        /// </summary>
        private async Task ChangeScopeAsync()
        {
            var window = new Window
            {
                Title = T("recipes_scope_title", RecipeText.Translate(_definition.Name ?? _definition.Id)),
                Icon = AppIcons.Window,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            var captureChecks = _extensible.Select(r => (r.Id, Check: Ui.Check(r.Name ?? r.Id, _useIn[r.Id]))).ToList();
            var destinationChecks = _onlyDestinations.Select(d => (d.Key, Check: Ui.Check(_page.Destinations.FirstOrDefault(x => x.Designation == d.Key)?.DisplayName ?? d.Key, d.Value))).ToList();
            var error = new TextBlock { Foreground = Brushes.IndianRed, IsVisible = false, Text = T("recipes_scope_nodestination") };
            Control Column(string title, IEnumerable<CheckBox> checks)
            {
                var list = checks.ToList();
                var all = new Button { Content = T("recipes_scope_all") };
                all.Click += (_, _) => list.ForEach(c => c.IsChecked = true);
                var none = new Button { Content = T("recipes_scope_none") };
                none.Click += (_, _) => list.ForEach(c => c.IsChecked = false);
                var panel = new StackPanel { Spacing = 4 };
                panel.Children.AddRange(list);
                return Ui.Section(title, Ui.Horizontal(all, none), new ScrollViewer { MaxHeight = 420, Content = panel });
            }

            var columns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 24 };
            columns.Children.Add(Column(T("recipes_scope_captures"), captureChecks.Select(c => c.Check)));
            if (destinationChecks.Count > 0)
            {
                columns.Children.Add(Column(T("recipes_scope_destinations"), destinationChecks.Select(c => c.Check)));
            }

            var ok = new Button { Content = Texts.Linux("ok"), IsDefault = true };
            var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true };
            cancel.Click += (_, _) => window.Close();
            ok.Click += (_, _) =>
            {
                if (destinationChecks.Count > 0 && destinationChecks.All(d => d.Check.IsChecked != true))
                {
                    error.IsVisible = true;
                    return;
                }

                foreach (var (id, check) in captureChecks)
                {
                    _useIn[id] = check.IsChecked == true;
                }

                foreach (var (designation, check) in destinationChecks)
                {
                    _onlyDestinations[designation] = check.IsChecked == true;
                }

                UpdateScope();
                _page.UpdateChanges();
                window.Close();
            };
            var buttons = Ui.Horizontal(ok, cancel);
            buttons.HorizontalAlignment = HorizontalAlignment.Right;
            window.Content = Ui.Page(Ui.Note(T("recipes_scope_description")), columns, error, buttons);
            await window.ShowDialog(_page.Owner);
        }

        private void Reset()
        {
            foreach (var item in _items.Where(i => i.Control != _switch))
            {
                item.Set(item.Option.GetDefaultValue());
            }

            foreach (var key in _useIn.Keys.ToList())
            {
                _useIn[key] = true;
            }

            foreach (var key in _onlyDestinations.Keys.ToList())
            {
                _onlyDestinations[key] = true;
            }

            if (Extension != null && _useIn.Count > 0)
            {
                UpdateScope();
            }

            _page.UpdateChanges();
        }

        public void Save()
        {
            foreach (var item in _items)
            {
                RecipeOptionStore.SetValue(_definition.Id, item.Option, item.Value());
            }

            if (Extension == null)
            {
                return;
            }

            var settings = CurrentSettings();
            RecipeOptionStore.SetValue(Extension.Id, RecipeExtension.ApplyToOption, RecipeExtension.ApplyToAll);
            RecipeOptionStore.SetValue(Extension.Id, RecipeExtension.OnlyRecipesOption, string.Empty);
            RecipeOptionStore.SetValue(Extension.Id, RecipeExtension.ExceptRecipesOption, string.Join(",", settings.ExceptRecipes.OrderBy(i => i, StringComparer.OrdinalIgnoreCase)));
            RecipeOptionStore.SetValue(Extension.Id, RecipeExtension.OnlyDestinationsOption, string.Join(",", settings.OnlyDestinations.OrderBy(i => i, StringComparer.OrdinalIgnoreCase)));
        }
    }

    private Window Owner => owner;

    private IReadOnlyList<IDestination> Destinations { get; } = destinations
        .Where(d => !string.Equals(d.Designation, WellKnownDestinations.Picker, StringComparison.OrdinalIgnoreCase)).ToList();
}
