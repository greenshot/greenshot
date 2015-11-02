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

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GreenshotOfficePlugin.OfficeExport;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotOfficePlugin.Destinations
{
	/// <summary>
	/// Description of PowerpointDestination.
	/// </summary>
	public class PowerpointLegacyDestination : AbstractLegacyDestination
	{
		private const int ICON_APPLICATION = 0;
		private const int ICON_PRESENTATION = 1;

		private static readonly string ExePath;
		private readonly string _presentationName;

		static PowerpointLegacyDestination()
		{
			ExePath = PluginUtils.GetExePath("POWERPNT.EXE");
			if (ExePath != null && File.Exists(ExePath))
			{
				WindowDetails.AddProcessToExcludeFromFreeze("powerpnt");
			}
			else
			{
				ExePath = null;
			}
		}

		public PowerpointLegacyDestination()
		{
		}

		public PowerpointLegacyDestination(string presentationName)
		{
			_presentationName = presentationName;
		}

		public override string Designation
		{
			get
			{
				return "Powerpoint";
			}
		}

		public override string Description
		{
			get
			{
				if (_presentationName == null)
				{
					return "Microsoft Powerpoint";
				}
				return _presentationName;
			}
		}

		public override int Priority
		{
			get
			{
				return 4;
			}
		}

		public override bool IsDynamic
		{
			get
			{
				return true;
			}
		}

		public override bool IsActive
		{
			get
			{
				return base.IsActive && ExePath != null;
			}
		}

		public override Image DisplayIcon
		{
			get
			{
				if (!string.IsNullOrEmpty(_presentationName))
				{
					return PluginUtils.GetCachedExeIcon(ExePath, ICON_PRESENTATION);
				}

				return PluginUtils.GetCachedExeIcon(ExePath, ICON_APPLICATION);
			}
		}

		public override IEnumerable<ILegacyDestination> DynamicDestinations()
		{
			foreach (string presentationName in PowerpointExporter.GetPowerpointPresentations())
			{
				yield return new PowerpointLegacyDestination(presentationName);
			}
		}

		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var exportInformation = new ExportInformation
			{
				DestinationDesignation = Designation, DestinationDescription = Description
			};
			string tmpFile = capture.CaptureDetails.Filename;
			Size imageSize = Size.Empty;
			if (tmpFile == null || capture.Modified || !Regex.IsMatch(tmpFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
			{
				tmpFile = ImageOutput.SaveNamedTmpFile(capture, capture.CaptureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
				imageSize = capture.Image.Size;
			}
			if (_presentationName != null)
			{
				exportInformation.ExportMade = PowerpointExporter.ExportToPresentation(_presentationName, tmpFile, imageSize, capture.CaptureDetails.Title);
			}
			else
			{
				if (!manuallyInitiated)
				{
					bool initialValue = false;
					IList<ILegacyDestination> destinations = new List<ILegacyDestination>();
					foreach (var presentation in PowerpointExporter.GetPowerpointPresentations())
					{
						if (!initialValue)
						{
							destinations.Add(new PowerpointLegacyDestination());
							initialValue = true;
						}
						destinations.Add(new PowerpointLegacyDestination(presentation));
					}
					if (destinations.Count > 0)
					{
						// Return the ExportInformation from the picker without processing, as this indirectly comes from us self
						return await ShowPickerMenuAsync(false, capture, destinations, token).ConfigureAwait(false);
					}
				}
				else if (!exportInformation.ExportMade)
				{
					exportInformation.ExportMade = PowerpointExporter.InsertIntoNewPresentation(tmpFile, imageSize, capture.CaptureDetails.Title);
				}
			}
			return exportInformation;
		}
	}
}