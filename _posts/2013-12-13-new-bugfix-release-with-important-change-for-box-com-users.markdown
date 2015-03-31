---
layout: post
status: publish
published: true
title: New Bugfix Release with Important Change for Box.com Users
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 1076
wordpress_url: http://getgreenshot.org/?p=1076
date: !binary |-
  MjAxMy0xMi0xMyAxOTozOTo0MSArMDEwMA==
date_gmt: !binary |-
  MjAxMy0xMi0xMyAxNzozOTo0MSArMDEwMA==
categories:
- Releases
tags:
- release
- bugfix
- '1.1'
comments: []
---
<p>So here is another release in 2013 - probably the last one ;-) Important especially for those using Greenshot's Box plugin to send screenshots directly to Box.co. Box.com is applying a breaking change to the authentication API on December 13th, which means that the Box plugin released with Greenshot 1.1.6 will no longer work - we have changed the plugin to work with the new API as of Greenshot 1.1.7.</p>
<p>There are also some bug fixes, especially we got rid of an annoying problem with screenshots being exported to the wrong Word document.<br />
You can read detailed information in the change log below or <a href="/downloads/" title="Downloads">download Greenshot 1.1.7 right now</a>.</p>
<p>We switched our version control system from Subversion (SVN) to Git and have moved our repository to BitBucket, you can find it here: <a href="https://bitbucket.org/greenshot/greenshot/overview">https://bitbucket.org/greenshot/greenshot/overview</a><br />
This change makes it easier for other developers to supply patches to our code by sending us pull requests.</p>
<p><code><br />
1.1.7.17-g98c8f59 Bugfix Release</p>
<p>Changes:<br />
* We moved our repository to BitBucket (GIT), this forced us to change the build script and the version</p>
<p>Bugs resolved (for bug details go to http://sourceforge.net/p/greenshot/bugs and search on the ID):<br />
* Bug #1517: (now it is really fixed) export to Microsoft Word always goes to the last active Word instance.<br />
* Bug #1589/#1584 System.NullReferenceException<br />
* Bug #1574: AccessException when loading plugins</p>
<p>Features:<br />
* Changed the Box plug-in to use the new V2 API, which is mandatory from 13.12.2013. The Box plug-in now needs the .NET 3.5 Full Framework.<br />
</code></p>
