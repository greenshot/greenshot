/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

using Greenshot.Capturing;
using Greenshot.Configuration;
using Greenshot.Core;
using Greenshot.Drawing.Fields;
using Greenshot.Drawing.Filters;
using Greenshot.Helpers;
using Greenshot.Plugin;

namespace Greenshot.Drawing {
	public delegate void SurfaceElementEventHandler(object source, DrawableContainerList element);
	public delegate void SurfaceDrawingModeEventHandler(object source, DrawingModes drawingMode);
	
	public enum DrawingModes { None, Rect, Ellipse, Text, Line, Arrow, Crop, Highlight, Obfuscate, Bitmap }
	/// <summary>
	/// Description of Surface.
	/// </summary>
	public class Surface : PictureBox, ISurface {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(Surface));
		public event SurfaceElementEventHandler MovingElementChanged;
		public event SurfaceDrawingModeEventHandler DrawingModeChanged;
		
		public FieldAggregator FieldAggregator = new FieldAggregator();
		
		private ICaptureDetails captureDetails = null;

		private int mX;
		private int mY;
		private bool mouseDown = false;
		private bool draggingInProgress = false;
		private DrawableContainer mouseDownElement = null;
		
		private Image originalImage;

		private DrawableContainerList elements = new DrawableContainerList();
		
		private DrawableContainerList selectedElements = new DrawableContainerList();
		private DrawableContainer drawingElement = null;
		private DrawableContainer undrawnElement = null;
		private DrawableContainer cropContainer = null;
		
		public bool KeysLocked = false;
		
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

		public Image OriginalImage {
			get { return originalImage; }
		}

		public ICaptureDetails CaptureDetails {
			get {return captureDetails;}
			set {captureDetails = value;}
		}
		
		public Surface() {
			LOG.Debug("Creating a surface!");
			SizeMode = PictureBoxSizeMode.AutoSize;
			this.MouseDown += new MouseEventHandler(SurfaceMouseDown);
			this.MouseUp += new MouseEventHandler(SurfaceMouseUp);
			this.MouseMove += new MouseEventHandler(SurfaceMouseMove);
			this.MouseDoubleClick += new MouseEventHandler(SurfaceDoubleClick);
			this.Paint += new PaintEventHandler(SurfacePaint);
			this.AllowDrop = true;
			this.DragDrop += new DragEventHandler(OnDragDrop);
			this.DragEnter += new DragEventHandler(OnDragEnter);
		}
		
		private void SetImage(Image image) {
			if (originalImage != null) {
				originalImage.Dispose();
			}
			if (Image != null) {
				Image.Dispose();
			}
			originalImage = new Bitmap(image);
			Image = new Bitmap(image);
		}

		public Surface(Image image) : this() {
			LOG.Debug("Got image with dimensions " + image.Width + "," + image.Height + " bpp: " + image.PixelFormat);
			SetImage(image);
		}
		
