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

using System.ComponentModel;
using Dapplo.Ini.Internationalization.Attributes;

namespace Greenshot.Base.Languages
{
    /// <summary>
    /// The texts of the [Editor] section of greenshot.{ietf}.ini, the English text is shown per property.
    /// Generated from the language files, add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("Editor")]
    public interface IEditorLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Bottom
        /// </summary>
        string AlignBottom { get; }

        /// <summary>
        /// Center
        /// </summary>
        string AlignCenter { get; }

        /// <summary>
        /// Horizontal alignment
        /// </summary>
        string AlignHorizontal { get; }

        /// <summary>
        /// Left
        /// </summary>
        string AlignLeft { get; }

        /// <summary>
        /// Middle
        /// </summary>
        string AlignMiddle { get; }

        /// <summary>
        /// Right
        /// </summary>
        string AlignRight { get; }

        /// <summary>
        /// Top
        /// </summary>
        string AlignTop { get; }

        /// <summary>
        /// Vertical alignment
        /// </summary>
        string AlignVertical { get; }

        /// <summary>
        /// Arrange
        /// </summary>
        string Arrange { get; }

        /// <summary>
        /// Arrow heads
        /// </summary>
        string Arrowheads { get; }

        /// <summary>
        /// Both
        /// </summary>
        string ArrowheadsBoth { get; }

        /// <summary>
        /// End point
        /// </summary>
        string ArrowheadsEnd { get; }

        /// <summary>
        /// None
        /// </summary>
        string ArrowheadsNone { get; }

        /// <summary>
        /// Start point
        /// </summary>
        string ArrowheadsStart { get; }

        /// <summary>
        /// Auto crop
        /// </summary>
        string Autocrop { get; }

        /// <summary>
        /// Auto crop not possible
        /// </summary>
        string AutocropNotPossible { get; }

        /// <summary>
        /// Fill color (0-9)
        /// </summary>
        string Backcolor { get; }

        /// <summary>
        /// Blur radius
        /// </summary>
        string BlurRadius { get; }

        /// <summary>
        /// Bold
        /// </summary>
        string Bold { get; }

        /// <summary>
        /// Border
        /// </summary>
        string Border { get; }

        /// <summary>
        /// Brightness
        /// </summary>
        string Brightness { get; }

        /// <summary>
        /// Error while accessing the clipboard. Please try again.
        /// </summary>
        string Clipboardfailed { get; }

        /// <summary>
        /// Close
        /// </summary>
        string Close { get; }

        /// <summary>
        /// Close all
        /// </summary>
        string CloseAll { get; }

        /// <summary>
        /// Do you want to save the screenshot?
        /// </summary>
        string CloseOnSave { get; }

        /// <summary>
        /// Save image?
        /// </summary>
        string CloseOnSaveTitle { get; }

        /// <summary>
        /// Confirm
        /// </summary>
        string Confirm { get; }

        /// <summary>
        /// Copy image to clipboard
        /// </summary>
        string Copyimagetoclipboard { get; }

        /// <summary>
        /// Copy path to clipboard
        /// </summary>
        string Copypathtoclipboard { get; }

        /// <summary>
        /// Copy
        /// </summary>
        string Copytoclipboard { get; }

        /// <summary>
        /// Add counter (I)
        /// </summary>
        string Counter { get; }

        /// <summary>
        /// Start value
        /// </summary>
        string CounterStartvalue { get; }

        /// <summary>
        /// Crop (C)
        /// </summary>
        string Crop { get; }

        /// <summary>
        /// Crop mode
        /// </summary>
        string CropMode { get; }

        /// <summary>
        /// Auto crop
        /// </summary>
        string CropmodeAuto { get; }

        /// <summary>
        /// Crop
        /// </summary>
        string CropmodeDefault { get; }

        /// <summary>
        /// Crop out horizontally
        /// </summary>
        string CropmodeHorizontal { get; }

        /// <summary>
        /// Crop out vertically
        /// </summary>
        string CropmodeVertical { get; }

        /// <summary>
        /// Cut mark
        /// </summary>
        string CutMark { get; }

        /// <summary>
        /// Lines
        /// </summary>
        string CutMarkLine { get; }

        /// <summary>
        /// None
        /// </summary>
        string CutMarkNone { get; }

        /// <summary>
        /// Torn edges
        /// </summary>
        string CutMarkTorn { get; }

        /// <summary>
        /// Waves
        /// </summary>
        string CutMarkWave { get; }

        /// <summary>
        /// Zig-zag
        /// </summary>
        string CutMarkZigzag { get; }

        /// <summary>
        /// Selection Tool (ESC)
        /// </summary>
        string Cursortool { get; }

        /// <summary>
        /// Cut
        /// </summary>
        string Cuttoclipboard { get; }

        /// <summary>
        /// Delete
        /// </summary>
        string Deleteelement { get; }

        /// <summary>
        /// Down one level
        /// </summary>
        string Downonelevel { get; }

