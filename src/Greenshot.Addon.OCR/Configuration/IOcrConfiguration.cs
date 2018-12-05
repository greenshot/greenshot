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

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Dapplo.Config.Ini;

#endregion

namespace Greenshot.Addon.OCR.Configuration
{
    /// <summary>
    ///     OCR Configuration.
    /// </summary>
    [IniSection("OCR")]
    [Description("Greenshot OCR Plugin configuration")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
#pragma warning disable CS1591
    public interface IOcrConfiguration : IIniSection
	{
		[Description("Language for OCR")]
		[DefaultValue ("miLANG_ENGLISH")]
		string Language { get; set; }

	    [Description("Orient image?")]
	    [DefaultValue(true)]
		bool Orientimage { get; set; }

		[Description("Straighten image?")]
		[DefaultValue(true)]
		bool StraightenImage { get; set; }
	}
}