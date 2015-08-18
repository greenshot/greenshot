---
layout: post
status: publish
published: true
title: How to send an image to Skype using command line arguments
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 792
wordpress_url: http://getgreenshot.org/?p=792
date: !binary |-
  MjAxMy0wMi0xNyAxOTowNjowNiArMDEwMA==
date_gmt: !binary |-
  MjAxMy0wMi0xNyAxNzowNjowNiArMDEwMA==
categories:
- How-to
tags:
- skype
- tools
- plugins
comments: []
---
<p><a href="/2013/02/17/how-to-send-an-image-to-skype-using-command-line-arguments/2-select-skype-contact-2/" rel="attachment wp-att-849" title="Selecting the Skype recipient"><img src="/assets/wp-content/uploads/2013/02/2-select-skype-contact-300x230.png" alt="2-select-skype-contact" width="300" height="230" class="alignleft size-medium wp-image-849"  alt="Selecting the Skype recipient" /></a>You may have read our recent blog article about <a href="/2013/01/28/how-to-use-the-external-command-plugin-to-send-screenshots-to-other-applications/" title="How to use the External Command Plugin to send screenshots to other applications">how to connect Greenshot to other desktop applications using the external command plugin</a>.</p>
<p>For most popular software, available command line parameters are well documented, so one can easily find out whether and how it is possible to send an image to it via command line arguments.</p>
<p>In the case of Skype, it takes a bit of investigation to find that out - the <a href="https://support.skype.com/en/faq/FA171/can-i-run-skype-for-windows-desktop-from-the-command-line" rel="nofollow">list of command line arguments published by Skype</a> is very short, and there is no option listed for sending files to Skype.</p>
<p>However, after having a look at Skype's context menu integration (when you right click a file in Windows explorer, there is a Skype entry in the "Send to" sub menu) I found out that there is a undocumented command line parameter called <code>SENDTO</code>.</p>
<p>So, actually it <strong>is</strong> possible to send a file to Skype via command line, and it is as easy as that:<br />
<code>"C:\Program Files (x86)\Skype\Phone\Skype.exe" /sendto: "C:\path\to\image.jpg"</code><br />
(of course, the path could be different on your computer)</p>
<p><a href="/assets/wp-content/uploads/2013/02/1-configure-skype-command.png" title="Configuring external command for Skype"><img src="/assets/wp-content/uploads/2013/02/1-configure-skype-command-300x173.png" alt="Configuring external command for Skype" width="300" height="173" class="alignright size-medium wp-image-851" /></a> That is good news for us, because if it is possible using command line arguments, of course it is easily possible to do so with Greenshot's external command plugin as well.<br />
It is easy to configure:</p>
<ul>
<li>Right click the Greenshot icon in your systray to bring up Greenshot's context menu</li>
<li>Choose the <a href="/2013/01/28/how-to-use-the-external-command-plugin-to-send-screenshots-to-other-applications/" title="How to use the External Command Plugin to send screenshots to other applications">Configure external commands</a> option</li>
<li>Click the "New" button and fill in the info we have just found out:<br><br />
<strong>Name:</strong> <code>Skype</code><br><br />
<strong>Command:</strong> <code>C:\Program Files (x86)\Skype\Phone\Skype.exe</code> (the path might be different on your computer)<br><br />
<strong>Argument:</strong> <code>/sendto: "{0}"</code></li>
<li>Click "Ok" to save the Skype command configuration</li>
<li>That's it: after your next screenshot, the Skype destination will be available in the destination picker or the image editor</li>
</ul>
