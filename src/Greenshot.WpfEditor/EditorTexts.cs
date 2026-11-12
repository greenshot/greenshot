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
using Greenshot.Base.Languages;
using Greenshot.Document;
using Greenshot.WpfEditor.Tools;

namespace Greenshot.WpfEditor
{
    /// <summary>
    /// The texts of the editor, registered on first use
    /// </summary>
    public static class EditorTexts
    {
        private static readonly Lazy<IWpfEditorLanguage> Language = new(() => Texts.Register<IWpfEditorLanguage>(new WpfEditorLanguageImpl()));

        public static IWpfEditorLanguage Wpf => Language.Value;

        /// <summary>
        /// Registers the texts, so {wpf:Text WpfEditor.…} in the XAML finds them
        /// </summary>
        public static void Register() => _ = Language.Value;

        public static string NameOf(ToolKind kind) => kind switch
        {
            ToolKind.Rectangle => Wpf.NameRectangle,
            ToolKind.Ellipse => Wpf.NameEllipse,
            ToolKind.Line => Wpf.NameLine,
            ToolKind.Arrow => Wpf.NameArrow,
            ToolKind.Freehand => Wpf.NameFreehand,
            ToolKind.Text => Wpf.NameText,
            ToolKind.SpeechBubble => Wpf.NameSpeechBubble,
            ToolKind.StepLabel => Wpf.NameStepLabel,
            ToolKind.Emoji => Wpf.NameEmoji,
            ToolKind.Highlight => Wpf.NameHighlight,
            ToolKind.Obfuscate => Wpf.NameObfuscate,
            ToolKind.CutMark => Wpf.NameCutMark,
            ToolKind.Crop => Wpf.NameCrop,
            _ => Wpf.NameSelect
        };

        /// <summary>
        /// The text of a value of the enums the panel offers
        /// </summary>
        public static string NameOf(object value) => value switch
        {
            ArrowHeads.None => Wpf.ArrowHeadsNone,
            ArrowHeads.Start => Wpf.ArrowHeadsStart,
            ArrowHeads.End => Wpf.ArrowHeadsEnd,
            ArrowHeads.Both => Wpf.ArrowHeadsBoth,
            HighlightMode.Text => Wpf.HighlightText,
            HighlightMode.Area => Wpf.HighlightArea,
            HighlightMode.Grayscale => Wpf.HighlightGrayscale,
            HighlightMode.Magnify => Wpf.HighlightMagnify,
            ObfuscateMode.Pixelate => Wpf.ObfuscatePixelate,
            ObfuscateMode.Blur => Wpf.ObfuscateBlur,
            EdgeStyle.None => Wpf.EdgeNone,
            EdgeStyle.Line => Wpf.EdgeLine,
            EdgeStyle.ZigZag => Wpf.EdgeZigZag,
            EdgeStyle.Wave => Wpf.EdgeWave,
            EdgeStyle.Torn => Wpf.EdgeTorn,
            ToolKind kind => NameOf(kind),
            _ => value?.ToString() ?? string.Empty
        };
    }
}
