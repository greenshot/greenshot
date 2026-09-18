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
using Greenshot.Plugin.Office.Destinations;

namespace Greenshot.Plugin.Office
{
    /// <summary>
    /// This is the OfficePlugin base code
    /// </summary>
    public class OfficePlugin : IGreenshotPlugin, IRecipeStepProvider
    {
        private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OfficePlugin));
        private IOfficeConfiguration _config;
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
        }

        /// <summary>
        /// Name of the plugin
        /// </summary>
        public string Name => "Office";

        /// <summary>
        /// Specifies if the plugin can be configured
        /// </summary>
        public bool IsConfigurable => true;

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


        /// <summary>
        /// Implementation of RegisterConfiguration phase: no configuration to register for Office plugin.
        /// </summary>
        public void RegisterConfiguration(IniConfig iniConfig)
        {
            var section = new OfficeConfigurationImpl();
            iniConfig.AddSection(section);
            _config = section;
        }

        /// <summary>
        /// Implementation of RegisterServices phase: register DI services after config is loaded.
        /// </summary>
        public void RegisterServices(IServiceLocator serviceLocator)
        {
            serviceLocator.AddService(Destinations());
            serviceLocator.AddService<IRecipeStepProvider>(this);
            StepRegistry.Instance.RegisterProvider(this);
        }

        /// <summary>
        /// Registers recipe step factories provided by the Office plugin.
        /// </summary>
        /// <param name="registry">The step registry.</param>
        public void RegisterSteps(IStepRegistry registry)
        {
            if (registry == null) return;
            registry.RegisterStepFactory("Office", config => new OfficeStep(config));
            registry.RegisterStepFactory("Excel", config => new OfficeStep(config));
            registry.RegisterStepFactory("PowerPoint", config => new OfficeStep(config));
            registry.RegisterStepFactory("Powerpoint", config => new OfficeStep(config));
            registry.RegisterStepFactory("Word", config => new OfficeStep(config));
            registry.RegisterStepFactory("OneNote", config => new OfficeStep(config));
            registry.RegisterStepFactory("Outlook", config => new OfficeStep(config));
        }

        /// <summary>
        /// Implementation of the IGreenshotPlugin.Start
        /// </summary>
        /// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
        public bool Start()
        {
            Image icon = null;
            try
            {
                icon = new WordDestination().DisplayIcon;
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

        public void Shutdown()
        {
            LOG.Debug("Office Plugin shutdown.");
            Language.LanguageChanged -= OnLanguageChanged;
            if (_config is INotifyPropertyChanged notify)
            {
                notify.PropertyChanged -= OnConfigPropertyChanged;
            }
        }

        /// <summary>
        /// Implementation of the IPlugin.Configure
        /// </summary>
        public void Configure()
        {
            var mainForm = SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>(isOptional: true);
            mainForm?.ShowSetting(Name);
        }

        public System.Windows.UIElement CreateConfigurationControl() => new Forms.OfficeConfigurationControl();
    }
}