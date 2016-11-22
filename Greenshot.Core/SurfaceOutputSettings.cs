//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System.Collections.Generic;
using Greenshot.Core.Configuration;
using Greenshot.Core.Gfx;

#endregion

namespace Greenshot.Core
{
	public class SurfaceOutputSettings
	{
		private bool _disableReduceColors;
		private bool _reduceColors;

		public SurfaceOutputSettings(IOutputConfiguration outputConfiguration = null)
		{
			_disableReduceColors = false;
			if (outputConfiguration != null)
			{
				Format = outputConfiguration.OutputFileFormat;
				JPGQuality = outputConfiguration.OutputFileJpegQuality;
				ReduceColors = outputConfiguration.OutputFileReduceColors;
			}
		}

		public SurfaceOutputSettings(OutputFormat format, IOutputConfiguration outputConfiguration = null) : this(outputConfiguration)
		{
			Format = format;
		}

		public SurfaceOutputSettings(OutputFormat format, int quality, IOutputConfiguration outputConfiguration = null) : this(format, outputConfiguration)
		{
			JPGQuality = quality;
		}

		public SurfaceOutputSettings(OutputFormat format, int quality, bool reduceColors) : this(format, quality)
		{
			ReduceColors = reduceColors;
		}

		/// <summary>
		///     Disable the reduce colors option, this overrules the enabling
		/// </summary>
		public bool DisableReduceColors
		{
			get { return _disableReduceColors; }
			set
			{
				// Quantizing os needed when output format is gif as this has only 256 colors!
				if (!OutputFormat.gif.Equals(Format))
				{
					_disableReduceColors = value;
				}
			}
		}

		public List<IEffect> Effects { get; } = new List<IEffect>();

		public OutputFormat Format { get; set; } = OutputFormat.png;

		public int JPGQuality { get; set; } = 80;

		public bool ReduceColors
		{
			get
			{
				// Fix for Bug #3468436, force quantizing when output format is gif as this has only 256 colors!
				if (OutputFormat.gif.Equals(Format))
				{
					return true;
				}
				return _reduceColors;
			}
			set { _reduceColors = value; }
		}

		public bool SaveBackgroundOnly { get; set; }

		public SurfaceOutputSettings PreventGreenshotFormat()
		{
			if (Format == OutputFormat.greenshot)
			{
				Format = OutputFormat.png;
			}
			return this;
		}
	}
}