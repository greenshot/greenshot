/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using Greenshot.Configuration;
using Greenshot.Drawing.Fields;
using Greenshot.Helpers;
using Greenshot.Plugin;
using Greenshot.Plugin.Drawing;
using GreenshotPlugin.Core;
using Greenshot.Memento;
using Greenshot.IniFile;
using Greenshot.Drawing.Filters;
using System.Drawing.Drawing2D;
using GreenshotPlugin.Controls;

namespace Greenshot.Drawing {
	public delegate void SurfaceElementEventHandler(object source, DrawableContainerList element);
	public delegate void SurfaceDrawingModeEventHandler(object source, DrawingModes drawingMode);
	
	public enum DrawingModes { None, Rect, Ellipse, Text, Line, Arrow, Crop, Highlight, Obfuscate, Bitmap, Path }

	/// <summary>
	/// Description of Surface.
	/// </summary>
	public class Surface : PictureBox, ISurface {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(Surface));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		public event SurfaceElementEventHandler MovingElementChanged;
		public event SurfaceDrawingModeEventHandler DrawingModeChanged;
		public event SurfaceSizeChangeEventHandler SurfaceSizeChanged;
		public event SurfaceMessageEventHandler SurfaceMessage;
		private bool inUndoRedo = false;
		private bool isSurfaceMoveMadeUndoable = false;
		private Stack<IMemento> undoStack = new Stack<IMemento>();
		private Stack<IMemento> redoStack = new Stack<IMemento>();
		private string lastSaveFullPath = null;

		public FieldAggregator FieldAggregator = new FieldAggregator();
		
		private ICaptureDetails captureDetails = null;
		
		private Bitmap buffer = null;

		Point mouseStart = Point.Empty;
		private bool mouseDown = false;
		private bool draggingInProgress = false;
		private DrawableContainer mouseDownElement = null;

		private DrawableContainerList elements = new DrawableContainerList();
		
		private DrawableContainerList selectedElements = new DrawableContainerList();
		private DrawableContainer drawingElement = null;
		private DrawableContainer undrawnElement = null;
		private DrawableContainer cropContainer = null;
		private IDrawableContainer cursorContainer = null;
		private TextureBrush transparencyBackgroundBrush;

		public IDrawableContainer CursorContainer {
			get {
				return cursorContainer;
			}
		}

		public bool HasCursor {
			get {
				return cursorContainer != null;
			}
		}

		public void RemoveCursor() {
			RemoveElement(cursorContainer, true);
			cursorContainer = null;
		}

		public TextureBrush TransparencyBackgroundBrush {
			get {
				return transparencyBackgroundBrush;
			}
			set {
				transparencyBackgroundBrush = value;
			}
		}

		public bool KeysLocked = false;
		
		private bool modified = false;
		public bool Modified {
			get {
				return modified;
			}
			set {
				modified = value;
			}
		}
		
		private DrawingModes drawingMode = DrawingModes.None;
		public DrawingModes DrawingMode {
			get {return drawingMode;}
			set {
				drawingMode = value;
				DrawingModeChanged.Invoke(this, drawingMode);
				DeselectAllElements();
				CreateUndrawnElement();
			}
		}
		
		public string LastSaveFullPath {
			get {
				return lastSaveFullPath;
			}
			set {
				lastSaveFullPath = value;
			}
		}

		public string UploadURL {
			get;
			set;
		}

		public ICaptureDetails CaptureDetails {
			get {return captureDetails;}
			set {captureDetails = value;}
		}
		
		public Surface() : base(){
			LOG.Debug("Creating a surface!");
			this.SizeMode = PictureBoxSizeMode.AutoSize;
			this.MouseDown += new MouseEventHandler(SurfaceMouseDown);
			this.MouseUp += new MouseEventHandler(SurfaceMouseUp);
			this.MouseMove += new MouseEventHandler(SurfaceMouseMove);
			this.MouseDoubleClick += new MouseEventHandler(SurfaceDoubleClick);
			this.Paint += new PaintEventHandler(SurfacePaint);
			this.AllowDrop = true;
			this.DragDrop += new DragEventHandler(OnDragDrop);
			this.DragEnter += new DragEventHandler(OnDragEnter);
			
			// bind selected & elements to this, otherwise they can't inform of modifications
			selectedElements.Parent = this;
			elements.Parent = this;
		}
		
		/// <summary>
		/// Private method, the current image is disposed the new one will stay.
		/// </summary>
		/// <param name="image">The new image</param>
		/// <param name="dispose">true if the old image needs to be disposed, when using undo this should not be true!!</param>
		private void SetImage(Image image, bool dispose) {
			// Dispose
			if (Image != null && dispose) {
				Image.Dispose();
			}

			// Set new values
			Image = image;
			modified = true;
		}

		public Surface(Image image) : this() {
			LOG.Debug("Got image with dimensions " + image.Width + "," + image.Height + " bpp: " + image.PixelFormat);
			SetImage(image, true);
		}
		
		public Surface(ICapture capture) : this(capture.Image) {
			// Make sure the image is NOT disposed, we took the reference directly into ourselves
			((Capture)capture).NullImage();

			if (capture.Cursor != null && capture.CursorVisible) {
				cursorContainer = AddIconContainer(capture.Cursor, capture.CursorLocation.X, capture.CursorLocation.Y);
				SelectElement(cursorContainer);
			}
			captureDetails = capture.CaptureDetails;
		}

