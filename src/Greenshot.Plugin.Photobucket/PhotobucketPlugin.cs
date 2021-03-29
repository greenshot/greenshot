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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Plugin.Photobucket
{
    /// <summary>
    /// This is the GreenshotPhotobucketPlugin base code
    /// </summary>
    [Plugin("Photobucket", true)]
    public class PhotobucketPlugin : IGreenshotPlugin
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(PhotobucketPlugin));
        private static PhotobucketConfiguration _config;
        private ComponentResourceManager _resources;
        private ToolStripMenuItem _itemPlugInConfig;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (_itemPlugInConfig == null) return;
            _itemPlugInConfig.Dispose();
            _itemPlugInConfig = null;
        }

        /// <summary>
        /// Implementation of the IGreenshotPlugin.Initialize
        /// </summary>
        /// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
        public bool Initialize()
        {
            SimpleServiceProvider.Current.AddService<IDestination>(new PhotobucketDestination(this));

            // Get configuration
            _config = IniConfig.GetIniSection<PhotobucketConfiguration>();
            _resources = new ComponentResourceManager(typeof(PhotobucketPlugin));

            _itemPlugInConfig = new ToolStripMenuItem(Language.GetString("photobucket", LangKey.configure))
            {
                Image = (Image) _resources.GetObject("Photobucket")
            };
            _itemPlugInConfig.Click += delegate { _config.ShowConfigDialog(); };

            PluginUtils.AddToContextMenu(_itemPlugInConfig);
            Language.LanguageChanged += OnLanguageChanged;
            return true;
        }

        public void OnLanguageChanged(object sender, EventArgs e)
        {
            if (_itemPlugInConfig != null)
            {
                _itemPlugInConfig.Text = Language.GetString("photobucket", LangKey.configure);
            }
        }

        public void Shutdown()
        {
            Log.Debug("Photobucket Plugin shutdown.");
            Language.LanguageChanged -= OnLanguageChanged;
        }

        /// <summary>
        /// Implementation of the IPlugin.Configure
        /// </summary>
        public void Configure()
        {
            _config.ShowConfigDialog();
        }

        /// <summary>
        /// Upload the capture to Photobucket
        /// </summary>
        /// <param name="captureDetails"></param>
        /// <param name="surfaceToUpload">ISurface</param>
        /// <param name="albumPath">Path to the album</param>
        /// <param name="uploadUrl">out string for the url</param>
        /// <returns>true if the upload succeeded</returns>
        public bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, string albumPath, out string uploadUrl)
        {
            SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, _config.UploadReduceColors);
            try
            {
                string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
                PhotobucketInfo photobucketInfo = null;

                // Run upload in the background
                new PleaseWaitForm().ShowAndWait("Photobucket", Language.GetString("photobucket", LangKey.communication_wait),
                    delegate { photobucketInfo = PhotobucketUtils.UploadToPhotobucket(surfaceToUpload, outputSettings, albumPath, captureDetails.Title, filename); }
                );
                // This causes an exeption if the upload failed :)
                Log.DebugFormat("Uploaded to Photobucket page: " + photobucketInfo.Page);
                uploadUrl = null;
                try
                {
                    if (_config.UsePageLink)
                    {
                        uploadUrl = photobucketInfo.Page;
                        Clipboard.SetText(photobucketInfo.Page);
                    }
                    else
                    {
                        uploadUrl = photobucketInfo.Original;
                        Clipboard.SetText(photobucketInfo.Original);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Can't write to clipboard: ", ex);
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Error(e);
                MessageBox.Show(Language.GetString("photobucket", LangKey.upload_failure) + " " + e.Message);
            }

            uploadUrl = null;
            return false;
        }
    }
}