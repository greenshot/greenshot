Greenshot - a free screenshot tool optimized for productivity
=============================================================

Welcome to the source repository for Greenshot

What is Greenshot?
------------------

Greenshot is a light-weight screenshot software tool for Windows with the following key features:

* Quickly create screenshots of a selected region, window or fullscreen; you can even capture complete (scrolling) web pages from Internet Explorer.
* Easily annotate, highlight or obfuscate parts of the screenshot.
* Export the screenshot in various ways: save to file, send to printer, copy to clipboard, attach to e-mail, send Office programs or upload to photo sites like Flickr or Picasa, and others.
and a lot more options simplyfying creation of and work with screenshots every day.

Being easy to understand and configurable, Greenshot is an efficient tool for project managers, software developers, technical writers, testers and anyone else creating screenshots.


[If you find that Greenshot saves you a lot of time and/or money, you are very welcome to support the development of this screenshot software.](http://getgreenshot.org/support/)


About this repository
---------------------
This repository is for Greenshot 1.3.x

For users the major changes since 1.2.x are:
* A newer and more modern configuration UI
* Due to the update of .NET 4.5 a lot of bugs are solved
* Added Windows 10 destinations, OCR & share
* Better DPI support
* Faster development
* Bug fixes


For developers, the major changes since 1.2.x are:
* Updated to .NET 4.5.x
* Moved logging from log4net to [Dapplo.Log](https://github.com/dapplo/Dapplo.Log) which is a very simple logger
* Using Dependency Injection (Inversion of Control), via [Dapplo.Addons](https://github.com/dapplo/Dapplo.Addons) which bases upon MEF.
* Add-ins (formerly plug-ins) can just place attributes on classes to be loaded and injected.
* Started using WPF via MVVM, provided by Caliburn.Micro via [Dapplo.CaliburnMicro](https://github.com/dapplo/Dapplo.CaliburnMicro)
* Added [Dapplo.Config](https://github.com/dapplo/Dapplo.Config) which provides language & configuration support.
* Added a configuration UI, which is build together via composition. Meaning add-ins just need to implement the correct class and use the correct attributes to be visible inside the new configuration.
* Using [Dapplo.HttpExtensions](https://github.com/dapplo/Dapplo.HttpExtensions) as the default HTTP client library, which should make it easier to use cloud services.
* Fody-Costura is supported for add-ins, which make it possible to simply embed the needed dependencies as dlls.
* Moved most native windows code to a separate project called [Dapplo.Windows](https://github.com/dapplo/Dapplo.Windows) which makes it easier to test
* Moved graphics code to a separate project, where benchmark tests are possible.

Currently known errors:
* The old .greenshot files cannot be loaded
* ...
