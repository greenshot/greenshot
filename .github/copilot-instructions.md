# Greenshot Copilot Instructions

## Repository Overview

**Greenshot** is a free, open-source screenshot tool for Windows optimized for productivity. It allows users to capture screenshots, annotate them, and export to various destinations (file, printer, clipboard, email, cloud services).

- **Repository Size**: ~13MB, ~1,100 files
- **Primary Language**: C# (.NET Framework 4.7.2)
- **Project Type**: Windows Desktop Application (WinForms/WPF)
- **Build System**: MSBuild (requires Visual Studio or MSBuild Tools for Windows)
- **Versioning**: Nerdbank.GitVersioning (version base: 1.4.x)

## Build Requirements & Environment

### Prerequisites
- **Operating System**: Windows (Linux/Mac not supported for building)
- **Build Tools**: MSBuild (from Visual Studio 2019+ or MSBuild Tools for Windows)
- **.NET SDK**: .NET 7.x SDK (for NuGet restore, see release.yml line 38-39)
- **Target Framework**: .NET Framework 4.7.2
- **Git Clone**: MUST use full clone with history (NOT shallow clone) due to Nerdbank.GitVersioning requirements

### Critical Build Notes
- **DO NOT use `dotnet build`** - It will fail with CodeTaskFactory errors. The project uses MSBuild-specific tasks that require full MSBuild (not .NET SDK's MSBuild).
- **ALWAYS use `msbuild` directly** from Visual Studio installation or MSBuild Tools.
- Shallow git clones will cause build failures with "Shallow clone lacks the objects" error. Use `git fetch --unshallow` if needed.

## Build & Validation Commands

### Restore NuGet Packages
```powershell
msbuild src/Greenshot.sln /p:Configuration=Release /restore /t:PrepareForBuild
```
**Time**: ~10-30 seconds  
**Note**: Run this BEFORE building. Environment variables for API credentials may be needed (see Secrets section).

### Build Solution
```powershell
msbuild src/Greenshot.sln /p:Configuration=Release /t:Rebuild /v:normal
```
**Time**: ~1-3 minutes  
**Output**: `src/Greenshot/bin/Release/net472/` (main executable and plugins)

### Build for Debug
```powershell
msbuild src/Greenshot.sln /p:Configuration=Debug /t:Rebuild /v:normal
```

### Clean Build
```powershell
msbuild src/Greenshot.sln /t:Clean /p:Configuration=Release
```

### No Automated Tests
There are NO automated test projects in this repository. Do not attempt to run tests.

## Project Structure & Architecture

### Root Directory Files
- **src/** - All source code (solution and projects)
- **installer/** - Inno Setup installer configuration
- **docs/** - Documentation (release-management.md)
- **build-and-deploy.ps1** - Manual release script (for signed releases)
- **prepare-portable.ps1** - Creates portable ZIP package
- **.github/workflows/** - CI/CD workflows (release.yml is main build workflow)

### Source Directory (`src/`)
```
src/
├── Greenshot.sln              # Main solution file
├── Directory.Build.props      # Shared MSBuild properties
├── Directory.Build.targets    # Shared MSBuild targets (token replacement)
├── .editorconfig              # Code style configuration
├── version.json               # Nerdbank.GitVersioning config
├── Greenshot/                 # Main application project
├── Greenshot.Base/            # Core/shared library
├── Greenshot.Editor/          # Image editor component
└── Greenshot.Plugin.*/        # Plugin projects (Box, Dropbox, Imgur, etc.)
```

### Main Application
- **Entry Point**: `src/Greenshot/GreenshotMain.cs`
- **Main Form**: `src/Greenshot/Forms/MainForm.cs`
- **Configuration**: `src/Greenshot/Configuration/`
- **Destination Handlers**: `src/Greenshot/Destinations/` (clipboard, email, file, etc.)
- **Capture Helpers**: `src/Greenshot/Helpers/CaptureHelper.cs`

### Plugins Architecture
Each plugin follows a consistent structure:
- Located in `src/Greenshot.Plugin.{Name}/`
- Has language files in `Languages/language_{plugin}*.xml`
- Build output goes to `src/Greenshot/bin/{Configuration}/net472/Plugins/{PluginName}/`
- Post-build events (in Directory.Build.props) copy plugins to main app output

### Key Configuration Files
- **src/.editorconfig** - Code style (Allman braces, 4 spaces, `_camelCase` fields)
- **src/Directory.Build.props** - Build properties, versioning, token replacement config
- **src/Directory.Build.targets** - Token replacement for API credentials
- **src/version.json** - Version base (1.4), release branch config

## Continuous Integration

### GitHub Actions Workflow (`.github/workflows/release.yml`)
**Triggers**: Push to `main` or `release/1.*` branches (excluding docs/config changes)

**Build Process**:
1. **Setup**: Windows runner, MSBuild, .NET 7.x SDK
2. **Restore**: `msbuild src/Greenshot.sln /p:Configuration=Release /restore /t:PrepareForBuild`
3. **Build**: `msbuild src/Greenshot.sln /p:Configuration=Release /t:Rebuild /v:normal`
4. **Package Installer**: Copies from `installer/Greenshot-INSTALLER-*.exe`
5. **Package Portable**: Runs `prepare-portable.ps1`, creates ZIP
6. **Deploy**: Creates GitHub release with installer and portable ZIP

**Important**: CI requires GitHub secrets for OAuth API credentials (Box, Dropbox, Flickr, Imgur, Photobucket, Picasa).

### Build Artifacts
- **Installer**: `installer/Greenshot-INSTALLER-{version}-RELEASE.exe`
- **Portable**: `Greenshot-PORTABLE-{version}-UNSTABLE-UNSIGNED.zip`

## Coding Conventions

**Follow Microsoft/Visual Studio defaults with these specific rules**:

1. **Braces**: Allman style (opening brace on new line)
2. **Indentation**: 4 spaces, NO tabs
3. **Fields**: `_camelCase` for instance, `s_camelCase` for static, `t_camelCase` for thread-static
4. **Visibility**: Always explicit (e.g., `private string _foo`)
5. **Namespaces**: At top of file, OUTSIDE namespace declarations, sorted alphabetically
6. **Keywords**: Use `int`, `string` instead of `Int32`, `String`
7. **var**: Only when type is obvious
8. **nameof()**: Prefer over string literals
9. **this.**: Avoid unless necessary
10. **Empty lines**: Avoid more than one consecutive blank line

See `CONTRIBUTING.md` for complete style guide.

## Common Issues & Workarounds

### Issue 1: Build Fails with "CodeTaskFactory not supported"
**Symptoms**: Error MSB4801 when using `dotnet build`  
**Cause**: .NET SDK's MSBuild doesn't support CodeTaskFactory used in Directory.Build.targets  
**Solution**: Use `msbuild` from Visual Studio or MSBuild Tools for Windows, NOT `dotnet build`

### Issue 2: "Shallow clone lacks the objects required"
**Symptoms**: Nerdbank.GitVersioning error during build  
**Cause**: Git clone is shallow (doesn't have full history)  
**Solution**: 
```bash
git fetch --unshallow
```
Or ensure initial clone uses: `git clone --depth=0` or full clone without `--depth`

### Issue 3: Missing API Credentials
**Symptoms**: Build succeeds but plugins may have empty OAuth credentials  
**Cause**: Environment variables for API keys not set  
**Context**: Credentials are templated via Directory.Build.targets for plugins (Box, Dropbox, Flickr, Imgur, Photobucket, Picasa)  
**Solution for Local Dev**: Credentials are optional for building. Set environment variables if needed:
```powershell
$env:Box13_ClientId = "your_id"
$env:Box13_ClientSecret = "your_secret"
# (repeat for other services)
```

### Issue 4: Installer Not Built
**Symptoms**: No .exe in `installer/` after build  
**Cause**: Installer creation is part of Greenshot project's post-build using Inno Setup (Tools.InnoSetup NuGet package)  
**Solution**: Build succeeds without installer; use CI workflow or manual build-and-deploy.ps1 for full release

## Making Code Changes

### Typical Workflow
1. **Restore**: `msbuild src/Greenshot.sln /p:Configuration=Debug /restore /t:PrepareForBuild`
2. **Build**: `msbuild src/Greenshot.sln /p:Configuration=Debug /t:Build /v:minimal`
3. **Make Changes**: Edit C# files following conventions
4. **Rebuild**: `msbuild src/Greenshot.sln /p:Configuration=Debug /t:Rebuild /v:minimal` (incremental)
5. **Test Manually**: Run `src/Greenshot/bin/Debug/net472/Greenshot.exe`

### Adding New Features
- Core functionality: `src/Greenshot.Base/` or `src/Greenshot/`
- Editor features: `src/Greenshot.Editor/`
- New plugins: Create new `Greenshot.Plugin.{Name}` project following existing plugin structure
- UI changes: Modify Forms in `src/Greenshot/Forms/` or `src/Greenshot.Editor/Forms/`

### Modifying Plugins
Each plugin in `src/Greenshot.Plugin.*/` is self-contained. Changes are automatically copied to main output via post-build events.

## Validation Checklist

Before submitting changes:
- [ ] Build succeeds: `msbuild src/Greenshot.sln /p:Configuration=Release /t:Rebuild`
- [ ] Code follows style conventions (see .editorconfig and CONTRIBUTING.md)
- [ ] No new TODO/HACK/FIXME without justification
- [ ] Manual testing of affected features (no automated tests available)
- [ ] Consider impact on CI workflow (`.github/workflows/release.yml`)

## Additional Resources

- **README.md**: Project overview, feature list
- **CONTRIBUTING.md**: Complete coding style guide
- **docs/release-management.md**: Versioning, release process
- **.github/workflows/release.yml**: Full CI build process
- **src/version.json**: Version configuration

## Trust These Instructions

These instructions have been validated against the actual repository structure and build process. When information here conflicts with generic .NET knowledge, trust these specific instructions for Greenshot. Only search further if encountering undocumented errors.
