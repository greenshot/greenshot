# Greenshot Changelogs

This directory contains comprehensive changelogs for all Greenshot releases.

## Available Changelogs

### Stable Releases

- **[1.3 Series](CHANGELOG-1.3.md)** - All stable releases from 1.3.290 to 1.3.312 (May 2025 - January 2026)
  - 8 stable releases
  - Final release for the 1.3 series
  - Includes security fixes, installer improvements, and portable releases

### Development Releases

- **[1.4 Series](CHANGELOG-1.4.md)** - Continuous development builds (November 2025 - Present)
  - Currently in active development
  - 100+ continuous builds
  - New features: Emoji support, OCR text redaction, improved capture technology
  - ⚠️ Unstable/unsigned builds for testing purposes

## Quick Navigation

### Latest Stable Release
**[Greenshot 1.3.312](CHANGELOG-1.3.md#version-1312-january-7-2026)** (January 7, 2026)
- Fixed external commands broken by security improvements
- [Download Installer](https://github.com/greenshot/greenshot/releases/download/v1.3.312/Greenshot-INSTALLER-1.3.312-RELEASE.exe)
- [Download Portable](https://github.com/greenshot/greenshot/releases/download/v1.3.312/Greenshot-PORTABLE-1.3.312-RELEASE.zip)

### Latest Development Build
**[Greenshot 1.4.107](CHANGELOG-1.4.md)** (February 14, 2026)
- Continuous build from main branch
- ⚠️ Unstable/unsigned - for testing only
- [View all 1.4 builds](https://github.com/greenshot/greenshot/releases)

## Changelog Format

Our changelogs follow these principles:

### User Changelogs
- **User-friendly language**: Focus on benefits and value for users
- **Important changes first**: Security fixes, major features, then minor improvements
- **Natural language**: Avoid overly technical jargon
- **Links to downloads**: Direct download links for stable releases
- **Links to technical details**: Reference to full technical changelog

### Technical Changelogs
- **Pull request references**: Link to PRs for detailed code changes
- **Issue references**: Link to issues that were fixed
- **Contributor attribution**: Credit all contributors
- **Breaking changes**: Clearly marked when applicable
- **Technical details**: Implementation specifics for developers

## Version History Overview

### 1.3 Series (Stable)
- **First Release**: 1.3.290 (May 23, 2025) - Initial stable release, final for 1.3 series
- **Latest Release**: 1.3.312 (January 7, 2026)
- **Total Stable Releases**: 8
- **Key Features**:
  - Security fixes (CVE-2023-34634, GHSA-8f7f-x7ww-xx5w, GHSA-7hvw-q8q5-gpmj)
  - Zoom functionality in editor
  - High-DPI support
  - Portable ZIP releases
  - Installation choice (per-user/all-users)
  - Windows 11 compatibility

### 1.4 Series (Development)
- **First Build**: 1.4.1 (November 18, 2025)
- **Latest Build**: 1.4.107 (February 14, 2026)
- **Status**: Active Development
- **Key Features**:
  - Emoji support in editor
  - OCR-based text redaction (Beta)
  - New capture technology
  - ImageSharp format support
  - Plugin modernization (Confluence REST API, Dropbox OAuth2)
  - Memory leak fixes
  - Better Windows 10/11 integration

## Release Types

### Stable Releases
- **Format**: `1.3.XXX`
- **Signing**: Code-signed with EV certificate
- **Branch**: `release/1.3`
- **Purpose**: Production use
- **Frequency**: As needed for critical fixes
- **Download**: Installer and Portable ZIP

### Continuous Builds
- **Format**: `1.4.XXX-gXXXXXXXXXX`
- **Signing**: Unsigned
- **Branch**: `main`
- **Purpose**: Testing and development
- **Frequency**: Every commit to main branch
- **Download**: Installer and Portable ZIP (UNSTABLE-UNSIGNED)

## Finding Information

### By Feature
- **Security fixes**: Check individual release notes in stable changelogs
- **New editor features**: See 1.3.290 and 1.4 changelogs
- **Plugin improvements**: See 1.4 changelog
- **Installation improvements**: See 1.3.292, 1.3.296

### By Issue/PR
- Each changelog entry includes links to GitHub issues and pull requests
- Use the GitHub repository's search to find specific issues
- Cross-reference with [GitHub Releases](https://github.com/greenshot/greenshot/releases)

### By Date
- Changelogs are organized chronologically within each version series
- Release dates are included in all changelog entries

## Related Documentation

- [Release Management](../release-management.md) - How releases are built and distributed
- [Contributing Guidelines](../../CONTRIBUTING.md) - How to contribute to Greenshot
- [GitHub Releases](https://github.com/greenshot/greenshot/releases) - Download releases and see GitHub-generated changelogs
- [Version History Website](https://getgreenshot.org/version-history/) - Public-facing version history

## Changelog Maintenance

These changelogs are maintained by the Greenshot team and community:

- **1.3 Changelogs**: Generated from GitHub releases for stable builds
- **1.4 Changelogs**: Updated regularly with continuous build information
- **Format**: Markdown for easy reading and web publishing
- **Location**: `docs/changelogs/` in the repository

### For Maintainers

When creating new releases:

1. Update the appropriate changelog file (CHANGELOG-1.3.md or CHANGELOG-1.4.md)
2. Follow the established format (see existing entries as templates)
3. Include both user-friendly descriptions and technical details
4. Add download links for stable releases
5. Credit all contributors
6. Update this README with latest version information

---

*For questions about changelogs or releases, please open an issue on [GitHub](https://github.com/greenshot/greenshot/issues).*
