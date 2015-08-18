---
layout: post
status: publish
published: true
title: ! 'Promised: No Bundled Installers, No Toolbars, No Unfair Advertisements'
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 1063
wordpress_url: http://getgreenshot.org/?p=1063
date: !binary |-
  MjAxMy0xMS0xOSAyMDo1OTo0NCArMDEwMA==
date_gmt: !binary |-
  MjAxMy0xMS0xOSAxODo1OTo0NCArMDEwMA==
categories:
- Uncategorized
tags:
- website
- sourceforge
- advertising
- malware
comments: []
---
<p>As Greenshot is a free and open source project, we depend on donations and advertising to keep the project running. You probably know that already, since we published a blog post about our <a href="/2013/05/30/why-a-free-web-needs-advertising/">thoughts of advertising</a> not so long ago.</p>
<p>Recently, some users told us that bundled stuff like browser toolbars etc. had been installed on their computer by what they <em>assumed</em> was the Greenshot installer, which would have surprised us a lot, since there are no bundled installers or similar annoyances in Greenshot. <strong>We are convinced that bundled installers are abusing the user's trust and therefore we strongly oppose to it, no matter how much money marketing companies pay for them.</strong> In dialog with our users and with some research we then found out that these issues actually had nothing to do with our installer at all.</p>
<h2>Unfortunate advertisements on SourceForge and our website</h2>
<p>The problem was of another nature: our advertising service (which happens to be the same as SourceForge's) recently started to deliver advertisements with big green buttons on it, labeled "Download". The advertised pages do not offer the original Greenshot installer, but other software. However, this fact is not obvious on these pages (probably intended), one of them even offered an executable called Greenshot_Setup.exe.</p>
<p><a href="/assets/wp-content/uploads/2013/11/greenshot-2013-11-16-10_13_28.png"><img src="/assets/wp-content/uploads/2013/11/greenshot-2013-11-16-10_13_28-300x84.png" alt="greenshot-2013-11-16 10_13_28" width="300" height="84" class="alignright size-medium wp-image-1072" /></a>Of course, we get more ad revenue when advertisements are clicked, but we certainly do not want to trick our users into clicking them accidentally, let alone into installing any other tools than Greenshot.</p>
<p>If this has happened to you: please take our honest apologies, we are very sorry for that. Our advertisements are delivered by Google Adsense, so if you installed anything from their ads, you might want to know that they at least have very <a href="https://support.google.com/adwordspolicy/answer/1316548">strict rules for advertisers</a>.</p>
<p><strong>We are absolutely positive about web advertising to keep web projects going, but we believe ads must be fair and must not impose constraints on visitors.</strong><br />
As a consequence, we have blocked all known advertisers with those unloved "download button ads" from our website and made advertisements even more recognizable by applying a prominent border. We have also gotten in touch with SourceForge asking them to get rid of these ads, too.</p>
<h2>Help us to keep advertisements clean</h2>
<p>We can blacklist advertisements we regard unfair or misleading, but of course new advertisements may come in now and then. Also, depending on where you live you might see ads that we will never see over here. Please help us to get rid of them, send uns an <a href="mailto:getgreenshot@gmail.com?subject=Suspicious%20advertisement&body=The%20following%20ad%20is%20unfair%20/%20misleading%20/%20suspicious.%0A%0ATarget%20URL%20of%20the%20ad:%20%0AScreenshot%20of%20the%20ad:%20">email</a>, and don't forget to include a screenshot of the advertisement and (important) the URL it directs you, otherwise we cannot block it.</p>
<h2>Bundled installers from SourceForge (aka DevShare)</h2>
<p>Speaking of SourceForge: lately we got repeatedly approached about bundled installers in SourceForge downloads. There seems to be a big misunderstanding related to this: It is true that SourceForge offers a revenue program called <a href="http://sourceforge.net/devshare/why">DevShare</a> to projects hosted on their platform, which involves a bundled installer. However, this program is opt-in. As long as the developers of a project do not explicitly choose to take part in DevShare, SourceForge are not bundling anything with the installers.</p>
<p><strong>We, the Greenshot development team, have decided to keep our installer clean, without a doubt, now and in future.</strong></p>
<h2>Further considerations</h2>
<p>We are concerned about these alarming flows around free and open source software. This concern mainly relates to practices of reckless people or companies trying to make money on the back of open source projects and their users, but also (to some extend) to the advertising strategy of SourceForge. We are currently considering further steps like moving our installer downloads to another server where we have better control over the ads. Unfortunately, qualititive and reliable Content Delivery Network (CDN) hosting is not cheap.</p>
<p>We are not alone: the developers of the open source image editor <a href="http://www.gimp.org/">GIMP no longer upload their releases to the SourceForge file system</a>; for the sake of completeness and fairness, here is <a href="http://sourceforge.net/blog/advertising-bundling-community-and-criticism/">SourceForge's statement regarding to this discussion</a>.</p>
