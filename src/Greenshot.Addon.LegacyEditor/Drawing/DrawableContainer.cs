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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addon.LegacyEditor.Drawing.Adorners;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addon.LegacyEditor.Drawing.Filters;
using Greenshot.Addon.LegacyEditor.Memento;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Addons.Interfaces.Drawing.Adorners;
using Greenshot.Gfx;
using Greenshot.Gfx.Legacy;

namespace Greenshot.Addon.LegacyEditor.Drawing
{
	/// <summary>
	///     represents a rectangle, ellipse, label or whatever. Can contain filters, too.
	///     serializable for clipboard support
	///     Subclasses should fulfill INotifyPropertyChanged contract, i.e. call
	///     OnPropertyChanged whenever a public property has been changed.
	/// </summary>
	[Serializable]
	public abstract class DrawableContainer : AbstractFieldHolderWithChildren, IDrawableContainer
	{
		private const int M11 = 0;
		private const int M22 = 3;
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		///     List of available Adorners
		/// </summary>
		[NonSerialized] private IList<IAdorner> _adorners = new List<IAdorner>();

		[NonSerialized]
		// "workbench" rectangle - used for calculating bounds during resizing (to be applied to this DrawableContainer afterwards)
		protected NativeRectFloat _boundsAfterResize = NativeRectFloat.Empty;

		[NonSerialized]
		// will store current bounds of this DrawableContainer before starting a resize
		protected NativeRect _boundsBeforeResize = NativeRect.Empty;

		protected EditStatus _defaultEditMode = EditStatus.Drawing;

		[NonSerialized] internal Surface _parent;

		[NonSerialized] private PropertyChangedEventHandler _propertyChanged;

		[NonSerialized] private bool _selected;

		[NonSerialized] private EditStatus _status = EditStatus.Undrawn;

		[NonSerialized] private TargetAdorner _targetAdorner;

		private bool _accountForShadowChange;

		private int _height;


		private int _left;

		private int _top;

		private int _width;

		public DrawableContainer(Surface parent, IEditorConfiguration editorConfiguration) : base(editorConfiguration)
		{
			InitializeFields();
			_parent = parent;
		}

		public IList<IFilter> Filters
		{
			get
			{
				var ret = new List<IFilter>();
				foreach (var c in Children)
				{
					if (c is IFilter)
					{
						ret.Add(c as IFilter);
					}
				}
				return ret;
			}
		}

        /// <summary>
        /// The adorner for the target
        /// </summary>
		public TargetAdorner TargetAdorner
		{
			get { return _targetAdorner; }
		}

        /// <summary>
        /// Specifies if this contain has a context menu
        /// </summary>
		public virtual bool HasContextMenu
		{
			get { return true; }
		}

        /// <summary>
        /// Specifies if this container has a default size
        /// </summary>
		public virtual bool HasDefaultSize
		{
			get { return false; }
		}

        /// <summary>
        /// The default size
        /// </summary>
		public virtual Size DefaultSize
		{
			get { throw new NotSupportedException("Object doesn't have a default size"); }
		}

        /// <summary>
        /// This specifies the edit status
        /// </summary>
		public EditStatus DefaultEditMode
		{
			get { return _defaultEditMode; }
		}

		/// <summary>
		///     The public accessible Dispose
		///     Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

        /// <summary>
        /// The property change event is triggered when a property is changed
        /// </summary>
		public event PropertyChangedEventHandler PropertyChanged
		{
			add { _propertyChanged += value; }
			remove { _propertyChanged -= value; }
		}

        /// <summary>
        /// The surface this container belongs to
        /// </summary>
		public ISurface Parent
		{
			get { return _parent; }
			set { SwitchParent((Surface) value); }
		}

        /// <summary>
        /// Is this container selected?
        /// </summary>
		public bool Selected
		{
			get { return _selected; }
			set
			{
				_selected = value;
				OnPropertyChanged("Selected");
			}
		}

		public EditStatus Status
		{
			get { return _status; }
			set { _status = value; }
		}

