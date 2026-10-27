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

#nullable disable

using System;
using System.Text.Json.Serialization;

namespace Greenshot.Linux.Recipes
{
    /// <summary>
    /// Represents an interactive button / option presented to the user during a UserPrompt step execution.
    /// </summary>
    public class PromptChoice
    {
        [JsonPropertyName("Key"), JsonRequired]
        public string Key { get; set; } = "Yes";

        [JsonPropertyName("Label")]
        public string Label { get; set; } = "Yes, Proceed";

        [JsonPropertyName("Style"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Style { get; set; } = "Primary"; // "Primary", "Secondary", "Danger"

        [JsonPropertyName("IsDefault"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsDefault { get; set; }

        [JsonPropertyName("IsCancel"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsCancel { get; set; }

        public PromptChoice()
        {
        }

        public PromptChoice(string key, string label, string style = "Primary", bool isDefault = false, bool isCancel = false)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Label = label ?? key;
            Style = style ?? "Primary";
            IsDefault = isDefault;
            IsCancel = isCancel;
        }

        public override string ToString() => $"{Label} [{Key}]";
    }
}
