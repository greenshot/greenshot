# Greenshot Changelogs

This directory contains comprehensive changelogs for all Greenshot releases.

## Available Changelogs

### Stable Releases

- **[1.3 Series](CHANGELOG-1.3.md)** - All stable releases from 1.3.290 to 1.3.315 (May 2025 - March 2026)
  - 9 stable releases
  - Final release for the 1.3 series
  - Includes security fixes, stability improvements, installation cleanup, and portable releases

### Development Releases

- **[1.4 Series](CHANGELOG-1.4.md)** - Continuous development builds (November 2025 - Present)
  - Currently in active development
  - 100+ continuous builds
  - New features: Emoji support, OCR text redaction, improved capture technology
  - ⚠️ Unstable/unsigned builds for testing purposes

## Quick Navigation

### Latest Stable Release
**[Greenshot 1.3.315](CHANGELOG-1.3.md#version-1315-march-20-2026)** (March 20, 2026)
- Critical security fix (GHSA-f8v9-7fph-fr2j), stability improvements, and installation cleanup
- [Download Installer](https://github.com/greenshot/greenshot/releases/download/v1.3.315/Greenshot-INSTALLER-1.3.315-RELEASE.exe)
- [Download Portable](https://github.com/greenshot/greenshot/releases/download/v1.3.315/Greenshot-PORTABLE-1.3.315-RELEASE.zip)

### Latest Development Build
**[Greenshot 1.4.108](CHANGELOG-1.4.md)** (March 14, 2026)
- Windows Graphics Capture API (Beta) for higher-quality captures
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
- **Latest Release**: 1.3.315 (March 20, 2026)
- **Total Stable Releases**: 9
- **Key Features**:
  - Security fixes (CVE-2023-34634, GHSA-8f7f-x7ww-xx5w, GHSA-7hvw-q8q5-gpmj, GHSA-f8v9-7fph-fr2j)
  - Zoom functionality in editor
  - High-DPI support
  - Portable ZIP releases
  - Installation choice (per-user/all-users)
  - Windows 11 compatibility

### 1.4 Series (Development)
- **First Build**: 1.4.1 (November 18, 2025)
- **Latest Build**: 1.4.108 (March 14, 2026)
- **Status**: Active Development
- **Key Features**:
  - Emoji support in editor
  - OCR-based text redaction (Beta)
  - New capture technology
  - ImageSharp format support
  - Plugin modernization (Confluence REST API, Dropbox OAuth2)
  - Memory leak fixes
  - Better Windows 10/11 integration
  - Windows Restart Manager integration for seamless updates
  - Modern command line interface with System.CommandLine

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

**Important**: Only document **official/stable releases** in these changelog files. Intermediate builds (continuous builds, tagged but not promoted as stable releases) should **not** get their own changelog entries. Instead, their changes are rolled up into the next official release's changelog entry.

---

*For questions about changelogs or releases, please open an issue on [GitHub](https://github.com/greenshot/greenshot/issues).*

*Last updated: March 20, 2026*
