# Greenshot Copilot Instructions

## Repository Overview

**Greenshot** is a free, open-source screenshot tool for Windows optimized for productivity. It allows users to capture screenshots, annotate them, and export to various destinations (file, printer, clipboard, email, cloud services).

- **Repository Size**: ~13MB, ~1,100 files
- **Primary Language**: C# (.NET Framework 4.8, target `net480`); `Greenshot.Mcp` targets .NET 10
- **Project Type**: Windows Desktop Application (WinForms/WPF)
- **Build System**: MSBuild (requires Visual Studio or MSBuild Tools for Windows)
- **Versioning**: Nerdbank.GitVersioning (version base: 1.4.x)

## Specialised Agents & Skills

- **translation-manager**: Always consult them about UI messages, esp. after addition/change/removal of UI messages (see `.github/agents/translation-manager.md` and `docs/translation/`).
- **release-documentation** (Skill with Visual Studio wrapper): The user may explicitly invoke this workflow when release notes, changelogs, or release blog posts need to be generated or updated. It is not automatically invoked by models (see `.github/skills/release-documentation/SKILL.md` and `.github/agents/release-documentation-specialist.agent.md`).
  - Changelogs are updated in `docs/changelogs/` and submitted as a PR against the original base branch (e.g. `main` or `release/1.3`).
  - Release blog posts are created under `_posts/` and submitted as a PR against the `gh-pages` branch.
  - All documentation is written in English.

## Build Requirements & Environment

### Prerequisites
- **Operating System**: Windows (Linux/Mac not supported for building)
- **Build Tools**: Visual Studio 2026 with the Desktop development with C++ workload, MSVC v145 x64/x86 build tools, Windows 10 SDK, and the .NET Framework 4.8 Developer Pack
- **.NET SDK**: .NET SDK 10.0.100 or newer (the MCP server targets .NET 10)
- **Target Frameworks**: Most projects target .NET Framework 4.8 (`net480`); `Greenshot.Mcp` targets `net10.0-windows`
- **Git Clone**: MUST use full clone with history (NOT shallow clone) due to Nerdbank.GitVersioning requirements

### Critical Build Notes
- Build the solution with Visual Studio's full `MSBuild.exe`, not `dotnet build` or `dotnet msbuild`. The solution contains C++ projects and MSBuild-specific tasks.
- Do not assume `msbuild` is on `PATH`; run from a Visual Studio Developer PowerShell or use the full path to `MSBuild\Current\Bin\MSBuild.exe`.
- Nerdbank.GitVersioning needs Git history. Avoid shallow clones; if the checkout is shallow, use `git fetch --unshallow`.

## Build & Validation Commands

### Restore NuGet Packages
```powershell
msbuild src/Greenshot.sln /p:Configuration=Release /restore /t:PrepareForBuild
```
**Note**: This is the restore/preparation step used by the release workflow. OAuth credentials are optional for local builds.

### Build Solution
```powershell
msbuild src/Greenshot.sln /p:Configuration=Release /t:Rebuild /v:normal
```
**Output**: `src/Greenshot/bin/Release/net480/` (main executable and plugins)

**Note**: Release also builds Greenshot Light, installers, checksums, SBOMs, portable ZIPs, and the MCP server. It requires the C++ build tools and .NET 10 SDK.

### Build for Debug
```powershell
msbuild src/Greenshot.sln /p:Configuration=Debug /t:Rebuild /v:normal
```

### Clean Build
```powershell
msbuild src/Greenshot.sln /t:Clean /p:Configuration=Release
```

