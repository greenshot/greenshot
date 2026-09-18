[InstallDelete]
// processed as the first step of installation.
#ifdef IsLightEdition
// Light edition strips all plugins so an installation over Full is completely clean
Type: filesandordirs; Name: "{app}\Plugins"
#endif

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

// Portable plugin directories
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Box"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Confluence"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Dropbox"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.ExternalCommand"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Flickr"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.GooglePhotos"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Imgur"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Jira"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Office"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Photobucket"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Win10"

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
Type: filesandordirs; Name: "{app}\Plugins\Photobucket"
Type: filesandordirs; Name: "{app}\Plugins\Win10"
Type: filesandordirs; Name: "{app}\Plugins\Zxing"

// Cleanup directory if there are no plugins left
Name: {app}\Plugins; Type: dirifempty;

// Cleanup the main directory if there are no files left
Name: {app}; Type: dirifempty;

// Clean up any loose manifest files or old packages before installing
Type: filesandordirs; Name: "{app}\Greenshot.ShellExt"
Type: files; Name: "{app}\Greenshot.ShellExt.msix"
