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


using System;
using System.Collections.Generic;
using System.Linq;
using Greenshot.Base.Recipes.Contracts;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// The properties of the annotations in the "Annotations" of an Annotation node, per annotation type: what the validator
    /// checks (names as declared, values of the declared type) and the recipe editor writes. The built-in types are declared here,
    /// a plugin declares the properties of its types when it registers them with the <see cref="IRecipeDrawableRegistry"/>.
    /// </summary>
    public static class AnnotationProperties
    {
        /// <summary>
        /// The properties of every annotation: its type, size and position
        /// </summary>
        public static IReadOnlyList<ParameterContract> Common { get; } = new[]
        {
            new ParameterContract("Type", ContractDataType.String, required: true, description: "The type of the annotation (Text, Rectangle, Arrow, ...)", supportsExpressions: false),
            Number("Width", "Width in pixels"),
            Number("Height", "Height in pixels"),
            Number("Size", "Width and height in pixels"),
            new ParameterContract("LockAspectRatio", ContractDataType.Boolean, description: "Keep the aspect ratio when resizing"),
            new ParameterContract("HorizontalAnchor", description: "Left, Center or Right of the capture; without it Left and Right (or OffsetX) place it"),
            new ParameterContract("VerticalAnchor", description: "Top, Center or Bottom of the capture; without it Top and Bottom (or OffsetY) place it"),
            Number("OffsetX", "Moves it to the right (negative: to the left) from where the anchor puts it"),
            Number("OffsetY", "Moves it down (negative: up) from where the anchor puts it"),
            Number("Left", "Distance from the left edge of the capture"),
            Number("Right", "Distance from the right edge of the capture"),
            Number("Top", "Distance from the top edge of the capture"),
            Number("Bottom", "Distance from the bottom edge of the capture"),
            Number("Margin", "Distance from the edge of the capture when anchored"),
            Number("MarginLeft", "Margin to the left edge"),
            Number("MarginRight", "Margin to the right edge"),
            Number("MarginTop", "Margin to the top edge"),
            Number("MarginBottom", "Margin to the bottom edge")
        };

        private static readonly ParameterContract LineThickness = Number("LineThickness", "Line thickness in pixels");
        private static readonly ParameterContract LineColor = new ParameterContract("LineColor", ContractDataType.Color, description: "Line (or text) color");
        private static readonly ParameterContract FillColor = new ParameterContract("FillColor", ContractDataType.Color, description: "Fill color");
        private static readonly ParameterContract Shadow = new ParameterContract("Shadow", ContractDataType.Boolean, description: "Draw a shadow");
        private static readonly ParameterContract Text = new ParameterContract("Text", description: "The text");
        private static readonly ParameterContract FontFamily = new ParameterContract("FontFamily", description: "Font name");
        private static readonly ParameterContract FontSize = new ParameterContract("FontSize", ContractDataType.Decimal, description: "Font size in points");
        private static readonly ParameterContract Bold = new ParameterContract("Bold", ContractDataType.Boolean, description: "Bold text");
        private static readonly ParameterContract FilePath = new ParameterContract("FilePath", ContractDataType.FilePath, description: "The file to show");
        private static readonly ParameterContract ImageData = new ParameterContract("ImageData", description: "The file contents, Base64 encoded");

        private static readonly Dictionary<string, IReadOnlyList<ParameterContract>> BuiltIn = new Dictionary<string, IReadOnlyList<ParameterContract>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Rectangle"] = new[] { LineThickness, LineColor, FillColor, Shadow },
            ["Ellipse"] = new[] { LineThickness, LineColor, FillColor, Shadow },
            ["Line"] = new[] { LineThickness, LineColor, Shadow },
            ["Arrow"] = new[] { LineThickness, LineColor, Shadow, new ParameterContract("ArrowHeads", description: "END_POINT, START_POINT, BOTH or NONE") },
            ["Freehand"] = new[] { LineThickness, LineColor, Shadow, new ParameterContract("Points", description: "The points of the line: \"x,y;x,y;...\"") },
            ["Text"] = new[]
            {
                Text, FontFamily, FontSize, Bold, new ParameterContract("Italic", ContractDataType.Boolean, description: "Italic text"),
                new ParameterContract("TextAlign", description: "Left, Center or Right"),
                new ParameterContract("FitToText", ContractDataType.Boolean, description: "Size it to the text when it has no Width"),
                LineColor, LineThickness, FillColor, Shadow
            },
            ["Speechbubble"] = new[]
            {
                Text, FontFamily, FontSize, Bold, LineColor, LineThickness, FillColor, Shadow,
                new ParameterContract("TailDirection", description: "BottomLeft, BottomCenter, BottomRight, TopLeft, TopCenter, TopRight, Left or Right"),
                Number("TailOffsetX", "Moves the tail point to the right"), Number("TailOffsetY", "Moves the tail point down"),
                Number("TailX", "The tail point, with TailY"), Number("TailY", "The tail point, with TailX")
            },
            ["StepLabel"] = new[] { Number("Number", "The number shown"), FillColor, LineColor, Shadow },
            ["Image"] = new[] { FilePath, ImageData, Shadow },
            ["Icon"] = new[] { FilePath },
            ["Cursor"] = new[] { new ParameterContract("CursorName", description: "A Windows cursor: Arrow, Hand, IBeam, ..."), FilePath, ImageData, Number("HotspotX", "The hot spot of the cursor image"), Number("HotspotY", "The hot spot of the cursor image") },
            ["Emoji"] = new[] { new ParameterContract("Emoji", description: "The emoji") },
            ["Svg"] = new[] { new ParameterContract("Content", description: "The SVG"), FilePath, ImageData },
            ["Blur"] = new[] { Number("BlurRadius", "Blur radius") },
            ["Pixelize"] = new[] { Number("PixelSize", "Pixel size") },
            ["Highlight"] = new[] { FillColor },
            ["Magnify"] = new[] { Number("MagnificationFactor", "Magnification factor") },
            ["Crop"] = Array.Empty<ParameterContract>()
        };

        private static ParameterContract Number(string name, string description) => new ParameterContract(name, ContractDataType.Integer, description: description);

        /// <summary>
        /// The annotation types of Greenshot itself
        /// </summary>
        public static IReadOnlyCollection<string> BuiltInTypes => BuiltIn.Keys;

        /// <summary>
        /// True for an annotation type of Greenshot itself
        /// </summary>
        public static bool IsBuiltIn(string annotationType) => annotationType != null && BuiltIn.ContainsKey(annotationType);

        /// <summary>
        /// The properties of a built-in annotation type (with the common ones), null for another type
        /// </summary>
        public static IReadOnlyList<ParameterContract> ForBuiltIn(string annotationType) =>
            annotationType != null && BuiltIn.TryGetValue(annotationType, out var properties) ? WithCommon(properties) : null;

        /// <summary>
        /// The common properties and those of the type; a type's own property replaces a common one with the same name
        /// </summary>
        public static IReadOnlyList<ParameterContract> WithCommon(IEnumerable<ParameterContract> properties)
        {
            var own = properties?.Where(p => p != null).ToList() ?? new List<ParameterContract>();
            return Common.Where(c => !own.Any(o => o.Matches(c.Name))).Concat(own).ToList();
        }
    }
}
