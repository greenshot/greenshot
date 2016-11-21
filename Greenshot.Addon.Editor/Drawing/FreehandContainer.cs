//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using Dapplo.Log;
using Greenshot.Addon.Editor.Helpers;
using Greenshot.Addon.Editor.Interfaces.Drawing;

#endregion

namespace Greenshot.Addon.Editor.Drawing
{
	/// <summary>
	///     The FreehandContainer can be used to draw freehand on the surface
	/// </summary>
	[Serializable]
	public class FreehandContainer : DrawableContainer
	{
		private static readonly LogSource Log = new LogSource();

		private static readonly float[] PointOffset =
		{
			0.5f, 0.25f, 0.75f
		};

		private readonly List<Point> _capturePoints = new List<Point>();

		[NonSerialized] private GraphicsPath _freehandPath = new GraphicsPath();

		private bool _isRecalculated;
		private Point _lastMouse = Point.Empty;

		private Color _lineColor = Color.Red;

		private int _lineThickness = 2;

		private Rectangle _myBounds = Rectangle.Empty;

		/// <summary>
		///     Constructor
		/// </summary>
		public FreehandContainer(Surface parent) : base(parent)
		{
			Width = parent.Width;
			Height = parent.Height;
			Top = 0;
			Left = 0;
		}

		/// <summary>
		///     Get the bounds in which we have something drawn, plus safety margin, these are not the normal bounds...
		/// </summary>
		public override Rectangle DrawingBounds
		{
			get
			{
				if (!_myBounds.IsEmpty)
				{
					int lineThickness = Math.Max(10, LineThickness);
					const int safetymargin = 10;
					return new Rectangle(_myBounds.Left + Left - (safetymargin + lineThickness), _myBounds.Top + Top - (safetymargin + lineThickness), _myBounds.Width + 2*(lineThickness + safetymargin), _myBounds.Height + 2*(lineThickness + safetymargin));
				}
				return new Rectangle(0, 0, _parent.Width, _parent.Height);
			}
		}

		[Field(FieldTypes.LINE_COLOR)]
		public Color LineColor
		{
			get { return _lineColor; }
			set
			{
				_lineColor = value;
				OnFieldPropertyChanged(FieldTypes.LINE_COLOR);
			}
		}

		[Field(FieldTypes.LINE_THICKNESS)]
		public int LineThickness
		{
			get { return _lineThickness; }
			set
			{
				_lineThickness = value;
				OnFieldPropertyChanged(FieldTypes.LINE_THICKNESS);
			}
		}

		public override bool ClickableAt(int x, int y)
		{
			bool returnValue = base.ClickableAt(x, y);
			if (returnValue)
			{
				using (Pen pen = new Pen(Color.White))
				{
					pen.Width = _lineThickness + 10;
					returnValue = _freehandPath.IsOutlineVisible(x - Left, y - Top, pen);
				}
			}
			return returnValue;
		}


