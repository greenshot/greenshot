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

using System.ComponentModel;
using Dapplo.Ini.Internationalization.Attributes;

namespace Greenshot.WpfEditor
{
    /// <summary>
    /// The texts of the [WpfEditor] section of greenshot.wpfeditor.{ietf}.ini, the English text is shown per property
    /// </summary>
    [IniLanguageSection("WpfEditor", ModuleName = "wpfeditor")]
    public interface IWpfEditorLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Greenshot editor (new)
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Editor (new)
        /// </summary>
        string Destination { get; }

        /// <summary>
        /// Untitled
        /// </summary>
        string Untitled { get; }

        /// <summary>
        /// Move
        /// </summary>
        string UndoMove { get; }

        /// <summary>
        /// Turn
        /// </summary>
        string UndoTurn { get; }

        /// <summary>
        /// Resize
        /// </summary>
        string UndoResize { get; }

        /// <summary>
        /// Add
        /// </summary>
        string UndoAdd { get; }

        /// <summary>
        /// Crop
        /// </summary>
        string UndoCrop { get; }

        /// <summary>
        /// Text
        /// </summary>
        string UndoText { get; }

        /// <summary>
        /// Delete
        /// </summary>
        string UndoDelete { get; }

        /// <summary>
        /// Cut
        /// </summary>
        string UndoCut { get; }

        /// <summary>
        /// Paste
        /// </summary>
        string UndoPaste { get; }

        /// <summary>
        /// Duplicate
        /// </summary>
        string UndoDuplicate { get; }

        /// <summary>
        /// Arrange
        /// </summary>
        string UndoArrange { get; }

        /// <summary>
        /// Style
        /// </summary>
        string UndoStyle { get; }

        /// <summary>
        /// Effect
        /// </summary>
        string UndoEffect { get; }

        /// <summary>
        /// Layer
        /// </summary>
        string UndoLayer { get; }

        /// <summary>
        /// Select (Esc)
        /// </summary>
        string ToolSelect { get; }

        /// <summary>
        /// Rectangle (R)
        /// </summary>
        string ToolRectangle { get; }

        /// <summary>
        /// Ellipse (E)
        /// </summary>
        string ToolEllipse { get; }

        /// <summary>
        /// Line (L)
        /// </summary>
        string ToolLine { get; }

        /// <summary>
        /// Arrow (A)
        /// </summary>
        string ToolArrow { get; }

        /// <summary>
        /// Freehand (F)
        /// </summary>
        string ToolFreehand { get; }

        /// <summary>
        /// Text (T)
        /// </summary>
        string ToolText { get; }

        /// <summary>
        /// Speech bubble (S)
        /// </summary>
        string ToolSpeechBubble { get; }

        /// <summary>
        /// Step label (I)
        /// </summary>
        string ToolStepLabel { get; }

        /// <summary>
        /// Emoji (J)
        /// </summary>
        string ToolEmoji { get; }

        /// <summary>
        /// Highlight (H)
        /// </summary>
        string ToolHighlight { get; }

        /// <summary>
        /// Obfuscate (O)
        /// </summary>
        string ToolObfuscate { get; }

        /// <summary>
        /// Cut mark (U)
        /// </summary>
        string ToolCutMark { get; }

        /// <summary>
        /// Crop (C)
        /// </summary>
        string ToolCrop { get; }

        /// <summary>
        /// Selection
        /// </summary>
        string NameSelect { get; }

        /// <summary>
        /// Rectangle
        /// </summary>
        string NameRectangle { get; }

        /// <summary>
        /// Ellipse
        /// </summary>
        string NameEllipse { get; }

        /// <summary>
        /// Line
        /// </summary>
        string NameLine { get; }

        /// <summary>
        /// Arrow
        /// </summary>
        string NameArrow { get; }

        /// <summary>
        /// Freehand
        /// </summary>
        string NameFreehand { get; }

