# Greenshot Translation Guide

This guide provides comprehensive instructions for working with translations in the Greenshot project.

## Overview

Greenshot supports **39 languages** in the main application and varying numbers of languages for different plugins. Translation files are stored in XML format (with a few legacy `.ini` files in the Office plugin).

### Repository Structure

```
src/
├── Greenshot/
│   └── Languages/
│       ├── language-en-US.xml    (Primary/Reference language - 281 resources)
│       ├── language-de-DE.xml
│       ├── language-fr-FR.xml
│       └── ... (39 total languages)
├── Greenshot.Plugin.Box/
│   └── Languages/
│       └── language_box-{locale}.xml    (19 languages)
├── Greenshot.Plugin.Confluence/
│   └── Languages/
│       └── language_confluence-{locale}.xml    (20 languages)
├── Greenshot.Plugin.Dropbox/
│   └── Languages/
│       └── language_dropbox-{locale}.xml    (19 languages)
├── Greenshot.Plugin.ExternalCommand/
│   └── Languages/
│       └── language_externalcommand-{locale}.xml    (20 languages)
├── Greenshot.Plugin.Flickr/
│   └── Languages/
│       └── language_flickr-{locale}.xml    (19 languages)
├── Greenshot.Plugin.GooglePhotos/
│   └── Languages/
│       └── language_googlephotos-{locale}.xml    (19 languages)
├── Greenshot.Plugin.Imgur/
│   └── Languages/
│       └── language_imgur-{locale}.xml    (21 languages)
├── Greenshot.Plugin.Jira/
│   └── Languages/
│       └── language_jira-{locale}.xml    (20 languages)
├── Greenshot.Plugin.Office/
│   └── Languages/
│       └── language_office-{locale}.ini    (2 languages - LEGACY FORMAT)
└── Greenshot.Plugin.Photobucket/
    └── Languages/
        └── language_photobucket-{locale}.xml    (19 languages)
```

## Supported Languages

### Main Application (39 languages)

Arabic (ar-SY), Catalan (ca-CA), Czech (cs-CZ), Danish (da-DK), German (de-DE), Franconian German (de-x-franconia), Greek (el-GR), **English (en-US)** [PRIMARY], Spanish (es-ES), Estonian (et-EE), Persian (fa-IR), Finnish (fi-FI), French (fr-FR), Quebec French (fr-QC), Hebrew (he-IL), Hungarian (hu-HU), Indonesian (id-ID), Italian (it-IT), Japanese (ja-JP), Kabyle (kab-DZ), Korean (ko-KR), Lithuanian (lt-LT), Latvian (lv-LV), Dutch (nl-NL), Norwegian (nn-NO), Polish (pl-PL), Brazilian Portuguese (pt-BR), Portuguese (pt-PT), Romanian (ro-RO), Russian (ru-RU), Slovak (sk-SK), Slovenian (sl-SI), Serbian (sr-RS), Swedish (sv-SE), Turkish (tr-TR), Ukrainian (uk-UA), Vietnamese (vi-VN), Simplified Chinese (zh-CN), Traditional Chinese (zh-TW)

### Plugin Language Coverage

**Most plugins support 19-21 languages:**
cs-CZ, de-DE, en-US, fr-FR, id-ID, it-IT, ja-JP, kab-DZ, ko-KR, lv-LV, pl-PL, pt-PT, ru-RU, sr-RS, sv-SE, tr-TR, uk-UA, zh-CN, zh-TW

**Some plugins also include:** nl-NL, sk-SK

**NOTE:** The Office plugin currently only has 2 languages (fr-FR, ja-JP) and uses the legacy `.ini` format instead of XML.

## Translation File Format

### XML Format (Standard)

```xml
<?xml version="1.0" encoding="utf-8"?>
<language description="English" ietf="en-US" version="1.0.4" languagegroup="1">
    <resources>
        <resource name="key_name">Translation text</resource>
        <resource name="another_key">Another translation</resource>
        <!-- Comments can be added for translator context -->
    </resources>
</language>
```

