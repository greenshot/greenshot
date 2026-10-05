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
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Dapplo.Ini;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Base.Languages;

namespace Greenshot.Plugin.ExternalCommand;

/// <summary>
/// An Plugin to run commands after an image was written
/// </summary>
public class ExternalCommandPlugin : IGreenshotPlugin, IConfigurablePlugin, IRecipeStepProvider
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ExternalCommandPlugin));
    private static ICoreConfiguration CoreConfig;
    private static IExternalCommandConfiguration ExternalCommandConfig;
    private ToolStripMenuItem _itemPlugInRoot;

    public ValueTask DisposeAsync()
    {
        // The menu item is removed and disposed in StopAsync
        return default;
    }

    /// <summary>
    /// Name of the plugin
    /// </summary>
    public string Name => "ExternalCommand";

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
            ExternalCommandConfig.OutputFormat.Add(command, WellKnownFileFormats.Png);
        }

        if (!ExternalCommandConfig.Commandline.ContainsKey(command))
        {
            Log.WarnFormat("Found missing commandline for {0}", command);
            return false;
        }

        string commandline = FilenameHelper.FillVariables(ExternalCommandConfig.Commandline[command], true);
        commandline = FilenameHelper.FillCmdVariables(commandline, true);

        if (!File.Exists(commandline) &&
            PluginUtils.GetExePath(commandline) == null &&
            WindowsAppHelper.FindPackage(commandline, command) == null)
        {
            Log.WarnFormat("Found 'invalid' commandline {0} for command {1}", ExternalCommandConfig.Commandline[command], command);
            return false;
        }

        return true;
    }

    public void ConfigureServices(IPluginServices services)
    {
        Texts.Register<IExternalCommandLanguage>(new ExternalCommandLanguageImpl());
        var externalCommandSection = new ExternalCommandConfigurationImpl();
        services.AddConfiguration(externalCommandSection);
        ExternalCommandConfig = externalCommandSection;

        services.AddService<IIconProvider>(new ExternalCommandIconProvider());
        // The destinations come from the loaded configuration
        services.AddServices(CreateDestinations);
        services.AddRecipeStepProvider(this);
        services.AddSettingsView<IExternalCommandConfiguration>(_ => new Forms.ExternalCommandConfigurationControl());
    }

    public object CreateSettingsViewModel(IServiceProvider services) => ExternalCommandConfig;

    /// <summary>
    /// Remove the invalid commands from the configuration, create the destinations for the others.
    /// </summary>
    private IEnumerable<IDestination> CreateDestinations()
    {
        CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();
        var commandsToDelete = new List<string>();
        foreach (string command in ExternalCommandConfig.Commands)
        {
            if (!IsCommandValid(command))
            {
                commandsToDelete.Add(command);
            }
        }

        foreach (string command in commandsToDelete)
        {
            ExternalCommandConfig.Delete(command);
        }

        return Destinations().ToList();
    }

    /// <summary>
    /// Registers recipe step factories provided by the ExternalCommand plugin.
    /// </summary>
    /// <param name="registry">The step registry.</param>
    public void RegisterSteps(IStepRegistry registry)
    {
        if (registry == null) return;
        registry.Register<ExternalCommandStep>(config => new ExternalCommandStep(config));
    }

    /// <summary>
    /// Add the quick link to the context menu (on the UI thread)
    /// </summary>
    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<IUiDispatcher>().RunOnUiAsync(Start, cancellationToken);

    private void Start()
    {
        _itemPlugInRoot = new ToolStripMenuItem();
        _itemPlugInRoot.Click += ConfigMenuClick;
        OnIconSizeChanged(this, new PropertyChangedEventArgs("IconSize"));
        OnLanguageChanged(this, null);

        PluginUtils.AddToContextMenu(_itemPlugInRoot);
        _itemPlugInRoot.Visible = ExternalCommandConfig?.QuicklinkEnabled ?? false;
        if (ExternalCommandConfig is INotifyPropertyChanged notify)
        {
            notify.PropertyChanged += OnConfigPropertyChanged;
        }
        Texts.Config.LanguageChanged += OnLanguageChanged;
        CoreConfig.PropertyChanged += OnIconSizeChanged;
    }

    private void OnConfigPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IExternalCommandConfiguration.QuicklinkEnabled))
        {
            if (_itemPlugInRoot != null)
            {
                _itemPlugInRoot.Visible = ExternalCommandConfig?.QuicklinkEnabled ?? false;
            }
        }
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
            _itemPlugInRoot.Text = PluginUtils.GetQuicklinkText("External command");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        UiDispatcher.Current.RunOnUiAsync(() =>
        {
            Log.Debug("Shutdown");
            if (ExternalCommandConfig is INotifyPropertyChanged notify)
            {
                notify.PropertyChanged -= OnConfigPropertyChanged;
            }

            Texts.Config.LanguageChanged -= OnLanguageChanged;
            CoreConfig.PropertyChanged -= OnIconSizeChanged;
            _itemPlugInRoot?.Dispose();
            _itemPlugInRoot = null;
        }, cancellationToken);

    private void ConfigMenuClick(object sender, EventArgs eventArgs)
    {
        // Show the settings of this plugin
        SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>(isOptional: true)?.ShowSetting(Name);
    }
}