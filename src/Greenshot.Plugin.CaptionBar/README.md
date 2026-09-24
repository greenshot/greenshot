# Greenshot.Plugin.CaptionBar

A Greenshot plugin that automatically adds a caption bar to every screenshot.

**Developer:** Igor Li (I756097) - igor.li@sap.com

## About Greenshot

**Greenshot** is a lightweight, free, and open-source screenshot tool that has been the standard screenshot application at SAP for years. It comes pre-installed on all SAP laptops in Walldorf and is widely used across SAP locations worldwide. Its small footprint, reliability, and extensive plugin support make it an ideal choice for enterprise environments.

This **CaptionBar** plugin extends Greenshot by automatically adding a bottom panel to every screenshot with:
- **Timestamp** on the left side - showing when the screenshot was taken
- **Custom text** on the right side - typically the user's full name and I/C/D-user code (e.g., "John Doe (I756097)")

The plugin works automatically with all export types: save to file, export to Office (Word, Excel, PowerPoint), copy to clipboard, and upload to cloud services.

## Example

![Screenshot with caption bar](https://via.placeholder.com/800x100.png?text=2/1/2026+3:00:34+PM++++++++++++++++++++++++++++++++++++Igor+Li+(I756097))

*Example screenshot with caption bar at the bottom*

## Features

✅ Automatic caption addition to all screenshots
✅ Customizable timestamp with flexible format
✅ Custom text (name, user ID, computer name, etc.)
✅ **Independent text alignment** - separate alignment for timestamp and custom text (Left, Center, Right)
✅ **Smart text wrapping** - automatic multi-line wrapping with dynamic font size reduction
✅ **Adaptive layout** - handles narrow screenshots and long text automatically
✅ Customizable background and text colors
✅ Customizable font and text size
✅ Works with all export methods (file, Office, clipboard)
✅ Easy to enable/disable via configuration
✅ No changes required to core Greenshot code

## Installation

### Quick Installation

1. Compile the plugin:
   ```bash
   cd src\Greenshot.Plugin.CaptionBar
   dotnet build -c Release
   ```

2. Copy the DLL to Greenshot folder:
   - From: `bin\Release\net480\Greenshot.Plugin.CaptionBar.dll`
   - To: `%AppData%\Greenshot\Plugins\` or Greenshot.exe folder

3. Configure in `greenshot.ini`:
   ```ini
   [CaptionBar]
   Enabled=True
   BarHeight=35
   BackgroundColor=64,64,64
   TextColor=White
   CustomText=<Your Name and I/C/D-user>
   ShowTimestamp=True
   TimestampFormat=M/d/yyyy h:mm:ss tt
   FontName=Segoe UI
   FontSize=13
   TimestampAlignment=Near
   CustomTextAlignment=Center
   EnableTextWrapping=True
   MaxLines=2
   MinFontSizePercent=50
   TextPadding=10
   ```

4. Restart Greenshot

## Configuration

All plugin settings are located in the `greenshot.ini` file under the `[CaptionBar]` section.

### Main Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `Enabled` | Enable/disable plugin | `True` |
| `BarHeight` | Caption bar height (pixels) | `35` |
| `BackgroundColor` | Background color (R,G,B) | `64,64,64` |
| `TextColor` | Text color | `White` |
| `CustomText` | Custom text | `<Your Name and I/C/D-user>` |
| `ShowTimestamp` | Show timestamp | `True` |
| `TimestampFormat` | Timestamp format | `M/d/yyyy h:mm:ss tt` |
| `FontName` | Font name | `Segoe UI` |
| `FontSize` | Font size (points) | `13` |
| `TimestampAlignment` | Timestamp alignment (Near/Center/Far) | `Near` (Left) |
| `CustomTextAlignment` | Custom text alignment (Near/Center/Far) | `Center` |
| `EnableTextWrapping` | Enable automatic text wrapping | `True` |
| `MaxLines` | Maximum lines when wrapping (1-4) | `2` |
| `MinFontSizePercent` | Minimum font size as percentage (20-100) | `50` |
| `TextPadding` | Horizontal padding in pixels | `10` |

### Text Alignment & Wrapping

The plugin now supports independent alignment for timestamp and custom text:
- **Near** = Left-aligned
- **Center** = Center-aligned
- **Far** = Right-aligned

When text doesn't fit in the available space, the plugin automatically:
1. Wraps text to multiple lines (up to `MaxLines`)
2. If still doesn't fit, reduces font size by 10% and retries
3. Continues until text fits or `MinFontSizePercent` is reached
4. If text still doesn't fit, truncates with ellipsis

The caption bar height adjusts dynamically based on the actual text size.

### Configuration Examples

See the [greenshot-captionbar-example.ini](greenshot-captionbar-example.ini) file for ready-to-use examples.

## Architecture

The plugin is implemented as a processor (`IProcessor`) that runs automatically after a screenshot is captured but before export. This ensures the caption is present in all exported images regardless of the export method.

### Components

- **CaptionBarPlugin.cs** - main plugin class, implements `IGreenshotPlugin`
- **CaptionBarProcessor.cs** - image processor, implements `IProcessor`
- **CaptionBarConfiguration.cs** - configuration class with `[IniProperty]` attributes

### Execution Order

1. **Capture** - screenshot is created
2. **IProcessor** - plugin adds caption to the image ✅
3. **Destinations** - image with caption is sent to destinations (file, Office, clipboard, etc.)

## Development

### Requirements

- Visual Studio 2019+ or .NET SDK 5.0+
- .NET Framework 4.8+
- Greenshot source code

### Build

```bash
# Build the plugin
cd src\Greenshot.Plugin.CaptionBar
dotnet build -c Release

# Or via Visual Studio
# 1. Open Greenshot.sln
# 2. Build → Build Solution
```

### Testing

1. Compile the plugin
2. Copy the DLL to Greenshot folder
3. Enable the plugin in `greenshot.ini`
4. Restart Greenshot
5. Take a screenshot and check the caption

### Coding Standards

Follow Greenshot coding standards as described in:
- [CONTRIBUTING.md](../../CONTRIBUTING.md)
- [CODE_OF_CONDUCT.md](../../CODE_OF_CONDUCT.md)

## Compatibility

- **Greenshot**: 1.3.x (1.3.0 - 1.3.312+)
- **.NET Framework**: 4.8+
- **Windows**: 10, 11
- **Export**: All formats (PNG, JPG, BMP, Office, clipboard)

**Important:** This plugin is specifically designed for Greenshot 1.3.x. Download Greenshot from: https://getgreenshot.org/downloads/

## Known Limitations

- Timestamp uses system time (not original file creation time)
- Caption is always positioned at the bottom (no top or side options)
- Very narrow screenshots (< 50px width) skip caption bar to prevent errors

## Future Plans

- [x] ~~Graphical user interface for configuration~~ (Implemented - settings dialog via plugin Configure)
- [ ] Support for positioning (top/bottom)
- [ ] Support for variables like `${username}`, `${computername}`
- [ ] Support for icons/logos
- [x] ~~Multi-line captions~~ (Implemented - auto-wrapping with configurable max lines)
- [x] ~~Independent text alignment~~ (Implemented - separate alignment for timestamp and custom text)
- [ ] Gradient backgrounds
- [ ] Semi-transparent backgrounds

## Changelog

All notable changes to this project will be documented in this section.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

### [1.1.0] - 2026-02-03

#### 🎨 Changed
- **Settings Form UI/UX Improvements**
  - Completely redesigned settings form using proper Windows Forms Designer pattern
  - Fixed overlapping labels and input fields
  - Proper control layout with correct spacing and positioning
  - All text now displays fully (ComboBox and TextBox values no longer cut off)
  - Numeric values now display correctly (35, 13, 10 instead of 5, 3, 2)
  - Font names display fully ("Segoe UI" instead of "egoe UI")
  - ComboBox options display completely ("Left (Near)", "Center", "Right (Far)")

#### 🔧 Fixed
- Settings form layout issues causing unusable UI
- Numeric control display errors in Windows Forms
- Text truncation in dropdown menus and input fields
- Manual coordinate calculations replaced with Designer-generated layout

#### 📦 Internal
- Introduced `CaptionBarForm` base class for design-time language support
- Split settings form into proper partial class structure (.cs and .Designer.cs)
- Migrated to Greenshot UI controls (GreenshotCheckBox, GreenshotTextBox, etc.)
- Implemented proper config binding with SectionName/PropertyName pattern
- Reduced code size by 60% (434 → 174 lines) with better maintainability
- Follows Greenshot plugin conventions (same pattern as Imgur, Box, Dropbox plugins)

#### 📝 Documentation
- Updated installation scripts to prevent duplicate CustomText prompts
- Added comprehensive "Updating an Existing Installation" section
- Documented Smart Merge feature for seamless plugin updates
- Improved troubleshooting guide

---

### [1.0.0] - 2026-02-01

#### 🎉 Initial Release

#### ✨ Features
- **Automatic Caption Bar** - Adds caption to every screenshot
- **Timestamp Display** - Configurable date/time format on left side
- **Custom Text** - User name and ID code on right side
- **Independent Text Alignment** - Separate alignment for timestamp and custom text (Near/Center/Far)
- **Smart Text Wrapping** - Automatic multi-line wrapping with dynamic font size reduction
- **Adaptive Layout** - Handles narrow screenshots and long text automatically
- **Customizable Appearance**
  - Configurable bar height (20-200 pixels)
  - Custom background color (RGB)
  - Custom text color
  - Custom font family and size
  - Text padding control
- **Text Wrapping Configuration**
  - Enable/disable automatic wrapping
  - Maximum lines (1-4)
  - Minimum font size percentage (20-100%)
- **Universal Compatibility** - Works with all export methods:
  - Save to file (PNG, JPG, BMP)
  - Copy to clipboard
  - Export to Office (Word, Excel, PowerPoint)
  - Upload to cloud services
  - All Greenshot destinations

#### 🔧 Configuration
- INI-based configuration via `greenshot.ini`
- 14 customizable parameters
- Easy enable/disable switch
- Example configuration included

#### 🚀 Performance
- Lightweight processor implementation
- Minimal overhead on screenshot capture
- No modifications to Greenshot core

#### 📦 Deployment
- PowerShell installation script with admin elevation
- Automatic Greenshot detection and installation
- Smart configuration merge for upgrades
- Comprehensive documentation package

#### 🛡️ Stability
- Handles edge cases (very narrow screenshots, long text)
- Graceful degradation when constraints are exceeded
- Robust error handling and logging

---

## Version Numbering

This project uses [Semantic Versioning](https://semver.org/):
- **MAJOR** version for incompatible API changes
- **MINOR** version for new functionality in a backwards compatible manner
- **PATCH** version for backwards compatible bug fixes

---

## License

This plugin is distributed under the GPL-3.0 license, same as the main Greenshot project.

```
Greenshot - a free and open source screenshot tool
Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 1 of the License, or
(at your option) any later version.
```

## Author

**Igor Li (I756097) - igor.li@sap.com**

This plugin was created for the Greenshot project in accordance with [CODE_OF_CONDUCT.md](../../CODE_OF_CONDUCT.md) and [CONTRIBUTING.md](../../CONTRIBUTING.md).

## Support

For bug reports or feature requests:
- GitHub Issues: https://github.com/greenshot/greenshot/issues
- Greenshot Forum: https://getgreenshot.org/

## Acknowledgments

- The Greenshot development team for an excellent tool and plugin architecture
- The Greenshot community for support and feedback
