#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System.Collections.Generic;
using GreenshotPlugin.Core;
using GreenshotPlugin.Core.Enums;
using GreenshotPlugin.Effects;
using GreenshotPlugin.IniFile;

#endregion

namespace GreenshotPlugin.Interfaces.Plugin
{
	public class SurfaceOutputSettings
	{
		private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
		private bool _disableReduceColors;
		private bool _reduceColors;

		public SurfaceOutputSettings()
		{
			_disableReduceColors = false;
			Format = CoreConfig.OutputFileFormat;
			JPGQuality = CoreConfig.OutputFileJpegQuality;
			ReduceColors = CoreConfig.OutputFileReduceColors;
		}

		public SurfaceOutputSettings(OutputFormats format) : this()
		{
			Format = format;
		}

		public SurfaceOutputSettings(OutputFormats format, int quality) : this(format)
		{
			JPGQuality = quality;
		}

		public SurfaceOutputSettings(OutputFormats format, int quality, bool reduceColors) : this(format, quality)
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