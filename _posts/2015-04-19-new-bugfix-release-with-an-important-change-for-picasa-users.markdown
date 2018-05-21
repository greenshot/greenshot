---
layout: post
status: publish
published: true
title: ! 'New Bugfix Release with an Important Change for Picasa Users'
release_version: 1.2.6.7
tags:
- windows
- 1.2
- bugfix
- release
---
<p>So here is another bugfix release, important especially for those using Greenshot’s Picasa plugin to send screenshots directly to Picasa. Google is deprecating the OAuth 1.x authentication API on the 20th of April, which means that the Picasa plugin released with Greenshot 1.2.5 will no longer work – we have changed the plugin to work with the new API as of Greenshot 1.2.6.</p>
<p>There is also a bug fix for the Box plugin, there were problems authenticating to box.com, this should be fixed with 1.2.6.<br />
Additionally we added a small feature which allows you to drag and drop images directly from a Google image search in a browser onto the Greenshot editor.<br />
You can read detailed information in the change log below or <a href="http://getgreenshot.org/downloads/" title="Downloads">download Greenshot</a> 1.2.6 right now. </p>
<code>
CHANGE LOG:
All details to our tickets can be found here: https://greenshot.atlassian.net
1.2.6.7-RELEASE
Bugs Resolved:
* BUG-1769: Switched to OAuth 2 for Picasa Authentication, OAuth 1.x will be terminated as of 20th of April 2015.
* BUG-1770: Fix problems when a font doesn't want to draw itself.
* Bug 1774: Problems logging in to Box.com
Features add:
* FEATURE-838: Added support for dragging an image from google image search results directly into the editor.
</code>
