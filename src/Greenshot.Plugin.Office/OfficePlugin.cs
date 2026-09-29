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
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Plugin.Office.Destinations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Threading;

namespace Greenshot.Plugin.Office
{
    /// <summary>
    /// This is the OfficePlugin base code
    /// </summary>
    public class OfficePlugin : IGreenshotPlugin, IConfigurablePlugin, IRecipeStepProvider
    {
        private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OfficePlugin));
        private IOfficeConfiguration _config;
        private ToolStripMenuItem _itemPlugInConfig;

        public ValueTask DisposeAsync()
        {
            // The menu item is removed and disposed in StopAsync
            return default;
        }

        /// <summary>
        /// Name of the plugin
        /// </summary>
        public string Name => "Office";

        private IEnumerable<IDestination> Destinations()
        {
            IDestination destination;
            try
            {
                destination = new ExcelDestination();
            }
            catch
            {
                destination = null;
            }

            if (destination != null)
            {
                yield return destination;
            }

            try
            {
                destination = new PowerpointDestination();
            }
            catch
            {
                destination = null;
            }

            if (destination != null)
            {
                yield return destination;
            }

            try
            {
                destination = new WordDestination();
            }
            catch
            {
                destination = null;
            }

            if (destination != null)
            {
                yield return destination;
            }

            try
            {
                destination = new OutlookDestination();
            }
            catch
            {
                destination = null;
            }

            if (destination != null)
            {
                yield return destination;
            }

            try
            {
                destination = new OneNoteDestination();
            }
            catch
            {
                destination = null;
            }

            if (destination != null)
            {
                yield return destination;
            }
        }


        public void ConfigureServices(IPluginServices services)
        {
            var section = new OfficeConfigurationImpl();
            services.AddConfiguration(section);
            _config = section;

            // The destinations look for the Office installation and read the configuration
            services.AddServices(() => Destinations().ToList());
            services.AddRecipeStepProvider(this);
            services.AddSettingsView<IOfficeConfiguration>(_ => new Forms.OfficeConfigurationControl());
        }

        public object CreateSettingsViewModel(IServiceProvider services) => _config;

        /// <summary>
        /// Registers recipe step factories provided by the Office plugin.
        /// </summary>
        /// <param name="registry">The step registry.</param>
        public void RegisterSteps(IStepRegistry registry)
        {
            if (registry == null) return;
            registry.Register<OfficeStep>(config => new OfficeStep(config));
        }

        /// <summary>
        /// Add the quick link to the context menu (on the UI thread)
        /// </summary>
        public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken) =>
            services.GetRequiredService<IUiDispatcher>().RunOnUiAsync(Start, cancellationToken);

        private void Start()
        {
            Image icon = null;
            try
            {
                icon = WordDestination.WordExePath == null ? null : PluginUtils.GetCachedExeIcon(WordDestination.WordExePath, 0);
            }
            catch
            {
                // Word may not be available
            }

            _itemPlugInConfig = new ToolStripMenuItem
            {
                Image = icon,
                Text = PluginUtils.GetQuicklinkText("Microsoft Office"),
                Visible = _config?.QuicklinkEnabled ?? false
            };
            _itemPlugInConfig.Click += delegate { ShowSettings(); };

            PluginUtils.AddToContextMenu(_itemPlugInConfig);
            Language.LanguageChanged += OnLanguageChanged;
            if (_config is INotifyPropertyChanged notify)
            {
                notify.PropertyChanged += OnConfigPropertyChanged;
            }
        }

        private void OnConfigPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IOfficeConfiguration.QuicklinkEnabled))
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
                _itemPlugInConfig.Text = PluginUtils.GetQuicklinkText("Microsoft Office");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) =>
            UiDispatcher.Current.RunOnUiAsync(() =>
            {
                LOG.Debug("Office Plugin shutdown.");
                Language.LanguageChanged -= OnLanguageChanged;
                if (_config is INotifyPropertyChanged notify)
                {
                    notify.PropertyChanged -= OnConfigPropertyChanged;
                }

                _itemPlugInConfig?.Dispose();
                _itemPlugInConfig = null;
            }, cancellationToken);

        /// <summary>
        /// Show the settings of this plugin
        /// </summary>
        private void ShowSettings()
        {
            SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>(isOptional: true)?.ShowSetting(Name);
        }
    }
}