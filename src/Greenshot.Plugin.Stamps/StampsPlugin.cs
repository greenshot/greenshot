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
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
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
    private StampLibrary _library;
    private TrayMenuEntry _trayMenuEntry;
    private StampsWindow _window;
    private int _hotkeyId = -1;

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

        // In the editor the stamps are a panel. Decided with the loaded configuration: nothing is registered for a user who isn't a beta tester
        var panel = new StampsPanel(() => Library);
        services.AddServices<IEditorPanel>(() => IsEnabled ? new IEditorPanel[] { panel } : Array.Empty<IEditorPanel>());
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
        _trayMenuEntry.Click += (s, e) => ShowStamps();
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
    /// The stamps folder, shared by the window and the panels of all editor windows: read the first time it's needed
    /// </summary>
    private StampLibrary Library
    {
        get
        {
            if (_library == null)
            {
                _library = new StampLibrary(LibraryDirectory, StampImages.SupportedExtensions());
                try
                {
                    _library.Load();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Log.Warn($"Can't read the stamps folder {_library.Directory}", ex);
                }
            }

            return _library;
        }
    }

    /// <summary>
    /// Show the stamps in a window of their own, without an editor: to give them to other programs. In the editor they are a panel.
    /// </summary>
    private void ShowStamps()
    {
        try
        {
            if (_window == null)
            {
                var viewModel = new StampsViewModel(Library, () => null);
                viewModel.Reload();
                _window = new StampsWindow(viewModel)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                _window.Closed += (s, e) => _window = null;
            }

            _window.ShowAndFocus();
        }
        catch (Exception ex)
        {
            Log.Error("Can't show the stamps", ex);
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
            ui.InvokeAsync(ShowStamps).FireAndLog("Show the stamps", Log));
        if (_hotkeyId < 0)
        {
            Log.WarnFormat("Can't register the hotkey {0} for the stamps", _config.Hotkey);
        }
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
