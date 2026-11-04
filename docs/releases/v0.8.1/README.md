# GreenShotPlus 0.8.1

Fixes polygon creation after the first click and improves dimension background controls and the Measurements panel.

## Changes

- Polygon creation now keeps its first vertex and preview after releasing the mouse. Windows releases mouse capture after each click; this no longer cancels an unfinished polygon. Click subsequent vertices, then the first vertex to close. Escape, right-click and tool changes still cancel creation; interrupted vertex drags restore their starting geometry.
- Transparent dimension backgrounds remember their previous colour. Unchecking **Transparent background** restores that colour, including after saving and reopening a project.
- Each dimension edge can have its own background colour and transparency. Select its row under **Edge dimensions**, then edit **Dimension background**. Choose **Use document background** to inherit the shared default again. With no edge selected, the controls edit the document default. Overrides support undo/redo and survive projects, clipboard and duplication.
- Measurements settings are grouped under collapsible headers: Object geometry, Edge dimensions, Dimension precision, Dimension background, Dimension font, and Units and calibration. The expanded panel is slightly wider to keep controls readable beside its scrollbar.

## Validation and download

- **340 editor tests passed**, zero failed or skipped.
- Native Windows mouse-message regressions cover the first polygon click, press-drag-release, visible preview after release, completion, vertex-drag undo/redo and interrupted capture rollback.
- Background tests cover independent edges and objects, remembered colours, inheritance, undo/redo, project/clipboard persistence and rendered fills. Panel collapse/expand behaviour is tested and the actual panel rendering was visually checked.
- The full Windows installer workflow builds the EXE and validates current-user installation and uninstallation on a disposable GitHub runner.

Download **GreenShotPlus-Setup.exe** from this release's assets; its SHA-256 checksum is included. Fork builds remain unsigned. This release does not change the installed application until you run the installer.
