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
using System.ComponentModel;
using Dapplo.Ini;
using Greenshot.Core.Enumerations;

#endregion

namespace Greenshot.Core.Configuration
{
	/// <summary>
	///     This interface represents all the capture settings
	/// </summary>
	public interface IIECaptureConfiguration : IIniSubSection
	{
		[Description("Enable/disable IE capture")]
		[DefaultValue(true)]
		bool IECapture { get; set; }

		[Description("The capture mode used to capture IE (Screen, GDI).")]
		[DefaultValue(WindowCaptureMode.Screen)]
		WindowCaptureMode IECaptureMode { get; set; }

		[Description("Enable/disable IE field capture, very slow but will make it possible to annotate the fields of a capture in the editor.")]
		[DefaultValue(false)]
		bool IEFieldCapture { get; set; }

		[Description("Comma separated list of Window-Classes which need to be checked for a IE instance!")]
		[DefaultValue("AfxFrameOrView70,IMWindowClass")]
		IList<string> WindowClassesToCheckForIE { get; set; }
	}
}