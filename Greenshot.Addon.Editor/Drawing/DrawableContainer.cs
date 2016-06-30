/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
using System.Runtime.Serialization;
using System.Windows.Forms;
using Dapplo.Config.Ini;
using Greenshot.Addon.Editor.Drawing.Fields;
using Greenshot.Addon.Editor.Drawing.Filters;
using Greenshot.Addon.Editor.Helpers;
using Greenshot.Addon.Editor.Memento;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Drawing;
using Dapplo.Log.Facade;
using Greenshot.Addon.Editor.Drawing.Adorners;

namespace Greenshot.Addon.Editor.Drawing
{
	/// <summary>
	/// represents a rectangle, ellipse, label or whatever. Can contain filters, too.
	/// serializable for clipboard support
	/// Subclasses should fulfill INotifyPropertyChanged contract, i.e. call
	/// OnPropertyChanged whenever a public property has been changed.
	/// </summary>
	[Serializable]
	public abstract class DrawableContainer : AbstractFieldHolder, IDrawableContainer
	{
		private static readonly LogSource Log = new LogSource();
		protected static readonly IEditorConfiguration EditorConfig = IniConfig.Current.Get<IEditorConfiguration>();
		private bool isMadeUndoable;
		private const int M11 = 0;
		private const int M12 = 1;
		private const int M21 = 2;
		private const int M22 = 3;

		[OnDeserialized]
		private void OnDeserializedInit(StreamingContext context)
		{
			_adorners = new List<IAdorner>();
			OnDeserialized(context);
		}

		/// <summary>
		/// Override to implement your own deserialization logic, like initializing properties which are not serialized
		/// </summary>
		/// <param name="streamingContext"></param>
		protected virtual void OnDeserialized(StreamingContext streamingContext)
		{
		}

		/// <summary>
		/// The public accessible Dispose
		/// Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			// Do nothing, this is overriden
		}

		~DrawableContainer() {
			Dispose(false);
		}

		[NonSerialized]
		private EditStatus _defaultEditMode = EditStatus.DRAWING;

		public EditStatus DefaultEditMode
		{
			get
			{
				return _defaultEditMode;
			}
			protected set
			{
				_defaultEditMode = value;
			}
		}

		[NonSerialized]
		private bool _isMadeUndoable;

		private readonly List<IFilter> _filters = new List<IFilter>();

		public List<IFilter> Filters
		{
			get
			{
				return _filters;
			}
		}

		[NonSerialized]
		internal Surface _parent;

		public ISurface Parent
		{
			get
			{
				return _parent;
			}
			set
			{
				SwitchParent((Surface) value);
			}
		}

		[NonSerialized]
		private TargetAdorner _targetAdorner;
		public TargetAdorner TargetAdorner {
			get {
				return _targetAdorner;
			}
		}

		[NonSerialized]
		private bool _selected;

		public bool Selected
		{
			get
			{
				return _selected;
			}
			set
			{
				_selected = value;
				OnPropertyChanged("Selected");
			}
		}

		[NonSerialized]
		private EditStatus _status = EditStatus.UNDRAWN;

		public EditStatus Status
		{
			get
			{
				return _status;
			}
			set
			{
				_status = value;
			}
		}


		private int _left;

		public int Left
		{
			get
			{
				return _left;
			}
			set
			{
				_left = value;
			}
		}

		private int _top;

		public int Top
		{
			get
			{
				return _top;
			}
			set
			{
				if (value != _top)
				{
					_top = value;
				}
			}
		}

		private int _width;

		public int Width
		{
			get
			{
				return _width;
			}
			set
			{
				if (value != _width)
				{
					_width = value;
				}
			}
		}

		private int _height;

		public int Height
		{
			get
			{
				return _height;
			}
			set
			{
				if (value != _height)
				{
					_height = value;
				}
			}
		}

		public Point Location
		{
			get
			{
				return new Point(_left, _top);
			}
		}

		public Size Size
		{
			get
			{
				return new Size(_width, _height);
			}
		}

		/// <summary>
		/// List of available Adorners
		/// </summary>
		[NonSerialized]
		private IList<IAdorner> _adorners = new List<IAdorner>();
		public IList<IAdorner> Adorners
		{
			get
			{
				return _adorners;
			}
		}

