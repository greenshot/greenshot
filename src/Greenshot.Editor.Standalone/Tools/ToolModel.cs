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
using Greenshot.Document;

namespace Greenshot.Editor.Standalone.Tools
{
    /// <summary>
    /// The tools of the editor, each draws one kind of element (or selects, or crops)
    /// </summary>
    public enum ToolKind
    {
        Select,
        Rectangle,
        Ellipse,
        Line,
        Arrow,
        Freehand,
        Text,
        SpeechBubble,
        StepLabel,
        Emoji,
        Highlight,
        Obfuscate,
        CutMark,
        Crop
    }

    /// <summary>
    /// The mouse (or pen) as a tool sees it: the point in canvas pixels, the keys held, and how many canvas pixels one
    /// screen pixel is (for handles and click tolerance, which keep their size on the screen at any zoom)
    /// </summary>
    public readonly record struct ToolPointer(PointD Point, bool Shift, bool Control, bool Alt, double PixelSize, int ClickCount = 1)
    {
        /// <summary>
        /// How far from a line a click still hits it, in canvas pixels
        /// </summary>
        public double Tolerance => 3 * PixelSize;
    }

    /// <summary>
    /// The keys a tool reacts to
    /// </summary>
    public enum ToolKey
    {
        Escape,
        Enter,
        Left,
        Right,
        Up,
        Down
    }

    /// <summary>
    /// The mouse cursor a tool wants, the view maps it to a cursor of its UI framework
    /// </summary>
    public enum ToolCursor
    {
        Arrow,
        Cross,
        Move,
        SizeNorthSouth,
        SizeWestEast,
        SizeNorthWestSouthEast,
        SizeNorthEastSouthWest,
        Rotate,
        Pen,
        Text
    }

    public enum AdornerKind
    {
        /// <summary>The outline of a selected element: Area turned by Rotation</summary>
        Outline,
        /// <summary>A square handle at Point</summary>
        Handle,
        /// <summary>The round handle which turns the element, at Point, with a line from To</summary>
        RotateHandle,
        /// <summary>A round handle at an end of a line or the tail of a speech bubble</summary>
        PointHandle,
        /// <summary>The area which is being selected with the mouse</summary>
        RubberBand,
        /// <summary>The area which stays after a crop, everything outside is dimmed</summary>
        CropArea
    }

    /// <summary>
    /// Something a tool shows over the document, in canvas pixels. The view draws it in screen pixels, so it keeps its size.
    /// </summary>
    public sealed record Adorner(AdornerKind Kind, RectD Area = default, double Rotation = 0, PointD Point = default, PointD To = default);

    /// <summary>
    /// What a tool can do with the document it works on. The tab of the document implements it.
    /// </summary>
    public interface IToolHost
    {
        /// <summary>
        /// The document as it is shown, with a preview of the change being made
        /// </summary>
        GreenshotDocument Document { get; }

        IReadOnlyCollection<Guid> Selection { get; }

        void Select(IEnumerable<Guid> ids);

        /// <summary>
        /// Shows the document while a change is being made (e.g. while the mouse drags), it isn't an undo step
        /// </summary>
        void Preview(GreenshotDocument document);

        /// <summary>
        /// The change is done: the document becomes an undo step with the description
        /// </summary>
        void Commit(GreenshotDocument document, string description);

        /// <summary>
        /// Back to the document before the preview
        /// </summary>
        void CancelPreview();

        /// <summary>
        /// The style a new element of the tool gets: what the user chose for the tool, over the defaults of its type
        /// </summary>
        Style StyleFor(ToolKind kind);

        /// <summary>
        /// What the user chose for the tools which isn't a style: highlight and obfuscate mode, arrow heads, the emoji
        /// </summary>
        ToolOptions Options { get; }

        /// <summary>
        /// Starts typing in the text of the element
        /// </summary>
        void EditText(Guid elementId);

        /// <summary>
        /// The adorners or the cursor of the tool changed, they are drawn again
        /// </summary>
        void ToolChanged();
    }

    /// <summary>
    /// The values of new elements which aren't part of their style
    /// </summary>
    public sealed class ToolOptions
    {
        public ArrowHeads ArrowHeads { get; set; } = ArrowHeads.End;
        public HighlightMode HighlightMode { get; set; } = HighlightMode.Text;
        public ObfuscateMode ObfuscateMode { get; set; } = ObfuscateMode.Pixelate;
        public int PixelSize { get; set; } = 5;
        public int BlurRadius { get; set; } = 3;
        public double Brightness { get; set; } = 0.9;
        public int Magnification { get; set; } = 2;
        public EdgeStyle CutMarkShape { get; set; } = EdgeStyle.Torn;
        public string Emoji { get; set; } = "\U0001F600";
    }

    /// <summary>
    /// A tool of the editor: it gets the mouse and keys in canvas pixels and changes the document through its host. It knows
    /// nothing of the UI framework, so another one can use it too.
    /// </summary>
    public interface IEditorTool
    {
        ToolKind Kind { get; }

        void Down(ToolPointer pointer);

        void Move(ToolPointer pointer);

        void Up(ToolPointer pointer);

        /// <summary>
        /// The mouse moves without a button pressed
        /// </summary>
        void Hover(ToolPointer pointer);

        /// <summary>
        /// True when the tool used the key
        /// </summary>
        bool Key(ToolKey key, bool shift, bool control);

        /// <summary>
        /// The tool is left, or the mouse capture was lost: a change being made is dropped
        /// </summary>
        void Cancel();

        ToolCursor Cursor { get; }

        IReadOnlyList<Adorner> Adorners { get; }
    }
}
