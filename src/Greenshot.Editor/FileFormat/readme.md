# Greenshot File Format

This document describes the file formats used by Greenshot Editor for saving editor files and templates. 
- Greenshot editor files contains an image and graphical elements (such as text, shapes, and highlights) layered on top of the image.
- Greenshot templates files contains only graphical elements.

---

# Greenshot Editor File

- Greenshot editor files are saved with the `.gsa` (Greenshot Archive) file extension.
- Class `GreenshotFile` represents a .gsa file as domain object.
- It contains the PNG image data that came from `Surface` (the editor canvas). More precisely from `Surface.Image`.
- It also contains a `DrawableContainerList` that came from `Surface.Elements`. A `DrawableContainer` object represents a graphical element (such as text, shapes, and highlights) that are placed on the editor canvas.
- For serialization/deserialization, the `GreenshotFile` is converted to a Data Transfer Object (DTO) `GreenshotFileDto`, which contains the data to be serialized.

## Supported File Formats

Greenshot editor files supports two file formats:
- Legacy file format (V1)
- Current file format (V2)

### 1. Legacy File Format (V1)

- **Serialization:** Uses .NET BinaryFormatter.
- **Markers:** Files are identified by the marker strings `"Greenshot01.02"` or `"Greenshot01.03"` or `"Greenshot01.04"` at the end of the file.
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
  4. 14 bytes for the Greenshot marker string (`"Greenshot01.02"`, `"Greenshot01.03"` and `"Greenshot01.04"`).
- **Note:**  
  Because the file starts with PNG data, it was possible to open `.greenshot` files with an image viewer and at least display the image.

### 2. Current File Format (V2)

- **Serialization:** Uses JSON with `System.Text.Json`.
- **Container:** V2 is implemented as a ZIP archive. The archive separates metadata, content (DTO) and image payloads.
- **Archive structure:**
  - `meta.json` — file metadata (format version, schema version, saved-by version, capture size, etc.) (see `GreenshotFileMetaInformationDto`).
  - `content.json` — serialized `GreenshotFileDto` containing the content (containers and other domain properties). Image binary payloads are not embedded in the JSON but are referenced by paths.
  - `screenshot/..` — folder containing the screenshot image.
  - `preview/..`  — folder containing the rendered preview image.
  - `images/..` — folder containing extracted image files from diferent container elements.
- **Archive example:**
  ```
  my_file.gsa (Greenshot Archive / ZIP)
  ├─ meta.json              # File metadata (format version, schema version, saved-by version, capture size, etc.)
  ├─ content.json           # Serialized GreenshotFileDto containing the content (containers and properties)
  ├─ screenshot/            # Folder containing the screenshot image
  │  └─ capture.png         # The screenshot image from the Surface.Image
  ├─ preview/               # Folder containing the rendered preview image
  │  └─ preview.png         # The rendered preview image
  └─ images/                # Folder containing extracted image files from different container elements
     ├─ image_1.png         # Image from an ImageContainer
     ├─ image_2.jpg         # Another image (possibly different format)
     ├─ image_3.svg         # Content from an SVGContainer
     └─ ...
  ```
- **Implementation:** See `Greenshot.Editor.FileFormat.V2.GreenshotFileV2`.
- **Notes:**
  - V2 intentionally separates metadata, content and image payloads. Image bytes found in DTOs are extracted into individual files inside the ZIP and the DTO is updated to reference the entry paths.
  - `meta.json` is used to determine the format version and schema version when loading a file; the loader reads the metadata first and then deserializes `content.json`.
  - The file format includes a schema version to support DTO migration logic. See `GreenshotFileV2.MigrateToCurrentVersion` for migration examples.

---

# Greenshot Template Files

- Greenshot template files are saved with the `.gst` file extension.
- Class `GreenshotTemplate` represents a .gst file as domain object.
- It contains a `DrawableContainerList` that came from `Surface.Elements`. This is exactly the same as the `DrawableContainerList` in a Greenshot editor file.
- For serialization/deserialization, the `GreenshotTemplate` is converted to a DTO `GreenshotTemplateDto`, which contains the data to be serialized.

## Supported File Formats

Greenshot template files supports two file formats:
- Legacy template file format (V1)
- Current template file format (V2)

### 1. Legacy File Template Format (V1)

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

### 2. Current File Template Format (V2)

