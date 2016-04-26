/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
using Dapplo.Utils;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Interfaces.Plugin;
using Greenshot.Addon.Windows;

namespace Greenshot.Addon.Jira
{
	/// <summary>
	/// Jira destination.
	/// </summary>
	[Destination(JiraDesignation), PartNotDiscoverable]
	public sealed class JiraDestination : AbstractDestination
	{
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(JiraDestination));
		private const string JiraDesignation = "Jira";
		private JiraMonitor _jiraMonitor;

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
		/// This should only be set from the plugin
		/// </summary>
		public JiraMonitor Monitor
		{
			get
			{
				return _jiraMonitor;
			}
			set
			{
				if (_jiraMonitor != null)
				{
					_jiraMonitor.JiraEvent -= JiraMonitor_JiraEvent;
					_jiraMonitor.Dispose();
				}
				_jiraMonitor = value;
				_jiraMonitor.JiraEvent += JiraMonitor_JiraEvent;
				UpdateChildren();
			}
		}

		/// <summary>
		/// Setup, this is only called for the base element
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();
			Designation = JiraDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, null, token);
			Text = JiraLanguage.UploadMenuItem;

			var resources = new ComponentResourceManager(typeof(JiraPlugin));
			// Make sure this runs on the UI
			UiContext.RunOn(() =>
			{
				using (var jiraLogo = (Bitmap)resources.GetObject("Jira"))
				{
					Icon = jiraLogo.ToBitmapSource();
				}
			}).Wait();
		}

		private void UpdateChildren()
		{
			IsEnabled = _jiraMonitor.RecentJiras.Any();  // As soon as we have issues this should be set to true

			Task.Run(() =>
			{
				return _jiraMonitor.RecentJiras.Select(jiraDetails => new JiraDestination
				{
					// TODO: Change to icon for the jira itself
					Icon = Icon,
					Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, jiraDetails, token),
					Text = FormatUpload(jiraDetails),
					JiraLanguage = JiraLanguage,
					JiraConfiguration = JiraConfiguration,
					GreenshotHost = GreenshotHost,
					// DO NOT set the JiraMonitor property on the children
					_jiraMonitor = _jiraMonitor
				}).ToList();
			}).ContinueWith(async destinations =>
			{
				Children.Clear();
				foreach (var jiraDestination in await destinations)
				{
					Children.Add(jiraDestination);
				}
			}, CancellationToken.None, TaskContinuationOptions.None, UiContext.UiTaskScheduler).ConfigureAwait(false);
		}

		private void JiraMonitor_JiraEvent(object sender, JiraEventArgs e)
		{
			UpdateChildren();
		}

		private static string FormatUpload(JiraDetails jira)
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
					var jiraApi = _jiraMonitor.GetJiraApiForKey(jiraDetails);
					// Run upload in the background
					await PleaseWaitWindow.CreateAndShowAsync(Text, JiraLanguage.CommunicationWait, async (progress, pleaseWaitToken) =>
					{
						var httpBehaviour = HttpBehaviour.Current.Clone();
						// Use UploadProgress
						httpBehaviour.UploadProgress = (percent) =>
						{
							UiContext.RunOn(() => progress.Report((int)(percent * 100)));
						};
						httpBehaviour.MakeCurrent();
						using (var stream = new MemoryStream())
						{
							ImageOutput.SaveToStream(capture, stream, outputSettings);
							stream.Position = 0;
							using (var streamContent = new StreamContent(stream))
							{
								streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/" + outputSettings.Format);
								var attachment = await jiraApi.AttachAsync(jiraDetails.JiraKey, streamContent, filename, "image/" + outputSettings.Format, pleaseWaitToken);
								return attachment[0].ContentUri;
							}
						}
					}, token);

					Log.Debug("Uploaded to Jira.");
					returnValue.ImageLocation = jiraApi.JiraBaseUri.AppendSegments("browse", jiraDetails.JiraKey);
				}
				catch (TaskCanceledException tcEx)
				{
					returnValue.Text = string.Format(JiraLanguage.UploadFailure, JiraDesignation);
					returnValue.NotificationType = NotificationTypes.Cancel;
					returnValue.ErrorText = tcEx.Message;
					Log.Information(tcEx.Message);
				}
				catch (Exception e)
				{
					returnValue.Text = string.Format(JiraLanguage.UploadFailure, JiraDesignation);
					returnValue.NotificationType = NotificationTypes.Fail;
					returnValue.ErrorText = e.Message;
					Log.Warning(e, "Upload to JIRA gave an exception");
				}
			}
			return returnValue;
		}
	}
}