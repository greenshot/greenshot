/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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

using Dapplo.Config.Ini;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using GreenshotPlugin.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GreenshotJiraPlugin
{
	/// <summary>
	/// Jira destination.
	/// </summary>
	public class JiraDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(JiraDestination));
		private static JiraConfiguration config = IniConfig.Get("Greenshot", "greenshot").Get<JiraConfiguration>();
		private JiraPlugin _jiraPlugin = null;
		private JiraDetails _jira = null;

		public JiraDestination(JiraPlugin jiraPlugin) {
			_jiraPlugin = jiraPlugin;
		}

		public JiraDestination(JiraPlugin jiraPlugin, JiraDetails jira) {
			_jiraPlugin = jiraPlugin;
			_jira = jira;
		}

		public override string Designation {
			get {
				return "Jira";
			}
		}

		private string FormatUpload(JiraDetails jira) {
			return string.Format("{0} - {1}", jira.JiraKey, jira.Title.Substring(0, Math.Min(40, jira.Title.Length)));
		}

		public override string Description {
			get {
				if (_jira == null) {
					return Language.GetString("jira", LangKey.upload_menu_item);
				} else {
					return FormatUpload(_jira);
				}
			}
		}

		public override bool IsActive {
			get {
				return base.IsActive && _jiraPlugin.JiraMonitor != null && _jiraPlugin.JiraMonitor.HasJiraInstances;
			}
		}

		public override bool IsDynamic {
			get {
				return true;
			}
		}
		public override Image DisplayIcon {
			get {
				var resources = new ComponentResourceManager(typeof(JiraPlugin));
				return (Image)resources.GetObject("Jira");
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations() {
			if (IsActive) {
				// Show only the last 10 JIRAs
				foreach (var jiraIssue in _jiraPlugin.JiraMonitor.RecentJiras.Take(10)) {
					yield return new JiraDestination(_jiraPlugin, jiraIssue);
				}
			}
		}

		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surfaceToUpload, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken)) {
			var exportInformation = new ExportInformation(this.Designation, this.Description);
			string filename = Path.GetFileName(FilenameHelper.GetFilenameFromPattern(config.FilenamePattern, config.UploadFormat, captureDetails));
			var outputSettings = new SurfaceOutputSettings(config.UploadFormat, config.UploadJpegQuality, config.UploadReduceColors);
			if (_jira != null) {
				try {
					var jiraApi = _jiraPlugin.JiraMonitor.GetJiraApiForKey(_jira);
					var multipartFormDataContent = new MultipartFormDataContent();
					using (var stream = new MemoryStream()) {
						ImageOutput.SaveToStream(surfaceToUpload, stream, outputSettings);
						stream.Position = 0;
						// Run upload in the background
						await PleaseWaitWindow.CreateAndShowAsync(Description, Language.GetString("jira", LangKey.communication_wait), (progress, pleaseWaitToken) => {
							using (var uploadStream = new ProgressStream(stream, progress)) {
								using (var streamContent = new StreamContent(uploadStream)) {
									streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/" + outputSettings.Format);
									multipartFormDataContent.Add(streamContent, "file", filename);
									return jiraApi.Attach(_jira.JiraKey, multipartFormDataContent);
								}
							}
						});
					}

					LOG.Debug("Uploaded to Jira.");
					exportInformation.ExportMade = true;
					exportInformation.Uri = string.Format("{0}/browse/{1}", jiraApi.JiraBaseUri, _jira.JiraKey); ;
				} catch (TaskCanceledException tcEx) {
					exportInformation.ErrorMessage = tcEx.Message;
					LOG.Info(tcEx.Message);
				} catch (Exception e) {
					exportInformation.ErrorMessage = e.Message;
					LOG.Warn(e);
					MessageBox.Show(Designation, Language.GetString("jira", LangKey.upload_failure) + " " + e.Message, MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			ProcessExport(exportInformation, surfaceToUpload);
			return exportInformation;
		}
	}
}
