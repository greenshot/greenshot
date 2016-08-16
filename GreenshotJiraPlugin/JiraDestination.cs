/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotJiraPlugin.Forms;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;

namespace GreenshotJiraPlugin {
	/// <summary>
	/// Description of JiraDestination.
	/// </summary>
	public class JiraDestination : AbstractDestination {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(JiraDestination));
		private static readonly JiraConfiguration Config = IniConfig.GetIniSection<JiraConfiguration>();
		private readonly JiraPlugin _jiraPlugin;
		private readonly JiraIssue _jira;
		
		public JiraDestination(JiraPlugin jiraPlugin) {
			_jiraPlugin = jiraPlugin;
		}

		public JiraDestination(JiraPlugin jiraPlugin, JiraIssue jira) {
			_jiraPlugin = jiraPlugin;
			_jira = jira;
		}

		public override string Designation {
			get {
				return "Jira";
			}
		}

		private string FormatUpload(JiraIssue jira) {
			return Designation + " - " + jira.Key + ": " + jira.Summary.Substring(0, Math.Min(20, jira.Summary.Length));
		}

		public override string Description {
			get
			{
				if (_jira == null) {
					return Language.GetString("jira", LangKey.upload_menu_item);
				}
				return FormatUpload(_jira);
			}
		}
		
		public override bool isActive {
			get {
				return base.isActive && !string.IsNullOrEmpty(Config.Url);
			}
		}

		public override bool isDynamic {
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
			if (JiraPlugin.Instance.CurrentJiraConnector == null || !JiraPlugin.Instance.CurrentJiraConnector.IsLoggedIn) {
				yield break;
			}
			List<JiraIssue> issues = JiraUtils.GetCurrentJiras();
			if (issues != null) {
				foreach(var jiraIssue in issues) {
					yield return new JiraDestination(_jiraPlugin, jiraIssue);
				}
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surfaceToUpload, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(Designation, Description);
			string filename = Path.GetFileName(FilenameHelper.GetFilename(Config.UploadFormat, captureDetails));
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(Config.UploadFormat, Config.UploadJpegQuality, Config.UploadReduceColors);
			if (_jira != null) {
				try {
					// Run upload in the background
					new PleaseWaitForm().ShowAndWait(Description, Language.GetString("jira", LangKey.communication_wait),
						delegate {
							_jiraPlugin.JiraConnector.AddAttachment(_jira.Key, filename, new SurfaceContainer(surfaceToUpload, outputSettings, filename));
						}
					);
					Log.Debug("Uploaded to Jira.");
					exportInformation.ExportMade = true;
					// TODO: This can't work:
					exportInformation.Uri = surfaceToUpload.UploadURL;
				} catch (Exception e) {
					MessageBox.Show(Language.GetString("jira", LangKey.upload_failure) + " " + e.Message);
				}
			} else {
				JiraForm jiraForm = new JiraForm(_jiraPlugin.JiraConnector);
				if (_jiraPlugin.JiraConnector.IsLoggedIn) {
					jiraForm.SetFilename(filename);
					DialogResult result = jiraForm.ShowDialog();
					if (result == DialogResult.OK) {
						try {
							// Run upload in the background
							new PleaseWaitForm().ShowAndWait(Description, Language.GetString("jira", LangKey.communication_wait),
								delegate {
									jiraForm.Upload(new SurfaceContainer(surfaceToUpload, outputSettings, filename));
								}
							);
							Log.Debug("Uploaded to Jira.");
							exportInformation.ExportMade = true;
							// TODO: This can't work:
							exportInformation.Uri = surfaceToUpload.UploadURL;
						} catch(Exception e) {
							MessageBox.Show(Language.GetString("jira", LangKey.upload_failure) + " " + e.Message);
						}
					}
				}
			}
			ProcessExport(exportInformation, surfaceToUpload);
			return exportInformation;
		}
	}
}
