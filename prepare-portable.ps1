param(
    [Parameter(Mandatory=$true)]
    [string]$RepositoryRootPath,
    [Parameter(Mandatory=$true)]
    [string]$BuildArtifactsPath,
    [Parameter(Mandatory=$true)]
    [string]$OutputPath
)

# Create portable directory
New-Item -ItemType Directory -Path "$OutputPath" -Force | Out-Null

# Copy greenshot.exe
Copy-Item "$BuildArtifactsPath\Greenshot.exe" "$OutputPath" -Force

# Copy greenshot.exe.config
Copy-Item "$BuildArtifactsPath\Greenshot.exe.config" "$OutputPath" -Force

# Copy all dlls
Copy-Item "$BuildArtifactsPath\*.dll" "$OutputPath" -Force

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
Copy-Item "$RepositoryRootPath\installer\additional_files\license.txt" "$OutputPath" -Force

# Copy readme file
Copy-Item "$RepositoryRootPath\installer\additional_files\readme.txt" "$OutputPath" -Force

# Copy and rename log config file
Copy-Item "$RepositoryRootPath\src\Greenshot\log4net-zip.xml" "$OutputPath\log4net.xml" -Force

# Copy Box Plugin
New-Item -ItemType Directory -Path "$OutputPath\Languages\Greenshot.Plugin.Box" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputPath\Plugins\Greenshot.Plugin.Box" -Force | Out-Null
Copy-Item "$RepositoryRootPath\src\Greenshot.Plugin.Box\Languages\language_box*.xml" "$OutputPath\Languages\Greenshot.Plugin.Box" -Force
Copy-Item "$BuildArtifactsPath\Plugins\Greenshot.Plugin.Box\Greenshot.Plugin.Box.dll" "$OutputPath\Plugins\Greenshot.Plugin.Box" -Force

# Copy Confluence Plugin
New-Item -ItemType Directory -Path "$OutputPath\Languages\Greenshot.Plugin.Confluence" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputPath\Plugins\Greenshot.Plugin.Confluence" -Force | Out-Null
Copy-Item "$RepositoryRootPath\src\Greenshot.Plugin.Confluence\Languages\language_confluence*.xml" "$OutputPath\Languages\Greenshot.Plugin.Confluence" -Force
Copy-Item "$BuildArtifactsPath\Plugins\Greenshot.Plugin.Confluence\Greenshot.Plugin.Confluence.dll" "$OutputPath\Plugins\Greenshot.Plugin.Confluence" -Force

# Copy Dropbox Plugin
New-Item -ItemType Directory -Path "$OutputPath\Languages\Greenshot.Plugin.Dropbox" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputPath\Plugins\Greenshot.Plugin.Dropbox" -Force | Out-Null
Copy-Item "$RepositoryRootPath\src\Greenshot.Plugin.Dropbox\Languages\language_dropbox*.xml" "$OutputPath\Languages\Greenshot.Plugin.Dropbox" -Force
Copy-Item "$BuildArtifactsPath\Plugins\Greenshot.Plugin.Dropbox\Greenshot.Plugin.Dropbox.dll" "$OutputPath\Plugins\Greenshot.Plugin.Dropbox" -Force

# Copy ExternalCommand Plugin
New-Item -ItemType Directory -Path "$OutputPath\Languages\Greenshot.Plugin.ExternalCommand" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputPath\Plugins\Greenshot.Plugin.ExternalCommand" -Force | Out-Null
Copy-Item "$RepositoryRootPath\src\Greenshot.Plugin.ExternalCommand\Languages\language_externalcommand*.xml" "$OutputPath\Languages\Greenshot.Plugin.ExternalCommand" -Force
Copy-Item "$BuildArtifactsPath\Plugins\Greenshot.Plugin.ExternalCommand\Greenshot.Plugin.ExternalCommand.dll" "$OutputPath\Plugins\Greenshot.Plugin.ExternalCommand" -Force

# Copy Flickr Plugin
New-Item -ItemType Directory -Path "$OutputPath\Languages\Greenshot.Plugin.Flickr" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputPath\Plugins\Greenshot.Plugin.Flickr" -Force | Out-Null
Copy-Item "$RepositoryRootPath\src\Greenshot.Plugin.Flickr\Languages\language_flickr*.xml" "$OutputPath\Languages\Greenshot.Plugin.Flickr" -Force
Copy-Item "$BuildArtifactsPath\Plugins\Greenshot.Plugin.Flickr\Greenshot.Plugin.Flickr.dll" "$OutputPath\Plugins\Greenshot.Plugin.Flickr" -Force