        /// <summary>
        /// Text
        /// </summary>
        string NameText { get; }

        /// <summary>
        /// Speech bubble
        /// </summary>
        string NameSpeechBubble { get; }

        /// <summary>
        /// Step label
        /// </summary>
        string NameStepLabel { get; }

        /// <summary>
        /// Emoji
        /// </summary>
        string NameEmoji { get; }

        /// <summary>
        /// Highlight
        /// </summary>
        string NameHighlight { get; }

        /// <summary>
        /// Obfuscate
        /// </summary>
        string NameObfuscate { get; }

        /// <summary>
        /// Cut mark
        /// </summary>
        string NameCutMark { get; }

        /// <summary>
        /// Crop
        /// </summary>
        string NameCrop { get; }

        /// <summary>
        /// File
        /// </summary>
        string File { get; }

        /// <summary>
        /// Open... (Ctrl+O)
        /// </summary>
        string Open { get; }

        /// <summary>
        /// Save (Ctrl+S)
        /// </summary>
        string Save { get; }

        /// <summary>
        /// Save as... (Ctrl+Shift+S)
        /// </summary>
        string SaveAs { get; }

        /// <summary>
        /// Copy image (Ctrl+Shift+C)
        /// </summary>
        string CopyImage { get; }

        /// <summary>
        /// Print... (Ctrl+P)
        /// </summary>
        string Print { get; }

        /// <summary>
        /// Close tab (Ctrl+W)
        /// </summary>
        string CloseTab { get; }

        /// <summary>
        /// Send to
        /// </summary>
        string SendTo { get; }

        /// <summary>
        /// Undo (Ctrl+Z)
        /// </summary>
        string Undo { get; }

        /// <summary>
        /// Undo {0} (Ctrl+Z)
        /// </summary>
        string UndoStep { get; }

        /// <summary>
        /// Redo (Ctrl+Y)
        /// </summary>
        string Redo { get; }

        /// <summary>
        /// Redo {0} (Ctrl+Y)
        /// </summary>
        string RedoStep { get; }

        /// <summary>
        /// Cut (Ctrl+X)
        /// </summary>
        string Cut { get; }

        /// <summary>
        /// Copy (Ctrl+C)
        /// </summary>
        string Copy { get; }

        /// <summary>
        /// Paste (Ctrl+V)
        /// </summary>
        string Paste { get; }

        /// <summary>
        /// Delete (Del)
        /// </summary>
        string Delete { get; }

        /// <summary>
        /// Duplicate (Ctrl+D)
        /// </summary>
        string Duplicate { get; }

        /// <summary>
        /// Select all (Ctrl+A)
        /// </summary>
        string SelectAll { get; }

        /// <summary>
        /// Bring to front (Home)
        /// </summary>
        string BringToFront { get; }

        /// <summary>
        /// Bring forward (Page up)
        /// </summary>
        string BringForward { get; }

        /// <summary>
        /// Send backward (Page down)
        /// </summary>
        string SendBackward { get; }

        /// <summary>
        /// Send to back (End)
        /// </summary>
        string SendToBack { get; }

        /// <summary>
        /// Turn clockwise (Ctrl+.)
        /// </summary>
        string TurnClockwise { get; }

        /// <summary>
        /// Turn counterclockwise (Ctrl+,)
        /// </summary>
        string TurnCounterClockwise { get; }

        /// <summary>
        /// Zoom in (Ctrl++)
        /// </summary>
        string ZoomIn { get; }

        /// <summary>
        /// Zoom out (Ctrl+-)
        /// </summary>
        string ZoomOut { get; }

        /// <summary>
        /// Best fit (Ctrl+9)
        /// </summary>
        string ZoomBestFit { get; }

        /// <summary>
        /// Actual size (Ctrl+0)
        /// </summary>
        string ZoomActualSize { get; }

        /// <summary>
        /// Insert image...
        /// </summary>
        string InsertImage { get; }

