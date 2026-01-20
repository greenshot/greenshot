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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormat.Dto.Container;
using Greenshot.Editor.FileFormat.Dto.Fields;
using log4net;

namespace Greenshot.Editor.FileFormat.Dto;


/// <summary>
/// Provides methods to convert various Data Transfer Object (DTO) types into their corresponding domain types, mainly drawable container and fields.
/// </summary>
/// <remarks>This class contains a collection of static ToDomain() methods that handle the transformation</remarks>
public static class ConvertDtoToDomain
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(ConvertDtoToDomain));

    /// <summary>
    /// Checks if the provided parent surface is null and creates a new one if it is.
    /// </summary>
    private static ISurface CheckOrCreateParentSurface( ISurface parentSurface) => parentSurface ?? SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();

    public static GreenshotFile ToDomain(GreenshotFileDto dto)
    {
        if (dto == null) return null;

        return new GreenshotFile
        {
            ContainerList = ToDomain(dto.ContainerList),
            Image = ByteArrayToImage(dto.Image),
            RenderedImage = ByteArrayToImage(dto.RenderedImage),
            SchemaVersion = dto.SchemaVersion,
            FormatVersion = dto.FormatVersion
        };
    }

    public static GreenshotTemplate ToDomain(GreenshotTemplateDto dto)
    {
        if (dto == null) return null;

        return new GreenshotTemplate
        {
            ContainerList = ToDomain(dto.ContainerList),
            SchemaVersion = dto.SchemaVersion,
            FormatVersion = dto.FormatVersion
        };
    }

    public static DrawableContainerList ToDomain(DrawableContainerListDto dto)
    {
        if (dto == null) return null;

        // new Surface for all Container
        ISurface parentSurface = CheckOrCreateParentSurface(null);

        var domainList = new DrawableContainerList();
        foreach (var item in dto.ContainerList)
        {
            domainList.Add(ToDomain(item, parentSurface));
        }
        return domainList;
    }

    public static IDrawableContainer ToDomain(DrawableContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);

        return dto switch
        {
            ImageContainerDto imageContainerDto => ToDomain(imageContainerDto, parentSurface),
            ArrowContainerDto arrowContainerDto => ToDomain(arrowContainerDto, parentSurface),
            LineContainerDto lineContainerDto => ToDomain(lineContainerDto, parentSurface),
            RectangleContainerDto rectangleContainerDto => ToDomain(rectangleContainerDto, parentSurface),
            IconContainerDto iconContainerDto => ToDomain(iconContainerDto, parentSurface),
            StepLabelContainerDto stepLabelContainerDto => ToDomain(stepLabelContainerDto, parentSurface),
            EllipseContainerDto ellipseContainerDto => ToDomain(ellipseContainerDto, parentSurface),
            HighlightContainerDto highlightContainerDto => ToDomain(highlightContainerDto, parentSurface),
            ObfuscateContainerDto obfuscateContainerDto => ToDomain(obfuscateContainerDto, parentSurface),
            CursorContainerDto cursorContainerDto => ToDomain(cursorContainerDto, parentSurface),
            FreehandContainerDto freehandContainerDto => ToDomain(freehandContainerDto, parentSurface),
            TextContainerDto textContainerDto => ToDomain(textContainerDto, parentSurface),
            SpeechbubbleContainerDto speechbubbleContainerDto => ToDomain(speechbubbleContainerDto, parentSurface),
            MetafileContainerDto metafileContainerDto => ToDomain(metafileContainerDto, parentSurface),
            SvgContainerDto svgContainerDto => ToDomain(svgContainerDto, parentSurface),
            EmojiContainerDto emojiContainerDto => ToDomain(emojiContainerDto, parentSurface),
            _ => throw new ArgumentException($"Unsupported IDrawableContainerDto type: {dto.GetType()}")
        };
    }

    public static ImageContainer ToDomain(ImageContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);

        var domain = new ImageContainer(parentSurface);
        if (dto.Image !=null)
        {
            // The image setter recalculates the position and dimensions 
            // this is no problem, because we correct them later in InitDrawableContainer()
            domain.Image = ByteArrayToImage(dto.Image);
        }else
        {
            Log.Warn("ImageContainerDto contains no image. Creating an empty image with tranparent background as a replacement.");
            // If no image is provided, we create an empty image with the specified dimensions
            domain.Image = ImageHelper.CreateEmpty(Math.Max(dto.Width,50), Math.Max(dto.Height,50), PixelFormat.Format32bppArgb, Color.Transparent);
        }

        return InitDrawableContainer(domain, dto);
    }

    public static MetafileContainer ToDomain(MetafileContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);
        
        var domain = new MetafileContainer(new MemoryStream(dto.MetafileData), parentSurface);
        domain.RotationAngle = dto.RotationAngle;

        return InitDrawableContainer(domain, dto);
    }

    public static SvgContainer ToDomain(SvgContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);
        
        var domain = new SvgContainer(new MemoryStream(dto.SvgData), parentSurface);
        domain.RotationAngle = dto.RotationAngle;

        return InitDrawableContainer(domain, dto);
    }

    public static EmojiContainer ToDomain(EmojiContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);

        var domain = new EmojiContainer((Surface)parentSurface, dto.Emoji);
        domain.RotationAngle = dto.RotationAngle;

        return InitDrawableContainer(domain, dto);
    }

    public static LineContainer ToDomain(LineContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);
        
        var domain = new LineContainer(parentSurface);
        return InitDrawableContainer(domain, dto);
    }

    public static RectangleContainer ToDomain(RectangleContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);
        
        var domain = new RectangleContainer(parentSurface);
        return InitDrawableContainer(domain, dto);
    }

    public static TextContainer ToDomain(TextContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);
        
        var domain = new TextContainer(parentSurface)
        {
            Text = dto.Text
        };
        return InitDrawableContainer(domain, dto);
    }

    public static SpeechbubbleContainer ToDomain(SpeechbubbleContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);
        
        var domain = new SpeechbubbleContainer(parentSurface)
        {
            Text = dto.Text,
            StoredTargetGripperLocation = new Point(dto.StoredTargetGripperLocation.X, dto.StoredTargetGripperLocation.Y)
        };
        return InitDrawableContainer(domain, dto);
    }

    public static ArrowContainer ToDomain(ArrowContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);
        
        var domain = new ArrowContainer(parentSurface);
        return InitDrawableContainer(domain, dto);
    }

    public static IconContainer ToDomain(IconContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);
        
        var domain = new IconContainer(parentSurface);
        if (dto.Icon !=null)
        {
            domain.Icon = ByteArrayToIcon(dto.Icon);
        }
        else
        {
            Log.Warn("IconContainerDto contains no Icon. Cannot create a replacement");
        }
        return InitDrawableContainer(domain, dto);
    }

    public static StepLabelContainer ToDomain(StepLabelContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);
        
        var domain = new StepLabelContainer(parentSurface)
        {
            Number = dto.Number,
            CounterStart = dto.CounterStart
        };
        return InitDrawableContainer(domain, dto);
    }

    public static EllipseContainer ToDomain(EllipseContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);
        
        var domain = new EllipseContainer(parentSurface);
        return InitDrawableContainer(domain, dto);
    }

    public static HighlightContainer ToDomain(HighlightContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);
        
        var domain = new HighlightContainer(parentSurface);
        return InitDrawableContainer(domain, dto);
    }

    public static ObfuscateContainer ToDomain(ObfuscateContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);
        
        var domain = new ObfuscateContainer(parentSurface);
        return InitDrawableContainer(domain, dto);
    }

    public static CursorContainer ToDomain(CursorContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);
        
        var domain = new CursorContainer(parentSurface);
        return InitDrawableContainer(domain, dto);
    }

    public static FreehandContainer ToDomain(FreehandContainerDto dto, ISurface parentSurface)
    {
        if (dto == null) return null;

        parentSurface = CheckOrCreateParentSurface(parentSurface);
        
        var domain = new FreehandContainer(parentSurface)
        {
            CapturePoints = dto.CapturePoints.Select(p => new Point(p.X, p.Y)).ToList()
        };
        return InitDrawableContainer(domain, dto);
    }

    public static IField ToDomain(FieldDto dto)
    {
        IFieldType FieldTypeNameToFieldTyp(string name)
        {
            foreach (var fieldType in FieldType.Values)
            {
                if (fieldType.Name.Equals(name))
                {
                    return fieldType;
                }
            }

            throw new ArgumentException($"Unknown field type name: {name}");
        }

        if (dto == null) return null;

        return new Field(FieldTypeNameToFieldTyp(dto.FieldTypeName), dto.Scope)
        {
            Value = ConvertDtoToValue(dto.Value)
        };
    }

    /// <summary>
    /// Converts a <see cref="FieldValueDto"/> instance to its corresponding value.
    /// </summary>
    /// <remarks>The method is public mainly because of testing.</remarks>
    /// <returns>The value extracted from the <paramref name="dto"/> using its <c>GetValue</c> method,  or <see langword="null"/>
    /// if <paramref name="dto"/> is <see langword="null"/>.</returns>
    public static object ConvertDtoToValue(FieldValueDto dto)
    {
        return dto?.GetValue();
    }

    /// <summary>
    /// Initializes a drawable container.
    /// </summary>
    /// <remarks>This method sets the position, dimensions, and field values of the container based on the
    /// provided DTO. It also invokes the <see cref="DrawableContainer.OnDeserialized"/> method to finalize the
    /// initialization process.</remarks>
    /// <typeparam name="T">The type of the drawable container, which must inherit from <see cref="DrawableContainer"/>.</typeparam>
    /// <param name="container">The drawable container instance to initialize. Must not be <see langword="null"/>.</param>
    /// <param name="dto">The data transfer object containing the properties and field values to apply. Must not be <see
    /// langword="null"/>.</param>
    /// <returns>The initialized drawable container of type <typeparamref name="T"/>.</returns>
    private static T InitDrawableContainer<T>(T container, DrawableContainerDto dto) where T : DrawableContainer
    {
        container.Left = dto.Left;
        container.Top = dto.Top;
        container.Width = dto.Width;
        container.Height = dto.Height;

        TranferFieldValues(dto.Fields, container);

        container.OnDeserialized();
        return container;
    }

    /// <summary>
    /// Transfers field values to a <see cref="DrawableContainer"/>.
    /// </summary>
    /// <remarks>This method uses <see cref="AbstractFieldHolderWithChildren.SetFieldValue"/> to add or update the field value.</remarks>
    private static void TranferFieldValues(List<FieldDto> dtoFields, DrawableContainer domain)
    {
        foreach (var field in dtoFields.Select(ToDomain))
        {
            domain.SetFieldValue(field.FieldType, field.Value);
        }
    }

    /// <summary>
    /// Converts a byte array into an <see cref="Image"/> object.
    /// </summary>
    private static Image ByteArrayToImage(byte[] byteArrayIn)
    {
        if (byteArrayIn == null) return null;
        using var ms = new MemoryStream(byteArrayIn);
        return Image.FromStream(ms);
    }

    /// <summary>
    /// Converts a byte array into an <see cref="Icon"/> object.
    /// </summary>
    private static Icon ByteArrayToIcon(byte[] byteArrayIn)
    {
        if (byteArrayIn == null) return null;
        using var ms = new MemoryStream(byteArrayIn);
        return new Icon(ms);
    }

}

