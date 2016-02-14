/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using Dapplo.Config.Ini;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;

namespace Greenshot.Addon.Interfaces.Plugin
{
	public class SurfaceOutputSettings
	{
		private static readonly ICoreConfiguration conf = IniConfig.Current.Get<ICoreConfiguration>();
		private bool reduceColors;
		private bool disableReduceColors;
		private List<IEffect> effects = new List<IEffect>();

		public SurfaceOutputSettings()
		{
			disableReduceColors = false;
			Format = conf.OutputFileFormat;
			JPGQuality = conf.OutputFileJpegQuality;
			ReduceColors = conf.OutputFileReduceColors;
		}

		public SurfaceOutputSettings(OutputFormat format) : this()
		{
			Format = format;
		}

		public SurfaceOutputSettings(OutputFormat format, int quality) : this(format)
		{
			JPGQuality = quality;
		}

		public SurfaceOutputSettings(OutputFormat format, int quality, bool reduceColors) : this(format, quality)
		{
			ReduceColors = reduceColors;
		}

		public SurfaceOutputSettings PreventGreenshotFormat()
		{
			if (Format == OutputFormat.greenshot)
			{
				Format = OutputFormat.png;
			}
			return this;
		}

		public OutputFormat Format
		{
			get;
			set;
		}

		public int JPGQuality
		{
			get;
			set;
		}

		public bool SaveBackgroundOnly
		{
			get;
			set;
		}

		public List<IEffect> Effects
		{
			get
			{
				return effects;
			}
		}

		public bool ReduceColors
		{
			get
			{
				// Fix for Bug #3468436, force quantizing when output format is gif as this has only 256 colors!
				if (OutputFormat.gif.Equals(Format))
				{
					return true;
				}
				return reduceColors;
			}
			set
			{
				reduceColors = value;
			}
		}

		/// <summary>
		/// Disable the reduce colors option, this overrules the enabling
		/// </summary>
		public bool DisableReduceColors
		{
			get
			{
				return disableReduceColors;
			}
			set
			{
				// Quantizing os needed when output format is gif as this has only 256 colors!
				if (!OutputFormat.gif.Equals(Format))
				{
					disableReduceColors = value;
				}
			}
		}
	}
}
