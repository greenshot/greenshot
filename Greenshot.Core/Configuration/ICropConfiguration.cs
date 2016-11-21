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

using System.ComponentModel;
using System.Drawing;
using Dapplo.Config.Ini;

#endregion

namespace Greenshot.Core.Configuration
{
	/// <summary>
	///     This interface represents all the UI settings
	/// </summary>
	public interface ICropConfiguration : IIniSubSection
	{
		[Description("Color of the area selection")]
		Color CropAreaColor { get; set; }

		[Description("Color of the area selection lines")]
		Color CropAreaLinesColor { get; set; }

		[Description("Sets if the zoomer is enabled")]
		[DefaultValue(true)]
		bool ZoomerEnabled { get; set; }

		[Description("Specify the transparency for the zoomer, from 0-1 (where 1 is no transparency and 0 is complete transparent. An usefull setting would be 0.7)")]
		[DefaultValue(1)]
		float ZoomerOpacity { get; set; }
	}
}