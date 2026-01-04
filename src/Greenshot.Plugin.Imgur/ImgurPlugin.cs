/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (c) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Windows.Forms;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.Imgur.Forms;

namespace Greenshot.Plugin.Imgur
{
    /// <summary>
    /// This is the ImgurPlugin code
    /// </summary>
    public class ImgurPlugin : IGreenshotPlugin
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ImgurPlugin));
        private static ImgurConfiguration _config;
        private ComponentResourceManager _resources;
        private ToolStripMenuItem _historyMenuItem;
        private ToolStripMenuItem _itemPlugInConfig;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (_historyMenuItem != null)
            {
                _historyMenuItem.Dispose();
                _historyMenuItem = null;
            }

            if (_itemPlugInConfig != null)
            {
                _itemPlugInConfig.Dispose();
                _itemPlugInConfig = null;
            }
        }

        /// <summary>
        /// Name of the plugin
        /// </summary>
        public string Name => "Imgur";

        /// <summary>
        /// Specifies if the plugin can be configured
        /// </summary>
        public bool IsConfigurable => true;

        private IEnumerable<IDestination> Destinations()
        {
            yield return new ImgurDestination(this);
        }

        /// <summary>
        /// Implementation of the IGreenshotPlugin.Initialize
        /// </summary>
        /// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
        public bool Initialize()
        {
            // Get configuration
            _config = IniConfig.GetIniSection<ImgurConfiguration>();
            _resources = new ComponentResourceManager(typeof(ImgurPlugin));

            ToolStripMenuItem itemPlugInRoot = new ToolStripMenuItem("Imgur")
            {
                Image = (Image) _resources.GetObject("Imgur")
            };

            // Provide the IDestination
            SimpleServiceProvider.Current.AddService(Destinations());
            _historyMenuItem = new ToolStripMenuItem(Language.GetString("imgur", LangKey.history));
            _historyMenuItem.Click += delegate { ImgurHistory.ShowHistory(); };
            itemPlugInRoot.DropDownItems.Add(_historyMenuItem);

            _itemPlugInConfig = new ToolStripMenuItem(Language.GetString("imgur", LangKey.configure));
            _itemPlugInConfig.Click += delegate { _config.ShowConfigDialog(); };
            itemPlugInRoot.DropDownItems.Add(_itemPlugInConfig);

            PluginUtils.AddToContextMenu(itemPlugInRoot);
            Language.LanguageChanged += OnLanguageChanged;

            // Enable history if there are items available
            UpdateHistoryMenuItem();
            return true;
        }

        public void OnLanguageChanged(object sender, EventArgs e)
        {
            if (_itemPlugInConfig != null)
            {
                _itemPlugInConfig.Text = Language.GetString("imgur", LangKey.configure);
            }

            if (_historyMenuItem != null)
            {
                _historyMenuItem.Text = Language.GetString("imgur", LangKey.history);
            }
        }

        private void UpdateHistoryMenuItem()
        {
            if (_historyMenuItem == null)
            {
                return;
            }

            try
            {
                var form = SimpleServiceProvider.Current.GetInstance<Form>();
                form.BeginInvoke((MethodInvoker) delegate
                {
                    var historyMenuItem = _historyMenuItem;
                    if (historyMenuItem == null)
                    {
                        return;
                    }

                    if (_config?.ImgurUploadHistory != null && _config.ImgurUploadHistory.Count > 0)
                    {
                        historyMenuItem.Enabled = true;
                    }
                    else
                    {
                        historyMenuItem.Enabled = false;
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error("Error loading history", ex);
            }
        }

        public virtual void Shutdown()
        {
            Log.Debug("Imgur Plugin shutdown.");
            Language.LanguageChanged -= OnLanguageChanged;
        }

        /// <summary>
        /// Implementation of the IPlugin.Configure
        /// </summary>
        public virtual void Configure()
        {
            _config.ShowConfigDialog();
        }

        /// <summary>
        /// Upload the capture to imgur
        /// </summary>
        /// <param name="captureDetails">ICaptureDetails</param>
        /// <param name="surfaceToUpload">ISurface</param>
        /// <param name="uploadUrl">out string for the url</param>
        /// <returns>true if the upload succeeded</returns>
        public bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, out string uploadUrl)
        {
            SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, _config.UploadReduceColors);
            try
            {
                string filename = Path.GetFileName(FilenameHelper.GetFilenameFromPattern(_config.FilenamePattern, _config.UploadFormat, captureDetails));
                ImgurInfo imgurInfo = null;

                // Run upload in the background
                new PleaseWaitForm().ShowAndWait("Imgur plug-in", Language.GetString("imgur", LangKey.communication_wait),
                    delegate
                    {
                        imgurInfo = ImgurUtils.UploadToImgur(surfaceToUpload, outputSettings, captureDetails.Title, filename);
                        if (imgurInfo != null && _config.AnonymousAccess)
                        {
                            Log.InfoFormat("Storing imgur upload for hash {0} and delete hash {1}", imgurInfo.Hash, imgurInfo.DeleteHash);
                            _config.ImgurUploadHistory.Add(imgurInfo.Hash, imgurInfo.DeleteHash);
                            _config.runtimeImgurHistory.Add(imgurInfo.Hash, imgurInfo);
                            UpdateHistoryMenuItem();
                        }
                    }
                );

                if (imgurInfo != null)
                {
                    // TODO: Optimize a second call for export
                    using (Image tmpImage = surfaceToUpload.GetImageForExport())
                    {
                        imgurInfo.Image = ImageHelper.CreateThumbnail(tmpImage, 90, 90);
                    }

                    IniConfig.Save();

                    if (_config.UsePageLink)
                    {
                        uploadUrl = imgurInfo.Page;
                    }
                    else
                    {
                        uploadUrl = imgurInfo.Original;
                    }

                    if (!string.IsNullOrEmpty(uploadUrl) && _config.CopyLinkToClipboard)
                    {
                        try
                        {
                            ClipboardHelper.SetClipboardData(uploadUrl);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Can't write to clipboard: ", ex);
                            uploadUrl = null;
                        }
                    }

                    return true;
                }
            }
            catch (Exception e)
            {
                Log.Error("Error uploading.", e);
                MessageBox.Show(Language.GetString("imgur", LangKey.upload_failure) + " " + e.Message);
            }

            uploadUrl = null;
            return false;
        }
    }
}