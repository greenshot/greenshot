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

namespace Greenshot.Base.Core.Enums
{
    /// <summary>
    /// This are the defined actions which can be initiated when the menu is clicked
    /// </summary>
    public enum ClickActions
    {
        DO_NOTHING,
        OPEN_LAST_IN_EXPLORER,
        OPEN_LAST_IN_EDITOR,
        OPEN_SETTINGS,
        SHOW_CONTEXT_MENU,
        CAPTURE_REGION,
        CAPTURE_SCREEN,
        CAPTURE_CLIPBOARD,
        CAPTURE_WINDOW,
        OPEN_EMPTY_EDITOR,
        OPEN_FILE_IN_EDITOR,
        OPEN_CLIPBOARD_IN_EDITOR
    }
}