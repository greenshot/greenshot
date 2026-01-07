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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceModel.Security;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Drawing.Adorners;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Helpers;
using Greenshot.Editor.Memento;
using log4net;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Description of Surface.
    /// </summary>
    public sealed class Surface : Control, ISurface, INotifyPropertyChanged
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(Surface));
        private static readonly CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();

        // Property to identify the Surface ID
        private Guid _uniqueId = Guid.NewGuid();

        /// <summary>
        ///     This value is used to start counting the step labels
        /// </summary>
        private int _counterStart = 1;

        /// <summary>
        /// The GUID of the surface
        /// </summary>
        public Guid ID
        {
            get => _uniqueId;
            set => _uniqueId = value;
        }

        /// <summary>
        /// Event handlers (do not serialize!)
        /// </summary>
        [NonSerialized] private PropertyChangedEventHandler _propertyChanged;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }

        [NonSerialized] private SurfaceElementEventHandler _movingElementChanged;

        public event SurfaceElementEventHandler MovingElementChanged
        {
            add => _movingElementChanged += value;
            remove => _movingElementChanged -= value;
        }

        [NonSerialized] private SurfaceDrawingModeEventHandler _drawingModeChanged;

        public event SurfaceDrawingModeEventHandler DrawingModeChanged
        {
            add => _drawingModeChanged += value;
            remove => _drawingModeChanged -= value;
        }

        [NonSerialized] private SurfaceSizeChangeEventHandler _surfaceSizeChanged;

        public event SurfaceSizeChangeEventHandler SurfaceSizeChanged
        {
            add => _surfaceSizeChanged += value;
            remove => _surfaceSizeChanged -= value;
        }

        [NonSerialized] private SurfaceMessageEventHandler _surfaceMessage;

        public event SurfaceMessageEventHandler SurfaceMessage
        {
            add => _surfaceMessage += value;
            remove => _surfaceMessage -= value;
        }

        [NonSerialized] private SurfaceForegroundColorEventHandler _foregroundColorChanged;

        public event SurfaceForegroundColorEventHandler ForegroundColorChanged
        {
            add => _foregroundColorChanged += value;
            remove => _foregroundColorChanged -= value;
        }

        [NonSerialized] private SurfaceBackgroundColorEventHandler _backgroundColorChanged;

        public event SurfaceBackgroundColorEventHandler BackgroundColorChanged
        {
            add => _backgroundColorChanged += value;
            remove => _backgroundColorChanged -= value;
        }

        [NonSerialized] private SurfaceLineThicknessEventHandler _lineThicknessChanged;

        public event SurfaceLineThicknessEventHandler LineThicknessChanged
        {
            add => _lineThicknessChanged += value;
            remove => _lineThicknessChanged -= value;
        }

        [NonSerialized] private SurfaceShadowEventHandler _shadowChanged;

        public event SurfaceShadowEventHandler ShadowChanged
        {
            add => _shadowChanged += value;
            remove => _shadowChanged -= value;
        }


        [NonSerialized] private int _currentDpi = 96;
        /// <summary>
        /// The most recent DPI value that was used
        /// </summary>
        public int CurrentDpi
        {
            get => _currentDpi;
            set => _currentDpi = value;
        }

        /// <summary>
        /// inUndoRedo makes sure we don't undo/redo while in a undo/redo action
        /// </summary>
        [NonSerialized] private bool _inUndoRedo;

        /// <summary>
        /// Make only one surface move cycle undoable, see SurfaceMouseMove
        /// </summary>
        [NonSerialized] private bool _isSurfaceMoveMadeUndoable;

        /// <summary>
        /// Undo/Redo stacks, should not be serialized as the file would be way to big
        /// </summary>
        [NonSerialized] private readonly Stack<IMemento> _undoStack = new Stack<IMemento>();

        [NonSerialized] private readonly Stack<IMemento> _redoStack = new Stack<IMemento>();

        /// <summary>
        /// Last save location, do not serialize!
        /// </summary>
        [NonSerialized] private string _lastSaveFullPath;

        /// <summary>
        /// current drawing mode, do not serialize!
        /// </summary>
        [NonSerialized] private DrawingModes _drawingMode = DrawingModes.None;

        /// <summary>
        /// the keys-locked flag helps with focus issues
        /// </summary>
        [NonSerialized] private bool _keysLocked;

        /// <summary>
        /// Location of the mouse-down (it "starts" here), do not serialize
        /// </summary>
        [NonSerialized] private Point _mouseStart = Point.Empty;

        /// <summary>
        /// are we in a mouse down, do not serialize
        /// </summary>
        [NonSerialized] private bool _mouseDown;

        /// <summary>
        /// The selected element for the mouse down, do not serialize
        /// </summary>
        [NonSerialized] private IDrawableContainer _mouseDownElement;

        /// <summary>
        /// all selected elements, do not serialize
        /// </summary>
        [NonSerialized] private readonly IDrawableContainerList selectedElements;

        /// <summary>
        /// the element we are drawing with, do not serialize
        /// </summary>
        [NonSerialized] private IDrawableContainer _drawingElement;

        /// <summary>
        /// the element we want to draw with (not yet drawn), do not serialize
        /// </summary>
        [NonSerialized] private IDrawableContainer _undrawnElement;

        /// <summary>
        /// the crop container, when cropping this is set, do not serialize
        /// </summary>
        [NonSerialized] private IDrawableContainer _cropContainer;

        /// <summary>
        /// the brush which is used for transparent backgrounds, set by the editor, do not serialize
        /// </summary>
        [NonSerialized] private Brush _transparencyBackgroundBrush;

        /// <summary>
        /// The buffer is only for drawing on it when using filters (to supply access)
        /// This saves a lot of "create new bitmap" commands
        /// Should not be serialized, as it's generated.
        /// The actual bitmap is in the paintbox...
        /// TODO: Check if this buffer is still needed!
        /// </summary>
        [NonSerialized] private Bitmap _buffer;

        /// <summary>
        /// all stepLabels for the surface, needed with serialization
        /// </summary>
        private readonly List<StepLabelContainer> _stepLabels = new List<StepLabelContainer>();

        public void AddStepLabel(StepLabelContainer stepLabel)
        {
            if (!_stepLabels.Contains(stepLabel))
            {
                _stepLabels.Add(stepLabel);
            }
        }

        public void RemoveStepLabel(StepLabelContainer stepLabel)
        {
            _stepLabels.Remove(stepLabel);
        }

        /// <summary>
        ///     The start value of the counter objects
        /// </summary>
        public int CounterStart
        {
            get => _counterStart;
            set
            {
                if (_counterStart == value)
                {
                    return;
                }

                _counterStart = value;
                Invalidate();
                _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CounterStart)));
            }
        }

        /// <summary>
        /// Count all the VISIBLE steplabels in the surface, up to the supplied one
        /// </summary>
        /// <param name="stopAtContainer">can be null, if not the counting stops here</param>
        /// <returns>number of steplabels before the supplied container</returns>
        public int CountStepLabels(IDrawableContainer stopAtContainer)
        {
            int number = CounterStart;
            foreach (var possibleThis in _stepLabels)
            {
                if (possibleThis.Equals(stopAtContainer))
                {
                    break;
                }

                if (IsOnSurface(possibleThis))
                {
                    number++;
                }
            }

            return number;
        }

        /// <summary>
        /// all elements on the surface, needed with serialization
        /// </summary>
        private readonly IDrawableContainerList _elements;

        /// <summary>
        /// all elements on the surface, needed with serialization
        /// </summary>
        private IFieldAggregator _fieldAggregator;

        /// <summary>
        /// the cursor container, needed with serialization as we need a direct acces to it.
        /// </summary>
        private IDrawableContainer _cursorContainer;

        /// <summary>
        /// the modified flag specifies if the surface has had modifications after the last export.
        /// Initial state is modified, as "it's not saved"
        /// After serialization this should actually be "false" (the surface came from a stream)
        /// For now we just serialize it...
        /// </summary>
        private bool _modified = true;

        /// <summary>
        /// The image is the actual captured image, needed with serialization
        /// </summary>
        private Image _image;

        public Image Image
        {
            get => _image;
            set
            {
                _image = value;
                UpdateSize();
            }
        }

        [NonSerialized] private Matrix _zoomMatrix = new Matrix(1, 0, 0, 1, 0, 0);
        [NonSerialized] private Matrix _inverseZoomMatrix = new Matrix(1, 0, 0, 1, 0, 0);
        [NonSerialized] private Fraction _zoomFactor = Fraction.Identity;

        public Fraction ZoomFactor
        {
            get => _zoomFactor;
            set
            {
                _zoomFactor = value;
                var inverse = _zoomFactor.Inverse();
                _zoomMatrix = new Matrix(_zoomFactor, 0, 0, _zoomFactor, 0, 0);
                _inverseZoomMatrix = new Matrix(inverse, 0, 0, inverse, 0, 0);
                UpdateSize();
            }
        }


        /// <summary>
        /// Sets the surface size as zoomed image size.
        /// </summary>
        private void UpdateSize()
        {
            var size = _image.Size;
            Size = new Size((int) (size.Width * _zoomFactor), (int) (size.Height * _zoomFactor));
        }

        /// <summary>
        /// The field aggregator is that which is used to have access to all the fields inside the currently selected elements.
        /// e.g. used to decided if and which line thickness is shown when multiple elements are selected.
        /// </summary>
        public IFieldAggregator FieldAggregator
        {
            get => _fieldAggregator;
            set => _fieldAggregator = value;
        }

        /// <summary>
        /// The cursor container has it's own accessor so we can find and remove this (when needed)
        /// </summary>
        public IDrawableContainer CursorContainer => _cursorContainer;

        /// <summary>
        /// A simple getter to ask if this surface has a cursor
        /// </summary>
        public bool HasCursor => _cursorContainer != null;

        /// <summary>
        /// A simple helper method to remove the cursor from the surface
        /// </summary>
        public void RemoveCursor()
        {
            RemoveElement(_cursorContainer);
            _cursorContainer = null;
        }

        /// <summary>
        /// The brush which is used to draw the transparent background
        /// </summary>
        public Brush TransparencyBackgroundBrush
        {
            get => _transparencyBackgroundBrush;
            set => _transparencyBackgroundBrush = value;
        }

        /// <summary>
        /// Are the keys on this surface locked?
        /// </summary>
        public bool KeysLocked
        {
            get => _keysLocked;
            set => _keysLocked = value;
        }

        /// <summary>
        /// Is this surface modified? This is only true if the surface has not been exported.
        /// </summary>
        public bool Modified
        {
            get => _modified;
            set => _modified = value;
        }

        /// <summary>
        /// The DrawingMode property specifies the mode for drawing, more or less the element type.
        /// </summary>
        public DrawingModes DrawingMode
        {
            get => _drawingMode;
            set
            {
                _drawingMode = value;
                if (_drawingModeChanged != null)
                {
                    SurfaceDrawingModeEventArgs eventArgs = new SurfaceDrawingModeEventArgs
                    {
                        DrawingMode = _drawingMode
                    };
                    _drawingModeChanged.Invoke(this, eventArgs);
                }

                DeselectAllElements();
                CreateUndrawnElement();
            }
        }

        /// <summary>
        /// Property for accessing the last save "full" path
        /// </summary>
        public string LastSaveFullPath
        {
            get => _lastSaveFullPath;
            set => _lastSaveFullPath = value;
        }

        /// <summary>
        /// Property for accessing the URL to which the surface was recently uploaded
        /// </summary>
        public string UploadUrl { get; set; }

        /// <summary>
        /// Property for accessing the capture details
        /// </summary>
        public ICaptureDetails CaptureDetails { get; set; }

        /// <summary>
        /// Adjust UI elements to the supplied DPI settings
        /// </summary>
        /// <param name="dpi"></param>
        public void AdjustToDpi(int dpi)
        {
            CurrentDpi = dpi;
            foreach (var element in this._elements)
            {
                element.AdjustToDpi(dpi);
            }
        }

        /// <summary>
        /// Base Surface constructor
        /// </summary>
        public Surface()
        {
            _fieldAggregator = new FieldAggregator(this);
            _elements = new DrawableContainerList(_uniqueId);
            selectedElements = new DrawableContainerList(_uniqueId);
            LOG.Debug("Creating surface!");
            MouseDown += SurfaceMouseDown;
            MouseUp += SurfaceMouseUp;
            MouseMove += SurfaceMouseMove;
            MouseDoubleClick += SurfaceDoubleClick;
            Paint += SurfacePaint;
            AllowDrop = true;
            DragDrop += OnDragDrop;
            DragEnter += OnDragEnter;
            // bind selected & elements to this, otherwise they can't inform of modifications
            selectedElements.Parent = this;
            _elements.Parent = this;
            // Make sure we are visible
            Visible = true;
            TabStop = false;
            // Enable double buffering
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.ContainerControl | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor, true);
        }

        /// <summary>
        /// Private method, the current image is disposed the new one will stay.
        /// </summary>
        /// <param name="newImage">The new image</param>
        /// <param name="dispose">true if the old image needs to be disposed, when using undo this should not be true!!</param>
        private void SetImage(Image newImage, bool dispose)
        {
            // Dispose
            if (_image != null && dispose)
            {
                _image.Dispose();
            }

            // Set new values
            Image = newImage;

            _modified = true;
        }

        /// <summary>
        /// Surface constructor with an image
        /// </summary>
        /// <param name="newImage"></param>
        public Surface(Image newImage) : this()
        {
            LOG.DebugFormat("Got image with dimensions {0} and format {1}", newImage.Size, newImage.PixelFormat);
            SetImage(newImage, true);
        }

        /// <summary>
        /// Surface contructor with a capture
        /// </summary>
        /// <param name="capture"></param>
        public Surface(ICapture capture) : this(capture.Image)
        {
            // check if cursor is captured, and visible
            if (capture.Cursor != null && capture.CursorVisible)
            {
                var cursorRect = new NativeRect(capture.CursorLocation, capture.Cursor.Size);
                var captureRect = new NativeRect(NativePoint.Empty, capture.Image.Size);
                // check if cursor is on the capture, otherwise we leave it out.
                if (cursorRect.IntersectsWith(captureRect))
                {
                    _cursorContainer = AddIconContainer(capture.Cursor, capture.CursorLocation.X, capture.CursorLocation.Y);
                    SelectElement(_cursorContainer);
                }
            }

            // Make sure the image is NOT disposed, we took the reference directly into ourselves
            ((Capture) capture).NullImage();

            CaptureDetails = capture.CaptureDetails;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LOG.Debug("Disposing surface!");
                if (_buffer != null)
                {
                    _buffer.Dispose();
                    _buffer = null;
                }

                if (_transparencyBackgroundBrush != null)
                {
                    _transparencyBackgroundBrush.Dispose();
                    _transparencyBackgroundBrush = null;
                }

                // Cleanup undo/redo stacks
                while (_undoStack != null && _undoStack.Count > 0)
                {
                    _undoStack.Pop().Dispose();
                }

                while (_redoStack != null && _redoStack.Count > 0)
                {
                    _redoStack.Pop().Dispose();
                }

                foreach (IDrawableContainer container in _elements)
                {
                    container.Dispose();
                }

                if (_undrawnElement != null)
                {
                    _undrawnElement.Dispose();
                    _undrawnElement = null;
                }

                if (_cropContainer != null)
                {
                    _cropContainer.Dispose();
                    _cropContainer = null;
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Undo the last action
        /// </summary>
        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                _inUndoRedo = true;
                IMemento top = _undoStack.Pop();
                _redoStack.Push(top.Restore());
                _inUndoRedo = false;
            }
        }

        /// <summary>
        /// Undo an undo (=redo)
        /// </summary>
        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                _inUndoRedo = true;
                IMemento top = _redoStack.Pop();
                _undoStack.Push(top.Restore());
                _inUndoRedo = false;
            }
        }

        /// <summary>
        /// Returns if the surface can do a undo
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// Returns if the surface can do a redo
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// Get the language key for the undo action
        /// </summary>
        public LangKey UndoActionLanguageKey => LangKey.none;

        /// <summary>
        /// Get the language key for redo action
        /// </summary>
        public LangKey RedoActionLanguageKey => LangKey.none;

        /// <summary>
        /// Make an action undo-able
        /// </summary>
        /// <param name="memento">The memento implementing the undo</param>
        /// <param name="allowMerge">Allow changes to be merged</param>
        public void MakeUndoable(IMemento memento, bool allowMerge)
        {
            if (_inUndoRedo)
            {
                throw new InvalidOperationException("Invoking do within an undo/redo action.");
            }

            if (memento != null)
            {
                bool allowPush = true;
                if (_undoStack.Count > 0 && allowMerge)
                {
                    // Check if merge is possible
                    allowPush = !_undoStack.Peek().Merge(memento);
                }

                if (allowPush)
                {
                    // Clear the redo-stack and dispose
                    while (_redoStack.Count > 0)
                    {
                        _redoStack.Pop().Dispose();
                    }

                    _undoStack.Push(memento);
                }
            }
        }

        /// <summary>
        /// This saves the elements of this surface to a stream.
        /// Is used to save a template of the complete surface
        /// </summary>
        /// <param name="streamWrite"></param>
        /// <returns></returns>
        public long SaveElementsToStream(Stream streamWrite)
        {
            long bytesWritten = 0;
            try
            {
                long lengtBefore = streamWrite.Length;
                BinaryFormatter binaryWrite = new BinaryFormatter();
                binaryWrite.Serialize(streamWrite, _elements);
                bytesWritten = streamWrite.Length - lengtBefore;
            }
            catch (Exception e)
            {
                LOG.Error("Error serializing elements to stream.", e);
            }

            return bytesWritten;
        }

        /// <summary>
        /// This loads elements from a stream, among others this is used to load a surface.
        /// </summary>
        /// <param name="streamRead"></param>
        public void LoadElementsFromStream(Stream streamRead)
        {
            try
            {
                BinaryFormatter binaryRead = new BinaryFormatter();
                binaryRead.Binder = new BinaryFormatterHelper();
                IDrawableContainerList loadedElements = (IDrawableContainerList) binaryRead.Deserialize(streamRead);
                loadedElements.Parent = this;
                // Make sure the steplabels are sorted according to their number
                _stepLabels.Sort((p1, p2) => p1.Number.CompareTo(p2.Number));
                DeselectAllElements();
                AddElements(loadedElements);
                SelectElements(loadedElements);
                FieldAggregator.BindElements(loadedElements);
            }
            catch (SecurityAccessDeniedException)
            {
                throw;
            }
            catch (Exception e)
            {
                LOG.Error("Error serializing elements from stream.", e);
            }
        }

        /// <summary>
        /// This is called from the DrawingMode setter, which is not very correct...
        /// But here an element is created which is not yet draw, thus "undrawnElement".
        /// The element is than used while drawing on the surface.
        /// </summary>
        private void CreateUndrawnElement()
        {
            if (_undrawnElement != null)
            {
                FieldAggregator.UnbindElement(_undrawnElement);
            }

            switch (DrawingMode)
            {
                case DrawingModes.Rect:
                    _undrawnElement = new RectangleContainer(this);
                    break;
                case DrawingModes.Ellipse:
                    _undrawnElement = new EllipseContainer(this);
                    break;
                case DrawingModes.Text:
                    _undrawnElement = new TextContainer(this);
                    break;
                case DrawingModes.SpeechBubble:
                    _undrawnElement = new SpeechbubbleContainer(this);
                    break;
                case DrawingModes.StepLabel:
                    _undrawnElement = new StepLabelContainer(this);
                    break;
                case DrawingModes.Line:
                    _undrawnElement = new LineContainer(this);
                    break;
                case DrawingModes.Arrow:
                    _undrawnElement = new ArrowContainer(this);
                    break;
                case DrawingModes.Highlight:
                    _undrawnElement = new HighlightContainer(this);
                    break;
                case DrawingModes.Obfuscate:
                    _undrawnElement = new ObfuscateContainer(this);
                    break;
                case DrawingModes.Crop:
                    _cropContainer = new CropContainer(this);
                    _undrawnElement = _cropContainer;
                    break;
                case DrawingModes.Bitmap:
                    _undrawnElement = new ImageContainer(this);
                    break;
                case DrawingModes.Path:
                    _undrawnElement = new FreehandContainer(this);
                    break;
                case DrawingModes.None:
                    _undrawnElement = null;
                    break;
            }

            if (_undrawnElement != null)
            {
                FieldAggregator.BindElement(_undrawnElement);
            }
        }

        #region Plugin interface implementations

        public IImageContainer AddImageContainer(Image image, int x, int y)
        {
            ImageContainer bitmapContainer = new ImageContainer(this)
            {
                Image = image,
                Left = x,
                Top = y
            };
            AddElement(bitmapContainer);
            return bitmapContainer;
        }

        public IImageContainer AddImageContainer(string filename, int x, int y)
        {
            ImageContainer bitmapContainer = new ImageContainer(this);
            bitmapContainer.Load(filename);
            bitmapContainer.Left = x;
            bitmapContainer.Top = y;
            AddElement(bitmapContainer);
            return bitmapContainer;
        }

        public IIconContainer AddIconContainer(Icon icon, int x, int y)
        {
            IconContainer iconContainer = new IconContainer(this)
            {
                Icon = icon,
                Left = x,
                Top = y
            };
            AddElement(iconContainer);
            return iconContainer;
        }

        public IIconContainer AddIconContainer(string filename, int x, int y)
        {
            IconContainer iconContainer = new IconContainer(this);
            iconContainer.Load(filename);
            iconContainer.Left = x;
            iconContainer.Top = y;
            AddElement(iconContainer);
            return iconContainer;
        }

        public ICursorContainer AddCursorContainer(Cursor cursor, int x, int y)
        {
            CursorContainer cursorContainer = new CursorContainer(this)
            {
                Cursor = cursor,
                Left = x,
                Top = y
            };
            AddElement(cursorContainer);
            return cursorContainer;
        }

        public ICursorContainer AddCursorContainer(string filename, int x, int y)
        {
            CursorContainer cursorContainer = new CursorContainer(this);
            cursorContainer.Load(filename);
            cursorContainer.Left = x;
            cursorContainer.Top = y;
            AddElement(cursorContainer);
            return cursorContainer;
        }

        public ITextContainer AddTextContainer(string text, int x, int y, FontFamily family, float size, bool italic, bool bold, bool shadow, int borderSize, Color color,
            Color fillColor)
        {
            TextContainer textContainer = new TextContainer(this)
            {
                Text = text,
                Left = x,
                Top = y
            };
            textContainer.SetFieldValue(FieldType.FONT_FAMILY, family.Name);
            textContainer.SetFieldValue(FieldType.FONT_BOLD, bold);
            textContainer.SetFieldValue(FieldType.FONT_ITALIC, italic);
            textContainer.SetFieldValue(FieldType.FONT_SIZE, size);
            textContainer.SetFieldValue(FieldType.FILL_COLOR, fillColor);
            textContainer.SetFieldValue(FieldType.LINE_COLOR, color);
            textContainer.SetFieldValue(FieldType.LINE_THICKNESS, borderSize);
            textContainer.SetFieldValue(FieldType.SHADOW, shadow);
            // Make sure the Text fits
            textContainer.FitToText();

            //AggregatedProperties.UpdateElement(textContainer);
            AddElement(textContainer);
            return textContainer;
        }

        #endregion

        #region DragDrop

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (LOG.IsDebugEnabled)
            {
                LOG.Debug("DragEnter got following formats: ");
                foreach (string format in ClipboardHelper.GetFormats(e.Data))
                {
                    LOG.Debug(format);
                }
            }

            if ((e.AllowedEffect & DragDropEffects.Copy) != DragDropEffects.Copy)
            {
                e.Effect = DragDropEffects.None;
            }
            else
            {
                if (ClipboardHelper.ContainsImage(e.Data) || ClipboardHelper.ContainsFormat(e.Data, "DragImageBits"))
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
        }

        /// <summary>
        /// This will help to fit the container to the surface
        /// </summary>
        /// <param name="drawableContainer">IDrawableContainer</param>
        private void FitContainer(IDrawableContainer drawableContainer)
        {
            double factor = 1;
            if (drawableContainer.Width > this.Width)
            {
                factor = drawableContainer.Width / (double)Width;
            }
            if (drawableContainer.Height > this.Height)
            {
                var otherFactor = drawableContainer.Height / (double)Height;
                factor = Math.Max(factor, otherFactor);
            }

            drawableContainer.Width = (int)(drawableContainer.Width / factor);
            drawableContainer.Height = (int)(drawableContainer.Height / factor);
        }

        /// <summary>
        /// Handle the drag/drop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDragDrop(object sender, DragEventArgs e)
        {
            NativePoint mouse = PointToClient(new NativePoint(e.X, e.Y));
            if (e.Data.GetDataPresent("Text"))
            {
                string possibleUrl = ClipboardHelper.GetText(e.Data);
                // Test if it's an url and try to download the image so we have it in the original form
                if (possibleUrl != null && possibleUrl.StartsWith("http"))
                {
                    var drawableContainer = NetworkHelper.DownloadImageAsDrawableContainer(possibleUrl);
                    if (drawableContainer != null)
                    {
                        drawableContainer.Left = Location.X;
                        drawableContainer.Top = Location.Y;
                        FitContainer(drawableContainer);
                        AddElement(drawableContainer);
                        return;
                    }
                }
            }

            foreach (var drawableContainer in ClipboardHelper.GetDrawables(e.Data))
            {
                drawableContainer.Left = mouse.X;
                drawableContainer.Top = mouse.Y;
                FitContainer(drawableContainer);
                AddElement(drawableContainer);
                mouse = mouse.Offset(10, 10);
            }
        }

        #endregion

        /// <summary>
        /// Auto crop the image
        /// </summary>
        /// <param name="cropArea">NativeRect with optional area to find a crop region</param>
        /// <returns>true if cropped</returns>
        public bool AutoCrop(NativeRect? cropArea = null)
        {
            NativeRect cropRectangle;
            using (Image tmpImage = GetImageForExport())
            {
                cropRectangle = ImageHelper.FindAutoCropRectangle(tmpImage, conf.AutoCropDifference, cropArea);
            }

            if (!IsCropPossible(ref cropRectangle, CropContainer.CropModes.AutoCrop))
            {
                return false;
            }

            DeselectAllElements();
            // Maybe a bit obscure, but the following line creates a drop container
            // It's available as "undrawnElement"
            DrawingMode = DrawingModes.Crop;
            _undrawnElement.Left = cropRectangle.X;
            _undrawnElement.Top = cropRectangle.Y;
            _undrawnElement.Width = cropRectangle.Width;
            _undrawnElement.Height = cropRectangle.Height;
            _undrawnElement.Status = EditStatus.UNDRAWN;
            AddElement(_undrawnElement);
            SelectElement(_undrawnElement);
            _drawingElement = null;
            _undrawnElement = null;
            return true;
        }

        /// <summary>
        /// A simple clear
        /// </summary>
        /// <param name="newColor">The color for the background</param>
        public void Clear(Color newColor)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = ImageHelper.CreateEmptyLike(Image, newColor);
            if (newBitmap == null) return;
            // Make undoable
            MakeUndoable(new SurfaceBackgroundChangeMemento(this, null), false);
            SetImage(newBitmap, false);
            Invalidate();
        }

        /// <summary>
        /// Apply a bitmap effect to the surface
        /// </summary>
        /// <param name="effect"></param>
        public void ApplyBitmapEffect(IEffect effect)
        {
            BackgroundForm backgroundForm = new BackgroundForm("Effect", "Please wait");
            backgroundForm.Show();
            Application.DoEvents();
            try
            {
                var imageRectangle = new NativeRect(NativePoint.Empty, Image.Size);
                Matrix matrix = new Matrix();
                Image newImage = ImageHelper.ApplyEffect(Image, effect, matrix);
                if (newImage != null)
                {
                    // Make sure the elements move according to the offset the effect made the bitmap move
                    _elements.Transform(matrix);
                    // Make undoable
                    MakeUndoable(new SurfaceBackgroundChangeMemento(this, matrix), false);
                    SetImage(newImage, false);
                    Invalidate();
                    if (_surfaceSizeChanged != null && !imageRectangle.Equals(new NativeRect(NativePoint.Empty, newImage.Size)))
                    {
                        _surfaceSizeChanged(this, null);
                    }
                }
                else
                {
                    // clean up matrix, as it hasn't been used in the undo stack.
                    matrix.Dispose();
                }
            }
            finally
            {
                // Always close the background form
                backgroundForm.CloseDialog();
            }
        }

        /// <summary>
        /// check if a crop is possible
        /// </summary>
        /// <param name="cropRectangle">Rectangle adapted to the dimensions of the image</param>
        /// <param name="cropMode">CropModes</param>
        /// <returns>true if this is possible</returns>
        public bool IsCropPossible(ref NativeRect cropRectangle, CropContainer.CropModes cropMode)
        {
            cropRectangle = new NativeRect(cropRectangle.Left, cropRectangle.Top, cropRectangle.Width, cropRectangle.Height).Normalize();
            //Fitting the rectangle to the dimensions of the image
            if (cropRectangle.Left < 0)
            {
                cropRectangle = new NativeRect(0, cropRectangle.Top, cropRectangle.Width + cropRectangle.Left, cropRectangle.Height);
            }

            if (cropRectangle.Top < 0)
            {
                cropRectangle = new NativeRect(cropRectangle.Left, 0, cropRectangle.Width, cropRectangle.Height + cropRectangle.Top);
            }

            if (cropRectangle.Left + cropRectangle.Width > Image.Width)
            {
                cropRectangle = new NativeRect(cropRectangle.Left, cropRectangle.Top, Image.Width - cropRectangle.Left, cropRectangle.Height);
            }

            if (cropRectangle.Top + cropRectangle.Height > Image.Height)
            {
                cropRectangle = new NativeRect(cropRectangle.Left, cropRectangle.Top, cropRectangle.Width, Image.Height - cropRectangle.Top);
            }

            // special condition for vertical 
            if(cropMode == CropContainer.CropModes.Vertical && cropRectangle.Width == Image.Width)
            {
                //crop out the hole image is not allowed
                return false;
            }

            // special condition for vertical 
            if (cropMode == CropContainer.CropModes.Horizontal && cropRectangle.Height == Image.Height)
            {
                //crop out the hole image is not allowed
                return false;
            }

            //condition for all other crop modes
            if (cropRectangle.Height > 0 && cropRectangle.Width > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Use to send any registered SurfaceMessageEventHandler a message, e.g. used for the notification area
        /// </summary>
        /// <param name="source">Who send</param>
        /// <param name="messageType">Type of message</param>
        /// <param name="message">Message itself</param>
        public void SendMessageEvent(object source, SurfaceMessageTyp messageType, string message)
        {
            if (_surfaceMessage == null) return;

            var eventArgs = new SurfaceMessageEventArgs
            {
                Message = message,
                MessageType = messageType,
                Surface = this
            };
            _surfaceMessage(source, eventArgs);
        }

        /// <summary>
        /// Use to update UI when pressing a key to change the foreground color
        /// </summary>
        /// <param name="source">Who send</param>
        /// <param name="color">new color</param>
        private void UpdateForegroundColorEvent(object source, Color color)
        {
            if (_foregroundColorChanged == null) return;

            var eventArgs = new SurfaceForegroundColorEventArgs
            {
                Color = color,
            };
            _foregroundColorChanged(source, eventArgs);
        }

        /// <summary>
        /// Use to update UI when pressing a key to change the background color
        /// </summary>
        /// <param name="source">Who send</param>
        /// <param name="color">new color</param>
        private void UpdateBackgroundColorEvent(object source, Color color)
        {
            if (_lineThicknessChanged == null) return;

            var eventArgs = new SurfaceBackgroundColorEventArgs
            {
                Color = color,
            };
            _backgroundColorChanged(source, eventArgs);
        }

        /// <summary>
        /// Use to update UI when pressing a key to change the line thickness
        /// </summary>
        /// <param name="source">Who send</param>
        /// <param name="thickness">new thickness</param>
        private void UpdateLineThicknessEvent(object source, int thickness)
        {
            if (_lineThicknessChanged == null) return;

            var eventArgs = new SurfaceLineThicknessEventArgs
            {
                Thickness = thickness,
            };
            _lineThicknessChanged(source, eventArgs);
        }

        /// <summary>
        /// Use to update UI when pressing the key to show/hide the shadow
        /// </summary>
        /// <param name="source">Who send</param>
        /// <param name="hasShadow">has shadow</param>
        private void UpdateShadowEvent(object source, bool hasShadow)
        {
            if (_shadowChanged == null) return;

            var eventArgs = new SurfaceShadowEventArgs
            {
                HasShadow = hasShadow,
            };
            _shadowChanged(source, eventArgs);
        }

        /// <summary>
        /// Crop the surface
        /// </summary>
        /// <param name="cropRectangle">NativeRect that remains</param>
        /// <returns>bool</returns>
        public bool ApplyCrop(NativeRect cropRectangle)
        {
            if (!IsCropPossible(ref cropRectangle, CropContainer.CropModes.Default)) return false;

            var imageRectangle = new NativeRect(NativePoint.Empty, Image.Size);
            Bitmap tmpImage;
            // Make sure we have information, this this fails
            try
            {
                tmpImage = ImageHelper.CloneArea(Image, cropRectangle, PixelFormat.DontCare);
            }
            catch (Exception ex)
            {
                ex.Data.Add("CropRectangle", cropRectangle);
                ex.Data.Add("Width", Image.Width);
                ex.Data.Add("Height", Image.Height);
                ex.Data.Add("Pixelformat", Image.PixelFormat);
                throw;
            }

            var matrix = new Matrix();
            matrix.Translate(-cropRectangle.Left, -cropRectangle.Top, MatrixOrder.Append);
            // Make undoable
            MakeUndoable(new SurfaceBackgroundChangeMemento(this, matrix), false);

            // Do not dispose otherwise we can't undo the image!
            SetImage(tmpImage, false);
            _elements.Transform(matrix);
            if (_surfaceSizeChanged != null && !imageRectangle.Equals(new NativeRect(NativePoint.Empty, tmpImage.Size)))
            {
                _surfaceSizeChanged(this, null);
            }

            Invalidate();
            return true;
        }

        /// <summary>
        /// Crop out the surface
        /// Splits the image in 3 parts(top, middle, bottom). Crop out the middle and joins top and bottom. 
        /// </summary>
        /// <param name="cropRectangle">NativeRect of the middle part</param>
        /// <returns>bool</returns>
        private bool ApplyHorizontalCrop(NativeRect cropRectangle)
        {
            if (!IsCropPossible(ref cropRectangle, CropContainer.CropModes.Horizontal)) return false;

            var imageRectangle = new NativeRect(NativePoint.Empty, Image.Size);
            var topRectangle = new NativeRect(0, 0, Image.Size.Width, cropRectangle.Top);
            var bottomRectangle = new NativeRect(0, cropRectangle.Top + cropRectangle.Height, Image.Size.Width, Image.Size.Height - cropRectangle.Top - cropRectangle.Height);

            Bitmap newImage;
            try
            {
                newImage = new Bitmap(Image.Size.Width, Image.Size.Height - cropRectangle.Height);

                using var graphics = Graphics.FromImage(newImage);

                var insertPositionTop = 0;
                if (topRectangle.Height > 0)
                {
                    graphics.DrawImage(Image, new NativeRect(0, insertPositionTop, topRectangle.Width, topRectangle.Height), topRectangle, GraphicsUnit.Pixel);
                    insertPositionTop += topRectangle.Height;
                }
                if (bottomRectangle.Height > 0)
                {
                    graphics.DrawImage(Image, new NativeRect(0, insertPositionTop, bottomRectangle.Width, bottomRectangle.Height), bottomRectangle, GraphicsUnit.Pixel);
                }
            }
            catch (Exception ex)
            {
                ex.Data.Add("CropRectangle", cropRectangle);
                ex.Data.Add("Width", Image.Width);
                ex.Data.Add("Height", Image.Height);
                ex.Data.Add("Pixelformat", Image.PixelFormat);
                throw;
            }
            var matrix = new Matrix();
            matrix.Translate(0, -(cropRectangle.Top + cropRectangle.Height), MatrixOrder.Append);
            // Make undoable
            MakeUndoable(new SurfaceBackgroundChangeMemento(this, matrix), false);

            // Do not dispose otherwise we can't undo the image!
            SetImage(newImage, false);

            _elements.Transform(matrix);
            if (_surfaceSizeChanged != null && !imageRectangle.Equals(new NativeRect(NativePoint.Empty, newImage.Size)))
            {
                _surfaceSizeChanged(this, null);
            }

            Invalidate();
            return true;
        }

        /// <summary>
        /// Crop out the surface
        /// Splits the image in 3 parts(left, middle, right). Crop out the middle and joins top and bottom.
        /// </summary>
        /// <param name="cropRectangle">NativeRect of the middle part</param>
        /// <returns>bool</returns>
        private bool ApplyVerticalCrop(NativeRect cropRectangle)
        {
            if (!IsCropPossible(ref cropRectangle, CropContainer.CropModes.Vertical)) return false;

            var imageRectangle = new NativeRect(NativePoint.Empty, Image.Size);
            var leftRectangle = new NativeRect(0, 0, cropRectangle.Left, Image.Size.Height);
            var rightRectangle = new NativeRect(cropRectangle.Left + cropRectangle.Width, 0, Image.Size.Width - cropRectangle.Width - cropRectangle.Left, Image.Size.Height);
            Bitmap newImage;
            try
            {
                newImage = new Bitmap(Image.Size.Width - cropRectangle.Width, Image.Size.Height);

                using var graphics = Graphics.FromImage(newImage);

                var insertPositionLeft = 0;
                if (leftRectangle.Width > 0)
                {
                    graphics.DrawImage(Image, new NativeRect(insertPositionLeft, 0, leftRectangle.Width, leftRectangle.Height), leftRectangle , GraphicsUnit.Pixel);
                    insertPositionLeft += leftRectangle.Width;
                }
                
                if (rightRectangle.Width > 0)
                {
                    graphics.DrawImage(Image, new NativeRect(insertPositionLeft, 0, rightRectangle.Width, rightRectangle.Height), rightRectangle,  GraphicsUnit.Pixel);
                }
            }
            catch (Exception ex)
            {
                ex.Data.Add("CropRectangle", cropRectangle);
                ex.Data.Add("Width", Image.Width);
                ex.Data.Add("Height", Image.Height);
                ex.Data.Add("Pixelformat", Image.PixelFormat);
                throw;
            }
            var matrix = new Matrix();
            matrix.Translate(-cropRectangle.Left - cropRectangle.Width, 0, MatrixOrder.Append);
            // Make undoable
            MakeUndoable(new SurfaceBackgroundChangeMemento(this, matrix), false);

            // Do not dispose otherwise we can't undo the image!
            SetImage(newImage, false);

            _elements.Transform(matrix);
            if (_surfaceSizeChanged != null && !imageRectangle.Equals(new NativeRect(NativePoint.Empty, newImage.Size)))
            {
                _surfaceSizeChanged(this, null);
            }

            Invalidate();
            return true;
        }

        /// <summary>
        /// The background here is the captured image.
        /// This is called from the SurfaceBackgroundChangeMemento.
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="matrix"></param>
        public void UndoBackgroundChange(Image previous, Matrix matrix)
        {
            SetImage(previous, false);
            if (matrix != null)
            {
                _elements.Transform(matrix);
            }

            _surfaceSizeChanged?.Invoke(this, null);
            Invalidate();
        }

        /// <summary>
        /// Check if an adorner was "hit", and change the cursor if so
        /// </summary>
        /// <param name="mouseEventArgs">MouseEventArgs</param>
        /// <returns>IAdorner</returns>
        private IAdorner FindActiveAdorner(MouseEventArgs mouseEventArgs)
        {
            foreach (IDrawableContainer drawableContainer in selectedElements)
            {
                foreach (IAdorner adorner in drawableContainer.Adorners)
                {
                    if (!adorner.IsActive && !adorner.HitTest(mouseEventArgs.Location)) continue;
                    
                    if (adorner.Cursor != null)
                    {
                        Cursor = adorner.Cursor;
                    }

                    return adorner;
                }
            }

            return null;
        }

        /// <summary>
        /// Translate mouse coordinates as if they were applied directly to unscaled image.
        /// </summary>
        private MouseEventArgs InverseZoomMouseCoordinates(MouseEventArgs e)
            => new MouseEventArgs(e.Button, e.Clicks, (int) (e.X / _zoomFactor), (int) (e.Y / _zoomFactor), e.Delta);

        /// <summary>
        /// This event handler is called when someone presses the mouse on a surface.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SurfaceMouseDown(object sender, MouseEventArgs e)
        {
            e = InverseZoomMouseCoordinates(e);

            // Handle Adorners
            var adorner = FindActiveAdorner(e);
            if (adorner != null)
            {
                adorner.MouseDown(sender, e);
                return;
            }

            _mouseStart = e.Location;

            // check contextmenu
            if (e.Button == MouseButtons.Right)
            {
                IDrawableContainerList selectedList = null;
                if (selectedElements != null && selectedElements.Count > 0)
                {
                    selectedList = selectedElements;
                }
                else
                {
                    // Single element
                    IDrawableContainer rightClickedContainer = _elements.ClickableElementAt(_mouseStart.X, _mouseStart.Y);
                    if (rightClickedContainer != null)
                    {
                        selectedList = new DrawableContainerList(ID)
                        {
                            rightClickedContainer
                        };
                    }
                }

                if (selectedList != null && selectedList.Count > 0)
                {
                    selectedList.ShowContextMenu(e, this);
                }

                return;
            }

            _mouseDown = true;
            _isSurfaceMoveMadeUndoable = false;

            if (_cropContainer != null && ((_undrawnElement == null) || (_undrawnElement != null && DrawingMode != DrawingModes.Crop)))
            {
                RemoveElement(_cropContainer, false);
                _cropContainer = null;
                _drawingElement = null;
            }

            if (_drawingElement == null && DrawingMode != DrawingModes.None)
            {
                if (_undrawnElement == null)
                {
                    DeselectAllElements();
                    if (_undrawnElement == null)
                    {
                        CreateUndrawnElement();
                    }
                }

                _drawingElement = _undrawnElement;
                // if a new element has been drawn, set location and register it
                if (_drawingElement != null)
                {
                    if (_undrawnElement != null)
                    {
                        _drawingElement.Status = _undrawnElement.DefaultEditMode;
                    }

                    if (!_drawingElement.HandleMouseDown(_mouseStart.X, _mouseStart.Y))
                    {
                        _drawingElement.Left = _mouseStart.X;
                        _drawingElement.Top = _mouseStart.Y;
                    }

                    AddElement(_drawingElement);
                    _drawingElement.Selected = true;
                }

                _undrawnElement = null;
            }
            else
            {
                // check whether an existing element was clicked
                // we save mouse down element separately from selectedElements (checked on mouse up),
                // since it could be moved around before it is actually selected
                _mouseDownElement = _elements.ClickableElementAt(_mouseStart.X, _mouseStart.Y);

                if (_mouseDownElement != null)
                {
                    _mouseDownElement.Status = EditStatus.MOVING;
                }
            }
        }

        /// <summary>
        /// This event handle is called when the mouse button is unpressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SurfaceMouseUp(object sender, MouseEventArgs e)
        {
            e = InverseZoomMouseCoordinates(e);

            // Handle Adorners
            var adorner = FindActiveAdorner(e);
            if (adorner != null)
            {
                adorner.MouseUp(sender, e);
                return;
            }

            Point currentMouse = new Point(e.X, e.Y);

            _elements.Status = EditStatus.IDLE;
            if (_mouseDownElement != null)
            {
                _mouseDownElement.Status = EditStatus.IDLE;
            }

            _mouseDown = false;
            _mouseDownElement = null;
            if (DrawingMode == DrawingModes.None)
            {
                // check whether an existing element was clicked
                IDrawableContainer element = _elements.ClickableElementAt(currentMouse.X, currentMouse.Y);
                bool shiftModifier = (ModifierKeys & Keys.Shift) == Keys.Shift;
                if (element != null)
                {
                    element.Invalidate();
                    bool alreadySelected = selectedElements.Contains(element);
                    if (shiftModifier)
                    {
                        if (alreadySelected)
                        {
                            DeselectElement(element);
                        }
                        else
                        {
                            SelectElement(element);
                        }
                    }
                    else
                    {
                        if (!alreadySelected)
                        {
                            DeselectAllElements();
                            SelectElement(element);
                        }
                    }
                }
                else if (!shiftModifier)
                {
                    DeselectAllElements();
                }
            }

            if (selectedElements.Count > 0)
            {
                selectedElements.Invalidate();
                selectedElements.Selected = true;
            }

            if (_drawingElement != null)
            {
                if (!_drawingElement.InitContent())
                {
                    _elements.Remove(_drawingElement);
                    _drawingElement.Invalidate();
                }
                else
                {
                    _drawingElement.HandleMouseUp(currentMouse.X, currentMouse.Y);
                    _drawingElement.Invalidate();
                    if (Math.Abs(_drawingElement.Width) < 5 && Math.Abs(_drawingElement.Height) < 5)
                    {
                        _drawingElement.Width = 25;
                        _drawingElement.Height = 25;
                    }

                    SelectElement(_drawingElement);
                    _drawingElement.Selected = true;
                }

                _drawingElement = null;
            }
        }

        /// <summary>
        /// This event handler is called when the mouse moves over the surface
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SurfaceMouseMove(object sender, MouseEventArgs e)
        {
            e = InverseZoomMouseCoordinates(e);

            // Handle Adorners
            var adorner = FindActiveAdorner(e);
            if (adorner != null)
            {
                adorner.MouseMove(sender, e);
                return;
            }

            Point currentMouse = e.Location;

            Cursor = DrawingMode != DrawingModes.None ? Cursors.Cross : Cursors.Default;

            if (!_mouseDown) return;

            if (_mouseDownElement != null)
            {
                // an element is currently dragged
                _mouseDownElement.Invalidate();
                selectedElements.Invalidate();
                // Move the element
                if (_mouseDownElement.Selected)
                {
                    if (!_isSurfaceMoveMadeUndoable)
                    {
                        // Only allow one undoable per mouse-down/move/up "cycle"
                        _isSurfaceMoveMadeUndoable = true;
                        selectedElements.MakeBoundsChangeUndoable(false);
                    }

                    // dragged element has been selected before -> move all
                    selectedElements.MoveBy(currentMouse.X - _mouseStart.X, currentMouse.Y - _mouseStart.Y);
                }
                else
                {
                    if (!_isSurfaceMoveMadeUndoable)
                    {
                        // Only allow one undoable per mouse-down/move/up "cycle"
                        _isSurfaceMoveMadeUndoable = true;
                        _mouseDownElement.MakeBoundsChangeUndoable(false);
                    }

                    // dragged element is not among selected elements -> just move dragged one
                    _mouseDownElement.MoveBy(currentMouse.X - _mouseStart.X, currentMouse.Y - _mouseStart.Y);
                }

                _mouseStart = currentMouse;
                _mouseDownElement.Invalidate();
                _modified = true;
            }
            else if (_drawingElement != null)
            {
                _drawingElement.HandleMouseMove(currentMouse.X, currentMouse.Y);
                _modified = true;
            }
        }

        /// <summary>
        /// This event handler is called when the surface is double clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SurfaceDoubleClick(object sender, MouseEventArgs e)
        {
            selectedElements.OnDoubleClick();
            selectedElements.Invalidate();
        }

        /// <summary>
        /// Privately used to get the rendered image with all the elements on it.
        /// </summary>
        /// <param name="renderMode"></param>
        /// <returns></returns>
        private Image GetImage(RenderMode renderMode)
        {
            // Generate a copy of the original image with a dpi equal to the default...
            Bitmap clone = ImageHelper.Clone(_image, PixelFormat.DontCare);
            // otherwise we would have a problem drawing the image to the surface... :(
            using (Graphics graphics = Graphics.FromImage(clone))
            {
                // Do not set the following, the containers need to decide themselves
                //graphics.SmoothingMode = SmoothingMode.HighQuality;
                //graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                //graphics.CompositingQuality = CompositingQuality.HighQuality;
                //graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                _elements.Draw(graphics, clone, renderMode, new Rectangle(Point.Empty, clone.Size));
            }

            return clone;
        }

        /// <summary>
        /// This returns the image "result" of this surface, with all the elements rendered on it.
        /// </summary>
        /// <returns></returns>
        public Image GetImageForExport()
        {
            return GetImage(RenderMode.EXPORT);
        }

        private static NativeRect ZoomClipRectangle(NativeRect rc, double scale, int inflateAmount = 0)
        {
            rc = new NativeRect(
                (int) (rc.X * scale),
                (int) (rc.Y * scale),
                (int) (rc.Width * scale) + 1,
                (int) (rc.Height * scale) + 1
            );
            if (scale > 1)
            {
                inflateAmount = (int) (inflateAmount * scale);
            }

            return rc.Inflate(inflateAmount, inflateAmount);
        }

        public void InvalidateElements(NativeRect rc)
            => Invalidate(ZoomClipRectangle(rc, _zoomFactor, 1));

        /// <summary>
        /// This is the event handler for the Paint Event, try to draw as little as possible!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="paintEventArgs">PaintEventArgs</param>
        private void SurfacePaint(object sender, PaintEventArgs paintEventArgs)
        {
            Graphics targetGraphics = paintEventArgs.Graphics;
            NativeRect targetClipRectangle = paintEventArgs.ClipRectangle;
            if (targetClipRectangle.IsEmpty)
            {
                LOG.Debug("Empty cliprectangle??");
                return;
            }

            // Correction to prevent rounding errors at certain zoom levels.
            // When zooming to N/M, clip rectangle top and left coordinates should be multiples of N.
            if (_zoomFactor.Numerator > 1 && _zoomFactor.Denominator > 1)
            {
                int horizontalCorrection = targetClipRectangle.Left % (int) _zoomFactor.Numerator;
                int verticalCorrection = targetClipRectangle.Top % (int) _zoomFactor.Numerator;
                if (horizontalCorrection != 0)
                {
                    targetClipRectangle = targetClipRectangle
                        .ChangeX(targetClipRectangle.X - horizontalCorrection)
                        .ChangeWidth(targetClipRectangle.X + horizontalCorrection);
                }

                if (verticalCorrection != 0)
                {
                    targetClipRectangle = targetClipRectangle
                        .ChangeY(targetClipRectangle.Y - verticalCorrection)
                        .ChangeHeight(targetClipRectangle.Y + verticalCorrection);
                }
            }

            NativeRect imageClipRectangle = ZoomClipRectangle(targetClipRectangle, _zoomFactor.Inverse(), 2);

            if (_elements.HasIntersectingFilters(imageClipRectangle) || _zoomFactor > Fraction.Identity)
            {
                if (_buffer != null)
                {
                    if (_buffer.Width != Image.Width || _buffer.Height != Image.Height || _buffer.PixelFormat != Image.PixelFormat)
                    {
                        _buffer.Dispose();
                        _buffer = null;
                    }
                }

                if (_buffer == null)
                {
                    _buffer = ImageHelper.CreateEmpty(Image.Width, Image.Height, Image.PixelFormat, Color.Empty, Image.HorizontalResolution, Image.VerticalResolution);
                    LOG.DebugFormat("Created buffer with size: {0}x{1}", Image.Width, Image.Height);
                }

                // Elements might need the bitmap, so we copy the part we need
                using (Graphics graphics = Graphics.FromImage(_buffer))
                {
                    // do not set the following, the containers need to decide this themselves!
                    //graphics.SmoothingMode = SmoothingMode.HighQuality;
                    //graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    //graphics.CompositingQuality = CompositingQuality.HighQuality;
                    //graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    DrawBackground(graphics, imageClipRectangle);
                    graphics.DrawImage(Image, imageClipRectangle, imageClipRectangle, GraphicsUnit.Pixel);
                    graphics.SetClip(ZoomClipRectangle(Rectangle.Round(targetGraphics.ClipBounds), _zoomFactor.Inverse(), 2));
                    _elements.Draw(graphics, _buffer, RenderMode.EDIT, imageClipRectangle);
                }

                if (_zoomFactor == Fraction.Identity)
                {
                    targetGraphics.DrawImage(_buffer, imageClipRectangle, imageClipRectangle, GraphicsUnit.Pixel);
                }
                else
                {
                    targetGraphics.ScaleTransform(_zoomFactor, _zoomFactor);
                    if (_zoomFactor > Fraction.Identity)
                    {
                        DrawSharpImage(targetGraphics, _buffer, imageClipRectangle);
                    }
                    else
                    {
                        DrawSmoothImage(targetGraphics, _buffer, imageClipRectangle);
                    }

                    targetGraphics.ResetTransform();
                }
            }
            else
            {
                DrawBackground(targetGraphics, targetClipRectangle);
                if (_zoomFactor == Fraction.Identity)
                {
                    targetGraphics.DrawImage(Image, imageClipRectangle, imageClipRectangle, GraphicsUnit.Pixel);
                    _elements.Draw(targetGraphics, null, RenderMode.EDIT, imageClipRectangle);
                }
                else
                {
                    targetGraphics.ScaleTransform(_zoomFactor, _zoomFactor);
                    DrawSmoothImage(targetGraphics, Image, imageClipRectangle);
                    _elements.Draw(targetGraphics, null, RenderMode.EDIT, imageClipRectangle);
                    targetGraphics.ResetTransform();
                }
            }

            // No clipping for the adorners
            targetGraphics.ResetClip();
            // Draw adorners last
            foreach (var drawableContainer in selectedElements)
            {
                foreach (var adorner in drawableContainer.Adorners)
                {
                    adorner.Paint(paintEventArgs);
                }
            }
        }

        private void DrawSmoothImage(Graphics targetGraphics, Image image, NativeRect imageClipRectangle)
        {
            var state = targetGraphics.Save();
            targetGraphics.SmoothingMode = SmoothingMode.HighQuality;
            targetGraphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            targetGraphics.CompositingQuality = CompositingQuality.HighQuality;
            targetGraphics.PixelOffsetMode = PixelOffsetMode.None;

            targetGraphics.DrawImage(image, imageClipRectangle, imageClipRectangle, GraphicsUnit.Pixel);

            targetGraphics.Restore(state);
        }

        private void DrawSharpImage(Graphics targetGraphics, Image image, NativeRect imageClipRectangle)
        {
            var state = targetGraphics.Save();
            targetGraphics.SmoothingMode = SmoothingMode.None;
            targetGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            targetGraphics.CompositingQuality = CompositingQuality.HighQuality;
            targetGraphics.PixelOffsetMode = PixelOffsetMode.None;

            targetGraphics.DrawImage(image, imageClipRectangle, imageClipRectangle, GraphicsUnit.Pixel);

            targetGraphics.Restore(state);
        }

        private void DrawBackground(Graphics targetGraphics, NativeRect clipRectangle)
        {
            // check if we need to draw the checkerboard
            if (Image.IsAlphaPixelFormat(Image.PixelFormat) && _transparencyBackgroundBrush != null)
            {
                targetGraphics.FillRectangle(_transparencyBackgroundBrush, clipRectangle);
            }
            else
            {
                targetGraphics.Clear(BackColor);
            }
        }

        /// <summary>
        /// Draw a checkboard when capturing with transparency
        /// </summary>
        /// <param name="e">PaintEventArgs</param>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        /// <summary>
        /// Add a new element to the surface
        /// </summary>
        /// <param name="element">the new element</param>
        /// <param name="makeUndoable">true if the adding should be undoable</param>
        /// <param name="invalidate">true if invalidate needs to be called</param>
        public void AddElement(IDrawableContainer element, bool makeUndoable = true, bool invalidate = true)
        {
            _elements.Add(element);
            if (element is DrawableContainer container)
            {
                container.FieldChanged += Element_FieldChanged;
            }

            element.Parent = this;
            if (element.Status == EditStatus.UNDRAWN)
            {
                element.Status = EditStatus.IDLE;
            }

            if (element.Selected)
            {
                // Use false, as the element is invalidated when invalidate == true anyway
                SelectElement(element, false);
            }

            if (invalidate)
            {
                element.Invalidate();
            }

            if (makeUndoable && element.IsUndoable)
            {
                MakeUndoable(new AddElementMemento(this, element), false);
            }

            _modified = true;
        }

        /// <summary>
        /// Remove the list of elements
        /// </summary>
        /// <param name="elementsToRemove">IDrawableContainerList</param>
        /// <param name="makeUndoable">flag specifying if the remove needs to be undoable</param>
        public void RemoveElements(IDrawableContainerList elementsToRemove, bool makeUndoable = true)
        {
            // fix potential issues with iterating a changing list
            DrawableContainerList cloned = new DrawableContainerList(elementsToRemove);

            if (makeUndoable)
            {
                // Take all containers to make undoable
                var undoableContainers = elementsToRemove.Where(c => c.IsUndoable).ToList();
                if (undoableContainers.Any())
                {
                    var undoableContainerList = new DrawableContainerList(undoableContainers);
                    MakeUndoable(new DeleteElementsMemento(this, undoableContainerList), false);
                }
            }

            SuspendLayout();
            foreach (var drawableContainer in cloned)
            {
                RemoveElement(drawableContainer, false, false, false);
            }

            ResumeLayout();
            Invalidate();
            if (_movingElementChanged != null)
            {
                SurfaceElementEventArgs eventArgs = new SurfaceElementEventArgs
                {
                    Elements = cloned
                };
                _movingElementChanged(this, eventArgs);
            }
        }

        /// <summary>
        /// Remove an element of the elements list
        /// </summary>
        /// <param name="elementToRemove">Element to remove</param>
        /// <param name="makeUndoable">flag specifying if the remove needs to be undoable</param>
        /// <param name="invalidate">flag specifying if an surface invalidate needs to be called</param>
        /// <param name="generateEvents">false to skip event generation</param>
        public void RemoveElement(IDrawableContainer elementToRemove, bool makeUndoable = true, bool invalidate = true, bool generateEvents = true)
        {
            DeselectElement(elementToRemove, generateEvents);
            _elements.Remove(elementToRemove);
            if (elementToRemove is DrawableContainer element)
            {
                element.FieldChanged -= Element_FieldChanged;
            }

            if (elementToRemove != null)
            {
                elementToRemove.Parent = null;
            }

            // Do not dispose, the memento should!! element.Dispose();
            if (invalidate)
            {
                Invalidate();
            }

            if (makeUndoable && elementToRemove is { IsUndoable: true })
            {
                MakeUndoable(new DeleteElementMemento(this, elementToRemove), false);
            }

            _modified = true;
        }

        /// <summary>
        /// Add the supplied elements to the surface
        /// </summary>
        /// <param name="elementsToAdd">DrawableContainerList</param>
        /// <param name="makeUndoable">true if the adding should be undoable</param>
        public void AddElements(IDrawableContainerList elementsToAdd, bool makeUndoable = true)
        {
            // fix potential issues with iterating a changing list
            DrawableContainerList cloned = new DrawableContainerList(elementsToAdd);
            if (makeUndoable)
            {
                // Take all containers to make undoable
                var undoableContainers = elementsToAdd.Where(c => c.IsUndoable).ToList();
                if (undoableContainers.Any())
                {
                    var undoableContainerList = new DrawableContainerList(undoableContainers);
                    MakeUndoable(new AddElementsMemento(this, undoableContainerList), false);
                }
            }

            SuspendLayout();
            foreach (var element in cloned)
            {
                element.Selected = true;
                AddElement(element, false, false);
            }

            ResumeLayout();
            Invalidate();
        }

        /// <summary>
        /// Returns if this surface has selected elements
        /// </summary>
        /// <returns>bool</returns>
        public bool HasSelectedElements => selectedElements is { Count: > 0 };

        /// <summary>
        /// Provides the selected elements
        /// </summary>
        public IDrawableContainerList SelectedElements => selectedElements;

        /// <summary>
        /// Remove all the selected elements
        /// </summary>
        public void RemoveSelectedElements()
        {
            if (!HasSelectedElements) return;

            // As RemoveElement will remove the element from the selectedElements list we need to copy the element to another list.
            RemoveElements(selectedElements);
            if (_movingElementChanged != null)
            {
                SurfaceElementEventArgs eventArgs = new SurfaceElementEventArgs();
                _movingElementChanged(this, eventArgs);
            }
        }

        /// <summary>
        /// Cut the selected elements from the surface to the clipboard
        /// </summary>
        public void CutSelectedElements()
        {
            if (!HasSelectedElements) return;
            ClipboardHelper.SetClipboardData(typeof(IDrawableContainerList), selectedElements);
            RemoveSelectedElements();
        }

        /// <summary>
        /// Copy the selected elements to the clipboard
        /// </summary>
        public void CopySelectedElements()
        {
            if (!HasSelectedElements) return;
            ClipboardHelper.SetClipboardData(typeof(IDrawableContainerList), selectedElements);
        }

        /// <summary>
        /// This method is called to confirm/cancel.
        /// Called when pressing enter or using the "check" in the editor.
        /// redirects to the specialized confirm/cancel method
        /// </summary>
        /// <param name="confirm">bool</param>
        public void Confirm(bool confirm)
        {
            if (DrawingMode == DrawingModes.Crop)
            {
                ConfirmCrop(confirm);
            }
            else
            {
                ConfirmSelectedConfirmableElements(confirm);
            }
        }

        /// <summary>
        /// This method is called to confirm/cancel "confirmable" elements
        /// Called when pressing enter or using the "check" in the editor.
        /// <br/>
        /// For crop-container there is a dedicated method <see cref="ConfirmCrop(bool)"/>.
        /// </summary>
        /// <param name="confirm">bool</param>
        public void ConfirmSelectedConfirmableElements(bool confirm)
        {
            // create new collection so that we can iterate safely (selectedElements might change due with confirm/cancel)
            List<IDrawableContainer> selectedDCs = new List<IDrawableContainer>(selectedElements);
            foreach (IDrawableContainer dc in selectedDCs.Where(c => c.IsConfirmable))
            {                
                throw new NotImplementedException($"No confirm/cancel defined for Container type {dc.GetType()}");               
            }

            // maybe the undo button has to be enabled
            _movingElementChanged?.Invoke(this, new SurfaceElementEventArgs());
        }

        /// <summary>
        /// This method is called to confirm/cancel the crop-container.
        /// Called when pressing enter or using the "check" in the editor.
        /// </summary>
        /// <param name="confirm">bool</param>
        public void ConfirmCrop(bool confirm)
        {
            if (_cropContainer is not CropContainer e) return;
                    
            if (confirm && selectedElements.Count > 0)
            {
                // No undo memento for the cropcontainer itself, only for the effect
                RemoveElement(_cropContainer, false);

                _ = e.GetFieldValue(FieldType.CROPMODE) switch
                {
                    CropContainer.CropModes.Horizontal => ApplyHorizontalCrop(_cropContainer.Bounds),
                    CropContainer.CropModes.Vertical => ApplyVerticalCrop(_cropContainer.Bounds),
                    _ => ApplyCrop(_cropContainer.Bounds)
                };

                _cropContainer.Dispose();
                _cropContainer = null;
            }
            else
            {
                RemoveCropContainer();
            }

            DrawingMode = DrawingModes.None;

            // maybe the undo button has to be enabled
            _movingElementChanged?.Invoke(this, new SurfaceElementEventArgs());
        }

        public void RemoveCropContainer()
        {
            if (_cropContainer == null) return;

            RemoveElement(_cropContainer, false);
            _cropContainer.Dispose();
            _cropContainer = null;
        }

        /// <summary>
        /// Paste all the elements that are on the clipboard
        /// </summary>
        public void PasteElementFromClipboard()
        {
            IDataObject clipboard = ClipboardHelper.GetDataObject();

            var formats = ClipboardHelper.GetFormats(clipboard);
            if (formats == null || formats.Count == 0)
            {
                return;
            }

            if (LOG.IsDebugEnabled)
            {
                LOG.Debug("List of clipboard formats available for pasting:");
                foreach (string format in formats)
                {
                    LOG.Debug("\tgot format: " + format);
                }
            }

            if (formats.Contains(typeof(IDrawableContainerList).FullName))
            {
                IDrawableContainerList dcs = (IDrawableContainerList) ClipboardHelper.GetFromDataObject(clipboard, typeof(IDrawableContainerList));
                if (dcs != null)
                {
                    // Make element(s) only move 10,10 if the surface is the same
                    bool isSameSurface = (dcs.ParentID == _uniqueId);
                    dcs.Parent = this;
                    var moveOffset = isSameSurface ? new NativePoint(10, 10) : NativePoint.Empty;
                    // Here a fix for bug #1475, first calculate the bounds of the complete IDrawableContainerList
                    NativeRect drawableContainerListBounds = NativeRect.Empty;
                    foreach (var element in dcs)
                    {
                        drawableContainerListBounds = drawableContainerListBounds == NativeRect.Empty
                            ? element.DrawingBounds
                            : drawableContainerListBounds.Union(element.DrawingBounds);
                    }

                    // And find a location inside the target surface to paste to
                    bool containersCanFit = drawableContainerListBounds.Width < Bounds.Width && drawableContainerListBounds.Height < Bounds.Height;
                    if (!containersCanFit)
                    {
                        NativePoint containersLocation = drawableContainerListBounds.Location;
                        containersLocation.Offset(moveOffset);
                        if (!Bounds.Contains(containersLocation))
                        {
                            // Easy fix for same surface
                            moveOffset = isSameSurface
                                ? new NativePoint(-10, -10)
                                : new NativePoint(-drawableContainerListBounds.Location.X + 10, -drawableContainerListBounds.Location.Y + 10);
                        }
                    }
                    else
                    {
                        NativeRect moveContainerListBounds = drawableContainerListBounds.Offset(moveOffset);
                        // check if the element is inside
                        if (!Bounds.Contains(moveContainerListBounds))
                        {
                            // Easy fix for same surface
                            if (isSameSurface)
                            {
                                moveOffset = new Point(-10, -10);
                            }
                            else
                            {
                                // For different surface, which is most likely smaller
                                int offsetX = 0;
                                int offsetY = 0;
                                if (drawableContainerListBounds.Right > Bounds.Right)
                                {
                                    offsetX = Bounds.Right - drawableContainerListBounds.Right;
                                    // Correction for the correction
                                    if (drawableContainerListBounds.Left + offsetX < 0)
                                    {
                                        offsetX += Math.Abs(drawableContainerListBounds.Left + offsetX);
                                    }
                                }

                                if (drawableContainerListBounds.Bottom > Bounds.Bottom)
                                {
                                    offsetY = Bounds.Bottom - drawableContainerListBounds.Bottom;
                                    // Correction for the correction
                                    if (drawableContainerListBounds.Top + offsetY < 0)
                                    {
                                        offsetY += Math.Abs(drawableContainerListBounds.Top + offsetY);
                                    }
                                }

                                moveOffset = new Point(offsetX, offsetY);
                            }
                        }
                    }

                    dcs.MoveBy(moveOffset.X, moveOffset.Y);
                    AddElements(dcs);
                    FieldAggregator.BindElements(dcs);
                    DeselectAllElements();
                    SelectElements(dcs);
                }
            }
            else if (ClipboardHelper.ContainsImage(clipboard))
            {
                NativePoint pasteLocation = GetPasteLocation(0.1f, 0.1f);

                foreach (var drawableContainer in ClipboardHelper.GetDrawables(clipboard))
                {
                    if (drawableContainer == null) continue;
                    DeselectAllElements();
                    drawableContainer.Left = pasteLocation.X;
                    drawableContainer.Top = pasteLocation.Y; 
                    AddElement(drawableContainer);
                    SelectElement(drawableContainer);
                    pasteLocation = pasteLocation.Offset(10, 10);
                }
            }
            else if (ClipboardHelper.ContainsText(clipboard))
            {
                NativePoint pasteLocation = GetPasteLocation(0.4f, 0.4f);

                string text = ClipboardHelper.GetText(clipboard);
                if (text != null)
                {
                    DeselectAllElements();
                    ITextContainer textContainer = AddTextContainer(text, pasteLocation.X, pasteLocation.Y,
                        FontFamily.GenericSansSerif, 12f, false, false, false, 2, Color.Black, Color.Transparent);
                    SelectElement(textContainer);
                }
            }
        }

        /// <summary>
        /// Find a location to paste elements.
        /// If mouse is over the surface - use it's position, otherwise use the visible area.
        /// Return a point in image coordinate space.
        /// </summary>
        /// <param name="horizontalRatio">0.0f for the left edge of visible area, 1.0f for the right edge of visible area.</param>
        /// <param name="verticalRatio">0.0f for the top edge of visible area, 1.0f for the bottom edge of visible area.</param>
        private NativePoint GetPasteLocation(float horizontalRatio = 0.5f, float verticalRatio = 0.5f)
        {
            var point = PointToClient(MousePosition);
            var rc = GetVisibleRectangle();
            if (!rc.Contains(point))
            {
                point = new NativePoint(
                    rc.Left + (int) (rc.Width * horizontalRatio),
                    rc.Top + (int) (rc.Height * verticalRatio)
                );
            }

            return ToImageCoordinates(point);
        }

        /// <summary>
        /// Get the rectangle bounding the part of this Surface currently visible in the editor (in surface coordinate space).
        /// </summary>
        public NativeRect GetVisibleRectangle()
        {
            var bounds = Bounds;
            var clientArea = Parent.ClientRectangle;
            return new NativeRect(
                Math.Max(0, -bounds.Left),
                Math.Max(0, -bounds.Top),
                clientArea.Width,
                clientArea.Height
            );
        }

        /// <summary>
        /// Get the rectangle bounding all selected elements (in surface coordinates space),
        /// or empty rectangle if nothing is selected.
        /// </summary>
        public NativeRect GetSelectionRectangle()
            => ToSurfaceCoordinates(selectedElements.DrawingBounds);

        /// <summary>
        /// Duplicate all the selected elements
        /// </summary>
        public void DuplicateSelectedElements()
        {
            LOG.DebugFormat("Duplicating {0} selected elements", selectedElements.Count);
            IDrawableContainerList dcs = selectedElements.Clone();
            dcs.Parent = this;
            dcs.MoveBy(10, 10);
            AddElements(dcs);
            DeselectAllElements();
            SelectElements(dcs);
        }

        /// <summary>
        /// Deselect the specified element
        /// </summary>
        /// <param name="container">IDrawableContainerList</param>
        /// <param name="generateEvents">false to skip event generation</param>
        public void DeselectElement(IDrawableContainer container, bool generateEvents = true)
        {
            container.Selected = false;
            selectedElements.Remove(container);
            FieldAggregator.UnbindElement(container);
            if (generateEvents && _movingElementChanged != null)
            {
                var eventArgs = new SurfaceElementEventArgs
                {
                    Elements = selectedElements
                };
                _movingElementChanged(this, eventArgs);
            }
        }

        /// <summary>
        /// Deselect the specified elements
        /// </summary>
        /// <param name="elements">IDrawableContainerList</param>
        public void DeselectElements(IDrawableContainerList elements)
        {
            if (elements.Count == 0)
            {
                return;
            }

            while (elements.Count > 0)
            {
                var element = elements[0];
                DeselectElement(element, false);
            }

            if (_movingElementChanged != null)
            {
                SurfaceElementEventArgs eventArgs = new SurfaceElementEventArgs
                {
                    Elements = selectedElements
                };
                _movingElementChanged(this, eventArgs);
            }

            Invalidate();
        }

        /// <summary>
        /// Deselect all the selected elements
        /// </summary>
        public void DeselectAllElements()
        {
            DeselectElements(selectedElements);
        }

        /// <summary>
        /// Select the supplied element
        /// </summary>
        /// <param name="container"></param>
        /// <param name="invalidate">false to skip invalidation</param>
        /// <param name="generateEvents">false to skip event generation</param>
        public void SelectElement(IDrawableContainer container, bool invalidate = true, bool generateEvents = true)
        {
            if (selectedElements.Contains(container)) return;

            selectedElements.Add(container);
            container.Selected = true;
            FieldAggregator.BindElement(container);
            if (generateEvents && _movingElementChanged != null)
            {
                SurfaceElementEventArgs eventArgs = new SurfaceElementEventArgs
                {
                    Elements = selectedElements
                };
                _movingElementChanged(this, eventArgs);
            }

            if (invalidate)
            {
                container.Invalidate();
            }
        }

        /// <summary>
        /// Select all elements, this is called when Ctrl+A is pressed
        /// </summary>
        public void SelectAllElements()
        {
            SelectElements(_elements);
        }

        /// <summary>
        /// Select the supplied elements
        /// </summary>
        /// <param name="elements"></param>
        public void SelectElements(IDrawableContainerList elements)
        {
            SuspendLayout();
            foreach (var drawableContainer in elements)
            {
                var element = (DrawableContainer) drawableContainer;
                SelectElement(element, false, false);
            }

            if (_movingElementChanged != null)
            {
                SurfaceElementEventArgs eventArgs = new SurfaceElementEventArgs
                {
                    Elements = selectedElements
                };
                _movingElementChanged(this, eventArgs);
            }

            ResumeLayout();
            Invalidate();
        }

        /// <summary>
        /// Process key presses on the surface, this is called from the editor (and NOT an override from the Control)
        /// </summary>
        /// <param name="k">Keys</param>
        /// <returns>false if no keys were processed</returns>
        public bool ProcessCmdKey(Keys k)
        {
            if (selectedElements.Count <= 0 && k != Keys.Escape) return false;

            bool shiftModifier = (ModifierKeys & Keys.Shift) == Keys.Shift;
            int px = shiftModifier ? 10 : 1;
            NativePoint moveBy = NativePoint.Empty;
            switch (k)
            {
                case Keys.Left:
                case Keys.Left | Keys.Shift:
                    moveBy = new NativePoint(-px, 0);
                    break;
                case Keys.Up:
                case Keys.Up | Keys.Shift:
                    moveBy = new NativePoint(0, -px);
                    break;
                case Keys.Right:
                case Keys.Right | Keys.Shift:
                    moveBy = new NativePoint(px, 0);
                    break;
                case Keys.Down:
                case Keys.Down | Keys.Shift:
                    moveBy = new NativePoint(0, px);
                    break;
                case Keys.PageUp:
                    PullElementsUp();
                    break;
                case Keys.PageDown:
                    PushElementsDown();
                    break;
                case Keys.Home:
                    PullElementsToTop();
                    break;
                case Keys.End:
                    PushElementsToBottom();
                    break;
                case Keys.Enter:
                    Confirm(true);
                    break;
                case Keys.Escape:
                    Confirm(false);
                    break;
                case Keys.D0 | Keys.Control:
                case Keys.D0 | Keys.Shift | Keys.Control:
                    SetSelectedElementColor(shiftModifier ? Color.Orange : Color.Transparent, false, shiftModifier);
                    break;
                case Keys.D1 | Keys.Control:
                case Keys.D1 | Keys.Shift | Keys.Control:
                    SetSelectedElementColor(Color.Red, false, shiftModifier);
                    break;
                case Keys.D2 | Keys.Control:
                case Keys.D2 | Keys.Shift | Keys.Control:
                    SetSelectedElementColor(Color.Green, false, shiftModifier);
                    break;
                case Keys.D3 | Keys.Control:
                case Keys.D3 | Keys.Shift | Keys.Control:
                    SetSelectedElementColor(Color.Blue, false, shiftModifier);
                    break;
                case Keys.D4 | Keys.Control:
                case Keys.D4 | Keys.Shift | Keys.Control:
                    SetSelectedElementColor(Color.Cyan, false, shiftModifier);
                    break;
                case Keys.D5 | Keys.Control:
                case Keys.D5 | Keys.Shift | Keys.Control:
                    SetSelectedElementColor(Color.Magenta, false, shiftModifier);
                    break;
                case Keys.D6 | Keys.Control:
                case Keys.D6 | Keys.Shift | Keys.Control:
                    SetSelectedElementColor(Color.Yellow, false, shiftModifier);
                    break;
                case Keys.D7 | Keys.Control:
                case Keys.D7 | Keys.Shift | Keys.Control:
                    SetSelectedElementColor(Color.Black, false, shiftModifier);
                    break;
                case Keys.D8 | Keys.Control:
                case Keys.D8 | Keys.Shift | Keys.Control:
                    SetSelectedElementColor(Color.Gray, false, shiftModifier);
                    break;
                case Keys.D9 | Keys.Control:
                case Keys.D9 | Keys.Shift | Keys.Control:
                    SetSelectedElementColor(Color.White, false, shiftModifier);
                    break;
                case Keys.Add | Keys.Control:
                case Keys.Add | Keys.Shift | Keys.Control:
                    ChangeLineThickness(shiftModifier ? 5 : 1);
                    break;
                case Keys.Subtract | Keys.Control:
                case Keys.Subtract | Keys.Shift | Keys.Control:
                    ChangeLineThickness(shiftModifier ? -5 : -1);
                    break;
                case Keys.Divide | Keys.Control:
                    FlipShadow();
                    break;
                /*case Keys.Delete:
                        RemoveSelectedElements();
                        break;*/
                default:
                    return false;
            }

            if (moveBy != NativePoint.Empty)
            {
                selectedElements.MakeBoundsChangeUndoable(true);
                selectedElements.MoveBy(moveBy.X, moveBy.Y);
            }

            return true;

        }

        // for laptops without numPads, also allow shift modifier
        private void SetSelectedElementColor(Color color, bool numPad, bool shift)
        {
            if (numPad || shift)
            {
                selectedElements.SetForegroundColor(color);
                UpdateForegroundColorEvent(this, color);
            }
            else
            {
                selectedElements.SetBackgroundColor(color);
                UpdateBackgroundColorEvent(this, color);
            }
            selectedElements.Invalidate();
        }

        private void ChangeLineThickness(int increaseBy)
        {
            var newThickness = selectedElements.IncreaseLineThickness(increaseBy);
            UpdateLineThicknessEvent(this, newThickness);
            selectedElements.Invalidate();
        }

        private void FlipShadow()
        {
            var shadow = selectedElements.FlipShadow();
            UpdateShadowEvent(this, shadow);
            selectedElements.Invalidate();
        }

        /// <summary>
        /// Property for accessing the elements on the surface
        /// </summary>
        public IDrawableContainerList Elements => _elements;

        /// <summary>
        /// pulls selected elements up one level in hierarchy
        /// </summary>
        public void PullElementsUp()
        {
            _elements.PullElementsUp(selectedElements);
            _elements.Invalidate();
        }

        /// <summary>
        /// pushes selected elements up to top in hierarchy
        /// </summary>
        public void PullElementsToTop()
        {
            _elements.PullElementsToTop(selectedElements);
            _elements.Invalidate();
        }

        /// <summary>
        /// pushes selected elements down one level in hierarchy
        /// </summary>
        public void PushElementsDown()
        {
            _elements.PushElementsDown(selectedElements);
            _elements.Invalidate();
        }

        /// <summary>
        /// pushes selected elements down to bottom in hierarchy
        /// </summary>
        public void PushElementsToBottom()
        {
            _elements.PushElementsToBottom(selectedElements);
            _elements.Invalidate();
        }

        /// <summary>
        /// indicates whether the selected elements could be pulled up in hierarchy
        /// </summary>
        /// <returns>true if selected elements could be pulled up, false otherwise</returns>
        public bool CanPullSelectionUp()
        {
            return _elements.CanPullUp(selectedElements);
        }

        /// <summary>
        /// indicates whether the selected elements could be pushed down in hierarchy
        /// </summary>
        /// <returns>true if selected elements could be pushed down, false otherwise</returns>
        public bool CanPushSelectionDown()
        {
            return _elements.CanPushDown(selectedElements);
        }

        private void Element_FieldChanged(object sender, FieldChangedEventArgs e)
        {
            selectedElements.HandleFieldChangedEvent(sender, e);
        }

        public bool IsOnSurface(IDrawableContainer container)
        {
            return _elements.Contains(container);
        }

        public NativePoint ToSurfaceCoordinates(NativePoint point)
        {
            Point[] points =
            {
                point
            };
            _zoomMatrix.TransformPoints(points);
            return points[0];
        }

        public NativeRect ToSurfaceCoordinates(NativeRect rc)
        {
            if (_zoomMatrix.IsIdentity)
            {
                return rc;
            }

            Point[] points =
            {
                rc.Location, rc.Location.Offset(rc.Size.Width, rc.Size.Height)
            };
            _zoomMatrix.TransformPoints(points);
            return new NativeRect(
                points[0].X,
                points[0].Y,
                points[1].X - points[0].X,
                points[1].Y - points[0].Y
            );
        }

        public NativePoint ToImageCoordinates(NativePoint point)
        {
            Point[] points =
            {
                point
            };
            _inverseZoomMatrix.TransformPoints(points);
            return points[0];
        }

        public NativeRect ToImageCoordinates(NativeRect rc)
        {
            if (_inverseZoomMatrix.IsIdentity)
            {
                return rc;
            }

            Point[] points =
            {
                rc.Location, rc.Location.Offset(rc.Size.Width, rc.Size.Height)
            };
            _inverseZoomMatrix.TransformPoints(points);
            return new NativeRect(
                points[0].X,
                points[0].Y,
                points[1].X - points[0].X,
                points[1].Y - points[0].Y
            );
        }
    }
}