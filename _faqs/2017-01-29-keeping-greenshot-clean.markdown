---
layout: faq
status: publish
published: true
title: What do we do to keep Greenshot clean from malware and viruses
permalink: /faq/is-greenshot-clean/
tags: []
---

Every now and than we get reports of users who have anti-virus software which detects _something_ in Greenshot,
and they are wondering how secure Greenshot actually is. 

First, we strongly recommend to always download Greenshot from our [official website](http://getgreenshot.org/)
 - there have been reports about some (even popular) software portals offering wrapped installers of open source projects,
bundling adware or even malware with the original installer. Also the installers for download are often outdated or "nightly builds" which might be buggy and are not intended to be used by a wide audience.


Athough there is no 100% garantee of building an application which is not infected. We have set a high standard, I would say much higher as some software you must pay for.

The following describes how we work to keep Greenshot clean:

1. Greenshot only contains program code that which we place into our Github Open Source repository, everybody can look into it meaning there are no secrets.
2. Only WE decide what ends up in our repository, and with that what ends into Greenshot, the community can supply us with changes but those are approved by us before moved into Greenshot.
3. Thanks to the nice people at [AppVeyor](https://www.appveyor.com/) Greenshot is build on a clean Windows image, not on our PCs. This has 2 advantages:
	* The Windows installation is never used by people and reset every time *before* a build starts. This makes sure that the Windows installation is not infected.
	* There is no chance that some modification to Greenshot, which is on our system but not pushed to github, ends up in our product.
4. We check Greenshot with [Virus Total](https://www.virustotal.com/) before we make it available to the public
5. We sign our product with a code signing certificate, since 1.2.9, so people can detect if someone tampered with the files.

So, as long as you download via *our* [site](https://getgreenshot.org/downloads) you should be fine.

About those people who reported that their anti-virus software detected something, upto now I think we did our work as:

* some never replied back to our question from *where* they downloaded Greenshot.
* some downloaded Greenshot from elsewhere, not from our site.
* the others were identified as false positives.

**See also:**

[Promised: No Bundled Installers, No Toolbars, No Unfair Advertisements](/2013/11/19/promised-no-bundled-installers-no-toolbars-no-unfair-advertisements/)


