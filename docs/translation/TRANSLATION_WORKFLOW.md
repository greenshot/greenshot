# Translation Workflow Checklist

This document provides step-by-step checklists for common translation tasks in the Greenshot project.

## Quick Reference

- **Base Language**: English (en-US) - `src/Greenshot/Languages/greenshot.en-US.ini`
- **Total Languages**: 40 in main app, 20-22 in most plugins
- **File Format**: INI language packs (`key=value` in `[Section]`s, UTF-8 without BOM)
- **Missing keys**: show the English text
- **Glossary**: See `TRANSLATION_GLOSSARY.md`
- **Guide**: See `TRANSLATION_GUIDE.md`

---

## Checklist 1: Adding a New Translation String

When a developer adds a new feature requiring translation:

### For the Developer (Adding to en-US)

- [ ] Add the new `key=value` line to `src/Greenshot/Languages/greenshot.en-US.ini`, in the right section (`[Core]`, `[Editor]`, `[Settings]`, `[SelfService]`, `[Recipe]`)
- [ ] Use a clear, descriptive key following existing patterns
  - [ ] Use an existing prefix inside the section where it fits (`contextmenu_`, `clipboard_`, `expert_`, etc.)
  - [ ] Use lowercase with underscores (e.g., `new_feature_title` in `[Editor]`)
- [ ] Write clear, concise English text on one line (`\n` for a line break)
- [ ] Add a string property with the matching name to the language interface, e.g. `ContextMenuTitle` for `context_menu_title` (key without `_` and `-`, PascalCase):
  ```csharp
  /// <summary>
  /// Title for the new feature dialog
  /// </summary>
  string NewFeatureTitle { get; }
  ```
  Runtime keys (enum texts like `WindowCaptureMode.Auto`) need no property, they are read with `Texts.Config.GetTranslation(key)`.
- [ ] Use the text in code as `Texts.Editor.NewFeatureTitle`, in XAML as `{wpf:Text Editor.NewFeatureTitle}`
- [ ] Check for reusable existing strings before adding new ones
- [ ] If the string contains placeholders, document them in the property's comment (language packs have no comments):
  ```csharp
  /// <summary>
  /// Could not save {0}: {1}
  /// {0} = filename, {1} = error message
  /// </summary>
  string ErrorSavingFile { get; }
  ```
- [ ] Commit the English file change
- [ ] Create issue/task for translators to update other languages

### For Plugin Developers

- [ ] Add to the plugin's en-US language pack (e.g., `greenshot.box.en-US.ini`, section `[Box]`)
- [ ] Add the property to the plugin's language interface (e.g., `IBoxLanguage.cs`), use it as `Texts.Get<IBoxLanguage>().NewText`
- [ ] For OK and Cancel use `Texts.Core.Ok` / `Texts.Core.Cancel` instead of own keys
- [ ] New plugin: see "Texts in a Plugin" in [TRANSLATION_GUIDE.md](TRANSLATION_GUIDE.md)
- [ ] Follow same naming conventions as main app
- [ ] Consider if the string should also be in the main app

---

## Checklist 2: Translating New Strings

When new strings appear in the English file:

### Preparation

- [ ] Pull latest changes from repository
- [ ] Identify which languages need updating (compare with en-US, see Tools below)
- [ ] Check the glossary (`TRANSLATION_GLOSSARY.md`) for standard terms
- [ ] Review context of the new strings:
  - [ ] Look at the section and key prefix
  - [ ] Read the comment of the property in the language interface
  - [ ] Check how similar strings are translated

### Translation Process

For each language you're translating:

- [ ] Open the target language pack (e.g., `greenshot.de-DE.ini`)
- [ ] Find the section where the new key belongs (same section as in en-US)
  - [ ] Keep the same order as the English file for easier comparison
  - [ ] Group related strings together
- [ ] Add a `key=value` line with exactly the key of the English file
- [ ] Translate the value:
  - [ ] Use glossary terms for consistency
  - [ ] Preserve placeholders (`{0}`, `{1}`, etc.) in correct grammatical position
  - [ ] Keep `\n` line breaks; the value stays on one line
  - [ ] Keep keyboard shortcuts (e.g., `(C)`) if present
  - [ ] Maintain similar length to English if possible (UI space constraints)
