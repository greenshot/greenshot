/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.IO;
using System.Windows.Forms;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Plugin.Box
{
    /// <summary>
    /// This is the Box base code
    /// </summary>
    public class BoxPlugin : IGreenshotPlugin
    {
        private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(BoxPlugin));
        private static BoxConfiguration _config;
        private ComponentResourceManager _resources;
        private ToolStripMenuItem _itemPlugInConfig;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Name of the plugin
        /// </summary>
        public string Name => "Box";

        /// <summary>
        /// Specifies if the plugin can be configured
        /// </summary>
        public bool IsConfigurable => true;

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
            _config = IniConfig.GetIniSection<BoxConfiguration>();
            _resources = new ComponentResourceManager(typeof(BoxPlugin));
            SimpleServiceProvider.Current.AddService<IDestination>(new BoxDestination(this));
            _itemPlugInConfig = new ToolStripMenuItem
            {
                Image = (Image) _resources.GetObject("Box"),
                Text = Language.GetString("box", LangKey.Configure)
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
                _itemPlugInConfig.Text = Language.GetString("box", LangKey.Configure);
            }
        }

        public void Shutdown()
        {
            LOG.Debug("Box Plugin shutdown.");
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
        public string Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload)
        {
            SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, false);
            try
            {
                string url = null;
                string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
                SurfaceContainer imageToUpload = new SurfaceContainer(surfaceToUpload, outputSettings, filename);

                new PleaseWaitForm().ShowAndWait("Box", Language.GetString("box", LangKey.communication_wait),
                    delegate { url = BoxUtils.UploadToBox(imageToUpload, captureDetails.Title, filename); }
                );

                if (url != null && _config.AfterUploadLinkToClipBoard)
                {
                    ClipboardHelper.SetClipboardData(url);
                }

                return url;
            }
            catch (Exception ex)
            {
                LOG.Error("Error uploading.", ex);
                MessageBox.Show(Language.GetString("box", LangKey.upload_failure) + " " + ex.Message);
                return null;
            }
        }
    }
}