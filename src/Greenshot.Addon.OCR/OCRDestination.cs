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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Dapplo.Log;
using Greenshot.Addon.OCR.Configuration;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Core.Enums;
using Greenshot.Gfx.Effects;

#endregion

namespace Greenshot.Addon.OCR
{
    /// <summary>
    ///     Description of OCRDestination.
    /// </summary>
    [Destination("OCR")]
    public class OcrDestination : AbstractDestination
	{
	    private static readonly LogSource Log = new LogSource();
	    private const int MinWidth = 130;
	    private const int MinHeight = 130;
        private readonly IOcrConfiguration _ocrConfiguration;
	    private readonly string _ocrCommand;

        public OcrDestination(
            IOcrConfiguration ocrConfiguration,
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage,
            ExportNotification exportNotification
            ) : base(coreConfiguration, greenshotLanguage)
        {
			_ocrConfiguration = ocrConfiguration;

		    var ocrDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		    if (ocrDirectory == null)
		    {
		        return;
		    }

		    _ocrCommand = Path.Combine(ocrDirectory, "greenshotocrcommand.exe");
		    IsActive = File.Exists(_ocrCommand) && HasModi();

		}

	    public override bool IsActive { get; }

	    public override string Description => "OCR";

		public override Bitmap GetDisplayIcon(double dpi)
		{
			var exePath = PluginUtils.GetExePath("MSPVIEW.EXE");
			if (exePath != null && File.Exists(exePath))
			{
				return PluginUtils.GetCachedExeIcon(exePath, 0, dpi > 100);
			}
			return null;
		}

	    protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description)
			{
				ExportMade = DoOcr(surface) != null
			};
			return exportInformation;
		}


        /// <summary>
        ///     Handling of the CaptureTaken "event" from the ICaptureHost
        ///     We do the OCR here!
        /// </summary>
        /// <param name="surface">Has the Image and the capture details</param>
        public string DoOcr(ISurface surface)
        {
            var outputSettings = new SurfaceOutputSettings(CoreConfiguration, OutputFormats.bmp, 0, true)
            {
                ReduceColors = true,
                SaveBackgroundOnly = true
            };
            // We only want the background
            // Force Grayscale output
            outputSettings.Effects.Add(new GrayscaleEffect());

            // Also we need to check the size, resize if needed to 130x130 this is the minimum
            if (surface.Screenshot.Width < MinWidth || surface.Screenshot.Height < MinHeight)
            {
                var addedWidth = MinWidth - surface.Screenshot.Width;
                if (addedWidth < 0)
                {
                    addedWidth = 0;
                }
                var addedHeight = MinHeight - surface.Screenshot.Height;
                if (addedHeight < 0)
                {
                    addedHeight = 0;
                }
                IEffect effect = new ResizeCanvasEffect(addedWidth / 2, addedWidth / 2, addedHeight / 2, addedHeight / 2);
                outputSettings.Effects.Add(effect);
            }
            var filePath = ImageOutput.SaveToTmpFile(surface, outputSettings, null);

            Log.Debug().WriteLine("Saved tmp file to: " + filePath);

            var text = "";
            try
            {
                var processStartInfo = new ProcessStartInfo(_ocrCommand, "\"" + filePath + "\" " + _ocrConfiguration.Language + " " + _ocrConfiguration.Orientimage + " " + _ocrConfiguration.StraightenImage)
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                using (var process = Process.Start(processStartInfo))
                {
                    if (process != null)
                    {
                        process.WaitForExit(30 * 1000);
                        if (process.ExitCode == 0)
                        {
                            text = process.StandardOutput.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error().WriteLine(e, "Error while calling Microsoft Office Document Imaging (MODI) to OCR: ");
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    Log.Debug().WriteLine("Cleaning up tmp file: " + filePath);
                    File.Delete(filePath);
                }
            }

            if (string.IsNullOrEmpty(text))
            {
                Log.Info().WriteLine("No text returned");
                return null;
            }

            // For for BUG-1884:
            text = text.Trim();

            try
            {
                Log.Debug().WriteLine("Pasting OCR Text to Clipboard: {0}", text);
                // Paste to Clipboard (the Plugin currently doesn't have access to the ClipboardHelper from Greenshot
                IDataObject ido = new DataObject();
                ido.SetData(DataFormats.Text, true, text);
                Clipboard.SetDataObject(ido, true);
            }
            catch (Exception e)
            {
                Log.Error().WriteLine(e, "Problem pasting text to clipboard: ");
            }
            return text;
        }

        private bool HasModi()
        {
            try
            {
                using (var process = Process.Start(_ocrCommand, "-c"))
                {
                    if (process != null)
                    {
                        process.WaitForExit();
                        return process.ExitCode == 0;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Debug().WriteLine("Error trying to initiate MODI: {0}", e.Message);
            }
            Log.Info().WriteLine("No Microsoft Office Document Imaging (MODI) found, disabling OCR");
            return false;
        }
    }
}