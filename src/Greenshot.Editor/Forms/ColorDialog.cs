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
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Interop;

namespace Greenshot.Editor.Forms
{
    /// <summary>
    /// Backward-compatible facade for ColorPickerWindow to support legacy WinForms and WPF callers seamlessly.
    /// </summary>
    public class ColorDialog : IDisposable
    {
        private static ColorDialog _instance;

        public static ColorDialog GetInstance() => _instance ??= new ColorDialog();

        public Color Color { get; set; } = Color.Black;

        public DialogResult DialogResult { get; set; } = DialogResult.Cancel;

        public ColorDialog()
        {
            _instance = this;
        }

        public DialogResult ShowDialog()
        {
            return ShowDialog((System.Windows.Forms.IWin32Window)null);
        }

        public DialogResult ShowDialog(System.Windows.Forms.IWin32Window owner)
        {
            var window = new ColorPickerWindow
            {
                SelectedColor = Color
            };

            if (owner != null)
            {
                new WindowInteropHelper(window)
                {
                    Owner = owner.Handle
                };
            }

            bool? result = window.ShowDialog();
            DialogResult = result == true ? DialogResult.OK : DialogResult.Cancel;

            if (result == true)
            {
                Color = window.SelectedColor;
            }

            return DialogResult;
        }

        public DialogResult ShowDialog(System.Windows.Window owner)
        {
            var window = new ColorPickerWindow
            {
                SelectedColor = Color
            };

            if (owner != null)
            {
                window.Owner = owner;
            }

            bool? result = window.ShowDialog();
            DialogResult = result == true ? DialogResult.OK : DialogResult.Cancel;

            if (result == true)
            {
                Color = window.SelectedColor;
            }

            return DialogResult;
        }

        public void Dispose()
        {
        }
    }
}
