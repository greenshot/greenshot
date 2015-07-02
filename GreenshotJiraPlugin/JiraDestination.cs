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

using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;
using System.Linq;

namespace GreenshotJiraPlugin {
	/// <summary>
	/// Description of JiraDestination.
	/// </summary>
	public class JiraDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(JiraDestination));
		private static JiraConfiguration config = IniConfig.GetIniSection<JiraConfiguration>();
		private JiraPlugin _jiraPlugin = null;
		private JiraDetails _jira = null;
		
		public JiraDestination(JiraPlugin jiraPlugin) {
			this._jiraPlugin = jiraPlugin;
		}

		public JiraDestination(JiraPlugin jiraPlugin, JiraDetails jira) {
			this._jiraPlugin = jiraPlugin;
			this._jira = jira;
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
		
		public override bool isActive {
			get {
				return base.isActive && _jiraPlugin.JiraMonitor != null && _jiraPlugin.JiraMonitor.HasJiraInstances;
			}
		}

		public override bool isDynamic {
			get {
				return true;
			}
		}
		public override Image DisplayIcon {
			get {
				ComponentResourceManager resources = new ComponentResourceManager(typeof(JiraPlugin));
				return (Image)resources.GetObject("Jira");
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations() {
			if (isActive) {
				// Show only the last 10 JIRAs
				foreach (JiraDetails jiraIssue in _jiraPlugin.JiraMonitor.RecentJiras.Take(10)) {
					yield return new JiraDestination(_jiraPlugin, jiraIssue);
				}
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surfaceToUpload, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(this.Designation, this.Description);
			string filename = Path.GetFileName(FilenameHelper.GetFilename(config.UploadFormat, captureDetails));
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(config.UploadFormat, config.UploadJpegQuality, config.UploadReduceColors);
			if (_jira != null) {
				try {
					// Run upload in the background
					new PleaseWaitForm().ShowAndWait(Description, Language.GetString("jira", LangKey.communication_wait),
						delegate() {
							var jiraApi = _jiraPlugin.JiraMonitor.GetJiraApiForKey(_jira);

							var multipartFormDataContent = new MultipartFormDataContent();
							using (MemoryStream stream = new MemoryStream()) {
								ImageOutput.SaveToStream(surfaceToUpload, stream, outputSettings);
								stream.Position = 0;
								using (var streamContent = new StreamContent(stream)) {
									streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/" + outputSettings.Format);
									multipartFormDataContent.Add(streamContent, "file", filename);
									var attachTask = jiraApi.Attach(_jira.JiraKey, multipartFormDataContent);
									attachTask.GetAwaiter().GetResult();
								}
							}
						}
					);
					LOG.Debug("Uploaded to Jira.");
					exportInformation.ExportMade = true;
					// TODO: This can't work:
					exportInformation.Uri = surfaceToUpload.UploadURL;
				} catch (Exception e) {
					MessageBox.Show(Language.GetString("jira", LangKey.upload_failure) + " " + e.Message);
				}
			}
			ProcessExport(exportInformation, surfaceToUpload);
			return exportInformation;
		}
	}
}
