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

The first action toolbar has independent **Snap to points** and **Snap through** checkboxes, both off on first use. The checkboxes and **Snapping radius** number are remembered in the normal editor configuration across restarts and apply across tabs. Radius is 1–100 screen pixels (default 8), independent of zoom. There is no grid.

Snap to points uses line/arrow endpoints and midpoints, rectangle/text-box corners, edge midpoints and centre, and ellipse centre/cardinal points. It works for drawing (including scale calibration), endpoint and resize handles, and translating objects or selections without changing their geometry. Active objects and the entire moving selection are excluded as targets. While drawing rectangles, text boxes and ellipses, the provisional bounding-box corners, edge midpoints and centre can align to target points. Shift/Ctrl constraints still apply. Before the first click, the pointer is attracted to nearby targets and the target marker previews the start point.

Snap through aligns a line/arrow or calibration endpoint through a target; the endpoint follows the pointer projected onto the forward ray and can extend beyond the target. The target must lie on the resulting segment. Rectangles and ellipses supply targets but do not receive this constraint.

Shift angle/aspect constraints take precedence; incompatible targets are skipped. Point snapping wins over through alignment when both are enabled. Existing centre/aspect modifiers and gesture undo remain in place, and numeric property edits do not snap. A small target marker and dotted through guide appear only while editing, never in saved/exported pixels. Completion, cancellation, tool/tab changes or changing a checkbox clears the guide.

The optional 24-byte versioned metadata trailer follows the serialized element list inside the legacy length-counted payload. Existing projects default to pixels. Older readers can read the original element list and ignore the trailer; saving through an older reader loses calibration. Invalid or unknown metadata is ignored.

## Polygons

Choose **Polygon** on the drawing toolbar. Click at least three vertices, then click the first vertex (within eight screen pixels) to close. Escape, right-click, changing tools or changing documents cancels unfinished creation without adding an object or undo entry. The preview and editing guides are never exported or saved.

Select a polygon to see numbered vertex handles. Click or drag a vertex to select it; the Measurements panel shows its editable **Vertex N X/Y** coordinates, read-only **Perimeter**, and **Net area (squared units)**. Coordinates use the image top-left origin and selected calibrated units. Values typed into the panel do not snap. Every committed coordinate edit and vertex drag is one undo action; Escape, right-click or switching tools/documents during a vertex drag restores its starting geometry. Vertex numbers remain stable after movement, resize and image transforms. Ordinary bounding-box handles resize the polygon; dragging its body translates it.

Polygon vertices and closed-edge midpoints provide snapping targets. Each new edge can snap to points or through points. Shift constrains an edge to 15-degree steps from the previous vertex. For a closed polygon, Shift constrains the dragged vertex from its visual anticlockwise neighbour, fixed at drag start. Signed area accounts for downward image Y; zero signed area (including self-intersection cancellation) uses the previous vertex index. Shift takes precedence over snapping; point snapping takes precedence over through alignment. There is no grid.

Net area is the absolute signed shoelace area: it is ordinary area for a simple polygon, while overlapping/self-crossing loops may cancel. Perimeter includes every closed edge. These values exclude stroke thickness. Geometry follows integer image pixels, including calibrated numeric edits. Polygon geometry round-trips through clipboard objects and `.greenshot` projects; calibration stays with the document. Older readers do not understand polygon objects.

`PolygonContainer` stores indexed vertices relative to its ordinary container bounds, supports exact whole-image vertex transforms and polygon hit testing, and participates in bounds undo snapshots. Temporary creation and numbered vertex overlays belong to `Surface.Polygons`. Flat grouping and editable object/group rotation are described below.

## Groups

Select at least two objects, then choose **Group** on the drawing toolbar (**Ctrl+Alt+G**). Ctrl+G retains its existing Grayscale action. Choose **Ungroup** (**Ctrl+Shift+G**) to restore independently selectable members. Groups are flat: grouping multiple groups or groups with other objects flattens their membership into one new group. Grouping and ungrouping are each one undo action and preserve object geometry, style and current drawing order.

Clicking any member selects the entire group. Dragging a member, keyboard movement, deletion, cut, copy and duplicate act on the whole group. Tab traverses a group as one selection. Group members display a shared dashed selection box; ungroup before editing individual handles or polygon vertices. Snapping excludes the complete moving selection. Grouping cannot include the image background or temporary drawing/crop guides.

Members remain in their original top-level positions, even when other objects lie between them in the drawing order. Grouping therefore preserves existing overlaps and inverted-filter behavior. Undoing group deletion restores those positions. Explicit bring-forward/send-back actions still operate on the selected members using the editor's existing stacking commands.