        /// <summary>
        /// Down to bottom
        /// </summary>
        string Downtobottom { get; }

        /// <summary>
        /// Draw arrow (A)
        /// </summary>
        string Drawarrow { get; }

        /// <summary>
        /// Draw ellipse (E)
        /// </summary>
        string Drawellipse { get; }

        /// <summary>
        /// Draw freehand (F)
        /// </summary>
        string Drawfreehand { get; }

        /// <summary>
        /// Highlight (H)
        /// </summary>
        string Drawhighlighter { get; }

        /// <summary>
        /// Draw line (L)
        /// </summary>
        string Drawline { get; }

        /// <summary>
        /// Draw rectangle (R)
        /// </summary>
        string Drawrectangle { get; }

        /// <summary>
        /// Add textbox (T)
        /// </summary>
        string Drawtextbox { get; }

        /// <summary>
        /// Shadow darkness
        /// </summary>
        string DropshadowDarkness { get; }

        /// <summary>
        /// Shadow offset
        /// </summary>
        string DropshadowOffset { get; }

        /// <summary>
        /// Dropshadow settings
        /// </summary>
        string DropshadowSettings { get; }

        /// <summary>
        /// Shadow thickness
        /// </summary>
        string DropshadowThickness { get; }

        /// <summary>
        /// Duplicate selected element
        /// </summary>
        string Duplicate { get; }

        /// <summary>
        /// Edit
        /// </summary>
        string Edit { get; }

        /// <summary>
        /// Effects
        /// </summary>
        string Effects { get; }

        /// <summary>
        /// E-Mail
        /// </summary>
        string Email { get; }

        /// <summary>
        /// File
        /// </summary>
        string File { get; }

        /// <summary>
        /// Stretch
        /// </summary>
        string Fit { get; }

        /// <summary>
        /// Size
        /// </summary>
        string Fontsize { get; }

        /// <summary>
        /// Line color (NumPad0-9, Shift+0-9)
        /// </summary>
        string Forecolor { get; }

        /// <summary>
        /// Grayscale
        /// </summary>
        string Grayscale { get; }

        /// <summary>
        /// Highlight area
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
        /// Highlight mode
        /// </summary>
        string HighlightMode { get; }

        /// <summary>
        /// Highlight text
        /// </summary>
        string HighlightText { get; }

        /// <summary>
        /// Image saved to {0}.
        /// </summary>
        string Imagesaved { get; }

        /// <summary>
        /// Drop shadow
        /// </summary>
        string ImageShadow { get; }

        /// <summary>
        /// Insert window
        /// </summary>
        string Insertwindow { get; }

        /// <summary>
        /// Invert
        /// </summary>
        string Invert { get; }

        /// <summary>
        /// Italic
        /// </summary>
        string Italic { get; }

        /// <summary>
        /// Load objects from file
        /// </summary>
        string LoadObjects { get; }

        /// <summary>
        /// Magnification factor
        /// </summary>
        string MagnificationFactor { get; }

        /// <summary>
        /// Match capture size
        /// </summary>
        string MatchCaptureSize { get; }

        /// <summary>
        /// Obfuscate (O)
        /// </summary>
        string Obfuscate { get; }

        /// <summary>
        /// Blur
        /// </summary>
        string ObfuscateBlur { get; }

        /// <summary>
        /// Obfuscation mode
        /// </summary>
        string ObfuscateMode { get; }

        /// <summary>
        /// Pixelize
        /// </summary>
        string ObfuscatePixelize { get; }

        /// <summary>
        /// Redact Text...
        /// </summary>
        string ObfuscateText { get; }

        /// <summary>
        /// Advanced Settings
        /// </summary>
        string ObfuscateTextAdvanced { get; }

        /// <summary>
        /// Apply
        /// </summary>
        string ObfuscateTextApply { get; }

        /// <summary>
        /// Auto Search
        /// </summary>
        string ObfuscateTextAutoSearch { get; }

        /// <summary>
        /// Case Sensitive
        /// </summary>
        string ObfuscateTextCaseSensitive { get; }

        /// <summary>
        /// Error
        /// </summary>
        string ObfuscateTextError { get; }

        /// <summary>
        /// Matches found: {0}
        /// </summary>
        string ObfuscateTextMatches { get; }

        /// <summary>
        /// No capture available
        /// </summary>
        string ObfuscateTextNoCapture { get; }

        /// <summary>
        /// OCR provider not available
        /// </summary>
        string ObfuscateTextNoOcrProvider { get; }

        /// <summary>
        /// No text found in image
        /// </summary>
        string ObfuscateTextNoText { get; }

        /// <summary>
        /// OCR failed
        /// </summary>
        string ObfuscateTextOcrFailed { get; }

        /// <summary>
        /// Use Regular Expression
        /// </summary>
        string ObfuscateTextRegex { get; }

