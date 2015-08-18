---
layout: faq
status: publish
published: true
title: Greenshot uses x MB of my RAM, why is that?
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 817
wordpress_url: http://getgreenshot.org/?post_type=faq&#038;p=817
date: !binary |-
  MjAxMy0wMi0xOSAyMToxNzoxNSArMDEwMA==
date_gmt: !binary |-
  MjAxMy0wMi0xOSAxOToxNzoxNSArMDEwMA==
categories: []
tags: []
comments: []
---
<p>Before talking about memory usage, let's make sure we are talking the same language: there are some points to notice, and a lot of things that are easily misunderstood.</p>
<ol>
<li>Which Greenshot version are you using? If it is not the latest stable release, please <a href="/downloads/" title="Downloads">upgrade</a>. Older versions had some memory issues, which we have fixed with version 1.0.</li>
<li>Which tool are you using to measure the memory usage of a program? If it is task manager, you should be aware that the displayed usage is probably higher than the actual value. If memory is available, Windows usually reserves more for running processes than actually needed. You might want to read our blog post about <a href="/2010/07/24/a-few-words-on-memory-usage-or-working-set-vs-private-working-set/">working set vs. private working set</a> for more information.</li>
<li>In which workflow situation did you encounter the memory usage that you think is too high? Of course, the actual memory usage varies depending on many factors, but when you start Greenshot, the memory usage should be somewhat consistent. When you start to work with it, of course it will go up, the bigger your screenshot, the more memory is needed to process it. After finishing your work and closing image editor windows, the memory usage should go down again. But this does not necessarily happen right away! Even though we mark certain objects as disposable (i.e. no longer needed), the .NET framework and/or Windows may decide to keep them in memory a bit longer for performance reasons.</li>
<li>If you want Greenshot to try to free your RAM from these objects earlier, there is an option in Greenshot's expert settings tab called "Minimize memory footprint". Be aware that checking it might result in loss of performance; that is why we advice not to use it and leave garbage collection (that's what nerds call cleaning up unneeded stuff from memory) to the .NET framework.</li>
<li>Speaking of .NET: it is well known that applications developed on top of the .NET framework often need some extra memory, this is a trade-off one has to consider. As always, there are pros and contras.</li>
</ol>
<p>By now, you probably have an idea that we have had our share of investigation regarding memory usage, we also have optmized a lot. And you can be sure that we are constantly having an eye on it, looking out for possible room for further improvement, keeping the RAM in mind while changing and adding the code. So, general feature requests like "reduce memory footprint" are not likely to change our way of working, since we care about this anyway :)<br />
However, if you have strong indication that Greenshot does not free memory permanently, you should tell us more details about the circumstances. If you encounter a situation which lets memory usage grow more and more after repeatedly doing the same thing, maybe you have actually found a memory leak we are not yet aware of. In these cases, please <a href="getgreenshot.org/tickets/">let us know</a>, including <a href="/2013/02/07/constructive-feedback-is-always-welcome/">information about your system and an exact description how to reproduce the behavior</a>. We'll have a look at it.</li>
</ol>
