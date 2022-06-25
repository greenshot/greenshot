/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Adorners;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Helpers;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Description of LineContainer.
    /// </summary>
    [Serializable()]
    public class LineContainer : DrawableContainer
    {
        public LineContainer(ISurface parent) : base(parent)
        {
            Init();
        }

        protected override void InitializeFields()
        {
            AddField(GetType(), FieldType.LINE_THICKNESS, 2);
            AddField(GetType(), FieldType.LINE_COLOR, Color.Red);
            AddField(GetType(), FieldType.SHADOW, true);
        }

        protected override void OnDeserialized(StreamingContext context)
        {
            Init();
        }

        protected void Init()
        {
            Adorners.Add(new MoveAdorner(this, Positions.TopLeft));
            Adorners.Add(new MoveAdorner(this, Positions.BottomRight));
        }

        public override void Draw(Graphics graphics, RenderMode rm)
        {

            int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);

            if (lineThickness <= 0) return;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.None;

            bool shadow = GetFieldValueAsBool(FieldType.SHADOW);
            if (shadow)
            {
                //draw shadow first
                int basealpha = 100;
                int alpha = basealpha;
                int steps = 5;
                int currentStep = 1;
                while (currentStep <= steps)
                {
                    using Pen shadowCapPen = new Pen(Color.FromArgb(alpha, 100, 100, 100), lineThickness);
                    graphics.DrawLine(shadowCapPen,
                        Left + currentStep,
                        Top + currentStep,
                        Left + currentStep + Width,
                        Top + currentStep + Height);

                    currentStep++;
                    alpha -= basealpha / steps;
                }
            }

            Color lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR);
            using Pen pen = new Pen(lineColor, lineThickness);
            graphics.DrawLine(pen, Left, Top, Left + Width, Top + Height);
        }

        public override bool ClickableAt(int x, int y)
        {
            int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS) + 5;
            if (lineThickness > 0)
            {
                using Pen pen = new Pen(Color.White)
                {
                    Width = lineThickness
                };
                using GraphicsPath path = new GraphicsPath();
                path.AddLine(Left, Top, Left + Width, Top + Height);
                return path.IsOutlineVisible(x, y, pen);
            }

            return false;
        }

        protected override IDoubleProcessor GetAngleRoundProcessor()
        {
            return LineAngleRoundBehavior.INSTANCE;
        }
    }
}