Greenshot - a free screenshot tool optimized for productivity
=============================================================
[![Backers on Open Collective](https://opencollective.com/greenshot/backers/badge.svg)](#backers)
 [![Sponsors on Open Collective](https://opencollective.com/greenshot/sponsors/badge.svg)](#sponsors) 
 
Welcome to the source repository for Greenshot

What is Greenshot?
------------------

Greenshot is an open source, light-weight screenshot software tool for Windows with the following key features:

* Quickly create screenshots of a selected region, window or fullscreen; you can even capture complete (scrolling) web pages from Internet Explorer.
* Easily annotate, highlight or obfuscate parts of the screenshot.
* Export the screenshot in various ways: save to file, send to printer, copy to clipboard, attach to e-mail, send Office programs or upload to photo sites like Flickr or Picasa, and others.
and a lot more options simplifying creation of and work with screenshots every day.

Being easy to understand and configurable, Greenshot is an efficient tool for project managers, software developers, technical writers, testers and anyone else creating screenshots.


[If you find that Greenshot saves you a lot of time and/or money, you are very welcome to support the development of this screenshot software.](http://getgreenshot.org/support/)


About this repository
---------------------
This repository is work in progress for the next Greenshot (2.0?).


Quick start for developers
----------------------------
* Download the latest (!!!) dotnet core SDK from here: https://github.com/dotnet/core-sdk ([quick-link to download](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-win-x64.exe))
  * Make sure you only have the latest dotnet core 3.0 installed!
* Make sure you have the latest Visual Studio 2019, and enable "Use previews of the dotnet core SDK", as is shown here: https://stackoverflow.com/a/55033763
* Clone the [repository](https://github.com/greenshot/greenshot/tree/develop), branch develop
* Open the solution from the src directory in Visual Studio
* Rebuild and start...

If you can't use Visual Studio 2019 (preview) try the following:
* Open a powershell / shell in the directory where you cloned this repo
* run dotnet build src/Greenshot.sln

For users the major changes since 1.2.x are:
* dotnet core 3.0 support (why, read here: https://blogs.msdn.microsoft.com/dotnet/2018/10/04/update-on-net-core-3-0-and-net-framework-4-8/ )
* A newer and more modern configuration UI, using MahApps.Metro
* Due to the update of .NET 2.0 to .NET 4.7.1 a lot of bugs are solved
* Added Windows 10 destinations, OCR & share
* Better DPI support
* Simplified code should make development easier and quicker
* Bug fixes


For developers, the major changes since 1.2.x are:
* Move to dotnet core 3.0
* A newer and more modern configuration UI, using MahApps.Metro
* Due to the update of .NET 2.0 to .NET 4.7.1 a lot of bugs are solved
* Added Windows 10 destinations, OCR & share
* Better DPI support
* Simplified code should make development easier and quicker
* Bug fixes


For developers, the major changes since 1.2.x are:
* Updated to .NET 4.7.1 and dotnet core 3.0 (multiple targets)
* Moved logging from log4net to [Dapplo.Log](https://github.com/dapplo/Dapplo.Log) which is a very simple logger (reviewing changing to Microsoft.Extensions.Logging)
* Using Dependency Injection (Inversion of Control) via [Dapplo.Addons](https://github.com/dapplo/Dapplo.Addons) which bases upon AutoFac.
* Using MVVM, provided by Caliburn.Micro via [Dapplo.CaliburnMicro](https://github.com/dapplo/Dapplo.CaliburnMicro)
* Added [Dapplo.Config](https://github.com/dapplo/Dapplo.Config) which provides language & configuration support.
* Added a configuration UI, which is build together via composition. Meaning add-ins just need to implement the correct class and use the correct attributes to be visible inside the new configuration.
* Using [Dapplo.HttpExtensions](https://github.com/dapplo/Dapplo.HttpExtensions) as the default HTTP client library, which should make it easier to use cloud services.
* Moved most native windows code to a separate project called [Dapplo.Windows](https://github.com/dapplo/Dapplo.Windows) which makes it easier to develop & test
* Moved graphics code to a separate project, where benchmark tests are possible.


Currently known errors:
* The old .greenshot files cannot be loaded
* Not all Addons are active, the way they are found needs to be fixed.
* Office Addon (if referenced) will not work on dotnet core 3.0 yet
* Windows 10 Addon (if referenced) will not work on dotnet core 3.0 yet
* MahApps.Metro doesn't support dotnet core 3.0 yet, working on it (first step ControlzEx: https://github.com/ControlzEx/ControlzEx/pull/66 )


## Contributors

This project exists thanks to all the people who contribute. [[Contribute](CONTRIBUTING.md)].
<a href="https://github.com/greenshot/greenshot/graphs/contributors"><img src="https://opencollective.com/greenshot/contributors.svg?width=890&button=false" /></a>


## Backers

Thank you to all our backers! 🙏 [[Become a backer](https://opencollective.com/greenshot#backer)]

<a href="https://opencollective.com/greenshot#backers" target="_blank"><img src="https://opencollective.com/greenshot/backers.svg?width=890"></a>


## Sponsors

Support this project by becoming a sponsor. Your logo will show up here with a link to your website. [[Become a sponsor](https://opencollective.com/greenshot#sponsor)]

<a href="https://opencollective.com/greenshot/sponsor/0/website" target="_blank"><img src="https://opencollective.com/greenshot/sponsor/0/avatar.svg"></a>
<a href="https://opencollective.com/greenshot/sponsor/1/website" target="_blank"><img src="https://opencollective.com/greenshot/sponsor/1/avatar.svg"></a>
<a href="https://opencollective.com/greenshot/sponsor/2/website" target="_blank"><img src="https://opencollective.com/greenshot/sponsor/2/avatar.svg"></a>
<a href="https://opencollective.com/greenshot/sponsor/3/website" target="_blank"><img src="https://opencollective.com/greenshot/sponsor/3/avatar.svg"></a>
<a href="https://opencollective.com/greenshot/sponsor/4/website" target="_blank"><img src="https://opencollective.com/greenshot/sponsor/4/avatar.svg"></a>
<a href="https://opencollective.com/greenshot/sponsor/5/website" target="_blank"><img src="https://opencollective.com/greenshot/sponsor/5/avatar.svg"></a>
<a href="https://opencollective.com/greenshot/sponsor/6/website" target="_blank"><img src="https://opencollective.com/greenshot/sponsor/6/avatar.svg"></a>
<a href="https://opencollective.com/greenshot/sponsor/7/website" target="_blank"><img src="https://opencollective.com/greenshot/sponsor/7/avatar.svg"></a>
<a href="https://opencollective.com/greenshot/sponsor/8/website" target="_blank"><img src="https://opencollective.com/greenshot/sponsor/8/avatar.svg"></a>
<a href="https://opencollective.com/greenshot/sponsor/9/website" target="_blank"><img src="https://opencollective.com/greenshot/sponsor/9/avatar.svg"></a>