		/// <summary>
		/// will store current bounds of this DrawableContainer before starting a resize
		/// </summary>
		[NonSerialized]
		protected Rectangle _boundsBeforeResize = Rectangle.Empty;

		/// <summary>
		/// "workbench" rectangle - used for calculatoing bounds during resizing (to be applied to this DrawableContainer afterwards)
		/// </summary>
		[NonSerialized]
		protected RectangleF _boundsAfterResize = RectangleF.Empty;

		public Rectangle Bounds
		{
			get
			{
				return new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();
			}
			set
			{
				Left = Round(value.Left);
				Top = Round(value.Top);
				Width = Round(value.Width);
				Height = Round(value.Height);
			}
		}

		public virtual void ApplyBounds(RectangleF newBounds)
		{
			Left = Round(newBounds.Left);
			Top = Round(newBounds.Top);
			Width = Round(newBounds.Width);
			Height = Round(newBounds.Height);
		}

		/// <summary>
		/// Don't allow default constructor!
		/// </summary>
		private DrawableContainer()
		{
		}

		/// <summary>
		/// a drawable container is always linked to a surface
		/// </summary>
		/// <param name="parent"></param>
		public DrawableContainer(Surface parent)
		{
			_parent = parent;
			InitFieldAttributes();
		}

		public void Add(IFilter filter)
		{
			_filters.Add(filter);
		}

		public void Remove(IFilter filter)
		{
			_filters.Remove(filter);
		}

		private int Round(float f)
		{
			if (float.IsPositiveInfinity(f) || f > int.MaxValue/2)
			{
				return int.MaxValue/2;
			}
			if (float.IsNegativeInfinity(f) || f < int.MinValue/2)
			{
				return int.MinValue/2;
			}
			return (int) Math.Round(f);
		}

		private int InternalLineThickness
		{
			get
			{
				FieldAttribute fieldAttribute;
				if (FieldAttributes.TryGetValue(FieldTypes.LINE_THICKNESS, out fieldAttribute))
				{
					return (int) fieldAttribute.GetValue(this);
				}
				return 0;
			}
		}

		private bool HasShadow
		{
			get
			{
				FieldAttribute fieldAttribute;
				if (FieldAttributes.TryGetValue(FieldTypes.SHADOW, out fieldAttribute))
				{
					return (bool) fieldAttribute.GetValue(this);
				}
				return false;
			}
		}

		public virtual Rectangle DrawingBounds
		{
			get
			{
				foreach (IFilter filter in Filters)
				{
					if (filter.Invert)
					{
						return new Rectangle(Point.Empty, _parent.Image.Size);
					}
				}
				// Take a base safetymargin
				int lineThickness = 5 + InternalLineThickness;
				int offset = lineThickness/2;

				int shadow = 0;
				if (HasShadow)
				{
					shadow += 10;
				}
				return new Rectangle(Bounds.Left - offset, Bounds.Top - offset, Bounds.Width + lineThickness + shadow, Bounds.Height + lineThickness + shadow);
			}
		}

		public override void Invalidate()
		{
			if (Status != EditStatus.UNDRAWN)
			{
				_parent.Invalidate(DrawingBounds);
			}
		}

		public void AlignToParent(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment)
		{
			if (_parent == null)
			{
				return;
			}

			int lineThickness = InternalLineThickness;
			if (horizontalAlignment == HorizontalAlignment.Left)
			{
				Left = lineThickness/2;
			}
			if (horizontalAlignment == HorizontalAlignment.Right)
			{
				Left = _parent.Width - Width - lineThickness/2;
			}
			if (horizontalAlignment == HorizontalAlignment.Center)
			{
				Left = (_parent.Width/2) - (Width/2) - lineThickness/2;
			}

			if (verticalAlignment == VerticalAlignment.TOP)
			{
				Top = lineThickness/2;
			}
			if (verticalAlignment == VerticalAlignment.BOTTOM)
			{
				Top = _parent.Height - Height - lineThickness/2;
			}
			if (verticalAlignment == VerticalAlignment.CENTER)
			{
				Top = (_parent.Height/2) - (Height/2) - lineThickness/2;
			}
		}

		public virtual bool InitContent()
		{
			return true;
		}

		public virtual void OnDoubleClick()
		{
		}

