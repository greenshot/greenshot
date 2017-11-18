---
layout: post
status: publish
published: true
title: ! 'A few words on memory usage or: working set vs. private working set'
tags:
- windows
- memory
- ram
- working set
- performance
---
<p>Every once in a while, we read statements like "Woah, the task manager says my Greenshot installation takes x MB of RAM when it is idle! Why does Greenshot need so much memory?"</p>
<p>Actually, it does not. Please do not judge any software by what the task manager says. Of course, the task manager is not lying to you; but its presentation of facts is rather misleading.</p>
<p>The value pointed out as "Mem usage" is actually the size of a processes <em>working set</em>. A working set is not reserved for a single process. When analyzing the memory performance of a process using a tool like Process Explorer (or - with Windows Vista or 7 - changing the displayed columns in the task manager; see link below how this can be done) it is obvious that the working set size is split into two values, <em>WS Private</em> and <em>WS Shareable</em>. </p>
<p>[caption id="attachment_244" align="alignleft" width="212" caption="Example: Process Explorer distinguishing Private and Shareable Working Set"]<a href="/assets/wp-content/uploads/2010/07/working_set.jpg"><img src="/assets/wp-content/uploads/2010/07/working_set.jpg" alt="Process Explorer screenshot" title="Private vs. Shareable Working Set" width="212" height="153" class="size-full wp-image-244" /></a>[/caption]</p>
<p>These terms are pretty much self-explanatory: the private part of a working set is how much the process actually needs, it cannot be used by other processes. The shareable part is most often the bigger part of a working set. What is in there is free for use by any other application running, which is most obvious when running multiple .NET applications. Launch Paint.NET next to Greenshot and you will notice <em>WS shared</em> increasing. Launch SharpDevelop and it will increase even more. Loaded resources are shared between applications, which is a good thing.<br />
If you are curious about the shared working set: cybernetnews has posted a nice article on <a href="http://cybernetnews.com/cybernotes-windows-memory-usage-explained/">Windows memory usage</a>.</p>
<p>Another thing: as long as RAM is not scarce, used memory is not wasted memory. Actually, unused memory is literally of no use. For this reason Windows may be more generous in spending your RAM than actually demanded. So if the process needs some more at a later point, it is already there and can be accessed quickly (i.e. more performant). If other processes demand it, it can still be reallocated.<br />
Just imagine three persons sitting on a sofa. No matter how big the sofa is, they will probably spread along the whole length instead of stacking up on one side of it, allowing each of them some extra space to sit comfortably and uncramped, leaving room to move. But when needed, they can still move together so that other persons can find a seat, too. Now there are maybe five or six persons sitting of the sofa, which still works out. But reaching out for ones drink on the table is a little bit more cumbersome, every move is a bit less effiecient now.</p>
<p>That said, be sure that we are always having an eye on memory usage, we definitely have no intent to waste memory for whatever reason. We can do our best to free resources as soon as they are no longer needed, but we also have to rely that Windows and .NET do a good job when it comes to memory management.</p>
