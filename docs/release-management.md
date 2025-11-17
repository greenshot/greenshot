# Release Management

Greenshot releases are delivered in different flavors.

## Versioning

This project uses NerdBank.GitVersioning (NB.GV) as configured in [src/version.json](../src/version.json). 
That file defines the base version and how NB.GV converts Git branches, tags and CI build 
metadata into SemVer version numbers for assemblies and NuGet packages.

Stable releases will always have a version following the pattern `<MAJOR>.<MINOR>.<GIT_HEIGHT>` 
(where GIT_HEIGHT is the number of commits since the version was first set to `<MAJOR>.<MINOR>`,  
for example `1.2.345`.

Unstable releases will always have a commit ref suffix, which clearly identifies the state from
which they have been built, for example `1.2.345-g17c033174ef`.

## Build Tooling

**Unstable releases** (also known as "continuos builds)" are built by 
[our Github release workflow](../.github/workflows/release.yml), whenever a commit has been pushed 
(or merged) to the `main` branch. 

Continous builds are not code-signed due to restrictions of the EV code signing process by Certum.

**Stable releases** (and release candidates) always should be code-signed and therefore cannot be 
built by the Github workflow mentioned above, due to restrictions mentioned above. Hence, we have a 
separate [release script for signed releases](../build-and-deploy.ps1), which is triggered manually
in a dedicated environment for building and signing releases.

## Version History Updates

Regardless of how the release is built, the [version history](https://getgreenshot.org/version-history/) 
and (if needed) the [download page](https://getgreenshot.org/downloads/) are automatically updated. 
This is done by a dedicated GitHub workflow for [updating Github Pages](../.github/workflows/update-gh-pages.yml).