- **Serialization:** Uses JSON with `System.Text.Json`.
- **Marker:** V2 template files are ZIP archives and do not rely on an in-file marker string. Format detection is performed by reading `meta.json` inside the archive.
- **Implementation:** See `Greenshot.Editor.FileFormat.V2.GreenshotTemplateV2`.
- **Archive structure:**
  1. `meta.json` — file metadata (format and schema version, saved-by version, etc.). See `GreenshotTemplateMetaInformationDto`.
  2. `content.json` — serialized `GreenshotTemplateDto` containing the containers and template data.
  3. `images/...` — extracted image files referenced by the template DTO.

# Summary Table

|File Type | Format | Marker                    | Serializer      | Security         | Implementation      | Support Status      |
|----------|--------|---------------------------|-----------------|------------------|---------------------|---------------------|
| Editor   | V1     | Greenshot01.02/03/04      | BinaryFormatter | Deprecated/Unsafe| GreenshotFileV1     | To be removed       |
| Template | V1     | -no marker-               | BinaryFormatter | Deprecated/Unsafe| GreenshotTemplateV1 | To be removed       |
| Editor   | V2     | zip/meta.json$...Version  | System.Text.Json| Modern/Safe      | GreenshotFileV2     | Current             |
| Template | V2     | zip/meta.json$...Version  | System.Text.Json| Modern/Safe      | GreenshotTemplateV2 | Current             |

---

# Versioning

Greenshot files and Greenshot templates using the same versioning concept. So the Greenshot templates reuses some global constants from the Greenshot file format.

Since File Format V2 Greenshot file versions are independent from the application version. 

The version is composed of two parts:

- **Format Version:** Indicates the serializer is used (i.e. BinaryFormatter or JSON/ZIP) and the binary structure of the file. 
- **Schema Version:** Indicates the version of the DTO structure.

The version is represented as `{format version}.{schema version}` (i.e. as string, `02.01`, this pattern matches markers in V1 and V2).

- **Format Version:** See `GreenshotFileVersionHandler.GreenshotFileFormatVersion`.
- **Schema Version:** See `GreenshotFileVersionHandler.CurrentSchemaVersion`.

---

# JSON (V2) Rules & Notes

- V2 uses `System.Text.Json` for DTO serialization.
- DTO classes are plain DTOs used for persistence and migration.
- `V2Helper.DefaultJsonSerializerOptions` exposes shared `JsonSerializerOptions` used throughout the V2 implementation.

Guidelines for DTO changes:
- Non-breaking changes should not require a schema version bump.
- Breaking changes require incrementing the schema version and implementing migration logic.

Image storage and the image attributes

- Image binary payloads are not embedded directly in `content.json`. Instead image bytes found on DTOs are stored as separate files inside the ZIP archive and the DTO is updated to reference those files by path.

- Two custom attributes control this behavior (see `Greenshot.Editor.FileFormat.Dto.GreenshotImageAttributes`):
  - `GreenshotImageDataAttribute` — applied to `byte[]` properties that contain image bytes. The attribute configures:
    - `PathPropertyName`: the name of the `string` property that will hold the entry path in the archive.
    - `StaticExtension` or `ExtensionPropertyName`: either a fixed extension (e.g. "png") or the name of a DTO property that provides the extension dynamically. Exactly one of these must be provided.
    - `TargetZipFolder`: optional subfolder inside the ZIP where the image file will be written (defaults to `images`).
    - `StaticFilename`: optional fixed filename (without extension); otherwise a unique name is generated during serialization.
  - `GreenshotImagePathAttribute` — applied to `string` properties that hold the relative path to the image entry. It links back to the `byte[]` image property via `ImagePropertyName`.

- Extraction (serialization to ZIP):
  - `V2Helper.ExtractImages` inspects DTOs for properties annotated with `GreenshotImageDataAttribute` (only DTO types allowed to contain images are processed).
  - For each non-empty `byte[]` image property the helper:
    1. Resolves the file extension (static or from the extension property) and normalizes it (`NormalizeExtension`). The extension property is reset to `null` so it won't be persisted separately in JSON.
    2. Resolves the target folder (defaults to `images`) and computes a file name (static or generated like `image_1`).
    3. Writes the image bytes as an individual entry in the ZIP (under e.g. `images/image_1.png`).
    4. Sets the corresponding path property on the DTO to the entry path and clears the `byte[]` image property so `content.json` contains no binary blobs.

- Loading (deserialization from ZIP):
  - `V2Helper.LoadImagesForDto` scans DTOs for properties annotated with `GreenshotImagePathAttribute` and uses the stored path value to read the corresponding entry bytes from the ZIP archive.
  - It assigns the read bytes back to the linked `byte[]` image property and clears the path property (so the in-memory DTO contains image bytes but `content.json` remains path-based).

