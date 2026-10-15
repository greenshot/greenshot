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
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

namespace Greenshot.Editor.FileFormat.V1.Legacy;

/// <summary>
/// Identifies current container types stored as legacy rectangle records in V1 files.
/// </summary>
internal static class LegacyV1ContainerMetadata
{
    public const string Scope = "Greenshot.Editor.FileFormat.V1.ContainerMetadata";
    public const string ContainerKind = "ContainerKind";
    public const string CutMark = "CutMark";
    public const string TornEdge = "TornEdge";
    public const string Horizontal = "Horizontal";
    public const string Seed = "Seed";
    public const string Style = "Style";
    public const string ShadowDarkness = "ShadowDarkness";
    public const string ShadowSize = "ShadowSize";
    public const string ShadowOffsetX = "ShadowOffsetX";
    public const string ShadowOffsetY = "ShadowOffsetY";
    public const string StyleScope = "StyleScope";
    public const string EdgeTop = "EdgeTop";
    public const string EdgeRight = "EdgeRight";
    public const string EdgeBottom = "EdgeBottom";
    public const string EdgeLeft = "EdgeLeft";
    public const string MarginTop = "MarginTop";
    public const string MarginRight = "MarginRight";
    public const string MarginBottom = "MarginBottom";
    public const string MarginLeft = "MarginLeft";
}
