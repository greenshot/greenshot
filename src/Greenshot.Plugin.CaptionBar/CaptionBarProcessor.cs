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
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

namespace Greenshot.Plugin.CaptionBar
{
    /// <summary>
    /// Processor that adds a caption bar to screenshots
    /// </summary>
    public class CaptionBarProcessor : AbstractProcessor
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(CaptionBarProcessor));
        private readonly CaptionBarConfiguration _config;

        public CaptionBarProcessor(CaptionBarConfiguration config)
        {
            _config = config;
        }

        public override string Designation => "CaptionBar";

        public override string Description => "Adds caption bar to screenshots";

        public override int Priority => 10;

        public override bool isActive => _config.Enabled;

        public override bool ProcessCapture(ISurface surface, ICaptureDetails captureDetails)
        {
            if (!_config.Enabled || _config.BarHeight <= 0)
            {
                return false;
            }

            try
            {
                Log.DebugFormat("Adding caption bar to capture");

                // Get the current image
                using (Image originalImage = surface.GetImageForExport())
                {
                    // Create new image with caption bar
                    Image newImage = AddCaptionBar(originalImage);

                    // Replace the image in the surface
                    surface.Image = newImage;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error adding caption bar", ex);
                return false;
            }
        }

        /// <summary>
        /// Scales image if text doesn't fit, maintaining original text size
        /// </summary>
        private Image ScaleImageIfNeeded(Image sourceImage, string timestamp, string customText, out float scaleFactor)
        {
            scaleFactor = 1.0f;

            // Skip scaling for very narrow images
            if (sourceImage.Width < 10)
            {
                return sourceImage;
            }

            bool hasTimestamp = !string.IsNullOrEmpty(timestamp);
            bool hasCustomText = !string.IsNullOrEmpty(customText);

            if (!hasTimestamp && !hasCustomText)
            {
                return sourceImage;
            }

            try
            {
                // Measure text widths with current font
                float measuredTimestampWidth = 0;
                float measuredCustomWidth = 0;

                using (Bitmap tempBitmap = new Bitmap(1, 1))
                using (Graphics tempGraphics = Graphics.FromImage(tempBitmap))
                using (Font font = new Font(_config.FontName, _config.FontSize))
                {
                    if (hasTimestamp)
                    {
                        using (StringFormat format = CreateStringFormat(_config.TimestampAlignment, false, 1))
                        {
                            SizeF size = MeasureTextNaturalSize(tempGraphics, timestamp, font, format);
                            measuredTimestampWidth = size.Width;
                        }
                    }

                    if (hasCustomText)
                    {
                        using (StringFormat format = CreateStringFormat(_config.CustomTextAlignment, false, 1))
                        {
                            SizeF size = MeasureTextNaturalSize(tempGraphics, customText, font, format);
                            measuredCustomWidth = size.Width;
                        }
                    }
                }

                Log.DebugFormat("Text measurements: timestamp={0}px, custom={1}px, available={2}px",
                    measuredTimestampWidth, measuredCustomWidth, sourceImage.Width);

                // Calculate required width based on alignment combination
                float requiredWidth = 0;
                float padding = _config.TextPadding;

                bool timestampLeft = _config.TimestampAlignment == StringAlignment.Near;
                bool timestampRight = _config.TimestampAlignment == StringAlignment.Far;
                bool timestampCenter = _config.TimestampAlignment == StringAlignment.Center;
                bool customLeft = _config.CustomTextAlignment == StringAlignment.Near;
                bool customRight = _config.CustomTextAlignment == StringAlignment.Far;
                bool customCenter = _config.CustomTextAlignment == StringAlignment.Center;

                // Both texts on the same side (Left or Right) - they will overlap
                if ((timestampLeft && customLeft) || (timestampRight && customRight))
                {
                    // Sum both widths to prevent overlap + TEXT_MARGIN matching CalculateTextRectangles
                    const int TEXT_MARGIN = 15;
                    requiredWidth = measuredTimestampWidth + TEXT_MARGIN + measuredCustomWidth + (padding * 3);
                    Log.DebugFormat("Both texts on same side, using sum: {0}px", requiredWidth);
                }
                // One or both texts centered
                else if (timestampCenter || customCenter)
                {
                    const int TEXT_MARGIN = 15;

                    if (timestampCenter && customCenter)
                    {
                        // Both centered: timestamp occupies center of bar, custom gets remaining space
                        requiredWidth = (measuredCustomWidth * 2) + measuredTimestampWidth + (padding * 4);
                    }
                    else
                    {
                        // One edge + one center: texts placed sequentially in their sub-rectangles
                        requiredWidth = measuredTimestampWidth + TEXT_MARGIN + measuredCustomWidth + (padding * 3);
                    }

                    Log.DebugFormat("Center alignment: bothCenter={0}, requiredWidth={1}px",
                        timestampCenter && customCenter, requiredWidth);
                }
                // Opposite sides (Left + Right)
                else if ((timestampLeft && customRight) || (timestampRight && customLeft))
                {
                    // Side-by-side layout + TEXT_MARGIN matching CalculateTextRectangles
                    const int TEXT_MARGIN = 15;
                    requiredWidth = measuredTimestampWidth + TEXT_MARGIN + measuredCustomWidth + (padding * 3);
                    Log.DebugFormat("Opposite sides, using sum: {0}px", requiredWidth);
                }
                // Single text or fallback
                else
                {
                    requiredWidth = Math.Max(measuredTimestampWidth, measuredCustomWidth) + (padding * 2);
                }

                // Apply minimum width constraint
                const int MIN_WIDTH = 150;
                if (sourceImage.Width < MIN_WIDTH)
                {
                    requiredWidth = Math.Max(requiredWidth, MIN_WIDTH);
                }

                Log.DebugFormat("Required width: {0}px", requiredWidth);

                // Calculate scale factor
                scaleFactor = requiredWidth / sourceImage.Width;

                if (scaleFactor <= 1.0f)
                {
                    Log.Debug("No scaling needed");
                    return sourceImage;
                }

                // Cap maximum scale factor
                const float MAX_SCALE_FACTOR = 3.0f;
                if (scaleFactor > MAX_SCALE_FACTOR)
                {
                    Log.WarnFormat("Scale factor {0:F2} exceeds maximum, capping at {1:F2}", scaleFactor, MAX_SCALE_FACTOR);
                    scaleFactor = MAX_SCALE_FACTOR;
                }

                // Calculate new dimensions
                int newWidth = (int)(sourceImage.Width * scaleFactor);
                int newHeight = (int)(sourceImage.Height * scaleFactor);

                Log.InfoFormat("Scaling image {0}x{1} -> {2}x{3} (factor={4:F2}) to fit text",
                    sourceImage.Width, sourceImage.Height, newWidth, newHeight, scaleFactor);

                // Create scaled bitmap
                Bitmap scaledBitmap = new Bitmap(newWidth, newHeight);
                using (Graphics g = Graphics.FromImage(scaledBitmap))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(sourceImage, 0, 0, newWidth, newHeight);
                }

                return scaledBitmap;
            }
            catch (OutOfMemoryException ex)
            {
                Log.Error("Out of memory while scaling image, using original", ex);
                scaleFactor = 1.0f;
                return sourceImage;
            }
            catch (Exception ex)
            {
                Log.Error("Error scaling image, using original", ex);
                scaleFactor = 1.0f;
                return sourceImage;
            }
        }

        /// <summary>
        /// Calculates separate rectangles for timestamp and custom text based on their alignments
        /// to prevent text overlap
        /// </summary>
        /// <param name="graphics">Graphics context for text measurement</param>
        /// <param name="timestamp">Timestamp text</param>
        /// <param name="customText">Custom text</param>
        /// <param name="font">Font to use</param>
        /// <param name="fullRect">Full available rectangle</param>
        /// <param name="timestampRect">Output: Rectangle for timestamp</param>
        /// <param name="customTextRect">Output: Rectangle for custom text</param>
        private void CalculateTextRectangles(
            Graphics graphics,
            string timestamp,
            string customText,
            Font font,
            Rectangle fullRect,
            out Rectangle timestampRect,
            out Rectangle customTextRect)
        {
            // Default: both texts use full rectangle (fallback)
            timestampRect = fullRect;
            customTextRect = fullRect;

            bool hasTimestamp = !string.IsNullOrEmpty(timestamp);
            bool hasCustomText = !string.IsNullOrEmpty(customText);

            // If only one text exists, use full rectangle
            if (!hasTimestamp || !hasCustomText)
            {
                return;
            }

            // Measure timestamp natural width
            float timestampWidth = 0;
            using (StringFormat tsFormat = CreateStringFormat(_config.TimestampAlignment, false, 1))
            {
                SizeF timestampSize = MeasureTextNaturalSize(graphics, timestamp, font, tsFormat);
                timestampWidth = timestampSize.Width;
            }

            Log.DebugFormat("Timestamp measured width: {0}px in fullRect width: {1}px",
                timestampWidth, fullRect.Width);

            // Add safety margin to prevent text truncation (ellipsis)
            const int TEXT_MARGIN = 15;
            timestampWidth += TEXT_MARGIN;

            // Calculate timestamp end position based on its alignment
            int timestampEndX;
            switch (_config.TimestampAlignment)
            {
                case StringAlignment.Near:
                    // Timestamp starts at left edge
                    timestampEndX = fullRect.X + (int)timestampWidth;
                    break;

                case StringAlignment.Center:
                    // Timestamp centered, ends at center + half width
                    int timestampCenterX = fullRect.X + (fullRect.Width / 2);
                    timestampEndX = timestampCenterX + (int)(timestampWidth / 2);
                    break;

                case StringAlignment.Far:
                    // Timestamp at right edge - custom text should go left of it
                    timestampEndX = fullRect.Right;
                    break;

                default:
                    timestampEndX = fullRect.X + (int)timestampWidth;
                    break;
            }

            // Adjust rectangles based on custom text alignment
            switch (_config.CustomTextAlignment)
            {
                case StringAlignment.Near:
                    // Custom text starts after timestamp with padding
                    int customStartX = timestampEndX + _config.TextPadding;
                    int availableWidth = fullRect.Right - customStartX;

                    if (availableWidth > 0)
                    {
                        customTextRect = new Rectangle(customStartX, fullRect.Y,
                            availableWidth, fullRect.Height);

                        // Limit timestamp rectangle to its actual width
                        int timestampRectWidth = timestampEndX - fullRect.X;
                        timestampRect = new Rectangle(fullRect.X, fullRect.Y,
                            timestampRectWidth, fullRect.Height);

                        Log.DebugFormat("CustomText=Near: customStartX={0}, availableWidth={1}",
                            customStartX, availableWidth);
                    }
                    break;

                case StringAlignment.Center:
                    // Custom text centers between timestamp end and right edge
                    int customStartXCenter = timestampEndX + _config.TextPadding;
                    int availableWidthCenter = fullRect.Right - customStartXCenter;

                    if (availableWidthCenter > 0)
                    {
                        customTextRect = new Rectangle(customStartXCenter, fullRect.Y,
                            availableWidthCenter, fullRect.Height);

                        // Limit timestamp rectangle
                        int timestampRectWidthCenter = timestampEndX - fullRect.X;
                        timestampRect = new Rectangle(fullRect.X, fullRect.Y,
                            timestampRectWidthCenter, fullRect.Height);

                        Log.DebugFormat("CustomText=Center: customStartX={0}, availableWidth={1}, will center in this space",
                            customStartXCenter, availableWidthCenter);
                    }
                    break;

                case StringAlignment.Far:
                    // Far alignment: custom text stays on right side
                    // Keep default fullRect for both (existing behavior)
                    Log.Debug("CustomText=Far: using default fullRect for both texts");
                    break;
            }

            Log.DebugFormat("Final rectangles: timestamp=({0},{1},{2},{3}), custom=({4},{5},{6},{7})",
                timestampRect.X, timestampRect.Y, timestampRect.Width, timestampRect.Height,
                customTextRect.X, customTextRect.Y, customTextRect.Width, customTextRect.Height);
        }

        private Image AddCaptionBar(Image sourceImage)
        {
            // Prepare text
            string timestamp = "";
            string customText = "";

            if (_config.ShowTimestamp)
            {
                try
                {
                    timestamp = DateTime.Now.ToString(_config.TimestampFormat);
                }
                catch (Exception ex)
                {
                    Log.Warn("Error formatting timestamp, using default format", ex);
                    timestamp = DateTime.Now.ToString("M/d/yyyy h:mm:ss tt");
                }
            }

            if (!string.IsNullOrEmpty(_config.CustomText))
            {
                customText = _config.CustomText;
            }

            // If both texts are empty, return original image
            if (string.IsNullOrEmpty(timestamp) && string.IsNullOrEmpty(customText))
            {
                Log.Debug("No text to display, returning original image");
                return sourceImage;
            }

            // Skip caption bar for extremely narrow screenshots
            if (sourceImage.Width < 10)
            {
                Log.Warn($"Screenshot too narrow ({sourceImage.Width}px), skipping caption bar");
                return sourceImage;
            }

            // Scale image if needed to fit all text
            float scaleFactor;
            Image workingImage = ScaleImageIfNeeded(sourceImage, timestamp, customText, out scaleFactor);

            int newWidth = workingImage.Width;

            // Calculate optimal layout (dynamic font size, bar height, wrapping)
            LayoutInfo layout;
            using (Bitmap measureBitmap = new Bitmap(1, 1))
            using (Graphics measureGraphics = Graphics.FromImage(measureBitmap))
            {
                layout = CalculateOptimalLayout(measureGraphics, timestamp, customText,
                                                newWidth, _config.FontSize);
            }
            int barHeight = layout.BarHeight;

            // Calculate new image size with fixed bar height
            int newHeight = workingImage.Height + barHeight;

            // Create new bitmap with extended height
            Bitmap expandedImage = new Bitmap(newWidth, newHeight);

            using (Graphics graphics = Graphics.FromImage(expandedImage))
            {
                // Set high quality rendering
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // Draw original image at the top
                graphics.DrawImage(workingImage, 0, 0, workingImage.Width, workingImage.Height);

                // Calculate caption bar position
                int barY = workingImage.Height;

                // Fill caption bar background
                using (Brush bgBrush = new SolidBrush(_config.BackgroundColor))
                {
                    graphics.FillRectangle(bgBrush, 0, barY, newWidth, barHeight);
                }

                // Create font with dynamically calculated size
                using (Font font = new Font(_config.FontName, layout.FontSize))
                using (Brush textBrush = new SolidBrush(_config.TextColor))
                {
                    bool hasTimestamp = !string.IsNullOrEmpty(timestamp);
                    bool hasCustomText = !string.IsNullOrEmpty(customText);

                    // Full width rectangle as baseline
                    int fullWidth = newWidth - (_config.TextPadding * 2);
                    Rectangle fullRect = new Rectangle(_config.TextPadding, barY, fullWidth, barHeight);

                    // Calculate separate rectangles for each text to avoid overlap
                    Rectangle timestampRect, customTextRect;
                    if (hasTimestamp && hasCustomText)
                    {
                        CalculateTextRectangles(graphics, timestamp, customText, font, fullRect,
                            out timestampRect, out customTextRect);
                    }
                    else
                    {
                        // Only one text - use full rectangle
                        timestampRect = fullRect;
                        customTextRect = fullRect;
                    }

                    // Draw timestamp with its alignment in its dedicated rectangle
                    if (hasTimestamp)
                    {
                        using (StringFormat format = CreateStringFormat(_config.TimestampAlignment, false, 1))
                        {
                            DrawAlignedText(graphics, timestamp, font, textBrush, timestampRect, format);
                        }
                    }

                    // Draw custom text with its alignment in its dedicated rectangle
                    if (hasCustomText)
                    {
                        bool useWrapping = _config.EnableTextWrapping && layout.LinesUsed > 1;
                        int maxLines = useWrapping ? _config.MaxLines : 1;
                        using (StringFormat format = CreateStringFormat(_config.CustomTextAlignment, useWrapping, maxLines))
                        {
                            DrawAlignedText(graphics, customText, font, textBrush, customTextRect, format);
                        }
                    }
                }
            }

            // Dispose scaled image if it was created (not the original)
            if (scaleFactor > 1.0f && workingImage != sourceImage)
            {
                workingImage.Dispose();
            }

            return expandedImage;
        }

        /// <summary>
        /// Internal structure for layout calculation results
        /// </summary>
        private struct LayoutInfo
        {
            public float FontSize;
            public int BarHeight;
            public int LinesUsed;
        }

        /// <summary>
        /// Calculates optimal font size and bar height to fit text
        /// </summary>
        private LayoutInfo CalculateOptimalLayout(Graphics graphics, string timestamp, string customText, int imageWidth, float configuredFontSize)
        {
            float currentFontSize = configuredFontSize;
            float minFontSize = configuredFontSize * (_config.MinFontSizePercent / 100f);

            // Ensure minimum font size is reasonable
            if (minFontSize < 4f)
            {
                minFontSize = 4f;
            }

            bool hasTimestamp = !string.IsNullOrEmpty(timestamp);
            bool hasCustomText = !string.IsNullOrEmpty(customText);

            Log.DebugFormat("Layout calculation: imageWidth={0}, TimestampAlign={1}, CustomAlign={2}",
                imageWidth, _config.TimestampAlignment, _config.CustomTextAlignment);

            while (currentFontSize >= minFontSize)
            {
                using (Font font = new Font(_config.FontName, currentFontSize))
                {
                    int timestampHeight = 0;
                    int customTextHeight = 0;
                    bool timestampFits = true;
                    bool customTextFits = true;

                    // Measure timestamp (always single line, no wrapping)
                    if (hasTimestamp)
                    {
                        int timestampWidth;
                        if (!hasCustomText)
                        {
                            // Timestamp only: use full width
                            timestampWidth = imageWidth - (_config.TextPadding * 2);
                        }
                        else
                        {
                            // Side-by-side: measure actual timestamp width instead of 50/50 split
                            using (StringFormat tsFormat = CreateStringFormat(_config.TimestampAlignment, false, 1))
                            {
                                SizeF tsNatural = MeasureTextNaturalSize(graphics, timestamp, font, tsFormat);
                                const int TEXT_MARGIN_LAYOUT = 15;
                                timestampWidth = (int)tsNatural.Width + TEXT_MARGIN_LAYOUT + _config.TextPadding;
                            }
                        }

                        using (StringFormat format = CreateStringFormat(_config.TimestampAlignment, false, 1))
                        {
                            SizeF timestampSize = MeasureTextWithAlignment(graphics, timestamp, font, format, timestampWidth);
                            timestampHeight = (int)Math.Ceiling(timestampSize.Height);

                            // For timestamp: check if actual measured width exceeds available width
                            // But be lenient - allow up to 110% of available width before reducing font
                            // (small overflow will be handled by ellipsis)
                            if (timestampSize.Width > timestampWidth * 1.1f)
                            {
                                timestampFits = false;
                            }
                        }
                    }

                    // Measure custom text (can wrap if enabled)
                    if (hasCustomText)
                    {
                        int customTextWidth;
                        if (!hasTimestamp)
                        {
                            // Custom text only: use full width
                            customTextWidth = imageWidth - (_config.TextPadding * 2);
                        }
                        else
                        {
                            // Side-by-side: custom text gets remainder after timestamp
                            using (StringFormat tsFormat = CreateStringFormat(_config.TimestampAlignment, false, 1))
                            {
                                SizeF tsNatural = MeasureTextNaturalSize(graphics, timestamp, font, tsFormat);
                                const int TEXT_MARGIN_LAYOUT = 15;
                                int tsUsed = (int)tsNatural.Width + TEXT_MARGIN_LAYOUT + (_config.TextPadding * 2);
                                customTextWidth = imageWidth - tsUsed - _config.TextPadding;
                                if (customTextWidth < 50) customTextWidth = 50;
                            }
                        }

                        // First measure natural width (without width constraint) to see if it fits on one line
                        SizeF naturalSize;
                        using (StringFormat singleLineFormat = CreateStringFormat(_config.CustomTextAlignment, false, 1))
                        {
                            naturalSize = MeasureTextNaturalSize(graphics, customText, font, singleLineFormat);
                        }

                        // Check if natural width fits in available space
                        if (naturalSize.Width <= customTextWidth)
                        {
                            // Fits on one line - use single line height
                            customTextHeight = (int)Math.Ceiling(naturalSize.Height);
                            customTextFits = true;
                        }
                        else if (_config.EnableTextWrapping)
                        {
                            // Doesn't fit on one line - try with wrapping
                            using (StringFormat wrappingFormat = CreateStringFormat(_config.CustomTextAlignment, true, _config.MaxLines))
                            {
                                SizeF wrappedSize = MeasureTextWithAlignment(graphics, customText, font, wrappingFormat, customTextWidth);
                                customTextHeight = (int)Math.Ceiling(wrappedSize.Height);

                                // Check if fits within MaxLines
                                int linesUsedByCustomText = Math.Max(1, (int)Math.Ceiling((double)customTextHeight / font.Height));
                                if (linesUsedByCustomText > _config.MaxLines)
                                {
                                    customTextFits = false;
                                }
                            }
                        }
                        else
                        {
                            // Doesn't fit and wrapping disabled - will be truncated
                            customTextHeight = (int)Math.Ceiling(naturalSize.Height);
                            customTextFits = false;
                        }
                    }

                    // Calculate total height (side-by-side layout: max of both heights)
                    int totalTextHeight = Math.Max(timestampHeight, customTextHeight);

                    Log.DebugFormat("  Font {0}px: timestamp({1}x{2}, fits={3}), custom({4}x{5}, lines={6}, fits={7}), total={8}",
                        currentFontSize,
                        hasTimestamp ? "yes" : "no", timestampHeight, timestampFits,
                        hasCustomText ? "yes" : "no", customTextHeight,
                        hasCustomText ? Math.Max(1, (int)Math.Ceiling((double)customTextHeight / font.Height)) : 0,
                        customTextFits, totalTextHeight);

                    // Check if both texts fit
                    if (timestampFits && customTextFits)
                    {
                        Log.DebugFormat("  SUCCESS: Using font={0}px, barHeight will be={1}", currentFontSize, totalTextHeight + (_config.TextPadding * 2));
                        int barHeight = totalTextHeight + (_config.TextPadding * 2);

                        // Cap bar height at 4x configured height to prevent extreme cases
                        int maxBarHeight = _config.BarHeight * 4;
                        if (barHeight > maxBarHeight)
                        {
                            barHeight = maxBarHeight;
                        }

                        // Ensure minimum bar height
                        if (barHeight < _config.BarHeight)
                        {
                            barHeight = _config.BarHeight;
                        }

                        int linesUsed = Math.Max(1, (int)Math.Ceiling((double)totalTextHeight / font.Height));

                        return new LayoutInfo
                        {
                            FontSize = currentFontSize,
                            BarHeight = barHeight,
                            LinesUsed = linesUsed
                        };
                    }

                    // Reduce font size and retry
                    currentFontSize *= 0.9f; // Reduce by 10%
                }
            }

            // Couldn't fit even at minimum size - use minimum font size and truncate
            Log.Warn($"Text doesn't fit even at minimum font size ({minFontSize}px), will truncate");

            using (Font font = new Font(_config.FontName, minFontSize))
            {
                int barHeight = (int)(font.Height * _config.MaxLines) + (_config.TextPadding * 2);

                return new LayoutInfo
                {
                    FontSize = minFontSize,
                    BarHeight = barHeight,
                    LinesUsed = _config.MaxLines
                };
            }
        }

        /// <summary>
        /// Measures text size with alignment and wrapping
        /// </summary>
        private SizeF MeasureTextWithAlignment(Graphics graphics, string text, Font font, StringFormat format, int maxWidth)
        {
            if (string.IsNullOrEmpty(text))
            {
                return SizeF.Empty;
            }

            try
            {
                SizeF layoutSize = new SizeF(maxWidth, float.MaxValue);
                return graphics.MeasureString(text, font, layoutSize, format);
            }
            catch (Exception ex)
            {
                Log.Warn("Error measuring text, using default size", ex);
                return new SizeF(maxWidth, font.Height);
            }
        }

        /// <summary>
        /// Measures natural text size without width constraint
        /// </summary>
        private SizeF MeasureTextNaturalSize(Graphics graphics, string text, Font font, StringFormat format)
        {
            if (string.IsNullOrEmpty(text))
            {
                return SizeF.Empty;
            }

            try
            {
                // Measure without width constraint to get natural size
                return graphics.MeasureString(text, font, new SizeF(float.MaxValue, float.MaxValue), format);
            }
            catch (Exception ex)
            {
                Log.Warn("Error measuring text natural size, using default", ex);
                return new SizeF(0, font.Height);
            }
        }

        /// <summary>
        /// Creates StringFormat for text alignment and wrapping
        /// </summary>
        private StringFormat CreateStringFormat(StringAlignment alignment, bool enableWrapping, int maxLines)
        {
            StringFormat format = new StringFormat(StringFormatFlags.NoClip);
            format.Alignment = alignment;
            format.LineAlignment = StringAlignment.Center;

            if (enableWrapping)
            {
                format.FormatFlags &= ~StringFormatFlags.NoWrap;
                format.Trimming = StringTrimming.EllipsisWord;
            }
            else
            {
                format.FormatFlags |= StringFormatFlags.NoWrap;
                format.Trimming = StringTrimming.EllipsisCharacter;
            }

            return format;
        }

        /// <summary>
        /// Draws text with alignment in specified rectangle
        /// </summary>
        private void DrawAlignedText(Graphics graphics, string text, Font font, Brush brush, Rectangle bounds, StringFormat format)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            try
            {
                graphics.DrawString(text, font, brush, bounds, format);
            }
            catch (Exception ex)
            {
                Log.Error("Error drawing text", ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Log.Debug("Disposing CaptionBarProcessor");
            }
            base.Dispose(disposing);
        }
    }
}
