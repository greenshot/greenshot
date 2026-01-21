/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2025 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using MessagePack;
using System.Text.Json.Serialization;

namespace Greenshot.Editor.FileFormat.Dto.Fields;

/// <summary>
/// Represents a field value that stores color information.
/// </summary>
[MessagePackObject(AllowPrivate = true)]
// This needs to be a partial class to support private properties with MessagePack serialization
public sealed partial class ColorFieldValueDto : FieldValueDto
{
    //TODO maybe store in 4 separate fields for better readability in JSON
    [Key(100)]
    public int Argb { get; set; } // Store Color as an ARGB integer

    [IgnoreMember]
    [JsonIgnore]
    public Color Value
    {
        get
        {
            return Color.FromArgb(Argb);
        }
        set
        {
            Argb = value.ToArgb();
        }
    }

    public override object GetValue()
    {
        return Value;
    }
}
