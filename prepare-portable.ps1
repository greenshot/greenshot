param(
    [Parameter(Mandatory=$true)]
    [string]$RepositoryRootPath,
    [Parameter(Mandatory=$true)]
    [string]$BuildArtifactsPath,
    [Parameter(Mandatory=$true)]
    [string]$OutputPath,
    # The light version has no plugins, like the light installer
    [switch]$Light
)

# Create portable directory
New-Item -ItemType Directory -Path "$OutputPath" -Force | Out-Null

# Copy checksum.SHA256
Copy-Item "$BuildArtifactsPath\checksum.SHA256" "$OutputPath" -Force

# Copy SBOM files
$SbomFiles = @("bom.json", "bom.xml", "manifest.spdx.json", "manifest.spdx.json.sha256")
foreach ($file in $SbomFiles) {
    $filePath = Join-Path $BuildArtifactsPath $file
    if (Test-Path $filePath) {
        Copy-Item $filePath "$OutputPath" -Force
    }
}
# Copy greenshot.exe
Copy-Item "$BuildArtifactsPath\Greenshot.exe" "$OutputPath" -Force
# Copy greenshot.exe.config
Copy-Item "$BuildArtifactsPath\Greenshot.exe.config" "$OutputPath" -Force
# Copy the command line (greenshot.com) and greenshot-proxy.exe, which forwards files and URLs to Greenshot
Copy-Item "$BuildArtifactsPath\greenshot.com" "$OutputPath" -Force
Copy-Item "$BuildArtifactsPath\greenshot-proxy.exe" "$OutputPath" -Force

# Copy all dlls
Copy-Item "$BuildArtifactsPath\*.dll" "$OutputPath" -Force

# Copy emoji resources
Copy-Item "$BuildArtifactsPath\emojis.xml" "$OutputPath" -Force
Copy-Item "$BuildArtifactsPath\Twemoji.Mozilla.ttf" "$OutputPath" -Force

# Copy help files
New-Item -ItemType Directory -Path "$OutputPath\Help" -Force | Out-Null
Copy-Item "$RepositoryRootPath\src\Greenshot\Languages\*.html" "$OutputPath\Help" -Force

# Copy languages files
New-Item -ItemType Directory -Path "$OutputPath\Languages" -Force | Out-Null
Copy-Item "$RepositoryRootPath\src\Greenshot\Languages\*.xml" "$OutputPath\Languages" -Force

# Create Dummy-INI
";dummy config, used to make greenshot store the configuration in this directory" | Set-Content "$OutputPath\greenshot.ini" -Encoding UTF8

# Create Dummy-defaults-INI
";In this file you should add your default settings" | Set-Content "$OutputPath\greenshot-defaults.ini" -Encoding UTF8

# Create Dummy-fixed-INI
";In this file you should add your fixed settings" | Set-Content "$OutputPath\greenshot-fixed.ini" -Encoding UTF8

# Copy license file
Copy-Item "$RepositoryRootPath\src\Greenshot-Installer\additional_files\license.txt" "$OutputPath" -Force

# Copy readme file
Copy-Item "$RepositoryRootPath\src\Greenshot-Installer\additional_files\readme.txt" "$OutputPath" -Force

# Copy and rename log config file
Copy-Item "$RepositoryRootPath\src\Greenshot\log4net-zip.xml" "$OutputPath\log4net.xml" -Force

# Copy the plugins: the build output has the installed layout (the plugin directories only contain what the plugin
# needs in addition to the main directory), so every plugin directory is copied as it is; the files are in checksum.SHA256
$pluginDirs = if ($Light) { @() } else { Get-ChildItem -Path "$BuildArtifactsPath\Plugins" -Directory }
foreach ($pluginDir in $pluginDirs) {
    $pluginName = $pluginDir.Name
    New-Item -ItemType Directory -Path "$OutputPath\Plugins\$pluginName" -Force | Out-Null
    Copy-Item "$($pluginDir.FullName)\*.dll" "$OutputPath\Plugins\$pluginName" -Force

    $pluginLanguages = "$RepositoryRootPath\src\$pluginName\Languages"
    if (Test-Path $pluginLanguages) {
        New-Item -ItemType Directory -Path "$OutputPath\Languages\$pluginName" -Force | Out-Null
        Copy-Item "$pluginLanguages\language_*.xml" "$OutputPath\Languages\$pluginName" -Force
    }
}
