# Plan: Remove BinaryFormatter while preserving V1 file compatibility

## Goal

Remove `BinaryFormatter` from Greenshot's V1 file-format handling while
continuing to support both:

- Reading existing V1 `.greenshot` files into the existing `LegacyClasses`.
- Writing V1 `.greenshot` files from `LegacyClasses` that can be read by older
  Greenshot applications.

Round trips may lose data where the current legacy conversion already
normalizes or drops information. Exact byte-for-byte reproduction is not a
goal. Compatibility with existing V1 readers is a goal.

The implementation must not use `BinaryFormatter` for either reading or
writing. The new reader and writer should be isolated behind the V1 format
implementation, and `LegacyClasses` remain the intermediate compatibility
model. This plan does not depend on the retired DTO conversion path.

## V1 wire format to preserve

The writer must emit the legacy V1.04 variant:

- **Editor file:** PNG screenshot bytes, an NRBF-encoded
  `LegacyDrawableContainerList` payload compatible with the historical
  Greenshot V1.04 object graph, an 8-byte payload length, and the
  `Greenshot01.04` marker.
- **Template file:** the NRBF-encoded drawable-container-list payload only;
  templates have no marker or file envelope.

The representative V1.02, V1.03, and V1.04 editor and template fixtures now
have asserted root identities, list members, array identities, item counts,
editor markers, and payload boundaries in `LegacyNrbfReaderTests`:

| Fixture generation | Root type and assembly identity | List item array | Example count |
|--------------------|----------------------------------|-----------------|---------------|
| V1.02 | `Greenshot.Drawing.DrawableContainerList`, `Greenshot, Version=1.2.0.0` | `Greenshot.Plugin.Drawing.IDrawableContainer[]` | 11 |
| V1.03 | `Greenshot.Editor.Drawing.DrawableContainerList`, `Greenshot.Editor, Version=1.3.0.0` | `Greenshot.Base.Interfaces.Drawing.IDrawableContainer[]` | 14 |
| V1.04 | `Greenshot.Editor.Drawing.DrawableContainerList`, `Greenshot.Editor, Version=1.4.0.0` | `Greenshot.Base.Interfaces.Drawing.IDrawableContainer[]` | 11 |

Across these versions the root member names and order are
`<ParentID>k__BackingField`, `_disposedValue`, `List\`1+_items`,
`List\`1+_size`, and `List\`1+_version`. Editor fixtures end with the
corresponding `Greenshot01.02`, `Greenshot01.03`, or `Greenshot01.04` marker.
Tests confirm that the footer length equals exactly the number of bytes
consumed by the NRBF decoder; template fixtures contain only the NRBF payload.

The V1.02 combined fixture contains 11 different containers; the V1.03
combined fixture contains 14, adding SVG and metafile to the common
containers; the V1.04 combined fixture contains 11. Separate V1.04 fixtures
exercise cursor and emoji records, with step-label represented in both V1.02
and V1.04. The matching template corpus provides combined V1.02/V1.03 files
and a V1.04 cursor file.

In the V1.04 root record, `List\`1+_items` is declared as the class
`Greenshot.Base.Interfaces.Drawing.IDrawableContainer[]`. Its value is a
`MemberReference` to an array record emitted after the root's primitive
size/version values. The writer follows this fixture ordering; writing the
array inline caused an older `BinaryFormatter` reader to leave the list
backing array unset, so assigning the list's parent then failed during
enumeration.

Nested `List<T>` values use a reference to a separately emitted item array.
Their backing arrays must retain the declared interface-array types, such as
`IFieldHolder[]` and `IField[]`, and be defined before drawable records
containing those lists. Emitting them as `object[]` fails when the legacy
runtime assigns the array to the typed `List<T>._items` member.

### Confirmed details and remaining contract work

