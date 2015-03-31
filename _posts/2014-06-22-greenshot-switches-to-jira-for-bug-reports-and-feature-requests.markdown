---
layout: post
status: publish
published: true
title: Greenshot Switches to JIRA for Bug Reports and Feature Requests
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 1102
wordpress_url: http://getgreenshot.org/?p=1102
date: !binary |-
  MjAxNC0wNi0yMiAyMTo1NTowOCArMDIwMA==
date_gmt: !binary |-
  MjAxNC0wNi0yMiAxOTo1NTowOCArMDIwMA==
categories:
- Announcements
tags:
- website
- sourceforge
- jira
- tickets
comments: []
---
<p>The difference between good and great software often originates from the developers closely listening to the users. As your ideas and comments are valuable input for us, we think that you need be able to easily report bugs and request features, as well as to quickly find information on problems/suggestions that have already been reported by other users.</p>
<p>For a long time, we have used the Sourceforge ticketing system for this, and although it worked for us many years we always thought it wasn't perfect for our needs. As every member of the Greenshot team also has experience with another ticketing tool, which we all like a lot, it is now time for a change.</p>
<p>We are proud to say we migrated the <a href="http://getgreenshot.org/tickets/" target="_blank">Greenshot bug/feature tracker</a> to Atlassian Jira, a professional ticketing system. We really think that this improves overall user experience both for us and for you, no matter if you want to report an issue or simply get an overview about what we are up to for the next version of Greenshot. On top of this it is integrated with Atlassian's BitBucket, where our source code are hosted, which makes it easy to spot what exactly has been changed and why.</p>
<p>Thankfully, <a href="https://www.atlassian.com/" target="_blank">Atlassian</a> has a great program to support open source teams with free software services and hosting. We were already using BitBucket and now we introduce Jira, maybe other products follow.</p>
<p>What does this mean for you?</p>
<p>Well, as we hate losing information, we migrated all the current bug reports and feature requests, with the complete history, to our new system. Also all Sourceforge users who reported tickets have been copied to our Jira. The users were created having the same username as with Sourceforge but without a password. At the first login you are asked to reset your password, to make this possible you will receive an email at your Sourceforge account with instructions. After successfully setting your password and being logged in you can change your email address if you like.</p>
<p>Additionally we have been looking for a solution to make it easier and quicker to bring a new version of Greenshot to you, independent of whether we want to build a release or just a version which should solve the bug(s) or add the feature request(s) you reported. For .NET applications this is not easy and as Greenshot is free it shouldn't cost money, so up to now we have been building Greenshot manually, which was very time intensive and sometimes caused a bit of stress when we forgot something and needed to start all over. Now we finally found an extremely good solution (which is also free for open source projects), and are proud to introduce <a href="http://www.appveyor.com/" target="_blank">AppVeyor</a>, a Continuous Integration system for .NET applications, which builds Greenshot for us. The people at AppVeyor really created a great system, it took only a couple of hours to make our build work, and the few questions we had were answered quickly. Although you won't notice this directly, it does make it possible for us to concentrate on the development so we hope we can fix and extend Greenshot at a quicker pace.</p>
<p>We hope to see you on our new Ticketing system and are looking forward to having a good time while further improving Greenshot together with you.</p>
