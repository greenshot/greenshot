/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Linq;
using System.Runtime.CompilerServices;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Editor;

namespace Greenshot.Editor.Panels.ViewModels
{
    /// <summary>
    /// Left, top, width and height of the one selected element. A change is one undo step, an element which keeps its aspect
    /// ratio (an emoji) changes the other side too. No UI toolkit here.
    /// </summary>
    internal sealed class SizeAndPositionViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IEditorPanelContext _context;

        public SizeAndPositionViewModel(IEditorPanelContext context)
        {
            _context = context;
            _context.SelectionChanged += OnChanged;
            _context.ElementsChanged += OnChanged;
            _context.CurrentSurfaceChanged += OnChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private IDrawableContainer Element
        {
            get
            {
                var selected = _context.CurrentSurface?.SelectedElements;
                return selected?.Count == 1 ? selected.First() : null;
            }
        }

        public int Left
        {
            get => Element?.Left ?? 0;
            set => Apply(element => new NativeRectFloat(value, element.Top, element.Width, element.Height));
        }

        public int Top
        {
            get => Element?.Top ?? 0;
            set => Apply(element => new NativeRectFloat(element.Left, value, element.Width, element.Height));
        }

        public int Width
        {
            get => Element?.Width ?? 0;
            set => Apply(element => new NativeRectFloat(element.Left, element.Top, value, KeepsAspectRatio(element) ? Scale(element.Height, value, element.Width) : element.Height));
        }

        public int Height
        {
            get => Element?.Height ?? 0;
            set => Apply(element => new NativeRectFloat(element.Left, element.Top, KeepsAspectRatio(element) ? Scale(element.Width, value, element.Height) : element.Width, value));
        }

        private static bool KeepsAspectRatio(IDrawableContainer element)
            => element is IHaveScaleOptions scalable && (scalable.GetScaleOptions() & ScaleOptions.Rational) == ScaleOptions.Rational;

        /// <summary>
        /// The other side for the new length, with the same aspect ratio
        /// </summary>
        internal static int Scale(int otherLength, int newLength, int oldLength)
            => oldLength == 0 ? otherLength : (int)Math.Round((double)otherLength * newLength / oldLength);

        private void Apply(Func<IDrawableContainer, NativeRectFloat> newBounds)
        {
            var element = Element;
            var surface = _context.CurrentSurface;
            if (element == null || surface == null)
            {
                return;
            }

            var bounds = newBounds(element);
            if (bounds.Left == element.Left && bounds.Top == element.Top && bounds.Width == element.Width && bounds.Height == element.Height)
            {
                return;
            }

            element.MakeBoundsChangeUndoable(false);
            element.Invalidate();
            element.ApplyBounds(bounds);
            element.Invalidate();
            surface.Modified = true;
            Refresh();
        }

        private void OnChanged(object sender, EventArgs e) => Refresh();

        private void Refresh()
        {
            OnPropertyChanged(nameof(Left));
            OnPropertyChanged(nameof(Top));
            OnPropertyChanged(nameof(Width));
            OnPropertyChanged(nameof(Height));
        }

        public void Dispose()
        {
            _context.SelectionChanged -= OnChanged;
            _context.ElementsChanged -= OnChanged;
            _context.CurrentSurfaceChanged -= OnChanged;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