An NRBF graph inspection showed that the root's item array contains
`Greenshot.Base.Interfaces.Drawing.IDrawableContainer` values and that nested
field-holder lists use
`System.Collections.Generic.List\`1[[Greenshot.Base.Interfaces.Drawing.IFieldHolder, ...]]`.
The inspected arrow and ellipse records include inherited container members,
`Children`, and `AbstractFieldHolder+fields`; list members include `_items`,
`_size`, and `_version`. V1.04 cursor uses
`Greenshot.Editor.Drawing.CursorContainer+CaptureCursorSerializationWrapper`
with `<ColorLayer>k__BackingField`, `<MaskLayer>k__BackingField`,
`<SizeWidth>k__BackingField`, `<SizeHeight>k__BackingField`,
`<HotspotX>k__BackingField`, and `<HotspotY>k__BackingField`. This is
fixture-derived evidence, not yet a complete serialization specification.

Before implementing record emission, extend the contract test/inspection to
record, for every fixture root and object kind:

1. Ordered member metadata (not only member names): member binary types,
   additional type information, assembly/library names and versions, and
   which members are inherited versus declared on each concrete type. In
   particular, capture the actual V1.04 application assembly identities
   emitted in each record and any historical type aliases accepted only by
   the reader.
2. Ordered member names, member binary types, additional type information,
   and which members are inherited versus declared on the concrete type.
3. Collection runtime type, array element identity, count/capacity behavior,
   and references/`ClassWithId` records used for repeated metadata.
4. Field and filter graph shapes, including primitive values, enum identities,
   `System.Drawing.Color`, nested field holders, and shared references.
5. Exact special-value layouts for `Image`, `Icon`, cursor bitmaps,
   `Metafile`, `MemoryStream`, `Point`, SVG, and the V1.04-only container
   properties. Record which cases the current safe reader intentionally
   normalizes or drops.
6. Editor-file offsets and PNG/payload boundary for each fixture, confirming
   that the stored payload length is the NRBF payload length and that the
   marker is exactly `Greenshot01.04`.

Use the fixture corpus and the historical Greenshot serialization code or a
known-good payload produced by an older application as the reference. Do not
assume that a payload accepted by `System.Formats.Nrbf` is necessarily accepted
by an older `BinaryFormatter` reader: assembly identity, member type metadata,
and special `ISerializable` layouts need an old-reader compatibility check.
Keep fixture discovery as committed assertions where practical; remove any
temporary graph-dump tools after extracting the contract.

The current `LegacyClasses` are read/compatibility objects, not a writer model:
`LegacyFieldHolder` and cursor serialization wrapper explicitly throw from
`ISerializable.GetObjectData`. The implementation must therefore emit NRBF
records from the properties directly. Do not make these classes serializable
by `BinaryFormatter` or call their `GetObjectData` methods.

#### Writer progress

`LegacyNrbfWriter` now emits the NRBF stream header, V1.04 root/list metadata,
the fixture-shaped reference to its typed `IDrawableContainer[]`, `Guid`
parent ID, and a `RectangleContainer` with its geometry, empty children, and
four standard fields (line thickness, line color, fill color, and shadow).
Ellipse and line containers also use the shared geometry-record writer.
Children and fields lists reference correctly typed backing arrays emitted
before the drawable records. Children arrays are currently empty; field arrays
can contain records. Field values currently support integers, booleans,
single- and double-precision floating-point values, strings, colors, and the
explicitly allowlisted `FieldFlag`, `PreparedFilter`, and
`ArrowHeadCombination` enums. Focused writer tests verify the array types, that
`System.Formats.Nrbf` decodes the payload, and that `LegacyNrbfReader`
reconstructs the rectangle coordinates and field values.

`WriteEditorFileWithRectangle_CreatesManualCompatibilityArtifact` wraps this
graph with a PNG screenshot, 8-byte payload length, and `Greenshot01.04`
marker. It writes `TestResults\V1WriterManualTest-NonDefaultFields.greenshot`
with a thicker magenta outline, light-green fill, and shadow disabled. The
test asserts these values survive safe-reader round-tripping; opening this
non-default artifact in the older app displayed the expected rectangle.
The older app has also been confirmed to load the ellipse and line basic-shapes
artifact, SVG and metafile fixtures, the standalone brightness and grayscale
filter fixtures, and the unrotated emoji fixture. The all-supported-containers
artifact now includes every currently supported writer container over the test
background image, omitting the separate brightness and grayscale containers as
requested; its old-app validation is still pending. Cursor writing is
implemented, including both bitmap layers and cursor dimensions/hotspot, and
has a standalone generated artifact; old-app validation of that artifact is
pending. Template writing also remains open. Normal application saves remain
V2-only, and V1 writing is not yet complete.

