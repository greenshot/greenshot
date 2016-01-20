---
layout: post
status: publish
published: true
title: Greenshot 0.8 beta is out

tags:
- '0.8'
- release
- features
---
<p>We have released a new version of Greenshot. With this release, version 0.8 is no longer flagged "unstable" :)<br />
<a href="/current/" class="button" title="Download Greenshot 0.8" rel="nofollow">Download</a><br />
Thanks to everyone who helped us creating the best Greenshot version ever by testing and providing feedback.</p>
<h2>So what's new in Greenshot 0.8 beta?</h2>
<p>Some of the most obvious (and most requested) features can be found in the image editor: for example, there's a new tool for cropping the screenshot down to the desired size.<br />
Very handy: attaching the screenshot to a email right out of Greenshot.<br />
We also added new utilities for highlighting things on a screenshot, you can also obfuscate things by blurring or pixelizing them.<br />
These are just some of the new features of Greenshot 0.8 - please have a look at the release notes at the end of this post for further information.</p>
<p>At least as much has changed under the hood - we have refactored quite a lot of the code and optimized things so that the new version performs much better and is more stable. Of course, we have also fixed many bugs, e.g. the odd "GDI+ exception" some of the users encountered often, problems when copying screenshots to the clipboard, etc...</p>
<p>We hope you like the new version as much as we do :)</p>
<p>P.S.: Greenshot is flattrable (do you say so?) now - if you are using <a href="https://flattr.com/">Flattr</a>, you might want to flattr here :) If you are not, have a look at their concept of contributing to "the internet"... Or consider to <a href="/support/">support the development</a> of this screenshot software in another way. Thank you :)</p>
<p><span style="color: #808080;">Bugs resolved:<br />
* save-as dialog honors default storage location again<br />
* fixed loop when holding down prnt key [ 1744618 ] - thanks to flszen.users.sourceforge.net for supplying a patch<br />
* fixed displayed grippers after duplicating a line element<br />
* fixed a lot of GDI+ exceptions when capturing by optimizing memory usage</span></p>
<p><span style="color: #808080;">* typo [ 2856747 ]<br />
* fixed clipboard functionality, should be more stable (not throwing exception with "Requested Clipboard operation did not succeed.") Bugs [ 2517965, 2817349, 2936190 ] and many more* fixed clipboard to work with OpenOffice [ 1842706 ]</span><span style="color: #808080;"><br />
* fixed initial startup problems when creating the startup shortcut<br />
* fixed exceptions when save path not available, incorrect or no permission when writing (will use SaveAs)<br />
* fixed camera sound is sometimes distorted<br />
* fixed region selection doesn't take the right size<br />
* fixed bug when capturing a maximized window: wrong size was taken and therefore additional pixels were captured<br />
* fixed capture bug which prevents a lot of people to use Greenshot (in 0.7.9 this was a "GDI+" exception).<br />
Problem was due to allocating the bitmap in the memory of the graphic card which is not always big enough.</span></p>
<p><span style="color: #808080;">Features added:<br />
* Optimized memory usage<br />
* Added crop tool<br />
* Added clipboard capture<br />
* Added plugin support<br />
* Added Bitmaps as object<br />
* Added filters for wiping sensitive information as was suggested in e.g. [ 2931946 ]<br />
* Added open from file<br />
* Added the captured window title (even when capturing a region) as option for the filename pattern<br />
* Added shadows which where supplied as patch in 2638542 and 2983930<br />
* Added Email as Output (a MAPI supporting Email client should be available)<br />
* Added double-click on icon to open last save location in Windows Explorer (or replacements)<br />
* Changed configuration loading to better support portable Greenshot usage.<br />
* Changed language from compiled resources to flexible xml files, user can add their own languages<br />
* Added "Select all" option for image editor<br />
* Added "Drag to", you can now drag images or image files to the Greenshot image editor.</span></p>
