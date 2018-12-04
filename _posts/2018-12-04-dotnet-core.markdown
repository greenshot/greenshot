---
layout: post
published: true
title: The future of Greenshot for Windows
tags:
- windows
---

What did we do the last year?
-----------------------------

Although we didn't release any new versions of Greenshot for some time now, a lot has happened!

What most people don't realize is that Greenshot in its current form would still work with .NET Framework 2.0, which is technology from 2002! With that in mind, you might understand that Greenshot desperately needs an overhaul! Not only we use old, maybe even obsolete, technologies but most parts of the program grew into something which is hard to maintain and it didn't really invite other developers to assist us.

Last year I started updating Greenshot so it works on and uses the most recent version of the .NET Framework. Using current technologies help to reduce the amount of code, get fixes a lot quicker and have new features available. To make this all possible I created a couple of new open source projects and moved generic parts of Greenshot in there:
* [Dapplo.Addons](https://github.com/dapplo/Dapplo.Addons) provides an add-on framework, based on [Autofac](https://github.com/autofac/Autofac), which makes it possible to extend Greenshot
* [Dapplo.CaliburnMicro](https://github.com/dapplo/Dapplo.CaliburnMicro) provides a composition & MVVM implementation using [CaliburnMicro](https://caliburnmicro.com/) and bases on Dapplo.Addons.
* [Dapplo.Config](https://github.com/dapplo/Dapplo.Config) Dapplo.Config provides the logic for handling the .ini file and also provides translations.
* [Dapplo.Confluence](https://github.com/dapplo/Dapplo.Confluence) provides an .NET API for accessing Atlassian Confluence
* [Dapplo.HttpExtensions](https://github.com/dapplo/Dapplo.HttpExtensions) providing logic which is used for the cloud services like box, dropbox etc.
* [Dapplo.Jira](https://github.com/dapplo/Dapplo.Jira) Dapplo.Jira provides an .NET API for accessing Atlassian Jira
* [Dapplo.Log](https://github.com/dapplo/Dapplo.Log) provides a simple logger for applications, this was build out of necessity and I hope to be able to delete this soon!
* [Dapplo.Windows](https://github.com/dapplo/Dapplo.Windows) provides the low level APIs for Windows (Win32), which contain the logic to locate the windows and other information which Greenshot needs.

These projects help to build a base for any .NET Windows application, and allows us to quickly add new features and fix existing. After going through Greenshot with a vacuum cleaner and a high pressure washer it should now also be easier for new developers to get acquainted to Greenshot and quickly learn to add new functionality. As soon as things get stable, there will be some documentation about writing your own add-ons.


Enter dotnet core 3.0
---------------------

Although the dotnet core technology, [version 1.0 saw the light in June 2016](https://en.wikipedia.org/wiki/.NET_Core), is pretty much platform agnostic like Java, it has not been possible to use existing Windows UI technologies with it. In May 2018, while I was working hard on getting things working with the latest .NET Framework version, Microsoft presented an interesting announcement during Build 2018 which you can read about [here](https://blogs.msdn.microsoft.com/dotnet/2018/05/07/net-core-3-and-support-for-windows-desktop-applications/) or watch in the video [here](https://www.youtube.com/watch?v=spgI12ZEBcs).

The announcement was just what I was waiting for! Microsoft demonstrated that with dotnet core 3.0 it will be possible to use the UI technologies that Greenshot is already using! Why is this so exiting? Although most reasons are explained in a second blog about dotnet core 3.0 which can be read [here](https://blogs.msdn.microsoft.com/dotnet/2018/10/04/update-on-net-core-3-0-and-net-framework-4-8/) I will try to translate this for our users.

One of the challenges with Greenshot was picking the version of the .NET Framework to run on, actually we were way to conservative and this limited our possibilities. The problem with the .NET Framework is that there can be only one version installed on a Windows PC! So if we want to use the newest version, we force a lot of people to update and a lot of companies might not be able to do so without extensively testing their other applications. The Greenshot installer needed to start the .NET Framework installer if this wasn't available, managing this added another complexity. With dotnet core this is a thing of the past, every application __can__ provide their own version of dotnet core and multiple versions run side by side!

Although Greenshot is open source it currently needs the .NET Framework to run, but this isn't open source! The [.NET Foundation](https://dotnetfoundation.org), which is an independent organization which supports many open source .NET [projects](https://dotnetfoundation.org/projects), is also taking good care of dotnet core. As soon as Greenshot is released for dotnet core, the complete stack (well ~99%) is running with open source technologies!

Another interesting fact is that the .NET Framework is used by billions of applications, this makes it __very__ hard to maintain backwards compatibility while adding new features or fixes. With dotnet core, which doesn't have all the legacy applications, a different approach is possible. By releasing more often and not having the need to support years of backwards compatibility, it's finally possible to introduce new features and maybe sometimes even make breaking changes. One thing where this shows is that dotnet core has many performance enhancements, which will also be noticeable in Greenshot.


Greenshot goes dotnet core 3.0
------------------------------

After the first major steps towards a new Greenshot, which I described before, I had the wish "making Greenshot dotnet core compatible". In May 2018 there wasn't much information on the topic so I tried to reach out to Microsoft. In June 2018 I managed to get a contact interested in working together, and we decided to try to use Greenshot as an early adopter of dotnet core 3.0.

I had the great pleasure to talk to many fine Microsoft employees about how and what of the .NET Framework is used by Greenshot, and what challenges we face providing Greenshot to our users. We discussed many use cases, some of them are probably still being worked on. At the same time I started the first work of making all the non UI parts dotnet core or netstandard compatible. And then in September 2018 it finally happened, the first alpha/preview build of dotnet core 3.0 was available.

With this available I worked for the last 3 months to get the whole of Greenshot and the backing projects running with dotnet core 3.0, which while it being very early would have been quite hard, so I am really grateful for the support I got from my contacts. While it was a _lot_ of work to make Greenshot dotnet core compatible, the UI part of the application actually was a case of "it just works", which is really amazing! And when one or the other bug showed up, the turn around time for fixes are usually one or two days.

At the time I am writing this the current state is that Greenshot is running on .NET Framework 4.7.1 and dotnet core 3.0 __side by side__! Having things work side by side is very convenient, we can stay on the "stable" .NET Framework and release from there, but as soon as dotnet core 3.0 is available for the general audience we can also release for this. I would say that the dotnet core implementation has around 95% of the functionality which .NET Framework has, and the only reason for this is time. There are __currently__ no show stoppers why the dotnet core 3.0 version of Greenshot would have less functionality, in fact I expect that it will be the other way around. 

I expect that the first release candidate of dotnet core 3.0 is available before I finish all the open ends on Greenshot, but we might be able to start releasing alpha builds at the end of the first quarter of 2019. Around that time I will also write a blog post about the coming features, and give a bit more information on the release timeline.


P.S.
I again want to thank the people at Microsoft who supported me and Greenshot. Thank you Rich, Daniel and all your colleagues, especially for staying professional while handling my annoying questions. I hope to be able to work with you again in the near future!


*What is left for now: we wish all our users happy times and enjoy the coming holidays!* <br/>
*Robin and the rest of the Greenshot team!*
