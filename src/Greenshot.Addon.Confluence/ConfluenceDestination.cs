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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using Autofac.Features.OwnedInstances;
using Dapplo.Confluence;
using Dapplo.Confluence.Entities;
using Dapplo.Log;
using Greenshot.Addon.Confluence.Configuration;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Gfx;

namespace Greenshot.Addon.Confluence
{
    /// <summary>
    ///     Description of ConfluenceDestination.
    /// </summary>
    [Destination("Confluence")]
    public class ConfluenceDestination : AbstractDestination
	{
	    private static readonly LogSource Log = new LogSource();
		private static readonly IBitmapWithNativeSupport ConfluenceIcon;
	    private readonly ExportNotification _exportNotification;
	    private readonly IConfluenceConfiguration _confluenceConfiguration;
	    private readonly IConfluenceLanguage _confluenceLanguage;
	    private readonly Func<Owned<PleaseWaitForm>> _pleaseWaitFormFactory;
	    private IConfluenceClient _confluenceClient;
	    private readonly Content _page;

		static ConfluenceDestination()
		{
			IsInitialized = false;
			try
			{
				var confluenceIconUri = new Uri("/Greenshot.Addon.Confluence;component/Images/Confluence.ico", UriKind.Relative);
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

		public ConfluenceDestination(
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage,
            ExportNotification exportNotification,
		    IConfluenceConfiguration confluenceConfiguration,
		    IConfluenceLanguage confluenceLanguage,
            Func<Owned<PleaseWaitForm>> pleaseWaitFormFactory
            ) : base(coreConfiguration, greenshotLanguage)
        {
            _exportNotification = exportNotification;
            _confluenceConfiguration = confluenceConfiguration;
            _confluenceLanguage = confluenceLanguage;
            _pleaseWaitFormFactory = pleaseWaitFormFactory;
        }

	    private ConfluenceDestination(
	        ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
	        ExportNotification exportNotification,
	        IConfluenceConfiguration confluenceConfiguration,
	        IConfluenceLanguage confluenceLanguage,
	        Func<Owned<PleaseWaitForm>> pleaseWaitFormFactory,
            Content page) : this(coreConfiguration, greenshotLanguage, exportNotification, confluenceConfiguration, confluenceLanguage, pleaseWaitFormFactory)
	    {
	        _page = page;
	    }

        /// <summary>
        /// Is the destination initialized?
        /// </summary>
        public static bool IsInitialized { get; private set; }

        /// <inheritdoc />
        public override string Description
		{
			get
			{
				if (_page == null)
				{
					return _confluenceLanguage.UploadMenuItem;
				}
				return _confluenceLanguage.UploadMenuItem + ": \"" + _page /*.Title */ + "\"";
			}
		}

        /// <inheritdoc />
        public override bool IsDynamic => true;

        /// <inheritdoc />
        public override bool IsActive => base.IsActive && !string.IsNullOrEmpty(_confluenceConfiguration.Url);

	    public override IBitmapWithNativeSupport DisplayIcon => ConfluenceIcon;

	    public override IEnumerable<IDestination> DynamicDestinations()
	    {
	        var currentPages = ConfluenceUtils.GetCurrentPages(null).Result;
			if (currentPages == null || currentPages.Count == 0)
			{
				yield break;
			}
			foreach (var currentPage in currentPages)
			{
				yield return new ConfluenceDestination(CoreConfiguration, GreenshotLanguage, _exportNotification,_confluenceConfiguration, _confluenceLanguage, _pleaseWaitFormFactory, currentPage);
			}
		}

        /// <inheritdoc />
        protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			// force password check to take place before the pages load

			var selectedPage = _page;
			var openPage = _page == null && _confluenceConfiguration.OpenPageAfterUpload;
			var filename = FilenameHelper.GetFilenameWithoutExtensionFromPattern(_confluenceConfiguration.OutputFileFilenamePattern, captureDetails);
			if (selectedPage == null)
			{

			}
			var extension = "." + _confluenceConfiguration.UploadFormat;
			if (!filename.ToLower().EndsWith(extension))
			{
				filename = filename + extension;
			}
			if (selectedPage != null)
			{
			    var uploaded = Upload(surface, selectedPage, filename, out var errorMessage);
				if (uploaded)
				{
					if (openPage)
					{
						try
						{
							Process.Start(selectedPage.Links.WebUi);
						}
						catch
						{
							// Ignore
						}
					}
					exportInformation.ExportMade = true;
					exportInformation.Uri = selectedPage.Links.WebUi;
				}
				else
				{
					exportInformation.ErrorMessage = errorMessage;
				}
			}
		    _exportNotification.NotifyOfExport(this, exportInformation, surface);
            return exportInformation;
		}

		private bool Upload(ISurface surfaceToUpload, Content page, string filename, out string errorMessage)
		{
			var outputSettings = new SurfaceOutputSettings(CoreConfiguration, _confluenceConfiguration.UploadFormat, _confluenceConfiguration.UploadJpegQuality, _confluenceConfiguration.UploadReduceColors);
			errorMessage = null;
			try
			{
			    // TODO: Create content
			    using (var ownedPleaseWaitForm = _pleaseWaitFormFactory())
			    {
			        ownedPleaseWaitForm.Value.ShowAndWait(Description, _confluenceLanguage.CommunicationWait,
			            () => _confluenceClient.Attachment.AttachAsync(page.Id, surfaceToUpload, filename, null,
			                "image/" + _confluenceConfiguration.UploadFormat.ToString().ToLower()));
			    }
				Log.Debug().WriteLine("Uploaded to Confluence.");
				if (!_confluenceConfiguration.CopyWikiMarkupForImageToClipboard)
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