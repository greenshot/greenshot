/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Drawing;
using Greenshot.Forms;
using Greenshot.Plugin;
using GreenshotPlugin.Core;

namespace Greenshot.Helpers {
	/// <summary>
	/// Description of PrintHelper.
	/// </summary>
	public class PrintHelper : IDisposable {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PrintHelper));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		
		private Image image;
		private PrintDocument printDocument = new PrintDocument();
		private PrintDialog printDialog = new PrintDialog();
		private PrintOptionsDialog printOptionsDialog = null;
		
		public PrintHelper(Image image, ICaptureDetails captureDetails) {
			this.image = image;
			printDialog.UseEXDialog = true;
			printDocument.DocumentName = FilenameHelper.GetFilenameWithoutExtensionFromPattern(conf.OutputFileFilenamePattern, captureDetails);
			printDocument.PrintPage += DrawImageForPrint;
			printDialog.Document = printDocument;
		}
		
		/**
		 * Destructor
		 */
		~PrintHelper() {
			Dispose(false);
		}

		/**
		 * The public accessible Dispose
		 * Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
		 */
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/**
		 * This Dispose is called from the Dispose and the Destructor.
		 * When disposing==true all non-managed resources should be freed too!
		 */
		protected void Dispose(bool disposing) {
			if (disposing) {
				if(image != null) image.Dispose();
				if(printDocument != null) printDocument.Dispose();
				if(printDialog != null) printDialog.Dispose();
				if(printOptionsDialog != null) printOptionsDialog.Dispose();
			}
			image = null;
			printDocument = null;
			printDialog = null;
			printOptionsDialog = null;
		}
		
		/// <summary>
		/// displays options dialog (if not disabled via settings) and windows
		/// print dialog.
		/// </summary>
		/// <returns>printer settings if actually printed, or null if print was cancelled or has failed</returns>
		public PrinterSettings PrintWithDialog() {
			PrinterSettings ret = null;
			if (printDialog.ShowDialog() == DialogResult.OK) {
				bool cancelled = false;
				printOptionsDialog = new PrintOptionsDialog();
				if (conf.OutputPrintPromptOptions) {
					DialogResult result = printOptionsDialog.ShowDialog();
					if(result != DialogResult.OK) {
						cancelled = true;
					}
				} 
				try {
					if(!cancelled) {
						printDocument.Print();
						ret = printDialog.PrinterSettings;
					}
				} catch(Exception e) {
					LOG.Error("An error ocurred while trying to print", e);
					ILanguage lang = Language.GetInstance();
					MessageBox.Show(lang.GetString(LangKey.print_error), lang.GetString(LangKey.error));
				}
				
			} 
			image.Dispose();
			image = null;
			return ret;
		}
		
		void DrawImageForPrint(object sender, PrintPageEventArgs e) {
			PrintOptionsDialog pod = printOptionsDialog;
			
			ContentAlignment alignment = pod.AllowPrintCenter ? ContentAlignment.MiddleCenter : ContentAlignment.TopLeft;

			if (conf.OutputPrintInverted) {
				// Invert Bitmap
				BitmapBuffer bb = new BitmapBuffer((Bitmap)image, false);
				bb.Lock();
				for(int y=0;y<bb.Height; y++) {
					for(int x=0;x<bb.Width; x++) {
						Color color = bb.GetColorAt(x, y);
						Color invertedColor = Color.FromArgb(color.A, color.R ^ 255, color.G ^ 255, color.B ^ 255);
						bb.SetColorAt(x, y, invertedColor);
					}
				}
				bb.Dispose();
			}

			RectangleF pageRect = e.PageSettings.PrintableArea;
			GraphicsUnit gu = GraphicsUnit.Pixel;
			RectangleF imageRect = image.GetBounds(ref gu);
			// rotate the image if it fits the page better
			if(pod.AllowPrintRotate) {
				if((pageRect.Width > pageRect.Height && imageRect.Width < imageRect.Height) ||
				   (pageRect.Width < pageRect.Height && imageRect.Width > imageRect.Height)) {
					image.RotateFlip(RotateFlipType.Rotate90FlipNone);
					imageRect = image.GetBounds(ref gu);
					if(alignment.Equals(ContentAlignment.TopLeft)) alignment = ContentAlignment.TopRight;
				}
			}
			RectangleF printRect = new RectangleF(0,0,imageRect.Width, imageRect.Height);;
			// scale the image to fit the page better
			if(pod.AllowPrintEnlarge || pod.AllowPrintShrink) {
				SizeF resizedRect = ScaleHelper.GetScaledSize(imageRect.Size,pageRect.Size,false);
				if((pod.AllowPrintShrink && resizedRect.Width < printRect.Width) ||
				   pod.AllowPrintEnlarge && resizedRect.Width > printRect.Width) {
					printRect.Size = resizedRect;
				}
				
			}
			
			// prepare timestamp
			float dateStringWidth = 0;
			float dateStringHeight = 0;
			if (conf.OutputPrintTimestamp) {
				Font f = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular);
				string dateString = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();
				dateStringWidth = e.Graphics.MeasureString(dateString, f).Width;
				dateStringHeight = e.Graphics.MeasureString(dateString, f).Height;
				e.Graphics.DrawString(dateString, f, Brushes.Black, pageRect.Width / 2 - (dateStringWidth / 2), pageRect.Height - dateStringHeight);
			}
			
			// align the image
			printRect = ScaleHelper.GetAlignedRectangle(printRect, new RectangleF(0, 0, pageRect.Width, pageRect.Height - dateStringHeight * 2), alignment);
			
			e.Graphics.DrawImage(image,printRect,imageRect,GraphicsUnit.Pixel);			
			
		}
	}
}
