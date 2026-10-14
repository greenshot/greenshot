/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Greenshot.Editor.Helpers
{
    /// <summary>
    /// What Copy (Edit > Copy, Ctrl+C) puts on the clipboard in the editor
    /// </summary>
    public enum EditorCopyTarget
    {
        /// <summary>
        /// The selected elements, in the format which only the editor can paste
        /// </summary>
        SelectedElements,

        /// <summary>
        /// The image of the editor, including the elements, like the clipboard destination
        /// </summary>
        Image
    }

    /// <summary>
    /// The decisions for the Copy command of the editor, apart from the form so they can be tested.
    /// </summary>
    public static class EditorCopyHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();

        /// <summary>
        /// Copy works on the selected elements, when there are none the image is copied (like the Copy of other image editors).
        /// </summary>
        public static EditorCopyTarget GetCopyTarget(bool hasSelectedElements) =>
            hasSelectedElements ? EditorCopyTarget.SelectedElements : EditorCopyTarget.Image;

        /// <summary>
        /// True when the control is for typing text: Ctrl+C belongs to it, to copy the selected text.
        /// </summary>
        public static bool IsTextInput(Control control) =>
            control switch
            {
                TextBoxBase => true,
                ComboBox comboBox => comboBox.DropDownStyle != ComboBoxStyle.DropDownList,
                _ => false
            };

        /// <summary>
        /// The control of this application which has the keyboard focus, null when there is none.
        /// The focus can be in the edit part of a combo box, that is not a control: its combo box is returned.
        /// </summary>
        public static Control GetFocusedControl()
        {
            var handle = GetFocus();
            return handle == IntPtr.Zero ? null : Control.FromChildHandle(handle);
        }
    }
}