		/// <summary>
		/// Initialize a target gripper
		/// </summary>
		protected void InitAdorner(Color gripperColor, Point location) {
			_targetAdorner = new TargetAdorner(this, location);
			Adorners.Add(_targetAdorner);
		}

		/// <summary>
		/// Create the default adorners for a rectangle based container
		/// </summary>

		protected void CreateDefaultAdorners() {
			if (Adorners.Count > 0)
			{
				Log.Warn().WriteLine("Adorners are already defined!");
			}
			// Create the GripperAdorners
			Adorners.Add(new ResizeAdorner(this, Positions.TopLeft));
			Adorners.Add(new ResizeAdorner(this, Positions.TopCenter));
			Adorners.Add(new ResizeAdorner(this, Positions.TopRight));
			Adorners.Add(new ResizeAdorner(this, Positions.BottomLeft));
			Adorners.Add(new ResizeAdorner(this, Positions.BottomCenter));
			Adorners.Add(new ResizeAdorner(this, Positions.BottomRight));
			Adorners.Add(new ResizeAdorner(this, Positions.MiddleLeft));
			Adorners.Add(new ResizeAdorner(this, Positions.MiddleRight));
		}

		public bool HasFilters
		{
			get
			{
				return Filters.Count > 0;
			}
		}

		public abstract void Draw(Graphics graphics, RenderMode renderMode);

		public virtual void DrawContent(Graphics graphics, Bitmap bmp, RenderMode renderMode, Rectangle clipRectangle)
		{
			if (Filters.Count > 0)
			{
				if (Status != EditStatus.IDLE)
				{
					DrawSelectionBorder(graphics, Bounds);
				}
				else
				{
					if (clipRectangle.Width != 0 && clipRectangle.Height != 0)
					{
						foreach (IFilter filter in Filters)
						{
							if (filter.Invert)
							{
								filter.Apply(graphics, bmp, Bounds, renderMode);
							}
							else
							{
								Rectangle drawingRect = new Rectangle(Bounds.Location, Bounds.Size);
								drawingRect.Intersect(clipRectangle);
								if (filter is MagnifierFilter)
								{
									// quick&dirty bugfix, because MagnifierFilter behaves differently when drawn only partially
									// what we should actually do to resolve this is add a better magnifier which is not that special
									filter.Apply(graphics, bmp, Bounds, renderMode);
								}
								else
								{
									filter.Apply(graphics, bmp, drawingRect, renderMode);
								}
							}
						}
					}
				}
			}
			Draw(graphics, renderMode);
		}

		public virtual bool Contains(int x, int y)
		{
			return Bounds.Contains(x, y);
		}

		public virtual bool ClickableAt(int x, int y)
		{
			Rectangle r = new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();
			r.Inflate(5, 5);
			return r.Contains(x, y);
		}

		protected void DrawSelectionBorder(Graphics g, Rectangle rect)
		{
			using (Pen pen = new Pen(Color.MediumSeaGreen))
			{
				pen.DashPattern = new float[]
				{
					1, 2
				};
				pen.Width = 1;
				g.DrawRectangle(pen, rect);
			}
		}

		public void ResizeTo(int width, int height, int anchorPosition)
		{
			Width = width;
			Height = height;
		}

		/// <summary>
		/// Make a following bounds change on this drawablecontainer undoable!
		/// </summary>
		/// <param name="allowMerge">true means allow the moves to be merged</param>
		public void MakeBoundsChangeUndoable(bool allowMerge)
		{
			if (_parent != null)
			{
				_parent.MakeUndoable(new DrawableContainerBoundsChangeMemento(this), allowMerge);
			}
		}

		public void MoveBy(int dx, int dy)
		{
			Left += dx;
			Top += dy;
		}

		/// <summary>
		/// A handler for the MouseDown, used if you don't want the surface to handle this for you
		/// </summary>
		/// <param name="x">current mouse x</param>
		/// <param name="y">current mouse y</param>
		/// <returns>true if the event is handled, false if the surface needs to handle it</returns>
		public virtual bool HandleMouseDown(int x, int y)
		{
			Left = _boundsBeforeResize.X = x;
			Top = _boundsBeforeResize.Y = y;
			return true;
		}

