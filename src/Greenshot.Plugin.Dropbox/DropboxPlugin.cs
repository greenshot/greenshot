/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
 *
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

namespace Greenshot.Plugin.Dropbox
{
    /// <summary>
    /// This is the Dropbox base code
    /// </summary>
    [Plugin("Dropbox", true)]
    public class DropboxPlugin : IGreenshotPlugin
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(DropboxPlugin));
        private static DropboxPluginConfiguration _config;
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
            if (_itemPlugInConfig == null) return;
            _itemPlugInConfig.Dispose();
            _itemPlugInConfig = null;
        }

        /// <summary>
        /// Implementation of the IGreenshotPlugin.Initialize
        /// </summary>
        public bool Initialize()
        {
            // Register configuration (don't need the configuration itself)
            _config = IniConfig.GetIniSection<DropboxPluginConfiguration>();
            _resources = new ComponentResourceManager(typeof(DropboxPlugin));
            SimpleServiceProvider.Current.AddService<IDestination>(new DropboxDestination(this));
            _itemPlugInConfig = new ToolStripMenuItem
            {
                Text = Language.GetString("dropbox", LangKey.Configure),
                Image = (Image) _resources.GetObject("Dropbox")
            };
            _itemPlugInConfig.Click += ConfigMenuClick;

            PluginUtils.AddToContextMenu(_itemPlugInConfig);
            Language.LanguageChanged += OnLanguageChanged;
            return true;
        }

        public void OnLanguageChanged(object sender, EventArgs e)
        {
            if (_itemPlugInConfig != null)
            {
                _itemPlugInConfig.Text = Language.GetString("dropbox", LangKey.Configure);
            }
        }

        public void Shutdown()
        {
            Log.Debug("Dropbox Plugin shutdown.");
        }

        /// <summary>
        /// Implementation of the IPlugin.Configure
        /// </summary>
        public void Configure()
        {
            _config.ShowConfigDialog();
        }

        public void ConfigMenuClick(object sender, EventArgs eventArgs)
        {
            _config.ShowConfigDialog();
        }

        /// <summary>
        /// This will be called when the menu item in the Editor is clicked
        /// </summary>
        public bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, out string uploadUrl)
        {
            uploadUrl = null;
            SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, false);
            try
            {
                bool result = false;
                new PleaseWaitForm().ShowAndWait("Dropbox", Language.GetString("dropbox", LangKey.communication_wait),
                    delegate { result = DropboxUtils.UploadToDropbox(surfaceToUpload, outputSettings, captureDetails); }
                );
                return result;
            }
            catch (Exception e)
            {
                Log.Error(e);
                MessageBox.Show(Language.GetString("dropbox", LangKey.upload_failure) + " " + e.Message);
                return false;
            }
        }
    }
}