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
using System.Text.Json.Serialization;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.FileFormat.Dto.Container;

/// <summary>
/// Data transfer object to serialize <see cref="CursorContainer"/> objects.
/// </summary>
public sealed class CursorContainerDto : DrawableContainerDto
{
    public int HotspotX { get; set; }
    public int HotspotY { get; set; }
    public int CursorWidth { get; set; }
    public int CursorHeight { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [GreenshotImageData(pathPropertyName: nameof(ColorLayerPath), staticExtension: "png")]
    public byte[] ColorLayer { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [GreenshotImagePath(nameof(ColorLayer))]
    public string ColorLayerPath { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [GreenshotImageData(pathPropertyName: nameof(MaskLayerPath), staticExtension: "png")]
    public byte[] MaskLayer { get; set; }
        
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [GreenshotImagePath(nameof(MaskLayer))]
    public string MaskLayerPath { get; set; }
}
