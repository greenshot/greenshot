/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Drawing;
using System.Runtime.InteropServices;

namespace Greenshot.Base.UnmanagedHelpers.Structs
{
    /// <summary>
    /// A floating point GDI Plus width/hight based rectangle.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RECTF
    {
        /// <summary>
        /// The X corner location of the rectangle.
        /// </summary>
        public float X;

        /// <summary>
        /// The Y corner location of the rectangle.
        /// </summary>
        public float Y;

        /// <summary>
        /// The width of the rectangle.
        /// </summary>
        public float Width;

        /// <summary>
        /// The height of the rectangle.
        /// </summary>
        public float Height;

        /// <summary>
        /// Creates a new GDI Plus rectangle.
        /// </summary>
        /// <param name="x">The X corner location of the rectangle.</param>
        /// <param name="y">The Y corner location of the rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        public RECTF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Creates a new GDI Plus rectangle from a System.Drawing.RectangleF.
        /// </summary>
        /// <param name="rect">The rectangle to base this GDI Plus rectangle on.</param>
        public RECTF(RectangleF rect)
        {
            X = rect.X;
            Y = rect.Y;
            Width = rect.Width;
            Height = rect.Height;
        }

        /// <summary>
        /// Creates a new GDI Plus rectangle from a System.Drawing.Rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to base this GDI Plus rectangle on.</param>
        public RECTF(Rectangle rect)
        {
            X = rect.X;
            Y = rect.Y;
            Width = rect.Width;
            Height = rect.Height;
        }

        /// <summary>
        /// Returns a RectangleF for this GDI Plus rectangle.
        /// </summary>
        /// <returns>A System.Drawing.RectangleF structure.</returns>
        public RectangleF ToRectangle()
        {
            return new RectangleF(X, Y, Width, Height);
        }

        /// <summary>
        /// Returns a RectangleF for a GDI Plus rectangle.
        /// </summary>
        /// <param name="rect">The GDI Plus rectangle to get the RectangleF for.</param>
        /// <returns>A System.Drawing.RectangleF structure.</returns>
        public static RectangleF ToRectangle(RECTF rect)
        {
            return rect.ToRectangle();
        }

        /// <summary>
        /// Returns a GDI Plus rectangle for a RectangleF structure.
        /// </summary>
        /// <param name="rect">The RectangleF to get the GDI Plus rectangle for.</param>
        /// <returns>A GDI Plus rectangle structure.</returns>
        public static RECTF FromRectangle(RectangleF rect)
        {
            return new RECTF(rect);
        }

        /// <summary>
        /// Returns a GDI Plus rectangle for a Rectangle structure.
        /// </summary>
        /// <param name="rect">The Rectangle to get the GDI Plus rectangle for.</param>
        /// <returns>A GDI Plus rectangle structure.</returns>
        public static RECTF FromRectangle(Rectangle rect)
        {
            return new RECTF(rect);
        }
    }
}