		/// <summary>
		///     This Dispose is called from the Dispose and the Destructor.
		/// </summary>
		/// <param name="disposing">When disposing==true all non-managed resources should be freed too!</param>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				if (_freehandPath != null)
				{
					_freehandPath.Dispose();
				}
			}
			_freehandPath = null;
		}

		/// <summary>
		///     Do the drawing of the freehand "stroke"
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="renderMode"></param>
		public override void Draw(Graphics graphics, RenderMode renderMode)
		{
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			using (Pen pen = new Pen(_lineColor))
			{
				pen.Width = _lineThickness;
				if (pen.Width > 0)
				{
					// Make sure the lines are nicely rounded
					pen.EndCap = LineCap.Round;
					pen.StartCap = LineCap.Round;
					pen.LineJoin = LineJoin.Round;

					// Move to where we need to draw
					graphics.TranslateTransform(Left, Top);
					if (_isRecalculated && Selected && (renderMode == RenderMode.EDIT))
					{
						DrawSelectionBorder(graphics, pen);
					}
					graphics.DrawPath(pen, _freehandPath);
					// Move back, otherwise everything is shifted
					graphics.TranslateTransform(-Left, -Top);
				}
			}
		}

		/// <summary>
		///     Draw a selectionborder around the freehand path
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="linePen"></param>
		protected void DrawSelectionBorder(Graphics graphics, Pen linePen)
		{
			using (Pen selectionPen = (Pen) linePen.Clone())
			{
				using (GraphicsPath selectionPath = (GraphicsPath) _freehandPath.Clone())
				{
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
			}
		}

		/// <summary>
		///     FreehandContainer are regarded equal if they are of the same type and their paths are equal.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			bool ret = false;
			if ((obj != null) && (GetType() == obj.GetType()))
			{
				FreehandContainer other = obj as FreehandContainer;
				if ((other != null) && _freehandPath.Equals(other._freehandPath))
				{
					ret = true;
				}
			}
			return ret;
		}

		public override int GetHashCode()
		{
			return _freehandPath.GetHashCode();
		}

		/// <summary>
		///     Called from Surface (the _parent) when the drawing begins (mouse-down)
		/// </summary>
		/// <returns>true if the surface doesn't need to handle the event</returns>
		public override bool HandleMouseDown(int mouseX, int mouseY)
		{
			_lastMouse = new Point(mouseX, mouseY);
			_capturePoints.Add(_lastMouse);
			return true;
		}

		/// <summary>
		///     Called from Surface (the _parent) if a mouse move is made while drawing
		/// </summary>
		/// <returns>true if the surface doesn't need to handle the event</returns>
		public override bool HandleMouseMove(int mouseX, int mouseY)
		{
			Point previousPoint = _capturePoints[_capturePoints.Count - 1];

			if (GeometryHelper.Distance2D(previousPoint.X, previousPoint.Y, mouseX, mouseY) >= 2*EditorConfig.FreehandSensitivity)
			{
				_capturePoints.Add(new Point(mouseX, mouseY));
			}
			if (GeometryHelper.Distance2D(_lastMouse.X, _lastMouse.Y, mouseX, mouseY) >= EditorConfig.FreehandSensitivity)
			{
				//path.AddCurve(new Point[]{lastMouse, new Point(mouseX, mouseY)});
				_freehandPath.AddLine(_lastMouse, new Point(mouseX, mouseY));
				_lastMouse = new Point(mouseX, mouseY);
				// Only re-calculate the bounds & redraw when we added something to the path
				_myBounds = Rectangle.Round(_freehandPath.GetBounds());
				Invalidate();
			}
			return true;
		}

		/// <summary>
		///     Called when the surface finishes drawing the element
		/// </summary>
		public override void HandleMouseUp(int mouseX, int mouseY)
		{
			// Make sure we don't loose the ending point
			if (GeometryHelper.Distance2D(_lastMouse.X, _lastMouse.Y, mouseX, mouseY) >= EditorConfig.FreehandSensitivity)
			{
				_capturePoints.Add(new Point(mouseX, mouseY));
			}
			RecalculatePath();
		}

		protected override void OnDeserialized(StreamingContext context)
		{
			RecalculatePath();
		}

		/// <summary>
		///     Here we recalculate the freehand path by smoothing out the lines with Beziers.
		/// </summary>
		private void RecalculatePath()
		{
			_isRecalculated = true;
			// Dispose the previous path, if we have one
			if (_freehandPath != null)
			{
				_freehandPath.Dispose();
			}
			_freehandPath = new GraphicsPath();

			// Here we can put some cleanup... like losing all the uninteresting  points.
			if (_capturePoints.Count >= 3)
			{
				int index = 0;
				while ((_capturePoints.Count - 1)%3 != 0)
				{
					// duplicate points, first at 50% than 25% than 75%
					_capturePoints.Insert((int) (_capturePoints.Count*PointOffset[index]), _capturePoints[(int) (_capturePoints.Count*PointOffset[index++])]);
				}
				_freehandPath.AddBeziers(_capturePoints.ToArray());
			}
			else if (_capturePoints.Count == 2)
			{
				_freehandPath.AddLine(_capturePoints[0], _capturePoints[1]);
			}

			// Recalculate the bounds
			_myBounds = Rectangle.Round(_freehandPath.GetBounds());
		}

		public override void Transform(Matrix matrix)
		{
			Point[] points = _capturePoints.ToArray();

			matrix.TransformPoints(points);
			_capturePoints.Clear();
			_capturePoints.AddRange(points);
			RecalculatePath();
		}
	}
}