<#
.SYNOPSIS
    Removes the plugin directories' copies of files that Greenshot itself ships.

.DESCRIPTION
    Every plugin build copies all its dlls into <output>\Plugins\<plugin>, including Greenshot.Base.dll and the
    libraries Greenshot already has in the main directory. Those copies are never shipped: the plugins use the files
    in the main directory. Removing them here makes the output directory the exact layout of an installation, so the
    installer, the portable version, the SBOM and checksum.SHA256 all describe the same files.
    A copy that differs from the main directory's file is kept (and reported), as the plugin may need that version.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDir
)

$pluginsDir = Join-Path $OutputDir 'Plugins'
if (-not (Test-Path $pluginsDir)) {
    return
}

$removed = 0
foreach ($file in Get-ChildItem -Path $pluginsDir -Recurse -File) {
    $mainFile = Join-Path $OutputDir $file.Name
    if (-not (Test-Path $mainFile -PathType Leaf)) {
        continue
    }
    if ((Get-FileHash $file.FullName -Algorithm SHA256).Hash -eq (Get-FileHash $mainFile -Algorithm SHA256).Hash) {
        Remove-Item $file.FullName -Force
        $removed++
    }
    else {
        Write-Warning "Kept $($file.FullName): it differs from $mainFile"
    }
}
Write-Host "Removed $removed plugin copies of files in $OutputDir"
