## Features added since v0.3

**v0.4**
- **Snapping preferences:** remembered switches and a zoom-independent radius.
- **Drawing previews:** point attraction and alignment while starting and drawing shapes/text.

**v0.5**
- **Polygons:** click to close; numbered vertices, editable coordinates, perimeter and area.
- **Polygon snapping:** vertices and edge midpoints, with Shift angle constraints.
- **Groups:** group/ungroup; move, delete, duplicate and copy/paste together.
- **Object/group rotation:** editable shapes, text, images and vectors around a shared pivot.
- **Project persistence:** polygon geometry, groups and rotation survive save/reopen.

**v0.6 — this release**
- **Consistent snap preview:** pointer attraction continues between successive objects.
- **New toolbar icons:** Polygon, Group, Ungroup and Rotate; standard icons unchanged.
- **Absolute rotation:** enter 36 degrees and reopen at 36 degrees; groups keep relative member angles.
- **Box selection:** drag with Select; Shift toggles enclosed objects/groups; Escape cancels.
- **Linked dimensions:** per-edge checkboxes for rectangles, polygons, lines and arrows; values follow current units.
- **Shared text style:** one document-wide dimension font, size and colour.
- **Arrow labels:** centred shaft gaps; short arrows keep their heads and place text above.
- **Vertex selection fix:** clicking selects; deliberate dragging moves, without an initial jump.

## Screenshots

Actual editor captures using synthetic demonstration projects.

**Polygon vertices, calibrated dimensions, edge checkboxes and snapping controls**

![Polygon editing and linked dimensions](https://raw.githubusercontent.com/uloxer/GreenShotPlus/greenshotplus-v0.6.0/docs/releases/v0.6.0/polygon-dimensions.png)

**Grouped editable objects**

![Grouped objects](https://raw.githubusercontent.com/uloxer/GreenShotPlus/greenshotplus-v0.6.0/docs/releases/v0.6.0/groups.png)

**Remembered absolute rotation**

![Absolute rotation at 36 degrees](https://raw.githubusercontent.com/uloxer/GreenShotPlus/greenshotplus-v0.6.0/docs/releases/v0.6.0/absolute-rotation.png)

**Box-selection result**

![Box selection of a rectangle and ellipse](https://raw.githubusercontent.com/uloxer/GreenShotPlus/greenshotplus-v0.6.0/docs/releases/v0.6.0/box-selection.png)

## Install and checks

Download **GreenShotPlus-Setup.exe**; its SHA-256 checksum is included. The full Windows installer build and current-user install/uninstall checks run on a disposable GitHub runner. Focused editor tests: **213/213 passed**. Fork builds are unsigned.

Rotation excludes filter effects and special object types. Dimension labels may overlap other objects or clip at the canvas boundary; short arrows use an above-line fallback. Older readers cannot read polygon objects and may discard newer group, rotation or dimension metadata.
