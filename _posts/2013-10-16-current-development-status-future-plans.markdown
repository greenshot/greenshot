---
layout: post
status: publish
published: true
title: Current Development Status and Plans for the Future
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 1001
wordpress_url: http://getgreenshot.org/?p=1001
date: !binary |-
  MjAxMy0xMC0xNiAyMTo1MTo1NiArMDIwMA==
date_gmt: !binary |-
  MjAxMy0xMC0xNiAxOTo1MTo1NiArMDIwMA==
categories:
- Announcements
tags:
- '1.1'
- '2.0'
- windows
- .net
comments: []
---
<p>So what have we been up to? You might have heard little of us, but this doesn't mean we were sleeping!</p>
<p>Of course we were working on Greenshot 1.1.6, we fixed some mean bugs and even managed to squeeze in a few small features! We will release the new version within the next few days, so stay tuned :)</p>
<p>But mainly we were busy with working on and planning for Greenshot 2.0! We need to make some decisions on how we continue the development, these decisions do not cover which features we are going to develop but rather are important infrastructure changes. When the development of Greenshot started in 2007, .NET Framework 2.0 was used and the target OS was Windows XP. Up till today we didn't change the .NET framework and still maintain code to support Windows XP, even though none of us has Windows XP anymore, which means that we cannot actually do the testing. You probably know that we are only a small development team, so we cannot support all Windows versions forever... Besides we are missing out on many features that Microsoft offers in newer versions of Windows and the .NET framework which we could use to improve on Greenshot. As .NET framework 2.0 and Windows XP both are getting end of lifed by Microsoft we will no longer be able to use and support these.</p>
<p>Based on these considerations, we decided two things:</p>
<ul>
<li>Greenshot 2.0 will be mainly a migration to the newer .NET Framework 4.X and we will remove all special support for Windows Vista and earlier.</li>
<li>Of course, Greenshot 1.1 will still be available for download. This version works fine on Windows XP and Windows Vista, and we will fix major bugs if they occur, this should be enough for those who bravely stick to the older versions of Windows.</li>
</ul>
<p>You might ask, "What benefit does this migration bring for me?" Well, we don't plan many new features for 2.0, but we will be able to enable some hidden and prepared features and improve a lot of the current. As Greenshot was built on a user interface technology that has gotten long in the tooth, adding new settings wasted loads of efforts, which sometimes meant that we didn't add them in the end. A lot of features, expert or new, were only available by changing the greenshot.ini manually. We are already building a new settings dialog where we can add them quickly and consistent, enhancing usability with an incremental search within the dialog. It is still work in progress, but here is a screenshot to give you an idea:</p>
<p><a href="http://getgreenshot.org/wp-content/uploads/2013/10/greenshot-settings-dialog1.png"><img src="http://getgreenshot.org/wp-content/uploads/2013/10/greenshot-settings-dialog1-300x277.png" alt="Greenshot 2.0 settings dialog (work in progress)" width="300" height="277" class="alignleft size-medium wp-image-1002" /></a></p>
<p>Finally, this should make it possible for us to improve the Greenshot experience, make the user interface feel more professional and finished.</p>
<p>One thing that does have to wait, is the editor. We have spent a lot of time developing it on the old framework, if we would wait with Greenshot 2.0 before this is migrated to the newer Framework you would have to wait for ages. We rather supply you with smaller treats, so you don't have to wait so long.</p>
<p>Some other things that we are working on, maybe not so important for most of our users, is the migration our source code from subversion to GIT. This should speed up some of our development and also enable an easier way of helping us out by developing your favorite features yourself. For those that can develop C#, you can create a fork of Greenshot in a few clicks and send us your changes by pull-request (similar to github).</p>
<p>But first of all, we will get Greenshot 1.1.6 ready for you - we will let you know when it is available for download.</p>
