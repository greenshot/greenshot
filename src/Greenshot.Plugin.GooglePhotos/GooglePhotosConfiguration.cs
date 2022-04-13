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
using System.Windows.Forms;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.IniFile;
using Greenshot.Plugin.GooglePhotos.Forms;

namespace Greenshot.Plugin.GooglePhotos
{
    /// <summary>
    /// Description of GooglePhotosConfiguration.
    /// </summary>
    [IniSection("GooglePhotos", Description = "Greenshot GooglePhotos Plugin configuration")]
    public class GooglePhotosConfiguration : IniSection
    {
        [IniProperty("UploadFormat", Description = "What file type to use for uploading", DefaultValue = "png")]
        public OutputFormat UploadFormat { get; set; }

        [IniProperty("UploadJpegQuality", Description = "JPEG file save quality in %.", DefaultValue = "80")]
        public int UploadJpegQuality { get; set; }

        [IniProperty("AfterUploadLinkToClipBoard", Description = "After upload send GooglePhotos link to clipboard.", DefaultValue = "true")]
        public bool AfterUploadLinkToClipBoard { get; set; }

        [IniProperty("AddFilename", Description = "Is the filename passed on to GooglePhotos", DefaultValue = "False")]
        public bool AddFilename { get; set; }

        [IniProperty("UploadUser", Description = "The GooglePhotos user to upload to", DefaultValue = "default")]
        public string UploadUser { get; set; }

        [IniProperty("UploadAlbum", Description = "The GooglePhotos album to upload to", DefaultValue = "default")]
        public string UploadAlbum { get; set; }

        [IniProperty("RefreshToken", Description = "GooglePhotos authorization refresh Token", Encrypted = true)]
        public string RefreshToken { get; set; }

        /// <summary>
        /// Not stored
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Not stored
        /// </summary>
        public DateTimeOffset AccessTokenExpires { get; set; }

        /// <summary>
        /// A form for token
        /// </summary>
        /// <returns>bool true if OK was pressed, false if cancel</returns>
        public bool ShowConfigDialog()
        {
            DialogResult result = new SettingsForm().ShowDialog();
            if (result == DialogResult.OK)
            {
                return true;
            }

            return false;
        }
    }
}