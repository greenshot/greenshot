---
name: release-documentation
description: Specialist skill for generating and maintaining changelogs, release notes, and blog posts for Greenshot releases, including creating PRs against base branches and gh-pages.
disable-model-invocation: true
---

# Release Documentation Skill

This skill defines the procedures, standards, and workflows for creating and maintaining Greenshot release documentation, changelogs, and release blog posts.

## General Principles

- **Language**: All changelogs, release notes, blog posts, git commits, PR titles, and PR descriptions must be written in **English** (`en-US`).
- **One Changelog Per Release**: There is no separate "user" vs "technical" changelog. Each official release gets exactly one changelog entry that is both user-understandable and properly attributed/linked. Do not create or maintain a second, more technical document.
- **Official Releases Only**: Only official stable releases receive dedicated release entries. Intermediate or continuous builds (e.g., continuous builds on `main`) do NOT get individual changelog entries; their changes are rolled up into the next official release's entry.
- **Shipped Software Only**: Changelogs and blog posts must include only changes that affect the software or user-visible distribution. Exclude repository-only changes with no shipped-software impact, such as GitHub Actions workflows, internal documentation, agent or skill configuration, CI/build maintenance, and other development-process changes.

---

## 1. Changelog Management (`docs/changelogs/`)

### File Structure & Locations
- **Stable Releases**: `docs/changelogs/CHANGELOG-<MAJOR>.<MINOR>.md` (e.g., `CHANGELOG-1.3.md`)
- **Development Builds**: `docs/changelogs/CHANGELOG-<MAJOR>.<MINOR>.md` (e.g., `CHANGELOG-1.4.md`)
- **Index & Overview**: `docs/changelogs/README.md`

Each stable release is appended as a new version section at the top of the relevant `CHANGELOG-<MAJOR>.<MINOR>.md` file, following the established format already used in that file (see, for example, the "Version 1.3.315" entry). Do not restructure or replace prior entries.

### Content Structure

A release's changelog entry is a single bullet list, grouped under short thematic headings, with one bullet per shipped feature or bug fix. Typical headings, used only when relevant content exists (omit empty ones, and add others as needed):

1. 🔒 **Security**
2. ✨ **New Features**
3. 🛠️ **Bug Fixes**
4. 📦 **Installer & Upgrades**

Other short thematic headings may be added when they better describe the changes (e.g. "Performance", "Editor").

#### Bullet Requirements

Every bullet must:
- Be written in short, plain, user-understandable language — describe the practical effect for a user, not the technical implementation. This is the only changelog text; do not write a second, more technical version of the same bullet elsewhere.
- Explicitly credit the contributor whose work introduced the change, using their GitHub handle linked to their GitHub profile, e.g. `[@handle](https://github.com/handle)`. Prefer crediting the original author of the change (e.g. the person who opened the originating PR or reported the issue), not merely whoever merged, cherry-picked, or backported it, unless the original author is unknown.
- Link the relevant pull request and/or issue number(s) that introduced the change, e.g. "fixed in [#1234](https://github.com/greenshot/greenshot/pull/1234)" and/or "(fixes [#1200](https://github.com/greenshot/greenshot/issues/1200))". Verify numbers and authorship from the actual PR/issue/commit; never guess.

