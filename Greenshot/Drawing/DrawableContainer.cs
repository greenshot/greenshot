/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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

using Greenshot.Configuration;
using Greenshot.Drawing.Adorners;
using Greenshot.Drawing.Fields;
using Greenshot.Drawing.Filters;
using Greenshot.Helpers;
using Greenshot.IniFile;
using Greenshot.Memento;
using Greenshot.Plugin;
using Greenshot.Plugin.Drawing;
using Greenshot.Plugin.Drawing.Adorners;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace Greenshot.Drawing
{
	/// <summary>
	/// represents a rectangle, ellipse, label or whatever. Can contain filters, too.
	/// serializable for clipboard support
	/// Subclasses should fulfill INotifyPropertyChanged contract, i.e. call
	/// OnPropertyChanged whenever a public property has been changed.
	/// </summary>
	[Serializable]
	public abstract class DrawableContainer : AbstractFieldHolderWithChildren, IDrawableContainer {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(DrawableContainer));
		protected static readonly EditorConfiguration EditorConfig = IniConfig.GetIniSection<EditorConfiguration>();
		private const int M11 = 0;
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

		protected EditStatus _defaultEditMode = EditStatus.DRAWING;
		public EditStatus DefaultEditMode {
			get {
				return _defaultEditMode;
			}
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
			if (!disposing) {
				return;
			}

			FieldAggregator aggProps = _parent.FieldAggregator;
			aggProps.UnbindElement(this);
		}

		~DrawableContainer() {
			Dispose(false);
		}

		[NonSerialized]
		private PropertyChangedEventHandler _propertyChanged;
		public event PropertyChangedEventHandler PropertyChanged {
			add { _propertyChanged += value; }
			remove{ _propertyChanged -= value; }
		}
		
		public List<IFilter> Filters {
			get {
				List<IFilter> ret = new List<IFilter>();
				foreach(IFieldHolder c in Children) {
					if (c is IFilter) {
						ret.Add(c as IFilter);
					}
				}
				return ret;
			}
		}
			
		[NonSerialized]
		internal Surface _parent;
		public ISurface Parent {
			get { return _parent; }
			set { SwitchParent((Surface)value); }
		}

		[NonSerialized]
		private TargetAdorner _targetGripper;
		public TargetAdorner TargetGripper {
			get {
				return _targetGripper;
			}
		}

		[NonSerialized]
		private bool _selected;
		public bool Selected {
			get {return _selected;}
			set {
				_selected = value;
				OnPropertyChanged("Selected");
			}
		}
		
		[NonSerialized]
		private EditStatus _status = EditStatus.UNDRAWN;
		public EditStatus Status {
			get {
				return _status;
			}
			set {
				_status = value;
			}
		}

		
		private int left;
		public int Left {
			get { return left; }
			set {
				if (value == left) {
					return;
				}
				left = value;
			}
		}
		
		private int top;
		public int Top {
			get { return top; }
			set {
				if (value == top) {
					return;
				}
				top = value;
			}
		}
		
		private int width;
		public int Width {
			get { return width; }
			set {
				if (value == width) {
					return;
				}
				width = value;
			}
		}
		
		private int height;
		public int Height {
			get { return height; }
			set {
				if (value == height) {
					return;
				}
				height = value;
			}
		}
		
		public Point Location {
			get {
				return new Point(left, top);
			}
			set {
				left = value.X;
				top = value.Y;
			}
		}

		public Size Size {
			get {
				return new Size(width, height);
			}
			set {
				width = value.Width;
				height = value.Height;
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

		[NonSerialized]
		// will store current bounds of this DrawableContainer before starting a resize
		protected Rectangle _boundsBeforeResize = Rectangle.Empty;
		
		[NonSerialized]
		// "workbench" rectangle - used for calculating bounds during resizing (to be applied to this DrawableContainer afterwards)
		protected RectangleF _boundsAfterResize = RectangleF.Empty;
		
		public Rectangle Bounds {
			get { return GuiRectangle.GetGuiRectangle(Left, Top, Width, Height); }
			set {
				Left = Round(value.Left);
				Top = Round(value.Top);
				Width = Round(value.Width);
				Height = Round(value.Height);
			}
		}
		
		public virtual void ApplyBounds(RectangleF newBounds) {
			Left = Round(newBounds.Left);
			Top = Round(newBounds.Top);
			Width = Round(newBounds.Width);
			Height = Round(newBounds.Height);
		}
		
		public DrawableContainer(Surface parent) {
			InitializeFields();
			_parent = parent;
		}

		public void Add(IFilter filter) {
			AddChild(filter);
		}
		
		public void Remove(IFilter filter) {
			RemoveChild(filter);
		}
		
		private static int Round(float f) {
			if(float.IsPositiveInfinity(f) || f>int.MaxValue/2) return int.MaxValue/2;
			if (float.IsNegativeInfinity(f) || f<int.MinValue/2) return int.MinValue/2;
			return (int)Math.Round(f);
		}
		
		private bool accountForShadowChange;
		public virtual Rectangle DrawingBounds {
			get {
				foreach(IFilter filter in Filters) {
					if (filter.Invert) {
						return new Rectangle(Point.Empty, _parent.Image.Size);
					}
				}
				// Take a base safetymargin
				int lineThickness = 5;
				if (HasField(FieldType.LINE_THICKNESS)) {
					lineThickness += GetFieldValueAsInt(FieldType.LINE_THICKNESS);
				}
				int offset = lineThickness/2;

				int shadow = 0;
				if (accountForShadowChange || (HasField(FieldType.SHADOW) && GetFieldValueAsBool(FieldType.SHADOW))){
					accountForShadowChange = false;
					shadow += 10;
				}
				return new Rectangle(Bounds.Left-offset, Bounds.Top-offset, Bounds.Width+lineThickness+shadow, Bounds.Height+lineThickness+shadow);
			}
		}

		public virtual void Invalidate() {
			if (Status != EditStatus.UNDRAWN) {
				_parent.Invalidate(DrawingBounds);
			}
		}
		
		public void AlignToParent(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment) {
			
			int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
			if (horizontalAlignment == HorizontalAlignment.Left) {
				Left = lineThickness/2;
			}
			if (horizontalAlignment == HorizontalAlignment.Right) {
				Left = _parent.Width - Width - lineThickness/2;
			}
			if (horizontalAlignment == HorizontalAlignment.Center) {
				Left = (_parent.Width / 2) - (Width / 2) - lineThickness/2;
			}

			if (verticalAlignment == VerticalAlignment.TOP) {
				Top = lineThickness/2;
			}
			if (verticalAlignment == VerticalAlignment.BOTTOM) {
				Top = _parent.Height - Height - lineThickness/2;
			}
			if (verticalAlignment == VerticalAlignment.CENTER) {
				Top = (_parent.Height / 2) - (Height / 2) - lineThickness/2;
			}
		}
		
		public virtual bool InitContent() { return true; }
		
		public virtual void OnDoubleClick() {}

		/// <summary>
		/// Initialize a target gripper
		/// </summary>
		protected void InitTargetGripper(Color gripperColor, Point location) {
			_targetGripper = new TargetAdorner(this, location);
			Adorners.Add(_targetGripper);
		}

		/// <summary>
		/// Create the default adorners for a rectangle based container
		/// </summary>

		protected void CreateDefaultAdorners() {
			if (Adorners.Count > 0)
			{
				LOG.Warn("Adorners are already defined!");
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

		public bool hasFilters {
			get {
				return Filters.Count > 0;
			}
		}

		public abstract void Draw(Graphics graphics, RenderMode renderMode);
		
		public virtual void DrawContent(Graphics graphics, Bitmap bmp, RenderMode renderMode, Rectangle clipRectangle) {
			if (Children.Count > 0) {
				if (Status != EditStatus.IDLE) {
					DrawSelectionBorder(graphics, Bounds);
				} else {
					if (clipRectangle.Width != 0 && clipRectangle.Height != 0) {
						foreach(IFilter filter in Filters) {
							if (filter.Invert) {
								filter.Apply(graphics, bmp, Bounds, renderMode);
							} else {
								Rectangle drawingRect = new Rectangle(Bounds.Location, Bounds.Size);
								drawingRect.Intersect(clipRectangle);
								if(filter is MagnifierFilter) {
                                    // quick&dirty bugfix, because MagnifierFilter behaves differently when drawn only partially
                                    // what we should actually do to resolve this is add a better magnifier which is not that special
                                    filter.Apply(graphics, bmp, Bounds, renderMode);
                                } else {
                                    filter.Apply(graphics, bmp, drawingRect, renderMode);
                                }
							}
						}
					}
	
				}
			}
			Draw(graphics, renderMode);
		}
		
		public virtual bool Contains(int x, int y) {
			return Bounds.Contains(x , y);
		}
		
		public virtual bool ClickableAt(int x, int y) {
			Rectangle r = GuiRectangle.GetGuiRectangle(Left, Top, Width, Height);
			r.Inflate(5, 5);
			return r.Contains(x, y);
		}
		
		protected void DrawSelectionBorder(Graphics g, Rectangle rect) {
			using (Pen pen = new Pen(Color.MediumSeaGreen)) {
				pen.DashPattern = new float[]{1,2};
				pen.Width = 1;
				g.DrawRectangle(pen, rect);
			}
		}
		

		public void ResizeTo(int width, int height, int anchorPosition) {
			Width = width;
			Height = height;
		}

		/// <summary>
		/// Make a following bounds change on this drawablecontainer undoable!
		/// </summary>
		/// <param name="allowMerge">true means allow the moves to be merged</param>
		public void MakeBoundsChangeUndoable(bool allowMerge) {
			_parent.MakeUndoable(new DrawableContainerBoundsChangeMemento(this), allowMerge);
		}
		
		public void MoveBy(int dx, int dy) {
			Left += dx;
			Top += dy;
		}
		
		/// <summary>
		/// A handler for the MouseDown, used if you don't want the surface to handle this for you
		/// </summary>
		/// <param name="x">current mouse x</param>
		/// <param name="y">current mouse y</param>
		/// <returns>true if the event is handled, false if the surface needs to handle it</returns>
		public virtual bool HandleMouseDown(int x, int y) {
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
		public virtual bool HandleMouseMove(int x, int y) {
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
		public virtual void HandleMouseUp(int x, int y) {
		}
		
		protected virtual void SwitchParent(Surface newParent) {

			_parent = newParent;
			foreach(IFilter filter in Filters) {
				filter.Parent = this;
			}
		}
		
		// drawablecontainers are regarded equal if they are of the same type and their bounds are equal. this should be sufficient.
		public override bool Equals(object obj) {
			bool ret = false;
			if (obj != null && GetType() == obj.GetType()) {
				DrawableContainer other = obj as DrawableContainer;
				if (other != null && left==other.left && top==other.top && width==other.width && height==other.height) {
					ret = true;
				}
			}
			return ret;
		}
		
		public override int GetHashCode() {
			// TODO: This actually doesn't make sense...
			// Place the container in a list, and you can't find it :)
			return left.GetHashCode() ^ top.GetHashCode() ^ width.GetHashCode() ^ height.GetHashCode() ^ GetFields().GetHashCode();
		}
		
		protected void OnPropertyChanged(string propertyName) {
			if (_propertyChanged != null) {
				_propertyChanged(this, new PropertyChangedEventArgs(propertyName));
				Invalidate();
			}
		}
		
		/// <summary>
		/// This method will be called before a field is changes.
		/// Using this makes it possible to invalidate the object as is before changing.
		/// </summary>
		/// <param name="fieldToBeChanged">The field to be changed</param>
		/// <param name="newValue">The new value</param>
		public virtual void BeforeFieldChange(Field fieldToBeChanged, object newValue) {
			_parent.MakeUndoable(new ChangeFieldHolderMemento(this, fieldToBeChanged), true);
			Invalidate();
		}
		
		/// <summary>
		/// Handle the field changed event, this should invalidate the correct bounds (e.g. when shadow comes or goes more pixels!)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void HandleFieldChanged(object sender, FieldChangedEventArgs e) {
			LOG.DebugFormat("Field {0} changed", e.Field.FieldType);
			if (e.Field.FieldType == FieldType.SHADOW) {
				accountForShadowChange = true;
			}
			Invalidate();
		}

		/// <summary>
		/// Retrieve the Y scale from the matrix
		/// </summary>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static float CalculateScaleY(Matrix matrix) {
			return matrix.Elements[M22];
		}

		/// <summary>
		/// Retrieve the X scale from the matrix
		/// </summary>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static float CalculateScaleX(Matrix matrix) {
			return matrix.Elements[M11];
		}

		/// <summary>
		/// Retrieve the rotation angle from the matrix
		/// </summary>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static int CalculateAngle(Matrix matrix) {
			const int M11 = 0;
			const int M21 = 2;
			var radians = Math.Atan2(matrix.Elements[M21], matrix.Elements[M11]);
			return (int)-Math.Round(radians * 180 / Math.PI);
		}

		/// <summary>
		/// This method is called on a DrawableContainers when:
		/// 1) The capture on the surface is modified in such a way, that the elements would not be placed correctly.
		/// 2) Currently not implemented: an element needs to be moved, scaled or rotated.
		/// This basis implementation makes sure the coordinates of the element, including the TargetGripper, is correctly rotated/scaled/translated.
		/// But this implementation doesn't take care of any changes to the content!!
		/// </summary>
		/// <param name="matrix"></param>
		public virtual void Transform(Matrix matrix) {
			if (matrix == null) {
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

		protected virtual ScaleHelper.IDoubleProcessor GetAngleRoundProcessor() {
			return ScaleHelper.ShapeAngleRoundBehavior.Instance;
		}
		
		public virtual bool HasContextMenu {
			get {
				return true;
			}
		}

		public virtual bool HasDefaultSize {
			get {
				return false;
			}
		}

		public virtual Size DefaultSize {
			get {
				throw new NotSupportedException("Object doesn't have a default size");
			}
		}

		/// <summary>
		/// Allows to override the initializing of the fields, so we can actually have our own defaults
		/// </summary>
		protected virtual void InitializeFields() {
		}
	}
}