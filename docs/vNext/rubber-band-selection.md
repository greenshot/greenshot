# Rubber-band selection

In selection mode only, press the left mouse button on empty canvas, then drag at least four screen pixels to select fully enclosed objects. The new gesture starts only where neither an existing object's hit area nor one of its handles is hit. Shift-drag toggles enclosed objects against the selection at drag start. Escape or lost mouse capture restores the original selection.

Below the four-pixel threshold, release is delegated to upstream's existing mouse-up handling: plain empty clicks clear selection, Shift-empty clicks preserve it, and the release-position object hit test is retained even when a tiny motion crosses an object's edge. Existing object clicks, Shift-click toggles, object dragging, resize handles, right-click menus, and drawing tools retain their original handlers. Control is not given a new meaning.

The drag threshold and selection outline follow zoom. Selection changes do not modify drawing geometry or add undo entries. No groups, snapping, measurements or rotation changes are required.