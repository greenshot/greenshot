# Image Viewer Translation Update - Summary

## Overview

Successfully added translations for the new Image Viewer feature to all 38 supported language files.

## Changes Summary

### Strings Added (13 per language = 494 total new translations)

1. **settings_destination_viewer** (1 string)
   - Position: After `settings_destination_printer` in the settings section
   - English: "Open in image viewer"
   - Purpose: Destination option to open captured images in the lightweight viewer

2. **viewer_* strings** (12 strings)
   - Position: New section at end of each file (before `</resources>` closing tag)
   - Strings:
     - `viewer_alwaysontop` - Window behavior setting
     - `viewer_close_all` - Menu command
     - `viewer_first_usage_message` - Warning about unsaved images
     - `viewer_first_usage_title` - Dialog title
     - `viewer_hide_cursor` - Menu command to hide captured mouse cursor
     - `viewer_hide_title` - Menu command to hide title bar
     - `viewer_process_again` - Menu command to re-process image
     - `viewer_reset_zoom` - Menu command to reset view
     - `viewer_save` - Save button
     - `viewer_saveas` - Save As button
     - `viewer_show_cursor` - Menu command to show captured mouse cursor
     - `viewer_show_title` - Menu command to show title bar

## Languages Updated (38 total)

### Western European
- de-DE (German)
- de-x-franconia (Franconian German dialect)
- en-GB implicitly covered by en-US
- es-ES (Spanish)
- fr-FR (French)
- fr-QC (French Canadian)
- it-IT (Italian)
- nl-NL (Dutch)
- pt-BR (Portuguese Brazilian)
- pt-PT (Portuguese Portugal)
- ca-CA (Catalan)

### Nordic
- da-DK (Danish)
- fi-FI (Finnish)
- nn-NO (Norwegian Nynorsk)
- sv-SE (Swedish)

### Eastern European
- cs-CZ (Czech)
- et-EE (Estonian)
- hu-HU (Hungarian)
- lt-LT (Lithuanian)
- lv-LV (Latvian)
- pl-PL (Polish)
- ro-RO (Romanian)
- ru-RU (Russian)
- sk-SK (Slovak)
- sl-SI (Slovenian)
- sr-RS (Serbian Cyrillic)
- uk-UA (Ukrainian)

### Middle East
- ar-SY (Arabic Syrian)
- fa-IR (Persian/Farsi)
- he-IL (Hebrew)
- tr-TR (Turkish)

### Asian
- ja-JP (Japanese)
- ko-KR (Korean)
- vi-VN (Vietnamese)
- zh-CN (Chinese Simplified)
- zh-TW (Chinese Traditional)
- id-ID (Indonesian)

### African
- kab-DZ (Kabyle)

### Greek
- el-GR (Greek)

## Translation Quality

### Consistency
- Followed Translation Glossary for standard terms
- "Save" / "Save as..." consistent with existing editor translations
- "Cursor" terminology matches existing usage
- "Always on top" uses standard UI terminology per language

### Language-Specific Considerations
- **Formality**: Formal register maintained (German "Sie", French "vous")
- **Technical terms**: "Zoom" and "pan" kept as loanwords where appropriate
- **Capitalization**: Language-specific rules followed (German nouns capitalized)
- **Character encoding**: Proper use of language-specific characters
  - Hungarian: Corrected 'ő' (double acute) vs 'õ' (tilde)
  - Right-to-left languages (Arabic, Hebrew): Proper text direction maintained

### Special Language Handling
- **de-x-franconia**: Based on de-DE with dialect-appropriate modifications
- **kab-DZ**: Kabyle translations adapted from French base with native terms
- **sr-RS**: Serbian Cyrillic script used appropriately

## Validation

### XML Validation
✅ All 38 files are well-formed XML
✅ UTF-8 encoding with BOM preserved
✅ Proper XML structure maintained

### Completeness Check
✅ All 38 files have exactly 1 `settings_destination_viewer` string
✅ All 38 files have exactly 12 `viewer_*` strings
✅ Total: 13 strings per language × 38 languages = 494 new translations

### Code Review
✅ No issues found (after corrections)
✅ Fixed Franconian dialect spacing
✅ Fixed Hungarian character encoding

### Security Scan (CodeQL)
✅ No security alerts found
✅ No vulnerable patterns detected

## Files Modified

All language files in `src/Greenshot/Languages/`:
- language-ar-SY.xml
- language-ca-CA.xml
- language-cs-CZ.xml
- language-da-DK.xml
- language-de-DE.xml
- language-de-x-franconia.xml
- language-el-GR.xml
- language-es-ES.xml
- language-et-EE.xml
- language-fa-IR.xml
- language-fi-FI.xml
- language-fr-FR.xml
- language-fr-QC.xml
- language-he-IL.xml
- language-hu-HU.xml
- language-id-ID.xml
- language-it-IT.xml
- language-ja-JP.xml
- language-kab-DZ.xml
- language-ko-KR.xml
- language-lt-LT.xml
- language-lv-LV.xml
- language-nl-NL.xml
- language-nn-NO.xml
- language-pl-PL.xml
- language-pt-BR.xml
- language-pt-PT.xml
- language-ro-RO.xml
- language-ru-RU.xml
- language-sk-SK.xml
- language-sl-SI.xml
- language-sr-RS.xml
- language-sv-SE.xml
- language-tr-TR.xml
- language-uk-UA.xml
- language-vi-VN.xml
- language-zh-CN.xml
- language-zh-TW.xml

## Context

The Image Viewer is a new lightweight alternative to the full image editor. It provides:
- Quick viewing of captured screenshots
- Basic operations (save, zoom, pan)
- Minimal UI (can hide title bar)
- Option to show/hide captured mouse cursor
- Ability to re-process images through the editor
- First-use warning about unsaved images

This feature complements the existing editor by offering a simpler, faster option for users who just want to view and save screenshots without editing.

## Translation Approach

1. **Research**: Reviewed existing translations for "Save", "Cursor", "Image", "Editor" terms
2. **Consistency**: Used established terminology from glossary
3. **Context**: Understood feature purpose to translate appropriately
4. **Quality**: Reverse-translation check for ambiguous terms
5. **Validation**: XML well-formedness and completeness verification
6. **Review**: Code review and corrections applied

## Next Steps for Maintainers

1. Review sample translations for accuracy (recommend checking 3-5 key languages)
2. Consider updating TRANSLATION_GLOSSARY.md with new terms if needed
3. Test in at least 2-3 languages to verify UI rendering
4. Monitor user feedback for any translation improvements

---

**Translation Completed**: 2026-02-03
**Translator Agent**: translation-manager
**Total Strings Added**: 494 (13 × 38 languages)
**Quality Checks**: ✅ All passed
