# Greenshot Translation Guide

This guide provides comprehensive instructions for working with translations in the Greenshot project.

## Overview

Greenshot supports **40 languages** in the main application and varying numbers of languages for different plugins. The application texts are stored in INI language packs, read with Dapplo.Ini. English (en-US) is the base language: every key that is missing in a translation shows the English text.

The help files (`help-{ietf}.html`) and the installer and website translations (`src/Greenshot/Languages/installer`, `src/Greenshot/Languages/website`) are separate and not described here.

### Repository Structure

```
src/
├── Greenshot/
│   └── Languages/
│       ├── greenshot.en-US.ini    (Base language - 598 keys)
│       ├── greenshot.de-DE.ini
│       ├── greenshot.fr-FR.ini
│       └── ... (40 language packs)
├── Greenshot.Plugin.Box/
│   └── Languages/
│       └── greenshot.box.{ietf}.ini    (20 languages)
├── Greenshot.Plugin.Confluence/
│   └── Languages/
│       └── greenshot.confluence.{ietf}.ini    (21 languages)
├── Greenshot.Plugin.Dropbox/
│   └── Languages/
│       └── greenshot.dropbox.{ietf}.ini    (20 languages)
├── Greenshot.Plugin.ExternalCommand/
│   └── Languages/
│       └── greenshot.externalcommand.{ietf}.ini    (21 languages)
├── Greenshot.Plugin.Imgur/
│   └── Languages/
│       └── greenshot.imgur.{ietf}.ini    (22 languages)
├── Greenshot.Plugin.Jira/
│   └── Languages/
│       └── greenshot.jira.{ietf}.ini    (21 languages)
└── Greenshot.Plugin.Office/
    └── Languages/
        └── greenshot.office.{ietf}.ini    (4 languages)
```

All language packs, core and plugins, are installed flat into the `Languages` folder of Greenshot.

## Supported Languages

### Main Application (40 languages)

Arabic (ar-SY), Catalan (ca-CA), Czech (cs-CZ), Danish (da-DK), German (de-DE), Franconian German (de-x-franconia), Greek (el-GR), **English (en-US)** [BASE], Spanish (es-ES), Estonian (et-EE), Persian (fa-IR), Finnish (fi-FI), French (fr-FR), Quebec French (fr-QC), Hebrew (he-IL), Hungarian (hu-HU), Indonesian (id-ID), Italian (it-IT), Japanese (ja-JP), Kabyle (kab-DZ), Korean (ko-KR), Lithuanian (lt-LT), Latvian (lv-LV), Dutch (nl-NL), Norwegian Nynorsk (nn-NO), Polish (pl-PL), Brazilian Portuguese (pt-BR), Portuguese (pt-PT), Romanian (ro-RO), Russian (ru-RU), Slovak (sk-SK), Slovenian (sl-SI), Serbian (sr-RS), Swedish (sv-SE), Turkish (tr-TR), Ukrainian (uk-UA), Valencian (va-VA), Vietnamese (vi-VN), Simplified Chinese (zh-CN), Traditional Chinese (zh-TW)

### Plugin Language Coverage

**Most plugins support 20-22 languages:**
cs-CZ, de-DE, en-US, fr-FR, id-ID, it-IT, ja-JP, kab-DZ, ko-KR, lv-LV, pl-PL, pt-BR, pt-PT, ru-RU, sr-RS, sv-SE, tr-TR, uk-UA, zh-CN, zh-TW

**Some plugins also include:** nl-NL (Confluence, Imgur, Jira), sk-SK (ExternalCommand, Imgur)

**NOTE:** The Office plugin has only 4 languages (de-DE, en-US, pt-BR, tr-TR).

## Translation File Format

### Core Language Pack

File name: `greenshot.{ietf}.ini`, for example `greenshot.de-DE.ini`. Example from `greenshot.de-DE.ini`:

```ini
[__language__]
Description=Deutsch

[Core]
about_title=Über Greenshot
bugreport_cancel=Schließen
clipboard_inuse=Greenshot kann nicht in die Zwischenablage schreiben, da sie vom Prozess {0} blockiert ist.
ClipboardFormat.DIB=Geräteunabhängiges Bitmap (DIB)

[Editor]
...
```

