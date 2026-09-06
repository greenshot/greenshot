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

- Overview of the translation system (39 languages, XML format)
- Repository structure and file locations
- Translation file format (XML and legacy INI)
- Translation workflow and best practices
- Language-specific notes
- Common issues and workarounds
- Migration TODOs

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

- Manual validation commands (xmllint, resource counting, etc.)
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

1. Check what changed in `language-en-US.xml`
2. Follow Checklist 3 in [TRANSLATION_WORKFLOW.md](TRANSLATION_WORKFLOW.md)
3. Refer to [TRANSLATION_GLOSSARY.md](TRANSLATION_GLOSSARY.md) for consistency
4. Validate using tools in [TRANSLATION_TOOLS.md](TRANSLATION_TOOLS.md)

### I'm a developer adding new strings

1. Add to `language-en-US.xml` with clear resource names
2. Follow Checklist 1 in [TRANSLATION_WORKFLOW.md](TRANSLATION_WORKFLOW.md)
3. Add context comments if meaning is not obvious
4. Update glossary if introducing new project-specific terms
5. Create an issue for translators to update other languages

### I want to validate translation files

1. See [TRANSLATION_TOOLS.md](TRANSLATION_TOOLS.md) for validation commands
2. Use the Python or PowerShell validation scripts
3. Follow Checklist 6 in [TRANSLATION_WORKFLOW.md](TRANSLATION_WORKFLOW.md) before committing

### I need help understanding translation context

1. Check resource name prefix in [TRANSLATION_GUIDE.md](TRANSLATION_GUIDE.md)
2. Look at German translation (usually high quality)
3. Search for similar translations in other languages
4. Check if there's a glossary entry in [TRANSLATION_GLOSSARY.md](TRANSLATION_GLOSSARY.md)
5. Ask for clarification if still unclear

## File Locations

### Main Application
```
src/Greenshot/Languages/
├── language-en-US.xml        ← Primary/Reference (281 resources)
├── language-de-DE.xml
├── language-fr-FR.xml
├── language-es-ES.xml
└── ... (39 total languages)
```

### Plugins
```
src/Greenshot.Plugin.*/Languages/
├── language_{plugin}-en-US.xml
├── language_{plugin}-de-DE.xml
└── ... (typically 19-21 languages)
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

| Component | Languages | Resources (en-US) | Format |
|-----------|-----------|-------------------|--------|
| Main App | 39 | 281 | XML |
| Box Plugin | 19 | ~10 | XML |
| Confluence Plugin | 20 | ~20 | XML |
| Dropbox Plugin | 19 | ~10 | XML |
| ExternalCommand Plugin | 20 | ~8 | XML |
| Flickr Plugin | 19 | ~10 | XML |
| GooglePhotos Plugin | 19 | ~10 | XML |
| Imgur Plugin | 21 | ~15 | XML |
| Jira Plugin | 20 | ~20 | XML |
| Office Plugin | 2 | ~6 | INI (legacy) |
| Photobucket Plugin | 19 | ~10 | XML |

## Quality Standards

All translations should meet these criteria:

- ✅ Valid XML (well-formed, UTF-8 with BOM)
- ✅ Complete (all resources from en-US present)
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

- **Language Loader Code**: `src/Greenshot.Base/Core/Language.cs`
- **IETF Language Tags**: [RFC 5646](https://tools.ietf.org/html/rfc5646)
- **Windows Language Groups**: [MSDN Reference](http://msdn.microsoft.com/en-us/goglobal/bb964663#EVD)
- **Contributing Guidelines**: `CONTRIBUTING.md` (for code style)

## Support

For translation questions or issues:

1. Check this documentation first
2. Search existing issues on GitHub
3. Ask in discussions or create a new issue
4. Tag with `translation` label

---

**Last Updated**: 2026-02-03
