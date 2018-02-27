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

namespace GreenshotGooglePhotosPlugin
{
    /// <summary>
    ///     This is the Google Photos base code
    /// </summary>
    [Export(typeof(IGreenshotPlugin))]
    public sealed class GooglePhotosPlugin : IGreenshotPlugin
	{
		private static readonly LogSource Log = new LogSource();
		private readonly IGooglePhotosConfiguration _googlePhotosConfiguration;
		private readonly IGreenshotHost _greenshotHost;
		private ToolStripMenuItem _itemPlugInRoot;
		private ComponentResourceManager _resources;

	    [ImportingConstructor]
	    public GooglePhotosPlugin(IGreenshotHost greenshotGreenshotHost, IGooglePhotosConfiguration googlePhotosConfiguration)
	    {
	        _greenshotHost = greenshotGreenshotHost;
	        _googlePhotosConfiguration = googlePhotosConfiguration;
	    }

        public void Dispose()
		{
			Dispose(true);
		}

		public IEnumerable<IDestination> Destinations()
		{
			yield return new GooglePhotosDestination(this);
		}


		public IEnumerable<IProcessor> Processors()
		{
			yield break;
		}

		/// <summary>
		///     Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		public bool Initialize()
		{
			// Get configuration
			_resources = new ComponentResourceManager(typeof(GooglePhotosPlugin));

			_itemPlugInRoot = new ToolStripMenuItem
			{
				Text = Language.GetString("googlephotos", LangKey.Configure),
				Tag = _greenshotHost,
				Image = (Image) _resources.GetObject("GooglePhotos")
			};
			PluginUtils.AddToContextMenu(_greenshotHost, _itemPlugInRoot);
			Language.LanguageChanged += OnLanguageChanged;
			return true;
		}

		public void Shutdown()
		{
			Log.Debug().WriteLine("GooglePhotos Plugin shutdown.");
			Language.LanguageChanged -= OnLanguageChanged;
			//host.OnImageEditorOpen -= new OnImageEditorOpenHandler(ImageEditorOpened);
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_itemPlugInRoot != null)
				{
					_itemPlugInRoot.Dispose();
					_itemPlugInRoot = null;
				}
			}
		}

		public void OnLanguageChanged(object sender, EventArgs e)
		{
			if (_itemPlugInRoot != null)
			{
				_itemPlugInRoot.Text = Language.GetString("googlePhotos", LangKey.Configure);
			}
		}

		public bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, out string uploadUrl)
		{
			var outputSettings = new SurfaceOutputSettings(_googlePhotosConfiguration.UploadFormat, _googlePhotosConfiguration.UploadJpegQuality);
			try
			{
				string url = null;
				new PleaseWaitForm().ShowAndWait("Google Photos", Language.GetString("googlephotos", LangKey.communication_wait),
					delegate
					{
						var filename = Path.GetFileName(FilenameHelper.GetFilename(_googlePhotosConfiguration.UploadFormat, captureDetails));
						url = GooglePhotosUtils.UploadToGooglePhotos(surfaceToUpload, outputSettings, captureDetails.Title, filename);
					}
				);
				uploadUrl = url;

				if (uploadUrl != null && _googlePhotosConfiguration.AfterUploadLinkToClipBoard)
				{
					ClipboardHelper.SetClipboardData(uploadUrl);
				}
				return true;
			}
			catch (Exception e)
			{
				Log.Error().WriteLine(e, "Error uploading.");
				MessageBox.Show(Language.GetString("googlephotos", LangKey.upload_failure) + " " + e.Message);
			}
			uploadUrl = null;
			return false;
		}
	}
}