		/**
		 * The public accessible Dispose
		 * Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
		 */
		public new void Dispose() {
			LOG.Debug("Disposing a surface!");
			if (buffer != null) {
				buffer.Dispose();
				buffer = null;
			}
			if (transparencyBackgroundBrush != null) {
				transparencyBackgroundBrush.Dispose();
				transparencyBackgroundBrush = null;
			}

			// Cleanup undo/redo stacks
			while(undoStack != null && undoStack.Count > 0) {
				undoStack.Pop().Dispose();
			}
			while(redoStack != null && redoStack.Count > 0) {
				redoStack.Pop().Dispose();
			}
			base.Dispose();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Undo the last action
		/// </summary>
		public void Undo() {
			if (undoStack.Count > 0) {
				inUndoRedo = true;
				IMemento top = undoStack.Pop();
				redoStack.Push(top.Restore());
				inUndoRedo = false;
			}
		} 

		/// <summary>
		/// Undo an undo (=redo)
		/// </summary>
		public void Redo() {
			if (redoStack.Count > 0) {
				inUndoRedo = true;
				IMemento top = redoStack.Pop();
				undoStack.Push(top.Restore());
				inUndoRedo = false;
			}
		}
		
		public bool CanUndo {
			get {
				return undoStack.Count > 0;
			}
		}
		public bool CanRedo {
			get {
				return redoStack.Count > 0;
			}
		}
		
		public LangKey UndoActionKey {
			get {
				if (CanUndo) {
					return undoStack.Peek().ActionKey;
				} else {
					return LangKey.none;
				}
			}
		}
		public LangKey RedoActionKey {
			get {
				if (CanRedo) {
					return redoStack.Peek().ActionKey;
				} else {
					return LangKey.none;
				}
			}
		}

		/// <summary>
		/// Make an action undo-able
		/// </summary>
		/// <param name="memento">The memento implementing the undo</param>
		public void MakeUndoable(IMemento memento, bool allowMerge) {
			if (inUndoRedo) {
				throw new InvalidOperationException("Involking do within an undo/redo action.");
			}
			if (memento != null) {
				bool allowPush = true;
				if (undoStack.Count > 0 && allowMerge) {
					// Check if merge is possible
					allowPush = !undoStack.Peek().Merge(memento);
				}
				if (allowPush) {
					// Clear the redo-stack and dispose
					while(redoStack.Count > 0) {
						redoStack.Pop().Dispose();
					}
					undoStack.Push(memento);
				}
			}
		}
		
		public void SaveElementsToStream(Stream streamWrite) {
			try {
				BinaryFormatter binaryWrite = new BinaryFormatter();
				binaryWrite.Serialize(streamWrite, elements);
			} catch (Exception e) {
				LOG.Error("Error serializing elements to stream.", e);
			}
		}
		
		public void LoadElementsFromStream(Stream streamRead) {
			try {
				BinaryFormatter binaryRead = new BinaryFormatter();
				DrawableContainerList loadedElements = (DrawableContainerList) binaryRead.Deserialize(streamRead);
				if (loadedElements != null) {
					loadedElements.Parent = this;
					DeselectAllElements();
					AddElements(loadedElements);
					SelectElements(loadedElements);
					FieldAggregator.BindElements(loadedElements);
				}
			} catch (Exception e) {
				LOG.Error("Error serializing elements from stream.", e);
			}
		}

		private void CreateUndrawnElement() {
			if(undrawnElement != null) {
				FieldAggregator.UnbindElement(undrawnElement);
			}
			switch (DrawingMode) {
				case DrawingModes.Rect:
					undrawnElement = new RectangleContainer(this);
					break;
				case DrawingModes.Ellipse:
					undrawnElement = new EllipseContainer(this);
					break;
				case DrawingModes.Text:
					undrawnElement = new TextContainer(this);
					break;
				case DrawingModes.Line:
					undrawnElement = new LineContainer(this);
					break;
				case DrawingModes.Arrow:
					undrawnElement = new ArrowContainer(this);
					break;
				case DrawingModes.Highlight:
					undrawnElement = new HighlightContainer(this);
					break;
				case DrawingModes.Obfuscate:
					undrawnElement = new ObfuscateContainer(this);
					break;
				case DrawingModes.Crop:
					cropContainer = new CropContainer(this);
					undrawnElement = cropContainer;
					break;
				case DrawingModes.Bitmap:
					undrawnElement = new BitmapContainer(this);
					break;
				case DrawingModes.Path:
					undrawnElement = new FreehandContainer(this);
					break;
				case DrawingModes.None:
					undrawnElement = null;
					break;
			}
			if (undrawnElement != null) {
				FieldAggregator.BindElement(undrawnElement);
			}
		}

		#region Plugin interface implementations
		public IBitmapContainer AddBitmapContainer(Bitmap bitmap, int x, int y) {
			BitmapContainer bitmapContainer = new BitmapContainer(this);
			bitmapContainer.Bitmap = bitmap;
			bitmapContainer.Left = x;
			bitmapContainer.Top = y;
			AddElement(bitmapContainer);
			return bitmapContainer;
		}

		public IBitmapContainer AddBitmapContainer(string filename, int x, int y) {
			BitmapContainer bitmapContainer = new BitmapContainer(this);
			bitmapContainer.Load(filename);
			bitmapContainer.Left = x;
			bitmapContainer.Top = y;
			AddElement(bitmapContainer);
			return bitmapContainer;
		}
		public IIconContainer AddIconContainer(Icon icon, int x, int y) {
			IconContainer iconContainer = new IconContainer(this);
			iconContainer.Icon = icon;
			iconContainer.Left = x;
			iconContainer.Top = y;
			AddElement(iconContainer);
			return iconContainer;
		}
		public IIconContainer AddIconContainer(string filename, int x, int y) {
			IconContainer iconContainer = new IconContainer(this);
			iconContainer.Load(filename);
			iconContainer.Left = x;
			iconContainer.Top = y;
			AddElement(iconContainer);
			return iconContainer;
		}
		public ICursorContainer AddCursorContainer(Cursor cursor, int x, int y) {
			CursorContainer cursorContainer = new CursorContainer(this);
			cursorContainer.Cursor = cursor;
			cursorContainer.Left = x;
			cursorContainer.Top = y;
			AddElement(cursorContainer);
			return cursorContainer;
		}
		public ICursorContainer AddCursorContainer(string filename, int x, int y) {
			CursorContainer cursorContainer = new CursorContainer(this);
			cursorContainer.Load(filename);
			cursorContainer.Left = x;
			cursorContainer.Top = y;
			AddElement(cursorContainer);
			return cursorContainer;
		}
		public IMetafileContainer AddMetafileContainer(string filename, int x, int y) {
			MetafileContainer metafileContainer = new MetafileContainer(this);
			metafileContainer.Load(filename);
			metafileContainer.Left = x;
			metafileContainer.Top = y;
			AddElement(metafileContainer);
			return metafileContainer;
		}
		public IMetafileContainer AddMetafileContainer(Metafile metafile, int x, int y) {
			MetafileContainer metafileContainer = new MetafileContainer(this);
			metafileContainer.Metafile = metafile;
			metafileContainer.Left = x;
			metafileContainer.Top = y;
			AddElement(metafileContainer);
			return metafileContainer;
		}

		public ITextContainer AddTextContainer(string text, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, FontFamily family, float size, bool italic, bool bold, bool shadow, int borderSize, Color color, Color fillColor) {
			TextContainer textContainer = new TextContainer(this);
			textContainer.Text = text;
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
			// Align to Surface
			textContainer.AlignToParent(horizontalAlignment, verticalAlignment);

			//AggregatedProperties.UpdateElement(textContainer);
			AddElement(textContainer);
			return textContainer;
		}
		#endregion

		#region DragDrop
		private List<string> GetFilenames(DragEventArgs e) {
			List<string> filenames = new List<string>();
			string[] dropFileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (dropFileNames != null && dropFileNames.Length > 0) {
				foreach(string filename in dropFileNames) {
					LOG.Debug("Found filename: " + filename);
					string ext=Path.GetExtension(filename).ToLower();
					if ((ext==".jpg") || (ext==".jpeg") ||(ext==".tiff") || (ext==".gif") || (ext==".png") || (ext==".bmp") || (ext==".ico") ||(ext==".wmf")) {
						filenames.Add(filename);
					}
				}
			}
			return filenames;
		}

		private void OnDragEnter(object sender, DragEventArgs e) {
			if(LOG.IsDebugEnabled) {
				LOG.Debug("DragEnter got following formats: ");
				foreach(string format in e.Data.GetFormats()) {
					LOG.Debug(format);
				}
			}
			if (draggingInProgress || (e.AllowedEffect & DragDropEffects.Copy) != DragDropEffects.Copy) {
				e.Effect=DragDropEffects.None;
			} else {
				List<string> filenames = GetFilenames(e);
				//|| e.Data.GetDataPresent(DataFormats.EnhancedMetafile, true)
				if ( (filenames != null && filenames.Count > 0) || e.Data.GetDataPresent(DataFormats.Bitmap, true) || e.Data.GetDataPresent(DataFormats.EnhancedMetafile, true)) {
					e.Effect=DragDropEffects.Copy;
				} else {
					e.Effect=DragDropEffects.None;
				}
			}
		}

		private void OnDragDrop(object sender, DragEventArgs e) {
			List<string> filenames = GetFilenames(e);
			Point mouse = this.PointToClient(new Point(e.X, e.Y));
			if ((filenames != null && filenames.Count > 0)) {
				foreach(string filename in filenames) {
					if (filename != null && filename.Trim().Length > 0) {
						LOG.Debug("Drop - filename: " + filename);
						if (filename.ToLower().EndsWith("wmf")) {
							AddMetafileContainer(filename, mouse.X, mouse.Y);
						} else {
							AddBitmapContainer(filename, mouse.X, mouse.Y);
						}
						mouse.Offset(10, 10);
					}
				}
			} else if (e.Data.GetDataPresent(DataFormats.Bitmap)) {
				AddBitmapContainer((Bitmap)e.Data.GetData(DataFormats.Bitmap, true), mouse.X, mouse.Y);
			} else if (e.Data.GetDataPresent(DataFormats.EnhancedMetafile)) {
				AddMetafileContainer((Metafile)e.Data.GetData(DataFormats.EnhancedMetafile, true), mouse.X, mouse.Y);
			}
		}
		
//		private void QueryContinueDragDrop(object sender, QueryContinueDragEventArgs e) {
//			LOG.Debug("QueryContinueDrag: " + e.Action);
//			if (e.EscapePressed) {
//				e.Action = DragAction.Cancel;
//			}
//		}
//		
//		private void GiveFeedbackDragDrop(object sender, GiveFeedbackEventArgs e) {
//			e.UseDefaultCursors = true;
//		}
		#endregion

		/// <summary>
		/// Auto crop the image
		/// </summary>
		/// <returns>true if cropped</returns>
		public bool AutoCrop() {
			Rectangle cropRectangle = ImageHelper.FindAutoCropRectangle(Image, conf.AutoCropDifference);
			if (isCropPossible(ref cropRectangle)) {
				DrawingMode = DrawingModes.Crop;
				cropContainer = new CropContainer(this);
				cropContainer.Left = cropRectangle.X;
				cropContainer.Top = cropRectangle.Y;
				cropContainer.Width = cropRectangle.Width;
				cropContainer.Height = cropRectangle.Height;
				DeselectAllElements();
				AddElement(cropContainer);
				SelectElement(cropContainer);
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// "Grow" the canvas with the specified pixels on the left, right, top and bottom. Using the backgroundColor.
		/// </summary>
		/// <param name="backgroundColor"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="top"></param>
		/// <param name="bottom"></param>
		public void GrowCanvas(Color backgroundColor, int left, int right, int top, int bottom) {
			Bitmap newImage = ImageHelper.GrowCanvas((Bitmap)Image, backgroundColor, left, right, top, bottom);
			// Make sure the elements move according to the offset the effect made the bitmap move
			elements.MoveBy(left, top);
			// Make undoable
			MakeUndoable(new SurfaceBackgroundChangeMemento(this, new Point(left, top)), false);
			SetImage(newImage, false);
			Invalidate();
			SurfaceSizeChanged(this);
		}
		
		/// <summary>
		/// Resize bitmap
		/// </summary>
		/// <param name="backgroundColor"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="top"></param>
		/// <param name="bottom"></param>
		public void ResizeBitmap(bool lockAspectRatio, bool canvasUsedNewSize, Color backgroundColor, int newWidth, int newHeight) {
			Point offset;
			Bitmap newImage = ImageHelper.ResizeBitmap((Bitmap)Image, lockAspectRatio, canvasUsedNewSize, backgroundColor, newWidth, newHeight, out offset);
			// Make sure the elements move according to the offset the effect made the bitmap move
			elements.MoveBy(offset.X, offset.Y);
			// Make undoable
			MakeUndoable(new SurfaceBackgroundChangeMemento(this, offset), false);
			SetImage(newImage, false);
			Invalidate();
			SurfaceSizeChanged(this);
		}

		/// <summary>
		/// Apply a bitmap effect to the surface
		/// </summary>
		/// <param name="effect"></param>
		public void ApplyBitmapEffect(Effects effect) {
			BackgroundForm backgroundForm = new BackgroundForm("Effect", "Please wait");
			backgroundForm.Show();
			Application.DoEvents();
			try {
				Rectangle imageRectangle = new Rectangle(Point.Empty, Image.Size);
				Bitmap newImage = null;
				Point offset = Point.Empty;
				switch (effect) {
					case Effects.Shadow:
						offset = new Point(6, 6);
						newImage = ImageHelper.CreateShadow((Bitmap)Image, 1f, 7, offset, PixelFormat.Format32bppRgb); //Image.PixelFormat);
						break;
					case Effects.TornEdge:
						offset = new Point(5, 5);
						using (Bitmap tmpImage = ImageHelper.CreateTornEdge((Bitmap)Image)) {
							newImage = ImageHelper.CreateShadow(tmpImage, 1f, 6, offset, PixelFormat.Format32bppRgb); //Image.PixelFormat);
						}
						break;
					case Effects.Border:
						newImage = ImageHelper.CreateBorder((Bitmap)Image, 2, Color.Black, Image.PixelFormat, out offset);
						break;
					case Effects.Grayscale:
						newImage = ImageHelper.CreateGrayscale((Bitmap)Image);
						break;
					case Effects.Invert:
						newImage = ImageHelper.CreateNegative((Bitmap)Image);
						break;
					case Effects.RotateClockwise:
					case Effects.RotateCounterClockwise:
						MakeUndoable(new DrawableContainerBoundsChangeMemento(elements.AsIDrawableContainerList()), false);
						RotateFlipType rotateFlipType = RotateFlipType.Rotate270FlipNone;
						if (effect == Effects.RotateClockwise) {
							rotateFlipType = RotateFlipType.Rotate90FlipNone;
						}
						foreach (DrawableContainer drawableContainer in elements) {
							if (drawableContainer.CanRotate) {
								drawableContainer.Rotate(rotateFlipType);
							}
						}
						newImage = ImageHelper.RotateFlip((Bitmap)Image, rotateFlipType);
						break;
				}
				// The following was added to correct any unneeded pixels, had the bad effect that sometimes everything was cropped... :(
				//Rectangle autoCropRectangle = ImageHelper.FindAutoCropRectangle(newImage, 0);
				//if (!Size.Empty.Equals(autoCropRectangle.Size) && !autoCropRectangle.Size.Equals(newImage.Size)) {
				//    LOG.InfoFormat("Crop to {0}", autoCropRectangle);
				//    using (Bitmap tmpImage = newImage) {
				//        newImage = ImageHelper.CloneArea(newImage, autoCropRectangle, PixelFormat.DontCare);
				//    }
				//    // Fix offset
				//    offset = new Point(offset.X - autoCropRectangle.X, offset.Y - autoCropRectangle.Y);
				//} else {
				//    LOG.DebugFormat("No cropping needed!");
				//}

				if (newImage != null) {
					// Make sure the elements move according to the offset the effect made the bitmap move
					elements.MoveBy(offset.X, offset.Y);
					// Make undoable
					MakeUndoable(new SurfaceBackgroundChangeMemento(this, offset), false);
					SetImage(newImage, false);
					Invalidate();
					if (SurfaceSizeChanged != null && !imageRectangle.Equals(new Rectangle(Point.Empty, newImage.Size))) {
						SurfaceSizeChanged(this);
					}
				}
			} finally {
				// Always close the background form
				backgroundForm.CloseDialog();
			}
		}

		/// <summary>
		/// check if a crop is possible
		/// </summary>
		/// <param name="cropRectangle"></param>
		/// <returns>true if this is possible</returns>
		public bool isCropPossible(ref Rectangle cropRectangle) {
			cropRectangle = Helpers.GuiRectangle.GetGuiRectangle(cropRectangle.Left, cropRectangle.Top, cropRectangle.Width, cropRectangle.Height);
			if (cropRectangle.Left < 0) cropRectangle = new Rectangle(0, cropRectangle.Top, cropRectangle.Width + cropRectangle.Left, cropRectangle.Height);
			if (cropRectangle.Top < 0) cropRectangle = new Rectangle(cropRectangle.Left, 0, cropRectangle.Width, cropRectangle.Height + cropRectangle.Top);
			if (cropRectangle.Left + cropRectangle.Width > Width) cropRectangle = new Rectangle(cropRectangle.Left, cropRectangle.Top, Width - cropRectangle.Left, cropRectangle.Height);
			if (cropRectangle.Top + cropRectangle.Height > Height) cropRectangle = new Rectangle(cropRectangle.Left, cropRectangle.Top, cropRectangle.Width, Height - cropRectangle.Top);

			if (cropRectangle.Height > 0 && cropRectangle.Width > 0) {
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
		public void SendMessageEvent(object source, SurfaceMessageTyp messageType, string message) {
			if (SurfaceMessage != null) {
				SurfaceMessageEventArgs eventArgs = new SurfaceMessageEventArgs();
				eventArgs.Message = message;
				eventArgs.MessageType = messageType;
				eventArgs.Surface = this;
				SurfaceMessage(source, eventArgs);
			}
		}

		/// <summary>
		/// Crop the surface
		/// </summary>
		/// <param name="cropRectangle"></param>
		/// <returns></returns>
		public bool ApplyCrop(Rectangle cropRectangle) {
			if (isCropPossible(ref cropRectangle)) {
				Rectangle imageRectangle = new Rectangle(Point.Empty, Image.Size);
				// we should not forget to Dispose the images!!
				Bitmap tmpImage = ImageHelper.CloneArea(Image, cropRectangle, PixelFormat.DontCare);

				Point offset = new Point(-cropRectangle.Left, -cropRectangle.Top);
				// Make undoable
				MakeUndoable(new SurfaceBackgroundChangeMemento(this, offset), false);

				SetImage(tmpImage, false);
				elements.MoveBy(offset.X, offset.Y);
				if (SurfaceSizeChanged != null && !imageRectangle.Equals(new Rectangle(Point.Empty, tmpImage.Size))) {
					SurfaceSizeChanged(this);
				}
				Invalidate();
				return true;
			}
			return false;
		}

		public void UndoBackgroundChange(Image previous, Point offset) {
			SetImage(previous, false);
			elements.MoveBy(offset.X, offset.Y);
			if (SurfaceSizeChanged != null) {
				SurfaceSizeChanged(this);
			}
			Invalidate();
		}
		
		void SurfaceMouseDown(object sender, MouseEventArgs e) {
			mouseStart = e.Location;
		
			// check contextmenu
			if (e.Button == MouseButtons.Right) {
				DrawableContainerList selectedList = null;
				if (selectedElements != null && selectedElements.Count > 0) {
					selectedList = selectedElements;
				} else {
					// Single element
					DrawableContainer rightClickedContainer = elements.ClickableElementAt(mouseStart.X, mouseStart.Y);
					if (rightClickedContainer != null) {
						selectedList = new DrawableContainerList();
						selectedList.Add(rightClickedContainer);
					}
				}
				if (selectedList != null && selectedList.Count > 0) {
					selectedList.ShowContextMenu(e, this);
				}
				return;
			}

			mouseDown = true;
			isSurfaceMoveMadeUndoable = false;

			if (cropContainer != null && ((undrawnElement == null) || (undrawnElement != null && DrawingMode != DrawingModes.Crop))) {
				RemoveElement(cropContainer, false);
				cropContainer = null;
				drawingElement = null;
			}

			if (drawingElement == null && DrawingMode != DrawingModes.None) {
				if (undrawnElement == null) {
					DeselectAllElements();
					if(undrawnElement == null) {
						CreateUndrawnElement();
					}
				}
				drawingElement = undrawnElement;
				drawingElement.Status = EditStatus.DRAWING;
				undrawnElement = null;
				// if a new element has been drawn, set location and register it
				if (drawingElement != null) {
					drawingElement.PropertyChanged += ElementPropertyChanged;
					if (!drawingElement.HandleMouseDown(mouseStart.X, mouseStart.Y)) {
						drawingElement.Left = mouseStart.X;
						drawingElement.Top = mouseStart.Y;
					}
					AddElement(drawingElement);
					drawingElement.Selected = true;
				}
			} else {
				// check whether an existing element was clicked
				// we save mouse down element separately from selectedElements (checked on mouse up),
				// since it could be moved around before it is actually selected
				mouseDownElement = elements.ClickableElementAt(mouseStart.X, mouseStart.Y);

				if (mouseDownElement != null) {
					mouseDownElement.Status = EditStatus.MOVING;
				}
			}
		}
		
		void SurfaceMouseUp(object sender, MouseEventArgs e) {
			Point currentMouse = new Point(e.X, e.Y);

			elements.Status = EditStatus.IDLE;
			if (mouseDownElement != null) {
				mouseDownElement.Status = EditStatus.IDLE;
			}
			mouseDown = false;
			mouseDownElement = null;
			if (DrawingMode == DrawingModes.None) {
				// check whether an existing element was clicked
				DrawableContainer element = elements.ClickableElementAt(currentMouse.X, currentMouse.Y);
				bool shiftModifier = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
				if (element != null) {
					element.Invalidate();
					bool alreadySelected = selectedElements.Contains(element);
					if (shiftModifier) {
						if (alreadySelected) {
							DeselectElement(element);
						} else {
							SelectElement(element);
						}
					} else {
						if (!alreadySelected) {
							DeselectAllElements();
							SelectElement(element);
						}
					}
				} else if(!shiftModifier) {
					DeselectAllElements();
				}
			}
			
			if (selectedElements.Count > 0) {
				selectedElements.ShowGrippers();
				selectedElements.Selected = true;
			}

			if (drawingElement != null) {
				if (!drawingElement.InitContent()) {
					elements.Remove(drawingElement);
					drawingElement.Invalidate();
				} else {
					drawingElement.HandleMouseUp(currentMouse.X, currentMouse.Y);
					drawingElement.Invalidate();
					if (Math.Abs(drawingElement.Width) < 5 && Math.Abs(drawingElement.Height) < 5) {
						drawingElement.Width = 25;
						drawingElement.Height = 25;
					}
					SelectElement(drawingElement);
					drawingElement.Selected = true;
				}
				drawingElement = null;
			}
		}
		
		void SurfaceMouseMove(object sender, MouseEventArgs e) {
			Point currentMouse = e.Location;

			if (DrawingMode != DrawingModes.None) {
				Cursor = Cursors.Cross;
			} else {
				Cursor = Cursors.Default;
			}

			if(mouseDown) {
				if(mouseDownElement != null) { // an element is currently dragged
					mouseDownElement.Invalidate();
					selectedElements.HideGrippers();
					// Move the element
					if(mouseDownElement.Selected) {
						if (!isSurfaceMoveMadeUndoable) {
							// Only allow one undoable per mouse-down/move/up "cycle"
							isSurfaceMoveMadeUndoable = true;
							selectedElements.MakeBoundsChangeUndoable(false);
						}
						// dragged element has been selected before -> move all
						selectedElements.MoveBy(currentMouse.X - mouseStart.X, currentMouse.Y - mouseStart.Y);
					} else {
						if (!isSurfaceMoveMadeUndoable) {
							// Only allow one undoable per mouse-down/move/up "cycle"
							isSurfaceMoveMadeUndoable = true;
							mouseDownElement.MakeBoundsChangeUndoable(false);
						}
						// dragged element is not among selected elements -> just move dragged one
						mouseDownElement.MoveBy(currentMouse.X - mouseStart.X, currentMouse.Y - mouseStart.Y);
					}
					mouseStart = currentMouse;
					mouseDownElement.Invalidate();
					modified = true;
				} else if(drawingElement != null) {
					drawingElement.HandleMouseMove(currentMouse.X, currentMouse.Y);
					modified = true;
				}
			}
		}
		
		void SurfaceDoubleClick(object sender, MouseEventArgs e) {
			selectedElements.OnDoubleClick();
			selectedElements.Invalidate();
		}

		private Image GetImage(RenderMode renderMode) {
			// Generate a copy of the original image with a dpi equal to the default...
			Bitmap clone = ImageHelper.Clone(Image);
			// otherwise we would have a problem drawing the image to the surface... :(
			using (Graphics graphics = Graphics.FromImage(clone)) {
				// Do not set the following, the containers need to decide themselves
				//graphics.SmoothingMode = SmoothingMode.HighQuality;
				//graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				//graphics.CompositingQuality = CompositingQuality.HighQuality;
				//graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				elements.Draw(graphics, clone, renderMode, new Rectangle(Point.Empty, clone.Size));
			}
			return clone;
		}
		
		public Image GetImageForExport() {
			return GetImage(RenderMode.EXPORT);
		}
		
		/// <summary>
		/// This is the event handler for the Paint Event, try to draw as little as possible!
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void SurfacePaint(object sender, PaintEventArgs e) {
			Graphics targetGraphics = e.Graphics;
			Rectangle clipRectangle = e.ClipRectangle;
			if (Rectangle.Empty.Equals(clipRectangle)) {
				LOG.Debug("Empty cliprectangle??");
				return;
			}

			if (elements.hasIntersectingFilters(clipRectangle)) {
				if (buffer != null) {
					if (buffer.Width != Image.Width || buffer.Height != Image.Height || buffer.PixelFormat != Image.PixelFormat) {
						buffer.Dispose();
						buffer = null;
					}
				}
				if (buffer == null) {
					LOG.DebugFormat("Created buffer with size: {0}x{1}", Image.Width, Image.Height);
					buffer = new Bitmap(Image.Width, Image.Height, Image.PixelFormat);
				}
				// Elements might need the bitmap, so we copy the part we need
				using (Graphics graphics = Graphics.FromImage(buffer)) {
					// do not set the following, the containers need to decide this themselves!
					//graphics.SmoothingMode = SmoothingMode.HighQuality;
					//graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
					//graphics.CompositingQuality = CompositingQuality.HighQuality;
					//graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
					graphics.DrawImage(Image, clipRectangle, clipRectangle, GraphicsUnit.Pixel);
					graphics.SetClip(targetGraphics);
					elements.Draw(graphics, buffer, RenderMode.EDIT, clipRectangle);
				}
				targetGraphics.DrawImage(buffer, clipRectangle, clipRectangle, GraphicsUnit.Pixel);
			} else {
				// Only "simple" elements need to be redrawn, as the image is already drawn before getting the event we don't need the next line:
				// targetGraphics.DrawImage(Image, clipRectangle, clipRectangle, GraphicsUnit.Pixel);
				elements.Draw(targetGraphics, null, RenderMode.EDIT, clipRectangle);
			}
		}
		
		// Draw a checkboard when capturing with transparency
		protected override void OnPaintBackground(PaintEventArgs e) {
			// check if we need to draw the checkerboard
			if (Image.PixelFormat == PixelFormat.Format32bppArgb && transparencyBackgroundBrush != null) {
				Graphics targetGraphics = e.Graphics;
				Rectangle clipRectangle = e.ClipRectangle;
				targetGraphics.FillRectangle(transparencyBackgroundBrush, clipRectangle);
			} else {
				Graphics targetGraphics = e.Graphics;
				targetGraphics.Clear(this.BackColor);
				//base.OnPaintBackground(e);
			}
		} 

		/// <summary>
		/// Wrapper for makeUndoable flag which was introduced later, will call AddElement with makeundoable set to true
		/// </summary>
		/// <param name="element">the new element</param>
		public void AddElement(DrawableContainer element) {
			AddElement(element, true);
		}

		/// <summary>
		/// Add a new element to the surface
		/// </summary>
		/// <param name="element">the new element</param>
		/// <param name="makeUndoable">true if the adding should be undoable</param>
		public void AddElement(DrawableContainer element, bool makeUndoable) {
			elements.Add(element);
			element.FieldChanged += element_FieldChanged;
			element.PropertyChanged += ElementPropertyChanged;
			if(element.Status == EditStatus.UNDRAWN) {
				element.Status = EditStatus.IDLE;
			}
			element.Invalidate();
			if (makeUndoable) {
				MakeUndoable(new AddElementMemento(this, element), false);
			}
			modified = true;
		}

		public void RemoveElement(IDrawableContainer elementToRemove, bool makeUndoable) {
			DrawableContainer element = elementToRemove as DrawableContainer;
			DeselectElement(element);
			elements.Remove(element);
			element.FieldChanged -= element_FieldChanged;
			element.PropertyChanged -= ElementPropertyChanged;
			// Do not dispose, the memento should!! element.Dispose();
			element.Invalidate();
			if (makeUndoable) {
				MakeUndoable(new DeleteElementMemento(this, element), false);
			}
			modified = true;
		}
		
		public void AddElements(DrawableContainerList elementsToAdd) {
			foreach(DrawableContainer element in elementsToAdd) {
				AddElement(element, true);
			}
		}
		
		public bool HasSelectedElements() {
			return (selectedElements != null && selectedElements.Count > 0);
		}
		
		public void RemoveSelectedElements() {
			if (HasSelectedElements()) {
				// As RemoveElement will remove the element from the selectedElements list we need to copy the element
				// to another list.
				List<DrawableContainer> elementsToRemove = new List<DrawableContainer>();
				foreach (DrawableContainer element in selectedElements) {
					// Collect to remove later
					elementsToRemove.Add(element);
				}
				// Remove now
				foreach(DrawableContainer element in elementsToRemove) {
					RemoveElement(element, true);
				}
				selectedElements.Clear();
				MovingElementChanged(this, selectedElements);
			}
		}
		
		public void CutSelectedElements() {
			if (HasSelectedElements()) {
				ClipboardHelper.SetClipboardData(typeof(DrawableContainerList), selectedElements);
				RemoveSelectedElements();
			}
		}
		
		public void CopySelectedElements() {
			if (HasSelectedElements()) {
				ClipboardHelper.SetClipboardData(typeof(DrawableContainerList), selectedElements);
			}
		}
		
		public void ConfirmSelectedConfirmableElements(bool confirm){
			// create new collection so that we can iterate safely (selectedElements might change due with confirm/cancel)
			List<DrawableContainer> selectedDCs = new List<DrawableContainer>(selectedElements);
			foreach(DrawableContainer dc in selectedDCs){
				if(dc.Equals(cropContainer)){
					DrawingMode = DrawingModes.None;
					// No undo memento for the cropcontainer itself, only for the effect
					RemoveElement(cropContainer, false);
					if(confirm) {
						ApplyCrop(cropContainer.Bounds);
					}
					cropContainer.Dispose();
				}
			}
		}
		
		public void PasteElementFromClipboard() {
			List<string> formats = ClipboardHelper.GetFormats();
			if (formats == null || formats.Count == 0) {
				return;
			}
			if (LOG.IsDebugEnabled) {
				LOG.Debug("List of clipboard formats available for pasting:");
				foreach(string format in formats) {
					LOG.Debug("\tgot format: " + format);
				}
			}
			
			if (formats.Contains(typeof(DrawableContainerList).FullName)) {
				DrawableContainerList dcs = (DrawableContainerList)ClipboardHelper.GetClipboardData(typeof(DrawableContainerList));
				if (dcs != null) {
					dcs.Parent = this;
					dcs.MoveBy(10,10);
					AddElements(dcs);
					FieldAggregator.BindElements(dcs);
					DeselectAllElements();
					SelectElements(dcs);
				}
			} else if (ClipboardHelper.ContainsImage()) {
				using (Image image = ClipboardHelper.GetImage()) {
					if (image != null) {
						DeselectAllElements();
						IBitmapContainer bitmapContainer = AddBitmapContainer(image as Bitmap, 0, 0);
						SelectElement(bitmapContainer);
					}
				}
			} else if (ClipboardHelper.ContainsText()) {
				string text = ClipboardHelper.GetText();
				if (text != null) {
					DeselectAllElements();
					ITextContainer textContainer = AddTextContainer(text, HorizontalAlignment.Center, VerticalAlignment.CENTER,
						FontFamily.GenericSansSerif, 12f, false, false, false, 2, Color.Black, Color.Transparent);
					SelectElement(textContainer);
				}
			}
		}
		
		public void DuplicateSelectedElements() {
			if(LOG.IsDebugEnabled) LOG.Debug("Duplicating "+selectedElements.Count+" selected elements");
			DrawableContainerList dcs = selectedElements.Clone();
			dcs.Parent = this;
			dcs.MoveBy(10,10);
			AddElements(dcs);
			DeselectAllElements();
			SelectElements(dcs);
		}
		
		public void DeselectElement(IDrawableContainer container) {
			DrawableContainer element = container as DrawableContainer;
			element.HideGrippers();
			element.Selected = false;
			selectedElements.Remove(element);
			FieldAggregator.UnbindElement(element);
			if (MovingElementChanged != null) {
				MovingElementChanged(this, selectedElements);
			}
		}

		public void DeselectAllElements() {
			if (HasSelectedElements()) {
				while(selectedElements.Count > 0) {
					DrawableContainer element = selectedElements[0];
					element.Invalidate();
					element.HideGrippers();
					element.Selected = false;
					selectedElements.Remove(element);
					FieldAggregator.UnbindElement(element);
				}
				if (MovingElementChanged != null) {
					MovingElementChanged(this, selectedElements);
				}
			}
		}
		
		public void SelectElement(IDrawableContainer container) {
			DrawableContainer element = container as DrawableContainer;
			if(!selectedElements.Contains(element)) {
				selectedElements.Add(element);
				element.ShowGrippers();
				element.Selected = true;
				FieldAggregator.BindElement(element);
				if (MovingElementChanged != null) {
					MovingElementChanged(this, selectedElements);
				}
				element.Invalidate();
			}
		}
		
		public void SelectAllElements() {
			SelectElements(elements);
		}
		
		public void SelectElements(DrawableContainerList elements) {
			foreach(DrawableContainer element in elements) {
				SelectElement(element);
			}
		}

		public void ProcessCmdKey(Keys k) {
			if (selectedElements.Count > 0) {
				bool shiftModifier = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
				int px = shiftModifier ? 10 : 1;
				Point moveBy = Point.Empty;
				
				switch (k) {
					case Keys.Left:
					case Keys.Left | Keys.Shift:
						moveBy = new Point(-px, 0);
						break;
					case Keys.Up:
					case Keys.Up | Keys.Shift:
						moveBy = new Point(0, -px);
						break;
					case Keys.Right:
					case Keys.Right | Keys.Shift:
						moveBy = new Point(px, 0);
						break;
					case Keys.Down:
					case Keys.Down | Keys.Shift:
						moveBy = new Point(0, px);
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
						return;
				}
				if (!Point.Empty.Equals(moveBy)) {
					selectedElements.MakeBoundsChangeUndoable(true);
					selectedElements.MoveBy(moveBy.X, moveBy.Y);
				}
			}
		}
		
		public DrawableContainerList Elements {
			get {
				return elements;
			}
		}

		/// <summary>
		/// pulls selected elements up one level in hierarchy
		/// </summary>
		public void PullElementsUp() {
			elements.PullElementsUp(selectedElements);
			elements.Invalidate();
		}
		
		/// <summary>
		/// pushes selected elements up to top in hierarchy
		/// </summary>
		public void PullElementsToTop() {
			elements.PullElementsToTop(selectedElements);
			elements.Invalidate();
		}
		
		/// <summary>
		/// pushes selected elements down one level in hierarchy
		/// </summary>
		public void PushElementsDown() {
			elements.PushElementsDown(selectedElements);
			elements.Invalidate();
		}
		
		/// <summary>
		/// pushes selected elements down to bottom in hierarchy
		/// </summary>
		public void PushElementsToBottom() {
			elements.PushElementsToBottom(selectedElements);
			elements.Invalidate();
		}
		
		/// <summary>
		/// indicates whether the selected elements could be pulled up in hierarchy
		/// </summary>
		/// <returns>true if selected elements could be pulled up, false otherwise</returns>
		public bool CanPullSelectionUp() {
			return elements.CanPullUp(selectedElements);
		}
		
		/// <summary>
		/// indicates whether the selected elements could be pushed down in hierarchy
		/// </summary>
		/// <returns>true if selected elements could be pushed down, false otherwise</returns>
		public bool CanPushSelectionDown() {
			return elements.CanPushDown(selectedElements);
		}
		
		public void ElementPropertyChanged(object sender, PropertyChangedEventArgs e) {
			//Invalidate();
		}
		
		public void element_FieldChanged(object sender, FieldChangedEventArgs e) {
			selectedElements.HandleFieldChangedEvent(sender, e);
		}
	}
}
