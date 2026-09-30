<#
.SYNOPSIS
    Creates the portable ZIP files (full and light) next to the installers of a local Release build.

.DESCRIPTION
    Uses prepare-portable.ps1, like the release workflow. The ZIP files are named after the installer that was just
    built: Greenshot-INSTALLER-<version>.exe gives Greenshot-PORTABLE-<version>.zip and Greenshot-Light-PORTABLE-<version>.zip.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$RepositoryRootPath,
    [Parameter(Mandatory = $true)]
    [string]$BuildArtifactsPath,
    [Parameter(Mandatory = $true)]
    [string]$InstallerDir
)

$ErrorActionPreference = 'Stop'

$installer = Get-ChildItem -Path $InstallerDir -Filter 'Greenshot-INSTALLER-*.exe' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $installer) {
    throw "No Greenshot-INSTALLER-*.exe found in $InstallerDir"
}
$version = $installer.BaseName.Substring('Greenshot-INSTALLER-'.Length)

$prepareScript = Join-Path $RepositoryRootPath 'prepare-portable.ps1'
foreach ($edition in @(@{ Prefix = 'Greenshot'; Light = $false }, @{ Prefix = 'Greenshot-Light'; Light = $true })) {
    $staging = Join-Path ([System.IO.Path]::GetTempPath()) ("greenshot-portable-" + [guid]::NewGuid().ToString('N'))
    try {
        & $prepareScript -RepositoryRootPath $RepositoryRootPath -BuildArtifactsPath $BuildArtifactsPath -OutputPath $staging -Light:$edition.Light
        $zip = Join-Path $InstallerDir ("{0}-PORTABLE-{1}.zip" -f $edition.Prefix, $version)
        Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zip -Force
        Write-Host "Created $zip"
    }
    finally {
        Remove-Item -Path $staging -Recurse -Force -ErrorAction SilentlyContinue
    }
}
