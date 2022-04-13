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
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Helpers;

namespace Greenshot.Editor.Drawing.Adorners
{
    /// <summary>
    /// This is the adorner for the line based containers
    /// </summary>
    public class MoveAdorner : AbstractAdorner
    {
        private NativeRect _boundsBeforeResize = NativeRect.Empty;
        private NativeRectFloat _boundsAfterResize = NativeRectFloat.Empty;

        public Positions Position { get; private set; }

        public MoveAdorner(IDrawableContainer owner, Positions position) : base(owner)
        {
            Position = position;
        }

        /// <summary>
        /// Returns the cursor for when the mouse is over the adorner
        /// </summary>
        public override Cursor Cursor => Cursors.SizeAll;

        /// <summary>
        /// Handle the mouse down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="mouseEventArgs"></param>
        public override void MouseDown(object sender, MouseEventArgs mouseEventArgs)
        {
            EditStatus = EditStatus.RESIZING;
            _boundsBeforeResize = new NativeRect(Owner.Left, Owner.Top, Owner.Width, Owner.Height);
            _boundsAfterResize = _boundsBeforeResize;
        }

        /// <summary>
        /// Handle the mouse move
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="mouseEventArgs"></param>
        public override void MouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            if (EditStatus != EditStatus.RESIZING)
            {
                return;
            }

            Owner.Invalidate();
            Owner.MakeBoundsChangeUndoable(false);

            // reset "workbench" rectangle to current bounds
            _boundsAfterResize = _boundsBeforeResize;

            // calculate scaled rectangle
            _boundsAfterResize = ScaleHelper.Scale(_boundsAfterResize, Position, new PointF(mouseEventArgs.X, mouseEventArgs.Y), ScaleHelper.GetScaleOptions());

            // apply scaled bounds to this DrawableContainer
            Owner.ApplyBounds(_boundsAfterResize);

            Owner.Invalidate();
        }

        /// <summary>
        /// Return the location of the adorner
        /// </summary>
        public override NativePoint Location
        {
            get
            {
                int x = 0, y = 0;
                switch (Position)
                {
                    case Positions.TopLeft:
                        x = Owner.Left;
                        y = Owner.Top;
                        break;
                    case Positions.BottomLeft:
                        x = Owner.Left;
                        y = Owner.Top + Owner.Height;
                        break;
                    case Positions.MiddleLeft:
                        x = Owner.Left;
                        y = Owner.Top + (Owner.Height / 2);
                        break;
                    case Positions.TopCenter:
                        x = Owner.Left + (Owner.Width / 2);
                        y = Owner.Top;
                        break;
                    case Positions.BottomCenter:
                        x = Owner.Left + (Owner.Width / 2);
                        y = Owner.Top + Owner.Height;
                        break;
                    case Positions.TopRight:
                        x = Owner.Left + Owner.Width;
                        y = Owner.Top;
                        break;
                    case Positions.BottomRight:
                        x = Owner.Left + Owner.Width;
                        y = Owner.Top + Owner.Height;
                        break;
                    case Positions.MiddleRight:
                        x = Owner.Left + Owner.Width;
                        y = Owner.Top + (Owner.Height / 2);
                        break;
                }

                return new NativePoint(x, y);
            }
        }
    }
}