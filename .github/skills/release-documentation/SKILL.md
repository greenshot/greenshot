---
name: release-documentation
description: Specialist skill for generating and maintaining changelogs, release notes, and blog posts for Greenshot releases, including creating PRs against base branches and gh-pages.
---

# Release Documentation Skill

This skill defines the procedures, standards, and workflows for creating and maintaining Greenshot release documentation, changelogs, and release blog posts.

## General Principles

- **Language**: All changelogs, release notes, blog posts, git commits, PR titles, and PR descriptions must be written in **English** (`en-US`).
- **Audience Focus**: Differentiate between user-facing communication (value, clarity, benefits) and developer-facing documentation (technical details, PRs, issues).
- **Official Releases Only**: Only official stable releases receive dedicated release entries. Intermediate or continuous builds (e.g., continuous builds on `main`) do NOT get individual changelog entries; their changes are rolled up into the next official release.

---

## 1. Changelog Management (`docs/changelogs/`)

### File Structure & Locations
- **Stable Releases**: `docs/changelogs/CHANGELOG-<MAJOR>.<MINOR>.md` (e.g., `CHANGELOG-1.3.md`)
- **Development Builds**: `docs/changelogs/CHANGELOG-<MAJOR>.<MINOR>.md` (e.g., `CHANGELOG-1.4.md`)
- **Index & Overview**: `docs/changelogs/README.md`

### Content Structure

Every official release entry in `CHANGELOG-<MAJOR>.<MINOR>.md` must contain two complementary parts:

#### A. User Changelog (Top Section)
- **User-friendly tone**: Focus on user value, improvements, and practical benefits.
- **Prioritization**:
  1. 🔒 **Security Fixes** (if any) — describe risk/impact in accessible terms.
  2. ✨ **Major Features / Enhancements**.
  3. 🛠️ **Bug Fixes & Stability**.
  4. 📦 **Installation & Packaging**.
- **Download Links** (for stable releases):
  - Installer link (`https://github.com/greenshot/greenshot/releases/download/v<version>/Greenshot-INSTALLER-<version>-RELEASE.exe`)
  - Portable ZIP link (`https://github.com/greenshot/greenshot/releases/download/v<version>/Greenshot-PORTABLE-<version>-RELEASE.zip`)

#### B. Technical Details (Bottom Section)
- References to GitHub Security Advisories (`GHSA-...`, `CVE-...`).
- References to PRs and Issues: `#<number>` with contributor attribution (`by @username`).
- Full compare link between release tags (e.g., `https://github.com/greenshot/greenshot/compare/v1.3.312...v1.3.315`).

### Changelog PR Workflow
1. Identify the base branch where the release is cut (e.g., `main`, `release/1.3`).
2. Collect git commit history, PRs, and resolved issues since the previous release.
3. Update the appropriate `docs/changelogs/CHANGELOG-X.X.md` and `docs/changelogs/README.md`.
4. Create a dedicated branch off the original branch (e.g., `git checkout -b docs/changelog-<version>`).
5. Commit and push the changes.
6. **Create a Pull Request targeting the ORIGINAL base branch** (e.g. `main` or `release/1.3`).

---

## 2. Release Blog Posts (`gh-pages` branch)

Blog posts announce new official releases on the Greenshot website ([getgreenshot.org](https://getgreenshot.org)).

### Target Branch & File Location
- **Branch**: `gh-pages`
- **File path**: `_posts/YYYY-MM-DD-<title-slug>.md` (or `.markdown`)

### Jekyll Frontmatter
Every blog post must begin with standard frontmatter:
```yaml
---
layout: post
status: publish
published: true
title: Greenshot <VERSION> Released — <Short Highlight Summary>
tags:
- status
- '<MAJOR>.<MINOR>'
---
```

### Blog Post Content Requirements
1. **Mandatory Download Link**: The **first paragraph** MUST include a prominent link to Greenshot's official download page:
   `https://getgreenshot.org/downloads/` (e.g., `We are pleased to announce the release of **Greenshot 1.3.315**... Download it from the [official download page](https://getgreenshot.org/downloads/).`)
2. **Key Highlights**: Summarize the most significant changes in full, engaging sentences.
3. **Sections**:
   - 🔒 **Security Fixes** (with responsible disclosure credits).
   - 🛠️ **Improvements & Fixes**.
   - 🔮 **What's Next** (e.g. roadmap, next major version status).
4. **Full Changelog Link**: Conclude with a link to the complete technical changelog in the repository.

### Blog Post PR Workflow
1. Fetch and branch off the remote `gh-pages` branch:
   ```bash
   git fetch origin gh-pages
   git checkout -b blog/release-<version> origin/gh-pages
   ```
2. Create the blog post file in `_posts/YYYY-MM-DD-<title-slug>.md`.
3. Verify that the first paragraph contains the download page link.
4. Commit the new post:
   ```bash
   git add _posts/YYYY-MM-DD-<title-slug>.md
   git commit -m "Add blog post for Greenshot <VERSION> release"
   ```
5. Push the branch to the remote.
6. **Create a Pull Request targeting the `gh-pages` branch** on `greenshot/greenshot`.

---

## 3. Checklist for Release Documentation

Before completing documentation work:
- [ ] All text is written in clean, natural English.
- [ ] No intermediate/continuous builds were documented as standalone releases.
- [ ] User changelog highlights benefits and key fixes in non-technical terms.
- [ ] Technical details section includes PR numbers, issues, and contributor mentions.
- [ ] `docs/changelogs/README.md` latest release info is updated.
- [ ] Changelog PR targets the original code branch (`main` or `release/X.X`).
- [ ] Blog post is placed on `gh-pages` under `_posts/`.
- [ ] Blog post first paragraph links to `https://getgreenshot.org/downloads/`.
- [ ] Blog post PR targets the `gh-pages` branch.
