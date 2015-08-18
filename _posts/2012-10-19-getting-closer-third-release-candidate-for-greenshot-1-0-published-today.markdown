---
layout: post
status: publish
published: true
title: ! 'Updated: Getting closer... third release candidate for Greenshot 1.0 published
  today'
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 568
wordpress_url: http://getgreenshot.org/?p=568
date: !binary |-
  MjAxMi0xMC0xOSAxNjo0ODoxNyArMDIwMA==
date_gmt: !binary |-
  MjAxMi0xMC0xOSAxNDo0ODoxNyArMDIwMA==
categories:
- Releases
tags:
- release candidate
- '1.0'
comments: []
---
<p><strong>Update: </strong> RC3 has introduced two annoying bugs with several plugins, RC4 fixes those and is available now.<br><br />
Read the <a href="/2012/10/20/release-candidate-4-is-available/">RC4 release post</a> for details.</p>
<hr>
<p>There is a German saying "Gut Ding will Weile haben", meaning roughly "good things need time".<br />
For Greenshot 1.0 RC3 we took a bit more time than planned, we want to make Greenshot 1.0 as good as possible and this time was needed.<br />
As we made a lot of changes the risk for new bugs is higher,  but if found these will be fixed quickly with RC4 / <ins datetime="2012-10-21T18:35:56+00:00">RC5</ins>.<br />
The final release should be shortly after <del datetime="2012-10-21T18:35:56+00:00">RC4</del> <ins datetime="2012-10-21T18:35:56+00:00">RC5</ins>.</p>
<p><a href="/version-history/" target="_blank">Download the latest Greenshot release candidate from our version history page</a>.</p>
<p>The exact details of the changes in Greenshot 1.0 RC3 are listed below, but here are the biggest:<br />
* Performance improvements, in several areas but mainly noticeable directly after captures.<br />
* The upload plugins now use our own code, this made it possible to get a consistent experience when authorizing with different platforms<br />
* By removing a lot of 3rd party libs Greenshot is now smaller, should also use a bit less memory and most upload plugins no longer need .NET 3.5<br />
* Finally fixed a bug in our shadow/torn edges effect which forced us to use a white background, now we can use transparency.<br />
* A lot of small bug and translation fixes</p>
<p>Happy Greenshotting!</p>
<p>Here's the complete changelog for RC3:<br />
<code><br />
1.0.3 build 2159 Release Candidate 3</p>
<p>Features added:<br />
* The Imgur plugin now has login support, uncheck the "Annonymous" checkbox in the settings.<br />
* Added new languages, see our blog<br />
* Moved Office destinations to their own plugin, making it possible for people to install Greenshot without</p>
<p>Bugs resolved:<br />
* Fixed OCR not working on Windows in 64 bit modus. People who want to use OCR without having office, read our FAQ!<br />
* Fixed a performance issue with the windows capture.<br />
* Fixed issues with Dropbox authentication, using our own OAuth implementation<br />
* Fixed problems with generating transparency for shadow & torn edges.<br />
* Fixed 2 bugs with the capture last region, see bug #3569703<br />
* Fixed weird window titles showing up in the "Capture Internet Explorer from list"<br />
* Fixed some more bugs with the dynamic destination picker and also fixed a small general issue with Office destinations.<br />
* Removed unneeded files in the installer, making it smaller<br />
* Fixed a lot of small translation texts and some icons in menus<br />
* Fixed missing translations and enlarged some labels of settings for several plugins: external command, Jira, Imgur, Flickr and Picasa<br />
* Fixed some upload issues, most uploads should have better performance and use less memory. Also removed some dependencies, which make the plugins and Greenshot smaller and work with .NET 2.0</p>
<p>Features removed:<br />
* Remove any history management of all non annonymous upload plug-ins, Greenshot is for uploading not managing uploads.</p>
<p>Known issues:<br />
* Greenshot general: the I-Beam cursor isn't displayed correctly on the final result.<br />
* Greenshot general: Not all hotkeys can be changed in the editor. For example the pause or the Windows key need to be modified directly in the ini.<br />
* Greenshot editor: Rotate only rotates the screenshot, not the added elements or cursor<br />
* Confluence Plug-in: the retrieving of the current page from firefox only works on the currently displayed Firefox tab. This is a problem since Firefox 13 and it is currently unknown if there is a fix.</p>
<p></code></p>
