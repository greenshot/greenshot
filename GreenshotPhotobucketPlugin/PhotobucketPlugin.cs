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
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
using Dapplo.Log;

#endregion

namespace GreenshotPhotobucketPlugin
{
    /// <summary>
    ///     This is the GreenshotPhotobucketPlugin base code
    /// </summary>
    [Export(typeof(IGreenshotPlugin))]
    public sealed class PhotobucketPlugin : IGreenshotPlugin
	{
	    private readonly IGreenshotHost _greenshotHost;
	    private readonly IPhotobucketConfiguration _photobucketConfiguration;
	    private static readonly LogSource Log = new LogSource();
		private ToolStripMenuItem _itemPlugInConfig;
		private ComponentResourceManager _resources;

        [ImportingConstructor]
	    public PhotobucketPlugin(IGreenshotHost greenshotHost, IPhotobucketConfiguration photobucketConfiguration)
        {
            _greenshotHost = greenshotHost;
            _photobucketConfiguration = photobucketConfiguration;
        }

		public void Dispose()
		{
			Dispose(true);
		}

		public IEnumerable<IDestination> Destinations()
		{
			yield return new PhotobucketDestination(this, null);
		}

		public IEnumerable<IProcessor> Processors()
		{
			yield break;
		}

		/// <summary>
		///     Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public bool Initialize()
		{
			_resources = new ComponentResourceManager(typeof(PhotobucketPlugin));

			_itemPlugInConfig = new ToolStripMenuItem(Language.GetString("photobucket", LangKey.configure))
			{
				Tag = _greenshotHost,
				Image = (Image) _resources.GetObject("Photobucket")
			};

			PluginUtils.AddToContextMenu(_greenshotHost, _itemPlugInConfig);
			Language.LanguageChanged += OnLanguageChanged;
			return true;
		}

		public void Shutdown()
		{
			Log.Debug().WriteLine("Photobucket Plugin shutdown.");
			Language.LanguageChanged -= OnLanguageChanged;
		}

		public void Dispose(bool disposing)
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

		public void OnLanguageChanged(object sender, EventArgs e)
		{
			if (_itemPlugInConfig != null)
			{
				_itemPlugInConfig.Text = Language.GetString("photobucket", LangKey.configure);
			}
		}

		/// <summary>
		///     Upload the capture to Photobucket
		/// </summary>
		/// <param name="captureDetails"></param>
		/// <param name="surfaceToUpload">ISurface</param>
		/// <param name="albumPath">Path to the album</param>
		/// <param name="uploadUrl">out string for the url</param>
		/// <returns>true if the upload succeeded</returns>
		public bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, string albumPath, out string uploadUrl)
		{
			var outputSettings = new SurfaceOutputSettings(_photobucketConfiguration.UploadFormat, _photobucketConfiguration.UploadJpegQuality, _photobucketConfiguration.UploadReduceColors);
			try
			{
				var filename = Path.GetFileName(FilenameHelper.GetFilename(_photobucketConfiguration.UploadFormat, captureDetails));
				PhotobucketInfo photobucketInfo = null;

				// Run upload in the background
				new PleaseWaitForm().ShowAndWait("Photobucket", Language.GetString("photobucket", LangKey.communication_wait),
					delegate { photobucketInfo = PhotobucketUtils.UploadToPhotobucket(surfaceToUpload, outputSettings, albumPath, captureDetails.Title, filename); }
				);
				// This causes an exeption if the upload failed :)
				Log.Debug().WriteLine("Uploaded to Photobucket page: " + photobucketInfo.Page);
				uploadUrl = null;
				try
				{
					if (_photobucketConfiguration.UsePageLink)
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
					Log.Error().WriteLine(ex, "Can't write to clipboard: ");
				}
				return true;
			}
			catch (Exception e)
			{
				Log.Error().WriteLine(e);
				MessageBox.Show(Language.GetString("photobucket", LangKey.upload_failure) + " " + e.Message);
			}
			uploadUrl = null;
			return false;
		}
	}
}