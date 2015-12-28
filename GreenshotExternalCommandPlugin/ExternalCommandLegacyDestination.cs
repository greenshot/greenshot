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
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Config.Ini;
using GreenshotPlugin.Core;
using GreenshotPlugin.Extensions;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotExternalCommandPlugin
{
	/// <summary>
	/// Description of OCRDestination.
	/// </summary>
	public class ExternalCommandLegacyDestination : AbstractLegacyDestination
	{
		private static readonly Serilog.ILogger LOG = Serilog.Log.Logger.ForContext(typeof(ExternalCommandLegacyDestination));
		private static readonly Regex UriRegexp = new Regex(@"(file|ftp|gopher|https?|ldap|mailto|net\.pipe|net\.tcp|news|nntp|telnet|uuid):((((?:\/\/)?)(?:[\-;:&=\+\$,\w]+@)?[A-Za-z0-9\.\-]+|(?:www\.|[\-;:&=\+\$,\w]+@)[A-Za-z0-9\.\-]+)((?:\/[\+~%\/\.\w\-_]*)?\??(?:[\-\+=&;%@\.\w_]*)#?(?:[\.\!\/\\\w]*))?)");
		private static readonly IExternalCommandConfiguration ExternalCommandConfiguration = IniConfig.Current.Get<IExternalCommandConfiguration>();
		private readonly string _presetCommand;

		public ExternalCommandLegacyDestination(string commando)
		{
			_presetCommand = commando;
		}

		public override string Designation
		{
			get
			{
				return "External " + _presetCommand.Replace(',', '_');
			}
		}

		public override string Description
		{
			get
			{
				return _presetCommand;
			}
		}

		public override IEnumerable<ILegacyDestination> DynamicDestinations()
		{
			yield break;
		}

		public override Image DisplayIcon
		{
			get
			{
				return IconCache.IconForCommand(_presetCommand);
			}
		}

		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var exportInformation = new ExportInformation
			{
				DestinationDesignation = Designation, DestinationDescription = Description
			};
			var outputSettings = new SurfaceOutputSettings();

			if (_presetCommand != null)
			{
				if (!ExternalCommandConfiguration.RunInbackground.ContainsKey(_presetCommand))
				{
					ExternalCommandConfiguration.RunInbackground.Add(_presetCommand, true);
				}
				bool runInBackground = ExternalCommandConfiguration.RunInbackground[_presetCommand];
				string fullPath = capture.CaptureDetails.Filename;
				if (fullPath == null)
				{
					fullPath = ImageOutput.SaveNamedTmpFile(capture, capture.CaptureDetails, outputSettings);
				}

				await CallExternalCommandAsync(exportInformation, fullPath, runInBackground, token);
			}
			return exportInformation;
		}

		/// <summary>
		/// Wrapper method for the background and normal call, this does all the logic:
		/// Call the external command, parse for URI, place to clipboard and set the export information
		/// </summary>
		/// <param name="exportInformation"></param>
		/// <param name="fullPath"></param>
		/// <param name="runInBackground"></param>
		/// <param name="token"></param>
		private async Task CallExternalCommandAsync(ExportInformation exportInformation, string fullPath, bool runInBackground, CancellationToken token = default(CancellationToken))
		{
			try
			{
				var result = await CallExternalCommandAsync(_presetCommand, fullPath, runInBackground, token);
				if (runInBackground)
				{
					exportInformation.ExportMade = true;
				}
				else if (result.ExitCode == 0)
				{
					exportInformation.ExportMade = true;
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
							exportInformation.ExportedToUri = new Uri(uriMatches[0].Groups[1].Value);
							LOG.Information("Got URI : {0} ", exportInformation.ExportedToUri);
							if (ExternalCommandConfiguration.UriToClipboard)
							{
								ClipboardHelper.SetClipboardData(exportInformation.ExportedToUri);
							}
						}
					}
				}
				else
				{
					LOG.Warning("Error calling external command: {0} ", result.StandardError);
					exportInformation.ExportMade = false;
					exportInformation.ErrorMessage = result.StandardError;
				}
			}
			catch (Exception ex)
			{
				LOG.Warning("Error calling external command: {0} ", ex.Message);
				exportInformation.ExportMade = false;
				exportInformation.ErrorMessage = ex.Message;
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
		private async Task<ProcessResult> CallExternalCommandAsync(string commando, string fullPath, bool runInBackground, CancellationToken token = default(CancellationToken))
		{
			Win32Exception w32Ex;
			try
			{
				return await CallExternalCommandAsync(commando, fullPath, null, runInBackground, token);
			}
			catch (Win32Exception ex)
			{
				// Retry later
				w32Ex = ex;
			}
			catch (Exception ex)
			{
				ex.Data.Add("commandline", ExternalCommandConfiguration.Commandline[_presetCommand]);
				ex.Data.Add("arguments", ExternalCommandConfiguration.Argument[_presetCommand]);
				throw;
			}
			try
			{
				return await CallExternalCommandAsync(commando, fullPath, "runas", runInBackground, token);
			}
			catch
			{
				w32Ex.Data.Add("commandline", ExternalCommandConfiguration.Commandline[_presetCommand]);
				w32Ex.Data.Add("arguments", ExternalCommandConfiguration.Argument[_presetCommand]);
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
		private static async Task<ProcessResult> CallExternalCommandAsync(string commando, string fullPath, string verb, bool runInBackground, CancellationToken token = default(CancellationToken))
		{
			string commandline = ExternalCommandConfiguration.Commandline[commando];
			string arguments = ExternalCommandConfiguration.Argument[commando];
			var result = new ProcessResult
			{
				ExitCode = -1
			};
			if (!string.IsNullOrEmpty(commandline))
			{
				using (var process = new Process())
				{
					process.StartInfo.FileName = commandline;
					process.StartInfo.Arguments = FormatArguments(arguments, fullPath);
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
					LOG.Information("Starting : {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
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
								LOG.Information("Output:\n{0}", output);
							}
						}
						if (ExternalCommandConfiguration.RedirectStandardError)
						{
							var standardError = process.StandardError.ReadToEnd();
							if (standardError.Trim().Length > 0)
							{
								result.StandardError = standardError;
								LOG.Warning("Error:\n{0}", standardError);
							}
						}
						LOG.Information("Finished : {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
						return process.ExitCode;
					}, token).ConfigureAwait(false);
					if (!runInBackground)
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

	/// <summary>
	/// Used to transport exit code and the standard error & output
	/// </summary>
	internal class ProcessResult
	{
		public string StandardOutput
		{
			get;
			set;
		}

		public string StandardError
		{
			get;
			set;
		}

		public int ExitCode
		{
			get;
			set;
		}
	}
}