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


[If you find that Greenshot saves you a lot of time and/or money, you are very welcome to support the development of this screenshot software.](http://getgreenshot.org/support-greenshot/)


About this repository
---------------------
This repository has all the sources of Greenshot, but we have multiple branches of which two should be important to know about:

**1.1**
Greenshot 1.1.x can be found in the 1.1 branch, this branch represents the current release.

**1.2**
Greenshot 1.2.x can be found in the 1.2 branch, this branch will be coming soon.
But it mostly has a few more bug fixes and changes as are in our Greenshot release, as we collect multiple fixes before releasing.
1.2 is a "dying" branch, as we want to change a lot of the underlying application for 2.0 and most of Greenshot will need to be rewritten.
This means pull request with fixes or **small** changes are very welcome!

We now have a continuous integration system, you can find the latest unstable 1.2 builds [here](https://ci.appveyor.com/project/Greenshot/greenshot/history)

Current build status 1.2 branch: ![Unknown](https://ci.appveyor.com/api/projects/status/yh4jnjbo03qrl60d/branch/1.2?svg=true)


Developing for Greenshot
------------------------
We develop Greenshot with Visual Studio Express 2013 and tested our solution on Visual Studio Professional 2012.
It should be possible to compile Greenshot directly after a checkout, eventually Visual Studio needs to upgrade the solution.
What doesn't work are the plug-in for cloud storage (like Box, Dropbox, Imgur, Picasa and Photobucket) as these need "API keys".
These keys are not in our Greenshot repository, if you want to develop on one of the plug-ins you will need to create you own keys by registering with theses services as a developer.
I will add a description here later to explain how include your API keys so you can develop.

P.S.
Statistics on the Greenshot project, supplied by Open HUB, can be found [here](https://www.openhub.net/p/greenshot)
