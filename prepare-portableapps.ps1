# Creates the PortableApps.com package of Greenshot Light (GreenshotPortable_<version>.paf.exe), on Windows.
# Layout (PortableApps.com Format 3.9, the template is installer\portable):
#   GreenshotPortable.exe              PortableApps.com Launcher, starts App\Greenshot\Greenshot.exe
#   App\AppInfo                        appinfo.ini, installer.ini, launcher configuration and icons
#   App\Greenshot                      Greenshot Light (prepare-portable.ps1 -Light -PortableApps)
#   Data\Settings, Data\Greenshot      greenshot.ini and the log, created by Greenshot (see docs/vNext/portable.md)
# The PortableApps.com Launcher and Installer are downloaded once into ToolsPath (pinned versions, SHA-256 checked) and extracted with 7-Zip.
param(
    [Parameter(Mandatory=$true)]
    [string]$RepositoryRootPath,
    # The Greenshot Light build (bin\Release-Light, see Greenshot.csproj)
    [Parameter(Mandatory=$true)]
    [string]$BuildArtifactsPath,
    # The package is created in OutputPath\GreenshotPortable, the installer next to it: OutputPath\GreenshotPortable_<version>.paf.exe
    [Parameter(Mandatory=$true)]
    [string]$OutputPath,
    [string]$ToolsPath = "$env:TEMP\PortableApps.com-tools"
)

$ErrorActionPreference = "Stop"

# Every release must use the current PortableApps.com Installer, see https://portableapps.com/development
$Tools = @(
    @{ Name = "PortableApps.comLauncher"; Version = "2.2.9"; Folder = "PortableApps.com%20Launcher"; Sha256 = "7bc93e47b885d5e953fa2082a2e65169ae98128026ac4d747f774a23bd01345b" },
    @{ Name = "PortableApps.comInstaller"; Version = "3.9.22"; Folder = "PortableApps.com%20Installer"; Sha256 = "689456f1fb1320c45e82a077afe6b87fb388fd3f75c41d9ee9ce8d45f3966b67" }
)

function Get-Tool($tool) {
    $toolPath = Join-Path $ToolsPath "$($tool.Name)_$($tool.Version)"
    if (Test-Path $toolPath) {
        return $toolPath
    }
    New-Item -ItemType Directory -Path $ToolsPath -Force | Out-Null
    $fileName = "$($tool.Name)_$($tool.Version).paf.exe"
    $download = Join-Path $ToolsPath $fileName
    Invoke-WebRequest -Uri "https://downloads.sourceforge.net/project/portableapps/$($tool.Folder)/$fileName" -OutFile $download -UserAgent "Wget"
    $hash = (Get-FileHash $download -Algorithm SHA256).Hash
    if ($hash -ne $tool.Sha256) {
        Remove-Item $download
        throw "$fileName has the SHA-256 $hash, expected $($tool.Sha256)"
    }
    # The .paf.exe is an NSIS installer, 7-Zip extracts it without running it
    & 7z x -y "-o$toolPath" $download | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "7-Zip couldn't extract $fileName"
    }
    Remove-Item $download
    return $toolPath
}

# Runs a PortableApps.com tool for the package: with the package as argument it works without questions,
# it only waits for the user when it failed, so it is stopped after the timeout
function Invoke-Tool($exe, $package, $result, $log) {
    $process = Start-Process -FilePath $exe -ArgumentList "`"$package`"" -PassThru
    if (-not $process.WaitForExit(600000)) {
        $process.Kill()
    }
    if (-not (Test-Path $result)) {
        if (Test-Path $log) {
            Get-Content $log | Write-Host
        }
        throw "$([System.IO.Path]::GetFileName($exe)) didn't create $result"
    }
}

# Absolute, the PortableApps.com tools and .NET don't know the PowerShell location
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
$OutputPath = (Resolve-Path $OutputPath).Path
$package = Join-Path $OutputPath "GreenshotPortable"
if (Test-Path $package) {
    Remove-Item $package -Recurse -Force
}
New-Item -ItemType Directory -Path $package -Force | Out-Null
Copy-Item "$RepositoryRootPath\installer\portable\*" $package -Recurse -Force

# Greenshot Light in App\Greenshot
& "$RepositoryRootPath\prepare-portable.ps1" -RepositoryRootPath $RepositoryRootPath -BuildArtifactsPath $BuildArtifactsPath -OutputPath "$package\App\Greenshot" -Light -PortableApps

# The version of Greenshot.exe: PackageVersion needs four numbers, DisplayVersion is the release version
$fileVersion = (Get-Item "$package\App\Greenshot\Greenshot.exe").VersionInfo
$packageVersion = "$($fileVersion.FileMajorPart).$($fileVersion.FileMinorPart).$($fileVersion.FileBuildPart).$($fileVersion.FilePrivatePart)"
$displayVersion = "$($fileVersion.FileMajorPart).$($fileVersion.FileMinorPart).$($fileVersion.FileBuildPart)"
$appInfo = "$package\App\AppInfo\appinfo.ini"
# Without a BOM: the PortableApps.com tools read it with GetPrivateProfileString, which would see the BOM as part of [Format]
$content = [System.IO.File]::ReadAllText($appInfo).Replace("@PACKAGEVERSION@", $packageVersion).Replace("@DISPLAYVERSION@", $displayVersion)
[System.IO.File]::WriteAllText($appInfo, $content)

$launcher = Get-Tool $Tools[0]
Invoke-Tool "$launcher\PortableApps.comLauncherGenerator.exe" $package "$package\GreenshotPortable.exe" "$launcher\Data\PortableApps.comLauncherGeneratorLog.txt"

$installer = Get-Tool $Tools[1]
$pafFile = Join-Path $OutputPath "GreenshotPortable_$displayVersion.paf.exe"
Invoke-Tool "$installer\PortableApps.comInstaller.exe" $package $pafFile "$installer\Data\PortableApps.comInstallerLog.txt"

Write-Host "Created $pafFile"
