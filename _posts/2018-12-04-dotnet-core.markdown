---
layout: post
draft: true
published: true
title: The future of Greenshot for Windows
tags:
- windows
---

What did we do the last year?
-----------------------------

Although we didn't release any new versions of Greenshot for Windows, we didn't sit still and actually a lot has happened!

What most people don't realize is that Greenshot in its current form would still work with .NET Framework 2.0, which is technology from 2002! With that fact, you might understand that Greenshot desperately needs an overhaul! Not only in the used technology, but most parts of the program grew into something which is hard to maintain and it didn't really invite other developers to assist us.

Last year I started updating Greenshot so it works and uses the most recent version of the .NET Framework. This enables us to use current technologies to reduce the amount of code, get fixes a lot quicker and add new features. To make this all possible I created a couple of new open source projects and moved some parts of Greenshot in there:
* [Dapplo.Addons](https://github.com/dapplo/Dapplo.Addons) provides an add-on framework, based on [Autofac](https://github.com/autofac/Autofac), which makes it possible to extend Greenshot
* [Dapplo.CaliburnMicro](https://github.com/dapplo/Dapplo.CaliburnMicro) Dapplo.CaliburnMicro provides a composition & MVVM implementation, using CaliburnMicro, and bases on Dapplo.Addons.
* [Dapplo.Confluence](https://github.com/dapplo/Dapplo.Confluence) provides an .NET API for accessing Atlassian Confluence
* [Dapplo.Confluence](https://github.com/dapplo/Dapplo.Confluence) Dapplo.Config provides the logic for handling the .ini file and also provides translations.
* [Dapplo.HttpExtensions](https://github.com/dapplo/Dapplo.HttpExtensions) providing logic which is used for the cloud services like box, dropbox etc.
* [Dapplo.Jira](https://github.com/dapplo/Dapplo.Jira) Dapplo.Jira provides an .NET API for accessing Atlassian Jira
* [Dapplo.Log](https://github.com/dapplo/Dapplo.Log) provides a simple logger for applications, this was build out of necessity and I hope to be able to delete this soon!
* [Dapplo.Windows](https://github.com/dapplo/Dapplo.Windows) provides the low level APIs for Windows (Win32), which contain the logic to locate the windows and other information which Greenshot needs.

These projects build a base upon which we can quickly add new features, and fix existing. After going through Greenshot with a vacuum cleaner and a high pressure washer it should be easier for new developers to get acquainted to Greenshot and add new functionality.


Greenshot goes dotnet core!
---------------------------

Although Microsoft dotnet core technology, which saw the light in June 2016, is platform agnostic, it has not been possible to use existing UI technologies in it. In May 2018, while working hard on getting things working on the latest .NET Framework version, Microsoft had an interesting announcement on Build 2018 which you can find [here](https://blogs.msdn.microsoft.com/dotnet/2018/05/07/net-core-3-and-support-for-windows-desktop-applications/) and a video [here](https://www.youtube.com/watch?v=spgI12ZEBcs).

The announcement was just what I was waiting for! Microsoft demonstrated that with dotnet core 3.0 it will be possible to use the same UI technologies that Greenshot is currently using! Why is this so exiting? Although most reasons are explained in a second blog about dotnet core 3.0 which can be found [here](https://blogs.msdn.microsoft.com/dotnet/2018/10/04/update-on-net-core-3-0-and-net-framework-4-8/) I will try to translate this for our users.

One of the challenges with Greenshot was picking the version of the .NET Framework to run on, we were very conservative. The problem with the .NET Framework is that there is only one version on a Windows PC! So if we want to use the newest version, we force a lot of people to update and a lot of companies might not be able to do so with extensive testing their other applications. With dotnet core 3.0 this should be a thing of the past, we can provide our own version of dotnet core and other applications can use theirs side by side!

Although Greenshot is open source it needed the .NET Framework to run, but this isn't open source! Now with dotnet core which is maintained by the [.NET Foundation](https://dotnetfoundation.org), which is an independent organization which helps many open source .NET [projects](https://dotnetfoundation.org/projects), and one of them is dotnet core! When Greenshot can run on dotnet core, we would have the complete stack (well ~99%) run on Open Source technologies!

Another interesting fact is that the .NET Framework is used by billions of applications, this makes it __very__ hard for Microsoft to maintain backwards compatibility while adding new features. With dotnet core, which is written almost from scratch, a different approach is possible. By releasing more often and not having years of backwards compatibility, it's finally possible to introduce new features and maybe sometimes make breaking changes. One thing where this shows is that dotnet core has many performance enhancements, which will also be noticeable in Greenshot.


How and when
------------

After the first major steps towards a new Greenshot, which I described before, I started the next journey "making Greenshot dotnet core compatible". In May 2018 there wasn't much information on the topic so I tried to reach out to Microsoft. In June 2018 I managed to get a contact interested in working together, and we decided to try to use Greenshot as an early adopter of dotnet core 3.0!

I had the pleasure to talk directly to many people at Microsoft about what of the .NET Framework Greenshot uses, and what challenges we have with providing Greenshot to our users. We discussed many uses cases, some of them are still being worked on. In the mean time I started the first work of making everything, wherever possible, dotnet core or netstandard compatible. And then in September 2018 it finally happened, the first alpha/preview build of dotnet core 3.0 was available.

With this available I worked for the last 3 months to get the whole of Greenshot running with dotnet core 3.0, without having such great contacts at Microsoft this would have been a lot harder, so I am really grateful for their support. While it was a _lot_ of work to make Greenshot dotnet core compatible, the UI part of the application actually was a case of "it just works", which is quite an amazing thing!

While writing this it's the first week of December 2018, the current state is that Greenshot is running on .NET Framework 4.7.1 and dotnet core 3.0 side by side! I would say that the dotnet core implementation has around 95% of the functionality which .NET Framework has, and this is mainly due to missing time as there are currently no show stoppers. Having things work side by side is very convenient, we can stay on the "stable" .NET Framework and release from there, but as soon as dotnet core 3.0 is available for the general audience we can also release for this.

Currently I expect that dotnet core 3.0 is quicker to release than I finish all the open ends on Greenshot, but we might be able to start releasing alpha builds at the end of the first quarter of 2019.


*Best wishes from Robin and the rest of the Greenshot team! We hope to have more news in the near future!*