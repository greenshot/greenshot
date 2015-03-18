---
layout: faq
status: publish
published: true
title: What exactly does the exe installer do?
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 880
wordpress_url: http://getgreenshot.org/?post_type=faq&#038;p=880
date: !binary |-
  MjAxMy0wNC0xNiAyMTo0MDo0MCArMDIwMA==
date_gmt: !binary |-
  MjAxMy0wNC0xNiAxOTo0MDo0MCArMDIwMA==
categories: []
tags: []
comments: []
---
<p>The Exe Installer does the following things:</p>
<ol>
<li>check pre-requisites (.NET 2.0 or higher)</li>
<li>display the license, ask where and what to install</li>
<li>uninstall any previously installed Greenshot</li>
<li>copy all the Greenshot files, including selected languages and plugins, to <code>Program files\Greenshot</code> (the actual path depends on 32 or 64 bit OS)</li>
<li>create shortcuts and a un-installer</li>
<li>use <a href="http://en.wikipedia.org/wiki/Native_Image_Generator">ngen</a> on the installed application, which should improve the performance (this takes a while)</li>
<li>create a startup registry entry</li>
<li>open our website with thank-you etc</li>
<li>start Greenshot (if selected)</li>
</ol>
<p>Except the startup registry there are no registry entries changed.</p>
<p>Our installer is created with Inno Setup, and details on the possible installer parameters can be found here: <a href="http://unattended.sourceforge.net/InnoSetup_Switches_ExitCodes.html">http://unattended.sourceforge.net/InnoSetup_Switches_ExitCodes.html</a></p>
<p><strong>See also:</strong><br />
<a href="/faq/in-which-cases-should-i-use-the-zip-package-instead-of-the-installer/">In which cases should I use the ZIP package instead of the installer?</a><br />
<a href="/faq/are-there-any-dependencies-to-other-software-frameworks/">Are there any dependencies to other software / frameworks?</a></p>
