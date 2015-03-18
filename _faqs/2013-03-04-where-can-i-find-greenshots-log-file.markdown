---
layout: faq
status: publish
published: true
title: Where can I find Greenshot's log file?
# override permalink for to keep old URLs intact
permalink: /faq/where-can-i-find-greenshots-log-file/
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 858
wordpress_url: http://getgreenshot.org/?post_type=faq&#038;p=858
date: !binary |-
  MjAxMy0wMy0wNCAyMjozMTo1MSArMDEwMA==
date_gmt: !binary |-
  MjAxMy0wMy0wNCAyMDozMTo1MSArMDEwMA==
categories: []
tags: []
comments: [] 
---
<p>In case you encounter things not working as expected in Greenshot, the log file might help use to help you: it hopefully contains some information on what went wrong, and maybe even why.<br />
Greenshot's log file is called Greenshot.log. Depending on which version of Greenshot you are using on which version of Windows, the log file can be found in one of the following directories:</p>
<p><strong>If you are using the installer version of Greenshot</strong>, the file is stored in your (local) application data folder, which is a path like</p>
<ul>
<li><code>C:\Users\%USERNAME%\AppData\Local\Greenshot\</code> in Windows Vista and newer version or </li>
<li><code>C:\Documents and Settings\%USERNAME%\Local Settings\Application Data\Greenshot\</code> in Windows XP</li>
</ul>
<p><strong>If you are using the ZIP version of Greenshot</strong>, the file is stored within the directory where you unzipped Greenshot to. This allows you to put Greenshot e.g. on a memory stick and use it as a portable application.</p>
<p>Shortcut: if you want to have a look at the log file quickly, you can right-click Greenshot's systray icon, select "About Greenshot" and hit the <kbd>L</kbd> key on your keyboard - Greenshot.log will open in your default text editor.</p>
<p>If you want to provide your log file along with a bug report, it might be a good idea to rename Greenshot.log to something else and reproduce the error again. A fresh Greenshot.log will be created, but its file size will be a lot smaller, since previous log messages are not included (which usually do not provide further information about the actual problem anyway).</p>
<p><strong>See also:</strong><br />
<a href="/faq/where-should-i-report-bugs/">Where should I report bugs?</a><br />
<a href="/faq/where-does-greenshot-store-its-configuration-settings/">Where does Greenshot store its configuration settings?</a><br />
<a href="/faq/is-greenshort-really-portable/">Is Greenshort really portable?</a></p>
