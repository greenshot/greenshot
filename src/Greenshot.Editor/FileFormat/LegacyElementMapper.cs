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
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Icons;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Filters;
using Greenshot.FileFormat.Legacy;
using log4net;

namespace Greenshot.Editor.FileFormat
{
    /// <summary>
    /// Maps the elements read from a Greenshot 1.2 - 1.4 file (<see cref="LegacyElement"/>) to the drawable containers of the editor.
    /// The containers are created like the editor creates them (constructor, fields with defaults), then the stored values are applied.
    /// This is the only place which has to change when the editor classes change, the reader stays as it is.
    /// </summary>
    internal static class LegacyElementMapper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(LegacyElementMapper));

        private static readonly IDictionary<string, Type> EnumTypes = new Dictionary<string, Type>
        {
            { "FieldFlag", typeof(FieldFlag) },
            { "FilterContainer+PreparedFilter", typeof(FilterContainer.PreparedFilter) },
            { "ArrowContainer+ArrowHeadCombination", typeof(ArrowContainer.ArrowHeadCombination) },
            { "StringAlignment", typeof(StringAlignment) }
        };

        /// <summary>
        /// Create the containers for the elements. Elements which can't be restored (unknown types, missing image data) are skipped and logged.
        /// </summary>
        /// <param name="elements">The elements read from the file</param>
        /// <param name="surface">The surface the containers are created for, its image must be set (a freehand line takes its size from it)</param>
        /// <returns>DrawableContainerList with the containers, not yet added to the surface</returns>
        public static DrawableContainerList ToContainers(IEnumerable<LegacyElement> elements, Surface surface)
        {
            if (surface == null) throw new ArgumentNullException(nameof(surface));

            var containers = new DrawableContainerList();
            foreach (var element in elements ?? Enumerable.Empty<LegacyElement>())
            {
                DrawableContainer container = null;
                try
                {
                    container = CreateContainer(element, surface);
                    if (container == null)
                    {
                        continue;
                    }

                    container.Left = element.Left;
                    container.Top = element.Top;
                    container.Width = element.Width;
                    container.Height = element.Height;
                    ApplyFields(container, element.Fields);
                    ApplyFilters(container, element.Filters);
                    if (container is SpeechbubbleContainer speechbubble && element is LegacySpeechbubbleElement legacySpeechbubble)
                    {
                        speechbubble.RestoreTargetGripper(new NativePoint(legacySpeechbubble.TargetGripperLocation.X, legacySpeechbubble.TargetGripperLocation.Y));
                    }

                    containers.Add(container);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Couldn't restore the element {element} ({element.TypeName}) from the file, it is skipped.", ex);
                    container?.Dispose();
                }
            }

            return containers;
        }

        private static DrawableContainer CreateContainer(LegacyElement element, Surface surface)
        {
            switch (element)
            {
                case LegacySpeechbubbleElement speechbubble:
                    var speechbubbleContainer = new SpeechbubbleContainer(surface);
                    speechbubbleContainer.ChangeText(speechbubble.Text, false);
                    return speechbubbleContainer;
                case LegacyTextElement text:
                    var textContainer = new TextContainer(surface);
                    textContainer.ChangeText(text.Text, false);
                    return textContainer;
                case LegacyStepLabelElement stepLabel:
                    return CreateStepLabel(stepLabel, surface);
                case LegacyFreehandElement freehand:
                    var freehandContainer = new FreehandContainer(surface);
                    freehandContainer.RestoreCapturePoints(freehand.Points.Select(p => new Point(p.X, p.Y)));
                    return freehandContainer;
                case LegacyImageElement image:
                    return CreateImage(image, surface);
                case LegacyIconElement icon:
                    return CreateIcon(icon, surface);
                case LegacyMetafileElement metafile:
                    return CreateMetafile(metafile, surface);
                case LegacySvgElement svg:
                    return CreateSvg(svg, surface);
                case LegacyEmojiElement emoji:
                    if (string.IsNullOrEmpty(emoji.Emoji))
                    {
                        Log.Warn("Emoji element without emoji, it is skipped.");
                        return null;
                    }
                    return new EmojiContainer(surface, emoji.Emoji)
                    {
                        RotationAngle = emoji.RotationAngle
                    };
                case LegacyCursorElement cursor:
                    return CreateCursor(cursor, surface);
            }

            switch (element.Kind)
            {
                case LegacyElementKind.Rectangle:
                    return new RectangleContainer(surface);
                case LegacyElementKind.Ellipse:
                    return new EllipseContainer(surface);
                case LegacyElementKind.Line:
                    return new LineContainer(surface);
                case LegacyElementKind.Arrow:
                    return new ArrowContainer(surface);
                case LegacyElementKind.Highlight:
                    return new HighlightContainer(surface);
                case LegacyElementKind.Obfuscate:
                    return new ObfuscateContainer(surface);
                default:
                    Log.Warn($"Element type {element.TypeName} is not supported, it is skipped.");
                    return null;
            }
        }

        private static DrawableContainer CreateStepLabel(LegacyStepLabelElement stepLabel, Surface surface)
        {
            // The constructor adds the label to the step labels of the surface, the number is used to sort them afterward
            var container = new StepLabelContainer(surface)
            {
                Number = stepLabel.Number
            };
            surface.CounterStart = stepLabel.CounterStart;

            // Old files have no thickness and shadow for step labels, these were drawn without
            if (stepLabel.Fields.All(f => f.FieldTypeName != FieldType.LINE_THICKNESS.Name))
            {
                container.SetFieldValue(FieldType.LINE_THICKNESS, 0);
            }

            if (stepLabel.Fields.All(f => f.FieldTypeName != FieldType.SHADOW.Name))
            {
                container.SetFieldValue(FieldType.SHADOW, false);
            }

            return container;
        }

        private static DrawableContainer CreateImage(LegacyImageElement image, Surface surface)
        {
            if (image.ImageData == null)
            {
                Log.Warn("Image element without image data, it is skipped.");
                return null;
            }

            var container = new ImageContainer(surface);
            using var stream = new MemoryStream(image.ImageData);
            using var loadedImage = Image.FromStream(stream);
            // The setter makes a copy
            container.Image = loadedImage;
            return container;
        }

        private static DrawableContainer CreateIcon(LegacyIconElement icon, Surface surface)
        {
            if (icon.IconData == null)
            {
                Log.Warn("Icon element without icon data, it is skipped.");
                return null;
            }

            var container = new IconContainer(surface);
            using var stream = new MemoryStream(icon.IconData);
            using var loadedIcon = new Icon(stream);
            // The setter makes a copy
            container.Icon = loadedIcon;
            return container;
        }

        /// <summary>
        /// GDI+ has no metafile encoder, so the files contain a PNG of the rendered metafile. It is drawn into a new metafile, this keeps
        /// the behavior of a metafile container (rotation, scaling).
        /// </summary>
        private static DrawableContainer CreateMetafile(LegacyMetafileElement metafile, Surface surface)
        {
            if (metafile.ImageData == null)
            {
                Log.Warn("Metafile element without image data, it is skipped.");
                return null;
            }

            using var stream = new MemoryStream(metafile.ImageData);
            using var image = Image.FromStream(stream);
            return new MetafileContainer(CreateMetafile(image), surface)
            {
                RotationAngle = metafile.RotationAngle
            };
        }

        private static Metafile CreateMetafile(Image image)
        {
            using var referenceBitmap = new Bitmap(1, 1);
            using var referenceGraphics = Graphics.FromImage(referenceBitmap);
            IntPtr hdc = referenceGraphics.GetHdc();
            Metafile newMetafile;
            try
            {
                newMetafile = new Metafile(hdc, new RectangleF(0, 0, image.Width, image.Height), MetafileFrameUnit.Pixel, EmfType.EmfPlusDual);
            }
            finally
            {
                referenceGraphics.ReleaseHdc(hdc);
            }

            using var graphics = Graphics.FromImage(newMetafile);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(image, 0, 0, image.Width, image.Height);
            return newMetafile;
        }

        private static DrawableContainer CreateSvg(LegacySvgElement svg, Surface surface)
        {
            if (svg.SvgData == null)
            {
                Log.Warn("SVG element without SVG data, it is skipped.");
                return null;
            }

            using var stream = new MemoryStream(svg.SvgData);
            return new SvgContainer(stream, surface)
            {
                RotationAngle = svg.RotationAngle
            };
        }

        private static DrawableContainer CreateCursor(LegacyCursorElement cursor, Surface surface)
        {
            if (cursor.ColorLayerData == null && cursor.MaskLayerData == null)
            {
                Log.Warn("Cursor element without cursor data, it is skipped.");
                return null;
            }

            return new CursorContainer(surface)
            {
                Cursor = new CapturedCursor
                {
                    ColorLayer = LoadBitmap(cursor.ColorLayerData),
                    MaskLayer = LoadBitmap(cursor.MaskLayerData),
                    Size = new NativeSize(cursor.CursorWidth, cursor.CursorHeight),
                    HotSpot = new NativePoint(cursor.HotspotX, cursor.HotspotY)
                }
            };
        }

        private static Bitmap LoadBitmap(byte[] data)
        {
            if (data == null)
            {
                return null;
            }

            using var stream = new MemoryStream(data);
            using var image = Image.FromStream(stream);
            // A copy which doesn't depend on the stream
            return new Bitmap(image);
        }

        /// <summary>
        /// Match the stored filters to those the container created (from its prepared filter field) and apply their values
        /// </summary>
        private static void ApplyFilters(DrawableContainer container, IReadOnlyList<LegacyFilter> legacyFilters)
        {
            var available = container.Filters.OfType<AbstractFilter>().ToList();
            foreach (var legacyFilter in legacyFilters)
            {
                var filter = available.FirstOrDefault(f => f.GetType().Name == legacyFilter.Name);
                if (filter == null)
                {
                    Log.Debug($"The stored filter {legacyFilter.Name} has no counterpart in {container.GetType().Name}, it is ignored.");
                    continue;
                }

                available.Remove(filter);
                filter.Invert = legacyFilter.Invert;
                ApplyFields(filter, legacyFilter.Fields);
            }
        }

        /// <summary>
        /// Apply the stored values to the fields the field holder has, fields it doesn't have (anymore) are ignored
        /// </summary>
        private static void ApplyFields(AbstractFieldHolder fieldHolder, IEnumerable<LegacyField> legacyFields)
        {
            foreach (var legacyField in legacyFields)
            {
                var fieldType = FieldType.Values.FirstOrDefault(t => t.Name == legacyField.FieldTypeName);
                if (fieldType == null || !fieldHolder.HasField(fieldType))
                {
                    Log.Debug($"The stored field {legacyField.FieldTypeName} has no counterpart in {fieldHolder.GetType().Name}, it is ignored.");
                    continue;
                }

                var field = fieldHolder.GetField(fieldType);
                if (TryConvertValue(legacyField.Value, field.Value, out var value))
                {
                    field.Value = value;
                }
                else
                {
                    Log.Debug($"The stored value {legacyField.Value} of {legacyField.FieldTypeName} can't be used, the default {field.Value} is kept.");
                }
            }
        }

        /// <summary>
        /// Convert a stored value to the type of the current value of the field
        /// </summary>
        internal static bool TryConvertValue(object legacyValue, object currentValue, out object value)
        {
            value = null;
            switch (legacyValue)
            {
                case null:
                case LegacyUnsupportedValue:
                    return false;
                case LegacyColor color:
                    value = ToColor(color);
                    return true;
                case LegacyEnumValue enumValue:
                    var enumType = currentValue?.GetType() is { IsEnum: true } currentType ? currentType : EnumTypes.TryGetValue(enumValue.TypeName, out var knownType) ? knownType : null;
                    if (enumType == null)
                    {
                        return false;
                    }

                    value = Enum.ToObject(enumType, enumValue.Value);
                    return true;
            }

            if (currentValue == null || currentValue.GetType() == legacyValue.GetType())
            {
                value = legacyValue;
                return true;
            }

            try
            {
                if (currentValue is Enum)
                {
                    value = Enum.ToObject(currentValue.GetType(), legacyValue);
                    return true;
                }

                if (currentValue is IConvertible && legacyValue is IConvertible)
                {
                    value = Convert.ChangeType(legacyValue, currentValue.GetType(), CultureInfo.InvariantCulture);
                    return true;
                }
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException)
            {
                // Can't be converted, the default is kept
            }

            return false;
        }

        internal static Color ToColor(LegacyColor color)
        {
            if (color.IsKnownColor)
            {
                return Color.FromKnownColor((KnownColor)color.KnownColor);
            }

            if (!color.IsArgb && color.IsNamedColor && !string.IsNullOrEmpty(color.Name))
            {
                return Color.FromName(color.Name);
            }

            return Color.FromArgb(color.ToArgb());
        }
    }
}
