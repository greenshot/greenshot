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

using Dapplo.Config.Ini;
using Dapplo.LogFacade;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Addon.ExternalCommand
{
	public static class ProcessStarter
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly Regex UriRegexp = new Regex(@"(file|ftp|gopher|https?|ldap|mailto|net\.pipe|net\.tcp|news|nntp|telnet|uuid):((((?:\/\/)?)(?:[\-;:&=\+\$,\w]+@)?[A-Za-z0-9\.\-]+|(?:www\.|[\-;:&=\+\$,\w]+@)[A-Za-z0-9\.\-]+)((?:\/[\+~%\/\.\w\-_]*)?\??(?:[\-\+=&;%@\.\w_]*)#?(?:[\.\!\/\\\w]*))?)");
		private static readonly IExternalCommandConfiguration ExternalCommandConfiguration = IniConfig.Current.Get<IExternalCommandConfiguration>();


		/// <summary>
		/// Wrapper method for the background and normal call, this does all the logic:
		/// Call the external command, parse for URI, place to clipboard and set the export information
		/// </summary>
		/// <param name="commandSettings">CommandSettings</param>
		/// <param name="notification">Notification</param>
		/// <param name="imageFilename"></param>
		/// <param name="token">CancellationToken</param>
		internal static async Task CallExternalCommandAsync(CommandSettings commandSettings, Notification notification, string imageFilename, CancellationToken token = default(CancellationToken))
		{
			try
			{
				var result = await CallExternalCommandAsync(commandSettings, imageFilename, token);
				if (commandSettings.RunInbackground)
				{
					notification.NotificationType = NotificationTypes.Success;
				}
				else if (result.ExitCode == 0)
				{
					notification.NotificationType = NotificationTypes.Success;
					if (!string.IsNullOrEmpty(result.StandardOutput))
					{
						MatchCollection uriMatches = UriRegexp.Matches(result.StandardOutput);
						// Place output on the clipboard before the URI, so if one is found this overwrites
						if (ExternalCommandConfiguration.OutputToClipboard)
						{
							ClipboardHelper.SetClipboardData(result.StandardOutput);
						}
						if (uriMatches.Count >= 0)
						{
							notification.ImageLocation = new Uri(uriMatches[0].Groups[1].Value);
							Log.Info().WriteLine("Got URI : {0} ", notification.ImageLocation);
							if (ExternalCommandConfiguration.UriToClipboard)
							{
								ClipboardHelper.SetClipboardData(notification.ImageLocation);
							}
						}
					}
				}
				else
				{
					Log.Warn().WriteLine("Error calling external command: {0} ", result.StandardError);
					notification.NotificationType = NotificationTypes.Fail;
					notification.ErrorText = result.StandardError;
				}
			}
			catch (Exception ex)
			{
				Log.Warn().WriteLine("Error calling external command: {0} ", ex.Message);
				notification.NotificationType = NotificationTypes.Fail;
				notification.ErrorText = ex.Message;
			}
		}

		/// <summary>
		/// Wrapper to retry with a runas
		/// </summary>
		/// <param name="commando"></param>
		/// <param name="fullPath"></param>
		/// <param name="runInBackground"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		internal static async Task<ProcessResult> CallExternalCommandAsync(CommandSettings commandSettings, string fullPath, CancellationToken token = default(CancellationToken))
		{
			Win32Exception w32Ex;
			try
			{
				return await CallExternalCommandAsync(commandSettings, fullPath, null, token);
			}
			catch (Win32Exception ex)
			{
				// Retry later
				w32Ex = ex;
			}
			catch (Exception ex)
			{
				ex.Data.Add("commandline", commandSettings.Commandline);
				ex.Data.Add("arguments", commandSettings.Arguments);
				throw;
			}
			try
			{
				return await CallExternalCommandAsync(commandSettings, fullPath, "runas", token);
			}
			catch
			{
				w32Ex.Data.Add("commandline", commandSettings.Commandline);
				w32Ex.Data.Add("arguments", commandSettings.Arguments);
				throw;
			}
		}

		/// <summary>
		/// The actual executing code for the external command
		/// </summary>
		/// <param name="commando"></param>
		/// <param name="fullPath"></param>
		/// <param name="verb"></param>
		/// <param name="runInBackground"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		internal static async Task<ProcessResult> CallExternalCommandAsync(CommandSettings commandSettings, string fullPath, string verb, CancellationToken token = default(CancellationToken))
		{
			var result = new ProcessResult
			{
				ExitCode = -1
			};
			if (!string.IsNullOrEmpty(commandSettings.Commandline))
			{
				using (var process = new Process())
				{
					process.StartInfo.FileName = commandSettings.Commandline;
					process.StartInfo.Arguments = FormatArguments(commandSettings.Arguments, fullPath);
					process.StartInfo.UseShellExecute = false;
					if (ExternalCommandConfiguration.RedirectStandardOutput)
					{
						process.StartInfo.RedirectStandardOutput = true;
					}
					if (ExternalCommandConfiguration.RedirectStandardError)
					{
						process.StartInfo.RedirectStandardError = true;
					}
					if (verb != null)
					{
						process.StartInfo.Verb = verb;
					}
					Log.Info().WriteLine("Starting : {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
					process.Start();
					var processTask = Task.Run(async () =>
					{
						await process.WaitForExitAsync(token).ConfigureAwait(false);
						if (ExternalCommandConfiguration.RedirectStandardOutput)
						{
							var output = process.StandardOutput.ReadToEnd();
							if (ExternalCommandConfiguration.ShowStandardOutputInLog && output.Trim().Length > 0)
							{
								result.StandardOutput = output;
								Log.Info().WriteLine("Output:\n{0}", output);
							}
						}
						if (ExternalCommandConfiguration.RedirectStandardError)
						{
							var standardError = process.StandardError.ReadToEnd();
							if (standardError.Trim().Length > 0)
							{
								result.StandardError = standardError;
								Log.Warn().WriteLine("Error:\n{0}", standardError);
							}
						}
						Log.Info().WriteLine("Finished : {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
						return process.ExitCode;
					}, token).ConfigureAwait(false);
					if (!commandSettings.RunInbackground)
					{
						await processTask;
						result.ExitCode = process.ExitCode;
					}
					return result;
				}
			}
			return result;
		}

		public static string FormatArguments(string arguments, string fullpath)
		{
			return string.Format(arguments, fullpath);
		}
	}
}
