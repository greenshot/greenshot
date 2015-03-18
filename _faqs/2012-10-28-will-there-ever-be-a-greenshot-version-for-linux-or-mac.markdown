---
layout: faq
status: publish
published: true
title: Will there ever be a Greenshot version for Linux or Mac?
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 604
wordpress_url: http://getgreenshot.org/?post_type=faq&#038;p=604
date: !binary |-
  MjAxMi0xMC0yOCAyMDoyMDowOSArMDEwMA==
date_gmt: !binary |-
  MjAxMi0xMC0yOCAxODoyMDowOSArMDEwMA==
categories: []
tags: []
comments: []
---
<p>Probably not, at least not in the near future. Greenshot is written with the Microsoft .NET framework, which does not run on Linux or iOS systems. Of course there is <a href="http://www.mono-project.com/" title="mono">mono</a> which allows runnning .NET apps on operation systems other than Windows, however it does neither support Windows Forms nor WPF, which would make it impossible to reuse Greenshot's existing code base. As we would rather not maintain multiple code bases, there are currently no plans to create a Greenshot version for Linux.</p>
