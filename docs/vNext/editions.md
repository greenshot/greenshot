# Downloads and editions

Greenshot 1.4 comes in two editions:

- **Greenshot** (the full version)
- **Greenshot Light**, the basics only. Light is built with `/p:GreenshotEdition=Light` (see `Greenshot.csproj`), which
  compiles the code for plugins, the AI tools, the browser extension and the toasts out of `Greenshot.exe`.

Each edition comes as an installer and as a portable version. The portable versions are described in
[portable.md](portable.md).

## Feature matrix

✓ = available, – = not available.

| | Installer | Installer Light | .zip | .zip Light | PortableApps.com |
|---|:---:|:---:|:---:|:---:|:---:|
| **Download** | `Greenshot-INSTALLER-…exe` | `Greenshot-Light-INSTALLER-…exe` | `Greenshot-PORTABLE-…zip` | `Greenshot-Light-PORTABLE-…zip` | `Greenshot-Light-PORTABLEAPPS-…paf.exe` |
| **Edition** | Greenshot | Light | Greenshot | Light | Light |
| `Greenshot.exe` | 64-bit | AnyCPU¹ | 64-bit | AnyCPU¹ | AnyCPU¹ |
| Capture, editor, recipes that come with Greenshot | ✓ | ✓ | ✓ | ✓ | ✓ |
| Built-in destinations (file, clipboard, printer, editor, …) | ✓ | ✓ | ✓ | ✓ | ✓ |
| Text recognition (OCR, Windows) | ✓ | ✓ | ✓ | ✓ | ✓ |
| Plugins: Box, Confluence, Dropbox, External command, Jira, Office, PDF, QR codes, Recipe editor | ✓ | – | ✓ | – | – |
| Own recipe files (load, import, save) | ✓ | – | ✓ | – | – |
| AI tools (`greenshot-mcp.exe`, a separate download, see [mcp-server.md](mcp-server.md)) | ✓ | – | ✓ | – | – |
| Command line `greenshot-cli.exe` and `greenshot-proxy.exe` | ✓ | – | ✓ | – | – |
| Notifications | toasts² | balloons | balloons | balloons | balloons |
| `greenshot:` links (e.g. `greenshot://self-service`) | ✓ | – | – | – | – |
| Opens images and `.greenshot` files from Explorer (*Open with*) | ✓ | ✓ | – | – | – |
| Start menu entry and uninstall | ✓ | ✓ | – | – | – |
| Start with Windows | installer option and settings | installer option and settings | settings³ | settings³ | settings³ |
| Self-Service window | ✓ | ✓ | ✓ | ✓ | ✓ |
| Settings (`greenshot.ini`) | `%APPDATA%\Greenshot` | `%APPDATA%\Greenshot` | next to `Greenshot.exe` | next to `Greenshot.exe` | `Data\Settings` |
| Log (`Greenshot.log`) | `%LOCALAPPDATA%\Greenshot` | `%LOCALAPPDATA%\Greenshot` | next to `Greenshot.exe` | next to `Greenshot.exe` | `Data\Greenshot` |
| Default folder for captures | Desktop | Desktop | Desktop | Desktop | `Documents\Pictures\Greenshots` of the drive |
| Leaves nothing on the computer⁴ | – | – | ✓ | ✓ | ✓ |

1. AnyCPU runs as a 32-bit process on 32-bit Windows and natively on ARM64. The full version is 64-bit because Windows
   only hands a click on a toast to a 64-bit Greenshot.exe (see `Greenshot.csproj`).
2. Toasts need Windows to show them. Otherwise Greenshot uses the balloons of the tray icon (see
   [notifications.md](notifications.md)).
3. The setting writes the path of this `Greenshot.exe` to the *Run* key of the current user (see [autostart.md](autostart.md)).
   If the portable version is moved, switch it off and on again.
4. The portable versions register no toasts, no file types and no links, and keep their files in their own folder
   (`GreenshotEnvironment.LeavesNoTraces`). Only settings the user changes on purpose, like *Start with Windows*, write
   to the computer.

## Signing

The release builds are signed with the Greenshot code signing certificate. This covers `Greenshot.exe`, its DLLs and
plugins, the installers, and the PortableApps.com Launcher and `.paf.exe`. The continuous builds on GitHub (marked
`UNSTABLE-UNSIGNED`) are not signed.
