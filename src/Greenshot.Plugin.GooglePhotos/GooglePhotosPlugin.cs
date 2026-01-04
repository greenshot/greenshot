/*
 * A GooglePhotos Plugin for Greenshot
 * Copyright (C) 2011  Francis Noel
 * 
 * For more information see: https://getgreenshot.org/
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

namespace Greenshot.Plugin.GooglePhotos
{
    /// <summary>
    /// This is the GooglePhotos base code
    /// </summary>
    public class GooglePhotosPlugin : IGreenshotPlugin
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(GooglePhotosPlugin));
        private static GooglePhotosConfiguration _config;
        private ComponentResourceManager _resources;
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
        public string Name => "GooglePhotos";

        /// <summary>
        /// Specifies if the plugin can be configured
        /// </summary>
        public bool IsConfigurable => true;

        /// <summary>
        /// Implementation of the IGreenshotPlugin.Initialize
        /// </summary>
        public bool Initialize()
        {
            SimpleServiceProvider.Current.AddService<IDestination>(new GooglePhotosDestination(this));

            // Get configuration
            _config = IniConfig.GetIniSection<GooglePhotosConfiguration>();
            _resources = new ComponentResourceManager(typeof(GooglePhotosPlugin));

            _itemPlugInRoot = new ToolStripMenuItem
            {
                Text = Language.GetString("googlephotos", LangKey.Configure),
                Image = (Image) _resources.GetObject("GooglePhotos")
            };
            _itemPlugInRoot.Click += ConfigMenuClick;
            PluginUtils.AddToContextMenu(_itemPlugInRoot);
            Language.LanguageChanged += OnLanguageChanged;
            return true;
        }

        public void OnLanguageChanged(object sender, EventArgs e)
        {
            if (_itemPlugInRoot != null)
            {
                _itemPlugInRoot.Text = Language.GetString("googlephotos", LangKey.Configure);
            }
        }

        public void Shutdown()
        {
            Log.Debug("GooglePhotos Plugin shutdown.");
            Language.LanguageChanged -= OnLanguageChanged;
            //host.OnImageEditorOpen -= new OnImageEditorOpenHandler(ImageEditorOpened);
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
            Configure();
        }

        public bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, out string uploadUrl)
        {
            SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality);
            try
            {
                string url = null;
                new PleaseWaitForm().ShowAndWait("GooglePhotos", Language.GetString("googlephotos", LangKey.communication_wait),
                    delegate
                    {
                        string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
                        url = GooglePhotosUtils.UploadToGooglePhotos(surfaceToUpload, outputSettings, captureDetails.Title, filename);
                    }
                );
                uploadUrl = url;

                if (uploadUrl != null && _config.AfterUploadLinkToClipBoard)
                {
                    ClipboardHelper.SetClipboardData(uploadUrl);
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Error("Error uploading.", e);
                MessageBox.Show(Language.GetString("googlephotos", LangKey.upload_failure) + " " + e.Message);
            }

            uploadUrl = null;
            return false;
        }
    }
}