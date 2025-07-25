# Greenshot File Format

This document describes the file formats used by Greenshot Editor for saving editor files and templates. 
- Greenshot editor files contains an image and graphical elements (such as text, shapes, and highlights) layered on top of the image.
- Greenshot templates files contains only graphical elements.

---

# Greenshot Editor File

- Greenshot editor files are saved with the `.greenshot` file extension.
- Class `GreenshotFile` represents a .greenshot file as domain object.
- It contains the PNG image data that came from `Surface` (the editor canvas). More precisely from `Surface.Image`.
- It also contains a `DrawableContainerList` that came from `Surface.Elements`. A `DrawableContainer` object represents a graphical element (such as text, shapes, and highlights) that are placed on the editor canvas.
- For serialization/deserialization, the `GreenshotFile` is converted to a Data Transfer Object (DTO) `GreenshotFileDto`, which contains the data to be serialized.

## Supported File Formats

Greenshot editor files supports two file formats:
- Legacy file format (V1)
- Current file format (V2)

### 1. Legacy File Format (V1)

- **Serialization:** Uses .NET BinaryFormatter.
- **Markers:** Files are identified by the marker strings `"Greenshot01.02"` or `"Greenshot01.03"` at the end of the file.
- **Security:** BinaryFormatter is deprecated and considered insecure. Support for this format will be removed in the future.
- **Implementation:** See `Greenshot.Editor.FileFormat.V1.GreenshotFileV1`.
- **Implementation Detail:**  
  The binary data for the drawable containers is not deserialized directly into a `DrawableContainerList`. Instead, there is an intermediate layer of legacy classes. The deserialization steps are as follows: First, the data is deserialized into a `LegacyDrawableContainerList`, then it is converted to a `DrawableContainerListDto`, and finally, it is converted to a `DrawableContainerList`.  
  Because the legacy classes are used during deserialization, it will be easier to remove support for BinaryFormatter from the application in the future. Only the `LegacyDrawableContainerList` and `LegacyDrawableContainer` classes uses the BinaryFormatter serialization attributes.
- **Binary Data Structure:**  
  The file consists of:
  1. The PNG image data.
  2. The binary data of the `DrawableContainerList` (serialized with BinaryFormatter).
  3. 8 bytes for the binary data size (Int64).
  4. 14 bytes for the Greenshot marker string (`"Greenshot01.02"` or `"Greenshot01.03"`).
- **Note:**  
  Because the file starts with PNG data, it was possible to open `.greenshot` files with an image viewer and at least display the image.

### 2. Current File Format (V2)

