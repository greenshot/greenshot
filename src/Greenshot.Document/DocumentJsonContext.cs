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
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Greenshot.Document
{
    /// <summary>
    /// The code which writes and reads the JSON of a document, made by the compiler for the types listed here. Nothing
    /// looks at the types while running (no reflection), so a program which is compiled ahead and trimmed (native AOT)
    /// reads and writes documents too. Every type an element or the document can hold is found from the listed ones;
    /// the element types are listed one by one, as an element is only known as Element where it is used.
    /// </summary>
    [JsonSerializable(typeof(GreenshotPackage.DocumentJson))]
    [JsonSerializable(typeof(DocumentMetadata))]
    [JsonSerializable(typeof(Dictionary<System.Guid, ImageAnalysis>))]
    [JsonSerializable(typeof(List<Element>))]
    [JsonSerializable(typeof(RectD))]
    [JsonSerializable(typeof(StepNumbering))]
    [JsonSerializable(typeof(RectangleElement))]
    [JsonSerializable(typeof(EllipseElement))]
    [JsonSerializable(typeof(LineElement))]
    [JsonSerializable(typeof(ArrowElement))]
    [JsonSerializable(typeof(FreehandElement))]
    [JsonSerializable(typeof(TextElement))]
    [JsonSerializable(typeof(SpeechBubbleElement))]
    [JsonSerializable(typeof(StepLabelElement))]
    [JsonSerializable(typeof(HighlightElement))]
    [JsonSerializable(typeof(ObfuscateElement))]
    [JsonSerializable(typeof(ImageElement))]
    [JsonSerializable(typeof(EmojiElement))]
    [JsonSerializable(typeof(CursorElement))]
    [JsonSerializable(typeof(CutMarkElement))]
    [JsonSerializable(typeof(FrameElement))]
    [JsonSerializable(typeof(PluginElement))]
    internal sealed partial class DocumentJsonContext : JsonSerializerContext
    {
        /// <summary>
        /// Every enum of the document is written as its name in camelCase. One converter per enum: the converter for all
        /// enums makes its code while running. A new enum of the document is added here, else it is written as a number.
        /// </summary>
        public static IEnumerable<JsonConverter> EnumConverters()
        {
            var names = JsonNamingPolicy.CamelCase;
            yield return new JsonStringEnumConverter<AnchorLine>(names);
            yield return new JsonStringEnumConverter<LabelPlace>(names);
            yield return new JsonStringEnumConverter<LabelOrientation>(names);
            yield return new JsonStringEnumConverter<ArrowHeads>(names);
            yield return new JsonStringEnumConverter<HighlightMode>(names);
            yield return new JsonStringEnumConverter<ObfuscateMode>(names);
            yield return new JsonStringEnumConverter<ImageScaling>(names);
            yield return new JsonStringEnumConverter<EdgeStyle>(names);
            yield return new JsonStringEnumConverter<Edges>(names);
            yield return new JsonStringEnumConverter<FramePlacement>(names);
            yield return new JsonStringEnumConverter<CanvasFit>(names);
            yield return new JsonStringEnumConverter<StepFormat>(names);
            yield return new JsonStringEnumConverter<LayerKind>(names);
            yield return new JsonStringEnumConverter<StrokePosition>(names);
            yield return new JsonStringEnumConverter<TextAlignment>(names);
        }
    }
}
