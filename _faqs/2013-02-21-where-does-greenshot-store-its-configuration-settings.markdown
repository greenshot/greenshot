---
layout: faq
status: publish
published: true
title: Where does Greenshot store its configuration settings?
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 818
wordpress_url: http://getgreenshot.org/?post_type=faq&#038;p=818
date: !binary |-
  MjAxMy0wMi0yMSAyMTo0NTo1MCArMDEwMA==
date_gmt: !binary |-
  MjAxMy0wMi0yMSAxOTo0NTo1MCArMDEwMA==
categories: []
tags: []
comments: []
---
<p>Greenshot keeps its configuration in a file called Greenshot.ini.<br />
Its storage location depends on which version of Greenshot you are using as well as your Windows version:</p>
<p><strong>If you are using the installer version of Greenshot</strong>, the file is stored in your (roaming) application data folder, which is a path like</p>
<ul>
<li><code>C:\Users\%USERNAME%\AppData\Roaming\Greenshot\</code> in Windows Vista and newer version or </li>
<li><code>C:\Documents and Settings\%USERNAME%\Application Data\Greenshot\</code> in Windows XP</li>
</ul>
<p><strong>If you are using the ZIP version of Greenshot</strong>, the file is stored within the directory where you unzipped Greenshot to. This allows you to put Greenshot e.g. on a memory stick and use it as a portable application.</p>
<p>If you want to have a look at the configuration file or need to change something (be careful!), right-click Greenshot's systray icon, select "About Greenshot" and hit the <kbd>I</kbd> key on your keyboard - Greenshot.ini will open in your default text editor.</p>
<p><strong>See also:</strong><br />
<a href="http://getgreenshot.org/faq/where-does-greenshot-store-its-configuration-settings/">Where does Greenshot store its configuration settings?</a><br />
<a href="http://getgreenshot.org/faq/what-is-the-best-way-to-control-greenshots-configuration-at-install-time/">What is the best way to control Greenshot’s configuration at install time?</a><br />
<a href="/faq/is-greenshort-really-portable/">Is Greenshort really portable?</a></p>
