/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Microsoft.Win32;

namespace Greenshot.Plugin.ExternalCommand.Forms;

public partial class ExternalCommandConfigurationControl : UserControl, INotifyPropertyChanged
{
    private static readonly IExternalCommandConfiguration ExternalCommandConfig = IniConfigRegistry.GetSection<IExternalCommandConfiguration>();
    private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

    public ObservableCollection<ExternalCommandItemViewModel> Commands { get; } = new ObservableCollection<ExternalCommandItemViewModel>();

    private ExternalCommandItemViewModel _selectedCommand;
    public ExternalCommandItemViewModel SelectedCommand
    {
        get => _selectedCommand;
        set
        {
            if (_selectedCommand != value)
            {
                _selectedCommand = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedCommand));
            }
        }
    }

    public bool HasSelectedCommand => SelectedCommand != null;

    public bool QuicklinkEnabled
    {
        get => ExternalCommandConfig?.QuicklinkEnabled ?? false;
        set
        {
            if (ExternalCommandConfig != null && ExternalCommandConfig.QuicklinkEnabled != value)
            {
                ExternalCommandConfig.QuicklinkEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public bool RedirectStandardError
    {
        get => ExternalCommandConfig?.RedirectStandardError ?? true;
        set
        {
            if (ExternalCommandConfig != null && ExternalCommandConfig.RedirectStandardError != value)
            {
                ExternalCommandConfig.RedirectStandardError = value;
                OnPropertyChanged();
            }
        }
    }

    public bool RedirectStandardOutput
    {
        get => ExternalCommandConfig?.RedirectStandardOutput ?? true;
        set
        {
            if (ExternalCommandConfig != null && ExternalCommandConfig.RedirectStandardOutput != value)
            {
                ExternalCommandConfig.RedirectStandardOutput = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanConfigureOutputOptions));
                OnPropertyChanged(nameof(CanConfigureUriToClipboard));
            }
        }
    }

    public bool ShowStandardOutputInLog
    {
        get => ExternalCommandConfig?.ShowStandardOutputInLog ?? false;
        set
        {
            if (ExternalCommandConfig != null && ExternalCommandConfig.ShowStandardOutputInLog != value)
            {
                ExternalCommandConfig.ShowStandardOutputInLog = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ParseOutputForUri
    {
        get => ExternalCommandConfig?.ParseOutputForUri ?? true;
        set
        {
            if (ExternalCommandConfig != null && ExternalCommandConfig.ParseOutputForUri != value)
            {
                ExternalCommandConfig.ParseOutputForUri = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanConfigureUriToClipboard));
            }
        }
    }

    public bool OutputToClipboard
    {
        get => ExternalCommandConfig?.OutputToClipboard ?? false;
        set
        {
            if (ExternalCommandConfig != null && ExternalCommandConfig.OutputToClipboard != value)
            {
                ExternalCommandConfig.OutputToClipboard = value;
                OnPropertyChanged();
            }
        }
    }

    public bool UriToClipboard
    {
        get => ExternalCommandConfig?.UriToClipboard ?? true;
        set
        {
            if (ExternalCommandConfig != null && ExternalCommandConfig.UriToClipboard != value)
            {
                ExternalCommandConfig.UriToClipboard = value;
                OnPropertyChanged();
            }
        }
    }

    public bool CanConfigureOutputOptions => RedirectStandardOutput;
    public bool CanConfigureUriToClipboard => RedirectStandardOutput && ParseOutputForUri;

    public ExternalCommandConfigurationControl()
    {
        DataContext = this;
        InitializeComponent();
        LoadCommands();
    }

    private void LoadCommands()
    {
        Commands.Clear();
        if (ExternalCommandConfig.Commands == null)
        {
            ExternalCommandConfig.Commands = new List<string>();
        }
        if (ExternalCommandConfig.Commandline == null)
        {
            ExternalCommandConfig.Commandline = new Dictionary<string, string>();
        }
        if (ExternalCommandConfig.Argument == null)
        {
            ExternalCommandConfig.Argument = new Dictionary<string, string>();
        }
        if (ExternalCommandConfig.OutputFormat == null)
        {
            ExternalCommandConfig.OutputFormat = new Dictionary<string, OutputFormat>();
        }
        if (ExternalCommandConfig.RunInbackground == null)
        {
            ExternalCommandConfig.RunInbackground = new Dictionary<string, bool>();
        }
        ExternalCommandConfig.RedirectStandardErrorCommand ??= new Dictionary<string, bool>();
        ExternalCommandConfig.RedirectStandardOutputCommand ??= new Dictionary<string, bool>();
        ExternalCommandConfig.ShowStandardOutputInLogCommand ??= new Dictionary<string, bool>();
        ExternalCommandConfig.ParseOutputForUriCommand ??= new Dictionary<string, bool>();
        ExternalCommandConfig.OutputToClipboardCommand ??= new Dictionary<string, bool>();
        ExternalCommandConfig.UriToClipboardCommand ??= new Dictionary<string, bool>();

        foreach (var cmd in ExternalCommandConfig.Commands)
        {
            var item = new ExternalCommandItemViewModel(ExternalCommandConfig, cmd);
            Commands.Add(item);
        }

        SelectedCommand = Commands.FirstOrDefault();
    }

    private void ButtonAdd_Click(object sender, RoutedEventArgs e)
    {
        int index = 1;
        string newName;
        do
        {
            newName = $"Command {index++}";
        } while (ExternalCommandConfig.Commands.Contains(newName));

        ExternalCommandConfig.Commands.Add(newName);
        ExternalCommandConfig.Commandline[newName] = string.Empty;
        ExternalCommandConfig.Argument[newName] = "\"{0}\"";
        ExternalCommandConfig.OutputFormat[newName] = CoreConfig?.OutputFileFormat ?? OutputFormat.png;
        ExternalCommandConfig.RunInbackground[newName] = true;
        ExternalCommandConfig.RedirectStandardErrorCommand[newName] = ExternalCommandConfig.RedirectStandardError;
        ExternalCommandConfig.RedirectStandardOutputCommand[newName] = ExternalCommandConfig.RedirectStandardOutput;
        ExternalCommandConfig.ShowStandardOutputInLogCommand[newName] = ExternalCommandConfig.ShowStandardOutputInLog;
        ExternalCommandConfig.ParseOutputForUriCommand[newName] = ExternalCommandConfig.ParseOutputForUri;
        ExternalCommandConfig.OutputToClipboardCommand[newName] = ExternalCommandConfig.OutputToClipboard;
        ExternalCommandConfig.UriToClipboardCommand[newName] = ExternalCommandConfig.UriToClipboard;

        var newItem = new ExternalCommandItemViewModel(ExternalCommandConfig, newName);
        Commands.Add(newItem);
        SelectedCommand = newItem;
    }

    private void ButtonDelete_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedCommand == null) return;

        string name = SelectedCommand.Name;
        ExternalCommandConfig.Delete(name);
        Commands.Remove(SelectedCommand);
        SelectedCommand = Commands.FirstOrDefault();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedCommand == null) return;

        var openFileDialog = new OpenFileDialog
        {
            Filter = "Executables (*.exe, *.bat, *.com, *.cmd)|*.exe;*.bat;*.com;*.cmd|All files (*.*)|*.*",
            FilterIndex = 1,
            CheckFileExists = true,
            Multiselect = false
        };

        string initialPath = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(SelectedCommand.CommandLine))
            {
                initialPath = Path.GetDirectoryName(SelectedCommand.CommandLine);
            }
        }
        catch
        {
            // Ignore invalid paths
        }

        openFileDialog.InitialDirectory = Directory.Exists(initialPath)
            ? initialPath
            : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        var window = Window.GetWindow(this);
        bool? result = window != null ? openFileDialog.ShowDialog(window) : openFileDialog.ShowDialog();
        if (result == true)
        {
            SelectedCommand.CommandLine = openFileDialog.FileName;
            if (string.IsNullOrWhiteSpace(SelectedCommand.Name) || SelectedCommand.Name.StartsWith("Command ", StringComparison.OrdinalIgnoreCase))
            {
                SelectedCommand.Name = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class ExternalCommandItemViewModel : INotifyPropertyChanged
{
    private readonly IExternalCommandConfiguration _config;
    private string _name;
    private string _commandLine;
    private string _arguments;
    private OutputFormat _outputFormat;
    private bool _runInBackground;
    private bool _redirectStandardError;
    private bool _redirectStandardOutput;
    private bool _showStandardOutputInLog;
    private bool _parseOutputForUri;
    private bool _outputToClipboard;
    private bool _uriToClipboard;
    private ImageSource _icon;

    public ExternalCommandItemViewModel(IExternalCommandConfiguration config, string commandName)
    {
        _config = config;
        _name = commandName;

        _commandLine = config.Commandline != null && config.Commandline.ContainsKey(commandName)
            ? config.Commandline[commandName]
            : string.Empty;

        _arguments = config.Argument != null && config.Argument.ContainsKey(commandName)
            ? config.Argument[commandName]
            : "\"{0}\"";

        _outputFormat = config.OutputFormat != null && config.OutputFormat.ContainsKey(commandName)
            ? config.OutputFormat[commandName]
            : OutputFormat.png;

        _runInBackground = config.RunInbackground != null && config.RunInbackground.ContainsKey(commandName)
            ? config.RunInbackground[commandName]
            : true;

        _redirectStandardError = config.RedirectStandardErrorCommand != null && config.RedirectStandardErrorCommand.TryGetValue(commandName, out var rse)
            ? rse
            : config.RedirectStandardError;

        _redirectStandardOutput = config.RedirectStandardOutputCommand != null && config.RedirectStandardOutputCommand.TryGetValue(commandName, out var rso)
            ? rso
            : config.RedirectStandardOutput;

        _showStandardOutputInLog = config.ShowStandardOutputInLogCommand != null && config.ShowStandardOutputInLogCommand.TryGetValue(commandName, out var sil)
            ? sil
            : config.ShowStandardOutputInLog;

        _parseOutputForUri = config.ParseOutputForUriCommand != null && config.ParseOutputForUriCommand.TryGetValue(commandName, out var pfu)
            ? pfu
            : config.ParseOutputForUri;

        _outputToClipboard = config.OutputToClipboardCommand != null && config.OutputToClipboardCommand.TryGetValue(commandName, out var otc)
            ? otc
            : config.OutputToClipboard;

        _uriToClipboard = config.UriToClipboardCommand != null && config.UriToClipboardCommand.TryGetValue(commandName, out var utc)
            ? utc
            : config.UriToClipboard;

        UpdateIcon();
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value && !string.IsNullOrWhiteSpace(value))
            {
                string oldName = _name;
                _name = value;

                // Rename in config
                if (_config.Commands != null)
                {
                    int index = _config.Commands.IndexOf(oldName);
                    if (index >= 0)
                    {
                        _config.Commands[index] = _name;
                    }
                }

                if (_config.Commandline != null && _config.Commandline.ContainsKey(oldName))
                {
                    string cmd = _config.Commandline[oldName];
                    _config.Commandline.Remove(oldName);
                    _config.Commandline[_name] = cmd;
                }

                if (_config.Argument != null && _config.Argument.ContainsKey(oldName))
                {
                    string arg = _config.Argument[oldName];
                    _config.Argument.Remove(oldName);
                    _config.Argument[_name] = arg;
                }

                if (_config.OutputFormat != null && _config.OutputFormat.ContainsKey(oldName))
                {
                    var fmt = _config.OutputFormat[oldName];
                    _config.OutputFormat.Remove(oldName);
                    _config.OutputFormat[_name] = fmt;
                }

                if (_config.RunInbackground != null && _config.RunInbackground.ContainsKey(oldName))
                {
                    bool rib = _config.RunInbackground[oldName];
                    _config.RunInbackground.Remove(oldName);
                    _config.RunInbackground[_name] = rib;
                }

                RenameInDictionary(_config.RedirectStandardErrorCommand, oldName, _name);
                RenameInDictionary(_config.RedirectStandardOutputCommand, oldName, _name);
                RenameInDictionary(_config.ShowStandardOutputInLogCommand, oldName, _name);
                RenameInDictionary(_config.ParseOutputForUriCommand, oldName, _name);
                RenameInDictionary(_config.OutputToClipboardCommand, oldName, _name);
                RenameInDictionary(_config.UriToClipboardCommand, oldName, _name);

                OnPropertyChanged();
            }
        }
    }

    public string CommandLine
    {
        get => _commandLine;
        set
        {
            if (_commandLine != value)
            {
                _commandLine = value;
                if (_config.Commandline != null)
                {
                    _config.Commandline[_name] = value;
                }
                UpdateIcon();
                OnPropertyChanged();
            }
        }
    }

    public string Arguments
    {
        get => _arguments;
        set
        {
            if (_arguments != value)
            {
                _arguments = value;
                if (_config.Argument != null)
                {
                    _config.Argument[_name] = value;
                }
                OnPropertyChanged();
            }
        }
    }

    public OutputFormat OutputFormat
    {
        get => _outputFormat;
        set
        {
            if (_outputFormat != value)
            {
                _outputFormat = value;
                if (_config.OutputFormat != null)
                {
                    _config.OutputFormat[_name] = value;
                }
                OnPropertyChanged();
            }
        }
    }

    public bool RunInBackground
    {
        get => _runInBackground;
        set
        {
            if (_runInBackground != value)
            {
                _runInBackground = value;
                if (_config.RunInbackground != null)
                {
                    _config.RunInbackground[_name] = value;
                }
                OnPropertyChanged();
            }
        }
    }

    public bool RedirectStandardError
    {
        get => _redirectStandardError;
        set
        {
            if (_redirectStandardError != value)
            {
                _redirectStandardError = value;
                if (_config.RedirectStandardErrorCommand != null)
                {
                    _config.RedirectStandardErrorCommand[_name] = value;
                }
                OnPropertyChanged();
            }
        }
    }

    public bool RedirectStandardOutput
    {
        get => _redirectStandardOutput;
        set
        {
            if (_redirectStandardOutput != value)
            {
                _redirectStandardOutput = value;
                if (_config.RedirectStandardOutputCommand != null)
                {
                    _config.RedirectStandardOutputCommand[_name] = value;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanConfigureOutputOptions));
                OnPropertyChanged(nameof(CanConfigureUriToClipboard));
            }
        }
    }

    public bool ShowStandardOutputInLog
    {
        get => _showStandardOutputInLog;
        set
        {
            if (_showStandardOutputInLog != value)
            {
                _showStandardOutputInLog = value;
                if (_config.ShowStandardOutputInLogCommand != null)
                {
                    _config.ShowStandardOutputInLogCommand[_name] = value;
                }
                OnPropertyChanged();
            }
        }
    }

    public bool ParseOutputForUri
    {
        get => _parseOutputForUri;
        set
        {
            if (_parseOutputForUri != value)
            {
                _parseOutputForUri = value;
                if (_config.ParseOutputForUriCommand != null)
                {
                    _config.ParseOutputForUriCommand[_name] = value;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanConfigureUriToClipboard));
            }
        }
    }

    public bool OutputToClipboard
    {
        get => _outputToClipboard;
        set
        {
            if (_outputToClipboard != value)
            {
                _outputToClipboard = value;
                if (_config.OutputToClipboardCommand != null)
                {
                    _config.OutputToClipboardCommand[_name] = value;
                }
                OnPropertyChanged();
            }
        }
    }

    public bool UriToClipboard
    {
        get => _uriToClipboard;
        set
        {
            if (_uriToClipboard != value)
            {
                _uriToClipboard = value;
                if (_config.UriToClipboardCommand != null)
                {
                    _config.UriToClipboardCommand[_name] = value;
                }
                OnPropertyChanged();
            }
        }
    }

    public bool CanConfigureOutputOptions => RedirectStandardOutput;
    public bool CanConfigureUriToClipboard => RedirectStandardOutput && ParseOutputForUri;

    private static void RenameInDictionary<T>(IDictionary<string, T> dict, string oldKey, string newKey)
    {
        if (dict != null && dict.TryGetValue(oldKey, out var val))
        {
            dict.Remove(oldKey);
            dict[newKey] = val;
        }
    }

    public ImageSource Icon
    {
        get => _icon;
        private set
        {
            _icon = value;
            OnPropertyChanged();
        }
    }

    private void UpdateIcon()
    {
        try
        {
            var icon = IconCache.IconForCommand(_name);
            if (icon != null)
            {
                Icon = icon.ToBitmapSource();
                return;
            }
        }
        catch
        {
            // Ignore icon lookup errors
        }

        try
        {
            string expanded = FilenameHelper.FillVariables(_commandLine, true);
            expanded = FilenameHelper.FillCmdVariables(expanded, true);
            if (File.Exists(expanded))
            {
                var icon = PluginUtils.GetCachedExeIcon(expanded, 0);
                if (icon != null)
                {
                    Icon = icon.ToBitmapSource();
                    return;
                }
            }
        }
        catch
        {
            // Ignore
        }

        try
        {
            var icon = WindowsAppHelper.GetAppLogo(_commandLine, _name);
            if (icon != null)
            {
                Icon = icon.ToBitmapSource();
                return;
            }
        }
        catch
        {
            // Ignore
        }

        Icon = null;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
