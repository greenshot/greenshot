# GreenShotPlus

Based on upstream Windows Greenshot commit `f25a9bba85bdfc1857ff3f14ac16fb192890652a`. Normal capture, drawing, clipboard and export paths are retained.

## Use

- Select a drawing tool or object to show the top colour palette. Choose **Line** or **Fill**, then a swatch. The original colour pickers remain available. Six recent colours share the picker's existing saved history.
- Expand the narrow right-hand **Measurements** panel. A single line or arrow shows **L1**, a rectangle **L1/L2**, and an ellipse **D1/D2**. Double-click a value or press F2 to edit it; Enter commits and Escape cancels. Values follow the chosen pixel/scaled units and exclude line thickness, arrowheads and shadows. Unsupported objects and multiple selections show no properties.
- The same grid shows editable global coordinates: **Start X/Y** and **End X/Y** for lines/arrows; all four named corners for rectangles; **Centre X/Y** for ellipses/circles. Origin is the image's top-left `(0,0)`, with X increasing right and Y down. Coordinates follow the selected units too.
- Editing a line/arrow length keeps its first point and direction. Rectangle width/height edits keep the top-left fixed; ellipse diameter edits keep the centre fixed to the nearest image pixel. Moving a line endpoint keeps the other endpoint fixed. Editing a rectangle corner moves the corresponding edge while keeping the opposite edge fixed and preserving an axis-aligned rectangle. Negative coordinates are allowed. Every committed geometry edit is undoable.
- Geometry remains on Greenshot's integer image-pixel grid: entered scaled lengths/coordinates are rounded to the nearest supported pixel geometry, and the grid displays the resulting actual values. In particular, an ellipse centre may move by half a pixel when switching between odd and even diameters.
- Click the ruler icon (**Scale**), click the first point, release, then click the second point. A moving preview follows the cursor between clicks. Hold **Shift** to use Greenshot's existing 15-degree angle snapping, including horizontal/vertical; the first point stays fixed. Escape, right-click, switching tools or changing documents cancels the unfinished line. Enter the known distance and units. The temporary line is never exported or saved as an object.
- Select pixels, mm, cm, m, inches or feet. Selecting real units before calibration starts Scale. **Calibrate / recalibrate** replaces calibration; **Clear calibration** removes it. Choosing pixels retains calibration for later use.
- Calibration belongs to each image and is saved in `.greenshot` projects. Templates, clipboard objects and raster exports do not carry it. Calibration and display-unit changes are undoable.
- Cropping, rotation and canvas expansion preserve scale. Uniform image resizing adjusts it; nonuniform resizing clears it with a notice. Undo restores the previous calibration.

## Implementation

`Measurements` contains immutable state, conversion, geometry and metadata. `MeasurementGeometry` reads and edits the existing container coordinates using the ordinary bounds-change undo memento. `Surface.Measurements` owns the temporary calibration interaction. UI integration lives in `ImageEditorForm.Measurements`, `MeasurementPanel` and `QuickColorPalette`. Existing shape renderers and the field aggregator are unchanged; `DrawableContainer` gains a nonserialized geometry notification.

## Object snapping

The first action toolbar has independent **Snap to points** and **Snap through** checkboxes, both off by default. Settings apply across tabs in the current editor window. There is no grid.

Snap to points uses line/arrow endpoints and midpoints, rectangle corners, edge midpoints and centre, and ellipse centre/cardinal points. It works for drawing (including scale calibration), endpoint and resize handles, and translating objects or selections without changing their geometry. Active objects and the entire moving selection are excluded as targets. The tolerance is eight screen pixels at any zoom.

Snap through aligns a line/arrow or calibration endpoint through a target; the endpoint follows the pointer projected onto the forward ray and can extend beyond the target. The target must lie on the resulting segment. Rectangles and ellipses supply targets but do not receive this constraint.

Shift angle/aspect constraints take precedence; incompatible targets are skipped. Point snapping wins over through alignment when both are enabled. Existing centre/aspect modifiers and gesture undo remain in place, and numeric property edits do not snap. A small target marker and dotted through guide appear only while editing, never in saved/exported pixels. Completion, cancellation, tool/tab changes or changing a checkbox clears the guide.

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
