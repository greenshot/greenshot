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
using System.Linq;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Recipes.Pipeline;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// The HDR file a recipe saves (the HdrFormat parameter of its SaveFile step), see HdrImage and IHdrImageEncoder
    /// </summary>
    public static class HdrOutput
    {
        /// <summary>
        /// The parameter of the SaveFile step
        /// </summary>
        public const string ParameterName = "HdrFormat";

        /// <summary>
        /// The values of the parameter: None saves no HDR file, Auto and Jxr save JPEG XR
        /// </summary>
        public static readonly string[] AllowedValues = { None, Auto, Jxr };

        public const string None = "None";
        public const string Auto = "Auto";
        public const string Jxr = "Jxr";

        /// <summary>
        /// True when a SaveFile step of the recipe saves an HDR file: the capture keeps its HDR image then (they cost memory, 8 bytes per pixel)
        /// </summary>
        public static bool IsRequested(CaptureRecipe recipe) =>
            recipe?.Nodes?.Any(node => node != null
                && string.Equals(node.StepType, WellKnownStepTypes.SaveFile, StringComparison.OrdinalIgnoreCase)
                && IsHdrFormat(node.GetParameter<string>(ParameterName))) == true;

        /// <summary>
        /// True when the context's recipe saves an HDR file
        /// </summary>
        public static bool IsRequested(CaptureFlowContext context) => IsRequested(context?.Recipe);

        /// <summary>
        /// True for a value which saves an HDR file (not empty, not None)
        /// </summary>
        public static bool IsHdrFormat(string value) =>
            !string.IsNullOrWhiteSpace(value) && !string.Equals(value, None, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// The file format of an HdrFormat value: jxr or null (None). JPEG XR is part of Windows and its brightness (1.0 = 80 nits) is the same in every viewer.
        /// JPEG XL is left out for now: the Windows encoder (JPEG XL Image Extension) does not write the intensity target, viewers outside Windows show its files too bright.
        /// </summary>
        /// <param name="hdrFormat">The HdrFormat parameter</param>
        public static string ResolveFormatId(string hdrFormat) => IsHdrFormat(hdrFormat) ? WellKnownFileFormats.Jxr : null;
    }
}
