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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Interfaces.Plugin;
using Dapplo.Log.Facade;

namespace Greenshot.Addon.ModiOcr
{
	[Destination(OcrDesignation), PartNotDiscoverable]
	public sealed class OcrDestination : AbstractDestination
	{
		private const string OcrDesignation = "Ocr";
		private static readonly LogSource Log = new LogSource();
		private const int MinWidth = 130;
		private const int MinHeight = 130;
		private static readonly string OcrCommand = Path.Combine(Path.GetDirectoryName(typeof(OcrPlugin).Assembly.Location), "ModiOcrCommand.exe");
		private static readonly BitmapSource OcrIcon;

		static OcrDestination()
		{
			string exePath = PluginUtils.GetExePath("MSPVIEW.EXE");
			if (exePath != null && File.Exists(exePath))
			{
				OcrIcon = PluginUtils.GetCachedExeIcon(exePath, 0).ToBitmapSource();
			}
		}

		[Import]
		private IOcrConfiguration OcrConfiguration
		{
			get;
			set;
		}

		/// <summary>
		/// Setup
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();
			Designation = OcrDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, token);
			Text = OcrDesignation;
			Icon = OcrIcon;
		}

		private async Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = OcrDesignation,
				SourceType = SourceTypes.Destination,
				Text = OcrDesignation
			};
			try
			{
				await DoOcrAsync(capture, token);
			}
			catch (TaskCanceledException tcEx)
			{
				returnValue.Text = "Scan cancelled.";
                returnValue.NotificationType = NotificationTypes.Cancel;
				returnValue.ErrorText = tcEx.Message;
				Log.Info().WriteLine(tcEx.Message);
			}
			catch (Exception e)
			{
				returnValue.Text = "Scan failed.";
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				Log.Warn().WriteLine(e, "OCR failed");
			}
			return returnValue;
        }

		/// <summary>
		/// Handling of the CaptureTaken "event" from the ICaptureHost
		/// We do the OCR here!
		/// </summary>
		/// <param name="capture">Has the Image</param>
		/// <param name="token"></param>
		private async Task<string> DoOcrAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var outputSettings = new SurfaceOutputSettings(OutputFormat.bmp, 0, true)
			{
				ReduceColors = true,
				// We only want the background
				SaveBackgroundOnly = true
			};
			// Force Grayscale output
			outputSettings.Effects.Add(new GrayscaleEffect());

			// Also we need to check the size, resize if needed to 130x130 this is the minimum
			if (capture.Image.Width < MinWidth || capture.Image.Height < MinHeight)
			{
				int addedWidth = MinWidth - capture.Image.Width;
				if (addedWidth < 0)
				{
					addedWidth = 0;
				}
				int addedHeight = MinHeight - capture.Image.Height;
				if (addedHeight < 0)
				{
					addedHeight = 0;
				}
				IEffect effect = new ResizeCanvasEffect(addedWidth / 2, addedWidth / 2, addedHeight / 2, addedHeight / 2);
				outputSettings.Effects.Add(effect);
			}
			string filePath = ImageOutput.SaveToTmpFile(capture, outputSettings, null);

			Log.Debug().WriteLine("Saved tmp file to: {0}", filePath);

			string text = "";
			try
			{
				var processStartInfo = new ProcessStartInfo(OcrCommand, "\"" + filePath + "\" " + OcrConfiguration.Language + " " + OcrConfiguration.Orientimage + " " + OcrConfiguration.StraightenImage)
				{
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					UseShellExecute = false
				};
				using (Process process = Process.Start(processStartInfo))
				{
					if (process != null)
					{
						await process.WaitForExitAsync(token);
						if (process.ExitCode == 0)
						{
							text = process.StandardOutput.ReadToEnd();
						}
					}
				}
			}
			catch (Exception e)
			{
				Log.Error().WriteLine("Error while calling Microsoft Office Document Imaging (MODI) to OCR: ", e);
			}
			finally
			{
				if (File.Exists(filePath))
				{
					Log.Debug().WriteLine("Cleaning up tmp file: {0}", filePath);
					File.Delete(filePath);
				}
			}

			text = text.Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				Log.Info().WriteLine("No text returned");
				return null;
			}

			try
			{
				Log.Debug().WriteLine("Pasting OCR Text to Clipboard: {0}", text);
				ClipboardHelper.SetClipboardData(text);
			}
			catch (Exception e)
			{
				Log.Error().WriteLine(e, "Problem pasting text to clipboard: ");
			}
			return text;
		}
	}
}