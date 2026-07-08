/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Plugin.CaptionBar
{
    /// <summary>
    /// Plugin that adds a caption bar to screenshots
    /// </summary>
    public class CaptionBarPlugin : IGreenshotPlugin
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(CaptionBarPlugin));
        private CaptionBarConfiguration _config;
        private CaptionBarProcessor _processor;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (_processor != null)
            {
                _processor.Dispose();
                _processor = null;
            }
        }

        /// <summary>
        /// Name of the plugin
        /// </summary>
        public string Name => "CaptionBar";

        /// <summary>
        /// Specifies if the plugin can be configured
        /// </summary>
        public bool IsConfigurable => true;

        /// <summary>
        /// Implementation of the IGreenshotPlugin.Initialize
        /// </summary>
        public virtual bool Initialize()
        {
            Log.Info("CaptionBar plugin initializing");

            try
            {
                // Load configuration
                _config = IniConfig.GetIniSection<CaptionBarConfiguration>();

                // Create and register processor
                _processor = new CaptionBarProcessor(_config);
                SimpleServiceProvider.Current.AddService<Greenshot.Base.Interfaces.IProcessor>(_processor);

                Log.InfoFormat("CaptionBar plugin initialized (Enabled: {0})", _config.Enabled);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error initializing CaptionBar plugin", ex);
                return false;
            }
        }

        /// <summary>
        /// Implementation of the IGreenshotPlugin.Shutdown
        /// </summary>
        public virtual void Shutdown()
        {
            Log.Info("CaptionBar plugin shutting down");
            Dispose();
        }

        /// <summary>
        /// Implementation of the IPlugin.Configure
        /// Shows the settings dialog for the plugin
        /// </summary>
        public virtual void Configure()
        {
            Log.Debug("Configure called - showing settings dialog");

            try
            {
                using (var settingsForm = new CaptionBarSettingsForm(_config))
                {
                    settingsForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error showing settings dialog", ex);
                MessageBox.Show("Error opening settings: " + ex.Message, "CaptionBar Plugin Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
