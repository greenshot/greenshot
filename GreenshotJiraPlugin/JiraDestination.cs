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
using Dapplo.HttpExtensions;
using Dapplo.Jira.Entities;
using GreenshotJiraPlugin.Forms;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
using Dapplo.Log;
using GreenshotPlugin.Addons;

#endregion

namespace GreenshotJiraPlugin
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
	    private readonly IJiraConfiguration _jiraConfiguration;

        [ImportingConstructor]
		public JiraDestination(IJiraConfiguration jiraConfiguration, JiraConnector jiraConnector)
        {
            _jiraConfiguration = jiraConfiguration;
            _jiraConnector = jiraConnector;
        }

		public JiraDestination(IJiraConfiguration jiraConfiguration, JiraConnector jiraConnector, Issue jiraIssue)
		{
		    _jiraConfiguration = jiraConfiguration;
		    _jiraConnector = jiraConnector;
			_jiraIssue = jiraIssue;
		}

		public override string Description
		{
			get
			{
				if (_jiraIssue?.Fields?.Summary == null)
				{
					return Language.GetString("jira", LangKey.upload_menu_item);
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
							displayIcon = _jiraConnector.GetIssueTypeBitmapAsync(_jiraIssue).Result;
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
					var resources = new ComponentResourceManager(typeof(JiraPlugin));
					displayIcon = (Bitmap) resources.GetObject("Jira");
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
			foreach (var jiraDetails in _jiraConnector.Monitor.RecentJiras)
			{
				yield return new JiraDestination(_jiraConfiguration, _jiraConnector, jiraDetails.JiraIssue);
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			var filename = Path.GetFileName(FilenameHelper.GetFilename(_jiraConfiguration.UploadFormat, captureDetails));
			var outputSettings = new SurfaceOutputSettings(_jiraConfiguration.UploadFormat, _jiraConfiguration.UploadJpegQuality, _jiraConfiguration.UploadReduceColors);
			if (_jiraIssue != null)
			{
				try
				{
					// Run upload in the background
					new PleaseWaitForm().ShowAndWait(Description, Language.GetString("jira", LangKey.communication_wait),
						async () =>
						{
							var surfaceContainer = new SurfaceContainer(surface, outputSettings, filename);
							await _jiraConnector.AttachAsync(_jiraIssue.Key, surfaceContainer);
							surface.UploadUrl = _jiraConnector.JiraBaseUri.AppendSegments("browse", _jiraIssue.Key).AbsoluteUri;
						}
					);
					Log.Debug().WriteLine("Uploaded to Jira {0}", _jiraIssue.Key);
					exportInformation.ExportMade = true;
					exportInformation.Uri = surface.UploadUrl;
				}
				catch (Exception e)
				{
					MessageBox.Show(Language.GetString("jira", LangKey.upload_failure) + " " + e.Message);
				}
			}
			else
			{
				var jiraForm = new JiraForm(_jiraConnector);
				jiraForm.SetFilename(filename);
				var dialogResult = jiraForm.ShowDialog();
				if (dialogResult == DialogResult.OK)
				{
					try
					{
						surface.UploadUrl = _jiraConnector.JiraBaseUri.AppendSegments("browse", jiraForm.GetJiraIssue().Key).AbsoluteUri;
						// Run upload in the background
						new PleaseWaitForm().ShowAndWait(Description, Language.GetString("jira", LangKey.communication_wait),
							async () => { await jiraForm.UploadAsync(new SurfaceContainer(surface, outputSettings, filename)); }
						);
						Log.Debug().WriteLine("Uploaded to Jira {0}", jiraForm.GetJiraIssue().Key);
						exportInformation.ExportMade = true;
						exportInformation.Uri = surface.UploadUrl;
					}
					catch (Exception e)
					{
						MessageBox.Show(Language.GetString("jira", LangKey.upload_failure) + " " + e.Message);
					}
				}
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}