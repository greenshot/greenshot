#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.OAuth;
using Dapplo.Log;
using Dapplo.Utils;
using Greenshot.Addon.OneDrive.Entities;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;

#endregion

namespace Greenshot.Addon.OneDrive
{
    /// <summary>
    /// Description of OneDriveUtils.
    /// </summary>
    public class OneDriveUtils
    {
        
        private static readonly LogSource Log = new LogSource();
        private static readonly IOneDriveConfiguration _config = IniConfig.Current.Get<IOneDriveConfiguration>();

        private static readonly HttpBehaviour Behaviour = new HttpBehaviour();

        private OneDriveUtils()
        {
        }

        /// <summary>
        ///     Do the actual upload to OneDrive
        /// </summary>
        /// <param name="oAuth2Settings">OAuth2Settings</param>
        /// <param name="surfaceToUpload">ISurface to upload</param>
        /// <param name="outputSettings">OutputSettings for the image file format</param>
        /// <param name="title">Title</param>
        /// <param name="filename">Filename</param>
        /// <param name="progress">IProgress</param>
        /// <param name="token">CancellationToken</param>
        /// <returns>OneDriveUploadResponse with details</returns>
        public static async Task<OneDriveUploadResponse> UploadToOneDriveAsync(OAuth2Settings oAuth2Settings, ISurface surfaceToUpload,
            SurfaceOutputSettings outputSettings, string title, string filename, IProgress<int> progress = null,
            CancellationToken token = default)
        {

            var uploadUri = new Uri(UrlUtils.GetUploadUrl(filename));
            var localBehaviour = Behaviour.ShallowClone();
            if (progress != null)
            {
                localBehaviour.UploadProgress = percent => { UiContext.RunOn(() => progress.Report((int)(percent * 100)), token); };
            }
            var oauthHttpBehaviour = OAuth2HttpBehaviourFactory.Create(oAuth2Settings, localBehaviour);
            using (var imageStream = new MemoryStream())
            {
                ImageOutput.SaveToStream(surfaceToUpload, imageStream, outputSettings);
                imageStream.Position = 0;
                using (var content = new StreamContent(imageStream))
                {
                    content.Headers.Add("Content-Type", "image/" + outputSettings.Format);
                    oauthHttpBehaviour.MakeCurrent();
                    return await uploadUri.PostAsync<OneDriveUploadResponse>(content, token);
                }
            }
        }



    }
}