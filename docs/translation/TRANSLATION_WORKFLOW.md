# Translation Workflow Checklist

This document provides step-by-step checklists for common translation tasks in the Greenshot project.

## Quick Reference

- **Primary Language**: English (en-US) - `src/Greenshot/Languages/language-en-US.xml`
- **Total Languages**: 39 in main app, 19-21 in most plugins
- **File Format**: XML (UTF-8 with BOM)
- **Glossary**: See `TRANSLATION_GLOSSARY.md`
- **Guide**: See `TRANSLATION_GUIDE.md`

---

## Checklist 1: Adding a New Translation String

When a developer adds a new feature requiring translation:

### For the Developer (Adding to en-US)

- [ ] Add the new resource to `src/Greenshot/Languages/language-en-US.xml`
- [ ] Use a clear, descriptive resource name following existing patterns
  - [ ] Use appropriate prefix (`editor_`, `settings_`, `contextmenu_`, etc.)
  - [ ] Use lowercase with underscores (e.g., `editor_new_feature_title`)
- [ ] Write clear, concise English text
- [ ] Add XML comment above if context is not obvious
  ```xml
  <!-- Title for the new feature dialog -->
  <resource name="feature_dialog_title">Feature Name</resource>
  ```
- [ ] Check for reusable existing strings before adding new ones
- [ ] If the string contains placeholders, document them:
  ```xml
  <!-- {0} = filename, {1} = error message -->
  <resource name="error_saving_file">Could not save {0}: {1}</resource>
  ```
- [ ] Commit the English file change
- [ ] Create issue/task for translators to update other languages

### For Plugin Developers

- [ ] Add to plugin-specific language files (e.g., `language_box-en-US.xml`)
- [ ] Follow same naming conventions as main app
- [ ] Consider if the string should also be in the main app

---

## Checklist 2: Translating New Strings

When new strings appear in the English file:

### Preparation

- [ ] Pull latest changes from repository
- [ ] Identify which languages need updating (compare with en-US)
- [ ] Check the glossary (`TRANSLATION_GLOSSARY.md`) for standard terms
- [ ] Review context of the new strings:
  - [ ] Look at resource name prefix
  - [ ] Read any XML comments
  - [ ] Check how similar strings are translated

### Translation Process

For each language you're translating:

- [ ] Open the target language file (e.g., `language-de-DE.xml`)
- [ ] Find the location where the new string should be inserted
  - [ ] Keep the same order as the English file for easier comparison
  - [ ] Group related strings together
- [ ] Add the new `<resource>` element with the same `name` attribute
- [ ] Translate the content:
  - [ ] Use glossary terms for consistency
  - [ ] Preserve placeholders (`{0}`, `{1}`, etc.) in correct grammatical position
  - [ ] Keep keyboard shortcuts (e.g., `(C)`) if present
  - [ ] Maintain similar length to English if possible (UI space constraints)
- [ ] Perform reverse translation check:
  - [ ] Translate your translation back to English mentally
  - [ ] Verify meaning is preserved
  - [ ] Adjust if meaning has shifted
- [ ] Compare with similar strings in other languages for consistency
- [ ] Save file with UTF-8 encoding

### Quality Checks

- [ ] Validate XML syntax:
  ```bash
  xmllint --noout src/Greenshot/Languages/language-XX-YY.xml
  ```
- [ ] Check for typos and grammatical errors
- [ ] Verify no resource names were changed (only content translated)
- [ ] Test file in Greenshot if possible (see Testing section)

---

## Checklist 3: Updating Changed Strings

When English strings are modified:

### Identify Changes

- [ ] Compare old and new versions of `language-en-US.xml`
- [ ] List changed resources (use git diff or comparison tool)
- [ ] Understand WHY each change was made:
  - [ ] Typo fix → Minor change
  - [ ] Clarity improvement → May need rethinking translation
  - [ ] Feature change → Requires new translation

### Update Translations

For each changed string in each language:

- [ ] Open the language file
- [ ] Find the corresponding resource
- [ ] Review the English change
- [ ] Update translation accordingly:
  - [ ] Minor English fixes may need minor translation fixes
  - [ ] Significant changes require re-translation
  - [ ] Consider if old translation is still valid despite English change
- [ ] Add comment if translation reasoning is not obvious
- [ ] Perform reverse translation check

---

## Checklist 4: Removing Obsolete Strings

When features are removed and strings are no longer needed:

### Verification

- [ ] Confirm the resource is removed from `language-en-US.xml`
- [ ] Search codebase to verify the resource is truly unused:
  ```bash
  grep -r "resource_name" src/
  ```
- [ ] Check if resource is used in multiple places (main app + plugins)

### Removal

For each language:

- [ ] Open the language file
- [ ] Find and remove the obsolete resource
- [ ] Save the file
- [ ] Note the removal in commit message

