// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Drawing;
using System.Windows.Forms;

namespace Greenshot.Addon.LegacyEditor.Controls
{
    /// <summary>
	///     ToolStripProfessionalRenderer without having a visual artifact
	///     See: http://stackoverflow.com/a/16926979 and http://stackoverflow.com/a/13418840
	/// </summary>
	public class CustomToolStripProfessionalRenderer : ToolStripProfessionalRenderer
    {
		public CustomToolStripProfessionalRenderer() : base(new CustomProfessionalColorTable())
		{
			RoundedEdges = false;
		}

		/// <summary>
		///     By overriding the OnRenderToolStripBorder we can make the ToolStrip without border
		/// </summary>
		/// <param name="e"></param>
		protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
		{
			// Don't draw a border
		}

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            if (e.Item.GetType() != typeof(ToolStripSplitButton))
            {
                base.OnRenderArrow(e);
                return;
            }
            var graphics = e.Graphics;
            var dropDownRect = e.ArrowRectangle;
            using (var brush = new SolidBrush(e.ArrowColor))
            {
                int halfHeight = e.ArrowRectangle.Height / 2;
                int halfWidth = e.ArrowRectangle.Width / 2;
                var middle = new Point(dropDownRect.Left + halfWidth, dropDownRect.Top + halfHeight);

                Point[] arrow;

                int verticalArrowStart = middle.Y - halfHeight / 3;
                int verticalArrowEnd = middle.Y + halfHeight / 3;
                int horizontalArrowStart = middle.X - halfWidth;
                int horizontalArrowEnd = middle.X + halfWidth;

                switch (e.Direction)
                {
                    case ArrowDirection.Up:

                        arrow = new[] {
                                     new Point(horizontalArrowStart, verticalArrowEnd),
                                     new Point(horizontalArrowEnd, verticalArrowEnd),
                                     new Point(middle.X, verticalArrowStart)};

                        break;
                    case ArrowDirection.Left:
                        arrow = new[] {
                                     new Point(horizontalArrowEnd, verticalArrowStart),
                                     new Point(horizontalArrowEnd, verticalArrowEnd),
                                     new Point(horizontalArrowStart, middle.Y)};

                        break;
                    case ArrowDirection.Right:
                        arrow = new[] {
                                     new Point(horizontalArrowStart, verticalArrowStart),
                                     new Point(horizontalArrowStart, verticalArrowEnd),
                                     new Point(horizontalArrowEnd, middle.Y)};

                        break;
                    default:
                        arrow = new[] {
                                 new Point(horizontalArrowStart, verticalArrowStart),
                                 new Point(horizontalArrowEnd, verticalArrowStart),
                                 new Point(middle.X, verticalArrowEnd) };
                        break;
                }
                graphics.FillPolygon(brush, arrow);
            }
        }
    }
}