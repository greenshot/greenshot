/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.Pdf.Configuration;

namespace Greenshot.Plugin.Pdf
{
    /// <summary>
    /// This is the PDF plugin for Greenshot
    /// </summary>
    public class PdfPlugin : IGreenshotPlugin
    {
        private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PdfPlugin));
        private PdfExportSettings _settings;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Name of the plugin
        /// </summary>
        public string Name => "PDF";

        /// <summary>
        /// Specifies if the plugin can be configured
        /// </summary>
        public bool IsConfigurable => true;

        private void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Implementation of the IGreenshotPlugin.Initialize
        /// </summary>
        public bool Initialize()
        {
            LOG.Debug("PDF Plugin initializing.");
            
            // Load settings
            _settings = IniConfig.GetIniSection<PdfExportSettings>();
            
            // Register the PDF file format handler
            SimpleServiceProvider.Current.AddService<IFileFormatHandler>(new PdfFileFormatHandler());
            
            return true;
        }

        public void Shutdown()
        {
            LOG.Debug("PDF Plugin shutdown.");
        }

        /// <summary>
        /// Implementation of the IPlugin.Configure
        /// </summary>
        public void Configure()
        {
            if (_settings == null)
            {
                _settings = IniConfig.GetIniSection<PdfExportSettings>();
            }

            var form = new Forms.PdfExportSettingsForm();
            if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                IniConfig.Save();
            }
        }
    }
}
