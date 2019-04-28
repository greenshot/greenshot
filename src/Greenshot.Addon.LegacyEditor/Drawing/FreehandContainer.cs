/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.Serialization;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Gfx.Legacy;

namespace Greenshot.Addon.LegacyEditor.Drawing {
	/// <summary>
	/// Description of PathContainer.
	/// </summary>
	[Serializable] 
	public class FreehandContainer : DrawableContainer {
		private static readonly float [] PointOffset = {0.5f, 0.25f, 0.75f};

		[NonSerialized]
		private readonly object _freehandPathLock = new object();
		private NativeRect _myBounds = NativeRect.Empty;
		private NativePoint _lastMouse = NativePoint.Empty;
		private readonly List<NativePointFloat> _capturePoints = new List<NativePointFloat>();
        [NonSerialized]
        private GraphicsPath _freehandPath = new GraphicsPath();
		private bool _isRecalculated;
		
		/// <summary>
		/// Constructor
		/// </summary>
		public FreehandContainer(Surface parent, IEditorConfiguration editorConfiguration) : base(parent, editorConfiguration) {
			Width = parent.Width;
			Height = parent.Height;
			Top = 0;
			Left = 0;
        }

		protected override void InitializeFields() {
			AddField(GetType(), FieldTypes.LINE_THICKNESS, 3);
			AddField(GetType(), FieldTypes.LINE_COLOR, Color.Red);
		}

		public override void Transform(Matrix matrix)
		{
		    var newPoints = _capturePoints.Cast<PointF>().ToArray();
            matrix.TransformPoints(newPoints);
			_capturePoints.Clear();
			_capturePoints.AddRange(newPoints.Cast<NativePointFloat>());
			RecalculatePath();
		}
		
		protected override void OnDeserialized(StreamingContext context) {
			RecalculatePath();
		}

		/// <summary>
		/// This Dispose is called from the Dispose and the Destructor.
		/// </summary>
		/// <param name="disposing">When disposing==true all non-managed resources should be freed too!</param>
		protected override void Dispose(bool disposing) {
			base.Dispose(disposing);
			if (disposing)
			{
				_freehandPath?.Dispose();
			}
			_freehandPath = null;
		}
		
		/// <summary>
		/// Called from Surface (the parent) when the drawing begins (mouse-down)
		/// </summary>
		/// <returns>true if the surface doesn't need to handle the event</returns>
		public override bool HandleMouseDown(int mouseX, int mouseY) {
			_lastMouse = new NativePoint(mouseX, mouseY);
			_capturePoints.Add(_lastMouse);
			return true;
		}

		/// <summary>
		/// Called from Surface (the parent) if a mouse move is made while drawing
		/// </summary>
		/// <returns>true if the surface doesn't need to handle the event</returns>
		public override bool HandleMouseMove(int mouseX, int mouseY) {
			NativePoint previousPoint = _capturePoints[_capturePoints.Count-1];

			if (GeometryHelper.Distance2D(previousPoint.X, previousPoint.Y, mouseX, mouseY) >= 2* _editorConfiguration.FreehandSensitivity) {
				_capturePoints.Add(new NativePoint(mouseX, mouseY));
			}
		    if (GeometryHelper.Distance2D(_lastMouse.X, _lastMouse.Y, mouseX, mouseY) < _editorConfiguration.FreehandSensitivity)
		    {
		        return true;
		    }
		    //path.AddCurve(new NativePoint[]{lastMouse, new NativePoint(mouseX, mouseY)});
		    _lastMouse = new NativePoint(mouseX, mouseY);
		    lock (_freehandPathLock)
		    {
		        _freehandPath.AddLine(_lastMouse, new NativePoint(mouseX, mouseY));
                // Only re-calculate the bounds & redraw when we added something to the path
		        NativeRectFloat rect = _freehandPath.GetBounds();
                _myBounds = rect.Round();
		    }

		    Invalidate();
		    return true;
		}

		/// <summary>
		/// Called when the surface finishes drawing the element
		/// </summary>
		public override void HandleMouseUp(int mouseX, int mouseY) {
			// Make sure we don't loose the ending point
			if (GeometryHelper.Distance2D(_lastMouse.X, _lastMouse.Y, mouseX, mouseY) >= _editorConfiguration.FreehandSensitivity) {
				_capturePoints.Add(new NativePoint(mouseX, mouseY));
			}
			RecalculatePath();
		}
		
