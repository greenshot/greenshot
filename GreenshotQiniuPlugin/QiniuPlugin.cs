/*
 * A Picasa Plugin for Greenshot
 * Copyright (C) 2011  Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;


using Qiniu.Http;
using Qiniu.Util;

namespace GreenshotQiniuPlugin
{
    public class QiniuPlugin : IGreenshotPlugin
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(QiniuPlugin));
        private static QiniuConfiguration _config;
        public static PluginAttribute Attributes;
        private IGreenshotHost _host;
        private ComponentResourceManager _resources;
        private ToolStripMenuItem _itemPlugInConfig;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_itemPlugInConfig != null)
                {
                    _itemPlugInConfig.Dispose();
                    _itemPlugInConfig = null;
                }
            }
        }

        public IEnumerable<IDestination> Destinations()
        {
            yield return new QiniuDestination(this);
        }

        public IEnumerable<IProcessor> Processors()
        {
            yield break;
        }

        /// <summary>
        /// Implementation of the IGreenshotPlugin.Initialize
        /// </summary>
        /// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
        /// <param name="myAttributes">My own attributes</param>
        /// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
        public virtual bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes)
        {
            _host = pluginHost;
            Attributes = myAttributes;

            // Get configuration
            _config = IniConfig.GetIniSection<QiniuConfiguration>();
            _resources = new ComponentResourceManager(typeof(QiniuPlugin));
            string text = Language.GetString("qiniu", LangKey.configure);
            _itemPlugInConfig = new ToolStripMenuItem(text)
            {
                Tag = _host,
                Image = (Image)_resources.GetObject("Qiniu")
            };
            _itemPlugInConfig.Click += delegate {
                _config.ShowConfigDialog();
            };

            PluginUtils.AddToContextMenu(_host, _itemPlugInConfig);
            Language.LanguageChanged += OnLanguageChanged;
            return true;
        }

        public void OnLanguageChanged(object sender, EventArgs e)
        {
            if (_itemPlugInConfig != null)
            {
                _itemPlugInConfig.Text = Language.GetString("Qiniu", LangKey.configure);
            }
        }

        public virtual void Shutdown()
        {
            Log.Debug("Qiniu Plugin shutdown.");
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
        /// Upload the capture to qiniu cloud
        /// </summary>
        /// <param name="captureDetails"></param>
        /// <param name="surfaceToUpload">ISurface</param>
        /// <param name="albumPath">Path to the album</param>
        /// <param name="uploadUrl">out string for the url</param>
        /// <returns>true if the upload succeeded</returns>
        public bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, out string uploadUrl)
        {
            SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, _config.UploadReduceColors);
            try
            {
                string filename = _config.ImageNamePrefix + DateTime.Now.ToString("yyyyMMddHHmmss") + "." + _config.UploadFormat.ToString().ToLower();
                Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
              
                string path = Directory.GetCurrentDirectory();

                string fullPath = Path.Combine(path, filename);

                // public static void Save(ISurface surface, string fullPath, bool allowOverwrite, SurfaceOutputSettings outputSettings, bool copyPathToClipboard)
                // Run upload in the background
                MemoryStream streamoutput = new MemoryStream();
                ImageOutput.SaveToStream(surfaceToUpload, streamoutput, outputSettings);
                new PleaseWaitForm().ShowAndWait(Attributes.Name, Language.GetString("qiniu", LangKey.communication_wait),
                    delegate
                    {
                        HttpResult result = QiniuUtils.UploadFile(streamoutput, filename);
                    }
                );
                // This causes an exeption if the upload failed :)
                //Log.DebugFormat("Uploaded to qiniu page: " + qiniuInfo.Page);

                uploadUrl =  _config.DefaultDomain + filename;

                string markdownURL = "![]("+uploadUrl+")";


                Clipboard.SetText(markdownURL);
               
                 
             
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e);
                MessageBox.Show(Language.GetString("qiniu", LangKey.upload_failure) + " " + e.Message);
            }
            uploadUrl = null;
            return false;
        }
    }
}