Example bullet:
- Fixed a crash that could occur when opening a screenshot with unusual clipboard data, reported by [@someuser](https://github.com/someuser) in [#1200](https://github.com/greenshot/greenshot/issues/1200) and fixed by [@othercontributor](https://github.com/othercontributor) in [#1234](https://github.com/greenshot/greenshot/pull/1234).

#### Security Bullets

Security bullets have extra requirements:
- If a GitHub Security Advisory (GHSA) is public, link it, e.g. `[GHSA-xxxx-xxxx-xxxx](https://github.com/greenshot/greenshot/security/advisories/GHSA-xxxx-xxxx-xxxx)`, and credit the researcher(s) who reported the issue by GitHub handle with a linked profile.
- If the advisory is not yet published (still in draft/triage) or the reporter's credit cannot be confirmed, do **not** fabricate or guess a link or a name. Instead, write the bullet to state that a security fix is included and that advisory details (and/or credit) will be added once the advisory is published, and explicitly flag this limitation back to whoever invoked this skill so they can follow up once the advisory is public.

#### Downloads & Full Changelog

Keep the existing per-release footer used in prior entries:
- **Downloads**: installer and portable ZIP links (`https://github.com/greenshot/greenshot/releases/download/v<version>/Greenshot-INSTALLER-<version>-RELEASE.exe` / `...-PORTABLE-<version>-RELEASE.zip`).
- **Full Changelog**: compare link between release tags (e.g., `https://github.com/greenshot/greenshot/compare/v1.3.312...v1.3.315`).

### Changelog PR Workflow
1. Identify the base branch where the release is cut (e.g., `main`, `release/1.3`).
2. Collect git commit history, PRs, and resolved issues since the previous release. Classify each change by whether it affects shipped software or its user-visible distribution; exclude repository-only and development-process changes. Roll up any intermediate/continuous build changes into this release's entry.
3. For each change, determine the original author/reporter (not just the merger or backporter) and the relevant PR/issue number(s); verify these from the actual PR, issue, or commit rather than guessing.
4. Append the new version section to the appropriate `docs/changelogs/CHANGELOG-X.X.md` and update `docs/changelogs/README.md`.
5. Create a dedicated branch off the original branch (e.g., `git checkout -b docs/changelog-<version>`).
6. Commit and push the changes.
7. **Create a Pull Request targeting the ORIGINAL base branch** (e.g. `main` or `release/1.3`).

---

## 2. Release Blog Posts (`gh-pages` branch)

Blog posts announce new official releases on the Greenshot website ([getgreenshot.org](https://getgreenshot.org)).

### Target Branch & File Location
- **Branch**: `gh-pages` — draft the blog post on a branch created from `gh-pages` and target the PR back at `gh-pages`.
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
3. **Content**: Cover the same changes as the changelog entry, written as full, engaging sentences (not a copy-pasted bullet list) that highlight the most important changes.
4. **Sections**:
   - 🔒 **Security Fixes** (with responsible disclosure credits, subject to the same "don't fabricate unpublished advisory details" rule as the changelog).
   - 🛠️ **Improvements & Fixes**.
   - 🔮 **What's Next** (e.g. roadmap, next major version status).
5. **Contributor Credits**: Add a short **Thanks to the contributors** section before the changelog link. Credit every human contributor whose work is included in the release, including code, documentation, translations, issue reports, reviews, and security disclosures where applicable. Use verified GitHub handles linked to their profiles (`[@username](https://github.com/username)`) and keep the wording warm and casual.
6. **Changelog Link**: Conclude with a link to the corresponding changelog entry in the repository.
7. **No Intermediate Builds**: Do not write a blog post for an intermediate/continuous build; its changes are covered by the next stable release's post.

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
- [ ] No intermediate/continuous builds were documented as standalone changelog entries or blog posts.
- [ ] Only shipped-software or user-visible distribution changes are included.
- [ ] Repository-only changes such as workflows, documentation, agents, skills, and CI maintenance are excluded.
- [ ] The changelog entry is a single bullet list, grouped under short thematic headings — no separate technical document.
- [ ] Every bullet is short and user-understandable, credits the original contributor with a linked GitHub handle, and links the relevant PR and/or issue number(s).
- [ ] Every security bullet either links a public GHSA advisory and credits the reporter, or explicitly says advisory details will follow and flags this back to whoever invoked the skill.
- [ ] Downloads and Full Changelog links are present for the release.
- [ ] `docs/changelogs/README.md` latest release info is updated.
- [ ] Changelog PR targets the original code branch (`main` or `release/X.X`).
- [ ] Blog post is placed on `gh-pages` under `_posts/`, drafted on a branch created from `gh-pages`.
- [ ] Blog post covers the same content as the changelog in full sentences, not a bullet copy.
- [ ] Blog post first paragraph links to `https://getgreenshot.org/downloads/`.
- [ ] Blog post includes a casual contributor thank-you section and links to the changelog at the end.
- [ ] Blog post PR targets the `gh-pages` branch.
