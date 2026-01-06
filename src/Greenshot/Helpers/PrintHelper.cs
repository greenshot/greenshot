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
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Effects;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Configuration;
using Greenshot.Editor.Helpers;
using Greenshot.Forms;
using log4net;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Description of PrintHelper.
    /// </summary>
    public class PrintHelper : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PrintHelper));
        private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();

        private ISurface _surface;
        private readonly ICaptureDetails _captureDetails;
        private PrintDocument _printDocument = new PrintDocument();
        private PrintDialog _printDialog = new PrintDialog();

        public PrintHelper(ISurface surface, ICaptureDetails captureDetails)
        {
            _surface = surface;
            _captureDetails = captureDetails;
            _printDialog.UseEXDialog = true;
            _printDocument.DocumentName = FilenameHelper.GetFilenameWithoutExtensionFromPattern(CoreConfig.OutputFileFilenamePattern, captureDetails);
            _printDocument.PrintPage += DrawImageForPrint;
            _printDialog.Document = _printDocument;
        }

        /**
         * Destructor
         */
        ~PrintHelper()
        {
            Dispose(false);
        }

        /**
         * The public accessible Dispose
         * Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
         */
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /**
         * This Dispose is called from the Dispose and the Destructor.
         * When disposing==true all non-managed resources should be freed too!
         */
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _printDocument?.Dispose();
                _printDialog?.Dispose();
            }

            _surface = null;
            _printDocument = null;
            _printDialog = null;
        }

        /// <summary>
        /// displays options dialog (if not disabled via settings) and windows
        /// print dialog.
        /// </summary>
        /// <returns>printer settings if actually printed, or null if print was cancelled or has failed</returns>
        public PrinterSettings PrintTo(string printerName)
        {
            PrinterSettings returnPrinterSettings = null;
            DialogResult? printOptionsResult = ShowPrintOptionsDialog();
            try
            {
                if (printOptionsResult == null || printOptionsResult == DialogResult.OK)
                {
                    _printDocument.PrinterSettings.PrinterName = printerName;
                    if (!IsColorPrint())
                    {
                        _printDocument.DefaultPageSettings.Color = false;
                    }

                    _printDocument.Print();
                    returnPrinterSettings = _printDocument.PrinterSettings;
                }
            }
            catch (Exception e)
            {
                Log.Error("An error occurred while trying to print", e);
                MessageBox.Show(Language.GetString(LangKey.print_error), Language.GetString(LangKey.error));
            }

            return returnPrinterSettings;
        }

        /// <summary>
        /// displays options dialog (if not disabled via settings) and windows
        /// print dialog.
        /// </summary>
        /// <returns>printer settings if actually printed, or null if print was cancelled or has failed</returns>
        public PrinterSettings PrintWithDialog()
        {
            PrinterSettings returnPrinterSettings = null;
            if (_printDialog.ShowDialog() != DialogResult.OK) return null;

            DialogResult? printOptionsResult = ShowPrintOptionsDialog();
            try
            {
                if (printOptionsResult == null || printOptionsResult == DialogResult.OK)
                {
                    if (!IsColorPrint())
                    {
                        _printDocument.DefaultPageSettings.Color = false;
                    }

                    _printDocument.Print();
                    returnPrinterSettings = _printDialog.PrinterSettings;
                }
            }
            catch (Exception e)
            {
                Log.Error("An error occurred while trying to print", e);
                MessageBox.Show(Language.GetString(LangKey.print_error), Language.GetString(LangKey.error));
            }

            return returnPrinterSettings;
        }

        private bool IsColorPrint()
        {
            return !CoreConfig.OutputPrintGrayscale && !CoreConfig.OutputPrintMonochrome;
        }

        /// <summary>
        /// display print options dialog (if the user has not configured Greenshot not to)
        /// </summary>
        /// <returns>result of the print dialog, or null if the dialog has not been displayed by config</returns>
        private DialogResult? ShowPrintOptionsDialog()
        {
            DialogResult? ret = null;
            if (CoreConfig.OutputPrintPromptOptions)
            {
                using PrintOptionsDialog printOptionsDialog = new PrintOptionsDialog();
                ret = printOptionsDialog.ShowDialog();
            }

            return ret;
        }

        private void DrawImageForPrint(object sender, PrintPageEventArgs e)
        {
            // Create the output settings
            SurfaceOutputSettings printOutputSettings = new SurfaceOutputSettings(OutputFormat.png, 100, false);

            ApplyEffects(printOutputSettings);

            bool disposeImage = ImageIO.CreateImageFromSurface(_surface, printOutputSettings, out var image);
            try
            {
                ContentAlignment alignment = CoreConfig.OutputPrintCenter ? ContentAlignment.MiddleCenter : ContentAlignment.TopLeft;

                // prepare timestamp
                float footerStringWidth = 0;
                float footerStringHeight = 0;
                string footerString = null; //DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();
                if (CoreConfig.OutputPrintFooter)
                {
                    footerString = FilenameHelper.FillPattern(CoreConfig.OutputPrintFooterPattern, _captureDetails, false);
                    using Font f = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular);
                    footerStringWidth = e.Graphics.MeasureString(footerString, f).Width;
                    footerStringHeight = e.Graphics.MeasureString(footerString, f).Height;
                }

                // Get a rectangle representing the printable Area
                RectangleF pageRect = e.PageSettings.PrintableArea;
                if (e.PageSettings.Landscape)
                {
                    float origWidth = pageRect.Width;
                    pageRect.Width = pageRect.Height;
                    pageRect.Height = origWidth;
                }

                // Subtract the dateString height from the available area, this way the area stays free
                pageRect.Height -= footerStringHeight;

                GraphicsUnit gu = GraphicsUnit.Pixel;
                RectangleF imageRect = image.GetBounds(ref gu);
                // rotate the image if it fits the page better
                if (CoreConfig.OutputPrintAllowRotate)
                {
                    if (pageRect.Width > pageRect.Height && imageRect.Width < imageRect.Height || pageRect.Width < pageRect.Height && imageRect.Width > imageRect.Height)
                    {
                        image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        imageRect = image.GetBounds(ref gu);
                        if (alignment.Equals(ContentAlignment.TopLeft))
                        {
                            alignment = ContentAlignment.TopRight;
                        }
                    }
                }

                RectangleF printRect = new RectangleF(0, 0, imageRect.Width, imageRect.Height);
                // scale the image to fit the page better
                if (CoreConfig.OutputPrintAllowEnlarge || CoreConfig.OutputPrintAllowShrink)
                {
                    SizeF resizedRect = ScaleHelper.GetScaledSize(imageRect.Size, pageRect.Size, false);
                    if (CoreConfig.OutputPrintAllowShrink && resizedRect.Width < printRect.Width || CoreConfig.OutputPrintAllowEnlarge && resizedRect.Width > printRect.Width)
                    {
                        printRect.Size = resizedRect;
                    }
                }

                // align the image
                printRect = ScaleHelper.GetAlignedRectangle(printRect, new RectangleF(0, 0, pageRect.Width, pageRect.Height), alignment);
                if (CoreConfig.OutputPrintFooter)
                {
                    //printRect = new RectangleF(0, 0, printRect.Width, printRect.Height - (dateStringHeight * 2));
                    using Font f = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular);
                    e.Graphics.DrawString(footerString, f, Brushes.Black, pageRect.Width / 2 - footerStringWidth / 2, pageRect.Height);
                }

                e.Graphics.DrawImage(image, printRect, imageRect, GraphicsUnit.Pixel);
            }
            finally
            {
                if (disposeImage)
                {
                    image?.Dispose();
                }
            }
        }

        private void ApplyEffects(SurfaceOutputSettings printOutputSettings)
        {
            // TODO:
            // add effects here
            if (CoreConfig.OutputPrintMonochrome)
            {
                byte threshold = CoreConfig.OutputPrintMonochromeThreshold;
                printOutputSettings.Effects.Add(new MonochromeEffect(threshold));
                printOutputSettings.ReduceColors = true;
            }

            // the invert effect should probably be the last
            if (CoreConfig.OutputPrintInverted)
            {
                printOutputSettings.Effects.Add(new InvertEffect());
            }
        }
    }
}