### Run the Tests
The repository has an xUnit v3 test project at `src/Greenshot.Tests/Greenshot.Tests.csproj`. First build with Visual Studio MSBuild (the solution includes MSBuild-specific tasks and C++ projects), then run the test project without rebuilding. From the repository root, use:
```powershell
msbuild src/Greenshot.sln /m /t:Build /p:Configuration=Debug /p:Restore=true /v:minimal
dotnet test src/Greenshot.Tests/Greenshot.Tests.csproj --configuration Debug --no-build
```
For Release, change the MSBuild property to `/p:Configuration=Release` and use `--configuration Release` with `dotnet test`. To run a subset, add a VSTest filter, for example `--filter "FullyQualifiedName~Ipc"`. Tests that require clipboard or desktop interaction need an active, unlocked Windows session; interactive-desktop tests are skipped when the session is unavailable.

## Project Structure & Architecture

### Root Directory Files
- **src/** - All source code (solution and projects)
- **installer/** - Generated installers and portable ZIP artifacts
- **src/Greenshot-Installer/** - Inno Setup scripts and release packaging targets
- **docs/** - Project documentation, including `release-management.md`
- **build-and-deploy.ps1** - Manual release script (for signed releases)
- **prepare-portable.ps1** - Prepares a portable folder from build artifacts
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
├── Greenshot.Tests/           # xUnit v3 test project
├── Greenshot.Mcp/             # .NET 10 MCP server
├── greenshot-proxy/           # Native C++ proxy and CLI projects
├── Greenshot-Installer/       # Release packaging and installer targets
└── Greenshot.Plugin.*/        # Plugin projects
```

### Main Application
- **Entry Point**: `src/Greenshot/GreenshotMain.cs`
- **Shell (startup, services, tray icon and menu)**: `src/Greenshot/Shell/`
- **Configuration**: `src/Greenshot/Configuration/`
- **Destination Handlers**: `src/Greenshot/Destinations/` (clipboard, email, file, etc.)
- **Capture Helpers**: `src/Greenshot/Helpers/CaptureHelper.cs`

### Plugins Architecture
Each plugin follows a consistent structure:
- Located in `src/Greenshot.Plugin.{Name}/`
- Has INI language files in `Languages/greenshot.{plugin}.{locale}.ini`
- Build output goes to `src/Greenshot/bin/{Configuration}/net480/Plugins/{PluginName}/`
- A post-build target in `src/Directory.Build.props` copies plugins to the main app output

### Key Configuration Files
- **src/.editorconfig** - Code style (Allman braces, 4 spaces, `_camelCase` fields)
- **src/Directory.Build.props** - Build properties, versioning, token replacement config
- **src/Directory.Build.targets** - Token replacement for API credentials
- **src/version.json** - Version base (1.4), release branch config

## Continuous Integration

### GitHub Actions Workflow (`.github/workflows/release.yml`)
**Triggers**: Push to `main` or `release/1.*` branches (excluding docs/config changes)

**Build Process**:
1. **Setup**: Windows runner, MSBuild, .NET 10 SDK, and a full-history checkout
2. **Restore**: `msbuild src/Greenshot.sln /p:Configuration=Release /restore /t:PrepareForBuild`
3. **Build**: `msbuild src/Greenshot.sln /p:Configuration=Release /t:Rebuild /v:normal`
4. **Package Installer**: Copies from `installer/Greenshot-INSTALLER-*.exe`
5. **Package Portable**: Runs `prepare-portable.ps1`, creates ZIP
6. **Deploy**: Creates GitHub release with installer and portable ZIP

**OAuth credentials**: The build templates currently consume Box, Dropbox, and Imgur credentials. They are optional for local builds; without them, those integrations use placeholder credentials.

### Build Artifacts
- **Installers**: `installer/Greenshot-INSTALLER-*.exe` and `installer/Greenshot-Light-INSTALLER-*.exe` (unsigned/unstable names for local builds; signing depends on the configured certificate)
- **Portable**: `installer/Greenshot-PORTABLE-*.zip` and `installer/Greenshot-Light-PORTABLE-*.zip`

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

### Issue 1: Solution Build Fails Under .NET SDK MSBuild
**Symptoms**: Build errors from MSBuild tasks or native C++ projects when using `dotnet build`  
**Cause**: The solution depends on full Visual Studio MSBuild and the C++ toolchain  
**Solution**: Build the solution with Visual Studio's `MSBuild.exe`, not `dotnet build` or `dotnet msbuild`.

### Issue 2: "Shallow clone lacks the objects required"
**Symptoms**: Nerdbank.GitVersioning error during build  
**Cause**: Git clone is shallow (doesn't have full history)  
**Solution**:
```bash
git fetch --unshallow
```
Or ensure initial clone uses: `git clone --depth=0` or full clone without `--depth`

### Issue 3: Missing API Credentials
**Symptoms**: Box, Dropbox, or Imgur authorization is not configured in a local build  
**Cause**: Environment variables for API keys not set  
**Context**: Build templates replace credentials for the Box, Dropbox, and Imgur plugins  
**Solution for Local Dev**: Credentials are optional for building. Set the relevant environment variables if you need to test those integrations:
```powershell
$env:Box13_ClientId = "your_id"
$env:Box13_ClientSecret = "your_secret"
$env:DropBox13_ClientId = "your_id"
$env:DropBox13_ClientSecret = "your_secret"
$env:Imgur13_ClientId = "your_id"
$env:Imgur13_ClientSecret = "your_secret"
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
5. **Test Manually**: Run `src/Greenshot/bin/Debug/net480/Greenshot.exe`

