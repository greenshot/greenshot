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

using System.Collections.Generic;

namespace Greenshot.FileFormat.Legacy
{
    /// <summary>
    /// The kind of a drawable container stored in a legacy .greenshot or .gst file.
    /// </summary>
    public enum LegacyElementKind
    {
        /// <summary>A type this reader doesn't know, e.g. one from a plugin. Only the common values are filled.</summary>
        Unknown,
        Rectangle,
        Ellipse,
        Line,
        Arrow,
        Text,
        Speechbubble,
        StepLabel,
        Freehand,
        Highlight,
        Obfuscate,
        Image,
        Icon,
        Metafile,
        Svg,
        Emoji,
        Cursor
    }

    /// <summary>
    /// One drawable container (element) as it was stored by Greenshot 1.2 - 1.4.
    /// Elements without extra data (rectangle, ellipse, line, arrow, highlight, obfuscate) use this class directly,
    /// the others use one of the derived classes.
    /// </summary>
    public class LegacyElement
    {
        /// <summary>The kind of element</summary>
        public LegacyElementKind Kind { get; internal set; }

        /// <summary>The full type name as stored in the file, e.g. "Greenshot.Drawing.RectangleContainer" (1.2) or "Greenshot.Editor.Drawing.RectangleContainer" (1.3)</summary>
        public string TypeName { get; internal set; }

        public int Left { get; internal set; }
        public int Top { get; internal set; }
        public int Width { get; internal set; }
        public int Height { get; internal set; }

        /// <summary>The fields (line thickness, colors, fonts...) of the element itself</summary>
        public IReadOnlyList<LegacyField> Fields { get; internal set; } = new List<LegacyField>();

        /// <summary>The filters (children) of a highlight or obfuscate element</summary>
        public IReadOnlyList<LegacyFilter> Filters { get; internal set; } = new List<LegacyFilter>();

        public override string ToString() => $"{Kind} ({Left},{Top},{Width}x{Height})";
    }

    /// <summary>A text element</summary>
    public class LegacyTextElement : LegacyElement
    {
        public string Text { get; internal set; }
    }

    /// <summary>A speech bubble, a text element with a target (the tail)</summary>
    public sealed class LegacySpeechbubbleElement : LegacyTextElement
    {
        /// <summary>Location of the tail target, absolute surface coordinates</summary>
        public LegacyPoint TargetGripperLocation { get; internal set; }
    }

    /// <summary>A step label (counter)</summary>
    public sealed class LegacyStepLabelElement : LegacyElement
    {
        /// <summary>The number of this label, used to restore the order of the labels</summary>
        public int Number { get; internal set; }

        /// <summary>The counter start of the surface when it was saved</summary>
        public int CounterStart { get; internal set; } = 1;
    }

    /// <summary>A freehand line</summary>
    public sealed class LegacyFreehandElement : LegacyElement
    {
        public IReadOnlyList<LegacyPoint> Points { get; internal set; } = new List<LegacyPoint>();
    }

    /// <summary>An image element</summary>
    public sealed class LegacyImageElement : LegacyElement
    {
        /// <summary>The encoded image as System.Drawing stored it (normally PNG), null if the file had none</summary>
        public byte[] ImageData { get; internal set; }
    }

    /// <summary>An icon element (e.g. the captured mouse cursor)</summary>
    public sealed class LegacyIconElement : LegacyElement
    {
        /// <summary>The icon in .ico format, null if the file had none</summary>
        public byte[] IconData { get; internal set; }
    }

    /// <summary>Base for the elements which can be rotated</summary>
    public abstract class LegacyVectorGraphicsElement : LegacyElement
    {
        /// <summary>Rotation in degrees</summary>
        public int RotationAngle { get; internal set; }
    }

    /// <summary>A metafile element (.emf/.wmf)</summary>
    public sealed class LegacyMetafileElement : LegacyVectorGraphicsElement
    {
        /// <summary>
        /// The image data as System.Drawing stored it. GDI+ has no metafile encoder, so this is normally a PNG of the
        /// rendered metafile, not the metafile itself.
        /// </summary>
        public byte[] ImageData { get; internal set; }
    }

    /// <summary>An SVG element</summary>
    public sealed class LegacySvgElement : LegacyVectorGraphicsElement
    {
        /// <summary>The SVG document</summary>
        public byte[] SvgData { get; internal set; }
    }

    /// <summary>An emoji element (1.4)</summary>
    public sealed class LegacyEmojiElement : LegacyVectorGraphicsElement
    {
        public string Emoji { get; internal set; }
    }

    /// <summary>A mouse cursor element (1.4)</summary>
    public sealed class LegacyCursorElement : LegacyElement
    {
        /// <summary>The color layer of the cursor, encoded (normally PNG), can be null</summary>
        public byte[] ColorLayerData { get; internal set; }

        /// <summary>The mask layer of the cursor, encoded (normally PNG), can be null</summary>
        public byte[] MaskLayerData { get; internal set; }

        public int CursorWidth { get; internal set; }
        public int CursorHeight { get; internal set; }
        public int HotspotX { get; internal set; }
        public int HotspotY { get; internal set; }
    }

    /// <summary>A filter of a highlight or obfuscate element</summary>
    public sealed class LegacyFilter
    {
        /// <summary>The type name without namespace, e.g. "BlurFilter", "HighlightFilter"</summary>
        public string Name { get; internal set; }

        public bool Invert { get; internal set; }

        public IReadOnlyList<LegacyField> Fields { get; internal set; } = new List<LegacyField>();

        public override string ToString() => Name;
    }
}
