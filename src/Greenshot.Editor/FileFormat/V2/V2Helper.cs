using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

    /// <summary>
    /// Default folder name within the zip archive where image files are stored.
    /// </summary>
    private const string DefaultImageFolder = "images";

    /// <summary>
    /// JsonSerializerOptions with settings for Greenshot file serialization.
    /// </summary>
    public static JsonSerializerOptions DefaultJsonSerializerOptions =>
        new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters =
                {
                    new JsonStringEnumConverter()
                }
        };

    /// <summary>
    /// Guarded list of DTO types that are allowed to contain image data for extraction and loading. This ensures that only
    /// these types are processed for image extraction <see cref="ExtractImages(object, ref int)"/> 
    /// and image loading <see cref="LoadImagesForDto(object, ZipArchive)(object, ref int)"/>, preventing unintended operations on other types. 
    /// </summary>
    private static readonly Type[] AllowedDtoTypesWithImages =
        {
            typeof(GreenshotFileDto),
            typeof(GreenshotTemplateDto),
            typeof(DrawableContainerDto)
        };


    /// <summary>
    /// Determines the Greenshot file format version from the specified JSON element and imageProperty name.
    /// </summary>
    /// <remarks>The method supports both numeric and string representations of the file format version in the
    /// JSON imageProperty. If the imageProperty is not present or cannot be parsed as a valid version, the method returns <see
    /// cref="GreenshotFileVersionHandler.GreenshotFileFormatVersion.Unknown"/>.</remarks>
    /// <param name="jsonElement">The <see cref="System.Text.Json.JsonElement"/> containing the imageProperty to inspect.</param>
    /// <param name="propertyName">The name of the imageProperty within <paramref name="jsonElement"/> that specifies the file format version.</param>
    /// <returns>A <see cref="GreenshotFileVersionHandler.GreenshotFileFormatVersion"/> value representing the detected file
    /// format version. Returns <see cref="GreenshotFileVersionHandler.GreenshotFileFormatVersion.Unknown"/> if the
    /// imageProperty is missing or cannot be parsed.</returns>
    public static GreenshotFileVersionHandler.GreenshotFileFormatVersion GetFormatVersion(JsonElement jsonElement, string propertyName)
    {
        if (jsonElement.TryGetPropertyIgnoreCase(propertyName, out var formatVersionElement))
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

    /// <summary>
    /// Extracts image data from data transfer object (DTO) and returns a dictionary mapping image file paths to their corresponding byte
    /// arrays.
    /// </summary>
    /// <remarks> Only DTOs of types specified in <see cref="AllowedDtoTypesWithImages"/> are processed.
    /// The method looks for properties decorated with the <see cref="GreenshotImageDataAttribute"/> attribute and of type byte[] to identify image data. <br/>
    /// For each such imageProperty, it extracts the image bytes, determines the target file path and adds both to the resulting dictionary. <br/>
    /// After extraction, the method sets the image imageProperty to null and updates the corresponding path imageProperty (identified by the attribute) with the new file path.<br/>
    /// The corresponding method for loading the images back to the DTO is <see cref="LoadImagesForDto(object, ZipArchive)"/>.
    /// </remarks>
    /// <param name="dto">The data transfer object containing image properties to extract. Must be of a type that is permitted to contain
    /// images <see cref="AllowedDtoTypesWithImages"/>.</param>
    /// <param name="imageIndex">A reference to an integer that tracks the current image index. This value is incremented for each image
    /// extracted.</param>
    /// <returns>A dictionary where each key is the file path of an extracted image and each value is the corresponding image
    /// data as a byte array.</returns>
    /// <exception cref="ArgumentException">Thrown if the provided dto is null or is not of a type that is allowed to contain images.</exception>
    public static Dictionary<string, byte[]> ExtractImages(object dto, ref int imageIndex)
    {
        // simple safeguard to ensure that only expected DTO types are processed for image extraction
        if (dto == null || !AllowedDtoTypesWithImages.Any(allowedType => allowedType.IsAssignableFrom(dto.GetType())))
        {
            var allowedTypeNames = string.Join(", ", AllowedDtoTypesWithImages.Select(t => nameof(t)));
            Log.Error($"DTO must be of type {allowedTypeNames}.");
            throw new ArgumentException($"DTO must be one of: {allowedTypeNames}.", nameof(dto));
        }

        var imagesToSave = new Dictionary<string, byte[]>();
        var dtoType = dto.GetType();
        var allDtoProperties = dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var imageProperty in allDtoProperties)
        {
            var imageAttribute = imageProperty.GetCustomAttribute<GreenshotImageDataAttribute>();
            if (imageAttribute == null || imageProperty.PropertyType != typeof(byte[]))
            {
                // ignore all properties without the attribute GreenshotImageDataAttribute or not of type byte[]
                // so this is no image property and we can skip
                continue;
            }

            var imageBytes = imageProperty.GetValue(dto) as byte[];
            if (imageBytes is null || imageBytes.Length == 0)
            {
                // skip if there is no image data
                continue;
            }

            var pathProperty = dtoType.GetProperty(imageAttribute.PathPropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (pathProperty == null || pathProperty.PropertyType != typeof(string))
            {
                Log.Warn($"Path imageProperty '{imageAttribute.PathPropertyName}' for image imageProperty '{imageProperty.Name}' not found or not string.");
                continue;
            }

            var extension = ResolveExtension(dto, dtoType, imageAttribute);
            var folder = ResolveTargetFolder(imageAttribute);
            var fileName = string.IsNullOrWhiteSpace(imageAttribute.StaticFilename) ? $"image_{imageIndex}" : imageAttribute.StaticFilename;
            var imagePath = $"{folder}/{fileName}.{extension}";
            imageIndex++;

            imagesToSave[imagePath] = imageBytes;

            pathProperty.SetValue(dto, imagePath);
            imageProperty.SetValue(dto, null);
        }

        return imagesToSave;
    }

    /// <summary>
    /// Resolves the target folder for an image based on the provided <see cref="GreenshotImageDataAttribute"/>.
    /// If the attribute does not specify a target folder, the method returns a default folder name defined by <see cref="DefaultImageFolder"/>.
    /// </summary>
    /// <param name="imageAttribute">The attribute containing information about the target folder.</param>
    /// <returns>The resolved target folder path.</returns>
    private static string ResolveTargetFolder(GreenshotImageDataAttribute imageAttribute)
    {
        var folder = imageAttribute.TargetZipFolder;
        if (string.IsNullOrWhiteSpace(folder))
        {
            return DefaultImageFolder;
        }

        folder = folder.Trim();
        folder = folder.Trim('/').Trim('\\');

        return string.IsNullOrWhiteSpace(folder) ? DefaultImageFolder : folder;
    }

    /// <summary>
    /// Resolves the file extension for an image imageProperty.
    /// </summary>
    /// <remarks>The extension could be in a static form defined in the <see cref="GreenshotImageDataAttribute.StaticExtension"/> imageProperty 
    /// or in a dynamic form provided by a imageProperty of the DTO, whose name is specified in the <see cref="GreenshotImageDataAttribute.ExtensionPropertyName"/> imageProperty. <br/>
    /// The extension imageProperty in the DTO is reset to null after resolution.</remarks>
    /// <param name="dto">The data transfer object containing properties relevant to the image. Must not be null.</param>
    /// <param name="dtoType">The type of the data transfer object, used to access its properties reflectively.</param>
    /// <param name="imageAttribute">The attribute containing metadata about the image, including a static extension and the name of a imageProperty that
    /// may provide a dynamic extension.</param>
    /// <returns>A string representing the resolved file extension. Returns the default "png" if no valid extension is found.</returns>
    private static string ResolveExtension(object dto, Type dtoType, GreenshotImageDataAttribute imageAttribute)
    {
        PropertyInfo extensionProperty = null;
        string extension = imageAttribute.StaticExtension;

        if (!string.IsNullOrEmpty(imageAttribute.ExtensionPropertyName))
        {
            extensionProperty = dtoType.GetProperty(imageAttribute.ExtensionPropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (extensionProperty == null || extensionProperty.PropertyType != typeof(string))
            {
                Log.Warn($"Extension imageProperty '{imageAttribute.ExtensionPropertyName}' for image imageProperty '{imageAttribute.PathPropertyName}' not found or not string.");
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

        // reset to null, so that it won't be saved in the json, because the extension is already part of the image path and doesn't need to be stored separately
        extensionProperty?.SetValue(dto, null);

        return extension;
    }

    /// <summary>
    /// Normalizes the file extension by trimming whitespace and removing any leading dots.
    /// If the extension is null, empty, or consists only of whitespace, "png" is returned as the default.
    /// </summary>
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

    /// <summary>
    /// Loads images for the specified Data Transfer Object (DTO) from the given zip archive.
    /// </summary>
    /// <remarks>Only DTOs of types specified in <see cref="AllowedDtoTypesWithImages"/> are processed.
    /// The method looks for properties decorated with the <see cref="GreenshotImagePathAttribute"/> attribute and of type string to identify image path properties. 
    /// For each such property, it reads the corresponding image bytes from the zip archive using the path specified in the property value, 
    /// sets the image bytes to the corresponding image property (identified by the <see cref="GreenshotImagePathAttribute.ImagePropertyName"/>), and then resets the path property to null.</remarks>
    /// <param name="dto">The data transfer object to load images for. Must not be null and must be of a type that is permitted to contain
    /// images <see cref="AllowedDtoTypesWithImages"/>. </param>
    /// <param name="zipArchive">The zip archive containing the images. Must not be null.</param>
    /// <exception cref="ArgumentException">Thrown if the DTO is null or not of an allowed type.</exception>
    public static void LoadImagesForDto(object dto, ZipArchive zipArchive)
    {
        // simple safeguard to ensure that only expected DTO types are processed for image extraction
        if (dto == null || !AllowedDtoTypesWithImages.Any(allowedType => allowedType.IsAssignableFrom(dto.GetType())))
        {
            var allowedTypeNames = string.Join(", ", AllowedDtoTypesWithImages.Select(t => nameof(t)));
            Log.Error($"DTO must be of type {allowedTypeNames}.");
            throw new ArgumentException($"DTO must be one of: {allowedTypeNames}.", nameof(dto));
        }

        var dtoType = dto.GetType();
        var allDtoProperties = dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var imagePathproperty in allDtoProperties)
        {
            var pathAttribute = imagePathproperty.GetCustomAttribute<GreenshotImagePathAttribute>();
            if (pathAttribute == null || imagePathproperty.PropertyType != typeof(string))
            {
                // ignore all properties without the attribute GreenshotImagePathAttribute or not of type string
                // so this is no image path property and we can skip
                continue;
            }

            var entryPath = imagePathproperty.GetValue(dto) as string;
            if (string.IsNullOrEmpty(entryPath))
            {
                // skip if there is no given path to an image
                continue;
            }

            var imageProperty = dtoType.GetProperty(pathAttribute.ImagePropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (imageProperty == null || imageProperty.PropertyType != typeof(byte[]))
            {
                Log.Warn($"Image imageProperty '{pathAttribute.ImagePropertyName}' for path imageProperty '{imagePathproperty.Name}' not found or not byte array.");
                continue;
            }

            var imageBytes = ReadEntryBytes(zipArchive, entryPath);
            imageProperty.SetValue(dto, imageBytes);
            imagePathproperty.SetValue(dto, null);
        }
    }

    /// <summary>
    /// Reads the bytes of an entry from the given zip archive.
    /// </summary>
    /// <param name="zipArchive">The zip archive containing the entry.</param>
    /// <param name="entryPath">The path of the entry within the zip archive.</param>
    /// <returns> The byte array of the entry, or an empty array if the entry is not found.</returns>
    private static byte[] ReadEntryBytes(ZipArchive zipArchive, string entryPath)
    {
        if (zipArchive == null)
        {
            throw new ArgumentNullException(nameof(zipArchive));
        }

        if (string.IsNullOrEmpty(entryPath))
        {
            throw new ArgumentException("Entry path cannot be null or empty.", nameof(entryPath));
        }

        var entry = zipArchive.GetEntry(entryPath);
        if (entry == null)
        {
            return Array.Empty<byte>();
        }

        using var entryStream = entry.Open();
        using var ms = new MemoryStream();
        entryStream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Serializes the specified Data Transfer Object (DTO) to a JSON string.
    /// </summary>
    /// <remarks>
    /// Use this if no special handling e.g. for images, metadata or versioning is required and a simple JSON representation of the DTO is sufficient. <br/>
    /// Use <see cref="DeserializeDto{TDto}(string)"/> to deserialize the JSON string back to a DTO of the specified type.
    /// </remarks>
    /// <typeparam name="TDto">The type of the Data Transfer Object to be serialized. Must be a class.</typeparam>
    /// <param name="dto">The Data Transfer Object to serialize. Cannot be null.</param>
    /// <returns>A JSON string representation of the provided DTO.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dto"/> is null.</exception>
    public static string SerializeDto<TDto>(TDto dto) where TDto : class
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        var options = DefaultJsonSerializerOptions;
        var json = JsonSerializer.Serialize(dto, options);
        return json;
    }

    /// <summary>
    /// Deserializes the specified JSON string to a Data Transfer Object (DTO) of the specified type.
    /// </summary>
    /// <remarks>
    /// Use this if no special handling e.g. for images, metadata or versioning is required and a simple JSON representation of the DTO is sufficient.<br/>
    /// Use this if the JSON string was created using <see cref="SerializeDto{TDto}(TDto)"/>.
    /// </remarks>
    /// <typeparam name="TDto">The type of the Data Transfer Object to be deserialized. Must be a class.</typeparam>
    /// <param name="json">The JSON string to deserialize. Cannot be null or whitespace.</param>
    /// <returns>The deserialized Data Transfer Object.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is null or whitespace.</exception>
    public static TDto DeserializeDto<TDto>(string json) where TDto : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON string cannot be null or whitespace.", nameof(json));
        }

        var options = DefaultJsonSerializerOptions;
        var dto = JsonSerializer.Deserialize<TDto>(json, options);
        return dto;
    }
}

