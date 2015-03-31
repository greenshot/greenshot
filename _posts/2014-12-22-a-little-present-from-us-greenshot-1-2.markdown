---
layout: post
status: publish
published: true
title: ! 'A little present from us: Greenshot 1.2'
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 1124
wordpress_url: http://getgreenshot.org/?p=1124
date: !binary |-
  MjAxNC0xMi0yMiAyMjoyMTowNiArMDEwMA==
date_gmt: !binary |-
  MjAxNC0xMi0yMiAyMDoyMTowNiArMDEwMA==
categories:
- Releases
tags:
- release
- translation
- '1.2'
- latvian
comments: []

---
<p><a href="http://getgreenshot.org/2014/12/22/a-little-present-from-us-greenshot-1-2/speech-bubbles/" rel="attachment wp-att-1127"><img src="http://getgreenshot.org/wp-content/uploads/2014/12/speech-bubbles-300x276.png" alt="Speech bubbles in Greenshot editor" width="300" height="276" class="alignright size-medium wp-image-1127" /></a></p>
<p>Finally! For those of you who celebrate Christmas, here is a little Christmas present from us. For the rest, here is a little present for you, for no particular reason :)</p>
<p>If have a look at our blog regularly or follow us on Twitter or another social media channel, you probably have noticed that we have been eagerly working on release candidates for Greenshot 1.2. The new version comes just in time before 2015 is about to start; after three release candidates and a lot of bug fixes, improvements and enhancements in between.</p>
<p>So, what's new, you ask? Here we go, just to name a few:</p>
<ul>
<li><strong>Speech bubbles</strong>, a.k.a. call-outs: we know that many of you have been waiting for those, and we are glad to see them in the editor ourselves - the ideal tool to annotate a specific element with text.</li>
<li><strong>Incrementing labels</strong>: ever needed to point out several steps of a workflow like "first click here, then there, and finally over there, ..."? Probably... well, you no longer have to take care of the enumeration with text boxes manually. Incrementing labels are the much smarter and more beautiful solution :)</li>
<li><strong>Torn edge and border settings</strong>: we have improved both features by providing extra settings for them, e.g. you can select which edges of your image should be torn.</li>
<li><strong>Region mode accessibility</strong>: it is now possible to select a screenshot region with the keyboard, which allows exact and easy fine-tuning of a selected region, and of course allows the feature to be used at all for people having problems with handling a pointing device like mouse or touchpad.</li>
</ul>
<p><a href="http://getgreenshot.org/2014/12/22/a-little-present-from-us-greenshot-1-2/incrementing-labels/" rel="attachment wp-att-1128"><img src="http://getgreenshot.org/wp-content/uploads/2014/12/incrementing-labels-300x185.png" alt="Incrementing labels in Greenshot editor" width="300" height="185" class="alignleft size-medium wp-image-1128" /></a> Of course, that's not all - way too much to describe everything in detail here... we have also fixed quite some bugs and usability issues and we have a new language aboard, welcome Latvian (kudos to Kārlis Kalviškis for supplying the translation) as well as updates for many other languages.</p>
<p>Thanks again to everyone who keeps supporting us by submitting translations, feature requests, bug reports and donations! We really appreciate your support a lot, your help keeps the project going. :)</p>
<p>So much for now, we're getting ready for Christmas and new year's eve now. Have a good time everybody, and have fun with Greenshot - the latest version is always available at http://getgreenshot.org/downloads/<br />
And of course, here is the complete change log:</p>
<p><code><br />
All details to our tickets can be found here: https://greenshot.atlassian.net</p>
<p>Features:<br />
* Added the possibility to select the region to capture by using the keyboard, use the cursor keys to move the cursor (ctrl-key speeds up the movement) and the enter key to mark the start and ending.<br />
* Added support for another clipboard format "Format17" (aka CF_DIBV5) which could improve the result of the "open image from clipboard" and drag&drop actions.<br />
* Editor: a resize effect with settings window has been added.<br />
* Editor: a settings window for the torn-edge effect has been added, the settings will be stored.<br />
* Editor: a settings window for the drop shadow effect has been added, the settings will be stored.<br />
* OneNote: Enabled and enhanced the OneNote destination, so we can test this and see if it's worth releasing.<br />
* External command: If a command outputs an URI this will be captured and placed on the clipboard, the behaviour currently can only be modified in the greenshot.ini<br />
* FEATURE-184, FEATURE-282, FEATURE-486: Image editor now has a speech bubble<br />
* FEATURE-281, FEATURE-669, FEATURE-707, FEATURE-734: Image editor now has auto incrementing labels<br />
* FEATURE-757: Greenshot will now store the last used region in the greenshot.ini, which makes it also available after a restart.<br />
* FEATURE-758: Due to the fact that more and more high DPI displays are used, we added a setting to change the icon size.<br />
* FEATURE-776: Improvement of the torn edge settings dialog<br />
* FEATURE-777: Improvement of the font family settings, also changed the used font for every family making it easier to find your font.</p>
<p>Changes:<br />
* Optimized Greenshots update check to use even less traffic by checking the time-stamp of the update feed before downloading it.<br />
* JIRA: With JIRA 6.x using the SOAP (Webservice) API the access has gotten really slow, we improved the performance slightly by loading some information parallel. (In Greenshot 2.x we will move to another API.)<br />
* Dynamic destination context-menu: If a destination has child items the parent is still selectable and executes the default export, we now no longer repeat the parent in the children.<br />
* Dynamic destination context-menu: We are now leaving the sorting to the destination code, this allows us to e.g. show the default printer on top of the list.</p>
<p>Bugs Resolved:<br />
* BUG-1559, BUG-1643: Repeating hotkeys are now prevented.<br />
* BUG-1610: Image editor: 'Obfuscate' and 'Highlight' and more, now should rotate / resize correctly.<br />
* BUG-1619: Image editor: Autocrop now also considers the elements.<br />
* BUG-1620: Greenshot crashing Chrome running in 'Windows 8 Mode'<br />
* BUG-1653: Accessibility issues: Editor "File" menu entry can't be activated and missing translations<br />
* BUG-1667: removed horizontal alignment of textbox in input mode, as it caused problems with textbox focus and could not be implemented consistently anyway (no vertical alignment possible)<br />
* BUG-1671: Fixed error that occurred when double-clicking systray icon before the first time a screenshot was saved to file<br />
* BUG-1686: Shadow (drop shadow or torn edge) grows if a filter (highlight etc) is used and an element is moved around<br />
* BUG-1688: While drawing a textbox there are black vertical lines inside<br />
* BUG-1695: Fixed an issue with processing the response from Imgur, which caused the error "Value cannot be null. Parameter name: key"<br />
* BUG-1699: UI jumps when Textbox or SpeechBubble tools are selected, and changing to another tool. (also the other way around)<br />
* BUG-1700: IE capture only works once<br />
* BUG-1709: Alignment of the text and speech-bubble wasn't always correctly saved in the .greenshot file<br />
* BUG-1710: After resizing the canvas the edges had a slightly different color (1px)<br />
* BUG-1719: color dialog didn't accept HTML color names<br />
... and a lot more small bug fixes to make Greenshot more stable.</p>
<p>Languages:<br />
* New language: Latvian (thanks to Kārlis Kalviškis for providing the translation)<br />
* Updates for Ukrainian, Japanese and Italian translation<br />
</code></p>
