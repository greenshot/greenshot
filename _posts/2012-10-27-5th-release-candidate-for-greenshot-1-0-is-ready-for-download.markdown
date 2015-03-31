---
layout: post
status: publish
published: true
title: 5th Release Candidate for Greenshot 1.0 is ready for Download
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 590
wordpress_url: http://getgreenshot.org/?p=590
date: !binary |-
  MjAxMi0xMC0yNyAxNjowNDoxOSArMDIwMA==
date_gmt: !binary |-
  MjAxMi0xMC0yNyAxNDowNDoxOSArMDIwMA==
categories:
- Releases
tags:
- release candidate
- '1.0'
comments: []
---
<p>We have just uploaded the fifth release candidate for Greenshot 1.0 - the new version contains a few bugfixes, for details have a look at the changelog at the end of this entry.</p>
<p>As always, please give us feedback if you have any problems. We are confident that (unless a serious bug is found in this release candidate) we will be ready for the final release of Greenshot 1.0 within the next few days. We are looking forward to this :)</p>
<p>For those of you that are already using Windows 8: we already did some testing with the current release candidate; we noticed that the installation process (during the optimzation phase) takes slightly longer than we are used to, but this might just be the case because if it is the first .NET application to be installed on a fresh Windows 8 system. So don't be surprised if the installer seems to do a slower job.</p>
<p>Enough talk, now <a href="/version-history/">download the latest release candidate</a> (RC5) :)</p>
<p>And here is the changelog:</p>
<p><code><br />
1.0.5 build #### Release Candidate 5</p>
<p>Bugs resolved<br />
* Fixed translation issue in expert settings, clipboard format options not being updated right away after switching UI language, see bug #3576073<br />
* Fixed bug #3578392: one of the clipboard formats we generate doesn't support transparency, but we forgot to remove it.<br />
* Fixed a bug that prevented the clipboard formats to represent the general output settings. (decrease colors if possible etc)<br />
* Fixed a bug that the context menu was shown 2x, it's not really visible but does not improve the performance.<br />
* Fixed a bug when capturung a minimized IE window, now we first restore it (un-minimize) before capturing.<br />
</code></p>