---

## Checklist 5: Adding a New Language

When adding support for a completely new language:

### Setup

- [ ] Determine the correct IETF language tag (e.g., `pt-BR`, `zh-CN`)
- [ ] Find the Windows language group number (see MSDN docs)
- [ ] Create new file: `src/Greenshot/Languages/language-XX-YY.xml`

### File Creation

- [ ] Copy `language-en-US.xml` as template
- [ ] Update XML header:
  ```xml
  <language description="[Language in itself]" ietf="XX-YY" version="1.0.0" languagegroup="N">
  ```
- [ ] Translate all resources:
  - [ ] Start with critical UI elements (menus, buttons)
  - [ ] Then settings and dialogs
  - [ ] Finally help text and detailed messages
- [ ] Use glossary to maintain consistency from the start

### Plugin Support

- [ ] Decide which plugins to support initially
- [ ] Create corresponding plugin language files:
  - `language_box-XX-YY.xml`
  - `language_imgur-XX-YY.xml`
  - etc.
- [ ] Translate plugin strings (usually 5-20 strings each)

### Testing

- [ ] Build Greenshot with new language
- [ ] Verify language appears in language selection
- [ ] Check UI for:
  - [ ] Text truncation issues
  - [ ] Layout problems
  - [ ] Missing translations (showing English keys)
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
  - [ ] XML entities used for special characters
  - [ ] Keyboard shortcuts preserved

### File Validation

- [ ] XML well-formed (use xmllint or validator)
- [ ] UTF-8 encoding with BOM
- [ ] No trailing whitespace in resource values (unless intentional)
- [ ] Consistent indentation (tabs match source file)

### Completeness Check

- [ ] All resources from en-US are present
- [ ] No extra resources not in en-US (unless legacy)
- [ ] No empty resource values (`<resource name="x"></resource>`)
- [ ] Version number in XML header is appropriate

### Testing

- [ ] Build Greenshot (if possible):
  ```powershell
  msbuild src/Greenshot.sln /p:Configuration=Release /t:Rebuild
  ```
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
- [ ] List all resources in en-US (current reference)
- [ ] For each language, identify:
  - [ ] Missing resources
  - [ ] Extra/obsolete resources
  - [ ] Resources to review (marked in English as changed)

### Batch Processing

- [ ] Process languages in priority order:
  1. [ ] Major languages (de-DE, fr-FR, es-ES, ja-JP, zh-CN)
  2. [ ] Secondary languages (other European languages)
  3. [ ] Other languages
- [ ] For each language:
  - [ ] Remove obsolete resources
  - [ ] Add missing resources (translate or mark as TODO)
  - [ ] Update changed resources
  - [ ] Validate file

### Documentation

- [ ] Create a sync report:
  - Which languages were updated
  - How many resources added/removed/changed per language
  - Any languages needing additional attention
- [ ] Update language coverage matrix
- [ ] Note any recurring issues or patterns

---

## Checklist 8: Machine Translation Review

If using machine translation tools (e.g., for initial drafts):

### Before Machine Translation

- [ ] Select an appropriate translation service
- [ ] Prepare context for the translator (screenshots, glossary)
- [ ] Understand tool limitations (may not handle XML well)

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

- [ ] Locate plugin language files: `src/Greenshot.Plugin.{Name}/Languages/`
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

### Issue: XML Validation Errors

**Symptoms**: Build fails, xmllint reports errors

**Checklist**:
- [ ] Check for unescaped special characters (`<`, `>`, `&`)
- [ ] Verify all tags are closed
- [ ] Check for mismatched quotes
- [ ] Ensure UTF-8 encoding with BOM

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
- [ ] Check if BOM (Byte Order Mark) is present
- [ ] Ensure characters are in Unicode range
- [ ] Test with different fonts/systems

---

## Tools and Resources

### Validation Tools

```bash
# Validate XML syntax
xmllint --noout src/Greenshot/Languages/language-XX-YY.xml

# Count resources
grep -c '<resource name=' src/Greenshot/Languages/language-XX-YY.xml

# Find empty resources
grep '<resource name="[^"]*"></resource>' src/Greenshot/Languages/language-XX-YY.xml

# Compare resource keys between languages
diff <(grep 'resource name=' src/Greenshot/Languages/language-en-US.xml | sort) \
     <(grep 'resource name=' src/Greenshot/Languages/language-de-DE.xml | sort)
```

### Recommended Approach

1. **Use a good XML editor**: Visual Studio Code, Notepad++, or specialized XML editors
2. **Enable XML validation**: Catch errors as you type
3. **Use version control**: Git to track changes and compare versions
4. **Test frequently**: Build and run Greenshot to see translations in context
5. **Document decisions**: Add comments for non-obvious translations

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

**Version**: 1.0  
**Last Updated**: 2026-02-03
