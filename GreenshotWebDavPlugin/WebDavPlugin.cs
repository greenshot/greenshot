/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using DecaTec.WebDav;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace GreenshotWebDavPlugin
{
    /// <summary>
    /// This is the WebDAV Plugin base code
    /// </summary>
    public class WebDavPlugin : IGreenshotPlugin
    {
        public static PluginAttribute Attributes;
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(WebDavPlugin));
        private static WebDavConfiguration _config;
        private IGreenshotHost _host;
        private ComponentResourceManager _resources;
        private ToolStripMenuItem _itemPlugInConfig;

        /// <summary>
        /// Call the protected Dispose method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose context menu item
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            if (_itemPlugInConfig == null)
            {
                return;
            }
            _itemPlugInConfig.Dispose();
            _itemPlugInConfig = null;
        }

        /// <summary>
        /// Returns an enumarable containing the plugins destionations
        /// </summary>
        /// <returns>The plugins destination</returns>
        public IEnumerable<IDestination> Destinations()
        {
            yield return new WebDavDestination(this);
        }

        /// <summary>
        /// Returns an enumarable containing the plugins processors
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IProcessor> Processors()
        {
            yield break;
        }

        /// <summary>
		/// Greenshot plugin initialize handler
		/// </summary>
		/// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="pluginAttribute">My own attributes</param>
        /// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
        public bool Initialize(IGreenshotHost pluginHost, PluginAttribute pluginAttribute)
        {
            // Assign parameters to local variables
            _host = pluginHost;
            Attributes = pluginAttribute;

            // Register configuration (don't need the configuration itself)
            _config = IniConfig.GetIniSection<WebDavConfiguration>();
            _resources = new ComponentResourceManager(typeof(WebDavPlugin));

            // Create configuration context menu entry
            _itemPlugInConfig = new ToolStripMenuItem
            {
                Text = Language.GetString("webdav", LanguageKeys.configure),
                Tag = _host,
                Image = (Image)_resources.GetObject("WebDAV")
            };
            _itemPlugInConfig.Click += ConfigMenuClick;

            // Register context menu / event
            PluginUtils.AddToContextMenu(_host, _itemPlugInConfig);
            Language.LanguageChanged += OnLanguageChanged;

            return true;
        }

        /// <summary>
        /// Greenshot language change handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnLanguageChanged(object sender, EventArgs e)
        {
            if (_itemPlugInConfig != null)
            {
                _itemPlugInConfig.Text = Language.GetString("webdav", LanguageKeys.configure);
            }
        }

        /// <summary>
		/// Greenshot plugin configure handler
		/// </summary>
        public virtual void Configure()
        {
            _config.ShowConfigDialog();
        }

        /// <summary>
        /// Context menu item click handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        public void ConfigMenuClick(object sender, EventArgs eventArgs)
        {
            _config.ShowConfigDialog();
        }

        /// <summary>
        /// Greenshot plugin shutdown handler
        /// </summary>
        public void Shutdown()
        {
            Log.Debug("WebDAV Plugin shutdown.");
        }

        /// <summary>
		/// This will be called when the menu item in the Editor is clicked
		/// </summary>
        public bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload)
        {
            SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, false);
            try
            {
                // Create filename
                string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));

                // Run upload in the background
                new PleaseWaitForm().ShowAndWait("WebDAV plug-in", Language.GetString("webdav", LanguageKeys.communication_wait),
                    async delegate
                    {
                        // The file URL of the file on the WebDAV server.
                        var fileUrl = $"{_config.Url}{filename}";

                        // Specify the user credentials and use it to create a WebDavSession instance.
                        var credentials = new NetworkCredential(_config.Username, _config.Password);

                        // Create WebDAV session and upload the file
                        using (var webDavSession = new WebDavSession(_config.Url, credentials))
                        {
                            var surfaceContainer = new SurfaceContainer(surfaceToUpload, outputSettings, filename);
                            using (var memoryStream = new MemoryStream())
                            {
                                surfaceContainer.WriteToStream(memoryStream);
                                memoryStream.Seek(0, SeekOrigin.Begin);
                                await webDavSession.UploadFileAsync(fileUrl, memoryStream);
                            }
                        }
                    }
                );
                return true;
            }
            catch (Exception e)
            {
                Log.Error("Error uploading.", e);
                MessageBox.Show(Language.GetString("webdav", LanguageKeys.upload_failure) + " " + e.Message);
            }
            return false;
        }
    }
}
