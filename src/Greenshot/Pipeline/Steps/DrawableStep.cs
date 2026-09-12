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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Expressions;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.Drawing.Fields;
using log4net;
using Newtonsoft.Json.Linq;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// Pipeline step that instantiates and places any available drawable element onto the visual surface.
    /// Supports absolute, calculated (expressions using width/height), and anchored (Left/Center/Right, Top/Middle/Bottom) positioning.
    /// </summary>
    public class DrawableStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DrawableStep));

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        public DrawableStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.Id ?? WellKnownStepTypes.Drawable;
        }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var payload = context.Payload;
            if (payload == null)
            {
                context.LogStep("DrawableStep skipped: visual payload is null.");
                return Task.CompletedTask;
            }

            var surface = payload.EnsureSurface();
            if (surface?.Image == null)
            {
                context.LogStep("DrawableStep skipped: surface image is not available.");
                return Task.CompletedTask;
            }

            int surfaceWidth = surface.Image.Width;
            int surfaceHeight = surface.Image.Height;

            var extraVariables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["payload.width"] = surfaceWidth,
                ["payload.height"] = surfaceHeight,
                ["surface.width"] = surfaceWidth,
                ["surface.height"] = surfaceHeight,
                ["width"] = surfaceWidth,
                ["w"] = surfaceWidth,
                ["height"] = surfaceHeight,
                ["h"] = surfaceHeight
            };

            // Support either a single drawable defined in parameters, or a list under "Drawables"
            var drawableConfigs = new List<Dictionary<string, object>>();

            if (NodeConfig.Parameters != null && NodeConfig.Parameters.TryGetValue("Drawables", out var drawablesObj) && drawablesObj != null)
            {
                if (drawablesObj is IEnumerable enumerable && !(drawablesObj is string))
                {
                    foreach (var item in enumerable)
                    {
                        if (item is Dictionary<string, object> d)
                        {
                            drawableConfigs.Add(d);
                        }
                        else if (item is JObject jObj)
                        {
                            drawableConfigs.Add(jObj.ToObject<Dictionary<string, object>>());
                        }
                    }
                }
            }
            else
            {
                drawableConfigs.Add(NodeConfig.Parameters ?? new Dictionary<string, object>());
            }

            var elementsToAdd = new DrawableContainerList();

            foreach (var rawParams in drawableConfigs)
            {
                var resolved = ExpressionEvaluator.Instance.ResolveParameters(rawParams, context, extraVariables);
                var container = CreateDrawable(surface, resolved, context, extraVariables);
                if (container != null)
                {
                    elementsToAdd.Add(container);
                }
            }

            if (elementsToAdd.Count > 0)
            {
                if (surface is Surface s)
                {
                    s.SuspendLayout();
                }
                foreach (var element in elementsToAdd)
                {
                    element.Selected = false;
                    surface.AddElement(element, makeUndoable: true, invalidate: false);
                }
                if (surface is Surface s2)
                {
                    s2.ResumeLayout();
                }
                surface.Invalidate();
                surface.Modified = true;

                // Invalidate composite cache
                if (payload.SharedRenderedBitmap != null)
                {
                    payload.SharedRenderedBitmap.Dispose();
                    payload.SharedRenderedBitmap = null;
                }

                context.LogStep($"DrawableStep added {elementsToAdd.Count} element(s) to surface.");
                Log.InfoFormat("DrawableStep '{0}' placed {1} element(s) on surface ({2}x{3})", Name, elementsToAdd.Count, surfaceWidth, surfaceHeight);
            }

            return Task.CompletedTask;
        }

        private static DrawableContainer CreateDrawable(
            ISurface surface,
            Dictionary<string, object> parameters,
            CaptureFlowContext context,
            Dictionary<string, object> extraVariables)
        {
            string drawableType = GetString(parameters, "DrawableType")
                ?? GetString(parameters, "Type")
                ?? GetString(parameters, "Shape")
                ?? GetString(parameters, "Element")
                ?? "Rectangle";

            DrawableContainer container = null;

            switch (drawableType.ToLowerInvariant())
            {
                case "rect":
                case "rectangle":
                    container = CreateRectangle(surface, parameters);
                    break;

                case "ellipse":
                case "circle":
                    container = CreateEllipse(surface, parameters);
                    break;

                case "line":
                    container = CreateLine(surface, parameters);
                    break;

                case "arrow":
                    container = CreateArrow(surface, parameters);
                    break;

                case "freehand":
                case "path":
                    container = CreateFreehand(surface, parameters);
                    break;

                case "text":
                case "textbox":
                case "label":
                    container = CreateText(surface, parameters);
                    break;

                case "speechbubble":
                case "bubble":
                    container = CreateSpeechbubble(surface, parameters);
                    break;

                case "step":
                case "steplabel":
                case "counter":
                    container = CreateStepLabel(surface, parameters);
                    break;

                case "image":
                case "bitmap":
                case "picture":
                    container = CreateImage(surface, parameters);
                    break;

                case "icon":
                    container = CreateIcon(surface, parameters);
                    break;

                case "cursor":
                    container = CreateCursor(surface, parameters);
                    break;

                case "emoji":
                    container = CreateEmoji(surface, parameters);
                    break;

                case "svg":
                    container = CreateSvg(surface, parameters);
                    break;

                case "blur":
                case "pixelize":
                case "obfuscate":
                    container = CreateObfuscate(surface, parameters, drawableType);
                    break;

                case "highlight":
                case "magnify":
                    container = CreateHighlight(surface, parameters, drawableType);
                    break;

                case "crop":
                    container = new CropContainer(surface);
                    break;

                default:
                    Log.WarnFormat("Unknown drawable type '{0}'. Defaulting to RectangleContainer.", drawableType);
                    container = CreateRectangle(surface, parameters);
                    break;
            }

            if (container != null)
            {
                ApplyPositioning(container, surface, parameters, extraVariables);
            }

            return container;
        }

        #region Container Creators

        private static RectangleContainer CreateRectangle(ISurface surface, Dictionary<string, object> p)
        {
            var rect = new RectangleContainer(surface);
            rect.SetFieldValue(FieldType.LINE_THICKNESS, GetInt(p, "LineThickness", GetInt(p, "BorderWidth", 2)));
            rect.SetFieldValue(FieldType.LINE_COLOR, GetColor(p, "LineColor", GetColor(p, "BorderColor", Color.Red)));
            rect.SetFieldValue(FieldType.FILL_COLOR, GetColor(p, "FillColor", Color.Transparent));
            rect.SetFieldValue(FieldType.SHADOW, GetBool(p, "Shadow", true));
            return rect;
        }

        private static EllipseContainer CreateEllipse(ISurface surface, Dictionary<string, object> p)
        {
            var ellipse = new EllipseContainer(surface);
            ellipse.SetFieldValue(FieldType.LINE_THICKNESS, GetInt(p, "LineThickness", 2));
            ellipse.SetFieldValue(FieldType.LINE_COLOR, GetColor(p, "LineColor", Color.Red));
            ellipse.SetFieldValue(FieldType.FILL_COLOR, GetColor(p, "FillColor", Color.Transparent));
            ellipse.SetFieldValue(FieldType.SHADOW, GetBool(p, "Shadow", true));
            return ellipse;
        }

        private static LineContainer CreateLine(ISurface surface, Dictionary<string, object> p)
        {
            var line = new LineContainer(surface);
            line.SetFieldValue(FieldType.LINE_THICKNESS, GetInt(p, "LineThickness", 2));
            line.SetFieldValue(FieldType.LINE_COLOR, GetColor(p, "LineColor", Color.Red));
            line.SetFieldValue(FieldType.SHADOW, GetBool(p, "Shadow", true));
            return line;
        }

        private static ArrowContainer CreateArrow(ISurface surface, Dictionary<string, object> p)
        {
            var arrow = new ArrowContainer(surface);
            arrow.SetFieldValue(FieldType.LINE_THICKNESS, GetInt(p, "LineThickness", 2));
            arrow.SetFieldValue(FieldType.LINE_COLOR, GetColor(p, "LineColor", Color.Red));
            arrow.SetFieldValue(FieldType.SHADOW, GetBool(p, "Shadow", true));

            string headsStr = GetString(p, "ArrowHeads") ?? GetString(p, "Heads") ?? "END_POINT";
            if (Enum.TryParse<ArrowContainer.ArrowHeadCombination>(headsStr, true, out var heads))
            {
                arrow.SetFieldValue(FieldType.ARROWHEADS, heads);
            }
            return arrow;
        }

        private static FreehandContainer CreateFreehand(ISurface surface, Dictionary<string, object> p)
        {
            var freehand = new FreehandContainer(surface);
            freehand.SetFieldValue(FieldType.LINE_THICKNESS, GetInt(p, "LineThickness", 2));
            freehand.SetFieldValue(FieldType.LINE_COLOR, GetColor(p, "LineColor", Color.Red));
            freehand.SetFieldValue(FieldType.SHADOW, GetBool(p, "Shadow", false));

            string pointsStr = GetString(p, "Points");
            if (!string.IsNullOrWhiteSpace(pointsStr))
            {
                var pairs = pointsStr.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                bool isFirst = true;
                Point lastPoint = Point.Empty;
                foreach (var pair in pairs)
                {
                    var coords = pair.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (coords.Length == 2 && int.TryParse(coords[0], out int px) && int.TryParse(coords[1], out int py))
                    {
                        if (isFirst)
                        {
                            freehand.HandleMouseDown(px, py);
                            isFirst = false;
                        }
                        else
                        {
                            freehand.HandleMouseMove(px, py);
                        }
                        lastPoint = new Point(px, py);
                    }
                }
                if (!isFirst)
                {
                    freehand.HandleMouseUp(lastPoint.X, lastPoint.Y);
                }
            }
            return freehand;
        }

        private static TextContainer CreateText(ISurface surface, Dictionary<string, object> p)
        {
            var text = new TextContainer(surface);
            text.Text = GetString(p, "Text") ?? string.Empty;
            text.SetFieldValue(FieldType.FONT_FAMILY, GetString(p, "FontFamily") ?? FontFamily.GenericSansSerif.Name);
            text.SetFieldValue(FieldType.FONT_SIZE, (float)GetDouble(p, "FontSize", 12.0));
            text.SetFieldValue(FieldType.FONT_BOLD, GetBool(p, "Bold", GetBool(p, "FontBold", false)));
            text.SetFieldValue(FieldType.FONT_ITALIC, GetBool(p, "Italic", GetBool(p, "FontItalic", false)));
            text.SetFieldValue(FieldType.LINE_COLOR, GetColor(p, "TextColor", GetColor(p, "LineColor", Color.Red)));
            text.SetFieldValue(FieldType.FILL_COLOR, GetColor(p, "FillColor", GetColor(p, "BackgroundColor", Color.Transparent)));
            text.SetFieldValue(FieldType.LINE_THICKNESS, GetInt(p, "LineThickness", GetInt(p, "BorderWidth", 0)));
            text.SetFieldValue(FieldType.SHADOW, GetBool(p, "Shadow", true));

            string alignH = GetString(p, "TextHorizontalAlignment") ?? GetString(p, "TextAlign") ?? "Center";
            if (string.Equals(alignH, "Left", StringComparison.OrdinalIgnoreCase) || string.Equals(alignH, "Near", StringComparison.OrdinalIgnoreCase))
                text.SetFieldValue(FieldType.TEXT_HORIZONTAL_ALIGNMENT, StringAlignment.Near);
            else if (string.Equals(alignH, "Right", StringComparison.OrdinalIgnoreCase) || string.Equals(alignH, "Far", StringComparison.OrdinalIgnoreCase))
                text.SetFieldValue(FieldType.TEXT_HORIZONTAL_ALIGNMENT, StringAlignment.Far);
            else
                text.SetFieldValue(FieldType.TEXT_HORIZONTAL_ALIGNMENT, StringAlignment.Center);

            if (GetBool(p, "FitToText", true) && !p.ContainsKey("Width"))
            {
                text.FitToText();
            }

            return text;
        }

        private static SpeechbubbleContainer CreateSpeechbubble(ISurface surface, Dictionary<string, object> p)
        {
            var bubble = new SpeechbubbleContainer(surface);
            bubble.Text = GetString(p, "Text") ?? string.Empty;
            bubble.SetFieldValue(FieldType.FONT_FAMILY, GetString(p, "FontFamily") ?? FontFamily.GenericSansSerif.Name);
            bubble.SetFieldValue(FieldType.FONT_SIZE, (float)GetDouble(p, "FontSize", 14.0));
            bubble.SetFieldValue(FieldType.FONT_BOLD, GetBool(p, "Bold", true));
            bubble.SetFieldValue(FieldType.LINE_COLOR, GetColor(p, "LineColor", Color.Blue));
            bubble.SetFieldValue(FieldType.FILL_COLOR, GetColor(p, "FillColor", Color.White));
            bubble.SetFieldValue(FieldType.LINE_THICKNESS, GetInt(p, "LineThickness", 2));
            bubble.SetFieldValue(FieldType.SHADOW, GetBool(p, "Shadow", false));
            return bubble;
        }

        private static StepLabelContainer CreateStepLabel(ISurface surface, Dictionary<string, object> p)
        {
            var stepLabel = new StepLabelContainer(surface);
            int number = GetInt(p, "Number", GetInt(p, "Counter", 1));
            stepLabel.Number = number;
            stepLabel.SetFieldValue(FieldType.FILL_COLOR, GetColor(p, "FillColor", Color.DarkRed));
            stepLabel.SetFieldValue(FieldType.LINE_COLOR, GetColor(p, "NumberColor", GetColor(p, "LineColor", Color.White)));
            stepLabel.SetFieldValue(FieldType.SHADOW, GetBool(p, "Shadow", false));
            return stepLabel;
        }

        private static ImageContainer CreateImage(ISurface surface, Dictionary<string, object> p)
        {
            var imgContainer = new ImageContainer(surface);
            string filePath = GetString(p, "FilePath") ?? GetString(p, "Path") ?? GetString(p, "File");
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                imgContainer.Load(filePath);
            }
            imgContainer.SetFieldValue(FieldType.SHADOW, GetBool(p, "Shadow", false));
            return imgContainer;
        }

        private static IconContainer CreateIcon(ISurface surface, Dictionary<string, object> p)
        {
            var iconContainer = new IconContainer(surface);
            string filePath = GetString(p, "FilePath") ?? GetString(p, "Path");
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                iconContainer.Load(filePath);
            }
            return iconContainer;
        }

        private static CursorContainer CreateCursor(ISurface surface, Dictionary<string, object> p)
        {
            var cursorContainer = new CursorContainer(surface);
            string filePath = GetString(p, "FilePath") ?? GetString(p, "Path");
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                cursorContainer.Load(filePath);
            }
            return cursorContainer;
        }

        private static EmojiContainer CreateEmoji(ISurface surface, Dictionary<string, object> p)
        {
            if (surface is Surface s)
            {
                string emoji = GetString(p, "Emoji") ?? "👍";
                int size = GetInt(p, "Size", 64);
                return new EmojiContainer(s, emoji, size);
            }
            return null;
        }

        private static SvgContainer CreateSvg(ISurface surface, Dictionary<string, object> p)
        {
            string filePath = GetString(p, "FilePath") ?? GetString(p, "Path");
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                using var stream = File.OpenRead(filePath);
                return new SvgContainer(stream, surface);
            }
            string svgXml = GetString(p, "Content") ?? GetString(p, "SvgXml") ?? GetString(p, "Xml");
            if (!string.IsNullOrWhiteSpace(svgXml))
            {
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgXml));
                return new SvgContainer(stream, surface);
            }
            return null;
        }

        private static ObfuscateContainer CreateObfuscate(ISurface surface, Dictionary<string, object> p, string type)
        {
            var obf = new ObfuscateContainer(surface);
            if (string.Equals(type, "Blur", StringComparison.OrdinalIgnoreCase) || string.Equals(GetString(p, "Mode"), "Blur", StringComparison.OrdinalIgnoreCase))
            {
                obf.SetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE, FilterContainer.PreparedFilter.BLUR);
                obf.SetFieldValue(FieldType.BLUR_RADIUS, GetInt(p, "BlurRadius", 10));
            }
            else
            {
                obf.SetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE, FilterContainer.PreparedFilter.PIXELIZE);
                obf.SetFieldValue(FieldType.PIXEL_SIZE, GetInt(p, "PixelSize", 5));
            }
            return obf;
        }

        private static HighlightContainer CreateHighlight(ISurface surface, Dictionary<string, object> p, string type)
        {
            var hl = new HighlightContainer(surface);
            if (string.Equals(type, "Magnify", StringComparison.OrdinalIgnoreCase) || string.Equals(GetString(p, "Mode"), "Magnify", StringComparison.OrdinalIgnoreCase))
            {
                hl.SetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT, FilterContainer.PreparedFilter.MAGNIFICATION);
                hl.SetFieldValue(FieldType.MAGNIFICATION_FACTOR, GetInt(p, "MagnificationFactor", 2));
            }
            else
            {
                hl.SetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT, FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT);
                hl.SetFieldValue(FieldType.FILL_COLOR, GetColor(p, "FillColor", GetColor(p, "HighlightColor", Color.Yellow)));
            }
            return hl;
        }

        #endregion

        #region Positioning & Alignment

        private static void ApplyPositioning(
            DrawableContainer container,
            ISurface surface,
            Dictionary<string, object> p,
            Dictionary<string, object> extraVariables)
        {
            int surfaceWidth = surface.Image?.Width ?? 0;
            int surfaceHeight = surface.Image?.Height ?? 0;

            // Resolve Width & Height
            if (p.TryGetValue("Width", out var wVal) && wVal != null)
            {
                int w = Convert.ToInt32(wVal);
                if (w > 0) container.Width = w;
            }
            if (p.TryGetValue("Height", out var hVal) && hVal != null)
            {
                int h = Convert.ToInt32(hVal);
                if (h > 0) container.Height = h;
            }

            int elemWidth = container.Width;
            int elemHeight = container.Height;

            int offsetX = GetInt(p, "OffsetX", 0);
            int offsetY = GetInt(p, "OffsetY", 0);
            int marginX = GetInt(p, "MarginX", GetInt(p, "Margin", 10));
            int marginY = GetInt(p, "MarginY", GetInt(p, "Margin", 10));
            int marginLeft = GetInt(p, "MarginLeft", marginX);
            int marginRight = GetInt(p, "MarginRight", marginX);
            int marginTop = GetInt(p, "MarginTop", marginY);
            int marginBottom = GetInt(p, "MarginBottom", marginY);

            // Horizontal anchor & coordinate resolution
            string hAnchor = GetString(p, "HorizontalAnchor")
                ?? GetString(p, "HorizontalAlignment")
                ?? GetString(p, "AnchorH")
                ?? GetString(p, "AlignH")
                ?? GetString(p, "Align")
                ?? GetString(p, "Anchor");

            bool hasExplicitLeft = p.ContainsKey("Left") || p.ContainsKey("left") || p.ContainsKey("X") || p.ContainsKey("x");
            bool hasExplicitRight = p.ContainsKey("Right") || p.ContainsKey("right");

            int posX;
            if (string.Equals(hAnchor, "Right", StringComparison.OrdinalIgnoreCase))
            {
                int rightVal = hasExplicitRight ? GetInt(p, "Right", GetInt(p, "right", 0)) : (hasExplicitLeft ? 0 : marginRight);
                posX = surfaceWidth - elemWidth - rightVal + offsetX;
            }
            else if (string.Equals(hAnchor, "Center", StringComparison.OrdinalIgnoreCase) || string.Equals(hAnchor, "Middle", StringComparison.OrdinalIgnoreCase))
            {
                posX = (surfaceWidth - elemWidth) / 2 + offsetX;
            }
            else // "Left" or unspecified
            {
                if (hasExplicitLeft)
                {
                    int leftVal = GetInt(p, "Left", GetInt(p, "left", GetInt(p, "X", GetInt(p, "x", 0))));
                    posX = leftVal + offsetX;
                }
                else if (hasExplicitRight)
                {
                    int rightVal = GetInt(p, "Right", GetInt(p, "right", 0));
                    posX = surfaceWidth - elemWidth - rightVal + offsetX;
                }
                else
                {
                    posX = (string.Equals(hAnchor, "Left", StringComparison.OrdinalIgnoreCase) ? marginLeft : 0) + offsetX;
                }
            }

            // Vertical anchor & coordinate resolution
            string vAnchor = GetString(p, "VerticalAnchor")
                ?? GetString(p, "VerticalAlignment")
                ?? GetString(p, "AnchorV")
                ?? GetString(p, "AlignV")
                ?? GetString(p, "VAlign");

            bool hasExplicitTop = p.ContainsKey("Top") || p.ContainsKey("top") || p.ContainsKey("Y") || p.ContainsKey("y");
            bool hasExplicitBottom = p.ContainsKey("Bottom") || p.ContainsKey("bottom");

            int posY;
            if (string.Equals(vAnchor, "Bottom", StringComparison.OrdinalIgnoreCase))
            {
                if (hasExplicitBottom)
                {
                    int bottomVal = GetInt(p, "Bottom", GetInt(p, "bottom", 0));
                    posY = surfaceHeight - elemHeight - bottomVal + offsetY;
                }
                else if (hasExplicitTop)
                {
                    // e.g. top was explicitly calculated like "top": "payload.height - 50"
                    int topVal = GetInt(p, "Top", GetInt(p, "top", GetInt(p, "Y", GetInt(p, "y", 0))));
                    posY = topVal + offsetY;
                }
                else
                {
                    posY = surfaceHeight - elemHeight - marginBottom + offsetY;
                }
            }
            else if (string.Equals(vAnchor, "Center", StringComparison.OrdinalIgnoreCase) || string.Equals(vAnchor, "Middle", StringComparison.OrdinalIgnoreCase))
            {
                posY = (surfaceHeight - elemHeight) / 2 + offsetY;
            }
            else // "Top" or unspecified
            {
                if (hasExplicitTop)
                {
                    int topVal = GetInt(p, "Top", GetInt(p, "top", GetInt(p, "Y", GetInt(p, "y", 0))));
                    posY = topVal + offsetY;
                }
                else if (hasExplicitBottom)
                {
                    int bottomVal = GetInt(p, "Bottom", GetInt(p, "bottom", 0));
                    posY = surfaceHeight - elemHeight - bottomVal + offsetY;
                }
                else
                {
                    posY = (string.Equals(vAnchor, "Top", StringComparison.OrdinalIgnoreCase) ? marginTop : 0) + offsetY;
                }
            }

            container.Left = posX;
            container.Top = posY;
        }

        #endregion

        #region Helper Parsers

        private static string GetString(Dictionary<string, object> p, string key)
        {
            if (p != null && p.TryGetValue(key, out var val) && val != null)
            {
                return val.ToString();
            }
            return null;
        }

        private static int GetInt(Dictionary<string, object> p, string key, int defaultValue = 0)
        {
            if (p != null && p.TryGetValue(key, out var val) && val != null)
            {
                if (int.TryParse(val.ToString(), out int i)) return i;
                if (double.TryParse(val.ToString(), out double d)) return (int)Math.Round(d);
            }
            return defaultValue;
        }

        private static double GetDouble(Dictionary<string, object> p, string key, double defaultValue = 0.0)
        {
            if (p != null && p.TryGetValue(key, out var val) && val != null)
            {
                if (double.TryParse(val.ToString(), out double d)) return d;
            }
            return defaultValue;
        }

        private static bool GetBool(Dictionary<string, object> p, string key, bool defaultValue = false)
        {
            if (p != null && p.TryGetValue(key, out var val) && val != null)
            {
                if (bool.TryParse(val.ToString(), out bool b)) return b;
            }
            return defaultValue;
        }

        private static Color GetColor(Dictionary<string, object> p, string key, Color fallback)
        {
            if (p != null && p.TryGetValue(key, out var val) && val != null)
            {
                if (val is Color c) return c;
                string s = val.ToString();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    try
                    {
                        return ColorTranslator.FromHtml(s);
                    }
                    catch
                    {
                        var named = Color.FromName(s);
                        return named.IsKnownColor ? named : fallback;
                    }
                }
            }
            return fallback;
        }

        #endregion
    }
}
