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
using System.Drawing;
using System.IO.Compression;
using System.Windows.Forms;
using Caliburn.Micro;
using Dapplo.Addons;
using Dapplo.HttpExtensions;
using Dapplo.Jira.Entities;
using Dapplo.Log;
using Greenshot.Addon.Jira.ViewModels;
using Greenshot.Addons;
using Greenshot.Addons.Addons;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;

#endregion

namespace Greenshot.Addon.Jira
{
    /// <summary>
    ///     Description of JiraDestination.
    /// </summary>
    [Destination("Jira")]
    public class JiraDestination : AbstractDestination
	{
	    private static readonly LogSource Log = new LogSource();
		private readonly Issue _jiraIssue;
	    private readonly JiraConnector _jiraConnector;
	    private readonly IWindowManager _windowManager;
	    private readonly JiraViewModel _jiraViewModel;
	    private readonly IResourceProvider _resourceProvider;
	    private readonly IJiraConfiguration _jiraConfiguration;
	    private readonly IJiraLanguage _jiraLanguage;

	    public JiraDestination(
	        IJiraConfiguration jiraConfiguration,
	        IJiraLanguage jiraLanguage,
	        JiraConnector jiraConnector,
	        JiraViewModel jiraViewModel,
	        IWindowManager windowManager,
            IResourceProvider resourceProvider,
	        ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage
	    ) : base(coreConfiguration, greenshotLanguage)
        {
            _jiraConfiguration = jiraConfiguration;
            _jiraLanguage = jiraLanguage;
            _jiraConnector = jiraConnector;
            _windowManager = windowManager;
            _jiraViewModel = jiraViewModel;
            _resourceProvider = resourceProvider;
        }

		protected JiraDestination(IJiraConfiguration jiraConfiguration,
		    IJiraLanguage jiraLanguage,
		    JiraConnector jiraConnector,
		    JiraViewModel jiraViewModel,
		    IWindowManager windowManager,
		    IResourceProvider resourceProvider,
		    Issue jiraIssue,
		    ICoreConfiguration coreConfiguration,
		    IGreenshotLanguage greenshotLanguage
		    ) : this(jiraConfiguration, jiraLanguage, jiraConnector, jiraViewModel, windowManager, resourceProvider, coreConfiguration, greenshotLanguage)
		{
			_jiraIssue = jiraIssue;
		}

		public override string Description
		{
			get
			{
				if (_jiraIssue?.Fields?.Summary == null)
				{
					return _jiraLanguage.UploadMenuItem;
				}
				// Format the title of this destination
				return _jiraIssue.Key + ": " + _jiraIssue.Fields.Summary.Substring(0, Math.Min(20, _jiraIssue.Fields.Summary.Length));
			}
		}

		public override bool IsActive => base.IsActive && !string.IsNullOrEmpty(_jiraConfiguration.Url);

		public override bool IsDynamic => true;

		public override Bitmap DisplayIcon
		{
			get
			{
			    Bitmap displayIcon = null;
				if (_jiraConnector != null)
				{
					if (_jiraIssue != null)
					{
						// Try to get the issue type as icon
						try
						{
						    displayIcon = null; //_jiraConnector.GetIssueTypeBitmapAsync(_jiraIssue).Result;
						}
						catch (Exception ex)
						{
							Log.Warn().WriteLine(ex, $"Problem loading issue type for {_jiraIssue.Key}, ignoring");
						}
					}
					if (displayIcon == null)
					{
						displayIcon = _jiraConnector.FavIcon;
					}
				}
				if (displayIcon == null)
				{
				    using (var bitmapStream = _resourceProvider.ResourceAsStream(GetType(), "jira.svgz"))
				    {
				        using (var gzStream = new GZipStream(bitmapStream, CompressionMode.Decompress))
				        {
				            displayIcon = SvgBitmap.FromStream(gzStream).Bitmap;
				        }
                        //displayIcon = BitmapHelper.FromStream(bitmapStream);
                    }
				}
				return displayIcon;
			}
		}

	    public override IEnumerable<IDestination> DynamicDestinations()
        {
			if (_jiraConnector == null || !_jiraConnector.IsLoggedIn)
			{
				yield break;
			}
			foreach (var jiraDetails in _jiraConnector.RecentJiras)
			{
			    yield return new JiraDestination(
			        _jiraConfiguration, _jiraLanguage, _jiraConnector, _jiraViewModel,
			        _windowManager, _resourceProvider, jiraDetails.JiraIssue, CoreConfiguration, GreenshotLanguage);
			}
		}

	    protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			if (_jiraIssue != null)
			{
				try
				{
					// Run upload in the background
					new PleaseWaitForm().ShowAndWait(Description, _jiraLanguage.CommunicationWait,
						async () =>
						{
							await _jiraConnector.AttachAsync(_jiraIssue.Key, surface).ConfigureAwait(true);
							surface.UploadUrl = _jiraConnector.JiraBaseUri.AppendSegments("browse", _jiraIssue.Key).AbsoluteUri;
						}
					);
					Log.Debug().WriteLine("Uploaded to Jira {0}", _jiraIssue.Key);
					exportInformation.ExportMade = true;
					exportInformation.Uri = surface.UploadUrl;
				}
				catch (Exception e)
				{
					MessageBox.Show(_jiraLanguage.UploadFailure + " " + e.Message);
				}
			}
			else
			{
                // TODO: set filename
			    // _jiraViewModel.SetFilename(filename);
			    if (_windowManager.ShowDialog(_jiraViewModel) == true)
			    {
					try
					{
						surface.UploadUrl = _jiraConnector.JiraBaseUri.AppendSegments("browse", _jiraViewModel.JiraIssue.Key).AbsoluteUri;
						// Run upload in the background
						new PleaseWaitForm().ShowAndWait(Description, _jiraLanguage.CommunicationWait,
						    async () =>
						    {
						        await _jiraConnector.AttachAsync(_jiraViewModel.JiraIssue.Key, surface, _jiraViewModel.Filename).ConfigureAwait(true);

						        if (!string.IsNullOrEmpty(_jiraViewModel.Comment))
						        {
						            await _jiraConnector.AddCommentAsync(_jiraViewModel.JiraIssue.Key, _jiraViewModel.Comment).ConfigureAwait(true);
						        }
						    }
						);
						Log.Debug().WriteLine("Uploaded to Jira {0}", _jiraViewModel.JiraIssue.Key);
						exportInformation.ExportMade = true;
						exportInformation.Uri = surface.UploadUrl;
					}
					catch (Exception e)
					{
						MessageBox.Show(_jiraLanguage.UploadFailure + " " + e.Message);
					}
				}
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}