### Writer implementation sequence

Implement the writer in independently testable stages:

1. Extend `LegacyNrbfWriter` beside the safe reader. It accepts a
   `LegacyDrawableContainerList`, leaves the destination open, and emits the
   populated typed item array. It currently supports rectangle, ellipse, line,
   arrow, text, freehand, speech-bubble, image, icon, highlight/obfuscate
   filters, SVG, metafile, step-label, emoji, and cursor. Focused older-app
   compatibility checks for cursor and the combined artifact remain open.
2. Verify each container's historical member metadata and references against
   fixtures and older-app output. The writer currently covers the basic shapes,
   text, image, icon, freehand, speech-bubble, highlight/obfuscate filters,
   SVG, metafile, step-label, emoji, and cursor. Cursor bitmap layers are
   encoded as PNG-backed `System.Drawing.Bitmap` records; its legacy wrapper
   contains the two layers, cursor size, and hotspot. The older app has
   confirmed rectangle, ellipse, line, SVG, metafile, brightness, grayscale,
   and emoji fixtures. Manual old-app validation of the cursor and combined
   all-supported artifacts remains pending.
3. Field records and integer, boolean, floating-point, string, color, and
   selected enum values are implemented and safe-reader tested. Non-empty
   child arrays and filter records are supported; focused tests cover reading
   their reconstructed legacy properties. Unsupported object/value kinds
   throw a clear `NotSupportedException`.
4. Drawable writer support is implemented for basic shapes, text, image, icon,
   freehand, speech-bubble, highlight/obfuscate filters, SVG, metafile,
   step-label, emoji, and cursor. Cursor and combined-artifact compatibility
   checks in the older app remain pending.
5. Add template payload writing using the same graph writer, with no editor
   envelope.
6. Add editor-file writing: PNG plus the container payload, payload length, and
   V1.04 marker are emitted for the manual compatibility artifacts. Validate
   overflow, truncation, stream positioning, and overwrite behavior.
7. Only after the writer is correct in isolation, expose it through an
   explicit V1 save entry point. Do not change the default V2 save path.

For each stage, add focused tests for empty/single/multiple containers,
nested children, field values, and supported media objects. Test stream
position and lengths, malformed or unsupported input, and safe-reader
round-tripping. Then run the whole legacy fixture suite and verify generated
V1.04 editor and template files with an actual older Greenshot reader. If an
older application cannot run in automated tests, retain a reproducible manual
compatibility procedure and its tested application version; safe-reader
round-tripping alone is not a compatibility sign-off.

The precise record-level contract—including historical type and assembly
identities, member names, member types, collection representation, and image
representations—must be confirmed against real files and the existing V1
reader behavior before implementation. The writer does not need to reproduce
the original file's exact bytes, but its output must be consumable by old
Greenshot V1 readers.

## Work plan

### 1. Capture the compatibility contract

- Collect representative V1.02, V1.03, and V1.04 editor files and template
  files, including examples with every supported container type.
- Record fixture-derived NRBF metadata as detailed in
  **Confirmed details and remaining contract work** above. Keep the resulting
  observations and regression assertions alongside the reader/writer tests.
- Record expected behavior from the current loader and, where possible, verify
  that the fixtures open in an older Greenshot application.
- Document the historical NRBF graph and V1.04 envelope. Pay particular
  attention to type identity and `SerializationInfo` member names used by
  `LegacyClasses` constructors.
- Define explicit supported and lossy cases. Existing normalization such as
  metafile conversion is acceptable when documented and tested.

**Exit criteria:** The team can describe the minimum NRBF records and envelope
needed for each supported fixture, and has a known-good compatibility corpus.
The currently confirmed root/list facts are useful initial evidence but do not
meet this exit criterion by themselves.

### 2. Introduce a safe NRBF reader

