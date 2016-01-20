---
layout: post
status: publish
published: true
title: Second release candidate for version 1.0 - translations needed
date: !binary |-
  MjAxMi0wOS0wOSAyMTo0NDo1MSArMDIwMA==
date_gmt: !binary |-
  MjAxMi0wOS0wOSAxOTo0NDo1MSArMDIwMA==

tags:
- translation
- release candidate
- '1.0'
comments: []
---
<p>We have released the second release candidate for Greenshot 1.0 today, fixing issues that users found while using the first release candidate. Thanks to those who helped us by providing valuable feedback. All changes since the previous version are listed at the end of this post.</p>
<p>Feel free to <a href="/version-history/" target="_blank">download and install Greenshot 1.0.2.2011</a> and (as always) please <a href="http://sourceforge.net/tracker/?group_id=191585&atid=937972&status=1" target="_blank">let us know</a> if you encounter any bugs or problems with it. (Note: as long as Greenshot 1.0 is in the release candidate phase, the big download button on our <a href="/downloads/" title="Downloads">download page</a> still points to Greenshot 0.8, always check the version history if you are looking for the latest release canidates or unstable builds.)</p>
<p>Just one thing: there are many translations which are not up to date with the latest changes yet. Greenshot has a fallback mechanism always to display the english message if a translation is not available, so that it can be used even if a translation is incomplete. However, we'd be happy to have as many translations up-to-date as possible when Greenshot 1.0 reaches final state. Please have a look at <a href="/2012/08/10/introducing-the-brand-new-greenshot-language-editor-translators-wanted/" title="Introducing the brand new Greenshot Language Editor – translators wanted">this blog entry</a> to see how you can help us with translations.<br />
By the way: we would like to offer the installer in more languages, and also the homepage of our website, so that Greenshot is easily accessible for people all over the world. For your convenience, we have extracted the text of those to language files, too. You can find them in the <a href="http://greenshot.svn.sourceforge.net/viewvc/greenshot/trunk/Greenshot/Languages/?sortby=date&sortdir=down#dirlist" target="_blank">Languages directory</a> of our repository, e.g. the english files are called language_installer-en-US.xml and language-website-en-US.xml.<br />
This means that you can use the Greenshot Language Editor for these files, too.</p>
<p>Below the release notes for Greenshot 1.0.2 build 2011 Release Candidate 2</p>
<p><code><br />
Bugs resolved:<br />
* Fixed a problem with the "please wait". It's now possible to cancel the operation, this was needed due to problems with Imgur and other plugin uploads<br />
* Fixed a memory leak<br />
* Fixed some problems with the IE capture<br />
* Fixed some jira & confluence plugin bugs<br />
* Fixed an Outlook export bug<br />
* Fixed an issue with writing the greenshot.ini file<br />
* Changed the installer to install only a default set of components, so Greenshot is not overloaded</p>
<p>Known issues:<br />
* Greenshot general: I-Beam cursor isn't displayed correctly on the final result.<br />
* Greenshot general: Not all hotkeys can be changed in the editor. For example the pause or the Windows key need to be modified directly in the ini.<br />
* Greenshot editor: Rotate only rotates the bitmap, not the added elements or cursor<br />
* Greenshot editor: The shadow and torn edges effects don't create a transparent background yet.<br />
* Confluence Plug-in: the retrieving of the current page from firefox only works on the currently displayed Firefox tab. This is a problem since Firefox 13 and it is currently unknown if there is a fix.<br />
* OCR Plug-in: OCR is not working on 64 bit Windows, as the MODI-OCR component from Microsoft is not available in 64 bit, in this case Greenshot should be run in 32-bit. We are working on this.</p>
<p></code></p>
