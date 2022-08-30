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

using System.Diagnostics.CodeAnalysis;

namespace Greenshot.Editor.Configuration
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum LangKey
    {
        none,
        contextmenu_capturefullscreen_all,
        contextmenu_capturefullscreen_left,
        contextmenu_capturefullscreen_top,
        contextmenu_capturefullscreen_right,
        contextmenu_capturefullscreen_bottom,
        contextmenu_captureie,
        editor_autocrop_not_possible,
        editor_clipboardfailed,
        editor_close_on_save,
        editor_close_on_save_title,
        editor_copytoclipboard,
        editor_cuttoclipboard,
        editor_deleteelement,
        editor_downonelevel,
        editor_downtobottom,
        editor_duplicate,
        editor_email,
        editor_imagesaved,
        editor_title,
        editor_uponelevel,
        editor_uptotop,
        editor_undo,
        editor_redo,
        editor_resetsize,
        error,
        error_multipleinstances,
        error_openfile,
        error_openlink,
        error_save,
        error_save_invalid_chars,
        print_error,
        quicksettings_destination_file,
        settings_destination,
        settings_destination_clipboard,
        settings_destination_editor,
        settings_destination_fileas,
        settings_destination_printer,
        settings_destination_picker,
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