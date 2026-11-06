# Polygon and polyline integration

This branch merges greenshot/greenshot#1591 at 8babdbd79adbbecf1d48ea3fceabb4c7d477d242, preserving its original commits and authorship. It extends that implementation with a closed, filled PolygonContainer and toolbar/Object-menu entry. No external coordination with the PR author is claimed.

PolylineContainer owns vertex storage, per-vertex adorners, angle constraints, exact undo mementos, and transforms. PolygonContainer only requires three vertices, closes on completion, and adds fill drawing and interior hit testing. Surface retains PR #1591's generic click-drawing lifecycle. There is no second competing polyline implementation.

Integration against upstream 44e8b62f3 keeps upstream's DPI-aware IconBinder and screen-coordinate adorner hit tests. Active-handle priority from #1591 is retained. PolylineVertexAdorner was adjusted to receive surface coordinates consistently with current upstream.

When #1591 merges upstream, merge that result first; Git can recognize the reused original commits. Retain the small polygon specialization and its registration/UI additions. Do not replace upstream's newer icon, paint-buffer, HDR, or adorner code wholesale.

No engineering measurements, snapping, grouping or rotation dependencies are included. Branding, licensing, thank-you behavior and installer settings remain upstream defaults.
## Review as a contribution to #1591

PR #1591 already closes a polyline and joins its final edge to the first vertex. Closure is not the new capability here. The additional gap addressed by PolygonContainer is a filled closed polygon: a fill-colour field, interior painting/hit testing, and a minimum of three vertices when completing it. Vertex editing, constraints, transforms and undo continue to use the existing polyline implementation.

Maintainers can review the small polygon specialization and its tests as a proposed delta to #1591 rather than adopt another polyline design. The separate toolbar/Object-menu entry is one discoverability option for that fill capability; it is not a claim that a separate tool is required by the upstream design. No external discussion or coordination with the PR author has occurred.