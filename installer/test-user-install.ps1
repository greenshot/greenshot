<# Run only on a disposable Windows build runner: exercises the real per-user installer and uninstaller. #>
param([Parameter(Mandatory = $true)][string]$InstallerPath)
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Use a disposable GitHub Actions runner for installation tests.' }
$installer = (Resolve-Path -LiteralPath $InstallerPath).Path
$installDir = Join-Path $env:LOCALAPPDATA 'Programs/Greenshot'
$userKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Greenshot_is1'
$machineKey = 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Greenshot_is1'
if ((Test-Path $userKey) -or (Test-Path $machineKey) -or (Test-Path -LiteralPath $installDir)) {
    throw 'Runner already has Greenshot; refusing to change an existing installation.'
}
$process = Start-Process -FilePath $installer -ArgumentList '/CURRENTUSER /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /TASKS="" /COMPONENTS="greenshot"' -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Installer failed: $($process.ExitCode)" }
try {
    if (-not (Test-Path $userKey)) { throw 'Per-user uninstall registration is missing' }
    if (Test-Path $machineKey) { throw 'Installer unexpectedly registered a machine-wide installation' }
    $registration = Get-ItemProperty $userKey
    $actualDir = $registration.InstallLocation.TrimEnd('\')
    if ($actualDir -ine $installDir) { throw "Unexpected user installation directory: $actualDir" }
    foreach ($file in @('Greenshot.exe', 'Greenshot.Base.dll', 'Greenshot.Editor.dll', 'greenshot-proxy.exe', 'greenshot-cli.exe')) {
        if (-not (Test-Path -LiteralPath (Join-Path $actualDir $file))) { throw "Missing installed file: $file" }
    }
    Push-Location $actualDir
    try { & (Join-Path $PSScriptRoot 'validate-installation.ps1') -WarningAction Stop }
    finally { Pop-Location }
    Write-Host "Verified per-user installation in $actualDir with no HKLM uninstall entry."
}
finally {
    $uninstaller = Join-Path $installDir 'unins000.exe'
    if (Test-Path -LiteralPath $uninstaller) {
        $process = Start-Process -FilePath $uninstaller -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART' -WindowStyle Hidden -Wait -PassThru
        if ($process.ExitCode -ne 0) { throw "Uninstaller failed: $($process.ExitCode)" }
    }
}
if (Test-Path $userKey) { throw 'Uninstaller left the uninstall registration behind' }
if (Test-Path -LiteralPath (Join-Path $installDir 'Greenshot.exe')) { throw 'Uninstaller left the application behind' }
Write-Host 'Verified per-user uninstall.'