- Replace `BinaryFormatter.Deserialize` in the V1 loading path with a reader
  that parses NRBF records as data and never instantiates arbitrary serialized
  runtime types.
- Choose a library or implement the required NRBF subset only after verifying
  compatibility with the repository's .NET Framework target and supported
  build environment.
- Map parsed records through a strict allowlist into `LegacyClasses`; do not
  resolve serialized type names to arbitrary CLR types.
- Preserve legacy aliases and version differences currently handled by
  `LegacySerializationBinder` and `LegacyClasses` constructors, including
  defaults inserted for older files.
- Decode serialized image, icon, metafile, and cursor payloads through
  constrained data handling. Do not use `BinaryFormatter` as a fallback.
- Keep current error behavior explicit: malformed, unsupported, or unexpected
  records must fail with a clear format/security error rather than silently
  producing incomplete objects.

**Exit criteria:** Existing fixtures load into equivalent `LegacyClasses`
through the safe reader, with no BinaryFormatter invocation.

### 3. Add an NRBF writer for `LegacyClasses`

- Implement an explicit serializer for the supported legacy object graph.
  Emit the historical V1.04 type/assembly identities and member data expected
  by older Greenshot readers.
- Support the container list, field holders and children, shared field values,
  and each supported container's specific properties.
- Implement deliberate lossy conversions for cases that cannot be represented
  as before, and reject unsupported data explicitly.
- Preserve valid serialization graph references where required by NRBF,
  especially for nested holders and repeated objects.
- Do not rely on `ISerializable.GetObjectData` implementations that currently
  throw, and do not invoke `BinaryFormatter.Serialize`.

**Exit criteria:** Generated container payloads are accepted by the safe reader
and by the legacy reader in an older Greenshot app.

### 4. Write the V1.04 editor and template envelopes

- Add a V1 editor-file save operation accepting screenshot image data and a
  `LegacyDrawableContainerList`.
- Write the screenshot as PNG followed by the NRBF payload, payload length,
  and `Greenshot01.04` marker in the exact order expected by existing readers.
- Add template writing for the payload-only layout.
- Keep V1 writing an explicit legacy operation; do not make it the default
  current-format save path.
- Validate stream positioning, truncation/overwrite behavior, length bounds,
  and image/payload boundaries.

**Exit criteria:** Output files pass structural checks, load through the new
safe reader, and open in an older Greenshot application.

### 5. Test, integrate, and remove BinaryFormatter

- Add regression tests over the fixture corpus for V1.02/.03/.04 reading and
  V1.04 editor/template writing.
- Test all supported container kinds, empty and nested lists, field values,
  image-bearing containers, and documented lossy cases.
- Compare parsed `LegacyClasses` after old-reader and new-reader paths wherever
  the old reader is available; verify generated files using an older app.
- Add negative tests for unknown types, malformed records, invalid lengths,
  and truncated files.
- Remove the V1 `BinaryFormatter` code path, its binder, and any obsolete
  serialization-only code once parity is demonstrated. Check the repository
  for any remaining format-related BinaryFormatter usage.
- Update the file-format documentation to describe the safe NRBF reader and
  writer, supported versions, limitations, and compatibility guarantees.

**Completion criteria:** Greenshot reads existing supported V1 files and writes
V1.04 files readable by older applications, with no BinaryFormatter use in V1
reading or writing.

## Key risks and decisions

- **NRBF library/framework support:** Confirm the selected parser and writer
  work with the project's .NET Framework target and build tooling. If no
  suitable writer exists, implement only the necessary NRBF record subset and
  validate it against old readers.
- **Historical identity requirements:** Older readers may require particular
  assembly-qualified type names and record layouts. Treat compatibility tests
  against older Greenshot as essential, not optional.
- **GDI+ object payloads:** Existing V1 files may encode drawing objects in
  ways that require special parsing or normalization. Handle these narrowly
  and never re-enable unsafe general-purpose object deserialization.
- **Security limits:** Bound file size, record count, nesting depth, image
  payload size, and references while parsing untrusted files.
- **Supported V1 variants:** Keep reading historical versions, but write only
  V1.04 unless compatibility testing demonstrates a need for older output
  variants.