`.greenshot` projects retain membership. Copy/paste works across editor windows and keeps relative positions and member drawing order. Every paste and duplicate creates independent objects and fresh group IDs, so editing a pasted group cannot affect its source or another paste. Older projects default to ungrouped objects; older readers may drop group membership when resaving.

## Object and group rotation

Select an object or group and choose **Rotate…** on the drawing toolbar. The dialog shows the current angle for a single object and accepts a **clockwise angle change in degrees**, including fractional angles. Each application is one undo action for the complete selection. Positive angles turn clockwise in image coordinates (Y down). A single object rotates about its transformed local bounding-box centre. Groups and multiple selected objects rotate about the arithmetic mean of their member centres; all members use that same image-space pivot. This pivot remains stable under repeated rotations, preserving relative member offsets and membership without changing paint order.

Rotation supports exact rectangle, ellipse, line, arrow, polygon, text, image, SVG and metafile objects **without filters**. Rotate is disabled for a selection containing unsupported members, with the reason in its tooltip. Bitmap-sampling effects (blur, pixelation, magnification, highlight/grayscale filters, including inverted regions), freehand, speech bubbles, step labels, emoji, cursor, cut/torn edges and temporary crop tools are excluded. Ungroup first to rotate only supported members; unsupported members are never silently flattened or partially rotated.

Rotation retains original local geometry, text and image/vector content and stores a double-precision affine mapping into image coordinates. It composes repeated turns without resampling source images or rounding every vertex at each turn. The displayed current angle is the direction of the transformed local X axis, normalised to -180…180 degrees. Whole-image rotation, crop translation, expansion and resizing compose onto this mapping, including nonuniform scaling; undo restores the mapping. Moving an object applies image-space movement. The public object bounds and hit testing follow its rendered geometry, while resize handles follow its local axes. Groups retain their shared selection box; ungroup to edit member handles.

Polygon vertex handles and their editable Measurements coordinates remain in image coordinates after rotation. Line/arrow endpoints and lengths also describe actual rendered geometry and remain editable. Polygon perimeter/net area use the transformed edges. Rotated rectangles/ellipses show actual lengths along their transformed local axes, accurate image-space centres and angle; these rows are read-only, so use the visible handles for resizing. This avoids pretending that a rotated corner is an axis-aligned rectangle corner. Local integer-pixel geometry maps to fractional image coordinates; editing an image-space coordinate rounds to the nearest supported local pixel geometry and displays the actual result. Typed edits never snap. Snapping targets and pointer gestures follow rendered vertices, endpoints, corners, midpoints and ellipse cardinal points.

Rotated text stays editable: double-click an ungrouped text object to edit it in a dialog, retaining its formatting and rotation. It does not use an unrotated native text overlay on the canvas. Raster exports apply the mapping once through `DrawContent`; the low-level `Draw` renderer continues to draw original model geometry. Rotation state round-trips through projects, clipboard and duplication, using optional fields so old projects retain identity geometry. Older readers may discard rotation metadata and display the original local geometry.

## Build and test

The **GreenShotPlus installer** GitHub Actions workflow builds the full Windows installer and verifies a current-user install, installed-file hashes and uninstall on a disposable runner. The setup defaults to **install for me only**, using `%LOCALAPPDATA%\Programs\Greenshot` and a per-user uninstall entry. Choosing **install for all users** requires administrator rights. This installer updates an existing per-user Greenshot installation. Fork builds are unsigned; an organisation's application-control policy may require IT approval.

The reproducible packaging entry point is `installer/build-greenshotplus.ps1`. It includes the existing plugins and native CLI/proxy programs. `installer/test-user-install.ps1` is restricted to disposable GitHub Actions runners.

Use the upstream Windows prerequisites in the root README. A first standalone SDK build can generate credential-placeholder source files too late for inclusion; rerun after generation. Private credentials are unnecessary for editor tests; external uploads require their own configuration.

```powershell
dotnet build src/Greenshot.Tests/Greenshot.Tests.csproj -c Debug
dotnet test src/Greenshot.Tests/Greenshot.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~Greenshot.Tests.Editor&Category!=Performance"
```

Measurement tests cover geometry, units, invalid input, document isolation, undo/redo, transformations, legacy/project serialization, temporary Scale drawing, colour undo and live panel updates. Existing editor tests cover keyboard behaviour, clipboard, rendering and file formats. Performance tests are excluded from this focused run.
