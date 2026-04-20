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
using System.Runtime.Serialization;
using System.ServiceModel.Security;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Filters;
using log4net;
using static Greenshot.Editor.Drawing.ArrowContainer;
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
            // Used specifically for the .ini configuration (besides the ones already defined)
            {"System.Int32",typeof(int) },
            {"System.Single",typeof(float) },
            {"System.Boolean",typeof(bool) },
            {"System.String",typeof(string) },
            // End ini configuration
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
            {"Greenshot.Editor.Drawing.ArrowContainer+ArrowHeadCombination", typeof(ArrowContainer.ArrowHeadCombination) },
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
            {"Greenshot.Editor.Drawing.Emoji.EmojiContainer", typeof(EmojiContainer) },
            {"Greenshot.Editor.Drawing.VectorGraphicsContainer", typeof(VectorGraphicsContainer) },
            {"Greenshot.Editor.Drawing.MetafileContainer", typeof(MetafileContainer) },
            {"Greenshot.Editor.Drawing.ImageContainer", typeof(ImageContainer) },
            {"Greenshot.Editor.Drawing.FilterContainer", typeof(FilterContainer) },
            {"Greenshot.Editor.Drawing.DrawableContainer", typeof(DrawableContainer) },
            {"Greenshot.Editor.Drawing.DrawableContainerList", typeof(DrawableContainerList) },
            {"Greenshot.Editor.Drawing.CursorContainer", typeof(CursorContainer) },
            {"Greenshot.Editor.Drawing.CursorContainer+CaptureCursorSerializationWrapper", typeof(CursorContainer.CaptureCursorSerializationWrapper) },
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
        /// Try to match the type for the given type name, this is used to check if the type is allowed to be deserialized, and to map old types to new ones. 
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="type"></param>
        /// <returns>bool true if the mapping was possible</returns>
        public static bool TryGetType(string typeName, out Type type)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                type = null;
                return false;
            }
            string comparingTypeName = typeName;
            var typeNameCommaLocation = typeName.IndexOf(",");
            if (typeNameCommaLocation > 0)
            {
                comparingTypeName = typeName.Substring(0, typeNameCommaLocation);
            }

            // Correct wrong types (because of refactoring) to the correct ones, this is needed to load old .greenshot files
            comparingTypeName = comparingTypeName.Replace("Greenshot.Drawing", "Greenshot.Editor.Drawing");
            comparingTypeName = comparingTypeName.Replace("Greenshot.Plugin.Drawing", "Greenshot.Base.Interfaces.Drawing");
            comparingTypeName = comparingTypeName.Replace("GreenshotPlugin.Interfaces.Drawing", "Greenshot.Base.Interfaces.Drawing");
            comparingTypeName = comparingTypeName.Replace("Greenshot.Drawing.Fields", "Greenshot.Editor.Drawing.Fields");
            comparingTypeName = comparingTypeName.Replace("Greenshot.Drawing.Filters", "Greenshot.Editor.Drawing.Filters");
            return TypeMapper.TryGetValue(comparingTypeName, out type);
        }

        /// <summary>
        /// Do the type mapping
        /// </summary>
        /// <param name="assemblyName">Assembly for the type that was serialized</param>
        /// <param name="typeName">Type that was serialized</param>
        /// <returns>Type which was mapped</returns>
        /// <exception cref="SecurityAccessDeniedException">If something smells fishy</exception>
        public override Type BindToType(string assemblyName, string typeName)
        {
            if (TryGetType(typeName, out var returnType))
            {
                LOG.Info($"Mapped {assemblyName} - {typeName} to {returnType.FullName}");
                return returnType;
            }
            LOG.Warn($"Unexpected Greenshot type in .greenshot file detected, maybe vulnerability attack created with ysoserial? Suspicious type: {assemblyName} - {typeName}");
            throw new SecurityAccessDeniedException($"Suspicious type in .greenshot file: {assemblyName} - {typeName}");
        }
    }
}