### Adding New Features
- Core functionality: `src/Greenshot.Base/` or `src/Greenshot/`
- Editor features: `src/Greenshot.Editor/`
- New plugins: Create new `Greenshot.Plugin.{Name}` project following existing plugin structure
- Where code goes and how it is named (Views/ViewModels, `Forms` only for WinForms, Recipes, optional parts, frozen .greenshot types): see `docs/code-structure.md`
- UI changes: WPF in the `Views`/`ViewModels` folders of the feature, WinForms in `src/Greenshot/Forms/` or `src/Greenshot.Editor/Forms/`
- New plugin language packs use INI format: `Languages/greenshot.{plugin}.{locale}.ini`

### Modifying Plugins
Each plugin in `src/Greenshot.Plugin.*/` is self-contained. Changes are automatically copied to main output via post-build events.

## Translation Tasks

**Whenever UI messages are added, changed or removed, all translations should be updated accordingly. For ALL translation-related tasks, delegate to the translation-manager custom agent.**

The translation-manager agent is a specialized expert with comprehensive knowledge of:
- Translation file structure and format (UTF-8 INI language packs)
- All 40 supported languages and their language files
- Translation glossary, workflow checklists, and validation tools
- Best practices for high-quality translations

**Translation tasks include:**
- Adding, updating, or removing translation strings
- Translating content to different languages
- Adding support for new languages
- Validating translation file completeness and correctness
- Reviewing translation quality

**Documentation**: See `docs/translation/README.md` for comprehensive translation documentation.

**Do not** attempt translation tasks yourself. Always use the translation-manager agent for any work involving language files or translation documentation.

## Validation Checklist

Before submitting changes:
- [ ] Build succeeds with Visual Studio MSBuild: `msbuild src/Greenshot.sln /m /p:Configuration=Debug /t:Build`
- [ ] Tests pass: `dotnet test src/Greenshot.Tests/Greenshot.Tests.csproj --configuration Debug --no-build` after building the solution
- [ ] Code follows style conventions (see .editorconfig and CONTRIBUTING.md)
- [ ] No new TODO/HACK/FIXME without justification
- [ ] Manually test affected UI or capture features as appropriate
- [ ] Consider impact on CI workflow (`.github/workflows/release.yml`)

## Additional Resources

- **README.md**: Project overview, feature list
- **CONTRIBUTING.md**: Complete coding style guide
- **docs/release-management.md**: Versioning, release process
- **.github/workflows/release.yml**: Full CI build process
- **src/version.json**: Version configuration

## Trust These Instructions

These instructions have been validated against the actual repository structure and build process. When information here conflicts with generic .NET knowledge, trust these specific instructions for Greenshot. Only search further if encountering undocumented errors.
