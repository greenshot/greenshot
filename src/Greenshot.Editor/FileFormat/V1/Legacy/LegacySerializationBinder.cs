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
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel.Security;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using log4net;

namespace Greenshot.Editor.FileFormat.V1.Legacy;

/// <summary>
/// This helps to map the serialization of the old .greenshot file to the legacy container.
/// It also prevents misuse like ysoserial attacks, by throwing an exception if a type is not mapped.
/// </summary>
internal sealed class LegacySerializationBinder : SerializationBinder
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(LegacySerializationBinder));
    private static readonly IDictionary<string, Type> TypeMapper = new Dictionary<string, Type>
        {
            // system types
            {"System.Guid",typeof(Guid) },
            {"System.Drawing.Rectangle",typeof(System.Drawing.Rectangle) },
            {"System.Drawing.Point",typeof(System.Drawing.Point) },
            {"System.Drawing.Color",typeof(System.Drawing.Color) },
            {"System.Drawing.Bitmap",typeof(System.Drawing.Bitmap) },
            {"System.Drawing.Imaging.Metafile",typeof(System.Drawing.Imaging.Metafile) },
            {"System.Drawing.Icon",typeof(System.Drawing.Icon) },
            {"System.Drawing.Size",typeof(System.Drawing.Size) },
            {"System.IO.MemoryStream",typeof(System.IO.MemoryStream) },
            {"System.Drawing.StringAlignment",typeof(System.Drawing.StringAlignment) },
            {"System.Collections.Generic.List`1[[System.Drawing.Point", typeof(List<System.Drawing.Point>)},

            // legacy types
            {"System.Collections.Generic.List`1[[Greenshot.Base.Interfaces.Drawing.IFieldHolder", typeof(List<LegacyFieldHolder>)},
            {"System.Collections.Generic.List`1[[Greenshot.Base.Interfaces.Drawing.IField", typeof(List<LegacyField>)},
            {"Greenshot.Editor.Drawing.ArrowContainer", typeof(LegacyArrowContainer) },
            {"Greenshot.Editor.Drawing.LineContainer", typeof(LegacyLineContainer) },
            {"Greenshot.Editor.Drawing.TextContainer", typeof(LegacyTextContainer) },
            {"Greenshot.Editor.Drawing.SpeechbubbleContainer", typeof(LegacySpeechbubbleContainer) },
            {"Greenshot.Editor.Drawing.RectangleContainer", typeof(LegacyRectangleContainer) },
            {"Greenshot.Editor.Drawing.EllipseContainer", typeof(LegacyEllipseContainer) },
            {"Greenshot.Editor.Drawing.FreehandContainer", typeof(LegacyFreehandContainer) },
            {"Greenshot.Editor.Drawing.HighlightContainer", typeof(LegacyHighlightContainer) },
            {"Greenshot.Editor.Drawing.IconContainer", typeof(LegacyIconContainer) },
            {"Greenshot.Editor.Drawing.ObfuscateContainer", typeof(LegacyObfuscateContainer) },
            {"Greenshot.Editor.Drawing.StepLabelContainer", typeof(LegacyStepLabelContainer) },
            {"Greenshot.Editor.Drawing.SvgContainer", typeof(LegacySvgContainer) },
            {"Greenshot.Editor.Drawing.Emoji.EmojiContainer", typeof(LegacyEmojiContainer) },
            {"Greenshot.Editor.Drawing.MetafileContainer", typeof(LegacyMetafileContainer) },
            {"Greenshot.Editor.Drawing.ImageContainer", typeof(LegacyImageContainer) },
            {"Greenshot.Editor.Drawing.DrawableContainer", typeof(LegacyDrawableContainer) },
            {"Greenshot.Editor.Drawing.DrawableContainerList", typeof(LegacyDrawableContainerList) },
            {"Greenshot.Editor.Drawing.Filters.HighlightFilter", typeof(LegacyHighlightFilter) },
            {"Greenshot.Editor.Drawing.Filters.GrayscaleFilter", typeof(LegacyGrayscaleFilter) },
            {"Greenshot.Editor.Drawing.Filters.MagnifierFilter", typeof(LegacyMagnifierFilter) },
            {"Greenshot.Editor.Drawing.Filters.BrightnessFilter", typeof(LegacyBrightnessFilter) },
            {"Greenshot.Editor.Drawing.Filters.BlurFilter", typeof(LegacyBlurFilter) },
            {"Greenshot.Editor.Drawing.Filters.PixelizationFilter", typeof(LegacyPixelizationFilter) },
            {"Greenshot.Base.Interfaces.Drawing.IDrawableContainer", typeof(LegacyDrawableContainer) },
            {"Greenshot.Base.Interfaces.Drawing.IFieldHolder", typeof(LegacyFieldHolder) },
            {"Greenshot.Base.Interfaces.Drawing.IField", typeof(LegacyField) },
            {"Greenshot.Editor.Drawing.Fields.Field", typeof(LegacyField) },
            {"Greenshot.Editor.Drawing.Fields.FieldType", typeof(LegacyFieldType) },
            {"Greenshot.Editor.Drawing.Fields.AbstractFieldHolder", typeof(LegacyFieldHolder) },

            // oiginal types, no wapper needed
            {"Greenshot.Base.Interfaces.Drawing.FieldFlag", typeof(FieldFlag) },
            {"Greenshot.Base.Interfaces.Drawing.EditStatus", typeof(EditStatus) },
            {"Greenshot.Editor.Drawing.FilterContainer+PreparedFilter", typeof(FilterContainer.PreparedFilter) },
            {"Greenshot.Editor.Drawing.ArrowContainer+ArrowHeadCombination", typeof(ArrowContainer.ArrowHeadCombination) },
        };

    /// <summary>
    /// Do the type mapping
    /// </summary>
    /// <param name="assemblyName">Assembly for the type that was serialized</param>
    /// <param name="typeName">Type that was serialized</param>
    /// <returns>Type which was mapped</returns>
    /// <exception cref="SecurityAccessDeniedException">If something smells fishy</exception>
    public override Type BindToType(string assemblyName, string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return null;
        }
        var typeNameCommaLocation = typeName.IndexOf(",");
        var comparingTypeName = typeName.Substring(0, typeNameCommaLocation > 0 ? typeNameCommaLocation : typeName.Length);

        // Correct wrong types
        comparingTypeName = comparingTypeName.Replace("Greenshot.Drawing", "Greenshot.Editor.Drawing");
        comparingTypeName = comparingTypeName.Replace("Greenshot.Plugin.Drawing", "Greenshot.Base.Interfaces.Drawing");
        comparingTypeName = comparingTypeName.Replace("GreenshotPlugin.Interfaces.Drawing", "Greenshot.Base.Interfaces.Drawing");
        comparingTypeName = comparingTypeName.Replace("Greenshot.Drawing.Fields", "Greenshot.Editor.Drawing.Fields");
        comparingTypeName = comparingTypeName.Replace("Greenshot.Drawing.Filters", "Greenshot.Editor.Drawing.Filters");

        if (TypeMapper.TryGetValue(comparingTypeName, out var returnType))
        {
            Log.Info($"Mapped {assemblyName} - {typeName} to {returnType.FullName}");
            return returnType;
        }
        Log.Warn($"Unexpected Greenshot type in .greenshot file detected, maybe vulnerability attack created with ysoserial? Suspicious type: {assemblyName} - {typeName}");
        throw new SecurityAccessDeniedException($"Suspicious type in .greenshot file: {assemblyName} - {typeName}");
    }
}