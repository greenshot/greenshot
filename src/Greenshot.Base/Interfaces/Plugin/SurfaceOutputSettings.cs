/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Effects;
using Greenshot.Base.IniFile;

namespace Greenshot.Base.Interfaces.Plugin
{
    public class SurfaceOutputSettings
    {
        private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
        private bool _reduceColors;
        private bool _disableReduceColors;

        public SurfaceOutputSettings()
        {
            _disableReduceColors = false;
            Format = CoreConfig.OutputFileFormat;
            JPGQuality = CoreConfig.OutputFileJpegQuality;
            ReduceColors = CoreConfig.OutputFileReduceColors;
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

        /// <summary>
        /// BUG-2056 reported a logical issue, using greenshot format as the default causes issues with the external commands.
        /// </summary>
        /// <returns>this for fluent API usage</returns>
        public SurfaceOutputSettings PreventGreenshotFormat()
        {
            // If OutputFormat is Greenshot, use PNG instead.
            if (Format == OutputFormat.greenshot)
            {
                Format = OutputFormat.png;
            }

            return this;
        }

        public OutputFormat Format { get; set; }

        public int JPGQuality { get; set; }

        public bool SaveBackgroundOnly { get; set; }

        public List<IEffect> Effects { get; } = new List<IEffect>();

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

        /// <summary>
        /// Disable the reduce colors option, this overrules the enabling
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
    }
}