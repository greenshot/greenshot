/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing.Printing;
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Drawing;
using Greenshot.Forms;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using Greenshot.Core;

namespace Greenshot.Helpers {
	/// <summary>
	/// Description of PrintHelper.
	/// </summary>
	public class PrintHelper : IDisposable {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PrintHelper));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();

		private ISurface surface;
		private ICaptureDetails captureDetails;
		private PrintDocument printDocument = new PrintDocument();
		private PrintDialog printDialog = new PrintDialog();

		public PrintHelper(ISurface surface, ICaptureDetails captureDetails) {
			this.surface = surface;
			this.captureDetails = captureDetails;
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
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (printDocument != null) {
					printDocument.Dispose();
				}
				if (printDialog != null) {
					printDialog.Dispose();
				}
			}
			surface = null;
			printDocument = null;
			printDialog = null;
		}

		/// <summary>
		/// displays options dialog (if not disabled via settings) and windows
		/// print dialog.
		/// </summary>
		/// <returns>printer settings if actually printed, or null if print was cancelled or has failed</returns>
		public PrinterSettings PrintTo(string printerName) {
			PrinterSettings returnPrinterSettings = null;
			DialogResult? printOptionsResult = ShowPrintOptionsDialog();
				try {
					if (printOptionsResult == null || printOptionsResult == DialogResult.OK) {
					printDocument.PrinterSettings.PrinterName = printerName;
					if (!IsColorPrint()) {
						printDocument.DefaultPageSettings.Color = false;
					}
					printDocument.Print();
					returnPrinterSettings = printDocument.PrinterSettings;
				}
			} catch (Exception e) {
				LOG.Error("An error ocurred while trying to print", e);
				MessageBox.Show(Language.GetString(LangKey.print_error), Language.GetString(LangKey.error));
			}
			return returnPrinterSettings;
		}

		/// <summary>
		/// displays options dialog (if not disabled via settings) and windows
		/// print dialog.
		/// </summary>
		/// <returns>printer settings if actually printed, or null if print was cancelled or has failed</returns>
		public PrinterSettings PrintWithDialog() {
			PrinterSettings returnPrinterSettings = null;
			if (printDialog.ShowDialog() == DialogResult.OK) {
				DialogResult? printOptionsResult = ShowPrintOptionsDialog();
				try {
					if (printOptionsResult == null || printOptionsResult == DialogResult.OK) {
						if (IsColorPrint()) {
							printDocument.DefaultPageSettings.Color = false;
						}
						printDocument.Print();
						returnPrinterSettings = printDialog.PrinterSettings;
					}
				} catch (Exception e) {
					LOG.Error("An error ocurred while trying to print", e);
					MessageBox.Show(Language.GetString(LangKey.print_error), Language.GetString(LangKey.error));
				}

			}
			return returnPrinterSettings;
		}

		private bool IsColorPrint() {
			return !conf.OutputPrintGrayscale && !conf.OutputPrintMonochrome;
		}

		/// <summary>
		/// display print options dialog (if the user has not configured Greenshot not to)
		/// </summary>
		/// <returns>result of the print dialog, or null if the dialog has not been displayed by config</returns>
		private DialogResult? ShowPrintOptionsDialog() {
			DialogResult? ret = null;
			if (conf.OutputPrintPromptOptions) {
				using (PrintOptionsDialog printOptionsDialog = new PrintOptionsDialog()) {
					ret = printOptionsDialog.ShowDialog();
				}
			} 
			return ret;
		}

		void DrawImageForPrint(object sender, PrintPageEventArgs e) {
			

			// Create the output settins
			SurfaceOutputSettings printOutputSettings = new SurfaceOutputSettings(OutputFormat.png, 100, false);

			ApplyEffects(printOutputSettings);

			Image image;
			bool disposeImage = ImageOutput.CreateImageFromSurface(surface, printOutputSettings, out image);
			try {
				ContentAlignment alignment = conf.OutputPrintCenter ? ContentAlignment.MiddleCenter : ContentAlignment.TopLeft;

				// prepare timestamp
				float footerStringWidth = 0;
				float footerStringHeight = 0;
				string footerString = null; //DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();
				if (conf.OutputPrintFooter) {
					footerString = FilenameHelper.FillPattern(conf.OutputPrintFooterPattern, captureDetails, false);
					using (Font f = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular)) {
						footerStringWidth = e.Graphics.MeasureString(footerString, f).Width;
						footerStringHeight = e.Graphics.MeasureString(footerString, f).Height;
					}
				}

				// Get a rectangle representing the printable Area
				RectangleF pageRect = e.PageSettings.PrintableArea;
				if(e.PageSettings.Landscape) {
					float origWidth = pageRect.Width;
					pageRect.Width = pageRect.Height;
					pageRect.Height = origWidth;
				}

				// Subtract the dateString height from the available area, this way the area stays free
				pageRect.Height -= footerStringHeight;

				GraphicsUnit gu = GraphicsUnit.Pixel;
				RectangleF imageRect = image.GetBounds(ref gu);
				// rotate the image if it fits the page better
				if (conf.OutputPrintAllowRotate) {
					if ((pageRect.Width > pageRect.Height && imageRect.Width < imageRect.Height) || (pageRect.Width < pageRect.Height && imageRect.Width > imageRect.Height)) {
						image.RotateFlip(RotateFlipType.Rotate90FlipNone);
						imageRect = image.GetBounds(ref gu);
						if (alignment.Equals(ContentAlignment.TopLeft)) {
							alignment = ContentAlignment.TopRight;
						}
					}
				}

				RectangleF printRect = new RectangleF(0, 0, imageRect.Width, imageRect.Height);
				// scale the image to fit the page better
				if (conf.OutputPrintAllowEnlarge || conf.OutputPrintAllowShrink) {
					SizeF resizedRect = ScaleHelper.GetScaledSize(imageRect.Size, pageRect.Size, false);
					if ((conf.OutputPrintAllowShrink && resizedRect.Width < printRect.Width) || conf.OutputPrintAllowEnlarge && resizedRect.Width > printRect.Width) {
						printRect.Size = resizedRect;
					}
				}

				// align the image
				printRect = ScaleHelper.GetAlignedRectangle(printRect, new RectangleF(0, 0, pageRect.Width, pageRect.Height), alignment);
				if (conf.OutputPrintFooter) {
					//printRect = new RectangleF(0, 0, printRect.Width, printRect.Height - (dateStringHeight * 2));
					using (Font f = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular)) {
						e.Graphics.DrawString(footerString, f, Brushes.Black, pageRect.Width / 2 - (footerStringWidth / 2), pageRect.Height);
					}
				}
				e.Graphics.DrawImage(image, printRect, imageRect, GraphicsUnit.Pixel);

			} finally {
				if (disposeImage && image != null) {
					image.Dispose();
					image = null;
				}
			}
		}

		private void ApplyEffects(SurfaceOutputSettings printOutputSettings) {
			// TODO:
			// add effects here
			if (conf.OutputPrintMonochrome) {
				byte threshold = conf.OutputPrintMonochromeThreshold;
				printOutputSettings.Effects.Add(new MonochromeEffect(threshold));
				printOutputSettings.ReduceColors = true;
			}

			// the invert effect should probably be the last
			if (conf.OutputPrintInverted) {
				printOutputSettings.Effects.Add(new InvertEffect());
			}
		}
	}
}
