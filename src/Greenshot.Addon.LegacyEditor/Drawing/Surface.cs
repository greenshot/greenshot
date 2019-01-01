﻿#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using Dapplo.HttpExtensions;
using Dapplo.Log;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addon.LegacyEditor.Memento;
using Greenshot.Addons.Config.Impl;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Addons.Interfaces.Drawing.Adorners;
using Greenshot.Gfx;
using Greenshot.Gfx.Effects;

#endregion

namespace Greenshot.Addon.LegacyEditor.Drawing
{
	/// <summary>
	///     Description of Surface.
	/// </summary>
	public sealed class Surface : Control, ISurface, INotifyPropertyChanged
	{
		private static readonly LogSource Log = new LogSource();

        /// <summary>
        /// The number of Surfaces in existance
        /// </summary>
        public static int Count { get; private set; }

		/// <summary>
		///     all elements on the surface, needed with serialization
		/// </summary>
		private readonly IDrawableContainerList _elements;


        [NonSerialized]
        private readonly IEditorConfiguration _editorConfiguration;

        [NonSerialized]
        private readonly ICoreConfiguration _coreConfiguration;

        [NonSerialized]
        private readonly Stack<IMemento> _redoStack = new Stack<IMemento>();

		/// <summary>
		///     all stepLabels for the surface, needed with serialization
		/// </summary>
		private readonly List<StepLabelContainer> _stepLabels = new List<StepLabelContainer>();

		/// <summary>
		///     Undo/Redo stacks, should not be serialized as the file would be way to big
		/// </summary>
		[NonSerialized]
        private readonly Stack<IMemento> _undoStack = new Stack<IMemento>();

		/// <summary>
		///     all selected elements, do not serialize
		/// </summary>
		[NonSerialized]
        private readonly IDrawableContainerList selectedElements;

		/// <summary>
		///     The buffer is only for drawing on it when using filters (to supply access)
		///     This saves a lot of "create new bitmap" commands
		///     Should not be serialized, as it's generated.
		///     The actual bitmap is in the paintbox...
		///     TODO: Check if this buffer is still needed!
		/// </summary>
		[NonSerialized]
        private Bitmap _buffer;

		/// <summary>
		///     This value is used to start counting the step labels
		/// </summary>
		private int _counterStart = 1;

		/// <summary>
		///     the cropcontainer, when cropping this is set, do not serialize
		/// </summary>
		[NonSerialized]
        private IDrawableContainer _cropContainer;

		/// <summary>
		///     the cursor container, needed with serialization as we need a direct acces to it.
		/// </summary>
		private IDrawableContainer _cursorContainer;

		/// <summary>
		///     the element we are drawing with, do not serialize
		/// </summary>
		[NonSerialized]
        private IDrawableContainer _drawingElement;

		/// <summary>
		///     current drawing mode, do not serialize!
		/// </summary>
		[NonSerialized]
        private DrawingModes _drawingMode = DrawingModes.None;

		[NonSerialized]
        private SurfaceDrawingModeEventHandler _drawingModeChanged;

		/// <summary>
		///     all elements on the surface, needed with serialization
		/// </summary>
		private FieldAggregator _fieldAggregator;

		/// <summary>
		///     The image is the actual captured image, needed with serialization
		/// </summary>
		private Bitmap _screenshot;

		/// <summary>
		///     inUndoRedo makes sure we don't undo/redo while in a undo/redo action
		/// </summary>
		[NonSerialized]
        private bool _inUndoRedo;

		/// <summary>
		///     Make only one surfacemove cycle undoable, see SurfaceMouseMove
		/// </summary>
		[NonSerialized]
        private bool _isSurfaceMoveMadeUndoable;

		/// <summary>
		///     the keyslocked flag helps with focus issues
		/// </summary>
		[NonSerialized]
        private bool _keysLocked;

		/// <summary>
		///     Last save location, do not serialize!
		/// </summary>
		[NonSerialized]
        private string _lastSaveFullPath;

		/// <summary>
		///     the modified flag specifies if the surface has had modifications after the last export.
		///     Initial state is modified, as "it's not saved"
		///     After serialization this should actually be "false" (the surface came from a stream)
		///     For now we just serialize it...
		/// </summary>
		private bool _modified = true;

		/// <summary>
		///     are we in a mouse down, do not serialize
		/// </summary>
		[NonSerialized]
        private bool _mouseDown;

		/// <summary>
		///     The selected element for the mouse down, do not serialize
		/// </summary>
		[NonSerialized]
        private IDrawableContainer _mouseDownElement;

		/// <summary>
		///     Location of the mouse-down (it "starts" here), do not serialize
		/// </summary>
		[NonSerialized]
        private NativePoint _mouseStart = NativePoint.Empty;

		[NonSerialized]
        private SurfaceElementEventHandler _movingElementChanged;

		/// <summary>
		///     Event handlers (do not serialize!)
		/// </summary>
		[NonSerialized]
        private PropertyChangedEventHandler _propertyChanged;

		[NonSerialized]
        private SurfaceMessageEventHandler _surfaceMessage;

		[NonSerialized]
        private SurfaceSizeChangeEventHandler _surfaceSizeChanged;

		/// <summary>
		///     the brush which is used for transparent backgrounds, set by the editor, do not serialize
		/// </summary>
		[NonSerialized]
        private Brush _transparencyBackgroundBrush;

		/// <summary>
		///     the element we want to draw with (not yet drawn), do not serialize
		/// </summary>
		[NonSerialized]
        private IDrawableContainer _undrawnElement;

		// Property to identify the Surface ID

