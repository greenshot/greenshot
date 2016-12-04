Developing with and for Greenshot
=================================

This documentation describes how one can develop addons for Greenshot, or use parts of Greenshot in their own application.

A background on Greenshot will help you to understand why certain things work the way the do.


Dependencies & Technologies
---------------------------

Before we can describe how one can add functionality to, or use functionality from Greenshot you should first know about what projects Greenshot uses.

Internally Greenshot uses the following technologies:

1. Windows .NET Framework 4.5.x, anything before 4.5 would make things very complicated due to the lack of async / Task
2. Managed Extensions Framework (MEF) - Until Greenshot 1.2.8 the code was very much like a bole of spaghetti, entangled functionality with the ocasional meatbal. Due to the complexity of the code, it was decided to make everything more modular and move the parts with functionality which is not a code Greenshot feature into separate open source project with completely rewritten code. Although this is still work in progress, by making the code more modular things are getting more flexible and easier to maintain. Although there are many good IOC or dependency injection frameworks, we decided to work with the which comes with the .NET Framework itself.
MEF allows us to load in the dependencies and compose addons into our application, the addons can add functionality on many different places.
3. Windows Presentation Framework (WPF) for the UI
4. HttpClient for Http connections, but with the help of a 3rd party library mentioned below.


To be able to manage the code of 3rd parties, Greenshot uses (like every .NET application should) NuGet.
Greenshot builds on the following external (3rd) packages:

1. [Dapplo.Log](https://github.com/dapplo/Dapplo.Log) for logging, with this it's possible for the calling application to redirect the internal logging into their own logger.
2. [Dapplo.Addons](https://github.com/dapplo/Dapplo.Addons) is used to bootstrap MEF, load addons (dlls) and enable starting services and exporting functionality
3. [Dapplo.Utils](https://github.com/dapplo/Dapplo.Utils) has many utilities used throughout the code
4. [Dapplo.Config](https://github.com/dapplo/Dapplo.Config) is used for configuration and translations, it works by generating implementations of interfaces which will be filled from configuration or translation files.
	1. Dapplo.InterfaceImp is used for generating the implementation classes
5. [Dapplo.CaliburnMicro](https://github.com/dapplo/Dapplo.CaliburnMicro) is a project which helps to bootstrap [CaliburnMicro](http://caliburnmicro.com/) an package which helps to implement a MVVM UI and aditionally add some functionality, like authentication and things like a wizard etc.
   1. [Dapplo.CaliburnMicro.Metro](https://github.com/dapplo/Dapplo.CaliburnMicro.Metro) adds [MahApps.Metro](http://mahapps.com/), an UI design package which makes your desktop application look like a Store Application.
   2. [Dapplo.CaliburnMicroNotifyIconWpf](https://github.com/dapplo/Dapplo.CaliburnMicro.NotifyIconWpf) adds the [WPF NotifyIcon](http://www.hardcodet.net/wpf-notifyicon) package from Philipp Sumi and makes it possible to use this in a more MVVM way.
6. System.Reactive 
7. [Dapplo.HttpExtensions](https://github.com/dapplo/Dapplo.HttpExtensions) is used for all Http communication, like in the box/dropbox/flickr/imgur/photobucket/picasa addons. This makes sure that the configuration for firewalls, timeouts etc is available and used throughout the complete application. It makes access to Http services fairly simple, and still have the posibility to handle pretty much everything. There is build in support for OAuth, streaming progress, json, xml etc, and resources are automatically disposed. This package is used together with:
   1. [Dapplo.Jira](https://github.com/dapplo/Dapplo.Jira) for communication with an Atlassian Jira server
   2. [Dapplo.Confluence](https://github.com/dapplo/Dapplo.Confluence) for communication with an Atlassian Confluence server 
8. [Dapplo.Windows](https://github.com/dapplo/Dapplo.Windows) for User32 and Gdi32 functionality to control and read native windows.
9. MahApps.Metro.IconPacks for the icons throughout Greenshot, there are multiple packs in there. The icons in the package are scalable, and make sure they look sharp on every resolution.


Modules
-------

As explained Greenshot builds heavily on modules, to make it possible to add functionality you can use different ways.

1. Startup & shutdown logic can be add via the IStartupAction & IShutdownAction from Dapplo.Addons. You can use this to start services, or create exports which might depend on some logic.
2. Export via interfaces, this allows you to add functionality to different parts of Greenshot, which is automatically displayed or used.

Here are some of the available interfaces, with what they do. This can either be used to export functionality into Greenshot or use them directly:

1. ICaptureSource is used for functionality which creates a screenshot, imports files etc. Available sources are:
	1. ClipboardCaptureSource to get the current image from the clipboard
    2. ScreenCaptureSource to get a screenshot from the screen
    3. WindowCaptureSource to get a screenshot from a window
    4. CursorCaptureSource to get the current cursor
2. ICaptureProcess is used for functionality to modify / process a capture, like crop/edit. Another posibility would be adding meta-data to the capture, depending on e.g. a QR code. Available implementations are:
	1. CropCaptureProcessor will open a window to have the user do an interactive crop of the capture.
    2. EffectCaptureProcessor will apply one or more effects (IEffect) to the capture.
3. ICaptureDestination is used to export a capture to one or more services or formats. The following are already available:
	1. ClipboardCaptureDestination to export a capture to the clipboard
    2. FileCaptureDestination to write a capture into a file.
    3. ....
4. IEffect is the basic interface which implementation would apply an effect to a capture, available are:
	1. DropShadowEffect
    2. TornEdgeEffect
    3. GrayscaleEffect
    4. ....
5. ICaptureFlow combines the ICaptureSource, ICaptureProcessor and ICaptureDestination into one object, and allows you to make them "flow". Executing a flow will call the source to get a capture, process the capture and send it to a destination.
6. ICaptureContext is the interface for an object which is a context passed to all the ICaptureSource, ICaptureProcessor and ICaptureDestinations. This currently only contains a capture.
7. ICapture is the interface for an object containing a capture
8. ICaptureDetails contains the meta-data for a capture


An example of using some of the API can be seen [here](https://github.com/greenshot/greenshot/blob/develop/Greenshot.Wpf.QuickTest/MainWindow.xaml.cs)

Editor
------

Using the Greenshot editor in your own application is very much possible, you currently will need at least an ICapture and need to supply the needed configuration & translations.
Here this will be described soon....