# Copy GooglePhotos Plugin
New-Item -ItemType Directory -Path "$OutputPath\Languages\Greenshot.Plugin.GooglePhotos" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputPath\Plugins\Greenshot.Plugin.GooglePhotos" -Force | Out-Null
Copy-Item "$RepositoryRootPath\src\Greenshot.Plugin.GooglePhotos\Languages\language_googlephotos*.xml" "$OutputPath\Languages\Greenshot.Plugin.GooglePhotos" -Force
Copy-Item "$BuildArtifactsPath\Plugins\Greenshot.Plugin.GooglePhotos\Greenshot.Plugin.GooglePhotos.dll" "$OutputPath\Plugins\Greenshot.Plugin.GooglePhotos" -Force

# Copy Imgur Plugin
New-Item -ItemType Directory -Path "$OutputPath\Languages\Greenshot.Plugin.Imgur" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputPath\Plugins\Greenshot.Plugin.Imgur" -Force | Out-Null
Copy-Item "$RepositoryRootPath\src\Greenshot.Plugin.Imgur\Languages\language_imgur*.xml" "$OutputPath\Languages\Greenshot.Plugin.Imgur" -Force
Copy-Item "$BuildArtifactsPath\Plugins\Greenshot.Plugin.Imgur\Greenshot.Plugin.Imgur.dll" "$OutputPath\Plugins\Greenshot.Plugin.Imgur" -Force

# Copy Jira Plugin
New-Item -ItemType Directory -Path "$OutputPath\Languages\Greenshot.Plugin.Jira" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputPath\Plugins\Greenshot.Plugin.Jira" -Force | Out-Null
Copy-Item "$RepositoryRootPath\src\Greenshot.Plugin.Jira\Languages\language_jira*.xml" "$OutputPath\Languages\Greenshot.Plugin.Jira" -Force
Copy-Item "$BuildArtifactsPath\Plugins\Greenshot.Plugin.Jira\Greenshot.Plugin.Jira.dll" "$OutputPath\Plugins\Greenshot.Plugin.Jira" -Force
Copy-Item "$BuildArtifactsPath\Plugins\Greenshot.Plugin.Jira\Dapplo.Jira.dll" "$OutputPath\Plugins\Greenshot.Plugin.Jira" -Force
Copy-Item "$BuildArtifactsPath\Plugins\Greenshot.Plugin.Jira\Dapplo.Jira.SvgWinForms.dll" "$OutputPath\Plugins\Greenshot.Plugin.Jira" -Force

# Copy Office Plugin
New-Item -ItemType Directory -Path "$OutputPath\Plugins\Greenshot.Plugin.Office" -Force | Out-Null
Copy-Item "$BuildArtifactsPath\Plugins\Greenshot.Plugin.Office\Greenshot.Plugin.Office.dll" "$OutputPath\Plugins\Greenshot.Plugin.Office" -Force

# Copy Photobucket Plugin
New-Item -ItemType Directory -Path "$OutputPath\Languages\Greenshot.Plugin.Photobucket" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputPath\Plugins\Greenshot.Plugin.Photobucket" -Force | Out-Null
Copy-Item "$RepositoryRootPath\src\Greenshot.Plugin.Photobucket\Languages\language_photobucket*.xml" "$OutputPath\Languages\Greenshot.Plugin.Photobucket" -Force
Copy-Item "$BuildArtifactsPath\Plugins\Greenshot.Plugin.Photobucket\Greenshot.Plugin.Photobucket.dll" "$OutputPath\Plugins\Greenshot.Plugin.Photobucket" -Force

# Copy Win10 Plugin
New-Item -ItemType Directory -Path "$OutputPath\Languages\Greenshot.Plugin.Win10" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputPath\Plugins\Greenshot.Plugin.Win10" -Force | Out-Null
Copy-Item "$BuildArtifactsPath\Plugins\Greenshot.Plugin.Win10\Greenshot.Plugin.Win10.dll" "$OutputPath\Plugins\Greenshot.Plugin.Win10" -Force
Copy-Item "$BuildArtifactsPath\Plugins\Greenshot.Plugin.Win10\Microsoft.Toolkit.Uwp.Notifications.dll" "$OutputPath\Plugins\Greenshot.Plugin.Win10" -Force