**Sections:**
- `[__language__]` (optional, first section, only in core packs): `Description=` is the language name shown in the language picker, written in that language (e.g. "Deutsch", "Français", "日本語"). It is needed for tags Windows doesn't know, such as `de-x-franconia`; without it the Windows native name of the language is used.
- `[Core]`: general texts (`about_title`, `contextmenu_capturearea`, ...) and the texts of enum values (`WindowCaptureMode.Auto`, `ClipboardFormat.PNG`, ...)
- `[Editor]`: the image editor (`undo`, `align_center`, ...)
- `[Settings]`: the settings dialog; the expert settings keys start with `expert_`
- `[SelfService]`: Self-Service and troubleshooting
- `[Recipe]`: capture recipes, including the labels of the built-in recipe extensions (`extension_border`, ...)

Every key must be inside a section. A key in the wrong section is not found. Write section names exactly as in en-US.

### Plugin Language Pack

File name: `greenshot.{module}.{ietf}.ini`, for example `greenshot.imgur.de-DE.ini`. The modules are `box`, `confluence`, `dropbox`, `externalcommand`, `imgur`, `jira` and `office`. A plugin pack has one section named after the plugin: `[Box]`, `[Confluence]`, `[Dropbox]`, `[ExternalCommand]`, `[Imgur]`, `[Jira]`, `[Office]`. Example from `greenshot.imgur.de-DE.ini`:

```ini
[Imgur]
upload_menu_item=Zu Imgur hochladen
settings_title=Imgur-Einstellungen
delete_question=Sind Sie sicher, dass Sie das Bild {0} von Imgur löschen möchten?
history=Verlauf...
```

### Lines and Values

- One `key=value` per line, without spaces around `=`.
- Keys are case-insensitive, and `_` and `-` are ignored when keys are compared (`about_title`, `AboutTitle` and `about-title` are the same key). Keep the spelling of the en-US file anyway.
- A value is one line. Write `\n` for a line break, `\t` for a tab and `\\` for a backslash:
  ```ini
  about_license=Copyright © 2007-2026 Thomas Braun, Jens Klingen, Robin Krom\nGreenshot comes with ABSOLUTELY NO WARRANTY. ...
  ```
- `;` and `#` inside a value are fine. Only lines starting with `;` or `#` are comments.
- No XML escaping: `&`, `<`, `>` and quotes are written as they are (`tooltip=Open Greenshot Self-Service & Troubleshooting (S)`).
- Files are UTF-8 without BOM, like the existing files.

### Fallback and User Overrides

- A key missing in a translation shows the English (en-US) text. A partial language pack works.
- Languages fall back through their parent language: for `de-DE`, a `greenshot.de.ini` is also used if it exists, then en-US.
- A file with the same name in `%APPDATA%\Greenshot\Languages` overrides single keys of the installed pack. It only needs the sections and keys it changes, so users can fix or add translations without replacing the installed files. This is also the easiest way to try a translation in a running Greenshot.

## Translation Workflow

### 1. Identifying Changes

When the English (en-US) language pack is updated, all other language packs need to be synchronized:

**Check for:**
- **New keys** added to en-US (need translation in all languages; until then English is shown)
- **Removed keys** in en-US (should be removed from all languages)
- **Modified texts** in en-US (translations may need updating)

### 2. Translation Best Practices

#### Understanding Context

- **Sections and key prefixes** indicate related features:
  - `[Editor]` - Image editor features
  - `[Settings]` - Settings/preferences (`expert_*` - expert settings)
  - `[Core]` `contextmenu_*` - Context menu items
  - `[Core]` `clipboard_*` - Clipboard operations
  - `[Core]` `colorpicker_*` - Color picker dialog
  
- **Examine surrounding keys** in the same section or with the same prefix to understand the UI context
- **Check other languages** to see how they handled similar phrases
- **Look at German translations** as they're typically high-quality and maintained

#### Quality Assurance

1. **Reverse translation check**: After translating, mentally translate back to English to verify meaning is preserved
2. **Consistency**: Use the same translation for the same English term throughout
3. **UI constraints**: Keep translations reasonably similar in length to English (some UI space is limited)
4. **Placeholders**: Preserve placeholders like `{0}`, `{1}` in the same order, and keep expressions like `${now:yyyy-MM-dd HH:mm:ss}` unchanged
5. **Keyboard shortcuts**: Keep keyboard shortcut indicators (e.g., `(C)` in "Crop (C)")
6. **Capitalization**: Follow the capitalization conventions of the target language

#### Common Pitfalls

- **Don't** remove or change keys (the part before `=`) or section names
- **Don't** put a key in another section, or before the first section
- **Don't** split a value over several lines; use `\n`
- **Don't** translate placeholder variables like `{0}`, `{1}`
- **Do** keep `\n` line breaks and formatting in multi-line messages
- **Do** keep HTML tags unchanged in HTML-containing messages

### 3. Maintaining Consistency

