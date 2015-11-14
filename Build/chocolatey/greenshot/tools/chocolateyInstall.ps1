if (Get-Process 'Greenshot' -ea SilentlyContinue) {Stop-Process -processname Greenshot}
Install-ChocolateyPackage 'greenshot' 'EXE' '/VERYSILENT' 'https://github.com/greenshot/greenshot/releases/download/Greenshot-RELEASE-1.2.8.12/Greenshot-INSTALLER-1.2.8.12-RELEASE.exe'
