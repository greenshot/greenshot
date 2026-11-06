/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Drawing.Drawing2D;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;

namespace Greenshot.Editor.Drawing
{
    /// <summary>A filled, closed specialization of the editable polyline from PR #1591.</summary>
    [Serializable]
    public class PolygonContainer : PolylineContainer
    {
        public PolygonContainer(ISurface parent) : base(parent) { }

        protected override void InitializeFields()
        {
            base.InitializeFields();
            AddField(GetType(), FieldType.FILL_COLOR, Color.Transparent);
        }

        public override bool InitContent()
        {
            if (Points.Length < 3) return false;
            _closed = true;
            return true;
        }

        public override void Draw(Graphics graphics, RenderMode renderMode)
        {
            if (Points.Length >= 3)
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                using var brush = new SolidBrush(GetFieldValueAsColor(FieldType.FILL_COLOR));
                graphics.FillPolygon(brush, Points);
            }
            base.Draw(graphics, renderMode);
        }

        public override bool ClickableAt(int x, int y)
        {
            if (Points.Length >= 3 && GetFieldValueAsColor(FieldType.FILL_COLOR).A > 0)
            {
                using var path = new GraphicsPath();
                path.AddPolygon(Points);
                if (path.IsVisible(x, y)) return true;
            }
            return base.ClickableAt(x, y);
        }
    }
}