**Use a glossary** for project-specific terms:
- "Screenshot" - How is this translated in your language?
- "Capture" - Consistent verb for taking screenshots
- "Region" vs "Area" - Choose one consistent term
- "Export" vs "Save" - Understand the distinction
- Plugin names (Box, Dropbox, Imgur, etc.) - Usually not translated

### 4. Adding Context to the English File

The language packs have no comments, the values must stay on one line. Context for translators lives in the language interface in code: every key with a fixed name is a property there, with the English text as documentation (see "Texts in Code" below). Put additional context in the pull request or issue that asks for the translation.

## Texts in Code

Every key with a fixed name is a typed string property on a language interface:

| Section | Interface | Used as |
|---------|-----------|---------|
| `[Core]` | `src/Greenshot.Base/Languages/ICoreLanguage.cs` | `Texts.Core.AboutTitle` |
| `[Editor]` | `src/Greenshot.Base/Languages/IEditorLanguage.cs` | `Texts.Editor.Undo` |
| `[Settings]` | `src/Greenshot.Base/Languages/ISettingsLanguage.cs` | `Texts.Settings.Language` |
| `[SelfService]` | `src/Greenshot.Base/Languages/ISelfServiceLanguage.cs` | `Texts.SelfService.Title` |
| `[Recipe]` | `src/Greenshot.Base/Languages/IRecipeLanguage.cs` | `Texts.Recipe.ImportTitle` |
| `[Imgur]` etc. | `src/Greenshot.Plugin.Imgur/IImgurLanguage.cs` etc. | `Texts.Get<IImgurLanguage>().History` |

In XAML a text is used as `{wpf:Text Editor.Undo}`.

The property name is the key without `_` and `-`, in PascalCase: key `context_menu_title` becomes property `ContextMenuTitle`.

Keys that are only known at runtime are looked up with `Texts.Config.GetTranslation(key)`: the texts of enum values (`WindowCaptureMode.Auto` in `[Core]`) and the labels of the built-in recipe extensions (written as `Recipe.extension_border`).

**Adding a new text:**
1. Add `key=value` to the en-US pack, in the right section
2. Add a string property with the matching name to the language interface (not needed for runtime keys)
3. Add the translations to the other language packs

## Texts in a Plugin

A plugin brings its own language packs, but it can use every text of Greenshot itself: the `[Core]`, `[Editor]`, `[Settings]`, `[SelfService]` and `[Recipe]` sections are loaded by Greenshot before any plugin starts. There is nothing to register for them.

**Shared texts:** use `Texts.Core.Ok` and `Texts.Core.Cancel` (`{wpf:Text Core.Ok}`, `{wpf:Text Core.Cancel}` in XAML) for the OK and Cancel buttons instead of adding own keys. They are translated in all languages, and translators don't have to translate them again for every plugin. Other texts which look the same (Close, URL, Image format, ...) stay in the plugin: their translation often differs per context.

**Own texts**, using Imgur as example:

1. **Language pack:** create `Languages/greenshot.{module}.en-US.ini` in the plugin project, with one section named after the plugin. The module name is the lower-case plugin name:
   ```ini
   [Imgur]
   history=History...
   delete_question=Are you sure you want to delete the image {0} from Imgur?
   ```
   Translations go next to it as `greenshot.imgur.de-DE.ini` etc. A missing translation shows English.

2. **Interface:** add an interface with a string property per key. The source generator of Dapplo.Ini, which comes with the reference to Greenshot.Base, generates the implementation `ImgurLanguageImpl`:
   ```csharp
   [IniLanguageSection("Imgur", ModuleName = "imgur")]
   public interface IImgurLanguage : INotifyPropertyChanged
   {
       /// <summary>
       /// History...
       /// </summary>
       string History { get; }
   }
   ```
   The section name must match the `[Imgur]` of the pack, `ModuleName` the `imgur` of the file name.

3. **Register** the section as the first thing in `ConfigureServices` of the plugin, so the texts can be used from then on:
   ```csharp
   public void ConfigureServices(IPluginServices services)
   {
       Texts.Register<IImgurLanguage>(new ImgurLanguageImpl());
       ...
   }
   ```
   In code the texts are then available as `Texts.Get<IImgurLanguage>().History`.

4. **Project file:** copy the packs to the output; the build copies them from there into the `Languages` folder of Greenshot:
   ```xml
   <None Include="Languages\greenshot*.ini">
     <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
   </None>
   ```

