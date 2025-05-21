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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Helpers;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Description of PathContainer.
    /// </summary>
    [Serializable]
    public class FreehandContainer : DrawableContainer
    {
        private static readonly float[] PointOffset =
        {
            0.5f, 0.25f, 0.75f
        };

        [NonSerialized]
        private GraphicsPath freehandPath = new GraphicsPath();

        private Rectangle myBounds = NativeRect.Empty;
        private Point lastMouse = NativePoint.Empty;
        private List<Point> capturePoints = new List<Point>();
        private bool isRecalculated;

        /// <summary>
        /// Constructor
        /// </summary>
        public FreehandContainer(ISurface parent) : base(parent)
        {
            Width = parent.Image.Width;
            Height = parent.Image.Height;
            Top = 0;
            Left = 0;
        }

        protected override void InitializeFields()
        {
            AddField(GetType(), FieldType.LINE_THICKNESS, 3);
            AddField(GetType(), FieldType.LINE_COLOR, Color.Red);
        }

        public override void Transform(Matrix matrix)
        {
            Point[] points = capturePoints.ToArray();

            matrix.TransformPoints(points);
            capturePoints.Clear();
            capturePoints.AddRange(points);
            RecalculatePath();
        }

        protected override void OnDeserialized(StreamingContext context)
        {
            RecalculatePath();
        }

        /// <summary>
        /// This Dispose is called from the Dispose and the Destructor.
        /// </summary>
        /// <param name="disposing">When disposing==true all non-managed resources should be freed too!</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                freehandPath?.Dispose();
            }

            freehandPath = null;
        }

        /// <summary>
        /// Called from Surface (the parent) when the drawing begins (mouse-down)
        /// </summary>
        /// <returns>true if the surface doesn't need to handle the event</returns>
        public override bool HandleMouseDown(int mouseX, int mouseY)
        {
            lastMouse = new Point(mouseX, mouseY);
            capturePoints.Add(lastMouse);
            return true;
        }

        /// <summary>
        /// Called from Surface (the parent) if a mouse move is made while drawing
        /// </summary>
        /// <returns>true if the surface doesn't need to handle the event</returns>
        public override bool HandleMouseMove(int mouseX, int mouseY)
        {
            Point previousPoint = capturePoints[capturePoints.Count - 1];

            if (GeometryHelper.Distance2D(previousPoint.X, previousPoint.Y, mouseX, mouseY) >= 2 * EditorConfig.FreehandSensitivity)
            {
                capturePoints.Add(new Point(mouseX, mouseY));
            }

            if (GeometryHelper.Distance2D(lastMouse.X, lastMouse.Y, mouseX, mouseY) < EditorConfig.FreehandSensitivity)
            {
                return true;
            }

            //path.AddCurve(new Point[]{lastMouse, new Point(mouseX, mouseY)});
            lastMouse = new Point(mouseX, mouseY);
            freehandPath.AddLine(lastMouse, new Point(mouseX, mouseY));
            // Only re-calculate the bounds & redraw when we added something to the path
            myBounds = Rectangle.Round(freehandPath.GetBounds());

            Invalidate();
            return true;
        }

        /// <summary>
        /// Called when the surface finishes drawing the element
        /// </summary>
        public override void HandleMouseUp(int mouseX, int mouseY)
        {
            // Make sure we don't loose the ending point
            if (GeometryHelper.Distance2D(lastMouse.X, lastMouse.Y, mouseX, mouseY) >= EditorConfig.FreehandSensitivity)
            {
                capturePoints.Add(new Point(mouseX, mouseY));
            }

            RecalculatePath();
        }

        /// <summary>
        /// Here we recalculate the freehand path by smoothing out the lines with Beziers.
        /// </summary>
        private void RecalculatePath()
        {
            // Store the previous path, to dispose it later when we are finished
            var previousFreehandPath = freehandPath;
            var newFreehandPath = new GraphicsPath();

            // Here we can put some cleanup... like losing all the uninteresting  points.
            if (capturePoints.Count >= 3)
            {
                int index = 0;
                while ((capturePoints.Count - 1) % 3 != 0)
                {
                    // duplicate points, first at 50% than 25% than 75%
                    capturePoints.Insert((int) (capturePoints.Count * PointOffset[index]), capturePoints[(int) (capturePoints.Count * PointOffset[index++])]);
                }

                newFreehandPath.AddBeziers(capturePoints.ToArray());
            }
            else if (capturePoints.Count == 2)
            {
                newFreehandPath.AddLine(capturePoints[0], capturePoints[1]);
            }

            // Recalculate the bounds
            myBounds = Rectangle.Round(newFreehandPath.GetBounds());

            // assign
            isRecalculated = true;
            freehandPath = newFreehandPath;

            // dispose previous
            previousFreehandPath?.Dispose();
        }

        /// <summary>
        /// Do the drawing of the freehand "stroke"
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="renderMode"></param>
        public override void Draw(Graphics graphics, RenderMode renderMode)
        {
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
            Color lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR);
            using var pen = new Pen(lineColor)
            {
                Width = lineThickness
            };
            if (!(pen.Width > 0))
            {
                return;
            }

            // Make sure the lines are nicely rounded
            pen.EndCap = LineCap.Round;
            pen.StartCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            // Move to where we need to draw
            graphics.TranslateTransform(Left, Top);
            var currentFreehandPath = freehandPath;
            if (currentFreehandPath != null)
            {
                if (isRecalculated && Selected && renderMode == RenderMode.EDIT)
                {
                    isRecalculated = false;
                    DrawSelectionBorder(graphics, pen, currentFreehandPath);
                }

                graphics.DrawPath(pen, currentFreehandPath);
            }

            // Move back, otherwise everything is shifted
            graphics.TranslateTransform(-Left, -Top);
        }

        /// <summary>
        /// Draw a selectionborder around the freehand path
        /// </summary>
        /// <param name="graphics">Graphics</param>
        /// <param name="linePen">Pen</param>
        /// <param name="path">GraphicsPath</param>
        protected static void DrawSelectionBorder(Graphics graphics, Pen linePen, GraphicsPath path)
        {
            using var selectionPen = (Pen) linePen.Clone();
            using var selectionPath = (GraphicsPath) path.Clone();
            selectionPen.Width += 5;
            selectionPen.Color = Color.FromArgb(120, Color.LightSeaGreen);
            graphics.DrawPath(selectionPen, selectionPath);
            selectionPath.Widen(selectionPen);
            selectionPen.DashPattern = new float[]
            {
                2, 2
            };
            selectionPen.Color = Color.LightSeaGreen;
            selectionPen.Width = 1;
            graphics.DrawPath(selectionPen, selectionPath);
        }

        /// <summary>
        /// Get the bounds in which we have something drawn, plus safety margin, these are not the normal bounds...
        /// </summary>
        public override NativeRect DrawingBounds
        {
            get
            {
                if (!myBounds.IsEmpty)
                {
                    int lineThickness = Math.Max(10, GetFieldValueAsInt(FieldType.LINE_THICKNESS));
                    int safetyMargin = 10;
                    return new NativeRect(myBounds.Left + Left - (safetyMargin + lineThickness), myBounds.Top + Top - (safetyMargin + lineThickness),
                        myBounds.Width + 2 * (lineThickness + safetyMargin), myBounds.Height + 2 * (lineThickness + safetyMargin));
                }

                if (_parent?.Image is Image image)
                {
                    return new NativeRect(0, 0, image.Width, image.Height);
                }

                return NativeRect.Empty;
            }
        }

        /// <summary>
        /// FreehandContainer are regarded equal if they are of the same type and their paths are equal.
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>bool</returns>
        public override bool Equals(object obj)
        {
            bool ret = false;
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            if (obj is FreehandContainer other && Equals(freehandPath, other.freehandPath))
            {
                ret = true;
            }

            return ret;
        }

        public override int GetHashCode()
        {
            return freehandPath?.GetHashCode() ?? 0;
        }

        public override bool ClickableAt(int x, int y)
        {
            bool returnValue = base.ClickableAt(x, y);
            if (returnValue)
            {
                int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
                using var pen = new Pen(Color.White)
                {
                    Width = lineThickness + 10
                };
                returnValue = freehandPath.IsOutlineVisible(x - Left, y - Top, pen);
            }

            return returnValue;
        }
   }
}