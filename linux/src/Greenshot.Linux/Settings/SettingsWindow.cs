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
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Core.Naming;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Editing;
using Greenshot.Linux.Helpers;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.Recipes.Triggers;

namespace Greenshot.Linux.Settings;

/// <summary>
/// What the settings window needs from the running Greenshot
/// </summary>
internal interface ISettingsHost : IEditorHost
{
    /// <summary>
    /// Does the desktop give Greenshot its keys (GlobalShortcuts portal)?
    /// </summary>
    bool HasGlobalShortcuts { get; }

    IReadOnlyList<Core.Plugins.IGreenshotPlugin> Plugins { get; }

    Core.Plugins.PluginPolicy Policy { get; }

    Core.Configuration.ISecretStore Secrets { get; }

    /// <summary>
    /// All destinations which are switched on, for the destinations an automatic step is used for
    /// </summary>
    IReadOnlyList<Core.Destinations.IDestination> RecipeDestinations { get; }

    /// <summary>
    /// The settings were saved: apply language, theme and the tray menu
    /// </summary>
    void ApplySettings();
}

/// <summary>
/// The settings window, the SettingsWindow of Windows with the tabs that apply to Linux.
/// Nothing is changed until OK is pressed.
/// </summary>
internal sealed class SettingsWindow : Window
{
    private readonly ISettingsHost _host;
    private readonly CoreSettings _settings;
    private readonly List<Action> _apply = new();
    private readonly TabItem _recipesTab;
    private readonly TabItem _aiToolsTab;

    public SettingsWindow(ISettingsHost host)
    {
        _host = host;
        _settings = host.Settings;
        Title = Texts.Settings("title");
        Icon = AppIcons.Window;
        Width = 760;
        Height = 620;
        MinWidth = 560;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var tabs = new TabControl { TabStripPlacement = Dock.Left };
        tabs.Items.Add(Tab(Texts.Settings("general"), GeneralPage()));
        tabs.Items.Add(Tab(Texts.Settings("capture"), CapturePage()));
        tabs.Items.Add(Tab(Texts.Settings("output"), OutputPage()));
        tabs.Items.Add(Tab(Texts.Settings("destination"), DestinationPage()));
        tabs.Items.Add(Tab(Texts.Settings("editor"), EditorPage()));
        tabs.Items.Add(Tab(Texts.Settings("printer"), PrinterPage()));
        tabs.Items.Add(Tab(Texts.Linux("plugins"), new PluginSettingsPage(host, this, _apply).Create()));
        _recipesTab = Tab(Texts.Settings("recipes"), new RecipeSettingsPage(this, host.RecipeDestinations, host.RecipeEditor, _apply).Create());
        tabs.Items.Add(_recipesTab);
        _aiToolsTab = Tab(Texts.Settings("aitools"), new AiToolsSettingsPage(_settings, _apply).Create());
        tabs.Items.Add(_aiToolsTab);

        var ok = new Button { Content = Texts.Linux("ok"), IsDefault = true, MinWidth = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
        ok.Click += (_, _) => Save();
        var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true, MinWidth = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
        cancel.Click += (_, _) => Close();
        var openIni = new Button { Content = Texts.Linux("open_ini") };
        openIni.Click += (_, _) => ProcessRunner.StartDetached("xdg-open", _settings.FilePath);

        var buttons = new DockPanel { Margin = new Thickness(12) };
        DockPanel.SetDock(openIni, Dock.Left);
        buttons.Children.Add(openIni);
        buttons.Children.Add(Ui.Horizontal(ok, cancel).With(p => p.HorizontalAlignment = HorizontalAlignment.Right));

        var root = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);
        root.Children.Add(tabs);
        Content = root;
    }

    private static TabItem Tab(string header, Control content) => new()
    {
        Header = Texts.WithoutAccessKey(header),
        Content = new ScrollViewer { Content = content, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled },
    };

    /// <summary>
    /// Selects a page by name: "recipes" (the "More…" of the quick settings) or "aitools"
    /// </summary>
    public void ShowPage(string? page)
    {
        if (page == "recipes")
        {
            _recipesTab.IsSelected = true;
        }
        else if (page == "aitools")
        {
            _aiToolsTab.IsSelected = true;
        }
    }

    private void Save()
    {
        foreach (var apply in _apply)
        {
            apply();
        }

        _settings.Save();
        _host.ApplySettings();
        Log.Info("The settings were saved");
        Close();
    }

    #region General

