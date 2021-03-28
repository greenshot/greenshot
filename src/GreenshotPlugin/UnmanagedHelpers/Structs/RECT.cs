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

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace GreenshotPlugin.UnmanagedHelpers.Structs
{
    [StructLayout(LayoutKind.Sequential), Serializable()]
    public struct RECT
    {
        private int _Left;
        private int _Top;
        private int _Right;
        private int _Bottom;

        public RECT(RECT rectangle)
            : this(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom)
        {
        }

        public RECT(Rectangle rectangle)
            : this(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom)
        {
        }

        public RECT(int left, int top, int right, int bottom)
        {
            _Left = left;
            _Top = top;
            _Right = right;
            _Bottom = bottom;
        }

        public int X
        {
            get { return _Left; }
            set { _Left = value; }
        }

        public int Y
        {
            get { return _Top; }
            set { _Top = value; }
        }

        public int Left
        {
            get { return _Left; }
            set { _Left = value; }
        }

        public int Top
        {
            get { return _Top; }
            set { _Top = value; }
        }

        public int Right
        {
            get { return _Right; }
            set { _Right = value; }
        }

        public int Bottom
        {
            get { return _Bottom; }
            set { _Bottom = value; }
        }

        public int Height
        {
            get { return _Bottom - _Top; }
            set { _Bottom = value - _Top; }
        }

        public int Width
        {
            get { return _Right - _Left; }
            set { _Right = value + _Left; }
        }

        public Point Location
        {
            get { return new Point(Left, Top); }
            set
            {
                _Left = value.X;
                _Top = value.Y;
            }
        }

        public Size Size
        {
            get { return new Size(Width, Height); }
            set
            {
                _Right = value.Width + _Left;
                _Bottom = value.Height + _Top;
            }
        }

        public static implicit operator Rectangle(RECT rectangle)
        {
            return new Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
        }

        public static implicit operator RECT(Rectangle rectangle)
        {
            return new RECT(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
        }

        public static bool operator ==(RECT rectangle1, RECT rectangle2)
        {
            return rectangle1.Equals(rectangle2);
        }

        public static bool operator !=(RECT rectangle1, RECT rectangle2)
        {
            return !rectangle1.Equals(rectangle2);
        }

        public override string ToString()
        {
            return "{Left: " + _Left + "; " + "Top: " + _Top + "; Right: " + _Right + "; Bottom: " + _Bottom + "}";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public bool Equals(RECT rectangle)
        {
            return rectangle.Left == _Left && rectangle.Top == _Top && rectangle.Right == _Right && rectangle.Bottom == _Bottom;
        }

        public Rectangle ToRectangle()
        {
            return new Rectangle(Left, Top, Width, Height);
        }

        public override bool Equals(object Object)
        {
            if (Object is RECT)
            {
                return Equals((RECT) Object);
            }
            else if (Object is Rectangle)
            {
                return Equals(new RECT((Rectangle) Object));
            }

            return false;
        }
    }
}