		public int Left
		{
			get { return _left; }
			set
			{
				if (value == _left)
				{
					return;
				}
				_left = value;
			}
		}

		public int Top
		{
			get { return _top; }
			set
			{
				if (value == _top)
				{
					return;
				}
				_top = value;
			}
		}

		public int Width
		{
			get { return _width; }
			set
			{
				if (value == _width)
				{
					return;
				}
				_width = value;
			}
		}

		public int Height
		{
			get { return _height; }
			set
			{
				if (value == _height)
				{
					return;
				}
				_height = value;
			}
		}

		public NativePoint Location
		{
			get { return new NativePoint(_left, _top); }
			set
			{
				_left = value.X;
				_top = value.Y;
			}
		}

		public Size Size
		{
			get { return new Size(_width, _height); }
			set
			{
				_width = value.Width;
				_height = value.Height;
			}
		}

		public IList<IAdorner> Adorners
		{
			get { return _adorners; }
		}

		public NativeRect Bounds
		{
			get { return new NativeRect(Left, Top, Width, Height).Normalize(); }
			set
			{
				Left = value.Left;
				Top = value.Top;
				Width = value.Width;
				Height = value.Height;
			}
		}

		public virtual void ApplyBounds(NativeRect newBounds)
		{
			Left = Round(newBounds.Left);
			Top = Round(newBounds.Top);
			Width = Round(newBounds.Width);
			Height = Round(newBounds.Height);
		}

		public virtual NativeRect DrawingBounds
		{
			get
			{
				foreach (var filter in Filters)
				{
					if (filter.Invert)
					{
						return new NativeRect(NativePoint.Empty, _parent.Screenshot.Size);
					}
				}
				// Take a base safetymargin
				var lineThickness = 5;
				if (HasField(FieldTypes.LINE_THICKNESS))
				{
					lineThickness += GetFieldValueAsInt(FieldTypes.LINE_THICKNESS);
				}
				var offset = lineThickness / 2;

				var shadow = 0;
				if (_accountForShadowChange || HasField(FieldTypes.SHADOW) && GetFieldValueAsBool(FieldTypes.SHADOW))
				{
					_accountForShadowChange = false;
					shadow += 10;
				}
				return new NativeRect(Bounds.Left - offset, Bounds.Top - offset, Bounds.Width + lineThickness + shadow, Bounds.Height + lineThickness + shadow);
			}
		}

		public virtual void Invalidate()
		{
			if (Status != EditStatus.Undrawn)
			{
				_parent?.Invalidate(DrawingBounds);
			}
		}

		public void AlignToParent(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment)
		{
			if (_parent == null)
			{
				return;
			}
			var lineThickness = GetFieldValueAsInt(FieldTypes.LINE_THICKNESS);
			if (horizontalAlignment == HorizontalAlignment.Left)
			{
				Left = lineThickness / 2;
			}
			if (horizontalAlignment == HorizontalAlignment.Right)
			{
				Left = _parent.Width - Width - lineThickness / 2;
			}
			if (horizontalAlignment == HorizontalAlignment.Center)
			{
				Left = _parent.Width / 2 - Width / 2 - lineThickness / 2;
			}

			if (verticalAlignment == VerticalAlignment.Top)
			{
				Top = lineThickness / 2;
			}
			if (verticalAlignment == VerticalAlignment.Bottom)
			{
				Top = _parent.Height - Height - lineThickness / 2;
			}
			if (verticalAlignment == VerticalAlignment.Center)
			{
				Top = _parent.Height / 2 - Height / 2 - lineThickness / 2;
			}
		}

		public virtual bool InitContent()
		{
			return true;
		}

		public bool HasFilters
		{
			get { return Filters.Count > 0; }
		}

		public virtual bool ClickableAt(int x, int y)
		{
			return new NativeRect(Left, Top, Width, Height).Normalize().Inflate(5, 5).Contains(x, y);
		}

		/// <summary>
		///     Make a following bounds change on this drawablecontainer undoable!
		/// </summary>
		/// <param name="allowMerge">true means allow the moves to be merged</param>
		public void MakeBoundsChangeUndoable(bool allowMerge)
		{
			_parent.MakeUndoable(new DrawableContainerBoundsChangeMemento(this), allowMerge);
		}

