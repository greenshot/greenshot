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
using System.Drawing;
using Greenshot.Core.Enumerations;

#endregion

namespace Greenshot.Core.Configuration
{
	/// <summary>
	///     This interface represents all the capture settings
	/// </summary>
	public interface ICaptureConfiguration
	{
		[Description("Capture delay in millseconds.")]
		[DefaultValue(100)]
		int CaptureDelay { get; set; }

		[Description("Should the mouse be captured?")]
		[DefaultValue(true)]
		bool CaptureMousepointer { get; set; }

		[Description("Use interactive window selection to capture? (false=Capture active window)")]
		[DefaultValue(false)]
		bool CaptureWindowsInteractive { get; set; }

		[Description("The background color for a DWM window capture.")]
		Color DWMBackgroundColor { get; set; }

		[Description("Enable/disable IE capture")]
		[DefaultValue(true)]
		bool IECapture { get; set; }

		[Description("The capture mode used to capture IE (Screen, GDI).")]
		[DefaultValue(WindowCaptureMode.Screen)]
		WindowCaptureMode IECaptureMode { get; set; }

		[Description("Enable/disable IE field capture, very slow but will make it possible to annotate the fields of a capture in the editor.")]
		[DefaultValue(false)]
		bool IEFieldCapture { get; set; }

		[Description("The last used region, for reuse in the capture last region")]
		Rectangle LastCapturedRegion { get; set; }

		[Description("List of productnames for which DWM capturing is skipped (using fallback).")]
		[DefaultValue("Citrix ICA Client")]
		IList<string> NoDWMCaptureForProduct { get; set; }


		[Description("List of productnames for which GDI capturing is skipped (using fallback).")]
		[DefaultValue("IntelliJ IDEA")]
		IList<string> NoGDICaptureForProduct { get; set; }

		[Description("The capture mode used to capture a screen. (Auto, FullScreen, Fixed)")]
		[DefaultValue(ScreenCaptureMode.Auto)]
		ScreenCaptureMode ScreenCaptureMode { get; set; }

		[Description("The screen number to capture when using ScreenCaptureMode Fixed.")]
		[DefaultValue(1)]
		int ScreenToCapture { get; set; }

		[Description("Enable/disable capture all children, very slow but will make it possible to use this information in the editor.")]
		[DefaultValue(false)]
		bool WindowCaptureAllChildLocations { get; set; }

		[Description("The capture mode used to capture a Window (Screen, GDI, Aero, AeroTransparent, Auto).")]
		[DefaultValue(WindowCaptureMode.Auto)]
		WindowCaptureMode WindowCaptureMode { get; set; }

		[Description("Remove the corners from a window capture")]
		[DefaultValue(true)]
		bool WindowCaptureRemoveCorners { get; set; }

		[Description("Comma separated list of Window-Classes which need to be checked for a IE instance!")]
		[DefaultValue("AfxFrameOrView70,IMWindowClass")]
		IList<string> WindowClassesToCheckForIE { get; set; }

		[Description("The cutshape which is used to remove the window corners, is mirrorred for all corners")]
		[DefaultValue("5,3,2,1,1")]
		IList<int> WindowCornerCutShape { get; set; }
	}
}