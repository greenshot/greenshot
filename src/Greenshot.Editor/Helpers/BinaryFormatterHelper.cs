/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Filters;
using log4net;
using static Greenshot.Editor.Drawing.FilterContainer;

namespace Greenshot.Editor.Helpers
{
    /// <summary>
    /// This helps to map the serialization of the old .greenshot file to the newer.
    /// It also prevents misuse.
    /// </summary>
    internal class BinaryFormatterHelper : SerializationBinder
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(BinaryFormatterHelper));
        private static readonly IDictionary<string, Type> TypeMapper = new Dictionary<string, Type>
        {
            {"System.Guid",typeof(Guid) },
            {"System.Drawing.Rectangle",typeof(System.Drawing.Rectangle) },
            {"System.Drawing.Point",typeof(System.Drawing.Point) },
            {"System.Drawing.Color",typeof(System.Drawing.Color) },
            {"System.Drawing.Bitmap",typeof(System.Drawing.Bitmap) },
            {"System.Drawing.Icon",typeof(System.Drawing.Icon) },
            {"System.Drawing.Size",typeof(System.Drawing.Size) },
            {"System.IO.MemoryStream",typeof(System.IO.MemoryStream) },
            {"System.Drawing.StringAlignment",typeof(System.Drawing.StringAlignment) },
            {"System.Collections.Generic.List`1[[Greenshot.Base.Interfaces.Drawing.IFieldHolder", typeof(List<IFieldHolder>)},
            {"System.Collections.Generic.List`1[[Greenshot.Base.Interfaces.Drawing.IField", typeof(List<IField>)},
            {"System.Collections.Generic.List`1[[System.Drawing.Point", typeof(List<System.Drawing.Point>)},
            {"Greenshot.Editor.Drawing.ArrowContainer", typeof(ArrowContainer) },
            {"Greenshot.Editor.Drawing.LineContainer", typeof(LineContainer) },
            {"Greenshot.Editor.Drawing.TextContainer", typeof(TextContainer) },
            {"Greenshot.Editor.Drawing.SpeechbubbleContainer", typeof(SpeechbubbleContainer) },
            {"Greenshot.Editor.Drawing.RectangleContainer", typeof(RectangleContainer) },
            {"Greenshot.Editor.Drawing.EllipseContainer", typeof(EllipseContainer) },
            {"Greenshot.Editor.Drawing.FreehandContainer", typeof(FreehandContainer) },
            {"Greenshot.Editor.Drawing.HighlightContainer", typeof(HighlightContainer) },
            {"Greenshot.Editor.Drawing.IconContainer", typeof(IconContainer) },
            {"Greenshot.Editor.Drawing.ObfuscateContainer", typeof(ObfuscateContainer) },
            {"Greenshot.Editor.Drawing.StepLabelContainer", typeof(StepLabelContainer) },
            {"Greenshot.Editor.Drawing.SvgContainer", typeof(SvgContainer) },
            {"Greenshot.Editor.Drawing.VectorGraphicsContainer", typeof(VectorGraphicsContainer) },
            {"Greenshot.Editor.Drawing.MetafileContainer", typeof(MetafileContainer) },
            {"Greenshot.Editor.Drawing.ImageContainer", typeof(ImageContainer) },
            {"Greenshot.Editor.Drawing.FilterContainer", typeof(FilterContainer) },
            {"Greenshot.Editor.Drawing.DrawableContainer", typeof(DrawableContainer) },
            {"Greenshot.Editor.Drawing.DrawableContainerList", typeof(DrawableContainerList) },
            {"Greenshot.Editor.Drawing.CursorContainer", typeof(CursorContainer) },
            {"Greenshot.Editor.Drawing.Filters.HighlightFilter", typeof(HighlightFilter) },
            {"Greenshot.Editor.Drawing.Filters.GrayscaleFilter", typeof(GrayscaleFilter) },
            {"Greenshot.Editor.Drawing.Filters.MagnifierFilter", typeof(MagnifierFilter) },
            {"Greenshot.Editor.Drawing.Filters.BrightnessFilter", typeof(BrightnessFilter) },
            {"Greenshot.Editor.Drawing.Filters.BlurFilter", typeof(BlurFilter) },
            {"Greenshot.Editor.Drawing.Filters.PixelizationFilter", typeof(PixelizationFilter) },
            {"Greenshot.Base.Interfaces.Drawing.IDrawableContainer", typeof(IDrawableContainer) },
            {"Greenshot.Base.Interfaces.Drawing.EditStatus", typeof(EditStatus) },
            {"Greenshot.Base.Interfaces.Drawing.IFieldHolder", typeof(IFieldHolder) },
            {"Greenshot.Base.Interfaces.Drawing.IField", typeof(IField) },
            {"Greenshot.Base.Interfaces.Drawing.FieldFlag", typeof(FieldFlag) },
            {"Greenshot.Editor.Drawing.Fields.Field", typeof(Field) },
            {"Greenshot.Editor.Drawing.Fields.FieldType", typeof(FieldType) },
            {"Greenshot.Editor.Drawing.FilterContainer+PreparedFilter", typeof(PreparedFilter) },
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
                LOG.Info($"Mapped {assemblyName} - {typeName} to {returnType.FullName}");
                return returnType;
            }
            LOG.Warn($"Unexpected Greenshot type in .greenshot file detected, maybe vulnerability attack created with ysoserial? Suspicious type: {assemblyName} - {typeName}");
            throw new SecurityAccessDeniedException($"Suspicious type in .greenshot file: {assemblyName} - {typeName}");
        }
    }
}