    private Control GeneralPage()
    {
        var languages = Texts.AvailableLanguages;
        var language = new ComboBox
        {
            ItemsSource = languages.Select(l => l.Description).ToList(),
            SelectedIndex = Math.Max(0, languages.ToList().FindIndex(l => l.Ietf.Equals(Texts.Current, StringComparison.OrdinalIgnoreCase))),
            MinWidth = 240,
        };
        _apply.Add(() =>
        {
            if (language.SelectedIndex >= 0)
            {
                _settings.Language = languages[language.SelectedIndex].Ietf;
            }
        });

        string[] themes = ["System", "Light", "Dark"];
        var theme = new ComboBox
        {
            ItemsSource = new[] { Texts.Linux("theme_system"), Texts.Settings("theme_light"), Texts.Settings("theme_dark") },
            SelectedIndex = Math.Max(0, Array.FindIndex(themes, t => t.Equals(_settings.Theme, StringComparison.OrdinalIgnoreCase))),
            MinWidth = 240,
        };
        _apply.Add(() => _settings.Theme = themes[Math.Max(0, theme.SelectedIndex)]);

        var autostart = Ui.Check(Texts.Linux("autostart"), Autostart.IsEnabled);
        _apply.Add(() =>
        {
            if (autostart.IsChecked != Autostart.IsEnabled)
            {
                Autostart.SetEnabled(autostart.IsChecked == true);
            }
        });

        var notifications = Ui.Check(Texts.Settings("shownotify"), _settings.ShowTrayNotification);
        _apply.Add(() => _settings.ShowTrayNotification = notifications.IsChecked == true);

        return Ui.Page(
            Ui.Section(Texts.Settings("applicationsettings"),
                Ui.Row(Texts.Settings("language"), language, Texts.Settings("tooltip_language")),
                Ui.Note(Texts.Linux("language_restart")),
                Ui.Row(Texts.Settings("theme"), theme),
                autostart,
                Ui.Note(Texts.Linux("autostart_compositor")),
                notifications),
            HotkeySection());
    }

    /// <summary>
    /// Wayland applications can't grab keys: either the desktop manages them (portal) or the user binds the commands
    /// </summary>
    private Control HotkeySection()
    {
        var list = new StackPanel { Spacing = 4 };
        (string Text, string Hotkey, string Command)[] hotkeys =
        [
            (Texts.Core("contextmenu_capturearea"), _settings.RegionHotkey, "capture region"),
            (Texts.Core("contextmenu_capturewindow"), _settings.WindowHotkey, "capture activewindow"),
            (Texts.Core("contextmenu_capturefullscreen"), _settings.FullscreenHotkey, "capture fullscreen"),
            (Texts.Core("contextmenu_capturelastregion"), _settings.LastregionHotkey, "capture last"),
            .. RecipeHotkeys(),
        ];
        foreach (var (text, hotkey, _) in hotkeys)
        {
            list.Children.Add(Ui.Row(Texts.WithoutAccessKey(text), new TextBlock { Text = hotkey, VerticalAlignment = VerticalAlignment.Center }));
        }

        if (_host.HasGlobalShortcuts)
        {
            return Ui.Section(Texts.Core("hotkeys"), Ui.Note(Texts.Linux("hotkeys_portal")), list);
        }

        var lines = string.Join('\n', hotkeys.Select(h => $"bindsym {SwayKey(h.Hotkey)} exec greenshot {h.Command}"));
        var config = new TextBox
        {
            Text = lines,
            IsReadOnly = true,
            AcceptsReturn = true,
            FontFamily = new FontFamily("monospace"),
        };
        var copy = new Button { Content = Texts.Linux("hotkeys_copy") };
        copy.Click += async (_, _) => await SystemClipboard.SetTextAsync(lines);
        return Ui.Section(Texts.Core("hotkeys"), Ui.Note(Texts.Linux("hotkeys_manual")), config, copy);
    }

    /// <summary>
    /// The hotkeys of the recipes: bound with the portal like the others, or by the user with "greenshot recipe &lt;id&gt;"
    /// </summary>
    private static IEnumerable<(string Text, string Hotkey, string Command)> RecipeHotkeys()
    {
        if (RecipeServices.Manager == null)
        {
            return [];
        }

        return TriggerManager.Instance.GetAllTriggers().OfType<HotkeyTrigger>()
            .Where(t => t.IsEnabled && !string.IsNullOrWhiteSpace(t.HotkeyString))
            .Select(t => (RecipeText.Translate(RecipeServices.Manager.GetRecipeById(t.TargetRecipeId)?.Name ?? t.TargetRecipeId), t.HotkeyString, $"recipe {t.TargetRecipeId}"))
            .OrderBy(h => h.Item1, StringComparer.CurrentCultureIgnoreCase);
    }

