<# Builds the full Windows installer using the existing Greenshot Inno Setup includes.
   Requires Visual Studio C++ build tools, .NET 10 SDK and the .NET Framework 4.8 targeting pack.
   v143 supports the hosted VS 2022 runner; use v145 for upstream's VS 2026 build environment. #>
param([string]$PlatformToolset = 'v145')
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
Push-Location $repository
try {
    function Invoke-Checked([string]$Program, [string[]]$Arguments) {
        & $Program @Arguments
        if ($LASTEXITCODE -ne 0) { throw "$Program failed with exit code $LASTEXITCODE" }
    }

    # Generate the upstream placeholder credential sources before C# source enumeration.
    # No private upload-service credentials are supplied by this fork.
    foreach ($template in Get-ChildItem src -Recurse -Filter '*.Credentials.template') {
        $generated = [IO.Path]::ChangeExtension($template.FullName, '.cs')
        if (-not (Test-Path -LiteralPath $generated)) {
            Copy-Item -LiteralPath $template.FullName -Destination $generated
        }
    }
    # Use the SDK's MSBuild for managed code: .NET 10 needs MSBuild 18, while the
    # hosted VS 2022 MSBuild is retained only for the native C projects below.
    Invoke-Checked dotnet @('build', 'src/Greenshot.Tests/Greenshot.Tests.csproj', '--configuration', 'Release', '--verbosity', 'minimal')
    foreach ($project in @('greenshot-proxy', 'greenshot-cli')) {
        Invoke-Checked msbuild @("src/greenshot-proxy/$project.vcxproj", '/p:Configuration=Release', '/p:Platform=x64', "/p:PlatformToolset=$PlatformToolset", '/v:minimal')
    }
    Invoke-Checked dotnet @('restore', 'src/Greenshot-Installer/Greenshot-Installer.csproj', '--verbosity', 'quiet')
    $output = Join-Path $repository 'src/Greenshot/bin/Release/net480'
    & ./src/Greenshot-Installer/scripts/prune-plugin-files.ps1 -OutputDir $output
    & ./src/Greenshot-Installer/scripts/check-core-files.ps1 -OutputDir $output -CoreFilesIss ./src/Greenshot-Installer/includes/core-files.iss
    if ($LASTEXITCODE -ne 0) { throw 'Installer file inventory validation failed' }
    & ./src/Greenshot-Installer/scripts/generate-checksums.ps1 -OutputDir $output

    $env:BuildVersionSimple = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $output 'Greenshot.exe')).FileVersion
    $env:BuildVersionEnhanced = "$env:BuildVersionSimple-plus-$(git rev-parse --short HEAD)"
    $compiler = Join-Path $env:USERPROFILE '.nuget/packages/tools.innosetup/7.1.0/tools/ISCC.exe'
    if (-not (Test-Path -LiteralPath $compiler)) { throw "Inno Setup compiler missing: $compiler" }
    Invoke-Checked $compiler @('/Qp', '/FGreenShotPlus-Setup', 'src/Greenshot-Installer/setup.iss')
    $installer = Join-Path $PSScriptRoot 'GreenShotPlus-Setup.exe'
    if (-not (Test-Path -LiteralPath $installer)) { throw 'Installer was not produced' }
    $hash = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  GreenShotPlus-Setup.exe" | Set-Content -LiteralPath "$installer.sha256" -Encoding ascii
    Write-Host "Built $installer"
}
finally { Pop-Location }
