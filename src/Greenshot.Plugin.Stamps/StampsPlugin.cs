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

using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Dapplo.Ini;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Forms;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Plugin.Stamps.Editor;
using Greenshot.Plugin.Stamps.Library;
using Greenshot.Plugin.Stamps.ViewModels;
using Greenshot.Plugin.Stamps.Views;

namespace Greenshot.Plugin.Stamps;

/// <summary>
/// Stamps: small images (PNG, SVG, ...) collected in a folder, with names and tags, inserted into the editor as elements
/// or given to other programs with the clipboard or by dragging. A beta feature: without "beta tester" nothing is registered.
/// See docs/vNext/stamps.md.
/// </summary>
public class StampsPlugin : IGreenshotPlugin
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(StampsPlugin));
    private IStampsConfiguration _config;
    private EditorTracker _editors;
    private TrayMenuEntry _trayMenuEntry;
    private StampsWindow _window;
    private int _hotkeyId = -1;
    // Shown for an editor window: it stays above the editor window which was active last
    private bool _followsEditor;

    public ValueTask DisposeAsync() => default;

    public string Name => "Stamps";

    /// <summary>
    /// The feature is for beta testers only
    /// </summary>
    internal static bool IsEnabled => IniConfigRegistry.GetSection<ICoreConfiguration>()?.IsBetaTester == true;

    public void ConfigureServices(IPluginServices services)
    {
        Texts.Register<IStampsLanguage>(new StampsLanguageImpl());
        var section = new StampsConfigurationImpl();
        services.AddConfiguration(section);
        _config = section;

        _editors = new EditorTracker(editor => ShowStamps(editor));
        _editors.ActiveEditorChanged += OnActiveEditorChanged;
        // Decided with the loaded configuration: nothing is registered for a user who isn't a beta tester
        services.AddServices<IEditorPlugin>(() => IsEnabled ? new IEditorPlugin[] { _editors } : Array.Empty<IEditorPlugin>());
    }

    /// <summary>
    /// The tray menu entry and the hotkey (on the UI thread)
    /// </summary>
    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<IUiDispatcher>().RunOnUiAsync(Start, cancellationToken);

    private void Start()
    {
        if (!IsEnabled)
        {
            return;
        }

        _trayMenuEntry = new TrayMenuEntry(Texts.Get<IStampsLanguage>().Menu);
        _trayMenuEntry.Click += (s, e) => ShowStamps(null);
        PluginUtils.AddToContextMenu(_trayMenuEntry);
        Texts.Config.LanguageChanged += OnLanguageChanged;

        RegisterHotkey();
        if (_config is INotifyPropertyChanged notify)
        {
            notify.PropertyChanged += OnConfigPropertyChanged;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        UiDispatcher.Current.RunOnUiAsync(() =>
        {
            Texts.Config.LanguageChanged -= OnLanguageChanged;
            if (_config is INotifyPropertyChanged notify)
            {
                notify.PropertyChanged -= OnConfigPropertyChanged;
            }

            UnregisterHotkey();
            _trayMenuEntry?.Dispose();
            _trayMenuEntry = null;
            _window?.Close();
        }, cancellationToken);

    /// <summary>
    /// The stamps folder: the configured one, else Stamps next to greenshot.ini (so the portable Greenshot keeps it next to itself)
    /// </summary>
    internal string LibraryDirectory
    {
        get
        {
            string configured = _config?.LibraryPath;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return Environment.ExpandEnvironmentVariables(configured.Trim());
            }

            string iniDirectory = Path.GetDirectoryName(GreenshotEnvironment.ConfigLocation);
            if (string.IsNullOrEmpty(iniDirectory))
            {
                iniDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Greenshot");
            }

            return Path.Combine(iniDirectory, "Stamps");
        }
    }

    /// <summary>
    /// Show the stamps window, for the editor window at its right edge and above it; without an editor window as a window of its own
    /// </summary>
    private void ShowStamps(IEditorWindow editor)
    {
        try
        {
            if (_window == null)
            {
                var viewModel = new StampsViewModel(new StampLibrary(LibraryDirectory, StampImages.SupportedExtensions()), () => _editors.ActiveEditor);
                viewModel.Reload();
                _window = new StampsWindow(viewModel);
                _window.Closed += (s, e) => _window = null;
                _followsEditor = false;
                if (editor == null)
                {
                    _window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
            }

            if (editor != null)
            {
                _followsEditor = true;
                _window.SetOwner(editor.Handle);
                _window.PlaceNextTo(editor.Handle);
            }

            _window.ShowAndFocus();
        }
        catch (Exception ex)
        {
            Log.Error("Can't show the stamps", ex);
        }
    }

    /// <summary>
    /// The stamps go into the editor window which was active last: the window stays above that one
    /// </summary>
    private void OnActiveEditorChanged(object sender, EventArgs e)
    {
        if (_window == null)
        {
            return;
        }

        ((StampsViewModel)_window.DataContext).OnActiveEditorChanged();
        // Before the editor window is gone: an owned window would be closed with it. Without an editor window it is a window of its own.
        if (_followsEditor)
        {
            _window.SetOwner(_editors.ActiveEditor?.Handle ?? IntPtr.Zero);
        }
    }

    private void RegisterHotkey()
    {
        UnregisterHotkey();
        var sequence = HotkeySequence.Parse(_config?.Hotkey ?? string.Empty);
        // An unknown key name is parsed as no key: "Ctrl + Foo" would be registered as Ctrl alone
        if (sequence.IsEmpty || sequence.Chords.Exists(chord => chord.Key == VirtualKeyCode.None))
        {
            return;
        }

        var ui = UiDispatcher.Current;
        _hotkeyId = HotkeyManager.RegisterHotKey(sequence, () =>
            // Posted, the keyboard hook must return quickly
            ui.InvokeAsync(ShowStampsForForegroundWindow).FireAndLog("Show the stamps", Log));
        if (_hotkeyId < 0)
        {
            Log.WarnFormat("Can't register the hotkey {0} for the stamps", _config.Hotkey);
        }
    }

    /// <summary>
    /// With the hotkey in an editor window the stamps are shown for it, else as a window of their own
    /// </summary>
    private void ShowStampsForForegroundWindow()
    {
        var editor = _editors.ActiveEditor;
        ShowStamps(editor != null && editor.Handle == User32Api.GetForegroundWindow() ? editor : null);
    }

    private void UnregisterHotkey()
    {
        if (_hotkeyId >= 0)
        {
            HotkeyManager.UnregisterHotKey(_hotkeyId);
            _hotkeyId = -1;
        }
    }

    private void OnConfigPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IStampsConfiguration.Hotkey))
        {
            RegisterHotkey();
        }
    }

    private void OnLanguageChanged(object sender, EventArgs e)
    {
        if (_trayMenuEntry != null)
        {
            _trayMenuEntry.Text = Texts.Get<IStampsLanguage>().Menu;
        }
    }
}
