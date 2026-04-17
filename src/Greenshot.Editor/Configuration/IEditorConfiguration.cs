/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Runtime.Serialization;
using Dapplo.Ini.Attributes;
using Dapplo.Ini.Interfaces;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;
using Greenshot.Base.Effects;

namespace Greenshot.Editor.Configuration
{
    [IniSection("Editor")]
    [Description("Greenshot editor configuration")]
    public interface IEditorConfiguration : IIniSection, IAfterLoad
    {
        [Description("Last used colors")]
        List<Color> RecentColors { get; set; }

        [DataMember(Name = "LastFieldValue")]
        [Description("Field values, make sure the last used settings are reused")]
        Dictionary<string, object> LastUsedFieldValues { get; set; }

        [Description("Match the editor window size to the capture")]
        [DefaultValue(true)]
        bool MatchSizeToCapture { get; set; }

        [Description("Placement flags")]
        [DefaultValue(0)]
        WindowPlacementFlags WindowPlacementFlags { get; set; }

        [Description("Show command")]
        [DefaultValue("Normal")]
        ShowWindowCommands ShowWindowCommand { get; set; }

        [Description("Position of minimized window")]
        [DefaultValue("-1,-1")]
        NativePoint WindowMinPosition { get; set; }

        [Description("Position of maximized window")]
        [DefaultValue("-1,-1")]
        NativePoint WindowMaxPosition { get; set; }

        [Description("Position of normal window")]
        [DefaultValue("100,100,400,400")]
        NativeRect WindowNormalPosition { get; set; }

        [Description("Reuse already open editor")]
        [DefaultValue(false)]
        bool ReuseEditor { get; set; }

        [Description("The smaller this number, the less smoothing is used. Decrease for detailed drawing, e.g. when using a pen. Increase for smoother lines. e.g. when you want to draw a smooth line. Minimal value is 1, max is 2147483647.")]
        [DefaultValue(3)]
        [Range(1, int.MaxValue, ErrorMessage = "FreehandSensitivity must be at least 1.")]
        int FreehandSensitivity { get; set; }

        [Description("Suppressed the 'do you want to save' dialog when closing the editor.")]
        [DefaultValue(false)]
        bool SuppressSaveDialogAtClose { get; set; }

        [Description("Settings for the drop shadow effect.")]
        DropShadowEffect DropShadowEffectSettings { get; set; }

        [Description("Settings for the torn edge effect.")]
        TornEdgeEffect TornEdgeEffectSettings { get; set; }

        [Description("The size for the editor when it's opened without a capture")]
        [DefaultValue("500,500")]
        NativeSize DefaultEditorSize { get; set; }

        [Description("Last used search pattern for text obfuscation")]
        [DefaultValue("")]
        string TextObfuscationSearchPattern { get; set; }

        [Description("Use regular expression for text obfuscation search")]
        [DefaultValue(false)]
        bool TextObfuscationUseRegex { get; set; }

        [Description("Case sensitive search for text obfuscation")]
        [DefaultValue(false)]
        bool TextObfuscationCaseSensitive { get; set; }

        [Description("Search scope for text obfuscation: 0=Words, 1=Lines")]
        [DefaultValue(0)]
        int TextObfuscationSearchScope { get; set; }

        [Description("Effect to apply for text redaction: BLUR, PIXELIZE, TEXT_HIGHTLIGHT, MAGNIFICATION")]
        [DefaultValue("PIXELIZE")]
        string TextObfuscationEffect { get; set; }

        [Description("Horizontal percentage to grow matched rectangles for text redaction")]
        [DefaultValue(10)]
        int TextObfuscationPaddingHorizontal { get; set; }

        [Description("Vertical percentage to grow matched rectangles for text redaction")]
        [DefaultValue(20)]
        int TextObfuscationPaddingVertical { get; set; }

        [Description("Horizontal offset in pixels for matched rectangles")]
        [DefaultValue(0)]
        int TextObfuscationOffsetHorizontal { get; set; }

        [Description("Vertical offset in pixels for matched rectangles")]
        [DefaultValue(-5)]
        int TextObfuscationOffsetVertical { get; set; }

        [DefaultValue("🙂")]
        [Description("Last used emoji")]
        string RecentEmoji { get; set; }
    }
}
