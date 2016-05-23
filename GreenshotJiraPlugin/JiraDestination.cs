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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using Jira;

namespace GreenshotJiraPlugin {
	/// <summary>
	/// Description of JiraDestination.
	/// </summary>
	public class JiraDestination : AbstractDestination {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(JiraDestination));
		private static readonly JiraConfiguration config = IniConfig.GetIniSection<JiraConfiguration>();
		private readonly JiraPlugin jiraPlugin = null;
		private readonly JiraIssue jira = null;
		
		public JiraDestination(JiraPlugin jiraPlugin) {
			this.jiraPlugin = jiraPlugin;
		}

		public JiraDestination(JiraPlugin jiraPlugin, JiraIssue jira) {
			this.jiraPlugin = jiraPlugin;
			this.jira = jira;
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
			get {
				if (jira == null) {
					return Language.GetString("jira", LangKey.upload_menu_item);
				} else {
					return FormatUpload(jira);
				}
			}
		}
		
		public override bool isActive {
			get {
				return base.isActive && !string.IsNullOrEmpty(config.Url);
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
			if (JiraPlugin.Instance.CurrentJiraConnector == null || !JiraPlugin.Instance.CurrentJiraConnector.isLoggedIn) {
				yield break;
			}
			List<JiraIssue> issues = JiraUtils.GetCurrentJiras();
			if (issues != null) {
				foreach(JiraIssue jiraIssue in issues) {
					yield return new JiraDestination(jiraPlugin, jiraIssue);
				}
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surfaceToUpload, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(this.Designation, this.Description);
			string filename = Path.GetFileName(FilenameHelper.GetFilename(config.UploadFormat, captureDetails));
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(config.UploadFormat, config.UploadJpegQuality, config.UploadReduceColors);
			if (jira != null) {
				try {
					// Run upload in the background
					new PleaseWaitForm().ShowAndWait(Description, Language.GetString("jira", LangKey.communication_wait),
						delegate() {
							jiraPlugin.JiraConnector.addAttachment(jira.Key, filename, new SurfaceContainer(surfaceToUpload, outputSettings, filename));
						}
					);
					LOG.Debug("Uploaded to Jira.");
					exportInformation.ExportMade = true;
					// TODO: This can't work:
					exportInformation.Uri = surfaceToUpload.UploadURL;
				} catch (Exception e) {
					MessageBox.Show(Language.GetString("jira", LangKey.upload_failure) + " " + e.Message);
				}
			} else {
				JiraForm jiraForm = new JiraForm(jiraPlugin.JiraConnector);
				if (jiraPlugin.JiraConnector.isLoggedIn) {
					jiraForm.setFilename(filename);
					DialogResult result = jiraForm.ShowDialog();
					if (result == DialogResult.OK) {
						try {
							// Run upload in the background
							new PleaseWaitForm().ShowAndWait(Description, Language.GetString("jira", LangKey.communication_wait),
								delegate() {
									jiraForm.upload(new SurfaceContainer(surfaceToUpload, outputSettings, filename));
								}
							);
							LOG.Debug("Uploaded to Jira.");
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
