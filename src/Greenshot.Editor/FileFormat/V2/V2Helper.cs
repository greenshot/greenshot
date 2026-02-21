using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using log4net;

namespace Greenshot.Editor.FileFormat.V2;

/// <summary>
/// Contains shared helper methods for Greenshot file and Greenshot template.
/// </summary>
public static class V2Helper
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(V2Helper));
    private const string ImageFolder = "images";

    /// <summary>
    /// JsonSerializerOptions with settings for Greenshot file serialization.
    /// </summary>
    public static JsonSerializerOptions GetJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        return options;
    }


    /// <summary>
    /// Determines the Greenshot file format version from the specified JSON element and property name.
    /// </summary>
    /// <remarks>The method supports both numeric and string representations of the file format version in the
    /// JSON property. If the property is not present or cannot be parsed as a valid version, the method returns <see
    /// cref="GreenshotFileVersionHandler.GreenshotFileFormatVersion.Unknown"/>.</remarks>
    /// <param name="jsonElement">The <see cref="System.Text.Json.JsonElement"/> containing the property to inspect.</param>
    /// <param name="propertyName">The name of the property within <paramref name="jsonElement"/> that specifies the file format version.</param>
    /// <returns>A <see cref="GreenshotFileVersionHandler.GreenshotFileFormatVersion"/> value representing the detected file
    /// format version. Returns <see cref="GreenshotFileVersionHandler.GreenshotFileFormatVersion.Unknown"/> if the
    /// property is missing or cannot be parsed.</returns>
    public static GreenshotFileVersionHandler.GreenshotFileFormatVersion GetFormatVersion(JsonElement jsonElement, string propertyName)
    {
        if (jsonElement.TryGetProperty(propertyName, out var formatVersionElement))
        {
            if (formatVersionElement.ValueKind == JsonValueKind.Number)
            {
                return (GreenshotFileVersionHandler.GreenshotFileFormatVersion)formatVersionElement.GetInt32();
            }

            if (formatVersionElement.ValueKind == JsonValueKind.String)
            {
                var text = formatVersionElement.GetString();
                if (!string.IsNullOrEmpty(text) && Enum.TryParse<GreenshotFileVersionHandler.GreenshotFileFormatVersion>(text, true, out var formatVersion))
                {
                    return formatVersion;
                }
            }
        }

        return GreenshotFileVersionHandler.GreenshotFileFormatVersion.Unknown;
    }

    public static Dictionary<string, byte[]> ExtractImages(object dto, ref int imageIndex)
    {
        if (dto is not (GreenshotFileDto or DrawableContainerDto))
        {
            Log.Error($"DTO must be of type {nameof(GreenshotFileDto)} or {nameof(DrawableContainerDto)}.");
            throw new ArgumentException($"DTO must be of type {nameof(GreenshotFileDto)} or {nameof(DrawableContainerDto)}.", nameof(dto));
        }

        var imagesToSave = new Dictionary<string, byte[]>();
        var dtoType = dto.GetType();
        var allDtoProperties = dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var property in allDtoProperties)
        {
            var imageAttribute = property.GetCustomAttribute<GreenshotImageDataAttribute>();
            if (imageAttribute == null || property.PropertyType != typeof(byte[]))
            {
                // ignore alle properties without the attribute GreenshotImageDataAttribute or not of type byte[]
                continue;
            }

            var pathProperty = dtoType.GetProperty(imageAttribute.PathPropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (pathProperty == null || pathProperty.PropertyType != typeof(string))
            {
                Log.Warn($"Path property '{imageAttribute.PathPropertyName}' for image property '{property.Name}' not found or not string.");
                continue;
            }

            var imageBytes = property.GetValue(dto) as byte[];
            if (imageBytes == null)
            {
                continue;
            }

            var extension = ResolveExtension(dto, dtoType, imageAttribute);
            var folder = ResolveTargetFolder(imageAttribute);
            var fileName = string.IsNullOrWhiteSpace(imageAttribute.StaticFilename) ? $"image_{imageIndex}" : imageAttribute.StaticFilename;
            var imagePath = $"{folder}/{fileName}.{extension}";
            imageIndex++;

            imagesToSave[imagePath] = imageBytes;

            pathProperty.SetValue(dto, imagePath);
            property.SetValue(dto, null);
        }

        return imagesToSave;
    }

    private static string ResolveTargetFolder(GreenshotImageDataAttribute imageAttribute)
    {
        var folder = imageAttribute.TargetZipFolder;
        if (string.IsNullOrWhiteSpace(folder))
        {
            return ImageFolder;
        }

        folder = folder.Trim();
        folder = folder.Trim('/').Trim('\\');

        return string.IsNullOrWhiteSpace(folder) ? ImageFolder : folder;
    }

    private static string ResolveExtension(object dto, Type dtoType, GreenshotImageDataAttribute imageAttribute)
    {
        PropertyInfo extensionProperty = null;
        string extension = imageAttribute.StaticExtension;

        if (!string.IsNullOrEmpty(imageAttribute.ExtensionPropertyName))
        {
            extensionProperty = dtoType.GetProperty(imageAttribute.ExtensionPropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (extensionProperty == null || extensionProperty.PropertyType != typeof(string))
            {
                Log.Warn($"Extension property '{imageAttribute.ExtensionPropertyName}' for image property '{imageAttribute.PathPropertyName}' not found or not string.");
            }
            else
            {
                var propertyValue = extensionProperty.GetValue(dto) as string;
                if (!string.IsNullOrWhiteSpace(propertyValue))
                {
                    extension = propertyValue;
                }
            }
        }

        extension = NormalizeExtension(extension);

        extensionProperty?.SetValue(dto, null);

        return extension;
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return "png";
        }

        extension = extension.Trim();
        extension = extension.TrimStart('.');

        return string.IsNullOrWhiteSpace(extension) ? "png" : extension;
    }


    public static void LoadImagesForDto(object dto, ZipArchive zipArchive)
    {
        if (dto is not (GreenshotFileDto or DrawableContainerDto))
        {
            Log.Error($"DTO must be of type {nameof(GreenshotFileDto)} or {nameof(DrawableContainerDto)}.");
            throw new ArgumentException($"DTO must be of type {nameof(GreenshotFileDto)} or {nameof(DrawableContainerDto)}.", nameof(dto));
        }

        var dtoType = dto.GetType();
        var allDtoProperties = dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var property in allDtoProperties)
        {
            var pathAttribute = property.GetCustomAttribute<GreenshotImagePathAttribute>();
            if (pathAttribute == null || property.PropertyType != typeof(string))
            {
                continue;
            }

            var entryPath = property.GetValue(dto) as string;
            if (string.IsNullOrEmpty(entryPath))
            {
                continue;
            }

            var imageProperty = dtoType.GetProperty(pathAttribute.ImagePropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (imageProperty == null || imageProperty.PropertyType != typeof(byte[]))
            {
                Log.Warn($"Image property '{pathAttribute.ImagePropertyName}' for path property '{property.Name}' not found or not byte array.");
                continue;
            }

            var imageBytes = ReadEntryBytes(zipArchive, entryPath);
            imageProperty.SetValue(dto, imageBytes);
            property.SetValue(dto, null);
        }
    }

    private static byte[] ReadEntryBytes(ZipArchive zipArchive, string entryPath)
    {
        var entry = zipArchive.GetEntry(entryPath);
        if (entry == null)
        {
            return null;
        }

        using var entryStream = entry.Open();
        using var ms = new MemoryStream();
        entryStream.CopyTo(ms);
        return ms.ToArray();
    }

    public static string SerializeDto<TDto>(TDto dto) where TDto : class
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }
        var options = GetJsonSerializerOptions();
        return JsonSerializer.Serialize(dto, options);
    }

    public static TDto DeserializeDto<TDto>(string json) where TDto : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON string cannot be null or whitespace.", nameof(json));
        }
        var options = GetJsonSerializerOptions();
        return JsonSerializer.Deserialize<TDto>(json, options);
    }
}