        /// <summary>
        /// Lines
        /// </summary>
        string ObfuscateTextScopeLines { get; }

        /// <summary>
        /// Words
        /// </summary>
        string ObfuscateTextScopeWords { get; }

        /// <summary>
        /// Search for:
        /// </summary>
        string ObfuscateTextSearch { get; }

        /// <summary>
        /// Search
        /// </summary>
        string ObfuscateTextSearchButton { get; }

        /// <summary>
        /// Search in:
        /// </summary>
        string ObfuscateTextSearchScope { get; }

        /// <summary>
        /// Redact Text
        /// </summary>
        string ObfuscateTextTitle { get; }

        /// <summary>
        /// Object
        /// </summary>
        string Object { get; }

        /// <summary>
        /// Open directory in Windows Explorer
        /// </summary>
        string Opendirinexplorer { get; }

        /// <summary>
        /// Paste
        /// </summary>
        string Pastefromclipboard { get; }

        /// <summary>
        /// Pixel size
        /// </summary>
        string PixelSize { get; }

        /// <summary>
        /// Preview quality
        /// </summary>
        string PreviewQuality { get; }

        /// <summary>
        /// Print
        /// </summary>
        string Print { get; }

        /// <summary>
        /// Expand capture and move outside
        /// </summary>
        string Pushout { get; }

        /// <summary>
        /// Redo {0}
        /// </summary>
        string Redo { get; }

        /// <summary>
        /// Remove transparency
        /// </summary>
        string RemoveTransparency { get; }

        /// <summary>
        /// Reset size
        /// </summary>
        string Resetsize { get; }

        /// <summary>
        /// Resize (Z)
        /// </summary>
        string Resize { get; }

        /// <summary>
        /// Maintain aspect ratio
        /// </summary>
        string ResizeAspectratio { get; }

        /// <summary>
        /// Height
        /// </summary>
        string ResizeHeight { get; }

        /// <summary>
        /// Percent
        /// </summary>
        string ResizePercent { get; }

        /// <summary>
        /// Pixels
        /// </summary>
        string ResizePixel { get; }

        /// <summary>
        /// Resize settings
        /// </summary>
        string ResizeSettings { get; }

        /// <summary>
        /// Width
        /// </summary>
        string ResizeWidth { get; }

        /// <summary>
        /// Rotate counter clockwise (Control + ,)
        /// </summary>
        string Rotateccw { get; }

        /// <summary>
        /// Rotate clockwise (Control + .)
        /// </summary>
        string Rotatecw { get; }

        /// <summary>
        /// Save
        /// </summary>
        string Save { get; }

        /// <summary>
        /// Save as...
        /// </summary>
        string Saveas { get; }

        /// <summary>
        /// Save objects to file
        /// </summary>
        string SaveObjects { get; }

        /// <summary>
        /// Select all
        /// </summary>
        string Selectall { get; }

        /// <summary>
        /// Print job was sent to '{0}'.
        /// </summary>
        string Senttoprinter { get; }

        /// <summary>
        /// Drop shadow (/)
        /// </summary>
        string Shadow { get; }

        /// <summary>
        /// Dock to edge
        /// </summary>
        string Snap { get; }

        /// <summary>
        /// Add speechbubble (S)
        /// </summary>
        string Speechbubble { get; }

        /// <summary>
        /// Image stored to clipboard.
        /// </summary>
        string Storedtoclipboard { get; }

        /// <summary>
        /// Line thickness
        /// </summary>
        string Thickness { get; }

        /// <summary>
        /// Greenshot image editor
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Torn edge
        /// </summary>
        string TornEdge { get; }

        /// <summary>
        /// Tear all sides
        /// </summary>
        string TornedgeAll { get; }

        /// <summary>
        /// Bottom side
        /// </summary>
        string TornedgeBottom { get; }

        /// <summary>
        /// Horizontal tooth range
        /// </summary>
        string TornedgeHorizontaltoothrange { get; }

        /// <summary>
        /// Left side
        /// </summary>
        string TornedgeLeft { get; }

        /// <summary>
        /// Right side
        /// </summary>
        string TornedgeRight { get; }

        /// <summary>
        /// Torn edges settings
        /// </summary>
        string TornedgeSettings { get; }

        /// <summary>
        /// Generate shadow
        /// </summary>
        string TornedgeShadow { get; }

        /// <summary>
        /// Tooth size
        /// </summary>
        string TornedgeToothsize { get; }

        /// <summary>
        /// Top side
        /// </summary>
        string TornedgeTop { get; }

        /// <summary>
        /// Vertical tooth range
        /// </summary>
        string TornedgeVerticaltoothrange { get; }

        /// <summary>
        /// Undo {0}
        /// </summary>
        string Undo { get; }

        /// <summary>
        /// Up one level
        /// </summary>
        string Uponelevel { get; }

        /// <summary>
        /// Up to top
        /// </summary>
        string Uptotop { get; }
    }
}
