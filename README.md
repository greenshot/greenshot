Greenshot - a free screenshot tool optimized for productivity
=============================================================

Welcome to the source repository for Greenshot

What is Greenshot?
------------------

Greenshot is a light-weight screenshot software tool for Windows with the following key features:

* Quickly create screenshots of a selected region, window or fullscreen; you can even capture complete (scrolling) web pages from Internet Explorer.
* Easily annotate, highlight or obfuscate parts of the screenshot.
* Export the screenshot in various ways: save to file, send to printer, copy to clipboard, attach to e-mail, send Office programs or upload to photo sites like Flickr or Picasa, and others.
and a lot more options simplyfying creation of and work with screenshots every day.

Being easy to understand and configurable, Greenshot is an efficient tool for project managers, software developers, technical writers, testers and anyone else creating screenshots.


[If you find that Greenshot saves you a lot of time and/or money, you are very welcome to support the development of this screenshot software.](https://getgreenshot.org/support/)


About this repository
---------------------
This repository has all the sources of Greenshot, but we have multiple branches of which three should be important to know about:

**develop**
This is the future of Greenshot, currrently highly unstable but the only place to develop new functionality.

**master**
Is the current release, in our case 1.2.8.84

**feature/1.2.9**
Greenshot 1.2.9, which has the state of being a release candidate 


You can find the latest release and unstable builds [here](http://getgreenshot.org/version-history/)


Developing for Greenshot
------------------------
We develop Greenshot with Visual Studio 2015 and tested our solution on Visual Studio Professional 2015.
It should be possible to compile Greenshot directly after a checkout, when a nuget package restore is made.
What doesn't work are the plug-in for cloud storage (like Box, Dropbox, Imgur, Picasa and Photobucket) as these need "API keys".
These keys are not in our Greenshot repository, if you want to develop on one of the plug-ins you will need to create you own keys by registering with theses services as a developer.
I will add a description here later to explain how include your API keys so you can develop.

P.S.
Statistics on the Greenshot project, supplied by Open HUB, can be found [here](https://www.openhub.net/p/greenshot)