- [ ] Perform reverse translation check:
  - [ ] Translate your translation back to English mentally
  - [ ] Verify meaning is preserved
  - [ ] Adjust if meaning has shifted
- [ ] Compare with similar strings in other languages for consistency
- [ ] Save file with UTF-8 encoding (without BOM)

### Quality Checks

- [ ] Check the structure (no line outside a section, every line `key=value`):
  ```bash
  awk '{ sub(/\r$/, "") }
       FNR == 1 { insection = 0 }
       /^\[.+\]$/ { insection = 1; next }
       /^[[:space:]]*$/ || /^[;#]/ { next }
       !insection || !/=/ { print FILENAME ":" FNR ": " $0 }' src/Greenshot/Languages/greenshot.XX-YY.ini
  ```
- [ ] Check for typos and grammatical errors
- [ ] Verify no keys or section names were changed (only values translated)
- [ ] Test file in Greenshot if possible (see Testing section)

---

## Checklist 3: Updating Changed Strings

When English strings are modified:

### Identify Changes

- [ ] Compare old and new versions of `greenshot.en-US.ini`
- [ ] List changed keys (use git diff or comparison tool)
- [ ] Understand WHY each change was made:
  - [ ] Typo fix → Minor change
  - [ ] Clarity improvement → May need rethinking translation
  - [ ] Feature change → Requires new translation

### Update Translations

For each changed string in each language:

- [ ] Open the language pack
- [ ] Find the corresponding key in the same section
- [ ] Review the English change
- [ ] Update translation accordingly:
  - [ ] Minor English fixes may need minor translation fixes
  - [ ] Significant changes require re-translation
  - [ ] Consider if old translation is still valid despite English change
- [ ] Explain non-obvious translation choices in the commit message or pull request
- [ ] Perform reverse translation check

---

## Checklist 4: Removing Obsolete Strings

When features are removed and strings are no longer needed:

### Verification

- [ ] Confirm the key is removed from `greenshot.en-US.ini`
- [ ] Confirm the property is removed from the language interface
- [ ] Search codebase to verify the text is truly unused, by property name and, for runtime keys, by key:
  ```bash
  grep -rn "ContextMenuTitle" src/ --include=*.cs --include=*.xaml
  grep -rn "extension_border" src/ --include=*.cs
  ```
- [ ] Check if the key is used in multiple places (main app + plugins)

### Removal

For each language:

- [ ] Open the language pack
- [ ] Find and remove the obsolete `key=value` line
- [ ] Save the file
- [ ] Note the removal in commit message

---

## Checklist 5: Adding a New Language

When adding support for a completely new language:

### Setup

- [ ] Determine the correct IETF language tag (e.g., `pt-BR`, `zh-CN`)
- [ ] Create new file: `src/Greenshot/Languages/greenshot.XX-YY.ini`

### File Creation

- [ ] Copy `greenshot.en-US.ini` as template, or start with only the sections and keys you translate (missing keys show English)
- [ ] Set the language name in the first section:
  ```ini
  [__language__]
  Description=[Language in itself]
  ```
  This is needed for tags Windows doesn't know (like `de-x-franconia`); otherwise the Windows native name is used.
- [ ] Translate all keys:
  - [ ] Start with critical UI elements (menus, buttons)
  - [ ] Then settings and dialogs
  - [ ] Finally help text and detailed messages
- [ ] Use glossary to maintain consistency from the start
- [ ] Add the language as an installer component in `src/Greenshot-Installer/includes/languages.iss` (copy the lines of an existing language)

### Plugin Support

- [ ] Decide which plugins to support initially
- [ ] Create corresponding plugin language packs (no `[__language__]` section):
  - `greenshot.box.XX-YY.ini`
  - `greenshot.imgur.XX-YY.ini`
  - etc.
- [ ] Translate plugin strings (9-25 strings each)

### Testing

- [ ] Build Greenshot with new language, or copy the language packs to `%APPDATA%\Greenshot\Languages`
- [ ] Verify language appears in language selection with the right name
- [ ] Check UI for:
  - [ ] Text truncation issues
  - [ ] Layout problems
  - [ ] Missing translations (showing English text)
  - [ ] Character encoding problems

### Documentation

