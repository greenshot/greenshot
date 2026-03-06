/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Plugin.ExternalCommand;

/// <summary>
/// An Plugin to run commands after an image was written
/// </summary>
public class ExternalCommandPlugin : IGreenshotPlugin
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ExternalCommandPlugin));
    private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
    private static readonly ExternalCommandConfiguration ExternalCommandConfig = IniConfig.GetIniSection<ExternalCommandConfiguration>();
    private ToolStripMenuItem _itemPlugInRoot;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing) return;
        if (_itemPlugInRoot == null) return;
        _itemPlugInRoot.Dispose();
        _itemPlugInRoot = null;
    }

    /// <summary>
    /// Name of the plugin
    /// </summary>
    public string Name => "ExternalCommand";

    /// <summary>
    /// Specifies if the plugin can be configured
    /// </summary>
    public bool IsConfigurable => true;

    private IEnumerable<IDestination> Destinations()
    {
        foreach (string command in ExternalCommandConfig.Commands)
        {
            yield return new ExternalCommandDestination(command);
        }
    }


    /// <summary>
    /// Check and eventually fix the command settings
    /// </summary>
    /// <param name="command"></param>
    /// <returns>false if the command is not correctly configured</returns>
    private bool IsCommandValid(string command)
    {
        if (!ExternalCommandConfig.RunInbackground.ContainsKey(command))
        {
            Log.WarnFormat("Found missing runInbackground for {0}", command);
            // Fix it
            ExternalCommandConfig.RunInbackground.Add(command, true);
        }

        if (!ExternalCommandConfig.Argument.ContainsKey(command))
        {
            Log.WarnFormat("Found missing argument for {0}", command);
            // Fix it
            ExternalCommandConfig.Argument.Add(command, "{0}");
        }

        if (!ExternalCommandConfig.OutputFormat.ContainsKey(command))
        {
            ExternalCommandConfig.OutputFormat.Add(command, OutputFormat.png);
        }

        if (!ExternalCommandConfig.Commandline.ContainsKey(command))
        {
            Log.WarnFormat("Found missing commandline for {0}", command);
            return false;
        }

        string commandline = FilenameHelper.FillVariables(ExternalCommandConfig.Commandline[command], true);
        commandline = FilenameHelper.FillCmdVariables(commandline, true);

        if (!File.Exists(commandline))
        {
            Log.WarnFormat("Found 'invalid' commandline {0} for command {1}", ExternalCommandConfig.Commandline[command], command);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Implementation of the IGreenshotPlugin.Initialize
    /// </summary>
    public virtual bool Initialize()
    {
        Log.DebugFormat("Initialize called");

        var commandsToDelete = new List<string>();
        // Check configuration
        foreach (string command in ExternalCommandConfig.Commands)
        {
            if (!IsCommandValid(command))
            {
                commandsToDelete.Add(command);
            }
        }

        // cleanup
        foreach (string command in commandsToDelete)
        {
            ExternalCommandConfig.Delete(command);
        }

        SimpleServiceProvider.Current.AddService(Destinations());

        _itemPlugInRoot = new ToolStripMenuItem();
        _itemPlugInRoot.Click += ConfigMenuClick;
        OnIconSizeChanged(this, new PropertyChangedEventArgs("IconSize"));
        OnLanguageChanged(this, null);

        PluginUtils.AddToContextMenu(_itemPlugInRoot);
        Language.LanguageChanged += OnLanguageChanged;
        CoreConfig.PropertyChanged += OnIconSizeChanged;
        return true;
    }

    /// <summary>
    /// Fix icon reference
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnIconSizeChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "IconSize")
        {
            try
            {
                string exePath = PluginUtils.GetExePath("cmd.exe");
                if (exePath != null && File.Exists(exePath))
                {
                    var icon = PluginUtils.GetCachedExeIcon(exePath, 0);
                    // Clone the icon to prevent issues when the cache is cleared
                    var iconClone = icon != null ? ImageHelper.Clone(icon) : null;
                    // Dispose the previous image before assigning the new one
                    var oldImage = _itemPlugInRoot.Image;
                    _itemPlugInRoot.Image = iconClone;
                    oldImage?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't get the cmd.exe image", ex);
            }
        }
    }

    private void OnLanguageChanged(object sender, EventArgs e)
    {
        if (_itemPlugInRoot != null)
        {
            _itemPlugInRoot.Text = Language.GetString("externalcommand", "contextmenu_configure");
        }
    }

    public virtual void Shutdown()
    {
        Log.Debug("Shutdown");
        Language.LanguageChanged -= OnLanguageChanged;
        CoreConfig.PropertyChanged -= OnIconSizeChanged;
    }

    private void ConfigMenuClick(object sender, EventArgs eventArgs)
    {
        Configure();
    }

    /// <summary>
    /// Implementation of the IPlugin.Configure
    /// </summary>
    public virtual void Configure()
    {
        Log.Debug("Configure called");
        new SettingsForm().ShowDialog();
    }
}