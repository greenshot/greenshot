/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

#nullable disable

using Greenshot.Linux.Core.Languages;
using System.Windows.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Greenshot.Linux.Editor;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Fields;
using Greenshot.Linux.Editor.Serialization;
using SkiaSharp;
using System.Text;
using Greenshot.Linux.RecipeEditor.ViewModels;

namespace Greenshot.Linux.RecipeEditor.Helpers
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

        /// <summary>
        /// What screen readers announce for the element in the import list (instead of the class name)
        /// </summary>
        public override string ToString() => string.IsNullOrEmpty(Description) ? Type : $"{Type}: {Description}";
    }

    /// <summary>
    /// Helper to convert Greenshot surface elements (drawables) and .greenshot files into recipe step annotations.
    /// </summary>
    public static class EditorAnnotationImporter
    {
        /// <summary>
        /// Analyzes all drawable containers from an ISurface and builds preview items with raw asset extraction.
        /// </summary>
        public static List<ImportableElementItem> AnalyzeSurfaceElements(EditorSurface surface)
        {
            var result = new List<ImportableElementItem>();
            if (surface?.Elements == null) return result;

            int index = 1;
            foreach (var element in surface.Elements)
            {
                var item = AnalyzeContainer(element, surface, index++);
                if (item != null)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        /// <summary>
        /// Analyzes a single container into an ImportableElementItem.
        /// </summary>
        public static ImportableElementItem AnalyzeContainer(DrawableElement container, EditorSurface surface = null, int index = 1)
        {
            if (container == null) return null;

            var dict = ConvertContainerToAnnotation(container, surface);
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
                item.Description = string.Format(Texts.Get("RecipeEditor", "annotation_text"), txt.Length > 25 ? txt.Substring(0, 22) + "..." : txt);
            }
            else if (type == "Speechbubble")
            {
                string txt = dict.TryGetValue("Text", out var val) ? val?.ToString() : "";
                item.Description = string.Format(Texts.Get("RecipeEditor", "annotation_bubble"), txt.Length > 25 ? txt.Substring(0, 22) + "..." : txt);
            }
            else if (type == "StepLabel")
            {
                string num = dict.TryGetValue("Number", out var val) ? val?.ToString() : "1";
                item.Description = string.Format(Texts.Get("RecipeEditor", "annotation_step_counter"), num);
            }
            else if (type == "Emoji")
            {
                string emo = dict.TryGetValue("Emoji", out var val) ? val?.ToString() : "🛡️";
                item.Description = string.Format(Texts.Get("RecipeEditor", "annotation_emoji"), emo);
            }
            else if (type == "Arrow" || type == "Line")
            {
                item.Description = string.Format(Texts.Get("RecipeEditor", "annotation_thickness"), type, dict.GetValueOrDefault("LineThickness", 2));
            }
            else if (type == "Blur")
            {
                item.Description = string.Format(Texts.Get("RecipeEditor", "annotation_blur"), dict.GetValueOrDefault("BlurRadius", 10));
            }
            else if (type == "Pixelize")
            {
                item.Description = string.Format(Texts.Get("RecipeEditor", "annotation_pixelize"), dict.GetValueOrDefault("PixelSize", 5));
            }
            else if (type == "Highlight")
            {
                item.Description = Texts.Get("RecipeEditor", "annotation_highlight");
            }
            else if (type == "Magnify")
            {
                item.Description = string.Format(Texts.Get("RecipeEditor", "annotation_magnifier"), dict.GetValueOrDefault("MagnificationFactor", 2));
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
                item.Description = string.Format(Texts.Get("RecipeEditor", "annotation_bitmap"), dims);
                if (container is ImageElement image && image.Png.Length > 0)
                {
                    item.RawData = image.Png;
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
        public static List<Dictionary<string, object>> ImportFromSurface(EditorSurface surface)
        {
            var analyzed = AnalyzeSurfaceElements(surface);
            return ProcessImport(analyzed);
        }

        /// <summary>
        /// Loads a .greenshot file (of the Linux editor) from disk into a new surface.
        /// </summary>
        public static EditorSurface LoadSurfaceFromGreenshotFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("Greenshot file not found", filePath);
            }

            var content = GreenshotFile.Load(filePath);
            var surface = new EditorSurface(content.Image);
            if (!string.IsNullOrEmpty(content.ElementsJson))
            {
                surface.LoadElements(content.ElementsJson);
            }
            else if (!string.IsNullOrEmpty(content.Warning))
            {
                throw new InvalidDataException(content.Warning);
            }
            return surface;
        }

        /// <summary>
        /// Converts a single element into an annotation parameter dictionary.
        /// </summary>
        public static Dictionary<string, object> ConvertContainerToAnnotation(DrawableElement container, EditorSurface surface = null)
        {
            if (container == null) return null;

            var bounds = container.NormalizedBounds;
            int left = (int)Math.Round(bounds.Left);
            int top = (int)Math.Round(bounds.Top);
            int width = (int)Math.Round(bounds.Width);
            int height = (int)Math.Round(bounds.Height);
            var style = container.Style;
            var fields = container.Fields;

            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["OffsetX"] = left.ToString(),
                ["OffsetY"] = top.ToString(),
                ["Width"] = width.ToString(),
                ["Height"] = height.ToString(),
                ["HorizontalAnchor"] = "None",
                ["VerticalAnchor"] = "None"
            };

            // The fields the element has
            if (fields.Contains(FieldType.LineThickness)) dict["LineThickness"] = style.LineThickness;
            if (fields.Contains(FieldType.LineColor)) dict["LineColor"] = FormatColor(style.LineColor);
            if (fields.Contains(FieldType.FillColor)) dict["FillColor"] = FormatColor(style.FillColor);
            if (fields.Contains(FieldType.Shadow)) dict["Shadow"] = style.Shadow;

            switch (container)
            {
                case SpeechBubbleElement speechbubble:
                {
                    dict["Type"] = "Speechbubble";
                    dict["Text"] = speechbubble.Text ?? "";
                    ExtractFontSettings(style, dict);
                    var target = speechbubble.Target;
                    int centerX = left + width / 2;
                    int centerY = top + height / 2;
                    float dx = target.X - centerX;
                    float dy = target.Y - centerY;
                    dict["TailDirection"] = dy >= 0 ? (dx < 0 ? "BottomLeft" : "BottomRight") : (dx < 0 ? "TopLeft" : "TopRight");
                    dict["TailOffsetX"] = ((int)Math.Round(target.X - left)).ToString();
                    dict["TailOffsetY"] = ((int)Math.Round(target.Y - top)).ToString();
                    break;
                }
                case TextElement textElement:
                    dict["Type"] = "Text";
                    dict["Text"] = textElement.Text ?? "";
                    ExtractFontSettings(style, dict);
                    break;
                case StepLabelElement stepLabel:
                {
                    dict["Type"] = "StepLabel";
                    int num = surface?.StepNumber(stepLabel) ?? 1;
                    dict["Number"] = num;
                    dict["Text"] = num.ToString();
                    break;
                }
                case ArrowElement arrow:
                    dict["Type"] = "Arrow";
                    dict["ArrowHeads"] = FormatArrowHeads(arrow.Style.ArrowHeads);
                    break;
                case LineElement:
                    dict["Type"] = "Line";
                    break;
                case EllipseElement:
                    dict["Type"] = "Ellipse";
                    break;
                case FreehandElement:
                    dict["Type"] = "Freehand";
                    break;
                case ObfuscateElement:
                    if (style.ObfuscateFilter == ObfuscateFilter.Blur)
                    {
                        dict["Type"] = "Blur";
                        dict["BlurRadius"] = style.BlurRadius;
                    }
                    else
                    {
                        dict["Type"] = "Pixelize";
                        dict["PixelSize"] = style.PixelSize;
                    }
                    break;
                case HighlightElement:
                    if (style.HighlightFilter == HighlightFilter.Magnification)
                    {
                        dict["Type"] = "Magnify";
                        dict["MagnificationFactor"] = style.MagnificationFactor;
                    }
                    else
                    {
                        dict["Type"] = "Highlight";
                        dict["FillColor"] = FormatColor(style.FillColor);
                    }
                    break;
                case ImageElement:
                    dict["Type"] = "Image";
                    break;
                default:
                    dict["Type"] = "Rectangle";
                    break;
            }

            return dict;
        }

        private static void ExtractFontSettings(ElementStyle style, Dictionary<string, object> dict)
        {
            if (!string.IsNullOrEmpty(style.FontFamily)) dict["FontFamily"] = style.FontFamily;
            dict["FontSize"] = (double)style.FontSize;
            dict["Bold"] = style.FontBold;
            dict["Italic"] = style.FontItalic;
            dict["TextAlign"] = style.TextHorizontalAlignment switch
            {
                TextHorizontalAlignment.Left => "Left",
                TextHorizontalAlignment.Right => "Right",
                _ => "Center"
            };
        }

        private static string FormatArrowHeads(ArrowHeads heads) => heads switch
        {
            ArrowHeads.None => "NONE",
            ArrowHeads.Start => "START_POINT",
            ArrowHeads.Both => "BOTH",
            _ => "END_POINT"
        };

        public static string FormatColor(SKColor color)
        {
            if (color.Alpha == 0) return "transparent";
            if (color.Alpha == 255) return $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
            return $"#{color.Alpha:X2}{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
        }
    }
}