		public Surface(ICapture capture) : this(capture.Image) {
			if (capture.Cursor != null && capture.CursorVisible) {
				SelectElement(AddIconContainer(capture.Cursor, capture.CursorLocation.X, capture.CursorLocation.Y));
			}
			captureDetails = capture.CaptureDetails;
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
		private HtmlContainer AddHtmlContainer(string html, int x, int y) {
			HtmlContainer htmlContainer = new HtmlContainer(this);
			htmlContainer.Left = x;
			htmlContainer.Top = y;
			htmlContainer.Html = html;
			AddElement(htmlContainer);
			return htmlContainer;
		}
		private UrlContainer AddUrlContainer(string url, int x, int y) {
			UrlContainer urlContainer = new UrlContainer(this);
			urlContainer.Left = x;
			urlContainer.Top = y;
			urlContainer.Url = url;
			AddElement(urlContainer);
			return urlContainer;
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
					if ((ext==".jpg") || (ext==".gif") || (ext==".png") || (ext==".bmp")) {
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
				if ( (filenames != null && filenames.Count > 0)
					|| e.Data.GetDataPresent(DataFormats.Bitmap, true) 
					//|| e.Data.GetDataPresent(DataFormats.EnhancedMetafile, true)
					) {
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
					LOG.Debug("Drop - filename: " + filename);
					AddBitmapContainer(filename, mouse.X, mouse.Y);
					mouse.Offset(10, 10);
				}
			} else if (e.Data.GetDataPresent(DataFormats.Bitmap)) {
				AddBitmapContainer((Bitmap)e.Data.GetData(DataFormats.Bitmap, true), mouse.X, mouse.Y);
//			} else if (e.Data.GetDataPresent(DataFormats.EnhancedMetafile)) {
//				AddBitmapContainer((Image)e.Data.GetData(DataFormats.EnhancedMetafile, true), mouse.X, mouse.Y);
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

		void ApplyCrop() {
			Rectangle r = cropContainer.Bounds;
			r = Helpers.GuiRectangle.GetGuiRectangle(r.Left, r.Top, r.Width, r.Height);
			if (r.Left < 0) r = new Rectangle(0, r.Top, r.Width + r.Left, r.Height);
			if (r.Top < 0) r = new Rectangle(r.Left, 0, r.Width, r.Height + r.Top);
			if (r.Left + r.Width > Width) r = new Rectangle(r.Left, r.Top, Width - r.Left, r.Height);
			if (r.Top + r.Height > Height) r = new Rectangle(r.Left, r.Top, r.Width, Height - r.Top);

			if (r.Height > 0 && r.Width > 0) {
				// we should not forget to Dispose the images!!
				using (Bitmap tmpImage = ((Bitmap)originalImage).Clone(r, originalImage.PixelFormat)) {
					tmpImage.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);
					SetImage(tmpImage);
					elements.MoveBy(-r.Left, -r.Top);
				}
			}
		}
		
		void SurfaceMouseDown(object sender, MouseEventArgs e) {
			mX = e.X; mY = e.Y;
			mouseDown = true;

			if (cropContainer != null && ((undrawnElement == null) || (undrawnElement != null && DrawingMode != DrawingModes.Crop))) {
				RemoveElement(cropContainer);
				cropContainer = null;
				drawingElement = null;
			}

			if(drawingElement == null && DrawingMode != DrawingModes.None) {
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
					drawingElement.Left = e.X;
					drawingElement.Top = e.Y;
					AddElement(drawingElement);
					drawingElement.Selected = true;
				}
			} else {
				// check whether an existing element was clicked
				// we save mouse down element separately from selectedElements (checked on mouse up),
				// since it could be moved around before it is actually selected
				mouseDownElement = elements.ClickableElementAt(e.X, e.Y);
				if(mouseDownElement != null) {
					mouseDownElement.Status = EditStatus.MOVING;
				}
			}
		}
		
		void SurfaceMouseUp(object sender, MouseEventArgs e) {
			elements.Status = EditStatus.IDLE;
			if(mouseDownElement != null) {
				mouseDownElement.Status = EditStatus.IDLE;
			}
			mouseDown = false;
			mouseDownElement = null;
			if(DrawingMode == DrawingModes.None) { // check whether an existing element was clicked
				DrawableContainer element = elements.ClickableElementAt(e.X, e.Y);
				bool shiftModifier = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
				if(element != null) {
					bool alreadySelected = selectedElements.Contains(element);
					if(shiftModifier) {
						if(alreadySelected) DeselectElement(element);
						else SelectElement(element);
					} else {
						if(!alreadySelected) {
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
					Invalidate();
				} else {
					if (Math.Abs(drawingElement.Width) < 5 && Math.Abs(drawingElement.Height) < 5) {
						drawingElement.Width = 25;
						drawingElement.Height = 25;
					}
					SelectElement(drawingElement);
					drawingElement.Selected = true;
				}
				drawingElement = null;
			}
			Invalidate();
		}
		
		void SurfaceMouseMove(object sender, MouseEventArgs e) {
			if (DrawingMode != DrawingModes.None) {
				Cursor = Cursors.Cross;
			} else {
				Cursor = Cursors.Default;
			}

			if(mouseDown) {
				if(mouseDownElement != null) { // an element is currently dragged
					selectedElements.HideGrippers();
					if(mouseDownElement.Selected) { // dragged element has been selected before -> move all
						selectedElements.MoveBy(e.X - mX, e.Y - mY);
					} else { // dragged element is not among selected elements -> just move dragged one
						mouseDownElement.MoveBy(e.X - mX, e.Y - mY);
					}
					mX = e.X; mY = e.Y;
					Invalidate();
				} else if(drawingElement != null) { // an element is currently drawn
					drawingElement.Width = e.X - drawingElement.Left;
					drawingElement.Height = e.Y - drawingElement.Top;
					Invalidate();
//				} else {
//					// Enable DragDrop
//					using (Bitmap bitmapToDrag = GetImageForExport()) {
//						
//						IDataObject dataObj = new DataObject();
//						string filename = Path.GetTempFileName() + ".png";
//						try {
//							bitmapToDrag.Save(filename, ImageFormat.Png);
//							LOG.Debug("FileDrop : " + filename);
//							dataObj.SetData("HTML Format", true, "<img src=\"" + filename + "\"/>");
//							//dataObj.SetData(DataFormats.FileDrop, new string[] {filename});
//							LOG.Debug("Starting DoDragDrop");
//							draggingInProgress = true;
//							DoDragDrop(dataObj, DragDropEffects.Copy);
//							LOG.Debug("Finished DoDragDrop");
//							draggingInProgress = false;
//							mouseDown = false;
//						} catch (Exception ex) {
//							LOG.Error("Error in DragDrop: ", ex);
//						} finally {
//							File.Delete(filename);
//						}
//					}
				}
			}
		}
		
		void SurfaceDoubleClick(object sender, MouseEventArgs e) {
			selectedElements.OnDoubleClick();
			Invalidate();
		}
		
		private Image GetImage(RenderMode renderMode) {
			Image clone = new Bitmap(Image);
			// This actually generates a copy of the original image with a dpi equal to the default...
			// otherwise we would have a problem drawing the image to the surface... :(
			using (Graphics graphics = Graphics.FromImage(clone)) {
				graphics.DrawImage(originalImage, Point.Empty);
				elements.Draw(graphics, (Bitmap)clone, renderMode);
			}
			return clone;
		}
		
		public Image GetImageForExport() {
			return GetImage(RenderMode.EXPORT);
		}

		void SurfacePaint(object sender, PaintEventArgs e) {
			Graphics graphics = e.Graphics;
			using (Image image = GetImage(RenderMode.EDIT)) {
				graphics.DrawImage(image, Point.Empty);
			}
			// Should be something like this, but due to the filter structure this won't work
			//graphics.DrawImage(originalImage, Point.Empty);
			//elements.Draw(graphics, (Bitmap)Image, RenderMode.EDIT);
		}
		
		public void AddElement(DrawableContainer element) {
			elements.Add(element);
			element.FieldChanged += element_FieldChanged;
			element.PropertyChanged += ElementPropertyChanged;
			if(element.Status == EditStatus.UNDRAWN) element.Status = EditStatus.IDLE;
			Invalidate();
		}

		public void RemoveElement(DrawableContainer element) {
			DeselectElement(element);
			elements.Remove(element);
			element.FieldChanged -= element_FieldChanged;
			element.PropertyChanged -= ElementPropertyChanged;
			element.Dispose();
			Invalidate();
		}
		
		public void AddElements(DrawableContainerList elementsToAdd) {
			elements.AddRange(elementsToAdd);
			foreach(DrawableContainer element in elementsToAdd) {
				element.FieldChanged += element_FieldChanged;
				element.PropertyChanged += ElementPropertyChanged;
				if(element.Status == EditStatus.UNDRAWN) element.Status = EditStatus.IDLE;
			}
			Invalidate();
		}
		
		public bool HasSelectedElements() {
			return (selectedElements != null && selectedElements.Count > 0);
		}
		
		public void RemoveSelectedElements() {
			if (HasSelectedElements()) {
				foreach (DrawableContainer element in selectedElements) {
					elements.Remove(element);
					element.FieldChanged -= element_FieldChanged;
					element.PropertyChanged -= ElementPropertyChanged;
					element.Dispose();
				}
				selectedElements.Clear();
				MovingElementChanged(this, selectedElements);
				Invalidate();
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
					if(confirm) {
						ApplyCrop();
					}
					DrawingMode = DrawingModes.None;
					RemoveElement(cropContainer);
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
			} else if (Clipboard.ContainsImage()) {
				using (Image image = Clipboard.GetImage()) {
					if (image != null) {
						DeselectAllElements();
						IBitmapContainer bitmapContainer = AddBitmapContainer(image as Bitmap, 0, 0);
						SelectElement(bitmapContainer);
					}
				}
			} else if ( ClipboardHelper.GetFormats().Contains("HTML Format")) {
				try {
					HtmlFragment htmlFragment = HtmlFragment.FromClipboard();
					DeselectAllElements();
					HtmlContainer htmlContainer = AddHtmlContainer(htmlFragment.Fragment, 0, 0);
					SelectElement(htmlContainer);
				} catch (Exception e) {
					LOG.Warn("Problem pasting HTML from the clipboard: ", e);
					// Switch to plain text
					string text = Clipboard.GetText();
					if (text != null) {
						DeselectAllElements();
						ITextContainer textContainer = AddTextContainer(text, HorizontalAlignment.Center, VerticalAlignment.CENTER,
							FontFamily.GenericSansSerif, 12f, false, false, false, 2, Color.Black, Color.Transparent);
						SelectElement(textContainer);
					}
				}
			} else if (Clipboard.ContainsText()) {
				string text = Clipboard.GetText();
				if (text != null) {
					if (text.StartsWith("http://")) {
						DeselectAllElements();
						UrlContainer urlContainer = AddUrlContainer(text, 0, 0);
						SelectElement(urlContainer);
					} else {
						DeselectAllElements();
						ITextContainer textContainer = AddTextContainer(text, HorizontalAlignment.Center, VerticalAlignment.CENTER,
			                FontFamily.GenericSansSerif, 12f, false, false, false, 2, Color.Black, Color.Transparent);
						SelectElement(textContainer);
					}
				}
			}
		}
		
		public void DuplicateSelectedElements() {
			if(LOG.IsDebugEnabled) LOG.Debug("Duplicating "+selectedElements.Count+" selected elements");
			DrawableContainerList dcs = (DrawableContainerList)Objects.DeepClone(selectedElements);
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
				Invalidate();
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
				int px = (k == Keys.Shift) ? 10 : 1;
				switch (k) {
					case Keys.Left:
						selectedElements.MoveBy(-1,0);
						break;
					case Keys.Left | Keys.Shift:
						selectedElements.MoveBy(-10,0);
						break;
					case Keys.Up:
						selectedElements.MoveBy(0,-1);
						break;
					case Keys.Up | Keys.Shift:
						selectedElements.MoveBy(0,-10);
						break;
					case Keys.Right:
						selectedElements.MoveBy(1,0);
						break;
					case Keys.Right | Keys.Shift:
						selectedElements.MoveBy(10,0);
						break;
					case Keys.Down:
						selectedElements.MoveBy(0,1);
						break;
					case Keys.Down | Keys.Shift:
						selectedElements.MoveBy(0,10);
						break;
					case Keys.PageUp:
						elements.PullElementsUp(selectedElements);
						break;
					case Keys.PageDown:
						elements.PushElementsDown(selectedElements);
						break;
					case Keys.Home:
						elements.PullElementsToTop(selectedElements);
						break;
					case Keys.End:
						elements.PushElementsToBottom(selectedElements);
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
				Invalidate();
			}
		}
		
		/// <summary>
		/// pulls selected elements up one level in hierarchy
		/// </summary>
		public void PullElementsUp() {
			elements.PullElementsUp(selectedElements);
			Invalidate();
		}
		
		/// <summary>
		/// pushes selected elements up to top in hierarchy
		/// </summary>
		public void PullElementsToTop() {
			elements.PullElementsToTop(selectedElements);
			Invalidate();
		}
		
		/// <summary>
		/// pushes selected elements down one level in hierarchy
		/// </summary>
		public void PushElementsDown() {
			elements.PushElementsDown(selectedElements);
			Invalidate();
		}
		
		/// <summary>
		/// pushes selected elements down to bottom in hierarchy
		/// </summary>
		public void PushElementsToBottom() {
			elements.PushElementsToBottom(selectedElements);
			Invalidate();
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
		
		public new void Dispose() {
			LOG.Debug("Disposing a surface!");
			originalImage.Dispose();
			base.Dispose();
		}
		
		public void ElementPropertyChanged(object sender, PropertyChangedEventArgs e) {
			Invalidate();
		}
		
		public void element_FieldChanged(object sender, FieldChangedEventArgs e) {
			Invalidate();
		}
	}
}
