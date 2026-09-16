---
layout: page
title: "Greenshot 1.3.319 – Release Notes"
permalink: /changelogs/greenshot-1.3.319/
---

# Greenshot 1.3.319

Released September 16, 2026

Greenshot 1.3.319 is the latest stable release in the 1.3 series. It improves
the safety and reliability of captures, editing, connected services, and
upgrades from Greenshot 1.3 to 1.4.

## Highlights

- **Safer connections to online services:** Greenshot now checks HTTPS/TLS
  certificates normally instead of accepting every certificate. This helps
  protect connected accounts and shared data from interception. Administrators
  can still configure explicit exceptions for trusted internal services.
- **More reliable captures and editing:** malformed clipboard data and unusual
  display or image sizes are handled more safely, reducing capture failures and
  editor errors.
- **Smoother upgrades:** the 1.3 application can close cleanly when the 1.4
  installer requests it, including when an edited image is open.
- **Cleaner installations:** upgrades remove leftover plugin directories from
  older installations and portable copies.
- **Improved remote-desktop behavior:** Greenshot avoids a tray-icon refresh
  that could make the icon disappear during captures in Remote Desktop
  sessions.

## Contributors

Thanks to [Robin Krom](https://github.com/Lakritzator) for the backported fixes
in [PR #1122](https://github.com/greenshot/greenshot/pull/1122) and installer
cleanup in [PR #1092](https://github.com/greenshot/greenshot/pull/1092), and to
[Christian Schulz](https://github.com/Christian-Schulz) for the upgrade and
Restart Manager work in
[PR #1109](https://github.com/greenshot/greenshot/pull/1109).

## Downloads

- [Greenshot downloads](https://getgreenshot.org/downloads/)
- [Greenshot 1.3.319 release and assets](https://github.com/greenshot/greenshot/releases/tag/v1.3.319)

For implementation details, see the
[technical changelog](https://getgreenshot.org/changelogs/greenshot-1.3.319-technical/).

## Changes in this release

This release contains the changes from
[v1.3.318 to v1.3.319](https://github.com/greenshot/greenshot/compare/v1.3.318...v1.3.319).
