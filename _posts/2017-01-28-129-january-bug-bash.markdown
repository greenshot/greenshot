---
layout: post
status: publish
published: true
release_version: 1.2.9.129
title: Greenshot 1.2.9 January Bug Bash
tags:
- 1.2
- bugfix
- release
---
Good news everyone!

Today we present you the result of our January Greenshot [Bug Bash](https://en.wikipedia.org/wiki/Bug_bash).
In 28 days we fixed 24 bugs, to make this possible we invested around 60 hours of development time.
We would like to thank all people for taking time to report bugs, without we wouldn't be able to know about them.
Especially the people who reported bugs and were available for questions & testing have
our utmost gratitude, without them we would not haven been able to fix this amount of bugs in such a short time.

You can find the complete list of all changes below.

Some special notes for people coming from Greenshot 1.2.8 (or older):

* For Jira plug-in users:
  * As Atlassian dropped the SOAP API, the Jira plug-in now uses the REST API. This was introduced with Jira 5.x, but the oldest version we were able to test against is 6.1.3 so we can't garantee it works for anything older
  * To make it possible to keep using the Jira plug-in we used code which was due in Greenshot 2.0, this is why **the Jira plug-in now needs .NET 4.5**
* We changed the behavior of the effect buttons: left mouse click directly applies and the right mouse click brings up the settings first.

And last but not least, we reached a total of 37 languages with our latest addition: [Taqbaylit](https://en.wikipedia.org/wiki/Kabyle_language)

P.S.
In case you missed it, we now also have an [OSX version of Greenshot](/2017/01/10/mac-os-launch/).


Happy ~~sc~~greenshotting,<BR/>
Your Greenshot team


*Release notes for Greenshot 1.2.9.129-569de71 RELEASE*

List of bugs fixed:

| Ticket | Description |
| --- | --- |
| [BUG-2051](https://greenshot.atlassian.net/browse/BUG-2051) | Scroll-lock button not usable as hotkey |
| [BUG-2056](https://greenshot.atlassian.net/browse/BUG-2056) | Cannot export to external command Paint.NET if .greenshot format is used |
| [BUG-2081](https://greenshot.atlassian.net/browse/BUG-2081) | Canvas resize (Ctrl + / Ctrl -) only works via numpad keys |
| [BUG-2093](https://greenshot.atlassian.net/browse/BUG-2093) | Shadow effects not rendering correctly on Windows 10 |
| [BUG-2095](https://greenshot.atlassian.net/browse/BUG-2095) | 'Save as' doesn't remember last saved directory (after restart) |
| [BUG-2097](https://greenshot.atlassian.net/browse/BUG-2097) | Window border is not captured on Windows 10 |
| [BUG-2102](https://greenshot.atlassian.net/browse/BUG-2102) | InvalidOperationException when selecting a color |  
| [BUG-2104](https://greenshot.atlassian.net/browse/BUG-2104) | Speechbubble can't be used after copy/paste |
| [BUG-2105](https://greenshot.atlassian.net/browse/BUG-2105) | Window border is not captured on Windows 10 |
| [BUG-2108](https://greenshot.atlassian.net/browse/BUG-2108) | Capture last region doesn't work |
| [BUG-2109](https://greenshot.atlassian.net/browse/BUG-2109) | Double-click on textbox causes NullReferenceException |
| [BUG-2111](https://greenshot.atlassian.net/browse/BUG-2111) | Drag and Drop image file on editor doesn't work |
| [BUG-2114](https://greenshot.atlassian.net/browse/BUG-2114) | Storage location reset to default if not available during start |
| [BUG-2115](https://greenshot.atlassian.net/browse/BUG-2115) | Error while trying to upload screen shot to Jira |
| [BUG-2116](https://greenshot.atlassian.net/browse/BUG-2116) | MemberAccessException when opening the color dialog |
| [BUG-2119](https://greenshot.atlassian.net/browse/BUG-2119) | Crash on editing (dragged line) |
| [BUG-2120](https://greenshot.atlassian.net/browse/BUG-2120) | Greenshot Editor Canvas Changed to White Screen |
| [BUG-2121](https://greenshot.atlassian.net/browse/BUG-2121) | NullReferenceException when moving text |
| [BUG-2122](https://greenshot.atlassian.net/browse/BUG-2122) | Jira Plugin: Crash when uploading to Jira |
| [BUG-2124](https://greenshot.atlassian.net/browse/BUG-2124) | Flickr plugin generates wrong image link |
| [BUG-2125](https://greenshot.atlassian.net/browse/BUG-2125) | Send to OneNote does not work |
| [BUG-2126](https://greenshot.atlassian.net/browse/BUG-2126) | MemberAccessException during un-DropShadow of Ellipse |
| [FEATURE-992](https://greenshot.atlassian.net/browse/FEATURE-992) | When reusing the editor, it is not brought to the front (fix) |
| [FEATURE-998](https://greenshot.atlassian.net/browse/FEATURE-998) | Opening last capture in explorer should select/jump to the file (fix) |

Changes:

* Added [Taqbaylit](https://en.wikipedia.org/wiki/Kabyle_language) as a new language.