[InstallDelete]
// processed as the first step of installation.
// The start with Windows is the Run value (see docs/vNext/autostart.md): a Greenshot link in a Startup folder would start it a second time at every logon
Type: files; Name: "{userstartup}\Greenshot.lnk"; Tasks: startup
Type: files; Name: "{commonstartup}\Greenshot.lnk"; Tasks: startup; Check: IsAdminInstallMode
#ifdef IsLightEdition
// Light edition strips all plugins so an installation over Full is completely clean
Type: filesandordirs; Name: "{app}\Plugins"
// Greenshot Light has no command line and no proxy, it opens files with Greenshot.exe
Type: files; Name: "{app}\greenshot-proxy.exe"
Type: files; Name: "{app}\greenshot-cli.exe"
// Greenshot Light has no toasts: the icon, the icons and the kept captures of the toasts of the full version (see tasks-icons-reg.iss for the registration)
Type: files; Name: "{localappdata}\Greenshot\greenshot.png"
// The error and warning icons of earlier 1.4 builds, they are in Notifications\Icons now
Type: files; Name: "{localappdata}\Greenshot\notification-*.png"
Type: filesandordirs; Name: "{localappdata}\Greenshot\Notifications"
#endif

// Libraries of an earlier version that this version no longer uses (e.g. Nodify.dll and Fizzler.dll, which were once
// installed in the main directory) would stay behind, as Inno Setup only replaces the files it installs. The installer
// installs every library Greenshot needs, so all are removed first.
Type: files; Name: "{app}\*.dll"
// The Native Messaging manifests are only installed with includes\browser-extension.iss
Type: files; Name: "{app}\org.greenshot.proxy*.json"
// The command line was greenshot.com before it was renamed to greenshot-cli.exe
Type: files; Name: "{app}\greenshot.com"
// The XML language files were replaced by the language packs greenshot.{ietf}.ini, plugins had theirs in Languages\Plugins
Type: files; Name: "{app}\Languages\language*.xml"
Type: filesandordirs; Name: "{app}\Languages\Plugins"
// Greenshot no longer uploads to Imgur, the plugin was removed (Self-Service deletes the earlier uploads)
Type: files; Name: "{app}\Languages\greenshot.imgur.*.ini"

// Delete plugins from Greenshot 1.2
Type: filesandordirs; Name: "{app}\Plugins\GreenshotBoxPlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotConfluencePlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotDropboxPlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotExternalCommandPlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotFlickrPlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotImgurPlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotJiraPlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotOCRPlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotOfficePlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotPhotobucketPlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotPicasaPlugin"

// Plugin directories as installed now (the same as in the portable version and the build output).
// Removed first, so no file of an older version stays behind.
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Box"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Confluence"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.CustomUploader"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Dropbox"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.ExternalCommand"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Flickr"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.GooglePhotos"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Imgur"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Jira"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Office"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Pdf"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Photobucket"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.RecipeEditor"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Stamps"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Win10"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Zxing"

// Newer 1.3 plugins
Type: filesandordirs; Name: "{app}\Plugins\Box"
Type: filesandordirs; Name: "{app}\Plugins\Confluence"
Type: filesandordirs; Name: "{app}\Plugins\Dropbox"
Type: filesandordirs; Name: "{app}\Plugins\ExternalCommand"
Type: filesandordirs; Name: "{app}\Plugins\Flickr"
Type: filesandordirs; Name: "{app}\Plugins\GooglePhotos"
Type: filesandordirs; Name: "{app}\Plugins\Imgur"
Type: filesandordirs; Name: "{app}\Plugins\Jira"
Type: filesandordirs; Name: "{app}\Plugins\Office"
Type: filesandordirs; Name: "{app}\Plugins\Pdf"
Type: filesandordirs; Name: "{app}\Plugins\Photobucket"
Type: filesandordirs; Name: "{app}\Plugins\RecipeEditor"
Type: filesandordirs; Name: "{app}\Plugins\Win10"
Type: filesandordirs; Name: "{app}\Plugins\Zxing"

// Cleanup directory if there are no plugins left
Name: {app}\Plugins; Type: dirifempty;

// Cleanup the main directory if there are no files left
Name: {app}; Type: dirifempty;

[UninstallDelete]
// greenshot-mcp.exe is a separate download, extracted next to Greenshot.exe: the uninstaller removes it too (it was closed first, see code.iss)
Type: files; Name: "{app}\greenshot-mcp.exe"
// The icons and the kept captures of the toasts (notifications), which Greenshot writes when it runs
Type: files; Name: "{localappdata}\Greenshot\greenshot.png"
// The error and warning icons of earlier 1.4 builds, they are in Notifications\Icons now
Type: files; Name: "{localappdata}\Greenshot\notification-*.png"
Type: filesandordirs; Name: "{localappdata}\Greenshot\Notifications"
// The multi-core JIT profile, which Greenshot writes when it runs
Type: files; Name: "{localappdata}\Greenshot\Startup.profile"
// The log with its rolled files (Greenshot.log.1 to .3, see log4net-release.xml); greenshot.ini in %AppData% is kept
Type: files; Name: "{localappdata}\Greenshot\Greenshot.log*"
Type: dirifempty; Name: "{localappdata}\Greenshot"
