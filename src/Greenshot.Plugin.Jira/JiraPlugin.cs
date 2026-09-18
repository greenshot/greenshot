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
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.WinForms.ContentConverter;
using Dapplo.Jira.SvgWinForms.Converters;
using Dapplo.Log;
using Greenshot.Base.Core;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Plugin.Jira.Forms;
using log4net;

namespace Greenshot.Plugin.Jira;

/// <summary>
/// This is the JiraPlugin base code
/// </summary>
public class JiraPlugin : IGreenshotPlugin, IRecipeStepProvider
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(JiraPlugin));
    private IJiraConfiguration _config;
    private ComponentResourceManager _resources;
    private ToolStripMenuItem _itemPlugInConfig;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing) return;
        if (_itemPlugInConfig != null)
        {
            _itemPlugInConfig.Dispose();
            _itemPlugInConfig = null;
        }
        var jiraConnector = SimpleServiceProvider.Current.GetInstance<JiraConnector>();
        jiraConnector?.Dispose();
    }

    /// <summary>
    /// Name of the plugin
    /// </summary>
    public string Name => "Jira";

    /// <summary>
    /// Specifies if the plugin can be configured
    /// </summary>
    public bool IsConfigurable => true;

    /// <summary>
    /// Implementation of RegisterConfiguration phase: register INI section before file is loaded.
    /// </summary>
    public void RegisterConfiguration(IniConfig iniConfig)
    {
        var section = new JiraConfigurationImpl();
        iniConfig.AddSection(section);
        _config = section;
    }

    /// <summary>
    /// Implementation of RegisterServices phase: register DI services after config is loaded.
    /// </summary>
    public void RegisterServices(IServiceLocator serviceLocator)
    {
        _resources = new ComponentResourceManager(typeof(JiraPlugin));
        serviceLocator.AddService(new JiraConnector());
        serviceLocator.AddService<IDestination>(new JiraDestination());
        serviceLocator.AddService<IRecipeStepProvider>(this);
        StepRegistry.Instance.RegisterProvider(this);
    }

    /// <summary>
    /// Registers recipe step factories provided by the Jira plugin.
    /// </summary>
    /// <param name="registry">The step registry.</param>
    public void RegisterSteps(IStepRegistry registry)
    {
        if (registry == null) return;
        registry.RegisterStepFactory("Jira", config => new JiraStep(config));
        registry.RegisterStepFactory("JiraUpload", config => new JiraStep(config));
        registry.RegisterStepFactory("UploadToJira", config => new JiraStep(config));
        registry.RegisterStepFactory("AttachToJira", config => new JiraStep(config));
    }

    /// <summary>
    /// Implementation of the IGreenshotPlugin.Start
    /// </summary>
    /// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
    public bool Start()
    {
        if (HttpExtensionsGlobals.HttpContentConverters.All(x => x.GetType() != typeof(SvgBitmapHttpContentConverter)))
        {
            HttpExtensionsGlobals.HttpContentConverters.Add(SvgBitmapHttpContentConverter.Instance.Value);
        }
        BitmapHttpContentConverter.RegisterGlobally();

        if (Log.IsDebugEnabled)
        {
            LogSettings.RegisterDefaultLogger<Log4NetLogger>(LogLevels.Verbose);
        }
        else if (Log.IsInfoEnabled)
        {
            LogSettings.RegisterDefaultLogger<Log4NetLogger>(LogLevels.Info);
        }
        else if (Log.IsWarnEnabled)
        {
            LogSettings.RegisterDefaultLogger<Log4NetLogger>(LogLevels.Warn);
        }
        else if (Log.IsErrorEnabled)
        {
            LogSettings.RegisterDefaultLogger<Log4NetLogger>(LogLevels.Error);
        }
        else
        {
            LogSettings.RegisterDefaultLogger<Log4NetLogger>(LogLevels.Fatal);
        }

        _itemPlugInConfig = new ToolStripMenuItem
        {
            Image = (Image) _resources?.GetObject("Jira"),
            Text = PluginUtils.GetQuicklinkText("Jira"),
            Visible = _config?.QuicklinkEnabled ?? false
        };
        _itemPlugInConfig.Click += delegate { Configure(); };

        PluginUtils.AddToContextMenu(_itemPlugInConfig);
        Language.LanguageChanged += OnLanguageChanged;
        if (_config is INotifyPropertyChanged notify)
        {
            notify.PropertyChanged += OnConfigPropertyChanged;
        }

        return true;
    }

    private void OnConfigPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IJiraConfiguration.QuicklinkEnabled))
        {
            if (_itemPlugInConfig != null)
            {
                _itemPlugInConfig.Visible = _config?.QuicklinkEnabled ?? false;
            }
        }
    }

    public void OnLanguageChanged(object sender, EventArgs e)
    {
        if (_itemPlugInConfig != null)
        {
            _itemPlugInConfig.Text = PluginUtils.GetQuicklinkText("Jira");
        }
    }

    public void Shutdown()
    {
        Log.Debug("Jira Plugin shutdown.");
        Language.LanguageChanged -= OnLanguageChanged;
        if (_config is INotifyPropertyChanged notify)
        {
            notify.PropertyChanged -= OnConfigPropertyChanged;
        }
        var jiraConnector = SimpleServiceProvider.Current.GetInstance<JiraConnector>();
        jiraConnector?.Logout();
    }

    /// <summary>
    /// Implementation of the IPlugin.Configure
    /// </summary>
    public void Configure()
    {
        var mainForm = SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>(isOptional: true);
        mainForm?.ShowSetting(Name);
    }

    public System.Windows.UIElement CreateConfigurationControl()
    {
        return new Forms.JiraConfigurationControl(_config);
    }
}