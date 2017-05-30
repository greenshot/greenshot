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
using System.Windows.Forms;
using Greenshot.IniFile;
using GreenshotPlugin.Core;
using System;

namespace GreenshotQiniuPlugin
{
    /// <summary>
    /// Description of QiniuConfiguration.
    /// </summary>
    [IniSection("Qiniu", Description = "Greenshot Qiniu Plugin configuration")]
    class QiniuConfiguration : IniSection
    {
        [IniProperty("UploadFormat", Description = "What file type to use for uploading", DefaultValue = "png")]
        public OutputFormat UploadFormat { get; set; }
        [IniProperty("UploadJpegQuality", Description = "JPEG file save quality in %.", DefaultValue = "80")]
        public int UploadJpegQuality { get; set; }
        [IniProperty("UploadReduceColors", Description = "Reduce color amount of the uploaded image to 256", DefaultValue = "False")]
        public bool UploadReduceColors { get; set; }
        [IniProperty("UsePageLink", Description = "Use pagelink instead of direct link on the clipboard", DefaultValue = "False")]
        public bool UsePageLink { get; set; }


        [IniProperty("ImgQiniuUrl", Description = "Url to Qiniu cloud.", DefaultValue = "")]
        public string ImgQiniuUrl { get; set; }

        [IniProperty("AccessKey", Description = "access key used for uploading", DefaultValue = "")]
        public string AccessKey { get; set; }

        [IniProperty("SecretKey", Description = "secret key used for uploading", DefaultValue = "")]
        public string SecretKey { get; set; }

        [IniProperty("Scope", Description = "scope key used for uploading", DefaultValue = "")]
        public string Scope { get; set; }
      
   
        /// <summary>
        /// A form for username/password
        /// </summary>
        /// <returns>bool true if OK was pressed, false if cancel</returns>
        public bool ShowConfigDialog()
        {
            SettingsForm settingsForm = new SettingsForm();
            DialogResult result = settingsForm.ShowDialog();
            return result == DialogResult.OK;
        }
        
    }
}