        /// <summary>
        /// Auto crop
        /// </summary>
        string AutoCrop { get; }

        /// <summary>
        /// Apply crop (Enter)
        /// </summary>
        string ApplyCrop { get; }

        /// <summary>
        /// Cancel (Esc)
        /// </summary>
        string CancelCrop { get; }

        /// <summary>
        /// Canvas
        /// </summary>
        string Canvas { get; }

        /// <summary>
        /// Line color
        /// </summary>
        string LineColor { get; }

        /// <summary>
        /// Line width
        /// </summary>
        string LineWidth { get; }

        /// <summary>
        /// Fill color
        /// </summary>
        string FillColor { get; }

        /// <summary>
        /// Shadow
        /// </summary>
        string Shadow { get; }

        /// <summary>
        /// Font
        /// </summary>
        string Font { get; }

        /// <summary>
        /// Font size
        /// </summary>
        string FontSize { get; }

        /// <summary>
        /// Bold
        /// </summary>
        string Bold { get; }

        /// <summary>
        /// Italic
        /// </summary>
        string Italic { get; }

        /// <summary>
        /// Text color
        /// </summary>
        string TextColor { get; }

        /// <summary>
        /// Align left
        /// </summary>
        string AlignLeft { get; }

        /// <summary>
        /// Center
        /// </summary>
        string AlignCenter { get; }

        /// <summary>
        /// Align right
        /// </summary>
        string AlignRight { get; }

        /// <summary>
        /// Align top
        /// </summary>
        string AlignTop { get; }

        /// <summary>
        /// Middle
        /// </summary>
        string AlignMiddle { get; }

        /// <summary>
        /// Align bottom
        /// </summary>
        string AlignBottom { get; }

        /// <summary>
        /// Arrow heads
        /// </summary>
        string ArrowHeads { get; }

        /// <summary>
        /// None
        /// </summary>
        string ArrowHeadsNone { get; }

        /// <summary>
        /// Start
        /// </summary>
        string ArrowHeadsStart { get; }

        /// <summary>
        /// End
        /// </summary>
        string ArrowHeadsEnd { get; }

        /// <summary>
        /// Both
        /// </summary>
        string ArrowHeadsBoth { get; }

        /// <summary>
        /// Highlight
        /// </summary>
        string HighlightMode { get; }

        /// <summary>
        /// Text
        /// </summary>
        string HighlightText { get; }

        /// <summary>
        /// Area
        /// </summary>
        string HighlightArea { get; }

        /// <summary>
        /// Grayscale
        /// </summary>
        string HighlightGrayscale { get; }

        /// <summary>
        /// Magnify
        /// </summary>
        string HighlightMagnify { get; }

        /// <summary>
        /// Highlight color
        /// </summary>
        string HighlightColor { get; }

        /// <summary>
        /// Magnification
        /// </summary>
        string Magnification { get; }

        /// <summary>
        /// Obfuscate
        /// </summary>
        string ObfuscateMode { get; }

        /// <summary>
        /// Pixelate
        /// </summary>
        string ObfuscatePixelate { get; }

        /// <summary>
        /// Blur
        /// </summary>
        string ObfuscateBlur { get; }

        /// <summary>
        /// Strength
        /// </summary>
        string ObfuscateSize { get; }

        /// <summary>
        /// Edge
        /// </summary>
        string EdgeShape { get; }

        /// <summary>
        /// Straight
        /// </summary>
        string EdgeNone { get; }

        /// <summary>
        /// Line
        /// </summary>
        string EdgeLine { get; }

        /// <summary>
        /// Zigzag
        /// </summary>
        string EdgeZigZag { get; }

        /// <summary>
        /// Wave
        /// </summary>
        string EdgeWave { get; }

        /// <summary>
        /// Torn
        /// </summary>
        string EdgeTorn { get; }

