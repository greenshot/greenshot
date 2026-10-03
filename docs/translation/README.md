# Translation Documentation

This directory contains comprehensive documentation for working with translations in the Greenshot project.

## Quick Start

**New to Greenshot translations?** Start here:
1. Read the [Translation Guide](TRANSLATION_GUIDE.md) to understand the structure
2. Review the [Translation Glossary](TRANSLATION_GLOSSARY.md) for standard terms
3. Follow the appropriate checklist in the [Translation Workflow](TRANSLATION_WORKFLOW.md)
4. Validate your work using tools in [Translation Tools](TRANSLATION_TOOLS.md)

## Documentation Files

### 📖 [TRANSLATION_GUIDE.md](TRANSLATION_GUIDE.md)
**Comprehensive guide to Greenshot translations**

- Overview of the translation system (40 languages, INI language packs)
- Repository structure and file locations
- Language pack format (sections, keys, values, fallback to English)
- How texts are used in code (language interfaces)
- Translation workflow and best practices
- Language-specific notes
- Common issues and workarounds
- TODOs

**Read this first** to understand how translations work in Greenshot.

### 📚 [TRANSLATION_GLOSSARY.md](TRANSLATION_GLOSSARY.md)
**Project-specific terminology and standard translations**

- Core application terms (Screenshot, Capture, Region, Window, etc.)
- Editor terms (Crop, Highlight, Obfuscate, Arrow, etc.)
- Destination terms (Export, Upload, Clipboard, etc.)
- Settings/configuration terms
- Plugin names (usually NOT translated)
- UI elements (buttons, dialogs)
- Special formatting notes (keyboard shortcuts, placeholders, HTML)

**Use this** to ensure consistency across all translations.

### ✅ [TRANSLATION_WORKFLOW.md](TRANSLATION_WORKFLOW.md)
**Step-by-step checklists for translation tasks**

Includes checklists for:
1. Adding a new translation string (for developers)
2. Translating new strings
3. Updating changed strings
4. Removing obsolete strings
5. Adding a new language
6. Reviewing translations
7. Synchronizing all languages
8. Machine translation review
9. Plugin translation

**Follow these checklists** to ensure you don't miss any steps.

### 🛠️ [TRANSLATION_TOOLS.md](TRANSLATION_TOOLS.md)
**Validation tools and automation scripts**

- Manual validation commands (structure check, key counting, comparing keys with en-US)
- Bash scripts for comparing and validating
- Python validation script (cross-platform)
- PowerShell validation script (Windows)
- Integration with build process (pre-commit hooks, GitHub Actions)
- Future enhancement ideas

**Use these tools** to validate your translation work before committing.

## Common Tasks

### I want to translate Greenshot to a new language

1. Read [TRANSLATION_GUIDE.md](TRANSLATION_GUIDE.md) - "Adding a New Language" section
2. Follow Checklist 5 in [TRANSLATION_WORKFLOW.md](TRANSLATION_WORKFLOW.md)
3. Use [TRANSLATION_GLOSSARY.md](TRANSLATION_GLOSSARY.md) for standard terms
4. Validate with [TRANSLATION_TOOLS.md](TRANSLATION_TOOLS.md)

### I want to update existing translations

1. Check what changed in `greenshot.en-US.ini`
2. Follow Checklist 3 in [TRANSLATION_WORKFLOW.md](TRANSLATION_WORKFLOW.md)
3. Refer to [TRANSLATION_GLOSSARY.md](TRANSLATION_GLOSSARY.md) for consistency
4. Validate using tools in [TRANSLATION_TOOLS.md](TRANSLATION_TOOLS.md)

### I'm a developer adding new strings

1. Add `key=value` to `greenshot.en-US.ini` in the right section, and the matching property to the language interface
2. Follow Checklist 1 in [TRANSLATION_WORKFLOW.md](TRANSLATION_WORKFLOW.md)
3. Add context in the property comment if meaning is not obvious
4. Update glossary if introducing new project-specific terms
5. Create an issue for translators to update other languages

