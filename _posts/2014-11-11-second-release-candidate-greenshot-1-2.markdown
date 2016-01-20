---
layout: post
status: publish
published: true
title: Second release candidate for Greenshot 1.2

tags:
- release candidate
- '1.2'

---
<p>It's time for another release candidate for Greenshot 1.2. If you have tried the <a href="/2014/09/18/release-candidate-for-greenshot-1-2-available/">first release candidate</a>, you probably already saw some of the brand new features like the speech bubble tool in the editor. Thanks to the awesome users who came up with useful bug reports and suggestions we now have ironed out some things that were not working perfectly yet.</p>
<p>But that's not all: a few small features have made it into the new release candidate also. </p>
<ul>
<li>There's a new option to resize the icons (e.g. of the editor toolbar), so they will be recognizable again if you are using a high-resolution display. (Most of the icons used are still optimized to be displayed at a size of 16 x 16 pixels, so they might look a little bit blurred when enlarged. We will add new (vector) icons when the time has come for an editor redesign.)</li>
<li>The last capture region can now be re-used even after a restart of Greenshot or a reboot.</li>
<li>In region mode, the area to capture can now be selected using the keyboard: use arrow keys to move the cursor, then <kbd>Enter</kbd> to lock one corner of the region and - after moving the cursors on - hit <kbd>Enter</kbd> again at the opposite corner to capture the region</li>
</ul>
<p>See below for the complete changelog of the second release candidate, if you haven't seen the first one yet you might want to have a look at the <a href="/2014/09/18/release-candidate-for-greenshot-1-2-available/">changelog of RC1</a>, too. In any case, you should check out the new release candidate - as always, it can be downloaded from our <a href="/version-history/">version history</a> (1.2.2.43-RC2).</p>
<p><em>And, as usual, the disclaimer: "release candidate" means that this version has not gone through extensive testing yet. It might be buggy, so if you do not like surprises, we suggest to stick with version 1.1 until we have ironed out all issues and publish the stable release of version 1.2. In case you encounter any problems with the release candidate, please <a href="getgreenshot.org/tickets/">file a bug report</a> - thanks a lot.</em></p>
<p><code><br />
Features:<br />
* Added the possibility to select the region to capture by using the keyboard, use the cursor keys to move the cursor (ctrl-key speeds up the movement) and the enter key to mark the start and ending.<br />
* FEATURE-757: Greenshot will now store the last used region in the greenshot.ini, which makes it also available after a restart.<br />
* FEATURE-758: Due to the fact that more and more high DPI displays are used, we added a setting to change the icon size.<br />
* Added support for another clipboard format "Format17" (aka CF_DIBV5) which should improve the result of the "open image from clipboard" and drag&drop actions.</p>
<p>Changes:<br />
* JIRA: With JIRA 6.x using the SOAP (Webservice) API the access has gotten really slow, we improved the performance slightly by loading some information parallel. (In Greenshot 2.x we will move to another API.)</p>
<p>Bugs Resolved:<br />
* BUG-1667: removed horizontal alignment of textbox in input mode, as it caused problems with textbox focus and could not be implemented consistently anyway (no vertical alignment possible)<br />
* BUG-1681: Improvements for the new speech bubble, text color is now the same as the border and the rounded corners are correctly calculated when using thick lines and a small bubble.<br />
* BUG-1695: Fixed an issue with processing the response from Imgur, which caused the error "Value cannot be null. Parameter name: key"</p>
<p></code></p>