- [ ] Add language to `TRANSLATION_GUIDE.md` supported languages list
- [ ] Add to `TRANSLATION_GLOSSARY.md` if adding first entries
- [ ] Update this checklist if new steps were needed

---

## Checklist 6: Reviewing Translations

Before committing translation work:

### Self-Review

- [ ] Spell check in target language
- [ ] Check grammar and punctuation
- [ ] Verify consistency:
  - [ ] Same term translated same way throughout
  - [ ] Glossary terms used correctly
  - [ ] Formatting matches (capitalization, punctuation)
- [ ] Check technical accuracy:
  - [ ] Placeholders present and correctly positioned
  - [ ] `\n` line breaks kept, every value on one line
  - [ ] Keyboard shortcuts preserved

### File Validation

- [ ] Every key inside a section, every line `key=value`, a comment or empty
- [ ] Sections and keys spelled as in en-US
- [ ] UTF-8 encoding without BOM
- [ ] No trailing whitespace in values (unless intentional)

### Completeness Check

- [ ] All keys from en-US are present (missing keys show English)
- [ ] No extra keys not in en-US (they are never shown)
- [ ] No empty values as placeholders (`key=`); leave the line out instead so English is shown
- [ ] No key appears twice in a section

### Testing

- [ ] Build Greenshot (if possible):
  ```powershell
  msbuild src/Greenshot.sln /p:Configuration=Release /t:Rebuild
  ```
  Or copy the language pack to `%APPDATA%\Greenshot\Languages` and use an installed Greenshot (remove the file afterwards).
- [ ] Launch Greenshot
- [ ] Select your language in settings
- [ ] Navigate through UI checking translations:
  - [ ] Context menu
  - [ ] Main editor window
  - [ ] Settings dialog
  - [ ] Error messages (if testable)
  - [ ] Plugin dialogs

---

## Checklist 7: Synchronizing All Languages

When performing a comprehensive sync of all languages:

### Preparation

- [ ] Create a spreadsheet or tool to track status
- [ ] List all keys in en-US (current reference)
- [ ] For each language, identify:
  - [ ] Missing keys
  - [ ] Extra/obsolete keys
  - [ ] Keys to review (changed in English)

### Batch Processing

- [ ] Process languages in priority order:
  1. [ ] Major languages (de-DE, fr-FR, es-ES, ja-JP, zh-CN)
  2. [ ] Secondary languages (other European languages)
  3. [ ] Other languages
- [ ] For each language:
  - [ ] Remove obsolete keys
  - [ ] Add missing keys (translate them; untranslated keys can stay out, English is shown)
  - [ ] Update changed keys
  - [ ] Validate file

### Documentation

- [ ] Create a sync report:
  - Which languages were updated
  - How many keys added/removed/changed per language
  - Any languages needing additional attention
- [ ] Update language coverage matrix
- [ ] Note any recurring issues or patterns

---

## Checklist 8: Machine Translation Review

If using machine translation tools (e.g., for initial drafts):

### Before Machine Translation

- [ ] Select an appropriate translation service
- [ ] Prepare context for the translator (screenshots, glossary)
- [ ] Understand tool limitations (keys, `\n` and placeholders must survive unchanged)

### After Machine Translation

- [ ] NEVER commit machine translations without review
- [ ] Check every single translation:
  - [ ] Accuracy of meaning
  - [ ] Natural phrasing in target language
  - [ ] Consistency with glossary
  - [ ] Cultural appropriateness
- [ ] Fix common machine translation errors:
  - [ ] Overly literal translations
  - [ ] Incorrect formality level
  - [ ] Lost idioms or context
  - [ ] Wrong term choices (glossary violations)
- [ ] Treat as first draft, requiring full review

---

## Checklist 9: Plugin Translation

When translating plugin-specific strings:

### Understanding Plugin Context

- [ ] Identify which plugin: Box, Imgur, Dropbox, etc.
- [ ] Understand plugin functionality:
  - Read plugin description
  - Check what the plugin does
  - Review English strings for context
- [ ] Note: Plugin names themselves are usually NOT translated

### Translation

- [ ] Locate plugin language packs: `src/Greenshot.Plugin.{Name}/Languages/greenshot.{module}.{ietf}.ini`
- [ ] Keep the single section named after the plugin (`[Box]`, `[Imgur]`, ...)
- [ ] Follow same process as main app translation
- [ ] Keep service-specific terms:
  - [ ] "Box" stays "Box"
  - [ ] "Imgur" stays "Imgur"
  - [ ] API terms may stay in English