		public void MoveBy(int dx, int dy)
		{
			Left += dx;
			Top += dy;
		}

		/// <summary>
		///     A handler for the MouseDown, used if you don't want the surface to handle this for you
		/// </summary>
		/// <param name="x">current mouse x</param>
		/// <param name="y">current mouse y</param>
		/// <returns>true if the event is handled, false if the surface needs to handle it</returns>
		public virtual bool HandleMouseDown(int x, int y)
		{
		    _boundsBeforeResize = _boundsBeforeResize.MoveTo(x, y);
            Left = x;
			Top = y;
			return true;
		}

		/// <summary>
		///     A handler for the MouseMove, used if you don't want the surface to handle this for you
		/// </summary>
		/// <param name="x">current mouse x</param>
		/// <param name="y">current mouse y</param>
		/// <returns>true if the event is handled, false if the surface needs to handle it</returns>
		public virtual bool HandleMouseMove(int x, int y)
		{
			Invalidate();

            // reset "workrbench" rectangle to current bounds
		    _boundsAfterResize = _boundsBeforeResize;
			ScaleHelper.Scale(_boundsBeforeResize, x, y, ref _boundsAfterResize, GetAngleRoundProcessor());

			// apply scaled bounds to this DrawableContainer
			ApplyBounds(_boundsAfterResize.Round());

			Invalidate();
			return true;
		}

		/// <summary>
		///     A handler for the MouseUp
		/// </summary>
		/// <param name="x">current mouse x</param>
		/// <param name="y">current mouse y</param>
		public virtual void HandleMouseUp(int x, int y)
		{
		}

		/// <summary>
		///     This method is called on a DrawableContainers when:
		///     1) The capture on the surface is modified in such a way, that the elements would not be placed correctly.
		///     2) Currently not implemented: an element needs to be moved, scaled or rotated.
		///     This basis implementation makes sure the coordinates of the element, including the TargetGripper, is correctly
		///     rotated/scaled/translated.
		///     But this implementation doesn't take care of any changes to the content!!
		/// </summary>
		/// <param name="matrix"></param>
		public virtual void Transform(Matrix matrix)
		{
			if (matrix == null)
			{
				return;
			}
			var topLeft = new NativePoint(Left, Top);
			var bottomRight = new NativePoint(Left + Width, Top + Height);
			Point[] points = {topLeft, bottomRight};
			matrix.TransformPoints(points);

			Left = points[0].X;
			Top = points[0].Y;
			Width = points[1].X - points[0].X;
			Height = points[1].Y - points[0].Y;
		}

		[OnDeserialized]
		private void OnDeserializedInit(StreamingContext context)
		{
			_adorners = new List<IAdorner>();
			OnDeserialized(context);
		}

		/// <summary>
		///     Override to implement your own deserialization logic, like initializing properties which are not serialized
		/// </summary>
		/// <param name="streamingContext"></param>
		protected virtual void OnDeserialized(StreamingContext streamingContext)
		{
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}
			_parent?.FieldAggregator?.UnbindElement(this);
		}

		~DrawableContainer()
		{
			Dispose(false);
		}

		public void Add(IFilter filter)
		{
			AddChild(filter);
		}

		public void Remove(IFilter filter)
		{
			RemoveChild(filter);
		}

		private static int Round(float f)
		{
			if (float.IsPositiveInfinity(f) || f > int.MaxValue / 2)
			{
				return int.MaxValue / 2;
			}
			if (float.IsNegativeInfinity(f) || f < int.MinValue / 2)
			{
				return int.MinValue / 2;
			}
			return (int) Math.Round(f);
		}

        /// <summary>
        /// This is called when the container is double clicked
        /// </summary>
		public virtual void OnDoubleClick()
		{
		}

