#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows;
using Confluence;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
using Dapplo.Log;
using Greenshot.Gfx;

#endregion

namespace GreenshotConfluencePlugin
{
	/// <summary>
	///     Description of ConfluenceDestination.
	/// </summary>
	public class ConfluenceDestination : AbstractDestination
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly ConfluenceConfiguration ConfluenceConfig = IniConfig.GetIniSection<ConfluenceConfiguration>();
		private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
		private static readonly Bitmap ConfluenceIcon;
		private readonly Page _page;

		static ConfluenceDestination()
		{
			IsInitialized = false;
			try
			{
				var confluenceIconUri = new Uri("/GreenshotConfluencePlugin;component/Images/Confluence.ico", UriKind.Relative);
				using (var iconStream = Application.GetResourceStream(confluenceIconUri)?.Stream)
				{
					// TODO: Check what to do with the IBitmap
					ConfluenceIcon = BitmapHelper.FromStream(iconStream);
				}
				IsInitialized = true;
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine("Problem in the confluence static initializer: {0}", ex.Message);
			}
		}

		public ConfluenceDestination()
		{
		}

		public ConfluenceDestination(Page page)
		{
			_page = page;
		}

		public static bool IsInitialized { get; private set; }

		public override string Designation
		{
			get { return "Confluence"; }
		}

		public override string Description
		{
			get
			{
				if (_page == null)
				{
					return Language.GetString("confluence", LangKey.upload_menu_item);
				}
				return Language.GetString("confluence", LangKey.upload_menu_item) + ": \"" + _page.Title + "\"";
			}
		}

		public override bool IsDynamic
		{
			get { return true; }
		}

		public override bool IsActive
		{
			get { return base.IsActive && !string.IsNullOrEmpty(ConfluenceConfig.Url); }
		}

		public override Bitmap DisplayIcon
		{
			get { return ConfluenceIcon; }
		}

		public override IEnumerable<IDestination> DynamicDestinations()
		{
			if (ConfluencePlugin.ConfluenceConnectorNoLogin == null || !ConfluencePlugin.ConfluenceConnectorNoLogin.IsLoggedIn)
			{
				yield break;
			}
			var currentPages = ConfluenceUtils.GetCurrentPages();
			if (currentPages == null || currentPages.Count == 0)
			{
				yield break;
			}
			foreach (var currentPage in currentPages)
			{
				yield return new ConfluenceDestination(currentPage);
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			// force password check to take place before the pages load
			if (!ConfluencePlugin.ConfluenceConnector.IsLoggedIn)
			{
				return exportInformation;
			}

			var selectedPage = _page;
			var openPage = _page == null && ConfluenceConfig.OpenPageAfterUpload;
			var filename = FilenameHelper.GetFilenameWithoutExtensionFromPattern(CoreConfig.OutputFileFilenamePattern, captureDetails);
			if (selectedPage == null)
			{
				var confluenceUpload = new ConfluenceUpload(filename);
				var dialogResult = confluenceUpload.ShowDialog();
				if (dialogResult.HasValue && dialogResult.Value)
				{
					selectedPage = confluenceUpload.SelectedPage;
					if (confluenceUpload.IsOpenPageSelected)
					{
						openPage = false;
					}
					filename = confluenceUpload.Filename;
				}
			}
			var extension = "." + ConfluenceConfig.UploadFormat;
			if (!filename.ToLower().EndsWith(extension))
			{
				filename = filename + extension;
			}
			if (selectedPage != null)
			{
				string errorMessage;
				var uploaded = Upload(surface, selectedPage, filename, out errorMessage);
				if (uploaded)
				{
					if (openPage)
					{
						try
						{
							Process.Start(selectedPage.Url);
						}
						catch
						{
							// Ignore
						}
					}
					exportInformation.ExportMade = true;
					exportInformation.Uri = selectedPage.Url;
				}
				else
				{
					exportInformation.ErrorMessage = errorMessage;
				}
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}

		private bool Upload(ISurface surfaceToUpload, Page page, string filename, out string errorMessage)
		{
			var outputSettings = new SurfaceOutputSettings(ConfluenceConfig.UploadFormat, ConfluenceConfig.UploadJpegQuality, ConfluenceConfig.UploadReduceColors);
			errorMessage = null;
			try
			{
				new PleaseWaitForm().ShowAndWait(Description, Language.GetString("confluence", LangKey.communication_wait),
					delegate
					{
						ConfluencePlugin.ConfluenceConnector.AddAttachment(page.Id, "image/" + ConfluenceConfig.UploadFormat.ToString().ToLower(), null, filename,
							new SurfaceContainer(surfaceToUpload, outputSettings, filename));
					}
				);
				Log.Debug().WriteLine("Uploaded to Confluence.");
				if (!ConfluenceConfig.CopyWikiMarkupForImageToClipboard)
				{
					return true;
				}
				var retryCount = 2;
				while (retryCount >= 0)
				{
					try
					{
						Clipboard.SetText("!" + filename + "!");
						break;
					}
					catch (Exception ee)
					{
						if (retryCount == 0)
						{
							Log.Error().WriteLine(ee);
						}
						else
						{
							Thread.Sleep(100);
						}
					}
					finally
					{
						--retryCount;
					}
				}
				return true;
			}
			catch (Exception e)
			{
				errorMessage = e.Message;
			}
			return false;
		}
	}
}