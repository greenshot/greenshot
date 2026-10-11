<#
.SYNOPSIS
    Writes checksum.SHA256 for the files Greenshot ships.

.DESCRIPTION
    Lists the program files of the main directory and every file in the plugin directories (run
    prune-plugin-files.ps1 first), with paths relative to the output directory and '/' as separator.
    Not listed: debug symbols, language files, configuration that users may change (log4net.xml), the SBOM (it has its own hash
    file) and build-only libraries that are not shipped.
    The self-service checksum page and installer\validate-installation.ps1 verify an installation against it; files
    of a plugin that is not installed (its directory does not exist) are not reported as missing.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDir
)

$OutputDir = (Resolve-Path $OutputDir).Path.TrimEnd('\')
$checksumFile = Join-Path $OutputDir 'checksum.SHA256'

$mainExtensions = @('.dll', '.exe', '.com', '.config', '.ttf', '.xml')
$excluded = @('Microsoft.Build.*.dll', 'Microsoft.IO.Redist.dll', 'Microsoft.NET.StringTools.dll',
              'language*.xml', 'bom.xml', 'log4net.xml', '*.pdb')

function Test-Excluded([string]$name) {
    foreach ($pattern in $excluded) {
        if ($name -like $pattern) { return $true }
    }
    return $false
}

function Get-Sha256Hash([string]$path) {
    $stream = [System.IO.File]::OpenRead($path)
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            $hash = $sha256.ComputeHash($stream)
            return [System.BitConverter]::ToString($hash).Replace('-', '').ToLowerInvariant()
        }
        finally {
            $sha256.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

$files = @()
$files += Get-ChildItem -Path $OutputDir -File |
    Where-Object { $mainExtensions -contains $_.Extension.ToLowerInvariant() -and -not (Test-Excluded $_.Name) }
$pluginsDir = Join-Path $OutputDir 'Plugins'
if (Test-Path $pluginsDir) {
    $files += Get-ChildItem -Path $pluginsDir -Recurse -File | Where-Object { -not (Test-Excluded $_.Name) }
}

$lines = foreach ($file in $files) {
    $relative = $file.FullName.Substring($OutputDir.Length + 1).Replace('\', '/')
    '{0}  {1}' -f (Get-Sha256Hash $file.FullName), $relative
}
$lines = $lines | Sort-Object { $_.Substring(66) }
[System.IO.File]::WriteAllText($checksumFile, (($lines -join "`n") + "`n"))
Write-Host "Wrote $($lines.Count) checksums to $checksumFile"
