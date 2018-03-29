#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Drawing.Drawing2D;
using Dapplo.Windows.Common.Structs;

#endregion

namespace Greenshot.Gfx.Legacy
{
	/// <summary>
	///     TODO: currently this is only used in the capture form, we might move this code directly to there!
	/// </summary>
	public abstract class RoundedRectangle
	{
		[Flags]
		public enum RectangleCorners
		{
			None = 0,
			TopLeft = 1,
			TopRight = 2,
			BottomLeft = 4,
			BottomRight = 8,
			All = TopLeft | TopRight | BottomLeft | BottomRight
		}

		public static GraphicsPath Create2(int x, int y, int width, int height, int radius)
		{
			var gp = new GraphicsPath();
			gp.AddLine(x + radius, y, x + width - radius * 2, y); // Line
			gp.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90); // Corner
			gp.AddLine(x + width, y + radius, x + width, y + height - radius * 2); // Line
			gp.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90); // Corner
			gp.AddLine(x + width - radius * 2, y + height, x + radius, y + height); // Line
			gp.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90); // Corner
			gp.AddLine(x, y + height - radius * 2, x, y + radius); // Line
			gp.AddArc(x, y, radius * 2, radius * 2, 180, 90); // Corner
			gp.CloseFigure();

			return gp;
		}

		public static GraphicsPath Create(int x, int y, int width, int height, int radius, RectangleCorners corners)
		{
			var xw = x + width;
			var yh = y + height;
			var xwr = xw - radius;
			var yhr = yh - radius;
			var xr = x + radius;
			var yr = y + radius;
			var r2 = radius * 2;
			var xwr2 = xw - r2;
			var yhr2 = yh - r2;

			var p = new GraphicsPath();
			p.StartFigure();

			//Top Left Corner
			if ((RectangleCorners.TopLeft & corners) == RectangleCorners.TopLeft)
			{
				p.AddArc(x, y, r2, r2, 180, 90);
			}
			else
			{
				p.AddLine(x, yr, x, y);
				p.AddLine(x, y, xr, y);
			}

			//Top Edge
			p.AddLine(xr, y, xwr, y);

			//Top Right Corner
			if ((RectangleCorners.TopRight & corners) == RectangleCorners.TopRight)
			{
				p.AddArc(xwr2, y, r2, r2, 270, 90);
			}
			else
			{
				p.AddLine(xwr, y, xw, y);
				p.AddLine(xw, y, xw, yr);
			}

			//Right Edge
			p.AddLine(xw, yr, xw, yhr);

			//Bottom Right Corner
			if ((RectangleCorners.BottomRight & corners) == RectangleCorners.BottomRight)
			{
				p.AddArc(xwr2, yhr2, r2, r2, 0, 90);
			}
			else
			{
				p.AddLine(xw, yhr, xw, yh);
				p.AddLine(xw, yh, xwr, yh);
			}

			//Bottom Edge
			p.AddLine(xwr, yh, xr, yh);

			//Bottom Left Corner
			if ((RectangleCorners.BottomLeft & corners) == RectangleCorners.BottomLeft)
			{
				p.AddArc(x, yhr2, r2, r2, 90, 90);
			}
			else
			{
				p.AddLine(xr, yh, x, yh);
				p.AddLine(x, yh, x, yhr);
			}

			//Left Edge
			p.AddLine(x, yhr, x, yr);

			p.CloseFigure();
			return p;
		}

		public static GraphicsPath Create(NativeRect rect, int radius, RectangleCorners corners)
		{
			return Create(rect.X, rect.Y, rect.Width, rect.Height, radius, corners);
		}

		public static GraphicsPath Create(int x, int y, int width, int height, int radius)
		{
			return Create(x, y, width, height, radius, RectangleCorners.All);
		}

		public static GraphicsPath Create(NativeRect rect, int radius)
		{
			return Create(rect.X, rect.Y, rect.Width, rect.Height, radius);
		}

		public static GraphicsPath Create(int x, int y, int width, int height)
		{
			return Create(x, y, width, height, 5);
		}

		public static GraphicsPath Create(NativeRect rect)
		{
			return Create(rect.X, rect.Y, rect.Width, rect.Height);
		}
	}
}