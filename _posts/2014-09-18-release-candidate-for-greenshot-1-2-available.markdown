---
layout: post
status: publish
published: true
title: Release candidate for Greenshot 1.2 available

tags:
- release candidate
- '1.2'

---
<p>We have just uploaded the first release candidate for Greenshot 1.2. It fixes some bugs of the prior version, but, far more important, adds some features that have been requested very often... most notably, the Greenshot 1.2 editor will finally draw speech bubbles / callouts, auto-incrementing labels and offer the possibility to resize the screenshot.</p>
<p>As usual, the disclaimer: "release candidate" means that this version has not gone through extensive testing yet. It might be buggy, so if you do not like surprises, we suggest to stick with version 1.1 until we have ironed out all issues and publish the stable release of version 1.2. In case you encounter any problems with the release candidate, please <a href="/tickets/">file a bug report</a> - thanks a lot.</p>
<p>And, as usal, here is the complete change log - you can download the release candidate (version 1.2.1.2-RC1 - installer, ZIP or PortableApps version) from our <a href="/version-history/">version history</a>:<br />
<code><br />
All details to our tickets can be found here: https://greenshot.atlassian.net</p>
<p>Features:<br />
* FEATURE-184, FEATURE-282, FEATURE-486: Image editor now has a speech bubble<br />
* FEATURE-281, FEATURE-669, FEATURE-707, FEATURE-734: Image editor now has auto incrementing labels<br />
* Editor: a resize effect with settings window has been added.<br />
* Editor: a settings window for the torn-edge effect has been added.<br />
* Editor: a settings window for the drop shadow effect has been added.<br />
* OneNote: Enabled and enhanced the OneNote destination, so we can test this and see if it's worth releasing.<br />
* External command: If a command outputs an URI this will be captured and placed on the clipboard, the behavior currently can only be modified in the greenshot.ini</p>
<p>Bugs resolved:<br />
* BUG-1559, BUG-1643: Repeating hotkeys are now prevented.<br />
* BUG-1610: Image editor: 'Obfuscate' and 'Highlight' and more, now should rotate / resize correctly.<br />
* BUG-1619: Image editor: Autocrop now also considers the elements.<br />
* BUG-1653: Accessibility issues: Editor "File" menu entry can't be activated and missing translations</p>
<p>Changes:<br />
* Dynamic destination context-menu: If a destination has child items the parent is still selectable and executes the default export, we now no longer repeat the parent in the children.<br />
* Dynamic destination context-menu: We are now leaving the sorting to the destination code, this allows us to e.g. show the default printer on top of the list.</p>
<p>Languages:<br />
* Updates for Ukrainian and Japanese translation</p>
<p></code></p>
