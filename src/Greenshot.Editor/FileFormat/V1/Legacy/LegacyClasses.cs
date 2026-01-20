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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks.Sources;
using Greenshot.Editor.Drawing.Fields;

namespace Greenshot.Editor.FileFormat.V1.Legacy;

/*
 * This File includes all the legacy classes that are used to deserialize the old .greenshot files in file format V1.
 * Each class is a copy of the old drawable container classes.
 * Each class only contains the properties that are needed for deserialization.
 * In some classes we have to handle backward compatibility.
 */

#region Field and FieldHolder

[Serializable]
internal class LegacyField
{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value. The field will be set during deserialization. 
    private object _myValue;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value. The field will be set during deserialization. 
    public LegacyFieldType FieldType { get; set; }
    public string Scope { get; set; }

    public object Value => _myValue;

    /// <summary>
    /// For backward compatibility, to set the value during deserialization manually
    /// </summary>
    internal void ResetValue(object newValue)
    {
        _myValue = newValue;
    }
}

[Serializable]
internal class LegacyFieldType
{
    public string Name { get; set; }
}

[Serializable]
internal class LegacyFieldHolder : ISerializable
{
    public IList<LegacyField> Fields;

    protected LegacyFieldHolder(SerializationInfo info, StreamingContext context)
    {
        Fields = (IList<LegacyField>)info.GetValue("AbstractFieldHolder+fields", typeof(IList<LegacyField>));
    }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        // is only called when the object is serialized 
        // we never want to serialize this object
        throw new NotImplementedException();
    }
}

[Serializable]
internal class LegacyFieldHolderWithChildren : LegacyFieldHolder
{
    public IList<LegacyFieldHolder> Children;

    protected LegacyFieldHolderWithChildren(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        Children = (IList<LegacyFieldHolder>)info.GetValue("Children", typeof(IList<LegacyFieldHolder>));
    }
}
#endregion

#region DrawableContainer

[Serializable]
internal class LegacyDrawableContainer : LegacyFieldHolderWithChildren
{
    public int Left;
    public int Top;
    public int Width;
    public int Height;

    protected LegacyDrawableContainer(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        Left = info.GetInt32("DrawableContainer+left");
        Top = info.GetInt32("DrawableContainer+top");
        Width = info.GetInt32("DrawableContainer+width");
        Height = info.GetInt32("DrawableContainer+height");
    }
}

[Serializable]
internal class LegacyDrawableContainerList : List<LegacyDrawableContainer>
{
}

