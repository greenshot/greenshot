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

using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Drawing.Adorners;

namespace Greenshot.Editor.Drawing.Adorners
{
    public class AbstractAdorner : IAdorner
    {
        public virtual EditStatus EditStatus { get; protected set; } = EditStatus.IDLE;

        /// <summary>
        /// The size of an adorner at 96 DPI, in screen pixels
        /// </summary>
        internal const int DefaultSizeAt96Dpi = 6;

        private static readonly NativeSize DefaultSize = new(DefaultSizeAt96Dpi, DefaultSizeAt96Dpi);

        protected NativeSize Size
        {
            get;
            set;
        }

        public AbstractAdorner(IDrawableContainer owner)
        {
            Size = DpiCalculator.ScaleWithDpi(DefaultSize, owner?.Parent?.CurrentDpi ?? 96);
            Owner = owner;
        }

        /// <summary>
        /// Returns the cursor for when the mouse is over the adorner
        /// </summary>
        public virtual Cursor Cursor
        {
            get { return Cursors.SizeAll; }
        }

        public IDrawableContainer Owner { get; set; }

        /// <summary>
        /// Test if the point is inside the adorner, as it is drawn: in screen pixels, the same size at every zoom
        /// </summary>
        /// <param name="point">NativePoint in surface coordinates</param>
        /// <returns>bool</returns>
        public virtual bool HitTest(NativePoint point)
        {
            if (Owner?.Parent == null)
            {
                return false;
            }

            // A margin of half the adorner around it, so it is easy to grab
            NativeRect hitBounds = BoundsOnSurface.Inflate(Size.Width / 2, Size.Height / 2);
            return hitBounds.Contains(point);
        }

        /// <summary>
        /// Handle the mouse down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="mouseEventArgs"></param>
        public virtual void MouseDown(object sender, MouseEventArgs mouseEventArgs)
        {
        }

        /// <summary>
        /// Handle the mouse move
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="mouseEventArgs"></param>
        public virtual void MouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
        }

        /// <summary>
        /// Handle the mouse up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="mouseEventArgs"></param>
        public virtual void MouseUp(object sender, MouseEventArgs mouseEventArgs)
        {
            EditStatus = EditStatus.IDLE;
        }

        /// <summary>
        /// Return the location of the adorner
        /// </summary>
        public virtual NativePoint Location { get; set; }

        /// <summary>
        /// Return the bounds of the Adorner
        /// </summary>
        public virtual NativeRect Bounds
        {
            get
            {
                NativePoint location = Location;
                return new NativeRect(location.X - (Size.Width / 2), location.Y - (Size.Height / 2), Size.Width, Size.Height);
            }
        }

        /// <summary>
        /// Return the bounds of the Adorner as displayed on the parent Surface
        /// </summary>
        protected virtual NativeRect BoundsOnSurface
        {
            get
            {
                NativePoint displayLocation = Owner.Parent.ToSurfaceCoordinates(Location);
                return new NativeRect(displayLocation.X - Size.Width / 2, displayLocation.Y - Size.Height / 2, Size.Width, Size.Height);
            }
        }

        /// <summary>
        /// The adorner is active if the edit status is not idle or undrawn
        /// </summary>
        public virtual bool IsActive
        {
            get { return EditStatus != EditStatus.IDLE && EditStatus != EditStatus.UNDRAWN; }
        }

        /// <summary>
        /// Adjust UI elements to the supplied DPI settings
        /// </summary>
        /// <param name="dpi">uint</param>
        public void AdjustToDpi(int dpi)
        {
            Size = DpiCalculator.ScaleWithDpi(DefaultSize, dpi);
        }

        public Color OutlineColor { get; set; } = Color.White;
        public Color FillColor { get; set; } = Color.Black;

        // Every adorner is painted on each paint of the surface, they share a brush and a pen per color (a handful of colors).
        // Only used on the UI thread, which paints all editors.
        private static readonly Dictionary<int, SolidBrush> SharedBrushes = new Dictionary<int, SolidBrush>();
        private static readonly Dictionary<int, Pen> SharedPens = new Dictionary<int, Pen>();

        private static SolidBrush BrushFor(Color color)
        {
            int argb = color.ToArgb();
            if (!SharedBrushes.TryGetValue(argb, out var brush))
            {
                brush = new SolidBrush(color);
                SharedBrushes.Add(argb, brush);
            }

            return brush;
        }

        private static Pen PenFor(Color color)
        {
            int argb = color.ToArgb();
            if (!SharedPens.TryGetValue(argb, out var pen))
            {
                pen = new Pen(color);
                SharedPens.Add(argb, pen);
            }

            return pen;
        }

        /// <summary>
        /// Draw the adorner
        /// </summary>
        /// <param name="paintEventArgs">PaintEventArgs</param>
        public virtual void Paint(PaintEventArgs paintEventArgs)
        {
            Graphics targetGraphics = paintEventArgs.Graphics;

            GraphicsState state = targetGraphics.Save();

            targetGraphics.CompositingMode = CompositingMode.SourceCopy;

            try
            {
                var bounds = BoundsOnSurface;
                targetGraphics.FillRectangle(BrushFor(FillColor), bounds);
                targetGraphics.DrawRectangle(PenFor(OutlineColor), bounds);
            }
            catch
            {
                // Ignore, BUG-2065
            }

            targetGraphics.Restore(state);
        }

        /// <summary>
        /// We ignore the Transform, as the coordinates are directly bound to those of the owner
        /// </summary>
        /// <param name="matrix"></param>
        public virtual void Transform(Matrix matrix)
        {
        }

        /// <summary>
        /// This is to TAG the adorner so we know the type
        /// </summary>
        public string Tag { get; set; }
    }
}