		/// <summary>
		/// A handler for the MouseMove, used if you don't want the surface to handle this for you
		/// </summary>
		/// <param name="x">current mouse x</param>
		/// <param name="y">current mouse y</param>
		/// <returns>true if the event is handled, false if the surface needs to handle it</returns>
		public virtual bool HandleMouseMove(int x, int y)
		{
			Invalidate();

			// reset "workrbench" rectangle to current bounds
			_boundsAfterResize.X = _boundsBeforeResize.Left;
			_boundsAfterResize.Y = _boundsBeforeResize.Top;
			_boundsAfterResize.Width = x - _boundsAfterResize.Left;
			_boundsAfterResize.Height = y - _boundsAfterResize.Top;

			ScaleHelper.Scale(_boundsBeforeResize, x, y, ref _boundsAfterResize, GetAngleRoundProcessor());

			// apply scaled bounds to this DrawableContainer
			ApplyBounds(_boundsAfterResize);

			Invalidate();
			return true;
		}

		/// <summary>
		/// A handler for the MouseUp
		/// </summary>
		/// <param name="x">current mouse x</param>
		/// <param name="y">current mouse y</param>
		public virtual void HandleMouseUp(int x, int y)
		{
		}

		protected virtual void SwitchParent(Surface newParent)
		{
			if (newParent == Parent)
			{
				return;
			}

			_parent = newParent;

			foreach (IFilter filter in Filters)
			{
				filter.Parent = this;
			}
		}

		// drawablecontainers are regarded equal if they are of the same type and their bounds are equal. this should be sufficient.
		public override bool Equals(object obj)
		{
			bool ret = false;
			if (obj != null && GetType() == obj.GetType())
			{
				DrawableContainer other = obj as DrawableContainer;
				if (other != null && (_left == other._left && _top == other._top && _width == other._width && _height == other._height))
				{
					ret = true;
				}
			}
			return ret;
		}

		public override int GetHashCode()
		{
			return _left.GetHashCode() ^ _top.GetHashCode() ^ _width.GetHashCode() ^ _height.GetHashCode() ^ fieldAttributes.GetHashCode();
		}

		public virtual bool CanRotate
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Retrieve the Y scale from the matrix
		/// </summary>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static float CalculateScaleY(Matrix matrix)
		{
			return matrix.Elements[M22];
		}

		/// <summary>
		/// Retrieve the X scale from the matrix
		/// </summary>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static float CalculateScaleX(Matrix matrix)
		{
			return matrix.Elements[M11];
		}

		/// <summary>
		/// Retrieve the rotation angle from the matrix
		/// </summary>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static int CalculateAngle(Matrix matrix)
		{
			const int M11 = 0;
			const int M21 = 2;
			var radians = Math.Atan2(matrix.Elements[M21], matrix.Elements[M11]);
			return (int) -Math.Round(radians*180/Math.PI);
		}

		/// <summary>
		/// This method is called on a DrawableContainers when:
		/// 1) The capture on the surface is modified in such a way, that the elements would not be placed correctly.
		/// 2) Currently not implemented: an element needs to be moved, scaled or rotated.
		/// This basis implementation makes sure the coordinates of the element, including the TargetGripper, is correctly rotated/scaled/translated.
		/// But this implementation doesn't take care of any changes to the content!!
		/// </summary>
		/// <param name="matrix"></param>
		public virtual void Transform(Matrix matrix)
		{
			if (matrix == null)
			{
				return;
			}
			Point topLeft = new Point(Left, Top);
			Point bottomRight = new Point(Left + Width, Top + Height);
			Point[] points = new[] { topLeft, bottomRight };
			matrix.TransformPoints(points);

			Left = points[0].X;
			Top = points[0].Y;
			Width = points[1].X - points[0].X;
			Height = points[1].Y - points[0].Y;
		}

		protected virtual ScaleHelper.IDoubleProcessor GetAngleRoundProcessor()
		{
			return ScaleHelper.ShapeAngleRoundBehavior.Instance;
		}

		public virtual bool HasContextMenu
		{
			get
			{
				return true;
			}
		}

		public virtual bool HasDefaultSize
		{
			get
			{
				return false;
			}
		}

		public virtual Size DefaultSize
		{
			get
			{
				throw new NotSupportedException("Object doesn't have a default size");
			}
		}
	}
}