		/// <summary>
		///     Base Surface constructor
		/// </summary>
		public Surface(ICoreConfiguration coreConfiguration, IEditorConfiguration editorConfiguration)
		{
			_fieldAggregator = new FieldAggregator(this, editorConfiguration);
			Count++;
			_elements = new DrawableContainerList(ID);
			selectedElements = new DrawableContainerList(ID);
			Log.Debug().WriteLine("Creating surface!");
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
        ///     Surface constructor with an image
        /// </summary>
        /// <param name="newBitmap">Bitmap</param>
        public Surface(ICoreConfiguration coreConfiguration, IEditorConfiguration editorConfiguration, Bitmap newBitmap) : this(coreConfiguration, editorConfiguration)
		{
			Log.Debug().WriteLine("Got Bitmap with dimensions {0} and format {1}", newBitmap.Size, newBitmap.PixelFormat);
			SetBitmap(newBitmap, true);
		}

		/// <summary>
		///     Surface contructor with a capture
		/// </summary>
		/// <param name="capture"></param>
		public Surface(ICoreConfiguration coreConfiguration, IEditorConfiguration editorConfiguration, ICapture capture) : this(coreConfiguration, editorConfiguration)
		{
			SetCapture(capture);
		}

        /// <summary>
        /// Use the supplied capture in the surface
        /// </summary>
        /// <param name="capture">ICapture</param>
	    public void SetCapture(ICapture capture)
	    {
	        var newBitmap = capture.Bitmap;
            Log.Debug().WriteLine("Got Bitmap with dimensions {0} and format {1}", newBitmap.Size, newBitmap.PixelFormat);
	        SetBitmap(newBitmap, true);
            // check if cursor is captured, and visible
            if (capture.Cursor != null && capture.CursorVisible)
	        {
	            var cursorRect = new NativeRect(capture.CursorLocation, capture.Cursor.Size);
	            var captureRect = new NativeRect(NativePoint.Empty, capture.Bitmap.Size);
	            // check if cursor is on the capture, otherwise we leave it out.
	            if (cursorRect.IntersectsWith(captureRect))
	            {
	                _cursorContainer = AddIconContainer(capture.Cursor, capture.CursorLocation.X, capture.CursorLocation.Y);
	                SelectElement(_cursorContainer);
	            }
	        }
	        // Make sure the image is NOT disposed, we took the reference directly into ourselves
	        ((Capture)capture).NullBitmap();

	        CaptureDetails = capture.CaptureDetails;
        }

		/// <summary>
		///     The field aggregator is that which is used to have access to all the fields inside the currently selected elements.
		///     e.g. used to decided if and which line thickness is shown when multiple elements are selected.
		/// </summary>
		public FieldAggregator FieldAggregator
		{
			get { return _fieldAggregator; }
			set { _fieldAggregator = value; }
		}

		/// <summary>
		///     The cursor container has it's own accessor so we can find and remove this (when needed)
		/// </summary>
		public IDrawableContainer CursorContainer => _cursorContainer;

		/// <summary>
		///     The brush which is used to draw the transparent background
		/// </summary>
		public Brush TransparencyBackgroundBrush
		{
			get { return _transparencyBackgroundBrush; }
			set { _transparencyBackgroundBrush = value; }
		}

		/// <summary>
		///     Are the keys on this surface locked?
		/// </summary>
		public bool KeysLocked
		{
			get { return _keysLocked; }
			set { _keysLocked = value; }
		}

		/// <summary>
		///     The DrawingMode property specifies the mode for drawing, more or less the element type.
		/// </summary>
		public DrawingModes DrawingMode
		{
			get { return _drawingMode; }
			set
			{
				_drawingMode = value;
				if (_drawingModeChanged != null)
				{
					var eventArgs = new SurfaceDrawingModeEventArgs
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
		///     Returns if the surface can do a undo
		/// </summary>
		public bool CanUndo => _undoStack.Count > 0;

		/// <summary>
		///     Returns if the surface can do a redo
		/// </summary>
		public bool CanRedo => _redoStack.Count > 0;

		public event PropertyChangedEventHandler PropertyChanged
		{
			add { _propertyChanged += value; }
			remove { _propertyChanged -= value; }
		}

		/// <summary>
		///     The GUID of the surface
		/// </summary>
		public Guid ID { get; set; } = Guid.NewGuid();

		public event SurfaceElementEventHandler MovingElementChanged
		{
			add { _movingElementChanged += value; }
			remove { _movingElementChanged -= value; }
		}

		public event SurfaceDrawingModeEventHandler DrawingModeChanged
		{
			add { _drawingModeChanged += value; }
			remove { _drawingModeChanged -= value; }
		}

		public event SurfaceSizeChangeEventHandler SurfaceSizeChanged
		{
			add { _surfaceSizeChanged += value; }
			remove { _surfaceSizeChanged -= value; }
		}

		public event SurfaceMessageEventHandler SurfaceMessage
		{
			add { _surfaceMessage += value; }
			remove { _surfaceMessage -= value; }
		}

		/// <summary>
		///     The start value of the counter objects
		/// </summary>
		public int CounterStart
		{
			get { return _counterStart; }
			set
			{
				if (_counterStart == value)
				{
					return;
				}

				_counterStart = value;
				Invalidate();
				_propertyChanged?.Invoke(this, new PropertyChangedEventArgs("CounterStart"));
			}
		}

		public Bitmap Screenshot
		{
			get { return _screenshot; }
			set
			{
				_screenshot = value;
				Size = _screenshot.Size;
			}
		}

		/// <summary>
		///     A simple getter to ask if this surface has a cursor
		/// </summary>
		public bool HasCursor => _cursorContainer != null;

		/// <summary>
		///     A simple helper method to remove the cursor from the surface
		/// </summary>
		public void RemoveCursor()
		{
			RemoveElement(_cursorContainer);
			_cursorContainer = null;
		}

		/// <summary>
		///     Is this surface modified? This is only true if the surface has not been exported.
		/// </summary>
		public bool Modified
		{
			get { return _modified; }
			set { _modified = value; }
		}

		/// <summary>
		///     Property for accessing the last save "full" path
		/// </summary>
		public string LastSaveFullPath
		{
			get { return _lastSaveFullPath; }
			set { _lastSaveFullPath = value; }
		}

		/// <summary>
		///     Property for accessing the URL to which the surface was recently uploaded
		/// </summary>
		public string UploadUrl { get; set; }

		/// <summary>
		///     Property for accessing the capture details
		/// </summary>
		public ICaptureDetails CaptureDetails { get; set; }

		/// <summary>
		///     Make an action undo-able
		/// </summary>
		/// <param name="memento">The memento implementing the undo</param>
		/// <param name="allowMerge">Allow changes to be merged</param>
		public void MakeUndoable(IMemento memento, bool allowMerge)
		{
			if (_inUndoRedo)
			{
				throw new InvalidOperationException("Invoking do within an undo/redo action.");
			}

		    if (memento == null)
		    {
		        return;
		    }

		    var allowPush = true;
		    if (_undoStack.Count > 0 && allowMerge)
		    {
		        // Check if merge is possible
		        allowPush = !_undoStack.Peek().Merge(memento);
		    }

		    if (!allowPush)
		    {
		        return;
		    }

		    // Clear the redo-stack and dispose
		    while (_redoStack.Count > 0)
		    {
		        _redoStack.Pop().Dispose();
		    }
		    _undoStack.Push(memento);
		}

		/// <summary>
		///     This saves the elements of this surface to a stream.
		///     Is used to save a template of the complete surface
		/// </summary>
		/// <param name="streamWrite"></param>
		/// <returns></returns>
		public long SaveElementsToStream(Stream streamWrite)
		{
			long bytesWritten = 0;
			try
			{
				var lengtBefore = streamWrite.Length;
				var binaryWrite = new BinaryFormatter();
				binaryWrite.Serialize(streamWrite, _elements);
				bytesWritten = streamWrite.Length - lengtBefore;
			}
			catch (Exception e)
			{
				Log.Error().WriteLine(e, "Error serializing elements to stream.");
			}
			return bytesWritten;
		}

		/// <summary>
		///     This loads elements from a stream, among others this is used to load a surface.
		/// </summary>
		/// <param name="streamRead"></param>
		public void LoadElementsFromStream(Stream streamRead)
		{
			try
			{
				var binaryRead = new BinaryFormatter();
				var loadedElements = (IDrawableContainerList) binaryRead.Deserialize(streamRead);
				loadedElements.Parent = this;
				// Make sure the steplabels are sorted accoring to their number
				_stepLabels.Sort((p1, p2) => p1.Number.CompareTo(p2.Number));
				DeselectAllElements();
				AddElements(loadedElements);
				SelectElements(loadedElements);
				FieldAggregator.BindElements(loadedElements);
			}
			catch (Exception e)
			{
				Log.Error().WriteLine(e, "Error serializing elements from stream.");
			}
		}

		/// <summary>
		///     Apply a bitmap effect to the surface
		/// </summary>
		/// <param name="effect"></param>
		public void ApplyBitmapEffect(IEffect effect)
		{
			var backgroundForm = new BackgroundForm("Effect", "Please wait");
			backgroundForm.Show();
			Application.DoEvents();
			try
			{
				var imageRectangle = new NativeRect(NativePoint.Empty, Screenshot.Size);
				var matrix = new Matrix();
				var newImage = Screenshot.ApplyEffect(effect, matrix);
				if (newImage != null)
				{
					// Make sure the elements move according to the offset the effect made the bitmap move
					_elements.Transform(matrix);
					// Make undoable
					MakeUndoable(new SurfaceBackgroundChangeMemento(this, matrix), false);
					SetBitmap(newImage, false);
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
		///     Use to send any registered SurfaceMessageEventHandler a message, e.g. used for the notification area
		/// </summary>
		/// <param name="source">Who send</param>
		/// <param name="messageType">Type of message</param>
		/// <param name="message">Message itself</param>
		public void SendMessageEvent(object source, SurfaceMessageTyp messageType, string message)
		{
			if (_surfaceMessage != null)
			{
				var eventArgs = new SurfaceMessageEventArgs
				{
					Message = message,
					MessageType = messageType,
					Surface = this
				};
				_surfaceMessage(source, eventArgs);
			}
		}

		/// <summary>
		///     This returns the image "result" of this surface, with all the elements rendered on it.
		/// </summary>
		/// <returns></returns>
		public Bitmap GetBitmapForExport()
		{
			return GetBitmap(RenderMode.Export);
		}

		/// <summary>
		///     Add a new element to the surface
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
			if (makeUndoable)
			{
				MakeUndoable(new AddElementMemento(this, element), false);
			}
			_modified = true;
		}

		/// <summary>
		///     Remove the list of elements
		/// </summary>
		/// <param name="elementsToRemove">IDrawableContainerList</param>
		/// <param name="makeUndoable">flag specifying if the remove needs to be undoable</param>
		public void RemoveElements(IDrawableContainerList elementsToRemove, bool makeUndoable = true)
		{
			// fix potential issues with iterating a changing list
			var cloned = new DrawableContainerList();
			cloned.AddRange(elementsToRemove);
			if (makeUndoable)
			{
				MakeUndoable(new DeleteElementsMemento(this, cloned), false);
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
				var eventArgs = new SurfaceElementEventArgs {Elements = cloned};
				_movingElementChanged(this, eventArgs);
			}
		}

		/// <summary>
		///     Remove an element of the elements list
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
			// Do not dispose, the memento should!!
			if (invalidate)
			{
				Invalidate();
			}
			if (makeUndoable)
			{
				MakeUndoable(new DeleteElementMemento(this, elementToRemove), false);
			}
			_modified = true;
		}

		/// <summary>
		///     Add the supplied elements to the surface
		/// </summary>
		/// <param name="elementsToAdd">DrawableContainerList</param>
		/// <param name="makeUndoable">true if the adding should be undoable</param>
		public void AddElements(IDrawableContainerList elementsToAdd, bool makeUndoable = true)
		{
			// fix potential issues with iterating a changing list
			var cloned = new DrawableContainerList();
			cloned.AddRange(elementsToAdd);
			if (makeUndoable)
			{
				MakeUndoable(new AddElementsMemento(this, cloned), false);
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
		///     Returns if this surface has selected elements
		/// </summary>
		/// <returns></returns>
		public bool HasSelectedElements => selectedElements != null && selectedElements.Count > 0;

		/// <summary>
		///     Remove all the selected elements
		/// </summary>
		public void RemoveSelectedElements()
		{
			if (HasSelectedElements)
			{
				// As RemoveElement will remove the element from the selectedElements list we need to copy the element to another list.
				RemoveElements(selectedElements);
				if (_movingElementChanged != null)
				{
					var eventArgs = new SurfaceElementEventArgs();
					_movingElementChanged(this, eventArgs);
				}
			}
		}

		/// <summary>
		///     Cut the selected elements from the surface to the clipboard
		/// </summary>
		public void CutSelectedElements()
		{
			if (HasSelectedElements)
			{
				ClipboardHelper.SetClipboardData(typeof(IDrawableContainerList), selectedElements);
				RemoveSelectedElements();
			}
		}

		/// <summary>
		///     Copy the selected elements to the clipboard
		/// </summary>
		public void CopySelectedElements()
		{
			if (HasSelectedElements)
			{
				ClipboardHelper.SetClipboardData(typeof(IDrawableContainerList), selectedElements);
			}
		}

		/// <summary>
		///     Paste all the elements that are on the clipboard
		/// </summary>
		public void PasteElementFromClipboard()
		{
			var clipboard = ClipboardHelper.GetDataObject();

			var formats = ClipboardHelper.GetFormats(clipboard);
			if (formats == null || formats.Count == 0)
			{
				return;
			}
			if (Log.IsDebugEnabled())
			{
				Log.Debug().WriteLine("List of clipboard formats available for pasting:");
				foreach (var format in formats)
				{
					Log.Debug().WriteLine("\tgot format: " + format);
				}
			}

			if (formats.Contains(typeof(IDrawableContainerList).FullName))
			{
				var dcs = (IDrawableContainerList) ClipboardHelper.GetFromDataObject(clipboard, typeof(IDrawableContainerList));
				if (dcs != null)
				{
					// Make element(s) only move 10,10 if the surface is the same
					var isSameSurface = dcs.ParentID == ID;
					dcs.Parent = this;
					var moveOffset = isSameSurface ? new NativePoint(10, 10) : NativePoint.Empty;
					// Here a fix for bug #1475, first calculate the bounds of the complete IDrawableContainerList
					var drawableContainerListBounds = NativeRect.Empty;
					foreach (var element in dcs)
					{
						drawableContainerListBounds = drawableContainerListBounds == NativeRect.Empty
							? element.DrawingBounds
							: drawableContainerListBounds.Union(element.DrawingBounds);
					}
					// And find a location inside the target surface to paste to
					var containersCanFit = drawableContainerListBounds.Width < Bounds.Width && drawableContainerListBounds.Height < Bounds.Height;
					if (!containersCanFit)
					{
						var containersLocation = drawableContainerListBounds.Location.Offset(moveOffset);
						if (!Bounds.Contains(containersLocation))
						{
							// Easy fix for same surface
							moveOffset = isSameSurface ? new NativePoint(-10, -10) : new NativePoint(-drawableContainerListBounds.Location.X + 10, -drawableContainerListBounds.Location.Y + 10);
						}
					}
					else
					{
						var moveContainerListBounds = drawableContainerListBounds.Offset(moveOffset);
						// check if the element is inside
						if (!Bounds.Contains(moveContainerListBounds))
						{
							// Easy fix for same surface
							if (isSameSurface)
							{
								moveOffset = new NativePoint(-10, -10);
							}
							else
							{
								// For different surface, which is most likely smaller
								var offsetX = 0;
								var offsetY = 0;
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
								moveOffset = new NativePoint(offsetX, offsetY);
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
				var x = 10;
				var y = 10;

				// FEATURE-995: Added a check for the current mouse cursor location, to paste the image on that location.
				var mousePositionOnControl = PointToClient(MousePosition);
				if (ClientRectangle.Contains(mousePositionOnControl))
				{
					x = mousePositionOnControl.X;
					y = mousePositionOnControl.Y;
				}

				foreach (var clipboardImage in ClipboardHelper.GetBitmaps(clipboard))
				{
					if (clipboardImage != null)
					{
						DeselectAllElements();
						var container = AddImageContainer(clipboardImage as Bitmap, x, y);
						SelectElement(container);
						clipboardImage.Dispose();
						x += 10;
						y += 10;
					}
				}
			}
			else if (ClipboardHelper.ContainsText(clipboard))
			{
				var text = ClipboardHelper.GetText(clipboard);
				if (text != null)
				{
					DeselectAllElements();
					var textContainer = AddTextContainer(text, HorizontalAlignment.Center, VerticalAlignment.CENTER,
						FontFamily.GenericSansSerif, 12f, false, false, false, 2, Color.Black, Color.Transparent);
					SelectElement(textContainer);
				}
			}
		}

		/// <summary>
		///     Duplicate all the selecteded elements
		/// </summary>
		public void DuplicateSelectedElements()
		{
			Log.Debug().WriteLine("Duplicating {0} selected elements", selectedElements.Count);
			var dcs = selectedElements.Clone();
			dcs.Parent = this;
			dcs.MoveBy(10, 10);
			AddElements(dcs);
			DeselectAllElements();
			SelectElements(dcs);
		}

		/// <summary>
		///     Deselect the specified element
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
				var eventArgs = new SurfaceElementEventArgs {Elements = selectedElements};
				_movingElementChanged(this, eventArgs);
			}
		}

		/// <summary>
		///     Deselect all the selected elements
		/// </summary>
		public void DeselectAllElements()
		{
			DeselectElements(selectedElements);
		}

		/// <summary>
		///     Select the supplied element
		/// </summary>
		/// <param name="container"></param>
		/// <param name="invalidate">false to skip invalidation</param>
		/// <param name="generateEvents">false to skip event generation</param>
		public void SelectElement(IDrawableContainer container, bool invalidate = true, bool generateEvents = true)
		{
			if (!selectedElements.Contains(container))
			{
				selectedElements.Add(container);
				container.Selected = true;
				FieldAggregator.BindElement(container);
				if (generateEvents && _movingElementChanged != null)
				{
					var eventArgs = new SurfaceElementEventArgs
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
		}

		/// <summary>
		///     Select the supplied elements
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
				var eventArgs = new SurfaceElementEventArgs {Elements = selectedElements};
				_movingElementChanged(this, eventArgs);
			}
			ResumeLayout();
			Invalidate();
		}

		/// <summary>
		///     Property for accessing the elements on the surface
		/// </summary>
		public IDrawableContainerList Elements => _elements;

		public bool IsOnSurface(IDrawableContainer container)
		{
			return _elements.Contains(container);
		}

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
		///     Count all the VISIBLE steplabels in the surface, up to the supplied one
		/// </summary>
		/// <param name="stopAtContainer">can be null, if not the counting stops here</param>
		/// <returns>number of steplabels before the supplied container</returns>
		public int CountStepLabels(IDrawableContainer stopAtContainer)
		{
			var number = CounterStart;
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
        ///     Private method, the current bitmap is disposed the new one will stay.
        /// </summary>
        /// <param name="newBitmap">The new bitmap</param>
        /// <param name="dispose">true if the old bitmap needs to be disposed, when using undo this should not be true!!</param>
        public void SetBitmap(Bitmap newBitmap, bool dispose = false)
		{
			// Dispose
			if (_screenshot != null && dispose)
			{
				_screenshot.Dispose();
			}

			// Set new values
			Screenshot = newBitmap;
			Size = newBitmap.Size;

			_modified = true;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Count--;
				Log.Debug().WriteLine("Disposing surface!");
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
				foreach (var container in _elements)
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
		///     Undo the last action
		/// </summary>
		public void Undo()
		{
			if (_undoStack.Count > 0)
			{
				_inUndoRedo = true;
				var top = _undoStack.Pop();
				_redoStack.Push(top.Restore());
				_inUndoRedo = false;
			}
		}

		/// <summary>
		///     Undo an undo (=redo)
		/// </summary>
		public void Redo()
		{
			if (_redoStack.Count > 0)
			{
				_inUndoRedo = true;
				var top = _redoStack.Pop();
				_undoStack.Push(top.Restore());
				_inUndoRedo = false;
			}
		}

		/// <summary>
		///     This is called from the DrawingMode setter, which is not very correct...
		///     But here an element is created which is not yet draw, thus "undrawnElement".
		///     The element is than used while drawing on the surface.
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
					_undrawnElement = new RectangleContainer(this, _editorConfiguration);
					break;
				case DrawingModes.Ellipse:
					_undrawnElement = new EllipseContainer(this, _editorConfiguration);
					break;
				case DrawingModes.Text:
					_undrawnElement = new TextContainer(this, _editorConfiguration);
					break;
				case DrawingModes.SpeechBubble:
					_undrawnElement = new SpeechbubbleContainer(this, _editorConfiguration);
					break;
				case DrawingModes.StepLabel:
					_undrawnElement = new StepLabelContainer(this, _editorConfiguration);
					break;
				case DrawingModes.Line:
					_undrawnElement = new LineContainer(this, _editorConfiguration);
					break;
				case DrawingModes.Arrow:
					_undrawnElement = new ArrowContainer(this, _editorConfiguration);
					break;
				case DrawingModes.Highlight:
					_undrawnElement = new HighlightContainer(this, _editorConfiguration);
					break;
				case DrawingModes.Obfuscate:
					_undrawnElement = new ObfuscateContainer(this, _editorConfiguration);
					break;
				case DrawingModes.Crop:
					_cropContainer = new CropContainer(this, _editorConfiguration);
					_undrawnElement = _cropContainer;
					break;
				case DrawingModes.Bitmap:
					_undrawnElement = new BitmapContainer(this, _editorConfiguration);
					break;
				case DrawingModes.Path:
					_undrawnElement = new FreehandContainer(this, _editorConfiguration);
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

		/// <summary>
		///     Auto crop the image
		/// </summary>
		/// <returns>true if cropped</returns>
		public bool AutoCrop()
		{
			NativeRect cropRectangle;
			using (var tmpImage = GetBitmapForExport())
			{
				cropRectangle = tmpImage.FindAutoCropRectangle(_coreConfiguration.AutoCropDifference);
			}
			if (!IsCropPossible(ref cropRectangle))
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
		///     A simple clear
		/// </summary>
		/// <param name="newColor">The color for the background</param>
		public void Clear(Color newColor)
		{
			//create a blank bitmap the same size as original
			var newBitmap = Screenshot.CreateEmptyLike(Color.Empty);
			if (newBitmap != null)
			{
				// Make undoable
				MakeUndoable(new SurfaceBackgroundChangeMemento(this, null), false);
				SetBitmap(newBitmap, false);
				Invalidate();
			}
		}

		/// <summary>
		///     check if a crop is possible
		/// </summary>
		/// <param name="cropRectangle"></param>
		/// <returns>true if this is possible</returns>
		public bool IsCropPossible(ref NativeRect cropRectangle)
		{
			cropRectangle = new NativeRect(cropRectangle.Left, cropRectangle.Top, cropRectangle.Width, cropRectangle.Height).Normalize();
			if (cropRectangle.Left < 0)
			{
				cropRectangle = new NativeRect(0, cropRectangle.Top, cropRectangle.Width + cropRectangle.Left, cropRectangle.Height);
			}
			if (cropRectangle.Top < 0)
			{
				cropRectangle = new NativeRect(cropRectangle.Left, 0, cropRectangle.Width, cropRectangle.Height + cropRectangle.Top);
			}
			if (cropRectangle.Left + cropRectangle.Width > Width)
			{
				cropRectangle = new NativeRect(cropRectangle.Left, cropRectangle.Top, Width - cropRectangle.Left, cropRectangle.Height);
			}
			if (cropRectangle.Top + cropRectangle.Height > Height)
			{
				cropRectangle = new NativeRect(cropRectangle.Left, cropRectangle.Top, cropRectangle.Width, Height - cropRectangle.Top);
			}
			if (cropRectangle.Height > 0 && cropRectangle.Width > 0)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		///     Crop the surface
		/// </summary>
		/// <param name="cropRectangle"></param>
		/// <returns></returns>
		public bool ApplyCrop(NativeRect cropRectangle)
		{
			if (IsCropPossible(ref cropRectangle))
			{
				var imageRectangle = new NativeRect(NativePoint.Empty, Screenshot.Size);
				Bitmap tmpImage;
				// Make sure we have information, this this fails
				try
				{
					tmpImage = Screenshot.CloneBitmap(PixelFormat.DontCare, cropRectangle) as Bitmap;
				}
				catch (Exception ex)
				{
					ex.Data.Add("CropRectangle", cropRectangle);
					ex.Data.Add("Width", Screenshot.Width);
					ex.Data.Add("Height", Screenshot.Height);
					ex.Data.Add("Pixelformat", Screenshot.PixelFormat);
					throw;
				}

				var matrix = new Matrix();
				matrix.Translate(-cropRectangle.Left, -cropRectangle.Top, MatrixOrder.Append);
				// Make undoable
				MakeUndoable(new SurfaceBackgroundChangeMemento(this, matrix), false);

				// Do not dispose otherwise we can't undo the image!
				SetBitmap(tmpImage, false);
				_elements.Transform(matrix);
				if (_surfaceSizeChanged != null && !imageRectangle.Equals(new NativeRect(NativePoint.Empty, tmpImage.Size)))
				{
					_surfaceSizeChanged(this, null);
				}
				Invalidate();
				return true;
			}
			return false;
		}

		/// <summary>
		///     The background here is the captured image.
		///     This is called from the SurfaceBackgroundChangeMemento.
		/// </summary>
		/// <param name="previous"></param>
		/// <param name="matrix"></param>
		public void UndoBackgroundChange(Bitmap previous, Matrix matrix)
		{
			SetBitmap(previous, false);
			if (matrix != null)
			{
				_elements.Transform(matrix);
			}
			_surfaceSizeChanged?.Invoke(this, null);
			Invalidate();
		}

		/// <summary>
		///     Check if an adorner was "hit", and change the cursor if so
		/// </summary>
		/// <param name="mouseEventArgs">MouseEventArgs</param>
		/// <returns>IAdorner</returns>
		private IAdorner FindActiveAdorner(MouseEventArgs mouseEventArgs)
		{
			foreach (var drawableContainer in selectedElements)
			{
				foreach (var adorner in drawableContainer.Adorners)
				{
					if (adorner.IsActive || adorner.HitTest(mouseEventArgs.Location))
					{
						if (adorner.Cursor != null)
						{
							Cursor = adorner.Cursor;
						}
						return adorner;
					}
				}
			}
			return null;
		}

		/// <summary>
		///     This event handler is called when someone presses the mouse on a surface.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SurfaceMouseDown(object sender, MouseEventArgs e)
		{
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
					var rightClickedContainer = _elements.ClickableElementAt(_mouseStart.X, _mouseStart.Y);
					if (rightClickedContainer != null)
					{
						selectedList = new DrawableContainerList(ID) {rightClickedContainer};
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

			if (_cropContainer != null && (_undrawnElement == null || _undrawnElement != null && DrawingMode != DrawingModes.Crop))
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
					_drawingElement.Status = _undrawnElement.DefaultEditMode;
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
		///     This event handle is called when the mouse button is unpressed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SurfaceMouseUp(object sender, MouseEventArgs e)
		{
			// Handle Adorners
			var adorner = FindActiveAdorner(e);
			if (adorner != null)
			{
				adorner.MouseUp(sender, e);
				return;
			}

			var currentMouse = new NativePoint(e.X, e.Y);

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
				var element = _elements.ClickableElementAt(currentMouse.X, currentMouse.Y);
				var shiftModifier = (ModifierKeys & Keys.Shift) == Keys.Shift;
				if (element != null)
				{
					element.Invalidate();
					var alreadySelected = selectedElements.Contains(element);
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
		///     This event handler is called when the mouse moves over the surface
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SurfaceMouseMove(object sender, MouseEventArgs e)
		{
			// Handle Adorners
			var adorner = FindActiveAdorner(e);
			if (adorner != null)
			{
				adorner.MouseMove(sender, e);
				return;
			}

			var currentMouse = e.Location;

			Cursor = DrawingMode != DrawingModes.None ? Cursors.Cross : Cursors.Default;

			if (_mouseDown)
			{
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
		}

		/// <summary>
		///     This event handler is called when the surface is double clicked.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SurfaceDoubleClick(object sender, MouseEventArgs e)
		{
			selectedElements.OnDoubleClick();
			selectedElements.Invalidate();
		}

		/// <summary>
		///     Privately used to get the rendered image with all the elements on it.
		/// </summary>
		/// <param name="renderMode"></param>
		/// <returns></returns>
		private Bitmap GetBitmap(RenderMode renderMode)
		{
			// Generate a copy of the original image with a dpi equal to the default...
			var clone = _screenshot.CloneBitmap();
			// otherwise we would have a problem drawing the image to the surface... :(
			using (var graphics = Graphics.FromImage(clone))
			{
			    // do not set SmoothingMode, PixelOffsetMode, CompositingQuality and InterpolationMode the containers need to decide this themselves!
                _elements.Draw(graphics, clone, renderMode, new NativeRect(NativePoint.Empty, clone.Size));
			}
			return clone;
		}

		/// <summary>
		///     This is the event handler for the Paint Event, try to draw as little as possible!
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="paintEventArgs">PaintEventArgs</param>
		private void SurfacePaint(object sender, PaintEventArgs paintEventArgs)
		{
			var targetGraphics = paintEventArgs.Graphics;
			var clipRectangle = paintEventArgs.ClipRectangle;
			if (NativeRect.Empty.Equals(clipRectangle))
			{
				Log.Debug().WriteLine("Empty cliprectangle??");
				return;
			}

			if (_elements.HasIntersectingFilters(clipRectangle))
			{
			    if (_buffer != null && (_buffer.Width != Screenshot.Width || _buffer.Height != Screenshot.Height || _buffer.PixelFormat != Screenshot.PixelFormat))
			    {
			        _buffer.Dispose();
			        _buffer = null;
			    }
			    if (_buffer == null)
				{
					_buffer = BitmapFactory.CreateEmpty(Screenshot.Width, Screenshot.Height, Screenshot.PixelFormat, Color.Empty, Screenshot.HorizontalResolution, Screenshot.VerticalResolution);
					Log.Debug().WriteLine("Created buffer with size: {0}x{1}", Screenshot.Width, Screenshot.Height);
				}
				// Elements might need the bitmap, so we copy the part we need
				using (var graphics = Graphics.FromImage(_buffer))
				{
                    // do not set SmoothingMode, PixelOffsetMode, CompositingQuality and InterpolationMode the containers need to decide this themselves!
                    DrawBackground(graphics, clipRectangle);
					graphics.DrawImage(Screenshot, clipRectangle, clipRectangle, GraphicsUnit.Pixel);
					graphics.SetClip(targetGraphics);
					_elements.Draw(graphics, _buffer, RenderMode.Edit, clipRectangle);
				}
				targetGraphics.DrawImage(_buffer, clipRectangle, clipRectangle, GraphicsUnit.Pixel);
			}
			else
			{
				DrawBackground(targetGraphics, clipRectangle);
				targetGraphics.DrawImage(Screenshot, clipRectangle, clipRectangle, GraphicsUnit.Pixel);
				_elements.Draw(targetGraphics, null, RenderMode.Edit, clipRectangle);
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

		private void DrawBackground(Graphics targetGraphics, NativeRect clipRectangle)
		{
			// check if we need to draw the checkerboard
			if (Image.IsAlphaPixelFormat(Screenshot.PixelFormat) && _transparencyBackgroundBrush != null)
			{
				targetGraphics.FillRectangle(_transparencyBackgroundBrush, clipRectangle);
			}
			else
			{
				targetGraphics.Clear(BackColor);
			}
		}

		/// <summary>
		///     Draw a checkboard when capturing with transparency
		/// </summary>
		/// <param name="e">PaintEventArgs</param>
		protected override void OnPaintBackground(PaintEventArgs e)
		{
            // We do not need an OnPaintBackground
        }

        /// <summary>
        ///     This method is called to confirm/cancel "confirmable" elements, like the crop-container.
        ///     Called when pressing enter or using the "check" in the editor.
        /// </summary>
        /// <param name="confirm"></param>
        public void ConfirmSelectedConfirmableElements(bool confirm)
		{
			// create new collection so that we can iterate safely (selectedElements might change due with confirm/cancel)
			var selectedDCs = new List<IDrawableContainer>(selectedElements);
			foreach (var dc in selectedDCs)
			{
				if (dc.Equals(_cropContainer))
				{
					DrawingMode = DrawingModes.None;
					// No undo memento for the cropcontainer itself, only for the effect
					RemoveElement(_cropContainer, false);
					if (confirm)
					{
						ApplyCrop(_cropContainer.Bounds);
					}
					_cropContainer.Dispose();
					_cropContainer = null;
					break;
				}
			}
		}

		/// <summary>
		///     Deselect the specified elements
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
				var eventArgs = new SurfaceElementEventArgs
				{
					Elements = selectedElements
				};
				_movingElementChanged(this, eventArgs);
			}
			Invalidate();
		}

		/// <summary>
		///     Select all elements, this is called when Ctrl+A is pressed
		/// </summary>
		public void SelectAllElements()
		{
			SelectElements(_elements);
		}

		/// <summary>
		///     Process key presses on the surface, this is called from the editor (and NOT an override from the Control)
		/// </summary>
		/// <param name="k">Keys</param>
		/// <returns>false if no keys were processed</returns>
		public bool ProcessCmdKey(Keys k)
		{
			if (selectedElements.Count > 0)
			{
				var shiftModifier = (ModifierKeys & Keys.Shift) == Keys.Shift;
				var px = shiftModifier ? 10 : 1;
				var moveBy = NativePoint.Empty;

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
						ConfirmSelectedConfirmableElements(true);
						break;
					case Keys.Escape:
						ConfirmSelectedConfirmableElements(false);
						break;
					/*case Keys.Delete:
						RemoveSelectedElements();
						break;*/
					default:
						return false;
				}
				if (!NativePoint.Empty.Equals(moveBy))
				{
					selectedElements.MakeBoundsChangeUndoable(true);
					selectedElements.MoveBy(moveBy.X, moveBy.Y);
				}
				return true;
			}
			return false;
		}

		/// <summary>
		///     pulls selected elements up one level in hierarchy
		/// </summary>
		public void PullElementsUp()
		{
			_elements.PullElementsUp(selectedElements);
			_elements.Invalidate();
		}

		/// <summary>
		///     pushes selected elements up to top in hierarchy
		/// </summary>
		public void PullElementsToTop()
		{
			_elements.PullElementsToTop(selectedElements);
			_elements.Invalidate();
		}

		/// <summary>
		///     pushes selected elements down one level in hierarchy
		/// </summary>
		public void PushElementsDown()
		{
			_elements.PushElementsDown(selectedElements);
			_elements.Invalidate();
		}

		/// <summary>
		///     pushes selected elements down to bottom in hierarchy
		/// </summary>
		public void PushElementsToBottom()
		{
			_elements.PushElementsToBottom(selectedElements);
			_elements.Invalidate();
		}

		/// <summary>
		///     indicates whether the selected elements could be pulled up in hierarchy
		/// </summary>
		/// <returns>true if selected elements could be pulled up, false otherwise</returns>
		public bool CanPullSelectionUp()
		{
			return _elements.CanPullUp(selectedElements);
		}

		/// <summary>
		///     indicates whether the selected elements could be pushed down in hierarchy
		/// </summary>
		/// <returns>true if selected elements could be pushed down, false otherwise</returns>
		public bool CanPushSelectionDown()
		{
			return _elements.CanPushDown(selectedElements);
		}

		public void Element_FieldChanged(object sender, FieldChangedEventArgs e)
		{
			selectedElements.HandleFieldChangedEvent(sender, e);
		}

		#region Plugin interface implementations

		public IBitmapContainer AddImageContainer(Bitmap bitmap, int x, int y)
		{
			var bitmapContainer = new BitmapContainer(this, _editorConfiguration)
			{
				Bitmap = bitmap,
				Left = x,
				Top = y
			};
			AddElement(bitmapContainer);
			return bitmapContainer;
		}

		public IBitmapContainer AddImageContainer(string filename, int x, int y)
		{
			var bitmapContainer = new BitmapContainer(this, _editorConfiguration);
			bitmapContainer.Load(filename);
			bitmapContainer.Left = x;
			bitmapContainer.Top = y;
			AddElement(bitmapContainer);
			return bitmapContainer;
		}

		public IIconContainer AddIconContainer(Icon icon, int x, int y)
		{
			var iconContainer = new IconContainer(this, _editorConfiguration)
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
			var iconContainer = new IconContainer(this, _editorConfiguration);
			iconContainer.Load(filename);
			iconContainer.Left = x;
			iconContainer.Top = y;
			AddElement(iconContainer);
			return iconContainer;
		}

		public ICursorContainer AddCursorContainer(Cursor cursor, int x, int y)
		{
			var cursorContainer = new CursorContainer(this, _editorConfiguration)
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
			var cursorContainer = new CursorContainer(this, _editorConfiguration);
			cursorContainer.Load(filename);
			cursorContainer.Left = x;
			cursorContainer.Top = y;
			AddElement(cursorContainer);
			return cursorContainer;
		}

		public ITextContainer AddTextContainer(string text, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, FontFamily family, float size,
			bool italic, bool bold, bool shadow, int borderSize, Color color, Color fillColor)
		{
			var textContainer = new TextContainer(this, _editorConfiguration) {Text = text};
			textContainer.SetFieldValue(FieldTypes.FONT_FAMILY, family.Name);
			textContainer.SetFieldValue(FieldTypes.FONT_BOLD, bold);
			textContainer.SetFieldValue(FieldTypes.FONT_ITALIC, italic);
			textContainer.SetFieldValue(FieldTypes.FONT_SIZE, size);
			textContainer.SetFieldValue(FieldTypes.FILL_COLOR, fillColor);
			textContainer.SetFieldValue(FieldTypes.LINE_COLOR, color);
			textContainer.SetFieldValue(FieldTypes.LINE_THICKNESS, borderSize);
			textContainer.SetFieldValue(FieldTypes.SHADOW, shadow);
			// Make sure the Text fits
			textContainer.FitToText();
			// Align to Surface
			textContainer.AlignToParent(horizontalAlignment, verticalAlignment);

			AddElement(textContainer);
			return textContainer;
		}

		#endregion

		#region DragDrop

		private void OnDragEnter(object sender, DragEventArgs e)
		{
			if (Log.IsDebugEnabled())
			{
				Log.Debug().WriteLine("DragEnter got following formats: ");
				foreach (var format in ClipboardHelper.GetFormats(e.Data))
				{
					Log.Debug().WriteLine(format);
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
		///     Handle the drag/drop
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnDragDrop(object sender, DragEventArgs e)
		{
			var mouse = PointToClient(new NativePoint(e.X, e.Y));
			if (e.Data.GetDataPresent("Text"))
			{
				var possibleUrl = ClipboardHelper.GetText(e.Data);
			    try
			    {
			        if (possibleUrl != null && possibleUrl.StartsWith("http"))
			        {
			            var uri = new Uri(possibleUrl);

			            using (var image = uri.GetAsAsync<Bitmap>().Result)
			            {
			                if (image != null)
			                {
			                    AddImageContainer(image, mouse.X, mouse.Y);
			                    return;
			                }
			            }
                    }
			    }
			    catch (Exception ex)
			    {
                    Log.Error().WriteLine(ex, "Couldn't download url {0}", possibleUrl);
			    }
				// Test if it's an url and try to download the image so we have it in the original form
				if (possibleUrl != null && possibleUrl.StartsWith("http"))
				{
				    var uri = new Uri(possibleUrl);

				    using (var image = uri.GetAsAsync<Bitmap>().Result)
				    {
					    if (image != null)
						{
							AddImageContainer(image, mouse.X, mouse.Y);
							return;
						}
					}
				}
			}

			foreach (var image in ClipboardHelper.GetBitmaps(e.Data))
			{
				AddImageContainer(image, mouse.X, mouse.Y);
				mouse.Offset(10, 10);
				image.Dispose();
			}
		}

		#endregion
	}
}