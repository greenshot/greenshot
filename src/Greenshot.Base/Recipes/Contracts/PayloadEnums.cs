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

namespace Greenshot.Base.Recipes.Contracts
{
    /// <summary>
    /// Describes a step's requirement or production of a visual payload element (bitmap, surface, or OCR text).
    /// </summary>
    public enum PayloadRequirement
    {
        /// <summary>The step does not interact with this payload component.</summary>
        None,

        /// <summary>The step requires this payload component to exist before execution.</summary>
        Required,

        /// <summary>The step can use this component if available, but does not strictly require it.</summary>
        Optional,

        /// <summary>The step creates or acquires this component during execution.</summary>
        Created
    }

    /// <summary>
    /// Describes how a step mutates the visual payload.
    /// </summary>
    public enum PayloadEffect
    {
        /// <summary>The step does not modify the image bitmap or surface.</summary>
        None,

        /// <summary>The step modifies the pixel data of the image (e.g. crop, grayscale, border, resize).</summary>
        MutatesPixels,

        /// <summary>The step attaches drawable annotation elements to the surface.</summary>
        AddsAnnotations,

        /// <summary>The step completely replaces the existing image payload.</summary>
        ReplacesImage
    }
}
