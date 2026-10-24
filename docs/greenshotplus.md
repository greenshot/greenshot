# GreenShotPlus

Based on upstream Windows Greenshot commit `f25a9bba85bdfc1857ff3f14ac16fb192890652a`. Normal capture, drawing, clipboard and export paths are retained.

## Use

- Select a drawing tool or object to show the top colour palette. Choose **Line** or **Fill**, then a swatch. The original colour pickers remain available. Six recent colours share the picker's existing saved history.
- Expand the narrow right-hand **Measurements** panel. A single line shows **L1**, a rectangle **L1/L2**, and an ellipse **D1/D2**. Values use image pixels, excluding line thickness and shadows. Unsupported objects and multiple selections show no dimensions.
- Click the ruler icon (**Scale**), then drag between two points of known distance. Enter the distance and units. The temporary line disappears and is never exported or saved as an object.
- Select pixels, mm, cm, m, inches or feet. Selecting real units before calibration starts Scale. **Calibrate / recalibrate** replaces calibration; **Clear calibration** removes it. Choosing pixels retains calibration for later use.
- Calibration belongs to each image and is saved in `.greenshot` projects. Templates, clipboard objects and raster exports do not carry it. Calibration and display-unit changes are undoable.
- Cropping, rotation and canvas expansion preserve scale. Uniform image resizing adjusts it; nonuniform resizing clears it with a notice. Undo restores the previous calibration.

## Implementation

`Measurements` contains immutable state, conversion, geometry and metadata. `Surface.Measurements` owns the temporary calibration interaction. UI integration lives in `ImageEditorForm.Measurements`, `MeasurementPanel` and `QuickColorPalette`. Existing shape renderers and the field aggregator are unchanged; `DrawableContainer` gains a nonserialized geometry notification.

The optional 24-byte versioned metadata trailer follows the serialized element list inside the legacy length-counted payload. Existing projects default to pixels. Older readers can read the original element list and ignore the trailer; saving through an older reader loses calibration. Invalid or unknown metadata is ignored.

## Build and test

The **GreenShotPlus installer** GitHub Actions workflow builds the full Windows installer and verifies a current-user install, installed-file hashes and uninstall on a disposable runner. The setup defaults to **install for me only**, using `%LOCALAPPDATA%\Programs\Greenshot` and a per-user uninstall entry. Choosing **install for all users** requires administrator rights. This installer updates an existing per-user Greenshot installation. Fork builds are unsigned; an organisation's application-control policy may require IT approval.

The reproducible packaging entry point is `installer/build-greenshotplus.ps1`. It includes the existing plugins and native CLI/proxy programs. `installer/test-user-install.ps1` is restricted to disposable GitHub Actions runners.

Use the upstream Windows prerequisites in the root README. A first standalone SDK build can generate credential-placeholder source files too late for inclusion; rerun after generation. Private credentials are unnecessary for editor tests; external uploads require their own configuration.

```powershell
dotnet build src/Greenshot.Tests/Greenshot.Tests.csproj -c Debug
dotnet test src/Greenshot.Tests/Greenshot.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~Greenshot.Tests.Editor&Category!=Performance"
```

Measurement tests cover geometry, units, invalid input, document isolation, undo/redo, transformations, legacy/project serialization, temporary Scale drawing, colour undo and live panel updates. Existing editor tests cover keyboard behaviour, clipboard, rendering and file formats. Performance tests are excluded from this focused run.