**XML Attributes:**
- `description`: Language name in that language (e.g., "Deutsch", "Français", "日本語")
- `ietf`: IETF language tag (e.g., "en-US", "de-DE", "pt-BR")
- `version`: Version of the translation (should match or be close to app version)
- `languagegroup`: Windows language group identifier (see [MSDN docs](http://msdn.microsoft.com/en-us/goglobal/bb964663#EVD))

**Special Characters:**
- Use XML entities for special characters: `&lt;` `&gt;` `&amp;` `&quot;` `&apos;`
- Unicode characters should be encoded properly in UTF-8
- Newlines are preserved within resource elements

### INI Format (Legacy - Office Plugin Only)

```ini
[language]
description=English
ietf=en-US
version=1.0.0
languagegroup=1
prefix=office

[messages]
settings_title=Office settings
word_lockaspect=Lock image aspect ratio on Word exports
```

**NOTE:** New translations should avoid the `.ini` format. The Office plugin should eventually be migrated to XML format.

## Translation Workflow

### 1. Identifying Changes

When the English (en-US) language file is updated, all other language files need to be synchronized:

**Check for:**
- **New resources** added to en-US (need translation in all languages)
- **Removed resources** in en-US (should be removed from all languages)
- **Modified resources** in en-US (translations may need updating)

### 2. Translation Best Practices

#### Understanding Context

- **Resource name prefixes** indicate related features:
  - `editor_*` - Image editor features
  - `settings_*` - Settings/preferences
  - `contextmenu_*` - Context menu items
  - `clipboard_*` - Clipboard operations
  - `colorpicker_*` - Color picker dialog
  
- **Examine surrounding messages** with the same prefix to understand the UI context
- **Check other languages** to see how they handled similar phrases
- **Look at German translations** as they're typically high-quality and maintained

#### Quality Assurance

1. **Reverse translation check**: After translating, mentally translate back to English to verify meaning is preserved
2. **Consistency**: Use the same translation for the same English term throughout
3. **UI constraints**: Keep translations reasonably similar in length to English (some UI space is limited)
4. **Placeholders**: Preserve placeholders like `{0}`, `{1}` in the same order
5. **Keyboard shortcuts**: Keep keyboard shortcut indicators (e.g., `(C)` in "Crop (C)")
6. **Capitalization**: Follow the capitalization conventions of the target language

#### Common Pitfalls

- **Don't** remove or change resource names (the `name="..."` attribute)
- **Don't** change XML structure or encoding
- **Don't** translate placeholder variables like `{0}`, `{1}`
- **Do** preserve newlines and formatting in multi-line messages
- **Do** keep HTML tags unchanged in HTML-containing messages

### 3. Maintaining Consistency

**Use a glossary** for project-specific terms:
- "Screenshot" - How is this translated in your language?
- "Capture" - Consistent verb for taking screenshots
- "Region" vs "Area" - Choose one consistent term
- "Export" vs "Save" - Understand the distinction
- Plugin names (Box, Dropbox, Imgur, etc.) - Usually not translated

### 4. Adding Context to English File

When adding new resources to the English file, consider adding XML comments to help translators:

```xml
<!-- Button label for confirming deletion -->
<resource name="delete_confirm">Delete</resource>

<!-- Error message when file cannot be saved -->
<resource name="save_error">Could not save file to {0}. Check permissions.</resource>
```

## Validation and Testing

### Manual Checks

1. **XML Well-formedness**: Ensure files are valid XML
   ```bash
   xmllint --noout src/Greenshot/Languages/language-de-DE.xml
   ```

2. **Encoding**: All XML files must be UTF-8 with BOM (`﻿<?xml`)

3. **Resource Count**: Compare resource counts between languages
   ```bash
   grep -c '<resource name=' src/Greenshot/Languages/language-*.xml
   ```

4. **Missing Translations**: Look for empty resource values
   ```bash
   grep '<resource name="[^"]*"></resource>' src/Greenshot/Languages/language-*.xml
   ```

### Automated Validation Ideas

Consider creating tools to:
- Compare resource keys between en-US and other languages
- Detect missing or extra resources
- Verify placeholder consistency (`{0}`, `{1}`, etc.)
- Check for common translation errors
- Validate XML structure and encoding

## Working with Specific Plugins

Each plugin has its own `Languages/` directory with fewer resources than the main application.

**Typical plugin resource counts:**
- **Main app**: ~281 resources
- **Plugins**: ~5-20 resources each

**Plugin naming convention:**
- Pattern: `language_{pluginname}-{locale}.xml`
- Example: `language_box-de-DE.xml`, `language_imgur-fr-FR.xml`

## Translation Priorities

When resources are limited, prioritize:

1. **Main application** (`src/Greenshot/Languages/`) - Most visible to users
2. **Commonly used plugins** (Imgur, Dropbox, Box) - High-traffic features
3. **Error messages** - Critical for troubleshooting
4. **UI labels and buttons** - Core user interaction
5. **Help text** - Nice to have but lower priority

## Language-Specific Notes

### German (de-DE)
- Typically the most up-to-date translation after English
- Can be used as a reference for quality
- Uses formal "Sie" form

### Asian Languages (ja-JP, ko-KR, zh-CN, zh-TW)
- May require more space in UI for same content
- Be mindful of character encoding (UTF-8)
- Traditional vs Simplified Chinese are separate

### Right-to-Left Languages (ar-SY, he-IL)
- Consider text direction in translations
- May require UI adjustments (not just translation)

## Getting Help

If you encounter:
- **Ambiguous English text** - Ask for clarification with context
- **Technical terms** - Check existing translations in German or other complete languages
- **UI/UX questions** - Describe the uncertainty and propose translation options
- **Special characters/encoding issues** - Document the specific characters involved

## Migration TODO

### Office Plugin
- [ ] Convert `language_office-*.ini` files to XML format
- [ ] Expand Office plugin translations to match main app language coverage
- [ ] Update Office plugin to use same translation loading mechanism

### Documentation
- [ ] Add screenshots of Greenshot UI with key areas labeled
- [ ] Create visual guide showing where different resource groups appear
- [ ] Build automated translation synchronization tools

## References

- **Language Loader Code**: `src/Greenshot.Base/Core/Language.cs`
- **IETF Language Tags**: [RFC 5646](https://tools.ietf.org/html/rfc5646)
- **Windows Language Groups**: [MSDN Reference](http://msdn.microsoft.com/en-us/goglobal/bb964663#EVD)
- **Contributing Guidelines**: `CONTRIBUTING.md` (for code style when examining code context)
