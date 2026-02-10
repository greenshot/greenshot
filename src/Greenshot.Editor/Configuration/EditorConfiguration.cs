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

using System;
using System.Collections.Generic;
using System.Drawing;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using Greenshot.Base.Effects;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;

namespace Greenshot.Editor.Configuration
{
    /// <summary>
    /// Description of CoreConfiguration.
    /// </summary>
    [IniSection("Editor", Description = "Greenshot editor configuration")]
    public class EditorConfiguration : IniSection
    {
        [IniProperty("RecentColors", Separator = "|", Description = "Last used colors")]
        public List<Color> RecentColors { get; set; }

        [IniProperty("LastFieldValue", Separator = "|", Description = "Field values, make sure the last used settings are re-used")]
        public Dictionary<string, object> LastUsedFieldValues { get; set; }

        [IniProperty("MatchSizeToCapture", Description = "Match the editor window size to the capture", DefaultValue = "True")]
        public bool MatchSizeToCapture { get; set; }

        [IniProperty("WindowPlacementFlags", Description = "Placement flags", DefaultValue = "0")]
        public WindowPlacementFlags WindowPlacementFlags { get; set; }

        [IniProperty("WindowShowCommand", Description = "Show command", DefaultValue = "Normal")]
        public ShowWindowCommands ShowWindowCommand { get; set; }

        [IniProperty("WindowMinPosition", Description = "Position of minimized window", DefaultValue = "-1,-1")]
        public NativePoint WindowMinPosition { get; set; }

        [IniProperty("WindowMaxPosition", Description = "Position of maximized window", DefaultValue = "-1,-1")]
        public NativePoint WindowMaxPosition { get; set; }

        [IniProperty("WindowNormalPosition", Description = "Position of normal window", DefaultValue = "100,100,400,400")]
        public NativeRect WindowNormalPosition { get; set; }

        [IniProperty("ReuseEditor", Description = "Reuse already open editor", DefaultValue = "false")]
        public bool ReuseEditor { get; set; }

        [IniProperty("FreehandSensitivity", Description = "The smaller this number, the less smoothing is used. Decrease for detailed drawing, e.g. when using a pen. Increase for smoother lines. e.g. when you want to draw a smooth line. Minimal value is 1, max is 2147483647.", DefaultValue = "3")]
        public int FreehandSensitivity { get; set; }

        [IniProperty("SuppressSaveDialogAtClose", Description = "Suppressed the 'do you want to save' dialog when closing the editor.", DefaultValue = "False")]
        public bool SuppressSaveDialogAtClose { get; set; }

        [IniProperty("DropShadowEffectSettings", Description = "Settings for the drop shadow effect.")]
        public DropShadowEffect DropShadowEffectSettings { get; set; }

        [IniProperty("TornEdgeEffectSettings", Description = "Settings for the torn edge effect.")]
        public TornEdgeEffect TornEdgeEffectSettings { get; set; }

        [IniProperty("DefaultEditorSize", Description = "The size for the editor when it's opened without a capture", DefaultValue = "500,500")]
        public NativeSize DefaultEditorSize { get; set; }

        [IniProperty("TextObfuscationSearchPattern", Description = "Last used search pattern for text obfuscation", DefaultValue = "")]
        public string TextObfuscationSearchPattern { get; set; }

        [IniProperty("TextObfuscationUseRegex", Description = "Use regular expression for text obfuscation search", DefaultValue = "false")]
        public bool TextObfuscationUseRegex { get; set; }

        [IniProperty("TextObfuscationCaseSensitive", Description = "Case sensitive search for text obfuscation", DefaultValue = "false")]
        public bool TextObfuscationCaseSensitive { get; set; }

        [IniProperty("TextObfuscationSearchScope", Description = "Search scope for text obfuscation: 0=Words, 1=Lines", DefaultValue = "0")]
        public int TextObfuscationSearchScope { get; set; }

        [IniProperty("TextObfuscationEffect", Description = "Effect to apply for text obfuscation: BLUR, PIXELIZE, TEXT_HIGHTLIGHT, MAGNIFICATION", DefaultValue = "PIXELIZE")]
        public string TextObfuscationEffect { get; set; }


        public override void AfterLoad()
        {
            base.AfterLoad();
            RecentColors ??= new List<Color>();

            if (FreehandSensitivity < 1)
            {
                FreehandSensitivity = 1;
            }
        }

        /// <param name="requestingType">Type of the class for which to create the field</param>
        /// <param name="fieldType">FieldType of the field to construct</param>
        /// <param name="preferredDefaultValue"></param>
        /// <returns>a new Field of the given fieldType, with the scope of it's value being restricted to the Type scope</returns>
        public IField CreateField(Type requestingType, IFieldType fieldType, object preferredDefaultValue)
        {
            string requestingTypeName = requestingType.Name;
            string requestedField = requestingTypeName + "." + fieldType.Name;
            object fieldValue = preferredDefaultValue;

            // Check if the configuration exists
            if (LastUsedFieldValues == null)
            {
                LastUsedFieldValues = new Dictionary<string, object>();
            }

            // Check if settings for the requesting type exist, if not create!
            if (LastUsedFieldValues.ContainsKey(requestedField))
            {
                // Check if a value is set (not null)!
                if (LastUsedFieldValues[requestedField] != null)
                {
                    fieldValue = LastUsedFieldValues[requestedField];
                }
                else
                {
                    // Overwrite null value
                    LastUsedFieldValues[requestedField] = fieldValue;
                }
            }
            else
            {
                LastUsedFieldValues.Add(requestedField, fieldValue);
            }

            return new Field(fieldType, requestingType)
            {
                Value = fieldValue
            };
        }

        public void UpdateLastFieldValue(IField field)
        {
            string requestedField = field.Scope + "." + field.FieldType.Name;
            // Check if the configuration exists
            LastUsedFieldValues ??= new Dictionary<string, object>();

            // check if settings for the requesting type exist, if not create!
            if (LastUsedFieldValues.ContainsKey(requestedField))
            {
                LastUsedFieldValues[requestedField] = field.Value;
            }
            else
            {
                LastUsedFieldValues.Add(requestedField, field.Value);
            }
        }

        public void ResetEditorPlacement()
        {
            WindowNormalPosition = new NativeRect(100, 100, 400, 400);
            WindowMaxPosition = new NativePoint(-1, -1);
            WindowMinPosition = new NativePoint(-1, -1);
            WindowPlacementFlags = 0;
            ShowWindowCommand = ShowWindowCommands.Normal;
        }

        public WindowPlacement GetEditorPlacement()
        {
            WindowPlacement placement = WindowPlacement.Create();
            placement.NormalPosition = WindowNormalPosition;
            placement.MaxPosition = WindowMaxPosition;
            placement.MinPosition = WindowMinPosition;
            placement.ShowCmd = ShowWindowCommand;
            placement.Flags = WindowPlacementFlags;
            return placement;
        }

        public void SetEditorPlacement(WindowPlacement placement)
        {
            WindowNormalPosition = placement.NormalPosition;
            WindowMaxPosition = placement.MaxPosition;
            WindowMinPosition = placement.MinPosition;
            ShowWindowCommand = placement.ShowCmd;
            WindowPlacementFlags = placement.Flags;
        }
    }
}