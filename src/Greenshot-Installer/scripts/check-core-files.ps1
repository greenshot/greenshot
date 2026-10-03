<#
.SYNOPSIS
    Checks that includes\core-files.iss installs exactly the program files of the build output.

.DESCRIPTION
    core-files.iss names every library and program it installs, without wildcards, so a library that a package brings
    along by accident doesn't ship. This check fails the build when a .dll, .exe or .com in the main directory of the build
    output is not in core-files.iss (it would be missing in the installation), or when core-files.iss names one that
    isn't there. Not checked: build-only libraries that are not shipped (as in generate-checksums.ps1) and
    greenshot-mcp.exe, which is a separate download.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDir,
    [Parameter(Mandatory = $true)]
    [string]$CoreFilesIss
)

$ErrorActionPreference = 'Stop'

$notShipped = @('Microsoft.Build.*.dll', 'Microsoft.IO.Redist.dll', 'Microsoft.NET.StringTools.dll', 'greenshot-mcp.exe')

$listed = @()
foreach ($line in Get-Content $CoreFilesIss) {
    if ($line -match '^\s*Source:\s*\{#ReleaseDir\}\\([^;]+);') {
        $name = $Matches[1].Trim()
        if ($name.Contains('*') -or $name.Contains('?')) {
            throw "core-files.iss must name each file, not '$name'"
        }
        $listed += $name
    }
}

$programFiles = Get-ChildItem -Path $OutputDir -File |
    Where-Object { @('.dll', '.exe', '.com') -contains $_.Extension.ToLowerInvariant() } |
    Where-Object { $name = $_.Name; -not ($notShipped | Where-Object { $name -like $_ }) } |
    ForEach-Object { $_.Name }

$problems = @()
foreach ($file in $programFiles) {
    if (-not ($listed | Where-Object { $_ -ieq $file })) {
        $problems += "$file is in $OutputDir but not in core-files.iss: add it, or keep it out of the build output"
    }
}
foreach ($file in $listed) {
    if (@('.dll', '.exe', '.com') -contains [System.IO.Path]::GetExtension($file).ToLowerInvariant() -and
        -not (Test-Path (Join-Path $OutputDir $file))) {
        $problems += "$file is in core-files.iss but not in $OutputDir"
    }
}
if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Host "error: $_" }
    exit 1
}
Write-Host "core-files.iss matches the $($programFiles.Count) program files in $OutputDir"