    /// <summary>
    /// "Shift + PrintScreen" as Sway writes it: Shift+Print
    /// </summary>
    internal static string SwayKey(string hotkey) => string.Join('+', hotkey
        .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(part => part.ToLowerInvariant() switch
        {
            "printscreen" or "print" => "Print",
            "ctrl" or "control" => "Ctrl",
            "alt" => "Alt",
            "shift" => "Shift",
            "win" or "super" => "Mod4",
            _ => part,
        }));

    #endregion

    #region Capture

    private Control CapturePage()
    {
        var mouse = Ui.Check(Texts.Settings("capture_mousepointer"), _settings.CaptureMousepointer);
        _apply.Add(() => _settings.CaptureMousepointer = mouse.IsChecked == true);
        var sound = Ui.Check(Texts.Settings("playsound"), _settings.PlayCameraSound);
        _apply.Add(() => _settings.PlayCameraSound = sound.IsChecked == true);
        var zoomer = Ui.Check(Texts.Settings("zoom"), _settings.ZoomerEnabled);
        _apply.Add(() => _settings.ZoomerEnabled = zoomer.IsChecked == true);
        var delay = new NumericUpDown { Value = _settings.CaptureDelay, Minimum = 0, Maximum = 10000, Increment = 50, FormatString = "0", Width = 160 };
        _apply.Add(() => _settings.CaptureDelay = (int)(delay.Value ?? 0));
        var interactive = Ui.Check(Texts.Settings("capture_windows_interactive"), _settings.CaptureWindowsInteractive);
        _apply.Add(() => _settings.CaptureWindowsInteractive = interactive.IsChecked == true);

        string[] backends = ["Auto", "Grim", "Portal"];
        var backend = new ComboBox
        {
            ItemsSource = new[] { Texts.Linux("capture_backend_auto"), Texts.Linux("capture_backend_grim"), Texts.Linux("capture_backend_portal") },
            SelectedIndex = Math.Max(0, Array.FindIndex(backends, b => b.Equals(_settings.CaptureBackend, StringComparison.OrdinalIgnoreCase))),
        };
        _apply.Add(() => _settings.CaptureBackend = backends[Math.Max(0, backend.SelectedIndex)]);

        var ocrLanguages = new TextBox { Text = _settings.OcrLanguages, Width = 220, HorizontalAlignment = HorizontalAlignment.Left };
        _apply.Add(() => _settings.OcrLanguages = ocrLanguages.Text?.Trim() ?? string.Empty);
        var installed = Ui.Note(Texts.Linux("ocr_languages_hint", "…"));
        _ = ShowInstalledOcrLanguagesAsync(installed);

        return Ui.Page(
            Ui.Section(Texts.Settings("capture"),
                mouse,
                sound,
                zoomer,
                Ui.Row(Texts.Settings("waittime"), delay)),
            Ui.Section(Texts.Settings("windowscapture"), interactive),
            Ui.Section(Texts.Linux("capture_backend"), backend, Ui.Note(Texts.Linux("capture_backend_restart"))),
            Ui.Section(Texts.Settings("ocr"), Ui.Row(Texts.Linux("ocr_languages"), ocrLanguages), installed));
    }

    private static async Task ShowInstalledOcrLanguagesAsync(TextBlock note)
    {
        if (!Core.Ocr.TesseractOcr.IsAvailable)
        {
            note.Text = Texts.Linux("ocr_missing");
            return;
        }

        var languages = await Core.Ocr.TesseractOcr.GetLanguagesAsync();
        note.Text = Texts.Linux("ocr_languages_hint", string.Join(", ", languages));
    }

    #endregion

    #region Output

