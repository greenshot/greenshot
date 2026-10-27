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

using System.Globalization;
using Greenshot.Linux.Core.Barcodes;
using Greenshot.Linux.Editor;
using Greenshot.Linux.Editor.Effects;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Fields;
using Greenshot.Linux.Editor.Rendering;
using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Expressions;
using Greenshot.Linux.Recipes.Pipeline;
using SkiaSharp;

namespace Greenshot.Linux.Recipes.Steps
{
    [StepInfo(WellKnownStepTypes.Annotation, "Add Annotation", "Adds annotations (shapes, text, arrows, icons, blur, stamps) to the capture. The properties of an annotation depend on its type.", "Annotations")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required, VisualMutation = PayloadEffect.AddsAnnotations)]
    [StepParameter("Annotations", ContractDataType.Object, ValueType = typeof(List<Dictionary<string, object>>), Required = true, SupportsExpressions = false,
        Description = "List of annotations: each has a Type (Text, Rectangle, Arrow, ...) and the properties of that type (see AnnotationProperties)")]
    public class AnnotationStep : ICaptureStep, IEvaluatesOwnParameters
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AnnotationStep));
        private static readonly object InitLock = new object();
        private static bool _builtInsRegistered;

        /// <summary>
        /// The marker of a Crop annotation: the surface is cropped to it after placing
        /// </summary>
        private sealed class CropMarker : RectangleElement;

        public string Name { get; }

        public RecipeNodeConfig NodeConfig { get; }

        public AnnotationStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.Id ?? WellKnownStepTypes.Annotation;
        }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var payload = context.Payload;
            var surface = payload?.EnsureSurface();
            if (surface?.Image == null)
            {
                context.LogStep("AnnotationStep skipped: surface image is not available.");
                return Task.CompletedTask;
            }

            int surfaceWidth = surface.Width;
            int surfaceHeight = surface.Height;
            var extraVariables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["payload.width"] = surfaceWidth,
                ["payload.height"] = surfaceHeight,
                ["surface.width"] = surfaceWidth,
                ["surface.height"] = surfaceHeight,
                ["width"] = surfaceWidth,
                ["w"] = surfaceWidth,
                ["height"] = surfaceHeight,
                ["h"] = surfaceHeight,
            };

            int added = 0;
            foreach (var rawParameters in NodeConfig.GetParameter<List<Dictionary<string, object>>>("Annotations") ?? new List<Dictionary<string, object>>())
            {
                var parameters = ExpressionEvaluator.Instance.ResolveParameters(rawParameters, context, extraVariables);
                var element = CreateDrawable(surface, parameters, context);
                if (element is CropMarker crop)
                {
                    var area = SKRectI.Round(crop.NormalizedBounds);
                    area.Intersect(SKRectI.Create(surface.Width, surface.Height));
                    if (!area.IsEmpty)
                    {
                        var result = ImageEffects.Crop(surface.Image, area);
                        surface.ReplaceImage(result.Image, result.ElementTransform);
                    }
                }
                else if (element != null)
                {
                    surface.Add(element, select: false);
                    added++;
                }
            }

            if (added > 0 || surface.Width != surfaceWidth || surface.Height != surfaceHeight)
            {
                surface.IsModified = true;
                // The surface changed: renders cached for the destinations are outdated
                payload.InvalidateExportSource();
                context.LogStep($"AnnotationStep added {added} element(s) to surface.");
                Log.InfoFormat("AnnotationStep '{0}' placed {1} element(s) on surface ({2}x{3})", Name, added, surfaceWidth, surfaceHeight);
            }

            return Task.CompletedTask;
        }

        public static void EnsureBuiltInDrawablesRegistered()
        {
            if (_builtInsRegistered) return;
            lock (InitLock)
            {
                if (_builtInsRegistered) return;
                RegisterBuiltInDrawables(RecipeDrawableRegistry.Instance);
                _builtInsRegistered = true;
            }
        }

        /// <summary>
        /// The annotation types; QRCode and Barcode are of the ZXing plugin on Windows, built in on Linux
        /// </summary>
        public static void RegisterBuiltInDrawables(RecipeDrawableRegistry registry)
        {
            registry.RegisterDrawableFactory("Rectangle", (s, p, c) => Shape(new RectangleElement(), p, SKColors.Red, SKColors.Transparent, true));
            registry.RegisterDrawableFactory("Ellipse", (s, p, c) => Shape(new EllipseElement(), p, SKColors.Red, SKColors.Transparent, true));
            registry.RegisterDrawableFactory("Line", (s, p, c) => Shape(new LineElement(), p, SKColors.Red, null, true));
            registry.RegisterDrawableFactory("Arrow", (s, p, c) => CreateArrow(p));
            registry.RegisterDrawableFactory("Freehand", (s, p, c) => CreateFreehand(p));
            registry.RegisterDrawableFactory("Text", (s, p, c) => CreateText(p));
            registry.RegisterDrawableFactory("Speechbubble", (s, p, c) => CreateSpeechbubble(p));
            registry.RegisterDrawableFactory("StepLabel", (s, p, c) => CreateStepLabel(s, p));
            registry.RegisterDrawableFactory("Image", (s, p, c) => CreateImage(p));
            registry.RegisterDrawableFactory("Icon", (s, p, c) => CreateImage(p));
            registry.RegisterDrawableFactory("Cursor", (s, p, c) => CreateCursor(p));
            registry.RegisterDrawableFactory("Emoji", (s, p, c) => CreateEmoji(p));
            registry.RegisterDrawableFactory("Blur", (s, p, c) => CreateObfuscate(p, ObfuscateFilter.Blur));
            registry.RegisterDrawableFactory("Pixelize", (s, p, c) => CreateObfuscate(p, ObfuscateFilter.Pixelize));
            registry.RegisterDrawableFactory("Highlight", (s, p, c) => CreateHighlight(p, HighlightFilter.TextHighlight));
            registry.RegisterDrawableFactory("Magnify", (s, p, c) => CreateHighlight(p, HighlightFilter.Magnification));
            registry.RegisterDrawableFactory("Crop", (s, p, c) => new CropMarker());
            registry.RegisterDrawableFactory("QRCode", (s, p, c) => CreateCode(p), CodeProperties);
            registry.RegisterDrawableFactory("Barcode", (s, p, c) => CreateCode(p), CodeProperties);
        }

        private static DrawableElement CreateDrawable(EditorSurface surface, Dictionary<string, object> parameters, CaptureFlowContext context)
        {
            EnsureBuiltInDrawablesRegistered();
            string type = GetString(parameters, "Type") ?? "Rectangle";
            var element = RecipeDrawableRegistry.Instance.CreateDrawable(type, surface, parameters, context);
            if (element == null)
            {
                Log.WarnFormat("Unknown drawable type '{0}'. Defaulting to a rectangle.", type);
                element = Shape(new RectangleElement(), parameters, SKColors.Red, SKColors.Transparent, true);
            }

            bool rational = type is "StepLabel" or "Emoji" or "QRCode";
            ApplyPositioning(element, surface, parameters, rational);
            if (element is SpeechBubbleElement bubble)
            {
                ApplySpeechbubbleTail(bubble, parameters);
            }

            return element;
        }

        #region Creators

        private static T WithStyle<T>(T element) where T : DrawableElement
        {
            element.Style = element.CreateDefaultStyle();
            return element;
        }

        private static T Shape<T>(T element, Dictionary<string, object> p, SKColor line, SKColor? fill, bool shadow) where T : DrawableElement
        {
            WithStyle(element);
            element.Style.LineThickness = GetInt(p, "LineThickness", 2);
            element.Style.LineColor = GetColor(p, "LineColor", line);
            if (fill.HasValue)
            {
                element.Style.FillColor = GetColor(p, "FillColor", fill.Value);
            }

            element.Style.Shadow = GetBool(p, "Shadow", shadow);
            return element;
        }

        private static ArrowElement CreateArrow(Dictionary<string, object> p)
        {
            var arrow = Shape(new ArrowElement(), p, SKColors.Red, null, true);
            arrow.Style.ArrowHeads = (GetString(p, "ArrowHeads") ?? "END_POINT").Replace("_", string.Empty).ToUpperInvariant() switch
            {
                "NONE" => ArrowHeads.None,
                "STARTPOINT" or "START" => ArrowHeads.Start,
                "BOTH" => ArrowHeads.Both,
                _ => ArrowHeads.End,
            };
            return arrow;
        }

        private static FreehandElement CreateFreehand(Dictionary<string, object> p)
        {
            var freehand = Shape(new FreehandElement(), p, SKColors.Red, null, false);
            foreach (var pair in (GetString(p, "Points") ?? string.Empty).Split([';', '|'], StringSplitOptions.RemoveEmptyEntries))
            {
                var coordinates = pair.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);
                if (coordinates.Length == 2 && float.TryParse(coordinates[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                    && float.TryParse(coordinates[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                {
                    freehand.Points.Add(new SKPoint(x, y));
                }
            }

            if (freehand.Points.Count > 0)
            {
                float left = freehand.Points.Min(pt => pt.X), top = freehand.Points.Min(pt => pt.Y);
                freehand.Bounds = new SKRect(left, top, Math.Max(left + 1, freehand.Points.Max(pt => pt.X)), Math.Max(top + 1, freehand.Points.Max(pt => pt.Y)));
            }

            return freehand;
        }

        private static void ApplyFont(TextElement text, Dictionary<string, object> p, float size, bool bold)
        {
            text.Text = GetString(p, "Text") ?? string.Empty;
            text.Style.FontFamily = GetString(p, "FontFamily") ?? "Sans";
            text.Style.FontSize = (float)GetDouble(p, "FontSize", size);
            text.Style.FontBold = GetBool(p, "Bold", bold);
            text.Style.FontItalic = GetBool(p, "Italic", false);
        }

        private static TextElement CreateText(Dictionary<string, object> p)
        {
            var text = WithStyle(new TextElement());
            ApplyFont(text, p, 12, false);
            text.Style.LineColor = GetColor(p, "LineColor", SKColors.Red);
            text.Style.FillColor = GetColor(p, "FillColor", SKColors.Transparent);
            text.Style.LineThickness = GetInt(p, "LineThickness", 0);
            text.Style.Shadow = GetBool(p, "Shadow", true);
            text.Style.TextHorizontalAlignment = (GetString(p, "TextAlign") ?? "Center").ToLowerInvariant() switch
            {
                "left" => TextHorizontalAlignment.Left,
                "right" => TextHorizontalAlignment.Right,
                _ => TextHorizontalAlignment.Center,
            };
            var size = text.DefaultSize ?? new SKSize(150, 40);
            if (GetBool(p, "FitToText", true) && !p.ContainsKey("Width"))
            {
                size = MeasureText(text);
            }

            text.Bounds = SKRect.Create(size);
            return text;
        }

        /// <summary>
        /// The size the text needs, FitToText of the Windows text container
        /// </summary>
        private static SKSize MeasureText(TextElement text)
        {
            using var font = TextLayout.CreateFont(text.Style);
            var lines = text.Text.Replace("\r\n", "\n").Split('\n');
            float width = lines.Max(line => font.MeasureText(line));
            float height = font.Spacing * lines.Length;
            float padding = Math.Max(4, text.Style.LineThickness) * 2;
            return new SKSize((float)Math.Ceiling(width + padding), (float)Math.Ceiling(height + padding));
        }

        private static SpeechBubbleElement CreateSpeechbubble(Dictionary<string, object> p)
        {
            var bubble = WithStyle(new SpeechBubbleElement());
            ApplyFont(bubble, p, 14, true);
            bubble.Style.LineColor = GetColor(p, "LineColor", SKColors.Blue);
            bubble.Style.FillColor = GetColor(p, "FillColor", SKColors.White);
            bubble.Style.LineThickness = GetInt(p, "LineThickness", 2);
            bubble.Style.Shadow = GetBool(p, "Shadow", false);
            bubble.Bounds = SKRect.Create(bubble.DefaultSize ?? new SKSize(200, 100));
            return bubble;
        }

        private static StepLabelElement CreateStepLabel(EditorSurface surface, Dictionary<string, object> p)
        {
            var label = WithStyle(new StepLabelElement());
            label.Style.FillColor = GetColor(p, "FillColor", new SKColor(0x8B, 0x00, 0x00));
            label.Style.LineColor = GetColor(p, "LineColor", SKColors.White);
            label.Style.Shadow = GetBool(p, "Shadow", false);
            label.Bounds = SKRect.Create(30, 30);
            if (p.ContainsKey("Number"))
            {
                // The labels count from CounterStart in their order, this one gets the number
                int before = surface.Elements.OfType<StepLabelElement>().Count();
                surface.CounterStart = GetInt(p, "Number", 1) - before;
            }

            return label;
        }

        private static byte[] ReadBytes(Dictionary<string, object> p)
        {
            string data = GetString(p, "ImageData");
            string path = GetString(p, "FilePath");
            try
            {
                if (!string.IsNullOrWhiteSpace(data))
                {
                    return Convert.FromBase64String(data);
                }

                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    return File.ReadAllBytes(path);
                }
            }
            catch (Exception ex) when (ex is FormatException or IOException or UnauthorizedAccessException)
            {
                Log.Warn("Failed to read the image of an annotation: " + ex.Message);
            }

            return null;
        }

        private static DrawableElement ImageOf(SKBitmap bitmap, Dictionary<string, object> p)
        {
            if (bitmap == null)
            {
                return null;
            }

            using (bitmap)
            {
                var image = ImageElement.FromBitmap(bitmap, SKPoint.Empty);
                image.Style = image.CreateDefaultStyle();
                image.Style.Shadow = GetBool(p, "Shadow", false);
                return image;
            }
        }

        private static DrawableElement CreateImage(Dictionary<string, object> p)
        {
            var bytes = ReadBytes(p);
            // An SVG stays sharp: drawn in the size of the annotation
            return bytes == null ? null : ImageOf(ImageSources.Decode(bytes, GetInt(p, "Width", GetInt(p, "Size", 0)), GetInt(p, "Height", GetInt(p, "Size", 0))), p);
        }

        private static DrawableElement CreateCursor(Dictionary<string, object> p)
        {
            var bytes = ReadBytes(p);
            if (bytes != null)
            {
                return ImageOf(ImageSources.Decode(bytes), p);
            }

            // The cursors of the desktop's theme for the names of the Windows cursors
            int size = Math.Max(GetInt(p, "Width", 0), GetInt(p, "Size", 32));
            var cursor = ImageSources.LoadCursor(GetString(p, "CursorName"), size);
            return ImageOf(cursor?.Image ?? ImageSources.DrawArrowCursor(size), p);
        }

        private static DrawableElement CreateEmoji(Dictionary<string, object> p)
        {
            int size = GetInt(p, "Size", 64);
            return ImageOf(ImageSources.RenderEmoji(GetString(p, "Emoji") ?? "👍", Math.Max(size, 64) * 2), p);
        }

        private static ObfuscateElement CreateObfuscate(Dictionary<string, object> p, ObfuscateFilter filter)
        {
            var element = WithStyle(new ObfuscateElement());
            element.Style.ObfuscateFilter = filter;
            element.Style.BlurRadius = GetInt(p, "BlurRadius", 10);
            element.Style.PixelSize = GetInt(p, "PixelSize", 5);
            element.Bounds = SKRect.Create(100, 50);
            return element;
        }

        private static HighlightElement CreateHighlight(Dictionary<string, object> p, HighlightFilter filter)
        {
            var element = WithStyle(new HighlightElement());
            element.Style.HighlightFilter = filter;
            element.Style.FillColor = GetColor(p, "FillColor", SKColors.Yellow);
            element.Style.MagnificationFactor = GetInt(p, "MagnificationFactor", 2);
            element.Bounds = SKRect.Create(100, 50);
            return element;
        }

        private static readonly IReadOnlyList<ParameterContract> CodeProperties = new[]
        {
            new ParameterContract("QrType", description: "Text, Link, WiFi, BusinessCard, Payment, Email, CalendarEvent, Phone, Sms or Geo"),
            new ParameterContract("Text", description: "The text or link (QrType Text or Link)"),
            new ParameterContract("Format", description: "The barcode format, e.g. QR_CODE, CODE_128"),
            new ParameterContract("ForeColor", ContractDataType.Color, description: "Color of the code"),
            new ParameterContract("BackColor", ContractDataType.Color, description: "Background color"),
            new ParameterContract("RoundedDots", ContractDataType.Boolean, description: "Rounded dots"),
            new ParameterContract("QuietZone", ContractDataType.Integer, description: "Empty border around the code, in modules"),
            Payload("WifiSsid"), Payload("WifiPassword"), Payload("WifiEncryption"),
            Payload("VcardFirstName"), Payload("VcardLastName"), Payload("VcardCompany"), Payload("VcardEmail"), Payload("VcardPhone"), Payload("VcardUrl"),
            Payload("EpcName"), Payload("EpcIban"), Payload("EpcBic"), Payload("EpcAmount"), Payload("EpcReference"), Payload("EpcMessage"),
            Payload("EmailTo"), Payload("EmailSubject"), Payload("EmailBody"),
            Payload("EventTitle"), Payload("EventLocation"), Payload("EventStart"), Payload("EventEnd"), Payload("EventDescription"),
            Payload("PhoneNumber"), Payload("SmsNumber"), Payload("SmsMessage"), Payload("Latitude"), Payload("Longitude"),
        };

        private static ParameterContract Payload(string name) => new ParameterContract(name, description: "Content of the code for its QrType");

        /// <summary>
        /// A QR code or barcode, like the BarcodeContainer of the ZXing plugin
        /// </summary>
        private static DrawableElement CreateCode(Dictionary<string, object> p)
        {
            string content = QrPayloads.Create(GetString(p, "QrType"), key => GetString(p, key));
            if (string.IsNullOrEmpty(content))
            {
                content = "https://getgreenshot.org";
            }

            var format = Barcodes.ParseFormat(GetString(p, "Format"));
            bool is2D = Barcodes.Is2D(format);
            int size = GetInt(p, "Size", 0);
            int width = size > 0 ? size : GetInt(p, "Width", is2D ? 160 : 250);
            int height = size > 0 ? size : GetInt(p, "Height", is2D ? 160 : 80);
            if (is2D)
            {
                width = height = Math.Min(width, height);
            }

            try
            {
                return ImageOf(Barcodes.Generate(content, format, GetColor(p, "ForeColor", SKColors.Black), GetColor(p, "BackColor", SKColors.White),
                    GetBool(p, "RoundedDots", false), width, height, GetInt(p, "QuietZone", 1)), p);
            }
            catch (Exception ex) when (ex is ArgumentException or ZXing.WriterException)
            {
                Log.Warn($"The code '{content}' can't be made as {format}: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Positioning & Alignment

        /// <summary>
        /// Size and position like the Windows AnnotationStep: Width, Height or Size, the anchors with offsets and margins, or
        /// Left/Top/Right/Bottom; StepLabel, Emoji and QRCode keep their aspect ratio
        /// </summary>
        public static void ApplyPositioning(DrawableElement element, EditorSurface surface, Dictionary<string, object> p, bool rational = false)
        {
            int surfaceWidth = surface.Width;
            int surfaceHeight = surface.Height;
            var current = element.NormalizedBounds;
            float width = current.Width;
            float height = current.Height;

            int explicitSize = GetInt(p, "Size", 0);
            int explicitWidth = GetInt(p, "Width", 0) > 0 ? GetInt(p, "Width", 0) : explicitSize;
            int explicitHeight = GetInt(p, "Height", 0) > 0 ? GetInt(p, "Height", 0) : explicitSize;
            if (GetBool(p, "LockAspectRatio", false) || rational)
            {
                double ratio = width > 0 && height > 0 ? width / height : 1.0;
                if (explicitWidth > 0 && explicitHeight <= 0)
                {
                    width = explicitWidth;
                    height = Math.Max(1, (float)Math.Round(explicitWidth / ratio));
                }
                else if (explicitHeight > 0 && explicitWidth <= 0)
                {
                    height = explicitHeight;
                    width = Math.Max(1, (float)Math.Round(explicitHeight * ratio));
                }
                else if (explicitWidth > 0 && explicitHeight > 0)
                {
                    if (Math.Abs(ratio - 1.0) < 0.001)
                    {
                        width = height = explicitSize > 0 ? explicitSize : Math.Min(explicitWidth, explicitHeight);
                    }
                    else if ((double)explicitWidth / explicitHeight > ratio)
                    {
                        height = explicitHeight;
                        width = Math.Max(1, (float)Math.Round(explicitHeight * ratio));
                    }
                    else
                    {
                        width = explicitWidth;
                        height = Math.Max(1, (float)Math.Round(explicitWidth / ratio));
                    }
                }
            }
            else
            {
                if (explicitWidth > 0) width = explicitWidth;
                if (explicitHeight > 0) height = explicitHeight;
            }

            int offsetX = GetInt(p, "OffsetX", 0);
            int offsetY = GetInt(p, "OffsetY", 0);
            int margin = GetInt(p, "Margin", 10);
            int marginLeft = GetInt(p, "MarginLeft", margin);
            int marginRight = GetInt(p, "MarginRight", margin);
            int marginTop = GetInt(p, "MarginTop", margin);
            int marginBottom = GetInt(p, "MarginBottom", margin);
            bool hasLeft = p.ContainsKey("Left"), hasRight = p.ContainsKey("Right"), hasTop = p.ContainsKey("Top"), hasBottom = p.ContainsKey("Bottom");

            string horizontal = GetString(p, "HorizontalAnchor");
            float x;
            if (string.Equals(horizontal, "Right", StringComparison.OrdinalIgnoreCase))
            {
                int right = hasRight ? GetInt(p, "Right", 0) : hasLeft ? 0 : marginRight;
                x = surfaceWidth - width - right + offsetX;
            }
            else if (string.Equals(horizontal, "Center", StringComparison.OrdinalIgnoreCase))
            {
                x = (surfaceWidth - width) / 2 + offsetX;
            }
            else if (hasLeft)
            {
                x = GetInt(p, "Left", 0) + offsetX;
            }
            else if (hasRight)
            {
                x = surfaceWidth - width - GetInt(p, "Right", 0) + offsetX;
            }
            else
            {
                x = (string.Equals(horizontal, "Left", StringComparison.OrdinalIgnoreCase) ? marginLeft : 0) + offsetX;
            }

            string vertical = GetString(p, "VerticalAnchor");
            float y;
            if (string.Equals(vertical, "Bottom", StringComparison.OrdinalIgnoreCase))
            {
                y = hasBottom ? surfaceHeight - height - GetInt(p, "Bottom", 0) + offsetY
                    : hasTop ? GetInt(p, "Top", 0) + offsetY
                    : surfaceHeight - height - marginBottom + offsetY;
            }
            else if (string.Equals(vertical, "Center", StringComparison.OrdinalIgnoreCase))
            {
                y = (surfaceHeight - height) / 2 + offsetY;
            }
            else if (hasTop)
            {
                y = GetInt(p, "Top", 0) + offsetY;
            }
            else if (hasBottom)
            {
                y = surfaceHeight - height - GetInt(p, "Bottom", 0) + offsetY;
            }
            else
            {
                y = (string.Equals(vertical, "Top", StringComparison.OrdinalIgnoreCase) ? marginTop : 0) + offsetY;
            }

            if (element is FreehandElement freehand && freehand.Points.Count > 0 && !p.ContainsKey("Left") && !p.ContainsKey("Top") && horizontal == null && vertical == null)
            {
                // Points are positions on the capture
                return;
            }

            var bounds = SKRect.Create(x, y, width, height);
            if (element is FreehandElement movedFreehand && movedFreehand.Points.Count > 0)
            {
                var old = movedFreehand.NormalizedBounds;
                float scaleX = old.Width > 0 ? bounds.Width / old.Width : 1, scaleY = old.Height > 0 ? bounds.Height / old.Height : 1;
                movedFreehand.Points = movedFreehand.Points.Select(pt => new SKPoint(bounds.Left + (pt.X - old.Left) * scaleX, bounds.Top + (pt.Y - old.Top) * scaleY)).ToList();
            }

            element.Bounds = bounds;
        }

        private static void ApplySpeechbubbleTail(SpeechBubbleElement bubble, Dictionary<string, object> p)
        {
            var r = bubble.NormalizedBounds;
            SKPoint tail;
            if (p.ContainsKey("TailX") && p.ContainsKey("TailY"))
            {
                tail = new SKPoint(GetInt(p, "TailX", (int)r.Left - 20), GetInt(p, "TailY", (int)r.Bottom + 25));
            }
            else
            {
                tail = (GetString(p, "TailDirection") ?? "BottomLeft").ToLowerInvariant() switch
                {
                    "bottomright" => new SKPoint(r.Right + 15, r.Bottom + 25),
                    "bottomcenter" => new SKPoint(r.MidX, r.Bottom + 25),
                    "topleft" => new SKPoint(r.Left - 15, r.Top - 25),
                    "topright" => new SKPoint(r.Right + 15, r.Top - 25),
                    "topcenter" => new SKPoint(r.MidX, r.Top - 25),
                    "left" => new SKPoint(r.Left - 25, r.MidY),
                    "right" => new SKPoint(r.Right + 25, r.MidY),
                    _ => new SKPoint(r.Left - 15, r.Bottom + 25),
                };
                tail.Offset(GetInt(p, "TailOffsetX", 0), GetInt(p, "TailOffsetY", 0));
            }

            bubble.Target = tail;
        }

        #endregion

        #region Helper Parsers

        private static string GetString(Dictionary<string, object> p, string key) =>
            p != null && p.TryGetValue(key, out var value) && value != null ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

        private static int GetInt(Dictionary<string, object> p, string key, int defaultValue = 0)
        {
            string text = GetString(p, key);
            if (text == null) return defaultValue;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)) return i;
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? (int)Math.Round(d) : defaultValue;
        }

        private static double GetDouble(Dictionary<string, object> p, string key, double defaultValue = 0.0) =>
            double.TryParse(GetString(p, key), NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : defaultValue;

        private static bool GetBool(Dictionary<string, object> p, string key, bool defaultValue = false) =>
            bool.TryParse(GetString(p, key), out bool b) ? b : defaultValue;

        private static SKColor GetColor(Dictionary<string, object> p, string key, SKColor fallback) =>
            p != null && p.TryGetValue(key, out var value) ? RecipeColors.Parse(value, fallback) : fallback;

        #endregion
    }
}
