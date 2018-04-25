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

using System.Diagnostics.CodeAnalysis;

#endregion

namespace Greenshot.Configuration
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public enum LangKey
	{
		contextmenu_capturefullscreen_all,
		contextmenu_capturefullscreen_left,
		contextmenu_capturefullscreen_top,
		contextmenu_capturefullscreen_right,
		contextmenu_capturefullscreen_bottom,
		contextmenu_captureie,
		editor_clipboardfailed,
		editor_email,
		error,
		error_openfile,
		error_openlink,
		print_error,
		settings_destination,
		settings_destination_fileas,
		settings_destination_printer,
		settings_filenamepattern,
		settings_message_filenamepattern,
		settings_printoptions,
		settings_tooltip_filenamepattern,
		settings_tooltip_language,
		settings_tooltip_primaryimageformat,
		settings_tooltip_storagelocation,
		settings_visualization,
		settings_window_capture_mode,
		tooltip_firststart,
		warning,
		warning_hotkeys,
		wait_ie_capture,
		update_found
	}
}