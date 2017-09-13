---
layout: post
status: publish
published: true
release_version: 1.2.10.6
title: Greenshot 1.2.10 September fixes
tags:
- 1.2
- bugfix
- release
---
Summer break is over! Today, the 12th of September 2017, we bring you a new Greenshot Release which goes by the version 1.2.10.6.

One of the biggest reasons for the current release needs to be explained.

It was about a year ago that we introduced the first version of Greenshot which was signed. Due to that change, when you start the installer, you will see "Verified publisher: Open Source developer, Robin Krom". This means your download of Greenshot is the real thing, an unmodified version without bloatware or malware of Greenshot exactly as we made it. If starting your download does not show the publisher, it most likely is not the real thing and might be "infected" (intentionally or by accident) with something which should not be in there.

*(By the way, if you want to make sure you are using the original version, you should never download Greenshot from anywhere else than from our website. Just keep in mind: always get Greenshot from getgreenshot.org)*

To make signing possible we had to buy something what is called a _code signing certificate_, which is usually valid for one year. So a few weeks ago, almost a year later, we got a nice info mail and later a reminder, that the certificate will expire. Although Greenshot is free, meaning investing money could be considered a bad idea, we did buy a renewal to make it possible to continue to bring Greenshot securely to the masses. Unfortunately we found out that some smart people, responsible for the rules behind code signing, have introduced the need for a smart-card (hardware) for the code signing process in Februar 2017.

Although we now have bought the right to renew the certificate, our certificate provider has unfortunately made this renewal process extremely hard (and it wasn't easy in the first place). Even today we still didn't manage to do so, mainly due us having incompatible hardware. The introduction of the new rules, now needing special hardware, adds additional costs and complexity, which a lot of open source projects can't afford.

Even when we manage to renew the certificate, the changed demands for **using** a certificate will most likely cause some headache to implement. We probably will find a solution some day, but I hope it doesn't cost us additional money and more weeks of discussions and trying. It would be nice if a company can assist us, and other open source developers, by providing the needed knowledge or maybe even provide a free solution. (Our email-address is in the ["impressum"](/impressum/).)

This version of Greenshot mainly contains bugfixes, the development was stopped one day before the certificate expired so we could still bring you an signed version of Greenshot.

We hope this release fixes some of the issues you were having, the release notes follow below.

Happy ~~screenshotting~~greenshotting,<BR/>
Your Greenshot team


*Release notes for Greenshot 1.2.10.6-c2414cf RELEASE*

List of changes:

| Ticket | Description |
| --- | --- |
|[BUG-2235](https://greenshot.atlassian.net/browse/BUG-2235)|Imgur authentication issues due to imgur api change|
|[BUG-2227](https://greenshot.atlassian.net/browse/BUG-2227)|NullReferenceException when accessing the Imgur History|
|[BUG-2225](https://greenshot.atlassian.net/browse/BUG-2225)|NullReferenceException when changing the color of text|
|[BUG-2219](https://greenshot.atlassian.net/browse/BUG-2219)|Korean is spelled incorrectly in the installer|
|[BUG-2213](https://greenshot.atlassian.net/browse/BUG-2213)|NullReferenceException in the Freehand tool|
|[BUG-2203](https://greenshot.atlassian.net/browse/BUG-2203)|ArgumentNullException in the Freehand tool|
|[BUG-2141](https://greenshot.atlassian.net/browse/BUG-2141)|Imgur authentication issues due to old embedded IE|
|[BUG-2172](https://greenshot.atlassian.net/browse/BUG-2172)|NullReferenceException when using the speech bubble|
|[BUG-2246](https://greenshot.atlassian.net/browse/BUG-2246)|Login issues with the Atlassian Jira Cloud|
|[FEATURE-1064](https://greenshot.atlassian.net/browse/FEATURE-1064)|Added CTRL+Backspace & CTRL+A to the text tool|