    private Control OutputPage()
    {
        var folder = new TextBox { Text = _settings.OutputFilePath };
        var choose = new Button { Content = Texts.Linux("choose") };
        choose.Click += async (_, _) =>
        {
            if (_host.FileChooser == null)
            {
                return;
            }

            var chosen = await _host.FileChooser.OpenFolderAsync(Texts.Settings("storagelocation_browse"));
            if (chosen != null)
            {
                folder.Text = chosen;
            }
        };
        choose.IsEnabled = _host.FileChooser != null;
        var folderRow = new DockPanel();
        DockPanel.SetDock(choose, Dock.Right);
        choose.Margin = new Thickness(6, 0, 0, 0);
        folderRow.Children.Add(choose);
        folderRow.Children.Add(folder);
        _apply.Add(() => _settings.OutputFilePath = folder.Text ?? string.Empty);

        var pattern = new TextBox { Text = _settings.OutputFileFilenamePattern };
        var example = Ui.Note(string.Empty);
        void UpdateExample()
        {
            // The example must not count up ${NUM}
            var preview = new FilenamePattern(() => _settings.OutputFileIncrementingNumber + 1) { Culture = Texts.Culture };
            var details = new CaptureDetails { Title = "Firefox" };
            example.Text = Texts.Linux("pattern_example", preview.GetFilename(pattern.Text ?? string.Empty, ImageFormats.Normalize(_settings.OutputFileFormat), details));
        }

        pattern.TextChanged += (_, _) => UpdateExample();
        UpdateExample();
        _apply.Add(() => _settings.OutputFileFilenamePattern = pattern.Text ?? string.Empty);

        var formats = ImageFormats.Supported.ToList();
        var format = new ComboBox
        {
            ItemsSource = formats,
            SelectedIndex = Math.Max(0, formats.IndexOf(ImageFormats.Normalize(_settings.OutputFileFormat))),
            MinWidth = 120,
        };
        _apply.Add(() => _settings.OutputFileFormat = formats[Math.Max(0, format.SelectedIndex)]);

        var quality = new Slider { Minimum = 1, Maximum = 100, Value = _settings.OutputFileJpegQuality, Width = 220, TickFrequency = 1, IsSnapToTickEnabled = true };
        var qualityText = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Width = 40 };
        quality.PropertyChanged += (_, e) =>
        {
            if (e.Property == Avalonia.Controls.Primitives.RangeBase.ValueProperty)
            {
                qualityText.Text = ((int)quality.Value).ToString(Texts.Culture);
            }
        };
        qualityText.Text = _settings.OutputFileJpegQuality.ToString(Texts.Culture);
        _apply.Add(() => _settings.OutputFileJpegQuality = (int)quality.Value);

        var copyPath = Ui.Check(Texts.Settings("copypathtoclipboard"), _settings.OutputFileCopyPathToClipboard);
        _apply.Add(() => _settings.OutputFileCopyPathToClipboard = copyPath.IsChecked == true);

        var counter = new NumericUpDown { Value = _settings.OutputFileIncrementingNumber, Minimum = 0, Maximum = 999999999, FormatString = "0", Width = 160 };
        _apply.Add(() => _settings.OutputFileIncrementingNumber = (uint)(counter.Value ?? 0));