        /// <summary>
        /// X
        /// </summary>
        string PositionX { get; }

        /// <summary>
        /// Y
        /// </summary>
        string PositionY { get; }

        /// <summary>
        /// Width
        /// </summary>
        string Width { get; }

        /// <summary>
        /// Height
        /// </summary>
        string Height { get; }

        /// <summary>
        /// Rotation
        /// </summary>
        string Rotation { get; }

        /// <summary>
        /// Background
        /// </summary>
        string Background { get; }

        /// <summary>
        /// Border and shadow
        /// </summary>
        string Frame { get; }

        /// <summary>
        /// Border width
        /// </summary>
        string FrameWidth { get; }

        /// <summary>
        /// Border color
        /// </summary>
        string FrameColor { get; }

        /// <summary>
        /// Drop shadow
        /// </summary>
        string FrameShadow { get; }

        /// <summary>
        /// Border
        /// </summary>
        string AddBorder { get; }

        /// <summary>
        /// Drop shadow
        /// </summary>
        string AddDropShadow { get; }

        /// <summary>
        /// Torn edges
        /// </summary>
        string AddTornEdges { get; }

        /// <summary>
        /// Remove
        /// </summary>
        string RemoveFrame { get; }

        /// <summary>
        /// Resize
        /// </summary>
        string Resize { get; }

        /// <summary>
        /// Keep the aspect ratio
        /// </summary>
        string KeepRatio { get; }

        /// <summary>
        /// Apply
        /// </summary>
        string Apply { get; }

        /// <summary>
        /// Layers
        /// </summary>
        string Layers { get; }

        /// <summary>
        /// Visible
        /// </summary>
        string LayerVisible { get; }

        /// <summary>
        /// Locked
        /// </summary>
        string LayerLocked { get; }

        /// <summary>
        /// Emoji
        /// </summary>
        string Emoji { get; }

        /// <summary>
        /// The emoji new emoji elements show
        /// </summary>
        string EmojiHint { get; }

        /// <summary>
        /// Color (#RRGGBB or #AARRGGBB)
        /// </summary>
        string ColorHex { get; }

        /// <summary>
        /// No color
        /// </summary>
        string ColorNone { get; }

        /// <summary>
        /// Opacity
        /// </summary>
        string Opacity { get; }

        /// <summary>
        /// Save the changes to "{0}"?
        /// </summary>
        string SaveChanges { get; }

        /// <summary>
        /// Save
        /// </summary>
        string SaveButton { get; }

        /// <summary>
        /// Don't save
        /// </summary>
        string DontSaveButton { get; }

        /// <summary>
        /// Cancel
        /// </summary>
        string CancelButton { get; }

        /// <summary>
        /// The file "{0}" could not be opened: {1}
        /// </summary>
        string OpenFailed { get; }

        /// <summary>
        /// The file "{0}" could not be saved: {1}
        /// </summary>
        string SaveFailed { get; }

        /// <summary>
        /// Sending to {0} failed: {1}
        /// </summary>
        string ExportFailed { get; }

        /// <summary>
        /// Greenshot documents
        /// </summary>
        string DocumentsFilter { get; }

        /// <summary>
        /// Images
        /// </summary>
        string ImagesFilter { get; }

        /// <summary>
        /// All supported files
        /// </summary>
        string SupportedFilter { get; }

        /// <summary>
        /// {0} %
        /// </summary>
        string ZoomStatus { get; }

        /// <summary>
        /// {0} x {1} px
        /// </summary>
        string SizeStatus { get; }

        /// <summary>
        /// {0} selected
        /// </summary>
        string SelectionStatus { get; }

        /// <summary>
        /// Open documents
        /// </summary>
        string Tabs { get; }

        /// <summary>
        /// Tools
        /// </summary>
        string Tools { get; }

        /// <summary>
        /// Document canvas
        /// </summary>
        string CanvasName { get; }
    }
}