5. **Installer:** add the packs to the plugin's file in `src/Greenshot-Installer/includes/plugins/`, installed flat into `{app}\Languages`:
   ```
   Source: {#SolutionDir}\Greenshot.Plugin.Imgur\Languages\greenshot.imgur.*.ini; DestDir: {app}\Languages; Components: plugins\imgur; Flags: {#DefaultInstallFlags};
   ```

6. **XAML:** add the namespace and use `{wpf:Text Section.Property}`:
   ```xml
   <Window ...
           xmlns:wpf="clr-namespace:Greenshot.Base.Wpf;assembly=Greenshot.Base"
           Title="{wpf:Text Imgur.History}">
       <Button Content="{wpf:Text Core.Ok}" IsDefault="True" />
   ```

**What `{wpf:Text ...}` does:** it is a markup extension in Greenshot.Base. `Imgur.History` names a section and a property; the extension finds the registered section object for `Imgur` and creates a normal WPF one-way `Binding` to its `History` property. The section objects raise `PropertyChanged` for the texts that change when the user picks another language, so an open window updates by itself. The unit test `LanguagePackTests.XamlTexts_ReferenceExistingProperties` checks that every `{wpf:Text ...}` in the XAML files names an existing property.

**Texts set from code** (WinForms controls, menu items) don't update by themselves. Set them again in a handler of `Texts.Config.LanguageChanged`, and remove the handler when the plugin shuts down:
```csharp
Texts.Config.LanguageChanged += OnLanguageChanged;

private void OnLanguageChanged(object sender, EventArgs e)
{
    _historyMenuItem.Text = Texts.Get<IImgurLanguage>().History;
}
```

**Avalonia:** the language interfaces and the generated sections don't depend on WPF, they work in any UI. Only `{wpf:Text ...}` is WPF specific; in Avalonia the section object can be bound directly, for example with a compiled binding (`x:DataType`), which also checks the property names at build time.

## Validation and Testing

### Manual Checks

1. **Structure**: No line outside a section, every line is `key=value`, a comment or empty
   ```bash
   awk '{ sub(/\r$/, "") }
        FNR == 1 { insection = 0 }
        /^\[.+\]$/ { insection = 1; next }
        /^[[:space:]]*$/ || /^[;#]/ { next }
        !insection || !/=/ { print FILENAME ":" FNR ": " $0 }' src/Greenshot/Languages/greenshot.de-DE.ini
   ```

2. **Encoding**: All language packs must be UTF-8 (without BOM)

3. **Key Count**: Compare key counts between languages (the count includes the `Description=` line)
   ```bash
   grep -c '^[^;#[][^=]*=' src/Greenshot/Languages/greenshot.*.ini
   ```

4. **Empty Values**: Look for keys with an empty value (`about_translation=` is empty on purpose)
   ```bash
   grep -n '^[^;#[][^=]*=[[:space:]]*$' src/Greenshot/Languages/greenshot.*.ini
   ```

See [TRANSLATION_TOOLS.md](TRANSLATION_TOOLS.md) for comparing keys with en-US and for the validation scripts.

### Testing in Greenshot

Copy the language pack to `%APPDATA%\Greenshot\Languages` and restart Greenshot; no build is needed. Remove the file afterwards, otherwise it keeps overriding the installed pack.

### Automated Validation Ideas

Consider creating tools to:
- Compare keys between en-US and other languages
- Detect missing or extra keys
- Verify placeholder consistency (`{0}`, `{1}`, etc.)
- Check for common translation errors
- Check that every key of en-US has a property on the language interface

## Working with Specific Plugins

Each plugin has its own `Languages/` directory with fewer keys than the main application.

**Plugin key counts (en-US):**
- **Main app**: 598 keys
- **Plugins**: 9-25 keys each

**Plugin naming convention:**
- Pattern: `greenshot.{module}.{ietf}.ini`
- Example: `greenshot.box.de-DE.ini`, `greenshot.imgur.fr-FR.ini`

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

## TODO

### Office Plugin
- [ ] Expand Office plugin translations to match main app language coverage

### Documentation
- [ ] Add screenshots of Greenshot UI with key areas labeled
- [ ] Create visual guide showing where different sections appear
- [ ] Build automated translation synchronization tools

## References

- **Language Loader Code**: `src/Greenshot.Base/Languages/Texts.cs`
- **Language Interfaces**: `src/Greenshot.Base/Languages/I*Language.cs`, `src/Greenshot.Plugin.*/I*Language.cs`
- **IETF Language Tags**: [RFC 5646](https://tools.ietf.org/html/rfc5646)
- **Contributing Guidelines**: `CONTRIBUTING.md` (for code style when examining code context)