        return Ui.Page(
            Ui.Section(Texts.Settings("preferredfilesettings"),
                Ui.Row(Texts.Settings("storagelocation"), folderRow, Texts.Settings("tooltip_storagelocation")),
                Ui.Row(Texts.Settings("filenamepattern"), pattern, Texts.Settings("tooltip_filenamepattern")),
                example,
                Ui.Note(Texts.Settings("message_filenamepattern")),
                Ui.Row(Texts.Settings("primaryimageformat"), format, Texts.Settings("tooltip_primaryimageformat")),
                copyPath,
                Ui.Row(Texts.Settings("expert_counter"), counter)),
            Ui.Section(Texts.Settings("qualitysettings"),
                Ui.Row(Texts.Settings("jpegquality"), Ui.Horizontal(quality, qualityText))));
    }

    #endregion

    #region Destinations

    private Control DestinationPage()
    {
        var configured = _settings.OutputDestinations;
        var picker = new RadioButton { Content = Texts.Settings("destination_picker"), GroupName = "destinations",
            IsChecked = configured.Contains(WellKnownDestinations.Picker, StringComparer.OrdinalIgnoreCase) };
        var chosen = new RadioButton { Content = Texts.Settings("destination"), GroupName = "destinations", IsChecked = picker.IsChecked != true };

        (string Designation, string Text)[] destinations =
        [
            (WellKnownDestinations.Editor, Texts.Settings("destination_editor")),
            (WellKnownDestinations.FileDefault, Texts.Settings("destination_file")),
            (WellKnownDestinations.FileWithDialog, Texts.Settings("destination_fileas")),
            (WellKnownDestinations.Clipboard, Texts.Settings("destination_clipboard")),
            (WellKnownDestinations.Printer, Texts.Settings("destination_printer")),
            (WellKnownDestinations.Email, Texts.Settings("destination_email")),
            (WellKnownDestinations.Ocr, Texts.Linux("ocr_destination")),
            (WellKnownDestinations.QrCode, Texts.Linux("qr_destination")),
            .. _host.Plugins.Where(p => _host.Policy.IsPluginEnabled(p.Name)).SelectMany(p => p.Destinations).Select(d => (d.Designation, d.DisplayName)),
        ];
        var list = new StackPanel { Spacing = 4, Margin = new Thickness(24, 0, 0, 0) };
        var checks = new List<(string Designation, CheckBox Check)>();
        foreach (var (designation, text) in destinations)
        {
            var check = Ui.Check(text, configured.Contains(designation, StringComparer.OrdinalIgnoreCase));
            checks.Add((designation, check));
            list.Children.Add(check);
        }

        void UpdateEnabled() => list.IsEnabled = chosen.IsChecked == true;
        picker.IsCheckedChanged += (_, _) => UpdateEnabled();
        chosen.IsCheckedChanged += (_, _) => UpdateEnabled();
        UpdateEnabled();

        _apply.Add(() =>
        {
            if (picker.IsChecked == true)
            {
                _settings.OutputDestinations = [WellKnownDestinations.Picker];
                return;
            }

            var selected = checks.Where(c => c.Check.IsChecked == true).Select(c => c.Designation).ToList();
            _settings.OutputDestinations = selected.Count > 0 ? selected : [WellKnownDestinations.Picker];
        });

        return Ui.Page(Ui.Section(Texts.Settings("destination"), picker, chosen, list));
    }

    #endregion

    #region Printer

    private Control PrinterPage()
    {
        CheckBox Option(string key, bool value, Action<bool> store)
        {
            var check = Ui.Check(Texts.Core(key), value);
            _apply.Add(() => store(check.IsChecked == true));
            return check;
        }

        var prompt = Ui.Check(Texts.Settings("alwaysshowprintoptionsdialog"), _settings.OutputPrintPromptOptions);
        _apply.Add(() => _settings.OutputPrintPromptOptions = prompt.IsChecked == true);
        var footerPattern = new TextBox { Text = _settings.OutputPrintFooterPattern };
        _apply.Add(() => _settings.OutputPrintFooterPattern = footerPattern.Text ?? string.Empty);

        return Ui.Page(
            Ui.Section(Texts.Settings("printoptions"), prompt),
            Ui.Section(Texts.Core("printoptions_layout"),
                Option("printoptions_allowshrink", _settings.OutputPrintAllowShrink, v => _settings.OutputPrintAllowShrink = v),
                Option("printoptions_allowenlarge", _settings.OutputPrintAllowEnlarge, v => _settings.OutputPrintAllowEnlarge = v),
                Option("printoptions_allowrotate", _settings.OutputPrintAllowRotate, v => _settings.OutputPrintAllowRotate = v),
                Option("printoptions_allowcenter", _settings.OutputPrintCenter, v => _settings.OutputPrintCenter = v),
                Option("printoptions_timestamp", _settings.OutputPrintFooter, v => _settings.OutputPrintFooter = v),
                Ui.Row(Texts.Settings("expert_footerpattern"), footerPattern)),
            Ui.Section(Texts.Core("printoptions_colors"),
                Option("printoptions_printgrayscale", _settings.OutputPrintGrayscale, v => _settings.OutputPrintGrayscale = v),
                Option("printoptions_printmonochrome", _settings.OutputPrintMonochrome, v => _settings.OutputPrintMonochrome = v),
                Option("printoptions_inverted", _settings.OutputPrintInverted, v => _settings.OutputPrintInverted = v)));
    }

    #endregion

    #region Editor

    private Control EditorPage()
    {
        var editor = _host.EditorSettings;
        var matchSize = Ui.Check(Texts.Editor("match_capture_size"), editor.MatchSizeToCapture);
        _apply.Add(() => editor.MatchSizeToCapture = matchSize.IsChecked == true);
        var suppress = Ui.Check(Texts.Settings("expert_suppresssavedialogatclose"), editor.SuppressSaveDialogAtClose);
        _apply.Add(() => editor.SuppressSaveDialogAtClose = suppress.IsChecked == true);
        var sensitivity = new NumericUpDown { Value = editor.FreehandSensitivity, Minimum = 1, Maximum = 100, FormatString = "0", Width = 160 };
        _apply.Add(() => editor.FreehandSensitivity = (int)(sensitivity.Value ?? 3));

        return Ui.Page(Ui.Section(Texts.Settings("editor"),
            matchSize,
            suppress,
            Ui.Row(Texts.Linux("editor_freehand_sensitivity"), sensitivity)));
    }

    #endregion
}

internal static class ControlExtensions
{
    public static T With<T>(this T control, Action<T> configure)
    {
        configure(control);
        return control;
    }
}