		/// <summary>
		/// Here we recalculate the freehand path by smoothing out the lines with Beziers.
		/// </summary>
		private void RecalculatePath() {
		    lock (_freehandPathLock)
		    {
		        _isRecalculated = true;
		        // Dispose the previous path, if we have one
		        _freehandPath?.Dispose();
		        _freehandPath = new GraphicsPath();

		        // Here we can put some cleanup... like losing all the uninteresting  points.
		        if (_capturePoints.Count >= 3)
		        {
		            int index = 0;
		            while ((_capturePoints.Count - 1) % 3 != 0)
		            {
		                // duplicate points, first at 50% than 25% than 75%
		                _capturePoints.Insert((int)(_capturePoints.Count * PointOffset[index]), _capturePoints[(int)(_capturePoints.Count * PointOffset[index++])]);
		            }
		            _freehandPath.AddBeziers(_capturePoints.Cast<PointF>().ToArray());
		        }
		        else if (_capturePoints.Count == 2)
		        {
		            _freehandPath.AddLine(_capturePoints[0], _capturePoints[1]);
		        }

		        // Recalculate the bounds
		        NativeRectFloat rect = _freehandPath.GetBounds();
                _myBounds = rect.Round();

            }

		}

		/// <summary>
		/// Do the drawing of the freehand "stroke"
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="renderMode"></param>
		public override void Draw(Graphics graphics, RenderMode renderMode) {
		    graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			
			int lineThickness = GetFieldValueAsInt(FieldTypes.LINE_THICKNESS);
			var lineColor = GetFieldValueAsColor(FieldTypes.LINE_COLOR);
			using (var pen = new Pen(lineColor)) {
				pen.Width = lineThickness;
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
			    lock (_freehandPathLock)
			    {
			        if (_isRecalculated && Selected && renderMode == RenderMode.Edit)
			        {
			            DrawSelectionBorder(graphics, pen, _freehandPath);
			        }
			        graphics.DrawPath(pen, _freehandPath);
			    }

                // Move back, otherwise everything is shifted
                graphics.TranslateTransform(-Left,-Top);
			}
		}

        /// <summary>
        /// Draw a selectionborder around the freehand path
        /// </summary>
        /// <param name="graphics">Graphics</param>
        /// <param name="linePen">Pen</param>
        /// <param name="path">GraphicsPath</param>
        protected static void DrawSelectionBorder(Graphics graphics, Pen linePen, GraphicsPath path) {
			using (var selectionPen = (Pen) linePen.Clone()) {
				using (var selectionPath = (GraphicsPath)path.Clone()) {
					selectionPen.Width += 5;
					selectionPen.Color = Color.FromArgb(120, Color.LightSeaGreen);
					graphics.DrawPath(selectionPen, selectionPath);
					selectionPath.Widen(selectionPen);
					selectionPen.DashPattern = new float[]{2,2};
					selectionPen.Color = Color.LightSeaGreen;
					selectionPen.Width = 1;
					graphics.DrawPath(selectionPen, selectionPath);
				}
			}
		}
		
		/// <summary>
		/// Get the bounds in which we have something drawn, plus safety margin, these are not the normal bounds...
		/// </summary>
		public override NativeRect DrawingBounds {
			get {
				if (!_myBounds.IsEmpty) {
					int lineThickness = Math.Max(10, GetFieldValueAsInt(FieldTypes.LINE_THICKNESS));
					int safetymargin = 10;
					return new NativeRect(_myBounds.Left + Left - (safetymargin+lineThickness), _myBounds.Top + Top - (safetymargin+lineThickness), _myBounds.Width + 2*(lineThickness+safetymargin), _myBounds.Height + 2*(lineThickness+safetymargin));
				}
				return new NativeRect(0, 0, _parent?.Width??0, _parent?.Height?? 0);
			}
		}

        /// <summary>
        /// FreehandContainer are regarded equal if they are of the same type and their paths are equal.
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>bool</returns>
        public override bool Equals(object obj) {
			bool ret = false;
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            if (obj is FreehandContainer other && Equals(_freehandPath, other._freehandPath)) {
                ret = true;
            }
            return ret;
		}

		public override int GetHashCode() {
		    lock (_freehandPathLock)
		    {
		        return _freehandPath?.GetHashCode() ?? 0;
		    }
		}

		public override bool ClickableAt(int x, int y) {
			bool returnValue = base.ClickableAt(x, y);
			if (returnValue) {
				int lineThickness = GetFieldValueAsInt(FieldTypes.LINE_THICKNESS);
				using (var pen = new Pen(Color.White)) {
					pen.Width = lineThickness + 10;
				    lock (_freehandPathLock)
				    {
				        returnValue = _freehandPath.IsOutlineVisible(x - Left, y - Top, pen);
                    }
                }
			}
			return returnValue;
		}
	}
}
