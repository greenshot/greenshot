/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Icons;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Filters;
using Greenshot.Plugin.RecipeEditor.ViewModels;

namespace Greenshot.Plugin.RecipeEditor.Helpers
{
    internal static class ImporterDictionaryExtensions
    {
        public static object GetValueOrDefault(this Dictionary<string, object> dictionary, string key, object defaultValue = null)
        {
            return dictionary != null && dictionary.TryGetValue(key, out var val) ? val : defaultValue;
        }
    }
    public enum AssetStorageMode
    {
        EmbedBase64,
        SaveToFile
    }

    /// <summary>
    /// Represents an element extracted from an ISurface ready for user review and import.
    /// </summary>
    public class ImportableElementItem : ViewModelBase
    {
        private bool _isSelected = true;
        private AssetStorageMode _storageMode = AssetStorageMode.EmbedBase64;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value);
        }

        public string Type { get; set; } = "Rectangle";
        public string Description { get; set; } = "";
        public string Dimensions { get; set; } = "";
        public bool CanSaveAsFile { get; set; }

        public AssetStorageMode StorageMode
        {
            get => _storageMode;
            set => SetField(ref _storageMode, value);
        }

        public byte[] RawData { get; set; }
        public string FileExtension { get; set; } = ".png";
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Helper to convert Greenshot surface elements (drawables) and .greenshot files into recipe step annotations.
    /// </summary>
    public static class EditorAnnotationImporter
    {
        /// <summary>
        /// Analyzes all drawable containers from an ISurface and builds preview items with raw asset extraction.
        /// </summary>
        public static List<ImportableElementItem> AnalyzeSurfaceElements(ISurface surface)
        {
            var result = new List<ImportableElementItem>();
            if (surface?.Elements == null) return result;

            int index = 1;
            foreach (var element in surface.Elements)
            {
                if (element is IDrawableContainer container)
                {
                    var item = AnalyzeContainer(container, index++);
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Analyzes a single container into an ImportableElementItem.
        /// </summary>
        public static ImportableElementItem AnalyzeContainer(IDrawableContainer container, int index = 1)
        {
            if (container == null) return null;

            var dict = ConvertContainerToAnnotation(container);
            if (dict == null) return null;

            string type = dict.TryGetValue("Type", out var t) && t != null ? t.ToString() : "Rectangle";
            string widthStr = dict.TryGetValue("Width", out var w) && w != null ? w.ToString() : "0";
            string heightStr = dict.TryGetValue("Height", out var h) && h != null ? h.ToString() : "0";
            string dims = $"{widthStr} × {heightStr} px";

            var item = new ImportableElementItem
            {
                Type = type,
                Dimensions = dims,
                Parameters = dict
            };

            // Build human-friendly description
            if (type == "Text")
            {
                string txt = dict.TryGetValue("Text", out var val) ? val?.ToString() : "";
                item.Description = $"Text: \"{(txt.Length > 25 ? txt.Substring(0, 22) + "..." : txt)}\"";
            }
            else if (type == "Speechbubble")
            {
                string txt = dict.TryGetValue("Text", out var val) ? val?.ToString() : "";
                item.Description = $"Bubble: \"{(txt.Length > 25 ? txt.Substring(0, 22) + "..." : txt)}\"";
            }
            else if (type == "StepLabel")
            {
                string num = dict.TryGetValue("Number", out var val) ? val?.ToString() : "1";
                item.Description = $"Step Counter #{num}";
            }
            else if (type == "Emoji")
            {
                string emo = dict.TryGetValue("Emoji", out var val) ? val?.ToString() : "🛡️";
                item.Description = $"Emoji {emo}";
            }
            else if (type == "Arrow" || type == "Line")
            {
                item.Description = $"{type} (Thickness: {dict.GetValueOrDefault("LineThickness", 2)})";
            }
            else if (type == "Blur")
            {
                item.Description = $"Blur (Radius: {dict.GetValueOrDefault("BlurRadius", 10)})";
            }
            else if (type == "Pixelize")
            {
                item.Description = $"Pixelize (Block: {dict.GetValueOrDefault("PixelSize", 5)})";
            }
            else if (type == "Highlight")
            {
                item.Description = $"Highlight Area";
            }
            else if (type == "Magnify")
            {
                item.Description = $"Magnifier ({dict.GetValueOrDefault("MagnificationFactor", 2)}x)";
            }
            else if (type == "QRCode" || type == "Barcode")
            {
                string txt = dict.TryGetValue("Text", out var val) ? val?.ToString() : "";
                item.Description = $"{type}: {(txt.Length > 20 ? txt.Substring(0, 18) + "..." : txt)}";
            }
            else if (type == "Image")
            {
                item.CanSaveAsFile = true;
                item.FileExtension = ".png";
                item.Description = $"Bitmap Image ({dims})";

                if (container is ImageContainer imgContainer && imgContainer.Image != null)
                {
                    try
                    {
                        using var ms = new MemoryStream();
                        imgContainer.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        item.RawData = ms.ToArray();
                    }
                    catch { }
                }
            }
            else if (type == "Svg")
            {
                item.CanSaveAsFile = true;
                item.FileExtension = ".svg";
                item.Description = "Scalable Vector Graphics (SVG)";

                if (dict.TryGetValue("Content", out var contentObj) && contentObj is string svgXml && !string.IsNullOrEmpty(svgXml))
                {
                    item.RawData = Encoding.UTF8.GetBytes(svgXml);
                }
            }
            else if (type == "Cursor")
            {
                item.CanSaveAsFile = true;
                item.FileExtension = ".png";
                item.Description = "Mouse Cursor (Bitmap)";

                if (container is CursorContainer curContainer && curContainer.Cursor != null)
                {
                    try
                    {
                        var cur = curContainer.Cursor;
                        int cw = cur.Size.Width > 0 ? cur.Size.Width : 32;
                        int ch = cur.Size.Height > 0 ? cur.Size.Height : 32;
                        using var bmp = new Bitmap(cw, ch);
                        using (var g = Graphics.FromImage(bmp))
                        {
                            Dapplo.Windows.Icons.CursorHelper.DrawCursorOnGraphics(g, cur, new NativePoint(0, 0), cur.Size);
                        }
                        using var ms = new MemoryStream();
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        item.RawData = ms.ToArray();
                        item.Parameters["HotspotX"] = cur.HotSpot.X;
                        item.Parameters["HotspotY"] = cur.HotSpot.Y;
                    }
                    catch { }
                }
            }
            else
            {
                item.Description = $"{type} ({dims})";
            }

            return item;
        }

        /// <summary>
        /// Finalizes selected items into dictionaries, writing files to disk if SaveToFile was chosen.
        /// </summary>
        public static List<Dictionary<string, object>> ProcessImport(
            IEnumerable<ImportableElementItem> items,
            string targetDirectory = null,
            string baseFileName = "asset")
        {
            var result = new List<Dictionary<string, object>>();
            if (items == null) return result;

            int fileCounter = 1;
            if (!string.IsNullOrWhiteSpace(targetDirectory) && !Directory.Exists(targetDirectory))
            {
                try
                {
                    Directory.CreateDirectory(targetDirectory);
                }
                catch { }
            }

            foreach (var item in items)
            {
                if (!item.IsSelected) continue;

                var dict = new Dictionary<string, object>(item.Parameters, StringComparer.OrdinalIgnoreCase);

                if (item.CanSaveAsFile && item.RawData != null && item.RawData.Length > 0)
                {
                    if (item.StorageMode == AssetStorageMode.SaveToFile && !string.IsNullOrWhiteSpace(targetDirectory))
                    {
                        string safeBase = string.Join("_", (baseFileName ?? "asset").Split(Path.GetInvalidFileNameChars()));
                        string fileName = $"{safeBase}_{item.Type.ToLowerInvariant()}_{fileCounter++}{item.FileExtension}";
                        string fullPath = Path.Combine(targetDirectory, fileName);
                        try
                        {
                            File.WriteAllBytes(fullPath, item.RawData);
                            dict["FilePath"] = fullPath;
                            dict.Remove("ImageData");
                            dict.Remove("Base64");
                        }
                        catch (Exception)
                        {
                            // Fallback to Base64 if file writing failed
                            dict["ImageData"] = Convert.ToBase64String(item.RawData);
                        }
                    }
                    else
                    {
                        // Embed as Base64
                        dict["ImageData"] = Convert.ToBase64String(item.RawData);
                        dict.Remove("FilePath");
                    }
                }

                result.Add(dict);
            }

            return result;
        }

        /// <summary>
        /// Imports all drawable containers from an ISurface as a list of annotation dictionaries.
        /// </summary>
        public static List<Dictionary<string, object>> ImportFromSurface(ISurface surface)
        {
            var analyzed = AnalyzeSurfaceElements(surface);
            return ProcessImport(analyzed);
        }

        /// <summary>
        /// Loads a .greenshot file from disk into a new ISurface.
        /// </summary>
        public static ISurface LoadSurfaceFromGreenshotFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("Greenshot file not found", filePath);
            }

            ISurface surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>()?.Invoke() ?? new Surface();
            return ImageIO.LoadGreenshotSurface(filePath, surface);
        }

        /// <summary>
        /// Converts a single IDrawableContainer into an annotation parameter dictionary.
        /// </summary>
        public static Dictionary<string, object> ConvertContainerToAnnotation(IDrawableContainer container)
        {
            if (container == null) return null;

            int left = container.Left;
            int top = container.Top;
            int width = container.Width;
            int height = container.Height;

            if (width < 0)
            {
                left += width;
                width = -width;
            }
            if (height < 0)
            {
                top += height;
                height = -height;
            }

            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["OffsetX"] = left.ToString(),
                ["OffsetY"] = top.ToString(),
                ["Width"] = width.ToString(),
                ["Height"] = height.ToString(),
                ["HorizontalAnchor"] = "None",
                ["VerticalAnchor"] = "None"
            };

            // Extract common field values if available
            if (container is IFieldHolder fieldHolder)
            {
                var lt = GetInt(fieldHolder, FieldType.LINE_THICKNESS);
                if (lt.HasValue) dict["LineThickness"] = lt.Value;

                var lc = GetVal(fieldHolder, FieldType.LINE_COLOR);
                if (lc is Color c1) dict["LineColor"] = FormatColor(c1);

                var fc = GetVal(fieldHolder, FieldType.FILL_COLOR);
                if (fc is Color c2) dict["FillColor"] = FormatColor(c2);

                var sh = GetBool(fieldHolder, FieldType.SHADOW);
                if (sh.HasValue) dict["Shadow"] = sh.Value;
            }

            // Container type classification and property extraction
            if (container is SpeechbubbleContainer speechbubble)
            {
                dict["Type"] = "Speechbubble";
                dict["Text"] = speechbubble.Text ?? "";
                ExtractFontSettings(speechbubble, dict);

                Point targetPoint = Point.Empty;
                if (speechbubble.TargetAdorner != null)
                {
                    targetPoint = speechbubble.TargetAdorner.Location;
                }
                else
                {
                    var field = typeof(SpeechbubbleContainer).GetField("_storedTargetGripperLocation", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field?.GetValue(speechbubble) is Point p && !p.IsEmpty)
                    {
                        targetPoint = p;
                    }
                }

                if (!targetPoint.IsEmpty)
                {
                    int centerX = left + width / 2;
                    int centerY = top + height / 2;
                    int dx = targetPoint.X - centerX;
                    int dy = targetPoint.Y - centerY;
                    string dir;
                    if (dy >= 0)
                    {
                        dir = dx < 0 ? "BottomLeft" : "BottomRight";
                    }
                    else
                    {
                        dir = dx < 0 ? "TopLeft" : "TopRight";
                    }
                    dict["TailDirection"] = dir;
                    dict["TailOffsetX"] = (targetPoint.X - left).ToString();
                    dict["TailOffsetY"] = (targetPoint.Y - top).ToString();
                }
            }
            else if (container is TextContainer textContainer)
            {
                dict["Type"] = "Text";
                dict["Text"] = textContainer.Text ?? "";
                ExtractFontSettings(textContainer, dict);
            }
            else if (container is StepLabelContainer stepLabel)
            {
                dict["Type"] = "StepLabel";
                int num = stepLabel.Number;
                dict["Number"] = num;
                dict["Text"] = num.ToString();
            }
            else if (container is IEmojiContainer emojiContainer)
            {
                dict["Type"] = "Emoji";
                dict["Emoji"] = emojiContainer.Emoji ?? "🛡️";
            }
            else if (container is ArrowContainer arrow)
            {
                dict["Type"] = "Arrow";
                object val = GetVal(arrow, FieldType.ARROWHEADS);
                if (val != null)
                {
                    dict["ArrowHeads"] = FormatArrowHeads(val);
                }
            }
            else if (container is LineContainer)
            {
                dict["Type"] = "Line";
            }
            else if (container is EllipseContainer)
            {
                dict["Type"] = "Ellipse";
            }
            else if (container is FreehandContainer)
            {
                dict["Type"] = "Freehand";
            }
            else if (container is RectangleContainer)
            {
                dict["Type"] = "Rectangle";
            }
            else if (container is ObfuscateContainer obfuscate)
            {
                var preset = GetVal(obfuscate, FieldType.PREPARED_FILTER_OBFUSCATE);
                bool isBlur = false;
                if (preset is FilterContainer.PreparedFilter pf && pf == FilterContainer.PreparedFilter.BLUR) isBlur = true;
                else if (preset != null && preset.ToString().IndexOf("BLUR", StringComparison.OrdinalIgnoreCase) >= 0) isBlur = true;
                else if (obfuscate.Filters.Any(f => f is BlurFilter)) isBlur = true;

                if (isBlur)
                {
                    dict["Type"] = "Blur";
                    var blurFilter = obfuscate.Filters.OfType<BlurFilter>().FirstOrDefault();
                    if (blurFilter != null)
                    {
                        var br = GetInt(blurFilter, FieldType.BLUR_RADIUS);
                        if (br.HasValue) dict["BlurRadius"] = br.Value;
                    }
                }
                else
                {
                    dict["Type"] = "Pixelize";
                    var pixFilter = obfuscate.Filters.OfType<PixelizationFilter>().FirstOrDefault();
                    if (pixFilter != null)
                    {
                        var ps = GetInt(pixFilter, FieldType.PIXEL_SIZE);
                        if (ps.HasValue) dict["PixelSize"] = ps.Value;
                    }
                }
            }
            else if (container is HighlightContainer highlight)
            {
                var preset = GetVal(highlight, FieldType.PREPARED_FILTER_HIGHLIGHT);
                bool isMagnify = false;
                if (preset is FilterContainer.PreparedFilter pf && pf == FilterContainer.PreparedFilter.MAGNIFICATION) isMagnify = true;
                else if (preset != null && preset.ToString().IndexOf("MAGNIF", StringComparison.OrdinalIgnoreCase) >= 0) isMagnify = true;
                else if (highlight.Filters.Any(f => f is MagnifierFilter)) isMagnify = true;

                if (isMagnify)
                {
                    dict["Type"] = "Magnify";
                    var magFilter = highlight.Filters.OfType<MagnifierFilter>().FirstOrDefault();
                    if (magFilter != null)
                    {
                        var mf = GetInt(magFilter, FieldType.MAGNIFICATION_FACTOR);
                        if (mf.HasValue) dict["MagnificationFactor"] = mf.Value;
                    }
                }
                else
                {
                    dict["Type"] = "Highlight";
                    var hlFilter = highlight.Filters.OfType<HighlightFilter>().FirstOrDefault();
                    if (hlFilter != null)
                    {
                        var fc = GetVal(hlFilter, FieldType.FILL_COLOR);
                        if (fc is Color c) dict["FillColor"] = FormatColor(c);
                    }
                }
            }
            else if (container is SvgContainer svg)
            {
                dict["Type"] = "Svg";
                try
                {
                    var field = typeof(SvgContainer).GetField("_svgContent", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field?.GetValue(svg) is MemoryStream ms)
                    {
                        dict["Content"] = Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
                catch { }
            }
            else if (container.GetType().Name.IndexOf("Barcode", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ExtractBarcodeSettings(container, dict);
            }
            else if (container is ImageContainer)
            {
                dict["Type"] = "Image";
            }
            else if (container.GetType().Name.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                dict["Type"] = "Icon";
            }
            else if (container.GetType().Name.IndexOf("Cursor", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                dict["Type"] = "Cursor";
            }
            else
            {
                dict["Type"] = "Rectangle";
            }

            return dict;
        }

        private static void ExtractFontSettings(IFieldHolder container, Dictionary<string, object> dict)
        {
            string ff = GetString(container, FieldType.FONT_FAMILY);
            if (!string.IsNullOrEmpty(ff)) dict["FontFamily"] = ff;

            double? fs = GetDouble(container, FieldType.FONT_SIZE);
            if (fs.HasValue) dict["FontSize"] = fs.Value;

            bool? fb = GetBool(container, FieldType.FONT_BOLD);
            if (fb.HasValue) dict["Bold"] = fb.Value;

            bool? fi = GetBool(container, FieldType.FONT_ITALIC);
            if (fi.HasValue) dict["Italic"] = fi.Value;

            var align = GetVal(container, FieldType.TEXT_HORIZONTAL_ALIGNMENT);
            if (align != null)
            {
                if (align is StringAlignment sa)
                {
                    dict["TextAlign"] = sa switch
                    {
                        StringAlignment.Near => "Left",
                        StringAlignment.Far => "Right",
                        _ => "Center"
                    };
                }
                else
                {
                    dict["TextAlign"] = align.ToString();
                }
            }
        }

        private static void ExtractBarcodeSettings(IDrawableContainer container, Dictionary<string, object> dict)
        {
            object model = null;
            var modelProp = container.GetType().GetProperty("Model");
            if (modelProp != null)
            {
                model = modelProp.GetValue(container);
            }
            if (model == null && container is DrawableContainer dc)
            {
                model = dc.Tag;
            }

            dict["Type"] = "QRCode";
            if (model != null)
            {
                var modelType = model.GetType();
                var rawTextProp = modelType.GetProperty("RawText");
                if (rawTextProp?.GetValue(model) is string rawText && !string.IsNullOrEmpty(rawText))
                {
                    dict["Text"] = rawText;
                }

                var foreColorProp = modelType.GetProperty("ForeColor");
                if (foreColorProp?.GetValue(model) is Color foreColor)
                {
                    dict["ForeColor"] = FormatColor(foreColor);
                }

                var backColorProp = modelType.GetProperty("BackColor");
                if (backColorProp?.GetValue(model) is Color backColor)
                {
                    dict["BackColor"] = FormatColor(backColor);
                }

                var marginProp = modelType.GetProperty("Margin");
                if (marginProp?.GetValue(model) is int margin)
                {
                    dict["Margin"] = margin;
                }

                var roundedDotsProp = modelType.GetProperty("RoundedDots");
                if (roundedDotsProp?.GetValue(model) is bool roundedDots)
                {
                    dict["RoundedDots"] = roundedDots;
                }

                var formatIndexProp = modelType.GetProperty("FormatIndex");
                if (formatIndexProp?.GetValue(model) is int fmtIdx && fmtIdx > 0)
                {
                    dict["Type"] = "Barcode";
                }
            }
        }

        private static object GetVal(IFieldHolder holder, IFieldType fieldType)
        {
            return holder != null && holder.HasField(fieldType) ? holder.GetField(fieldType)?.Value : null;
        }

        private static int? GetInt(IFieldHolder holder, IFieldType fieldType)
        {
            var val = GetVal(holder, fieldType);
            if (val is int i) return i;
            if (val != null && int.TryParse(val.ToString(), out int parsed)) return parsed;
            return null;
        }

        private static double? GetDouble(IFieldHolder holder, IFieldType fieldType)
        {
            var val = GetVal(holder, fieldType);
            if (val is double d) return d;
            if (val is float f) return f;
            if (val != null && double.TryParse(val.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsed)) return parsed;
            return null;
        }

        private static bool? GetBool(IFieldHolder holder, IFieldType fieldType)
        {
            var val = GetVal(holder, fieldType);
            if (val is bool b) return b;
            if (val != null && bool.TryParse(val.ToString(), out bool parsed)) return parsed;
            return null;
        }

        private static string GetString(IFieldHolder holder, IFieldType fieldType)
        {
            var val = GetVal(holder, fieldType);
            return val?.ToString();
        }

        private static string FormatArrowHeads(object val)
        {
            if (val == null) return "END_POINT";
            if (val is int i)
            {
                return i switch
                {
                    0 => "NONE",
                    1 => "START_POINT",
                    2 => "END_POINT",
                    3 => "BOTH",
                    _ => "END_POINT"
                };
            }
            string s = val.ToString();
            if (int.TryParse(s, out int parsed))
            {
                return FormatArrowHeads(parsed);
            }
            return s;
        }

        public static string FormatColor(Color color)
        {
            if (color.A == 0) return "transparent";
            if (color.A == 255) return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
