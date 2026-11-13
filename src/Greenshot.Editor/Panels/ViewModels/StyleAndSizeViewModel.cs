/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Panels.ViewModels
{
    /// <summary>
    /// Copy the style or the size of the selected element, apply them to the selected elements. No UI toolkit here.
    /// </summary>
    internal sealed class StyleAndSizeViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IEditorPanelContext _context;

        public StyleAndSizeViewModel(IEditorPanelContext context)
        {
            _context = context;
            _context.SelectionChanged += OnChanged;
            _context.CurrentSurfaceChanged += OnChanged;
            ElementStyleClipboard.Changed += OnChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool CanCopy => ElementStyleClipboard.CanCopy(_context.CurrentSurface?.SelectedElements);

        public bool CanApplyStyle => ElementStyleClipboard.CanApplyStyle(_context.CurrentSurface?.SelectedElements);

        public bool CanApplySize => ElementStyleClipboard.CanApplySize(_context.CurrentSurface?.SelectedElements);

        public void CopyStyle()
        {
            if (CanCopy)
            {
                ElementStyleClipboard.CopyStyle(_context.CurrentSurface.SelectedElements[0]);
            }
        }

        public void ApplyStyle()
        {
            var surface = _context.CurrentSurface;
            if (surface != null)
            {
                ElementStyleClipboard.ApplyStyle(surface, surface.SelectedElements);
            }
        }

        public void CopySize()
        {
            if (CanCopy)
            {
                ElementStyleClipboard.CopySize(_context.CurrentSurface.SelectedElements[0]);
            }
        }

        public void ApplySize()
        {
            var surface = _context.CurrentSurface;
            if (surface != null)
            {
                ElementStyleClipboard.ApplySize(surface, surface.SelectedElements);
            }
        }

        private void OnChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(CanCopy));
            OnPropertyChanged(nameof(CanApplyStyle));
            OnPropertyChanged(nameof(CanApplySize));
        }

        public void Dispose()
        {
            _context.SelectionChanged -= OnChanged;
            _context.CurrentSurfaceChanged -= OnChanged;
            ElementStyleClipboard.Changed -= OnChanged;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
