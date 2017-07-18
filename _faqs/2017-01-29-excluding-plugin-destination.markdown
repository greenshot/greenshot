---
layout: faq
status: publish
published: true
title: How can I remove plugins or destinations from Greenshot?
permalink: /faq/how-remove-plugins-or-destinations-from-greenshot/
tags: []
---

This document describes how to make Greenshot skip loading of certain plugins, or not to show certain destinations.

Plug-in
-------

The best way to remove plugins, is to use the installer and only select those you want.
But sometimes this is not possible, so we build a way into Greenshot to skip the loading of them.

This can be done by added a setting to the "Core" section called "ExcludePlugins" to excluding plugins, or you can use "IncludePlugins" which has a list of every plugin you want and the rest is automatically excluded.
This might solve the adding of additional plugins when new releases bring new plugins, but you might also loose functionality without knowing: in the next release the editor is a plugin.

Here is an example on how to exclude plugins by adding the plugins you don't want to the greenshot.ini:

1. Find the greenshot.ini by reading [this](http://getgreenshot.org/faq/where-does-greenshot-store-its-configuration-settings/) or modify/create the greenshot-fixed.ini as described [here](/faq/what-is-the-best-way-to-control-greenshots-configuration-at-install-time/)
2. Stop Greenshot
3. Open file in an editor
4. Modify the line which should be under the "[Core]" section of the ini starting with "ExcludePlugins" (or add it, if not available), you can add all the plugin names with a comma in between.
5. Save the file
6. Start Greenshot again.

An example to exclude the box and dropbox plug-ins add the following line somewhere under the ```[Core]``` section:

```
ExcludePlugins=Box Plugin,Dropbox Plugin
```

The names of the plugin are visible in the Plugins tab of the settings or the greenshot.log (due to unfortunate decision while coding, they all end with " Plugin".

For completeness we describe the plugins here (do not include the quotes!):

* "Box Plugin"
* "Confluence Plugin"
* "Dropbox Plugin"
* "External command Plugin"
* "Flickr Plugin"
* "Imgur Plugin"
* "Jira Plugin"
* "OCR Plugin"
* "Office Plugin"
* "Photobucket Plugin"
* "Picasa-Web Plugin"


Destination
-----------

If you want to exclude destinations only, for example the office plugin has multiple and you might want only one of them, you can modify the "ExcludeDestinations" line.

The names of the destinations are a bit harder to find, here is the currently known list (do not include the quotes!):

* "Box"
* "Clipboard"
* "Confluence"
* "Dropbox"
* "EMail"
* "Editor"
* "Excel"
* "External MS Paint"
* "External Paint.NET"
* "FileDialog"
* "FileNoDialog"
* "Flickr"
* "Imgur"
* "Jira"
* "OCR"
* "OneNote"
* "Outlook"
* "Photobucket"
* "Picasa"
* "Powerpoint"
* "Printer"
* "Word"

**See also:**

[Where does Greenshot store its configuration settings?](/faq/where-does-greenshot-store-its-configuration-settings/)

[What is the best way to control Greenshot's configuration at install time?](/faq/what-is-the-best-way-to-control-greenshots-configuration-at-install-time/)