- [ ] Coordinate with main app terms:
  - [ ] "Upload" should match main app translation
  - [ ] "Settings" should match main app translation

---

## Common Issues and Solutions

### Issue: Translation Not Shown (English Appears)

**Symptoms**: The English text is shown although the key is translated

**Checklist**:
- [ ] Check the key is in the same section as in en-US
- [ ] Check the key is spelled as in en-US (case, `_` and `-` don't matter)
- [ ] Check no line before the first section holds keys
- [ ] Check the value is on one line (a continuation line is not part of the value)
- [ ] Check the file name: `greenshot.{ietf}.ini` or `greenshot.{module}.{ietf}.ini`
- [ ] Check for an older file with the same name in `%APPDATA%\Greenshot\Languages` that overrides the key

### Issue: Text Truncated in UI

**Symptoms**: Translation is cut off in interface

**Checklist**:
- [ ] Review translation length vs English
- [ ] Consider shorter synonyms
- [ ] Check if UI can be adjusted (report to developers)
- [ ] Use common abbreviations if acceptable in target language

### Issue: Placeholders Not Working

**Symptoms**: `{0}` appears literally in UI instead of value

**Checklist**:
- [ ] Verify placeholder format matches exactly: `{0}`, `{1}`, etc.
- [ ] Check no spaces inside braces: `{ 0 }` is wrong
- [ ] Ensure placeholder count matches English version
- [ ] Verify order is appropriate for target language grammar

### Issue: Characters Display as Boxes/Question Marks

**Symptoms**: Special characters not rendering

**Checklist**:
- [ ] Verify file is UTF-8 encoded
- [ ] Ensure characters are in Unicode range
- [ ] Test with different fonts/systems

---

## Tools and Resources

### Validation Tools

```bash
# Check that no line is outside a section and every line is key=value
awk '{ sub(/\r$/, "") }
     FNR == 1 { insection = 0 }
     /^\[.+\]$/ { insection = 1; next }
     /^[[:space:]]*$/ || /^[;#]/ { next }
     !insection || !/=/ { print FILENAME ":" FNR ": " $0 }' src/Greenshot/Languages/greenshot.XX-YY.ini

# Count keys per section
awk '/^\[/ { section = $0; next } /^[;#]/ || !/=/ { next } { count[section]++ }
     END { for (s in count) printf "%-16s %d\n", s, count[s] }' src/Greenshot/Languages/greenshot.XX-YY.ini | sort

# Find empty values
grep -n '^[^;#[][^=]*=[[:space:]]*$' src/Greenshot/Languages/greenshot.XX-YY.ini

# Compare keys between languages ("<" = missing in XX-YY, ">" = not in en-US)
keys() { awk '/^\[/ { s = $0; next } /^[;#]/ { next } /=/ { print s substr($0, 1, index($0, "=") - 1) }' "$1" | sort; }
diff <(keys src/Greenshot/Languages/greenshot.en-US.ini) \
     <(keys src/Greenshot/Languages/greenshot.XX-YY.ini)
```

The `diff` compares the exact spelling of keys. `compare_keys.sh` and the validation scripts in `TRANSLATION_TOOLS.md` compare keys the way Greenshot does (case-insensitive, `_` and `-` ignored).

### Recommended Approach

1. **Use a good text editor**: Visual Studio Code, Notepad++ or any editor that saves UTF-8 without BOM and doesn't wrap lines when saving
2. **Turn on INI syntax highlighting**: Sections and keys are easier to see
3. **Use version control**: Git to track changes and compare versions
4. **Test frequently**: Copy the language pack to `%APPDATA%\Greenshot\Languages` to see translations in context
5. **Document decisions**: Explain non-obvious translations in the commit message or pull request

---

## Sign-off Checklist

Before submitting translation work:

- [ ] All checklists above completed as applicable
- [ ] Files validated and tested
- [ ] Commit message clearly describes what was translated/updated
- [ ] Changes reviewed by another translator if possible
- [ ] Glossary updated if new terms were established
- [ ] Documentation updated if process changed

---

**Version**: 1.1  
**Last Updated**: 2026-10-03
