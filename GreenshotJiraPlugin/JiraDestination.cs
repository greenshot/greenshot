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

using GreenshotPlugin.Core;
using GreenshotPlugin.Windows;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Dapplo.HttpExtensions;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Destination;
using GreenshotPlugin.Interfaces.Plugin;
using GreenshotPlugin.Extensions;

namespace GreenshotJiraPlugin
{
	/// <summary>
	/// Jira destination.
	/// </summary>
	[Destination(JiraDesignation)]
	public sealed class JiraDestination : AbstractDestination
	{
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof (JiraDestination));
		private const string JiraDesignation = "Jira";
		private static readonly BitmapSource JiraIcon;

		static JiraDestination()
		{
			var resources = new ComponentResourceManager(typeof(JiraPlugin));
			using (var jiraLogo = (Bitmap) resources.GetObject("Jira"))
			{
				JiraIcon = jiraLogo.ToBitmapSource();
			}
		}

		[Import]
		private JiraPlugin Plugin
		{
			get;
			set;
		}

		[Import]
		private IJiraConfiguration JiraConfiguration
		{
			get;
			set;
		}

		[Import]
		private IJiraLanguage JiraLanguage
		{
			get;
			set;
		}

		[Import]
		private IGreenshotHost GreenshotHost
		{
			get;
			set;
		}

		/// <summary>
		/// Setup, this is only called for the base element
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();
			Designation = JiraDesignation;
			Export = async (capture, token) => await ExportCaptureAsync(capture, null, token);
			Text = JiraLanguage.UploadMenuItem;
			Icon = JiraIcon;
			if (Plugin.JiraMonitor != null)
			{
				UpdateChildren();
				Plugin.JiraMonitor.JiraEvent += JiraMonitor_JiraEvent;
			}
			else
			{
				IsEnabled = false;
            }
		}

		private void UpdateChildren()
		{
			IsEnabled = Plugin.JiraMonitor.RecentJiras.Count() > 0;  // As soon as we have issues this should be set to true
			Children.Clear();
			foreach (var jiraDetails in Plugin.JiraMonitor.RecentJiras)
			{
				var jiraDestination = new JiraDestination
				{
					Icon = JiraIcon,
					Export = async (capture, token) => await ExportCaptureAsync(capture, jiraDetails, token),
					Text = FormatUpload(jiraDetails),
					Plugin = Plugin,
					JiraLanguage = JiraLanguage,
					JiraConfiguration = JiraConfiguration,
					GreenshotHost = GreenshotHost
				};
				Children.Add(jiraDestination);
			}
		}

		private void JiraMonitor_JiraEvent(object sender, JiraEventArgs e)
		{
			GreenshotHost.GreenshotForm.AsyncInvoke(() => UpdateChildren());
		}

		private string FormatUpload(JiraDetails jira)
		{
			return $"{jira.JiraKey} - {jira.Title.Substring(0, Math.Min(40, jira.Title.Length))}";
		}

		public async Task<INotification> ExportCaptureAsync(ICapture capture, JiraDetails jiraDetails, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = JiraDesignation,
				SourceType = SourceTypes.Destination,
				Text = string.Format(JiraLanguage.UploadSuccess, JiraDesignation)
			};

			string filename = Path.GetFileName(FilenameHelper.GetFilenameFromPattern(JiraConfiguration.FilenamePattern, JiraConfiguration.UploadFormat, capture.CaptureDetails));
			var outputSettings = new SurfaceOutputSettings(JiraConfiguration.UploadFormat, JiraConfiguration.UploadJpegQuality, JiraConfiguration.UploadReduceColors);
			if (jiraDetails != null)
			{
				try
				{
					var jiraApi = Plugin.JiraMonitor.GetJiraApiForKey(jiraDetails);
					// Run upload in the background
					await PleaseWaitWindow.CreateAndShowAsync(Text, JiraLanguage.CommunicationWait, async (progress, pleaseWaitToken) =>
					{
						var multipartFormDataContent = new MultipartFormDataContent();
						using (var stream = new MemoryStream())
						{
							ImageOutput.SaveToStream(capture, stream, outputSettings);
							stream.Position = 0;
							using (var uploadStream = new ProgressStream(stream, progress))
							{
								using (var streamContent = new StreamContent(uploadStream))
								{
									streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/" + outputSettings.Format);
									multipartFormDataContent.Add(streamContent, "file", filename);
									using (var reponseMessage = await jiraApi.Attach(jiraDetails.JiraKey, multipartFormDataContent, pleaseWaitToken))
									{
										return reponseMessage.GetAsStringAsync(token: token);
									}
								}
							}
						}
					}, token);

					LOG.Debug("Uploaded to Jira.");
					returnValue.ImageLocation = jiraApi.JiraBaseUri.AppendSegments("browse", jiraDetails.JiraKey);
				}
				catch (TaskCanceledException tcEx)
				{
					returnValue.Text = string.Format(JiraLanguage.UploadFailure, JiraDesignation);
					returnValue.NotificationType = NotificationTypes.Cancel;
					returnValue.ErrorText = tcEx.Message;
					LOG.Info(tcEx.Message);
				}
				catch (Exception e)
				{
					returnValue.Text = string.Format(JiraLanguage.UploadFailure, JiraDesignation);
					returnValue.NotificationType = NotificationTypes.Fail;
					returnValue.ErrorText = e.Message;
					LOG.Warn(e);
				}
			}
			return returnValue;
		}
	}
}