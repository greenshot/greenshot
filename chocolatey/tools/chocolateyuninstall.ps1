$packageName = "Greenshot"
$installerType = "exe"


# Uninstall $packageName if older version is installed
if (Test-Path "$env:ProgramFiles\$packageName") {
    Uninstall-ChocolateyPackage $packageName $installerType "/VERYSILENT /NORESTART" "$env:ProgramFiles\$packageName\unins000.exe"
}

if (Test-Path "${env:ProgramFiles(x86)}\$packageName") {
    Uninstall-ChocolateyPackage $packageName $installerType "/VERYSILENT /NORESTART" "${env:ProgramFiles(x86)}\$packageName\unins000.exe"
}