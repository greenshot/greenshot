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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Wpf;
using Microsoft.Win32;

namespace Greenshot.Plugin.ExternalCommand.Forms;

public partial class ExternalCommandDetailWindow : Window
{
    private static readonly IExternalCommandConfiguration ExternalCommandConfig = IniConfigRegistry.GetSection<IExternalCommandConfiguration>();
    private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

    private readonly string _commando;
    private readonly int _commandIndex;
    private readonly EnumDisplayer _outputFormatDisplayer;

    public ExternalCommandDetailWindow(string commando = null)
    {
        InitializeComponent();

        try
        {
            Icon = GreenshotResources.GetGreenshotIcon()?.ToBitmapSource();
        }
        catch
        {
            // Ignore in headless/test environments
        }

        _commando = commando;
        _outputFormatDisplayer = (EnumDisplayer) Resources["outputFormats"];

        if (commando != null)
        {
            TextBoxName.Text = commando;
            TextBoxCommandLine.Text = ExternalCommandConfig.Commandline.ContainsKey(commando) ? ExternalCommandConfig.Commandline[commando] : string.Empty;
            TextBoxArguments.Text = ExternalCommandConfig.Argument.ContainsKey(commando) ? ExternalCommandConfig.Argument[commando] : string.Empty;

            var format = ExternalCommandConfig.OutputFormat.ContainsKey(commando) ? ExternalCommandConfig.OutputFormat[commando] : OutputFormat.png;
            ComboBoxOutputFormat.SelectedValue = _outputFormatDisplayer.Convert(format, typeof(string), null, null);

            _commandIndex = ExternalCommandConfig.Commands.FindIndex(s => s == commando);
        }
        else
        {
            TextBoxArguments.Text = "\"{0}\"";
            var defaultFormat = CoreConfig.OutputFileFormat;
            ComboBoxOutputFormat.SelectedValue = _outputFormatDisplayer.Convert(defaultFormat, typeof(string), null, null);
        }

        ValidateInputs();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
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
            if (!string.IsNullOrWhiteSpace(TextBoxCommandLine.Text))
            {
                initialPath = Path.GetDirectoryName(TextBoxCommandLine.Text);
            }
        }
        catch
        {
            // Ignore path extraction errors
        }

        if (initialPath != null && Directory.Exists(initialPath))
        {
            openFileDialog.InitialDirectory = initialPath;
        }
        else
        {
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        }

        if (openFileDialog.ShowDialog(this) == true)
        {
            TextBoxCommandLine.Text = openFileDialog.FileName;
            if (string.IsNullOrWhiteSpace(TextBoxName.Text))
            {
                TextBoxName.Text = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
            }
        }
    }

    private void Input_Changed(object sender, TextChangedEventArgs e)
    {
        ValidateInputs();
    }

    private void ValidateInputs()
    {
        if (ButtonOk == null)
        {
            return;
        }

        string name = TextBoxName.Text?.Trim();
        string cmd = TextBoxCommandLine.Text?.Trim();
        string args = TextBoxArguments.Text?.Trim();

        bool isValid = !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(cmd) && !string.IsNullOrEmpty(args);

        if (isValid && ExternalCommandConfig.Commands != null)
        {
            // If new or renamed, name must be unique
            if (_commando == null || !_commando.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                if (ExternalCommandConfig.Commands.Exists(c => c.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    isValid = false;
                }
            }
        }

        ButtonOk.IsEnabled = isValid;
    }

    private void Button_OK_Click(object sender, RoutedEventArgs e)
    {
        string commandName = TextBoxName.Text.Trim();
        string commandLine = TextBoxCommandLine.Text.Trim();
        string arguments = TextBoxArguments.Text.Trim();

        OutputFormat outputFormat = OutputFormat.png;
        if (ComboBoxOutputFormat.SelectedValue is string displayName)
        {
            var converted = _outputFormatDisplayer.ConvertBack(displayName, typeof(OutputFormat), null, null);
            if (converted is OutputFormat format)
            {
                outputFormat = format;
            }
        }

        if (ExternalCommandConfig.Commands == null)
        {
            ExternalCommandConfig.Commands = new System.Collections.Generic.List<string>();
        }
        if (ExternalCommandConfig.Commandline == null)
        {
            ExternalCommandConfig.Commandline = new System.Collections.Generic.Dictionary<string, string>();
        }
        if (ExternalCommandConfig.Argument == null)
        {
            ExternalCommandConfig.Argument = new System.Collections.Generic.Dictionary<string, string>();
        }
        if (ExternalCommandConfig.OutputFormat == null)
        {
            ExternalCommandConfig.OutputFormat = new System.Collections.Generic.Dictionary<string, OutputFormat>();
        }

        if (_commando != null)
        {
            if (_commandIndex >= 0 && _commandIndex < ExternalCommandConfig.Commands.Count)
            {
                ExternalCommandConfig.Commands[_commandIndex] = commandName;
            }
            ExternalCommandConfig.Commandline.Remove(_commando);
            ExternalCommandConfig.Commandline[commandName] = commandLine;
            ExternalCommandConfig.Argument.Remove(_commando);
            ExternalCommandConfig.Argument[commandName] = arguments;
            ExternalCommandConfig.OutputFormat.Remove(_commando);
            ExternalCommandConfig.OutputFormat[commandName] = outputFormat;
        }
        else
        {
            ExternalCommandConfig.Commands.Add(commandName);
            ExternalCommandConfig.Commandline[commandName] = commandLine;
            ExternalCommandConfig.Argument[commandName] = arguments;
            ExternalCommandConfig.OutputFormat[commandName] = outputFormat;
        }

        DialogResult = true;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