- Restrictions and safeguards:
  - Only types listed in `V2Helper.AllowedDtoTypesWithImages` are processed for image extraction/loading (e.g. `GreenshotFileDto`, `GreenshotTemplateDto`, `DrawableContainerDto`). This prevents accidental processing of unrelated DTOs.
  - Image properties must be of type `byte[]`. Path properties must be of type `string`.
  - If an expected path or extension property is missing or of the wrong type, the helpers log a warning and skip that property.

- Folder and naming conventions:
  - Default image folder inside the ZIP is `images` (see `V2Helper.DefaultImageFolder`).
  - Extensions are normalized (leading dots removed; empty values default to `png`).
  - Extension properties are reset to `null` during extraction because the extension becomes part of the filename stored in the path property.

- Implementation notes:
  - The helpers `V2Helper.ExtractImages`, `V2Helper.LoadImagesForDto`, and `V2Helper.ReadEntryBytes` are the central functions to examine when changing how images are identified, extracted or loaded.
  - When adding new DTO types that may contain images, add them to `V2Helper.AllowedDtoTypesWithImages` and annotate the image and path properties with the appropriate attributes.

# DTO Migration

If the DTO structure changes in the future (i.e. a breaking change), you must:

- Increment the `SchemaVersion`.
- Implement migration logic to convert older DTO versions to the current version (see `GreenshotFileV2.MigrateToCurrentVersion` / `GreenshotTemplateV2.MigrateToCurrentVersion`).
- Ensure backward compatibility for loading older files. 

---

# How to handle files

Main entry point for file handling is the `FileFormatHandler` class. 

It provides methods to save and load files. There is a `GreenshotFileFormatHandler` for handling Greenshot editor files and a `GreenshotTemplateFormatHandler` for handling Greenshot template files.

Every `FileFormatHandler` calls the associated `VersionHandler` wich determines the file format version and calls the appropriate class that implements the serialization/deserialization logic.

From `FileFormatHandler` to the implementation, only first-class domain objects (`GreenshotFile`, `GreenshotTemplate`) or file streams are passed as parameters. _(In particular, no `Surface` or `DrawableContainer`.)_

---

# Serialization

The serialization is done by these steps (V2):
 1. Convert the domain object (`GreenshotFile` or `GreenshotTemplate`) to a DTO (`GreenshotFileDto` or `GreenshotTemplateDto`) by using `Greenshot.Editor.FileFormat.Dto.ConvertDomainToDto.ToDto()`
 2. Extract image bytes from DTOs using `V2Helper.ExtractImages` and write them into the ZIP archive as individual entries.
 3. Serialize and write the meta information to `meta.json` inside the archive.
 4. Serialize and write the DTO (without embedded image bytes) to `content.json` inside the archive.

# Deserialization

The deserialization is done by these steps:
1. Determine the file format version by inspecting the file (for V1: marker or PNG header; for V2: open ZIP and read `meta.json`).
   - A: If the file format version is V1:
      - Extract the PNG image data from the file.
      - convert the binary data to `LegacyDrawableContainerList` by using the BinaryFormatter deserializer. _(In this step there is some backward compatibility logic to handle older versions of the legacy classes.)_
      - convert this to `DrawableContainerListDto`.
      - use the image data and `DrawableContainerListDto` to create a `GreenshotFileDto` or `GreenshotTemplateDto`.
   - B: If the file format version is V2:
      - open the ZIP archive, read `content.json` and deserialize it into `GreenshotFileDto` or `GreenshotTemplateDto` using `System.Text.Json`.
      - load image files from the ZIP and assign the image byte arrays into the DTO using `V2Helper.LoadImagesForDto`.
1. Convert the DTO to a domain object (`GreenshotFile` or `GreenshotTemplate`) by using `Greenshot.Editor.FileFormat.Dto.ConvertDtoToDomain.ToDomain()`.
1. Call `OnDeserialize()` on every `DrawableContainer` domain object to perform additional initialization.


# Clipboard

In the Greenshot Editor it is possible to copy and paste selected graphical elements (`DrawableContainerList`) to and from the clipboard. For the clipboard data we uses the same mechanism as for the file format. 
The `DrawableContainerList` is converted to a `DrawableContainerListDto` and serialized with JSON (V2). The clipboard data is stored as a string in the clipboard.