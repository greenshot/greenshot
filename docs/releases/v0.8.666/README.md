# GreenShotPlus 0.8.666

The Measurements panel now adapts to selection and available space, with a draggable width and clearer tables and controls.

- Drag the panel's left edge to resize it. Its width is kept when collapsed and reopened in the same editor, with limits that preserve canvas space. Very narrow windows collapse the panel automatically; widen the window to reopen it.
- Tables and controls follow the panel width. Table columns and rows can be resized individually, and rows have more breathing room. Edge row heights survive refreshes.
- Empty geometry and edge sections are hidden when nothing is selected. Selected-edge precision appears only when an edge is available. Selecting a different object starts the panel at the top.
- Font and calibration buttons use compact, separate rows. Shorter labels and numbered edge rows reduce repetition and clipping. Collapsible categories and independent dimension backgrounds are retained.
- The installer and installed-app registration display **0.8.666**. The application retains upstream assembly versioning.

## Validation and download

- **341 editor tests passed**, zero failed or skipped. Editor and application builds passed.
- The actual panel was rendered and inspected with no selection, a rectangle and a polygon at narrow and wide widths.
- Layout regressions cover panel dragging, width limits, width memory, small-window behaviour, table sizing, row-height retention, collapsible sections, button text, overlaps and horizontal clipping.
- The installer workflow builds the full EXE and checks current-user installation, exact registered version, package contents and uninstallation on a disposable Windows runner.

Download **GreenShotPlus-Setup.exe** and its **.sha256** checksum from the release assets. Fork builds are unsigned. Publishing this release does not update the existing local installation.
