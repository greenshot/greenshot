---
layout: post
status: publish
published: true
title: Third (and hopefully last) release candidate for Greenshot 1.2
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 1118
wordpress_url: http://getgreenshot.org/?p=1118
date: !binary |-
  MjAxNC0xMi0wNSAyMTozMzozNCArMDEwMA==
date_gmt: !binary |-
  MjAxNC0xMi0wNSAxOTozMzozNCArMDEwMA==
categories:
- Releases
tags:
- release candidate
- '1.2'
comments: []
---
<p>Okay - finishing line in sight :) we have uploaded the third release candidate for Greenshot 1.2. We have been working on ironing out a few more bugs of the latest version, as well as a few improvements and some user interface fine tuning. Again, thanks to anyone who supported us with their valuable feedback. You are doing a great job, we really appreciate that.</p>
<p>Most notable improvements are: torn edge and drop shadow settings are now stored and remembered next time, we have also improved the usability of the torn edge settings dialog. In the editor, the font family selection now displays its entries using the respective font so that it is easier to find the font you are looking for.<br />
We also got rid of several bugs, mostly related to the editor, e.g. the new speech bubble feature still had some flaws.</p>
<p>See below for the complete changelog of the third release candidate, if you haven't seen the previous ones yet you might want to have a look at the <a href="/2014/09/18/release-candidate-for-greenshot-1-2-available/">changelog of RC1</a> and <a href="/2014/11/11/second-release-candidate-greenshot-1-2/">RC2</a>, too. In any case, you should check out the new release candidate - as always, it can be downloaded from our <a href="/version-history/">version history</a> (1.2.3.29-RC3).</p>
<p><em>Yeah, you know that already, but just to be sure: "release candidate" means that this version has not gone through extensive testing yet. It might be buggy, so if you do not like surprises, we suggest to stick with version 1.1 until we have ironed out all issues and publish the stable release of version 1.2. In case you encounter any problems with the release candidate, please <a href="getgreenshot.org/tickets/">file a bug report</a> - thanks a lot.</em></p>
<p><code><br />
Features:<br />
* Greenshot now stores the settings of the torn edge & drop shadow effects<br />
* FEATURE-776: Improvement of the torn edge settings dialog<br />
* FEATURE-777: Improvement of the tool settings (font family)</p>
<p>Changes:<br />
* Optimized Greenshots update check to use even less traffic by checking the time-stamp of the update feed before downloading it.</p>
<p>Bugs Resolved:<br />
* BUG-1620: Greenshot crashing Chrome running in 'Windows 8 Mode'<br />
* BUG-1682: Speech bubble tail "gripper" moved outside of the drawing area, making it impossible to move the tail<br />
* BUG-1686: Shadow (drop shadow or torn edge) grows if a filter (highlight etc) is used and an element is moved around<br />
* BUG-1687: Speech bubble didn't have a working shadow<br />
* BUG-1698: Cannot enter textbox/Speechbubble lowercase text after changing font family<br />
* BUG-1699: UI jumps when Textbox or SpeechBubble tools are selected, and changing to another tool. (also the other way around)<br />
* BUG-1700: IE capture only works once<br />
* BUG-1701: Drop shadow setting "shadow thickness" wasn't restored.<br />
* BUG-1709: Alignment of the text and speech-bubble wasn't always correctly saved in the .greenshot file<br />
* BUG-1710: After resizing the canvas the edges had a slightly different color (1px)<br />
* BUG-1711: Changing settings (e.g. fill color) on the speech bubble before drawing it, caused an exception.<br />
... and a lot more small changes to make Greenshot more stable.<br />
</code></p>