		/// <summary>
		///     Initialize a target gripper
		/// </summary>
		protected void InitAdorner(Color gripperColor, NativePoint location)
		{
            // TODO: Pass the gripperColor to the target adorner
			_targetAdorner = new TargetAdorner(this, location, gripperColor);
			Adorners.Add(_targetAdorner);
		}

		/// <summary>
		///     Create the default adorners for a rectangle based container
		/// </summary>
		protected void CreateDefaultAdorners()
		{
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

		public abstract void Draw(Graphics graphics, RenderMode renderMode);

		public virtual void DrawContent(Graphics graphics, IBitmapWithNativeSupport bmp, RenderMode renderMode, NativeRect clipRectangle)
		{
			if (Children.Count > 0)
			{
				if (Status != EditStatus.Idle)
				{
					DrawSelectionBorder(graphics, Bounds);
				}
				else
				{
					if (clipRectangle.Width != 0 && clipRectangle.Height != 0)
					{
						foreach (var filter in Filters)
						{
							if (filter.Invert)
							{
								filter.Apply(graphics, bmp, Bounds, renderMode);
							}
							else
							{
								var drawingRect = new NativeRect(Bounds.Location, Bounds.Size);
							    drawingRect = drawingRect.Intersect(clipRectangle);
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

		protected void DrawSelectionBorder(Graphics g, NativeRect rect)
		{
			using (var pen = new Pen(Color.MediumSeaGreen))
			{
				pen.DashPattern = new float[] {1, 2};
				pen.Width = 1;
				g.DrawRectangle(pen, rect);
			}
		}


		public void ResizeTo(int width, int height)
		{
			Width = width;
			Height = height;
		}

		protected virtual void SwitchParent(Surface newParent)
		{
			if (newParent == Parent)
			{
				return;
			}
			_parent?.FieldAggregator?.UnbindElement(this);

			_parent = newParent;
			foreach (var filter in Filters)
			{
				filter.Parent = this;
			}
		}

		protected void OnPropertyChanged(string propertyName)
		{
			if (_propertyChanged != null)
			{
				_propertyChanged(this, new PropertyChangedEventArgs(propertyName));
				Invalidate();
			}
		}

		/// <summary>
		///     This method will be called before a field changes.
		///     Using this makes it possible to invalidate the object as is before changing.
		/// </summary>
		/// <param name="fieldToBeChanged">The field to be changed</param>
		/// <param name="newValue">The new value</param>
		public virtual void BeforeFieldChange(IField fieldToBeChanged, object newValue)
		{
			_parent?.MakeUndoable(new ChangeFieldHolderMemento(this, fieldToBeChanged), true);
			Invalidate();
		}

		/// <summary>
		///     Handle the field changed event, this should invalidate the correct bounds (e.g. when shadow comes or goes more
		///     pixels!)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void HandleFieldChanged(object sender, FieldChangedEventArgs e)
		{
			Log.Debug().WriteLine("Field {0} changed", e.Field.FieldType);
			if (Equals(e.Field.FieldType, FieldTypes.SHADOW))
			{
				_accountForShadowChange = true;
			}
		}

		/// <summary>
		///     Retrieve the Y scale from the matrix
		/// </summary>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static float CalculateScaleY(Matrix matrix)
		{
			return matrix.Elements[M22];
		}

		/// <summary>
		///     Retrieve the X scale from the matrix
		/// </summary>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static float CalculateScaleX(Matrix matrix)
		{
			return matrix.Elements[M11];
		}

		/// <summary>
		///     Retrieve the rotation angle from the matrix
		/// </summary>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static int CalculateAngle(Matrix matrix)
		{
			const int M11 = 0;
			const int M21 = 2;
			var radians = Math.Atan2(matrix.Elements[M21], matrix.Elements[M11]);
			return (int) -Math.Round(radians * 180 / Math.PI);
		}

		protected virtual IDoubleProcessor GetAngleRoundProcessor()
		{
			return ScaleHelper.ShapeAngleRoundBehavior.Instance;
		}

		/// <summary>
		///     Allows to override the initializing of the fields, so we can actually have our own defaults
		/// </summary>
		protected virtual void InitializeFields()
		{
		}
	}
}