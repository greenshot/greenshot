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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;

namespace Greenshot.Editor.Drawing.Filters
{
    /// <summary>
    /// Graphical filter which can be added to DrawableContainer.
    /// Subclasses should fulfill INotifyPropertyChanged contract, i.e. call
    /// OnPropertyChanged whenever a public property has been changed.
    /// </summary>
    [Serializable]
    public abstract class AbstractFilter : AbstractFieldHolder, IFilter
    {
        [NonSerialized] private PropertyChangedEventHandler propertyChanged;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { propertyChanged += value; }
            remove { propertyChanged -= value; }
        }

        private bool invert;

        public bool Invert
        {
            get { return invert; }
            set
            {
                invert = value;
                OnPropertyChanged("Invert");
            }
        }

        protected DrawableContainer parent;

        public DrawableContainer Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public AbstractFilter(DrawableContainer parent)
        {
            this.parent = parent;
        }

        public DrawableContainer GetParent()
        {
            return parent;
        }

        /// <summary>
        /// Applies the filter to a single rectangle.
        /// By default, delegates to the multi-rectangle Apply overload.
        /// </summary>
        public virtual void Apply(Graphics graphics, Bitmap applyBitmap, NativeRect rect, RenderMode renderMode)
        {
            Apply(graphics, applyBitmap, new[] { rect }, renderMode);
        }

        /// <summary>
        /// Applies the filter to one or more rectangles with clipping and graphics state management.
        /// </summary>
        public virtual void Apply(Graphics graphics, Bitmap applyBitmap, IEnumerable<NativeRect> rects, RenderMode renderMode)
        {
            ApplyWithClipping(graphics, applyBitmap, rects, applyRect => ApplyFilter(graphics, applyBitmap, applyRect, renderMode));
        }

        /// <summary>
        /// Renders the filter effect onto the graphics surface within the applyRect bounds.
        /// When this method is called, clipping has already been configured on the graphics context
        /// (handling Invert exclusions or non-inverted inclusion path), and graphics state is restored automatically.
        /// Subclasses implementing area/image filters should override this method.
        /// </summary>
        protected virtual void ApplyFilter(Graphics graphics, Bitmap applyBitmap, NativeRect applyRect, RenderMode renderMode)
        {
        }

        /// <summary>
        /// Executes the given render action within a clipped region for the specified rectangles.
        /// Handles graphics state saving and restoring, bounding rectangle calculation,
        /// and clipping (including multi-rectangle inverted exclusion).
        /// </summary>
        protected void ApplyWithClipping(Graphics graphics, Bitmap applyBitmap, IEnumerable<NativeRect> rects, Action<NativeRect> renderAction)
        {
            if (graphics == null || applyBitmap == null || renderAction == null)
            {
                return;
            }

            var rectList = rects as IList<NativeRect> ?? rects?.ToList() ?? new List<NativeRect>();
            if (rectList.Count == 0)
            {
                return;
            }

            NativeRect applyRect;
            if (Invert)
            {
                applyRect = new NativeRect(0, 0, applyBitmap.Width, applyBitmap.Height);
            }
            else
            {
                applyRect = rectList.Aggregate(NativeRect.Empty, (current, r) => current.IsEmpty ? r : current.Union(r))
                                    .Intersect(new NativeRect(0, 0, applyBitmap.Width, applyBitmap.Height));
            }

            if (applyRect.Width <= 0 || applyRect.Height <= 0)
            {
                return;
            }

            GraphicsState state = graphics.Save();
            try
            {
                if (Invert)
                {
                    graphics.SetClip(applyRect);
                    foreach (var rect in rectList)
                    {
                        graphics.ExcludeClip(rect);
                    }
                }
                else
                {
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        foreach (var rect in rectList)
                        {
                            path.AddRectangle(rect);
                        }
                        graphics.SetClip(path);
                    }
                }

                renderAction(applyRect);
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}