---
name: release-documentation
description: Specialist skill for generating and maintaining changelogs, release notes, and blog posts for Greenshot releases, including creating PRs against base branches and gh-pages.
disable-model-invocation: true
---

# Release Documentation Skill

This skill defines the procedures, standards, and workflows for creating and maintaining Greenshot release documentation, changelogs, and release blog posts.

## General Principles

- **Language**: All changelogs, release notes, blog posts, git commits, PR titles, and PR descriptions must be written in **English** (`en-US`).
- **Audience Focus**: Differentiate between user-facing communication (value, clarity, benefits) and developer-facing documentation (technical details, PRs, issues).
- **Official Releases Only**: Only official stable releases receive dedicated release entries. Intermediate or continuous builds (e.g., continuous builds on `main`) do NOT get individual changelog entries; their changes are rolled up into the next official release.
- **Shipped Software Only**: Changelogs and blog posts must include only changes that affect the software or user-visible distribution. Exclude repository-only changes with no shipped-software impact, such as GitHub Actions workflows, internal documentation, agent or skill configuration, CI/build maintenance, and other development-process changes.

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
- A **Contributors** section naming every human contributor whose work is included in the release, including code, documentation, translations, issue reports, reviews, and security disclosures where applicable. Use the contributor's GitHub handle (`@username`) and do not guess identities; verify them from the relevant PR, issue, commit, or advisory.
- Full compare link between release tags (e.g., `https://github.com/greenshot/greenshot/compare/v1.3.312...v1.3.315`).

### Changelog PR Workflow
1. Identify the base branch where the release is cut (e.g., `main`, `release/1.3`).
2. Collect git commit history, PRs, and resolved issues since the previous release. Classify each change by whether it affects shipped software or its user-visible distribution; exclude repository-only and development-process changes.
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
2. **Tone**: Keep the writing casual, warm, and approachable. Write like a helpful announcement to Greenshot users, not like a formal press release. Use contractions where natural, avoid corporate language, and explain technical changes in plain English.
3. **Key Highlights**: Summarize the most significant changes in full, engaging sentences.
4. **Sections**:
   - 🔒 **Security Fixes** (with responsible disclosure credits).
   - 🛠️ **Improvements & Fixes**.
   - 🔮 **What's Next** (e.g. roadmap, next major version status).
5. **Contributor Credits**: Add a short **Thanks to the contributors** section before the full changelog link. Credit every human contributor whose work is included in the release, including code, documentation, translations, issue reports, reviews, and security disclosures where applicable. Use verified GitHub handles (`@username`) and keep the wording warm and casual.
6. **Full Changelog Link**: Conclude with a link to the complete technical changelog in the repository.

Do not mention changes that do not affect shipped software, including GitHub workflow changes, repository documentation, agent or skill changes, CI/build maintenance, and other internal development-process work.

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
- [ ] Only shipped-software or user-visible distribution changes are included.
- [ ] Repository-only changes such as workflows, documentation, agents, skills, and CI maintenance are excluded.
- [ ] User changelog highlights benefits and key fixes in non-technical terms.
- [ ] Technical details section includes PR numbers, issues, and contributor mentions.
- [ ] Every human contributor included in the release is credited by a verified GitHub handle.
- [ ] The blog post includes a casual contributor thank-you section.
- [ ] `docs/changelogs/README.md` latest release info is updated.
- [ ] Changelog PR targets the original code branch (`main` or `release/X.X`).
- [ ] Blog post is placed on `gh-pages` under `_posts/`.
- [ ] Blog post first paragraph links to `https://getgreenshot.org/downloads/`.
- [ ] Blog post PR targets the `gh-pages` branch.