### I want to validate translation files

1. See [TRANSLATION_TOOLS.md](TRANSLATION_TOOLS.md) for validation commands
2. Use the Python or PowerShell validation scripts
3. Follow Checklist 6 in [TRANSLATION_WORKFLOW.md](TRANSLATION_WORKFLOW.md) before committing

### I need help understanding translation context

1. Check the section and key prefix in [TRANSLATION_GUIDE.md](TRANSLATION_GUIDE.md)
2. Look at German translation (usually high quality)
3. Search for similar translations in other languages
4. Check if there's a glossary entry in [TRANSLATION_GLOSSARY.md](TRANSLATION_GLOSSARY.md)
5. Ask for clarification if still unclear

## File Locations

### Main Application
```
src/Greenshot/Languages/
├── greenshot.en-US.ini        ← Base language (597 keys)
├── greenshot.de-DE.ini
├── greenshot.fr-FR.ini
├── greenshot.es-ES.ini
└── ... (40 languages)
```

### Plugins
```
src/Greenshot.Plugin.*/Languages/
├── greenshot.{module}.en-US.ini
├── greenshot.{module}.de-DE.ini
└── ... (typically 20-22 languages)
```

### Documentation
```
docs/translation/
├── README.md                     ← This file (documentation index)
├── TRANSLATION_GUIDE.md          ← Comprehensive guide
├── TRANSLATION_GLOSSARY.md       ← Standard terms
├── TRANSLATION_WORKFLOW.md       ← Checklists
├── TRANSLATION_TOOLS.md          ← Validation tools
└── TRANSLATION_ANALYSIS.md       ← Analysis and statistics
```

## Statistics

| Component | Languages | Keys (en-US) | Format |
|-----------|-----------|--------------|--------|
| Main App | 40 | 598 | INI |
| Box Plugin | 20 | 9 | INI |
| Confluence Plugin | 21 | 25 | INI |
| Dropbox Plugin | 20 | 10 | INI |
| ExternalCommand Plugin | 21 | 21 | INI |
| Imgur Plugin | 22 | 23 | INI |
| Jira Plugin | 21 | 22 | INI |
| Office Plugin | 4 | 19 | INI |

Languages include en-US. Main app keys per section: Core 106, Editor 143, Settings 116, Recipe 29, SelfService 204.

## Quality Standards

All translations should meet these criteria:

- ✅ Valid language pack (every key in a section, UTF-8 without BOM)
- ✅ Complete (all keys from en-US present; missing keys show English)
- ✅ Consistent (using glossary terms)
- ✅ Accurate (reverse translation check passed)
- ✅ Natural (sounds native in target language)
- ✅ Tested (no truncation, placeholders work correctly)

## Contributing

When contributing to translation documentation:

1. Keep documentation up-to-date with codebase changes
2. Add examples for clarity
3. Update statistics when language counts change
4. Document any new validation tools or scripts
5. Note language-specific issues discovered during translation

## Translation Agent

The **translation-manager** agent (`.github/agents/translation-manager.md`) is a specialized AI agent for translation work. It:

- Has access to all translation documentation
- Follows the glossary and workflow checklists
- Performs reverse translation checks
- Asks for clarification when context is ambiguous
- Maintains consistency across all languages

When working with the translation agent, reference the appropriate documentation files and checklists.

## Additional Resources

- **Language Loader Code**: `src/Greenshot.Base/Languages/Texts.cs`
- **Language Interfaces**: `src/Greenshot.Base/Languages/I*Language.cs`, `src/Greenshot.Plugin.*/I*Language.cs`
- **IETF Language Tags**: [RFC 5646](https://tools.ietf.org/html/rfc5646)
- **Contributing Guidelines**: `CONTRIBUTING.md` (for code style)

## Support

For translation questions or issues:

1. Check this documentation first
2. Search existing issues on GitHub
3. Ask in discussions or create a new issue
4. Tag with `translation` label

---

**Last Updated**: 2026-10-03
