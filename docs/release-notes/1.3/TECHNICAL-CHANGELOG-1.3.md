---
layout: page
title: "Greenshot 1.3.319 – Technical Changelog"
permalink: /changelogs/greenshot-1.3.319-technical/
---

# Greenshot 1.3.319 – Technical Changelog

Released September 16, 2026

This entry documents the three commits between
[v1.3.318 and v1.3.319](https://github.com/greenshot/greenshot/compare/v1.3.318...v1.3.319)
at the tip of the [`release/1.3`](https://github.com/greenshot/greenshot/tree/release/1.3)
branch.

## Security and networking

- Replaced the process-wide callback that accepted every TLS certificate with
  certificate validation based on the platform policy.
- Added explicit, opt-in configuration for certificate exceptions:
  `AllowedUntrustedCertificateHosts` supports host patterns and
  `AllowedCertificateThumbprints` supports SHA-1/SHA-256 thumbprints.
- Logged rejected certificates and explicitly accepted exceptions, preserving a
  diagnostic trail for configured internal services.

## Capture, clipboard, and image handling

- Made file-descriptor enumeration tolerate malformed or truncated clipboard
  streams instead of failing the complete operation.
- Guarded bitmap creation and off-screen capture against non-positive
  dimensions and converted invalid bitmap errors into the existing capture
  retry/error path.
- Initialized editor capture details consistently and protected editor control
  refreshes while a form or surface is being disposed.
- Fixed centered scaling to expand symmetrically around the click origin.
- Reported `IOException` alongside `ExternalException` when an output image
  cannot be written.

## Runtime stability and resource handling

- Kept the capture-window synchronization target alive until the background
  window-details operation has finished.
- Avoided notification-area icon remove/re-add operations during optimized
  Remote Desktop sessions, preventing the icon from disappearing from the
  client taskbar.
- Changed the menu-selection default image to an instance-owned resource and
  disposed it with the control.
- Hardened OneDrive screenshot-setting detection against missing or truncated
  settings files.
- Added an exit guard so repeated shutdown requests do not run application
  cleanup more than once.

## Upgrade and installer integration

- Added handling for the installer’s `WM_DESTROY` shutdown request so the
  running 1.3 application exits when upgrading to 1.4.
- When an external shutdown closes an edited image, the editor can complete the
  close without leaving the installer in a cancelled or inconsistent state.
- Expanded installer cleanup to remove both portable plugin directory names
  (`Greenshot.Plugin.*`) and newer 1.3 plugin directory names.

## Contributors and source changes

- [Robin Krom](https://github.com/Lakritzator): [PR #1122](https://github.com/greenshot/greenshot/pull/1122)
  (backported 1.4 fixes) and [PR #1092](https://github.com/greenshot/greenshot/pull/1092)
  (installer cleanup).
- [Christian Schulz](https://github.com/Christian-Schulz):
  [PR #1109](https://github.com/greenshot/greenshot/pull/1109) (upgrade and
  Restart Manager integration).

## Release references

- [Release v1.3.319](https://github.com/greenshot/greenshot/releases/tag/v1.3.319)
- [Commit range v1.3.318..v1.3.319](https://github.com/greenshot/greenshot/compare/v1.3.318...v1.3.319)
