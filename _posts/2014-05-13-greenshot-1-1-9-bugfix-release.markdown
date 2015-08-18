---
layout: post
status: publish
published: true
title: Greenshot 1.1.9 Bugfix Release
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 1097
wordpress_url: http://getgreenshot.org/?p=1097
date: !binary |-
  MjAxNC0wNS0xMyAyMToxOTowMiArMDIwMA==
date_gmt: !binary |-
  MjAxNC0wNS0xMyAxOToxOTowMiArMDIwMA==
categories:
- Releases
tags:
- release
- '1.1'
comments: []
---
<p>A new version of Greenshot is available for download, containing bug fixes and small improvements. Most notably, we have</p>
<ul>
<li>fixed capture problems that came with a recent update for Windows 8.1, affecting users with multiple displays and different scaling</li>
<li>repaired upload to Box, which was broken in the previous version</li>
<li>updated Flickr plugin to be prepared for a breaking change in the Flickr API effective as of June, 27th</li>
</ul>
<p>Please see the change log below for more details about what's new. As always, you should make sure to download Greenshot from our <a href="/downloads/" title="Downloads">download page</a>, rather than being confident in 3rd party download portals to deliver the original version.</p>
<p><code><br />
CHANGE LOG:</p>
<p>1.1.9.13-g01ce82d Windows 8.1 & Box bug-fix Release</p>
<p>Bugs resolved:<br />
* Bug #1604,#1631,#1634: Capture problems since update to Windows 8.1 with multiple displays<br />
* Bug #1627: Box upload failed since 1.1.8<br />
* Unreported: The greenshot.ini values "NoGDICaptureForProduct" and "NoDWMCaptureForProduct" had incorrect defaults, which might cause issues with some applications when using the auto capture mode.</p>
<p>Features:<br />
* Feature #697: Added the possibility to replace characters/strings when a pattern is used, to have more control over the resulting filename. E.G. ${title:r ,_} will cause all the spaces to be replaced by underscores.<br />
* Feature #712: The amount of colour which images are reduce to, if the setting is active, has been made configurable in the greenshot.ini property OutputFileReduceColorsTo. Default stays at 256,<br />
* Feature #723: Adding a newline when exporting to Word or an open Outlook email, this makes it possible to repeat exports.</p>
<p>Changes:<br />
* Flickr plug-in: from June 27th, 2014 Flickr will only accept uploads via HTTPS! As the switch is already possible Greenshot has been changed accordingly.</p>
<p>Languages:<br />
* Updated the French translation for the Microsoft Office plug-in<br />
</code></p>
