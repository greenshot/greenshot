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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Drawing.Adorners;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Drawing.Adorners;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Filters;
using Greenshot.Editor.Helpers;
using Greenshot.Editor.Memento;
using log4net;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// represents a rectangle, ellipse, label or whatever. Can contain filters, too.
    /// serializable for clipboard support
    /// Subclasses should fulfill INotifyPropertyChanged contract, i.e. call
    /// OnPropertyChanged whenever a public property has been changed.
    /// </summary>
    [Serializable]
    public abstract class DrawableContainer : AbstractFieldHolderWithChildren, IDrawableContainer
    {
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

        public EditStatus DefaultEditMode
        {
            get { return _defaultEditMode; }
        }

        /// <summary>
        /// The public accessible Dispose
        /// Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

        [NonSerialized] private PropertyChangedEventHandler _propertyChanged;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }

        public IList<IFilter> Filters
        {
            get
            {
                List<IFilter> ret = new List<IFilter>();
                foreach (IFieldHolder c in Children)
                {
                    if (c is IFilter)
                    {
                        ret.Add(c as IFilter);
                    }
                }

                return ret;
            }
        }

        [NonSerialized] internal ISurface _parent;

        public ISurface Parent
        {
            get => _parent;
            set => SwitchParent(value);
        }

        protected Surface InternalParent
        {
            get => (Surface)_parent;
        }

        [NonSerialized] private TargetAdorner _targetAdorner;
        public TargetAdorner TargetAdorner => _targetAdorner;

        [NonSerialized] private bool _selected;

        public bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                OnPropertyChanged("Selected");
            }
        }

        [NonSerialized] private EditStatus _status = EditStatus.UNDRAWN;

        public EditStatus Status
        {
            get => _status;
            set => _status = value;
        }


        private int left;

        public int Left
        {
            get => left;
            set
            {
                if (value == left)
                {
                    return;
                }

                left = value;
            }
        }

        private int top;

        public int Top
        {
            get => top;
            set
            {
                if (value == top)
                {
                    return;
                }

                top = value;
            }
        }

        private int width;

        public int Width
        {
            get => width;
            set
            {
                if (value == width)
                {
                    return;
                }

                width = value;
            }
        }

        private int height;

        public int Height
        {
            get => height;
            set
            {
                if (value == height)
                {
                    return;
                }

                height = value;
            }
        }

        public NativePoint Location
        {
            get => new NativePoint(left, top);
            set
            {
                left = value.X;
                top = value.Y;
            }
        }

        public NativeSize Size
        {
            get => new NativeSize(width, height);
            set
            {
                width = value.Width;
                height = value.Height;
            }
        }

        /// <summary>
        /// List of available Adorners
        /// </summary>
        [NonSerialized] private IList<IAdorner> _adorners = new List<IAdorner>();

        public IList<IAdorner> Adorners => _adorners;

        [NonSerialized]
        // will store current bounds of this DrawableContainer before starting a resize
        protected NativeRect _boundsBeforeResize = NativeRect.Empty;

        [NonSerialized]
        // "workbench" rectangle - used for calculating bounds during resizing (to be applied to this DrawableContainer afterwards)
        protected NativeRectFloat _boundsAfterResize = NativeRectFloat.Empty;

        public NativeRect Bounds
        {
            get => new NativeRect(Left, Top, Width, Height).Normalize();
            set
            {
                Left = Round(value.Left);
                Top = Round(value.Top);
                Width = Round(value.Width);
                Height = Round(value.Height);
            }
        }

        public virtual void ApplyBounds(NativeRectFloat newBounds)
        {
            Left = Round(newBounds.Left);
            Top = Round(newBounds.Top);
            Width = Round(newBounds.Width);
            Height = Round(newBounds.Height);
        }

        public DrawableContainer(ISurface parent)
        {
            InitializeFields();
            _parent = parent;
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
            if (float.IsPositiveInfinity(f) || f > int.MaxValue / 2) return int.MaxValue / 2;
            if (float.IsNegativeInfinity(f) || f < int.MinValue / 2) return int.MinValue / 2;
            return (int) Math.Round(f);
        }

        private bool accountForShadowChange;

        public virtual NativeRect DrawingBounds
        {
            get
            {
                foreach (IFilter filter in Filters)
                {
                    if (filter.Invert)
                    {
                        return new NativeRect(Point.Empty, _parent.Image.Size);
                    }
                }

                // Take a base safety margin
                int lineThickness = 5;

                // add adorner size
                lineThickness += Adorners.Max(adorner => Math.Max(adorner.Bounds.Width, adorner.Bounds.Height));

                if (HasField(FieldType.LINE_THICKNESS))
                {
                    lineThickness += GetFieldValueAsInt(FieldType.LINE_THICKNESS);
                }

                int offset = lineThickness / 2;

                int shadow = 0;
                if (accountForShadowChange || (HasField(FieldType.SHADOW) && GetFieldValueAsBool(FieldType.SHADOW)))
                {
                    accountForShadowChange = false;
                    shadow += 10;
                }

                return new NativeRect(Bounds.Left - offset, Bounds.Top - offset, Bounds.Width + lineThickness + shadow, Bounds.Height + lineThickness + shadow);
            }
        }

        public virtual void Invalidate()
        {
            if (Status != EditStatus.UNDRAWN)
            {
                _parent?.InvalidateElements(DrawingBounds);
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
        /// <param name="location">NativePoint</param>
        protected void InitTargetAdorner(NativePoint location)
        {
            _targetAdorner = new TargetAdorner(this, location);
            Adorners.Add(_targetAdorner);
        }

        /// <summary>
        /// Create the default adorners for a rectangle based container
        /// </summary>
        protected void CreateDefaultAdorners()
        {
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

        public bool HasFilters => Filters.Count > 0;

        public abstract void Draw(Graphics graphics, RenderMode renderMode);

        public virtual void DrawContent(Graphics graphics, Bitmap bmp, RenderMode renderMode, NativeRect clipRectangle)
        {
            if (Children.Count > 0)
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
                                var drawingRect = new NativeRect(Bounds.Location, Bounds.Size).Intersect(clipRectangle);
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

        /// <summary>
        /// Adjust UI elements to the supplied DPI settings
        /// </summary>
        /// <param name="dpi">uint with dpi value</param>
        public void AdjustToDpi(int dpi)
        {
            foreach (var adorner in Adorners)
            {
                adorner.AdjustToDpi(dpi);
            }
        }

        /// <inheritdoc cref="IDrawableContainer"/>
        public virtual void AddContextMenuItems(ContextMenuStrip menu, ISurface surface, MouseEventArgs mouseEventArgs)
        {
            // Empty as we do not want to add something to the context menu for every element
        }

        public virtual bool Contains(int x, int y)
        {
            return Bounds.Contains(x, y);
        }

        public virtual bool ClickableAt(int x, int y)
        {
            var r = new NativeRect(Left, Top, Width, Height).Normalize();
            r = r.Inflate(5, 5);
            return r.Contains(x, y);
        }

        protected void DrawSelectionBorder(Graphics g, NativeRect rect)
        {
            using Pen pen = new Pen(Color.MediumSeaGreen)
            {
                DashPattern = new float[]
                {
                    1, 2
                },
                Width = 1
            };
            g.DrawRectangle(pen, rect);
        }

        /// <inheritdoc/>
        public virtual bool IsUndoable => true;

        /// <inheritdoc/>
        public virtual bool IsConfirmable => false;

        /// <summary>
        /// Make a following bounds change on this DrawableContainer undoable!
        /// </summary>
        /// <param name="allowMerge">true means allow the moves to be merged</param>
        public virtual void MakeBoundsChangeUndoable(bool allowMerge)
        {
            if (!IsUndoable)
            {
                return;
            }
            _parent?.MakeUndoable(new DrawableContainerBoundsChangeMemento(this), allowMerge);
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
            _boundsBeforeResize = Bounds.MoveTo(x, y);
            Left =  x;
            Top = y;
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

            // reset "workbench" rectangle to current bounds
            _boundsAfterResize = new NativeRectFloat(_boundsBeforeResize.Left, _boundsBeforeResize.Top, x - _boundsAfterResize.Left, y - _boundsAfterResize.Top);

            var scaleOptions = (this as IHaveScaleOptions)?.GetScaleOptions();
            _boundsAfterResize = ScaleHelper.Scale(_boundsAfterResize, x, y, GetAngleRoundProcessor(), scaleOptions);

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

        protected virtual void SwitchParent(ISurface newParent)
        {
            if (newParent == Parent)
            {
                return;
            }

            _parent?.FieldAggregator?.UnbindElement(this);

            _parent = newParent;
            foreach (IFilter filter in Filters)
            {
                filter.Parent = this;
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (_propertyChanged == null) return;

            _propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            Invalidate();
        }

        /// <summary>
        /// This method will be called before a field is changes.
        /// Using this makes it possible to invalidate the object as is before changing.
        /// </summary>
        /// <param name="fieldToBeChanged">The field to be changed</param>
        /// <param name="newValue">The new value</param>
        public virtual void BeforeFieldChange(IField fieldToBeChanged, object newValue)
        {
            if (IsUndoable)
            {
                _parent?.MakeUndoable(new ChangeFieldHolderMemento(this, fieldToBeChanged), true);
            }
            Invalidate();
        }

        /// <summary>
        /// Handle the field changed event, this should invalidate the correct bounds (e.g. when shadow comes or goes more pixels!)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HandleFieldChanged(object sender, FieldChangedEventArgs e)
        {
            LOG.DebugFormat("Field {0} changed", e.Field.FieldType);
            if (Equals(e.Field.FieldType, FieldType.SHADOW))
            {
                accountForShadowChange = true;
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
            return (int) -Math.Round(radians * 180 / Math.PI);
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
            Point[] points = new[]
            {
                topLeft, bottomRight
            };
            matrix.TransformPoints(points);

            Left = points[0].X;
            Top = points[0].Y;
            Width = points[1].X - points[0].X;
            Height = points[1].Y - points[0].Y;
        }

        protected virtual IDoubleProcessor GetAngleRoundProcessor()
        {
            return ShapeAngleRoundBehavior.INSTANCE;
        }

        public virtual bool HasContextMenu => true;

        public virtual bool HasDefaultSize => false;

        public virtual NativeSize DefaultSize => throw new NotSupportedException("Object doesn't have a default size");

        /// <summary>
        /// Allows to override the initializing of the fields, so we can actually have our own defaults
        /// </summary>
        protected virtual void InitializeFields()
        {
        }
    }
}