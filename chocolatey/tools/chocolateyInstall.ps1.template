$ErrorActionPreference = 'Stop' # stop on all errors
$toolsDir       = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  unzipLocation = $toolsDir
  fileType      = 'EXE'
  url           = 'DOWNLOAD_URL'
  softwareName  = 'Greenshot*'
  checksum      = 'CHECKSUM_PLACEHOLDER'
  checksumType  = 'CHECKSUM_TYPE'
  silentArgs   = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-'
  validExitCodes= @(0)
}

Install-ChocolateyPackage @packageArgs