[Serializable]
internal class LegacyLineContainer : LegacyDrawableContainer
{
    protected LegacyLineContainer(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
internal class LegacyArrowContainer : LegacyDrawableContainer
{
    protected LegacyArrowContainer(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
internal class LegacyRectangleContainer : LegacyDrawableContainer
{
    protected LegacyRectangleContainer(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
internal class LegacyEllipseContainer : LegacyDrawableContainer
{
    protected LegacyEllipseContainer(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
internal class LegacyHighlightContainer : LegacyDrawableContainer
{
    protected LegacyHighlightContainer(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
internal class LegacyObfuscateContainer : LegacyDrawableContainer
{
    protected LegacyObfuscateContainer(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
internal class LegacyTextContainer : LegacyDrawableContainer
{
    public string Text;

    protected LegacyTextContainer(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        Text = info.GetString("text");
    }
}

[Serializable]
internal class LegacyImageContainer : LegacyDrawableContainer
{
    public Image Image;

    protected LegacyImageContainer(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        try
        {
            Image = (Image)info.GetValue("_image", typeof(Image));
        }
        catch (SerializationException)
        {
            // backword compatibility
            // try to serialize file version 01.02
            Image = (Image)info.GetValue("image", typeof(Image));
        }

    }
}

[Serializable]
internal class LegacyIconContainer : LegacyDrawableContainer
{
    public Icon Icon;

    protected LegacyIconContainer(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        Icon = (Icon)info.GetValue("icon", typeof(Icon));
    }
}

[Serializable]
internal class LegacySpeechbubbleContainer : LegacyDrawableContainer
{
    public string Text;
    public Point StoredTargetGripperLocation;

    protected LegacySpeechbubbleContainer(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        Text = info.GetString("TextContainer+text");
        StoredTargetGripperLocation = (Point)info.GetValue("_storedTargetGripperLocation", typeof(Point));
    }
}

[Serializable]
internal class LegacyFreehandContainer : LegacyDrawableContainer
{
    public List<Point> CapturePoints;

    protected LegacyFreehandContainer(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        CapturePoints = (List<Point>)info.GetValue("capturePoints", typeof(List<Point>));
    }
}

[Serializable]
internal class LegacyMetafileContainer : LegacyDrawableContainer
{
    public int RotationAngle;

    public Metafile Metafile;

    public MemoryStream MetafileContent;

    protected LegacyMetafileContainer(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        RotationAngle = info.GetInt32("VectorGraphicsContainer+_rotationAngle");

        Metafile = (Metafile)info.GetValue("_metafile", typeof(Metafile));

        if (Metafile != null)
        {
            ConvertWmfToPngAndReplaceMetafileAndStream(ref Metafile, ref MetafileContent);
        }
    }

    /// <summary>
    /// Workaround for serialization for file version > 1.03.
    /// For old files we need to recreate MetafileContent. We have to convert it to png, because there is no encoder for .wmf or .emf files in GDI+.
    /// </summary>
    /// <returns></returns>
    private void ConvertWmfToPngAndReplaceMetafileAndStream(
        ref Metafile metafile,
        ref MemoryStream pngStream)
    {
        if (metafile == null)
            throw new ArgumentNullException(nameof(metafile));

        int width = metafile.Width;
        int height = metafile.Height;

        // Render original metafile to bitmap
        using Bitmap bitmap = new Bitmap(width, height);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            g.DrawImage(metafile, 0, 0, width, height);
        }

        // Dispose old MemoryStream if exists
        pngStream?.Dispose();

        // Save bitmap as PNG into new MemoryStream
        pngStream = new MemoryStream();
        bitmap.Save(pngStream, ImageFormat.Png);
        pngStream.Position = 0;

        // Create new Metafile
        using Bitmap tempBitmap = new Bitmap(1, 1);
        using Graphics referenceGraphics = Graphics.FromImage(tempBitmap);
        IntPtr hdc = referenceGraphics.GetHdc();

        Metafile newMetafile = new Metafile(hdc, EmfType.EmfOnly);

        referenceGraphics.ReleaseHdc(hdc);

        using (Graphics gMetafile = Graphics.FromImage(newMetafile))
        {
            gMetafile.DrawImage(bitmap, 0, 0, width, height);
        }

        // Dispose old metafile and replace
        metafile?.Dispose();
        metafile = newMetafile;
    }
}

[Serializable]
internal class LegacySvgContainer : LegacyDrawableContainer
{
    public int RotationAngle;
    public MemoryStream SvgContent;

    protected LegacySvgContainer(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        RotationAngle = info.GetInt32("VectorGraphicsContainer+_rotationAngle");
        SvgContent = (MemoryStream)info.GetValue("_svgContent", typeof(MemoryStream));
    }
}

[Serializable]
internal class LegacyEmojiContainer : LegacyDrawableContainer
{
    public int RotationAngle;
    public string Emoji;

    protected LegacyEmojiContainer(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        RotationAngle = info.GetInt32("VectorGraphicsContainer+_rotationAngle");
        Emoji = info.GetString("_emoji");
    }
}

[Serializable]
internal class LegacyStepLabelContainer : LegacyDrawableContainer
{
    public int Number;
    public int CounterStart;

    protected LegacyStepLabelContainer(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        Number = info.GetInt32("_number");
        CounterStart = info.GetInt32("_counterStart");

        // Backward compatibility: Ensure SHADOW and LINE_THICKNESS fields exist
        if (!Fields.Any(x => x.Scope == "StepLabelContainer" && x.FieldType.Name == "LINE_THICKNESS" ))
        {
            var lineThicknessField = new LegacyField
            {
                Scope = "StepLabelContainer",
                FieldType = new LegacyFieldType { Name = "LINE_THICKNESS" }
            };
            lineThicknessField.ResetValue(0);
            Fields.Add(lineThicknessField);
        }

        if (!Fields.Any(x => x.Scope == "StepLabelContainer" && x.FieldType.Name == "SHADOW"))
        {
            var shadowField = new LegacyField
            {
                Scope = "StepLabelContainer",
                FieldType = new LegacyFieldType { Name = "SHADOW" }
            };
            shadowField.ResetValue(false);
            Fields.Add(shadowField);
        }
    }
}

#endregion

#region FilterContainer

[Serializable]
internal class LegacyHighlightFilter : LegacyFieldHolder
{
    protected LegacyHighlightFilter(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
internal class LegacyBlurFilter : LegacyFieldHolder
{
    protected LegacyBlurFilter(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
internal class LegacyBrightnessFilter : LegacyFieldHolder
{
    protected LegacyBrightnessFilter(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
internal class LegacyGrayscaleFilter : LegacyFieldHolder
{
    protected LegacyGrayscaleFilter(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
internal class LegacyMagnifierFilter : LegacyFieldHolder
{
    protected LegacyMagnifierFilter(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

[Serializable]
internal class LegacyPixelizationFilter : LegacyFieldHolder
{
    protected LegacyPixelizationFilter(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
#endregion


