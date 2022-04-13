/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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

        private static readonly NativeSize DefaultSize = new(6, 6);

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
        /// Test if the point is inside the adorner
        /// </summary>
        /// <param name="point">NativePoint</param>
        /// <returns>bool</returns>
        public virtual bool HitTest(NativePoint point)
        {
            NativeRect hitBounds = Bounds.Inflate(3, 3);
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

        /// <summary>
        /// Draw the adorner
        /// </summary>
        /// <param name="paintEventArgs">PaintEventArgs</param>
        public virtual void Paint(PaintEventArgs paintEventArgs)
        {
            Graphics targetGraphics = paintEventArgs.Graphics;

            var bounds = BoundsOnSurface;
            GraphicsState state = targetGraphics.Save();

            targetGraphics.CompositingMode = CompositingMode.SourceCopy;

            try
            {
                using var fillBrush = new SolidBrush(FillColor);
                targetGraphics.FillRectangle(fillBrush, bounds);
                using var lineBrush = new SolidBrush(OutlineColor);
                using var pen = new Pen(lineBrush);
                targetGraphics.DrawRectangle(pen, bounds);
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