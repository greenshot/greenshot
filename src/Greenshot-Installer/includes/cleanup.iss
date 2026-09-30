[InstallDelete]
// processed as the first step of installation.
#ifdef IsLightEdition
// Light edition strips all plugins so an installation over Full is completely clean
Type: filesandordirs; Name: "{app}\Plugins"
#endif

// Libraries of an earlier version that this version no longer uses (e.g. Nodify.dll and Fizzler.dll, which were once
// installed in the main directory) would stay behind, as Inno Setup only replaces the files it installs. The installer
// installs every library Greenshot needs, so all are removed first.
Type: files; Name: "{app}\*.dll"
// The Native Messaging manifests are only installed with includes\browser-extension.iss
Type: files; Name: "{app}\org.greenshot.proxy*.json"

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
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Dropbox"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.ExternalCommand"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Flickr"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.GooglePhotos"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Imgur"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Jira"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Office"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Photobucket"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.RecipeEditor"
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
Type: filesandordirs; Name: "{app}\Plugins\Photobucket"
Type: filesandordirs; Name: "{app}\Plugins\RecipeEditor"
Type: filesandordirs; Name: "{app}\Plugins\Win10"
Type: filesandordirs; Name: "{app}\Plugins\Zxing"

// Cleanup directory if there are no plugins left
Name: {app}\Plugins; Type: dirifempty;

// Cleanup the main directory if there are no files left
Name: {app}; Type: dirifempty;