- **Serialization:** Uses [MessagePack](https://github.com/MessagePack-CSharp/MessagePack-CSharp/) for serialization.
- **Marker:** Files are identified by the marker string `"Greenshot02.01"` at the end of the file. The schema version `".01"` would be ignored for determines the file format.
- **Implementation:** See `Greenshot.Editor.FileFormat.V2.GreenshotFileV2`.
- **Binary Data Structure:**  
  The file consists of:
  1. The PNG image data of the rendered image for export
  2. The binary data of the `GreenshotFileDto`. (serialized with MessagePack).
  3. 8 bytes for the binary data size (Int64).
  4. 11 bytes for the Greenshot marker string `"Greenshot02"`.
  5. 3 bytes for the Greenshot file schema version (i.e. `".01"`).
- **Note:**  
  In contrast to the legacy file format, the current file format contains the rendered PNG image. This means that all graphical elements are rendered into the PNG image, and the file still can be opened with any image viewer. The image from the editors canvas is stored in the serialized `GreenshotFileDto`.
---

# Greenshot Template Files

- Greenshot template files are saved with the `.gst` file extension.
- Class `GreenshotTemplate` represents a .gst file as domain object.
- It contains a `DrawableContainerList` that came from `Surface.Elements`. This is exactly the same as the `DrawableContainerList` in a Greenshot editor file.
- For serialization/deserialization, the `GreenshotTemplate` is converted to a DTO `GreenshotTemplateDto`, which contains the data to be serialized.

## Supported File Formats

Greenshot template files supports two file formats:
- Legacy file format (V1)
- Current file format (V2)

### 1. Legacy File Format (V1)

- **Serialization:** Uses .NET BinaryFormatter.
- **Markers:** Files don't have a marker string.
- **Security:** BinaryFormatter is deprecated and considered insecure. Support for this format will be removed in the future.
- **Implementation:** See `Greenshot.Editor.FileFormat.V1.GreenshotTemplateV1`.
- **Implementation Detail:**  
  The binary data for the drawable containers is not deserialized directly into a `DrawableContainerList`. Instead, there is an intermediate layer of legacy classes. The deserialization steps are as follows: First, the data is deserialized into a `LegacyDrawableContainerList`, then it is converted to a `DrawableContainerListDto`, and finally, it is converted to a `DrawableContainerList`.  
  Because the legacy classes are used during deserialization, it will be easier to remove support for BinaryFormatter from the application in the future. Only the `LegacyDrawableContainerList` and `LegacyDrawableContainer` classes contain the BinaryFormatter serialization attributes.
- **Binary Data Structure:**  
  The file consists of:
  1. The binary data of the `DrawableContainerList` (serialized with BinaryFormatter).

### 2. Current File Format (V2)

- **Serialization:** Uses [MessagePack](https://github.com/MessagePack-CSharp/MessagePack-CSharp/) for serialization.
- **Marker:** Files are identified by the marker string `"GreenshotTemplate02"` at the beginning of the file. _(Note: Schema version (i.e. `".01"` ) is not part of the marker)_.
- **Implementation:** See `Greenshot.Editor.FileFormat.V2.GreenshotTemplateV2`.
- **Binary Data Structure:**
  The file consists of:
  1. 22 bytes for the Greenshot marker string with complete version string (i.e. `"GreenshotTemplate02.01"` ).
  2. The binary data of `GreenshotTemplateDto`. (serialized with MessagePack).
  

## Summary Table

|File Type | Format | Marker              | Serializer      | Security         | Implementation      | Support Status      |
|----------|--------|---------------------|-----------------|------------------|---------------------|---------------------|
| Editor   | V1     | Greenshot01.02/03   | BinaryFormatter | Deprecated/Unsafe| GreenshotFileV1     | To be removed       |
| Template | V1     | -no marker-         | BinaryFormatter | Deprecated/Unsafe| GreenshotTemplateV1 | To be removed       |
| Editor   | V2     | Greenshot02         | MessagePack     | Modern/Safe      | GreenshotFileV2     | Current             |
| Template | V2     | GreenshotTemplate02 | MessagePack     | Modern/Safe      | GreenshotTemplateV2 | Current             |

---

# Versioning

Greenshot files and Greenshot templates using the same versioning concept. So the Greenshot templates reuses some global constants from the Greenshot file format.

Since File Format V2 Greenshot file versions are independent of the application version. 

The version is composed of two parts:

- **Format Version:** Indicates the serializer is used (i.e. BinaryFormatter or MessagePack) and the binary structure of the file. 
- **Schema Version:** Indicates the version of the DTO structure.

The version is represented as `{format version}.{schema version}` (i.e. as string, `02.01`, this pattern still matches markers in V1 and V2).

- **Format Version:** See `VersionHandler.GreenshotFileFormatVersion`.
- **Schema Version:** See `VersionHandler.SchemaVersion`.

---

# MessagePack

Since File Format V2, Greenshot uses [MessagePack](https://github.com/MessagePack-CSharp/MessagePack-CSharp/) for serialization.

    TL;DR: For using MessagePack, the DTO classes are annotated with attributes `[MessagePackObject]`.
    Every property that should be serialized is annotated with `[Key]` and has a unique key value.

    Serialization/Deserialization:
    var bytes = MessagePackSerializer.Serialize<GreenshotFileDto>(dto);
    var dto = MessagePackSerializer.Deserialize<GreenshotFileDto>(bytes);


## MessagePack Rules

- **Always** use `int` as `[Key]` value.
- **Do not change** the `[Key]` value of existing properties.
- **Do not remove** properties unless they are marked with `[IgnoreMember]`.
- **Add new properties** with new, unique `[Key]` values.
- **Obsolete properties** have to be marked with `[IgnoreMember]` instead of being removed.
- `[Key]` values `0 - 9` are reserved for operational functions like versioning and have no relation to domain data.
- Use `[Key]` values `10 - 99` for base class properties and `100 - 199` for inherited class properties and so on.
- If possible, avoid inheritance and keep DTO classes as simple as possible.
- If possible, include no business logic in DTO classes. Use the `DtoHelper` class for that.


## Rules for Schema and Format Versioning

- **Non-breaking changes** (do **not** require incrementing `SchemaVersion`):
  - Adding required properties with default values.
  - Adding new optional properties.
  - Changing property types that are compatible (i.e., `int` → `long`).
  - Changing default values.
  - Marking properties as obsolete with `[IgnoreMember]`.

- **Breaking changes** (require incrementing `SchemaVersion`):
  - Reordering values in enums used in DTO properties.
  - Adding required properties without default values.
  - Changing property types that are not compatible (i.e., `int` → `string`).
  - Changing the meaning of a property content, that requires migration logic.
  - Moving properties between DTOs.

- **Format Version** changes if:
  - the serialization method changes (an other serializer than MessagePack and BinaryFormatter is used)
  - the binary data structure changes (i.e. the marker string changes).

---

## DTO Migration

If the DTO structure changes in the future (i.e. a breaking change), you must:

- Increment the `SchemaVersion`.
- Implement migration logic to convert older DTO versions to the current version (see `GreenshotFileV2.MigrateToCurrentVersion`).
- Ensure backward compatibility for loading older files. 

---

# How to handle files

Main entry point for file handling is the `FileFormatHandler` class. 

It provides methods to save and load files. There is a `GreenshotFileFormatHandler` for handling Greenshot editor files and a `GreenshotTemplateFormatHandler` for handling Greenshot template files.

Every `FileFormatHandler` calls the associated `VersionHandler` wich determines the file format version and calls the appropriate class that implements the serialization/deserialization logic.

From `FileFormatHandler` to the implementation, only first-class domain objects (`GreenshotFile`, `GreenshotTemplate`) or file streams are passed as parameters. 

---

# Serialization

The serialization is done by these steps:
 1. Convert the domain object (`GreenshotFile` or `GreenshotTemplate`) to a DTO (`GreenshotFileDto` or `GreenshotTemplateDto`) by using `Greenshot.Editor.FileFormat.Dto.ConvertDomainToDto.ToDto()`
 1. Convert the DTO to binary data using the MessagePack serializer. _(Serialization with BinaryFormatter is not implemented anymore.)_

# Deserialization

The deserialization is done by these steps:
1. determine the file format version.
   - A: If the file format version is V1:
      - Extract the PNG image data from the file.
      - convert the binary data to `LegacyDrawableContainerList` by using the BinaryFormatter deserializer. _(In this step there is some backward compatibility logic to handle older versions of the legacy classes.)_
      - convert this to `DrawableContainerListDto`.
      - use the image data and `DrawableContainerListDto` to create a `GreenshotFileDto` or `GreenshotTemplateDto`.
   - B: If the file format version is V2:
      - convert the binary data to `GreenshotFileDto` or `GreenshotTemplateDto` using the MessagePack deserializer directly.
1. Convert the DTO to a domain object (`GreenshotFile` or `GreenshotTemplate`) by using `Greenshot.Editor.FileFormat.Dto.ConvertDtoToDomain.ToDomain()`.
1. Call `OnDeserialize()` on every `DrawableContainer` domain object to perform additional initialization.


# Clipboard

In the Greenshot Editor it is possible to copy and paste selected graphical elements (`DrawableContainerList`) to and from the clipboard. For the clipboard data we uses the same mechanism as for the file format. 
The `DrawableContainerList` is converted to a `DrawableContainerListDto` and serialized with MessagePack. The clipboard data is stored as a byte array in the clipboard. 