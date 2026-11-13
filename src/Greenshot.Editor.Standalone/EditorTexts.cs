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
using System.Globalization;
using System.IO;
using Dapplo.Ini.Internationalization;
using Dapplo.Ini.Internationalization.Configuration;
using Greenshot.Document;
using Greenshot.Editor.Standalone.Settings;
using Greenshot.Editor.Standalone.Diagnostics;
using Greenshot.Editor.Standalone.Tools;

namespace Greenshot.Editor.Standalone
{
    /// <summary>
    /// The texts of the editor, from the language packs greenshot-editor.{ietf}.ini in the folder Languages, loaded on first use.
    /// The language is the one set for the editor, else the language of Greenshot, else the one of the system.
    /// </summary>
    public static class EditorTexts
    {
        private const string BaseName = "greenshot-editor";
        private const string BaseLanguage = "en-US";
        private static readonly Lazy<IEditorLanguage> Language = new(Load);
        private static LanguageConfig _config;

        public static IEditorLanguage Text => Language.Value;

        /// <summary>
        /// The language in use, e.g. en-US
        /// </summary>
        public static string CurrentLanguage
        {
            get
            {
                _ = Language.Value;
                return _config?.CurrentLanguage ?? BaseLanguage;
            }
        }

        /// <summary>
        /// The language the editor should use: its own setting, the language of Greenshot or the language of the system
        /// </summary>
        public static string RequestedLanguage()
        {
            string language = EditorSettings.Current.Language;
            if (string.IsNullOrWhiteSpace(language))
            {
                language = EditorSettings.ReadGreenshotValue("Core", "Language");
            }

            return string.IsNullOrWhiteSpace(language) ? CultureInfo.CurrentUICulture.Name : language;
        }

        /// <summary>
        /// Changes the language while the editor runs, e.g. when Greenshot tells that its language changed
        /// </summary>
        public static void SetLanguage(string language)
        {
            _ = Language.Value;
            try
            {
                if (_config != null && !string.IsNullOrWhiteSpace(language) && _config.ResolveLanguage(language) is string resolved && resolved != _config.CurrentLanguage)
                {
                    _config.SetLanguage(resolved);
                }
            }
            catch (Exception ex)
            {
                EditorLog.Warn($"The language {language} could not be loaded", ex);
            }
        }

        private static IEditorLanguage Load()
        {
            var section = new EditorLanguageImpl();
            string requested = BaseLanguage;
            try
            {
                requested = RequestedLanguage();
                _config = LanguageConfigRegistry.ForFile(BaseName)
                    .WithBaseLanguage(BaseLanguage)
                    .WithCurrentLanguage(string.IsNullOrWhiteSpace(requested) ? BaseLanguage : requested)
                    .ResolveLanguages()
                    .AddSearchPath(Path.Combine(AppContext.BaseDirectory, "Languages"))
                    .RegisterSection<IEditorLanguage>(section)
                    .Build();
                EditorLog.Info($"Language {_config.CurrentLanguage} (requested {requested})");
            }
            catch (Exception ex)
            {
                // Without language packs every text shows its key, the editor still works
                EditorLog.Error($"The language packs could not be loaded (requested {requested})", ex);
            }

            return section;
        }

        public static string NameOf(ToolKind kind) => kind switch
        {
            ToolKind.Rectangle => Text.NameRectangle,
            ToolKind.Ellipse => Text.NameEllipse,
            ToolKind.Line => Text.NameLine,
            ToolKind.Arrow => Text.NameArrow,
            ToolKind.Freehand => Text.NameFreehand,
            ToolKind.Text => Text.NameText,
            ToolKind.SpeechBubble => Text.NameSpeechBubble,
            ToolKind.StepLabel => Text.NameStepLabel,
            ToolKind.Emoji => Text.NameEmoji,
            ToolKind.Highlight => Text.NameHighlight,
            ToolKind.Obfuscate => Text.NameObfuscate,
            ToolKind.CutMark => Text.NameCutMark,
            ToolKind.Crop => Text.NameCrop,
            _ => Text.NameSelect
        };

        /// <summary>
        /// The text of a value of the enums the panel offers
        /// </summary>
        public static string NameOf(object value) => value switch
        {
            ArrowHeads.None => Text.ArrowHeadsNone,
            ArrowHeads.Start => Text.ArrowHeadsStart,
            ArrowHeads.End => Text.ArrowHeadsEnd,
            ArrowHeads.Both => Text.ArrowHeadsBoth,
            HighlightMode.Text => Text.HighlightText,
            HighlightMode.Area => Text.HighlightArea,
            HighlightMode.Grayscale => Text.HighlightGrayscale,
            HighlightMode.Magnify => Text.HighlightMagnify,
            ObfuscateMode.Pixelate => Text.ObfuscatePixelate,
            ObfuscateMode.Blur => Text.ObfuscateBlur,
            EdgeStyle.None => Text.EdgeNone,
            EdgeStyle.Line => Text.EdgeLine,
            EdgeStyle.ZigZag => Text.EdgeZigZag,
            EdgeStyle.Wave => Text.EdgeWave,
            EdgeStyle.Torn => Text.EdgeTorn,
            ToolKind kind => NameOf(kind),
            _ => value?.ToString() ?? string.Empty
        };
    }
}
