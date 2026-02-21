# Release Documentation Update Summary

**Date**: February 21, 2026  
**Version**: Greenshot 1.4.108  
**Feature**: File Dialog Overhaul

## Overview

Updated all release documentation for the file dialog modernization feature in Greenshot 1.4.108.

## Files Modified

### 1. docs/changelogs/CHANGELOG-1.4.md

**Changes made**:
- Updated "Latest Build" to v1.4.108 (February 21, 2026)
- Added "Modern File Dialogs" entry in **Editor Enhancements** section (user-friendly)
- Added "Modern File and Folder Dialogs" entry in **Configuration & Settings** section (technical details)
- Added v1.4.108 to the **Release Timeline** with feature highlight
- Added feature to **Comparison with 1.3** list
- Updated "Last updated" date to February 21, 2026
- Updated "Latest continuous build" to v1.4.108-g1f6f93d8ea

### 2. docs/changelogs/README.md

**Changes made**:
- Updated "Latest Development Build" to Greenshot 1.4.108 (February 21, 2026)
- Updated "Latest Build" in 1.4 Series section to 1.4.108
- Added "Modern file/folder dialogs with long path support" to Key Features list

## Files Created

### 3. docs/blog-posts/2026-02-modern-file-dialogs.md

**New blog post draft** with:
- User-friendly title and introduction
- Explanation of what changed (legacy dialogs → modern dialogs)
- Key benefits section:
  - Support for long file paths (260+ characters)
  - Modern user experience improvements
  - List of affected areas in the application
- Technical details section (brief, for interested readers)
- Call to action (how to try the development build)
- Links to changelog and releases
- Disclaimer about development build status

### 4. docs/blog-posts/README.md

**New documentation** for blog posts directory:
- Purpose and usage guidelines
- Format and structure recommendations
- Writing style guidelines (user-friendly, natural language, clear structure)
- Index of available blog posts
- Publishing workflow suggestions
- Template for future blog posts

## Documentation Principles Followed

### User-Friendly Language
- Used "modern dialogs" instead of technical API names
- Focused on benefits: "long file paths", "modern experience"
- Avoided jargon like "COM/P-Invoke" in user-facing sections
- Natural, conversational tone

### Technical Details
- Included technical information in separate section
- Referenced specific package (Dapplo.Windows.Dialogs v2.0.63)
- Listed affected components (SaveFileDialog, OpenFileDialog, FolderBrowserDialog)
- Mentioned implementation approach (builder patterns, COM/P-Invoke)

### Important Changes First
- Placed feature in both "Editor Enhancements" (highly visible) and "Configuration & Settings" (logical location)
- Highlighted in release timeline with bold text
- Added to comparison list showing new features in 1.4

### Links and References
- Linked to GitHub releases for downloads
- Referenced complete changelog
- Provided context about development vs. stable builds

## Content Placement

The feature appears in:

1. **CHANGELOG-1.4.md** - Multiple strategic locations:
   - Major Features → Editor Enhancements (user-facing, high visibility)
   - Bug Fixes & Improvements → Configuration & Settings (technical, logical grouping)
   - Release Timeline (chronological listing)
   - Comparison with 1.3 (feature highlights)

2. **README.md** - Summary locations:
   - Latest Development Build info
   - 1.4 Series Key Features list

3. **Blog Post** - Standalone announcement:
   - Full-length article format
   - User-focused content
   - Clear call-to-action

## Consistency

All documentation maintains:
- Version number: 1.4.108
- Date: February 21, 2026
- Commit hash: 1f6f93d8ea
- Feature name: "Modern file/folder dialogs with long path support"
- Key benefit: Long file path support (260+ characters)
- Package: Dapplo.Windows.Dialogs 2.0.63
- Attribution: @Copilot

## Next Steps

For stable release (when 1.4 becomes stable):

1. Create entry in appropriate stable changelog (e.g., CHANGELOG-1.4-stable.md if created)
2. Update blog post status from "Development Build" to "Stable Release"
3. Add direct download links to installer and portable ZIP
4. Publish blog post to website
5. Create release announcement with links to blog post
6. Update version history on website (automated via GitHub workflow)

## Notes

- All documentation follows the established Greenshot documentation style
- User changelog focuses on value and benefits
- Technical changelog includes implementation details
- Blog post provides engaging, accessible content for general audience
- Maintained consistency with existing 1.4 changelog entries
