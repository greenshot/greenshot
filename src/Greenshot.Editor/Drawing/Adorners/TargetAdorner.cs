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
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Editor.Drawing.Adorners
{
    /// <summary>
    /// This implements the special target "gripper", e.g. used for the Speech-Bubble tail
    /// </summary>
    public sealed class TargetAdorner : AbstractAdorner
    {
        public TargetAdorner(IDrawableContainer owner, Point location, Color? fillColor = null, Color? outlineColor = null) : base(owner)
        {
            Location = location;
            FillColor = fillColor ?? Color.Green;
            OutlineColor = outlineColor ?? Color.White;
        }

        /// <summary>
        /// Handle the mouse down
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="mouseEventArgs">MouseEventArgs</param>
        public override void MouseDown(object sender, MouseEventArgs mouseEventArgs)
        {
            EditStatus = EditStatus.MOVING;
        }

        /// <summary>
        /// Handle the mouse move
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="mouseEventArgs">MouseEventArgs</param>
        public override void MouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            if (EditStatus != EditStatus.MOVING)
            {
                return;
            }

            Owner.Invalidate();
            NativePoint newGripperLocation = new NativePoint(mouseEventArgs.X, mouseEventArgs.Y);
            NativeRect imageBounds = new NativeRect(0, 0, Owner.Parent.Image.Width, Owner.Parent.Image.Height);
            // Check if gripper inside the parent (surface), if not we need to move it inside
            // This was made for BUG-1682
            if (!imageBounds.Contains(newGripperLocation))
            {
                if (newGripperLocation.X > imageBounds.Right)
                {
                    newGripperLocation = newGripperLocation.ChangeX(imageBounds.Right - 5);
                }

                if (newGripperLocation.X < imageBounds.Left)
                {
                    newGripperLocation = newGripperLocation.ChangeX(imageBounds.Left);
                }

                if (newGripperLocation.Y > imageBounds.Bottom)
                {
                    newGripperLocation = newGripperLocation.ChangeY(imageBounds.Bottom - 5);
                }

                if (newGripperLocation.Y < imageBounds.Top)
                {
                    newGripperLocation = newGripperLocation.ChangeY(imageBounds.Top);
                }
            }

            Location = newGripperLocation;
            Owner.Invalidate();
        }

        /// <summary>
        /// Made sure this adorner is transformed
        /// </summary>
        /// <param name="matrix">Matrix</param>
        public override void Transform(Matrix matrix)
        {
            if (matrix == null)
            {
                return;
            }

            Point[] points = new Point[]
            {
                Location
            };
            matrix.TransformPoints(points);
            Location = points[0];
        }
    }
}