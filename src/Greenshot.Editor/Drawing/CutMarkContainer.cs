/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.Serialization;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// How the place where a strip was cut out of the image is marked
    /// </summary>
    public enum CutMarkStyle
    {
        /// <summary>
        /// No mark, the parts are joined seamlessly
        /// </summary>
        None,
        /// <summary>
        /// Two straight lines
        /// </summary>
        Line,
        /// <summary>
        /// Two zig-zag lines, like a break line in a technical drawing
        /// </summary>
        ZigZag,
        /// <summary>
        /// Two wavy lines
        /// </summary>
        Wave,
        /// <summary>
        /// Two torn edges, like the torn edge effect
        /// </summary>
        Torn
    }

    /// <summary>
    /// Marks the place where a strip was cut out of the image (crop out horizontally / vertically).
    /// It is a band over the joint: the fill color between two lines, the lines follow the style.
    /// A band that is wider than high marks a horizontal joint, otherwise a vertical one.
    /// </summary>
    [Serializable]
    public sealed class CutMarkContainer : DrawableContainer
    {
        /// <summary>
        /// The thickness of the band, in pixels, when it is placed after a cut out
        /// </summary>
        public const int DefaultBandSize = 14;

        // Keeps the torn edges the same on every redraw and after saving / loading
        private readonly int _seed;

        public CutMarkContainer(ISurface parent) : this(parent, Environment.TickCount)
        {
        }

        public CutMarkContainer(ISurface parent, int seed) : base(parent)
        {
            _seed = seed;
            Init();
        }

        protected override void OnDeserialized(StreamingContext streamingContext)
        {
            base.OnDeserialized(streamingContext);
            Init();
        }

        private void Init()
        {
            CreateDefaultAdorners();
        }

        protected override void InitializeFields()
        {
            AddField(GetType(), FieldType.LINE_THICKNESS, 1);
            AddField(GetType(), FieldType.LINE_COLOR, Color.DimGray);
            AddField(GetType(), FieldType.FILL_COLOR, Color.White);
            AddField(GetType(), FieldType.SHADOW, false);
            AddField(GetType(), FieldType.CUT_MARK_STYLE, CutMarkStyle.Torn);
        }

        public override void Draw(Graphics graphics, RenderMode rm)
        {
            if (GetFieldValue(FieldType.CUT_MARK_STYLE) is not CutMarkStyle style || style == CutMarkStyle.None)
            {
                return;
            }

            var rect = new NativeRect(Left, Top, Width, Height).Normalize();
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
            Color lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR, Color.DimGray);
            Color fillColor = GetFieldValueAsColor(FieldType.FILL_COLOR, Color.White);
            bool shadow = GetFieldValueAsBool(FieldType.SHADOW);

            CreateEdges(style, rect, _seed, out var firstEdge, out var secondEdge);
            DrawBand(graphics, firstEdge, secondEdge, lineThickness, lineColor, fillColor, shadow);
        }

        /// <summary>
        /// Draws the band between the two edges
        /// </summary>
        private static void DrawBand(Graphics graphics, PointF[] firstEdge, PointF[] secondEdge, int lineThickness, Color lineColor, Color fillColor, bool shadow)
        {
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.None;

            bool lineVisible = lineThickness > 0 && Colors.IsVisible(lineColor);
            if (Colors.IsVisible(fillColor))
            {
                using var path = new GraphicsPath();
                path.AddLines(firstEdge);
                path.AddLines(secondEdge.Reverse().ToArray());
                path.CloseFigure();
                using Brush brush = new SolidBrush(fillColor);
                graphics.FillPath(brush, path);
            }

            if (shadow)
            {
                // The shadow falls from the first part onto the band, over the fill
                DrawShadow(Math.Max(1, lineThickness), (alpha, currentStep, shadowPen, nil) =>
                {
                    var shadowEdge = (PointF[])firstEdge.Clone();
                    Offset(shadowEdge, firstEdge, secondEdge, currentStep);
                    graphics.DrawLines(shadowPen, shadowEdge);
                });
            }

            if (lineVisible)
            {
                using var pen = new Pen(lineColor, lineThickness)
                {
                    LineJoin = LineJoin.Round
                };
                graphics.DrawLines(pen, firstEdge);
                graphics.DrawLines(pen, secondEdge);
            }
        }

        /// <summary>
        /// Moves the points of the shadow edge towards the second edge
        /// </summary>
        private static void Offset(PointF[] points, PointF[] firstEdge, PointF[] secondEdge, int step)
        {
            bool horizontal = Math.Abs(firstEdge[firstEdge.Length - 1].X - firstEdge[0].X) >= Math.Abs(firstEdge[firstEdge.Length - 1].Y - firstEdge[0].Y);
            float direction = horizontal ? Math.Sign(secondEdge[0].Y - firstEdge[0].Y) : Math.Sign(secondEdge[0].X - firstEdge[0].X);
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = horizontal ? new PointF(points[i].X, points[i].Y + direction * (step + 1)) : new PointF(points[i].X + direction * (step + 1), points[i].Y);
            }
        }

        /// <summary>
        /// Creates the two edges of the band for the style, along the long side of the rectangle
        /// </summary>
        /// <param name="style">CutMarkStyle, not None</param>
        /// <param name="rect">NativeRect of the band</param>
        /// <param name="seed">int for the random parts of the torn style</param>
        /// <param name="firstEdge">the top or left edge</param>
        /// <param name="secondEdge">the bottom or right edge</param>
        public static void CreateEdges(CutMarkStyle style, NativeRect rect, int seed, out PointF[] firstEdge, out PointF[] secondEdge)
        {
            bool horizontal = rect.Width >= rect.Height;
            float length = horizontal ? rect.Width : rect.Height;
            float thickness = horizontal ? rect.Height : rect.Width;

            // Coordinates along (u) and across (v) the band
            var first = new List<PointF>();
            var second = new List<PointF>();
            switch (style)
            {
                case CutMarkStyle.ZigZag:
                {
                    float amplitude = thickness / 3;
                    float step = Math.Max(2, thickness / 2);
                    int i = 0;
                    for (float u = 0; ; u += step, i++)
                    {
                        float v = i % 2 == 0 ? 0 : amplitude;
                        first.Add(new PointF(Math.Min(u, length), v));
                        second.Add(new PointF(Math.Min(u, length), v + thickness - amplitude));
                        if (u >= length) break;
                    }
                    break;
                }
                case CutMarkStyle.Wave:
                {
                    float amplitude = thickness / 6;
                    float period = Math.Max(4, thickness * 2);
                    for (float u = 0; ; u += 2)
                    {
                        float clamped = Math.Min(u, length);
                        float v = amplitude + amplitude * (float)Math.Sin(2 * Math.PI * clamped / period);
                        first.Add(new PointF(clamped, v));
                        second.Add(new PointF(clamped, v + thickness - 2 * amplitude));
                        if (u >= length) break;
                    }
                    break;
                }
                case CutMarkStyle.Torn:
                {
                    var random = new Random(seed);
                    float toothHeight = thickness / 3;
                    float toothWidth = Math.Max(4, thickness);
                    for (float u = 0; ; u += toothWidth)
                    {
                        float clamped = Math.Min(u, length);
                        first.Add(new PointF(clamped, (float)random.NextDouble() * toothHeight));
                        second.Add(new PointF(clamped, thickness - (float)random.NextDouble() * toothHeight));
                        if (u >= length) break;
                    }
                    break;
                }
                default:
                {
                    float gap = thickness / 3;
                    first.Add(new PointF(0, gap));
                    first.Add(new PointF(length, gap));
                    second.Add(new PointF(0, thickness - gap));
                    second.Add(new PointF(length, thickness - gap));
                    break;
                }
            }

            PointF ToImage(PointF p) => horizontal ? new PointF(rect.Left + p.X, rect.Top + p.Y) : new PointF(rect.Left + p.Y, rect.Top + p.X);
            firstEdge = first.Select(ToImage).ToArray();
            secondEdge = second.Select(ToImage).ToArray();
        }

        public override bool ClickableAt(int x, int y)
        {
            var rect = new NativeRect(Left, Top, Width, Height).Normalize();
            return rect.Inflate(2, 2).Contains(x, y);
        }
    }
}
