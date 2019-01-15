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

using System.Collections.Generic;
using Greenshot.Addons.Config.Impl;
using Greenshot.Addons.Core;
using Greenshot.Core.Enums;
using Greenshot.Gfx.Effects;

#endregion

namespace Greenshot.Addons.Interfaces.Plugin
{
    /// <summary>
    /// This contains the settings for outputting a surface
    /// </summary>
	public class SurfaceOutputSettings
	{
        private bool _disableReduceColors;
		private bool _reduceColors;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileConfiguration">IFileConfiguration</param>
		public SurfaceOutputSettings(IFileConfiguration fileConfiguration)
		{
			_disableReduceColors = false;
			Format = fileConfiguration?.OutputFileFormat ?? OutputFormats.png;
			JPGQuality = fileConfiguration?.OutputFileJpegQuality ?? 80;
			ReduceColors = fileConfiguration?.OutputFileReduceColors ?? false;
		}

		public SurfaceOutputSettings(IFileConfiguration fileConfiguration, OutputFormats format) : this(fileConfiguration)
		{
			Format = format;
		}

		public SurfaceOutputSettings(IFileConfiguration fileConfiguration, OutputFormats format, int quality) : this(fileConfiguration, format)
		{
			JPGQuality = quality;
		}

		public SurfaceOutputSettings(IFileConfiguration fileConfiguration, OutputFormats format, int quality, bool reduceColors) : this(fileConfiguration, format, quality)
		{
			ReduceColors = reduceColors;
		}

        public OutputFormats Format { get; set; }

		public int JPGQuality { get; set; }

		public bool SaveBackgroundOnly { get; set; }

		public List<IEffect> Effects { get; } = new List<IEffect>();

		public bool ReduceColors
		{
			get
			{
				// Fix for Bug #3468436, force quantizing when output format is gif as this has only 256 colors!
				if (OutputFormats.gif.Equals(Format))
				{
					return true;
				}
				return _reduceColors;
			}
			set { _reduceColors = value; }
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
				if (!OutputFormats.gif.Equals(Format))
				{
					_disableReduceColors = value;
				}
			}
		}

		/// <summary>
		///     BUG-2056 reported a logical issue, using greenshot format as the default causes issues with the external commands.
		/// </summary>
		/// <returns>this for fluent API usage</returns>
		public SurfaceOutputSettings PreventGreenshotFormat()
		{
			// If OutputFormats is Greenshot, use PNG instead.
			if (Format == OutputFormats.greenshot)
			{
				Format = OutputFormats.png;
			}
			return this;
		}
	}
}