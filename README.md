Greenshot - a free screenshot tool optimized for productivity
=============================================================

Welcome to the source repository for Greenshot.

What is Greenshot?
------------------

Greenshot is a light-weight screenshot software tool for Windows with the following key features:

* Quickly create screenshots of a selected region, window or fullscreen.
* Easily annotate, highlight or obfuscate parts of the screenshot.
* Export the screenshot in various ways: save to file, send to printer, copy to clipboard, attach to e-mail, send Office programs or upload to photo sites like Flickr or Picasa, and others.
and a lot more options simplifying creation of and work with screenshots every day.

Being easy to understand and configurable, Greenshot is an efficient tool for project managers, software developers, technical writers, testers and anyone else creating screenshots.


[If you find that Greenshot saves you a lot of time and/or money, you are very welcome to support the development of this screenshot software.](https://getgreenshot.org/support/)

Trademark and Logo Usage Policy
-------------------------------

The Greenshot logo and trademark are the property of the Greenshot development team. Unauthorized use of the logo and trademark is generally prohibited. However, we allow the use of the Greenshot name and logo in the following contexts:

* In blog posts, articles, or reviews that discuss or promote the Greenshot, provided that the usage is fair and does not imply endorsement by Greenshot.
* In educational materials or presentations that accurately represent the project.

Please refrain from using the Greenshot logo and trademark in any promotional materials, products, or in a manner that may cause confusion or imply endorsement without prior written permission.

If you have any questions or wish to seek permission for other uses, please contact us.

Thank you for your understanding and cooperation.


About this repository
---------------------
This is the ongoing development branch for future Greenshot releases. 

Releases
--------

You can find a list of all releases (stable and unstable) in the [Github releases](https://github.com/greenshot/greenshot/releases) or in the [version history on our website](https://getgreenshot.org/version-history/).
The [downloads page on our website](https://getgreenshot.org/downloads/) always links to the latest stable release.

Getting Started for Developers:
-------------------------------

These instructions are made to assist developers in getting started with Greenshot so they can contribute to the repository. Please verify system prerequisites are met before trying to build.

IDE System Requirements:
------------------------

* Windows OS environment
* Greenshot is build using (as of this writing) .net Framework 4.8.0 This means any version between .net Framework 4.8.0-4.8.1 will suffice. 
* Visual Studio 2022 or newer (works fine with 2026)
* .NET SDK 9.0.311 also for building (supported by VS 2022)

Build Instructions:
-------------------

* Open Visual Studio 2022 or 2026
* Clone GitHub Repository using Visual Studio, using the link in the green code button above. Alternatively, you can download the repository to your machine and open the solution file located in /src/Greenshot.sln.
* Choose Build->Build Solution in Visual Studio to build binaries.
* Verify all components are built successfully.
* You are ready to start contributing to Greenshot.

Solution configurations:
------------------------

| Configuration | Builds | Output |
|---|---|---|
| Debug (the default) | Everything but the installer project: Greenshot, the plugins, greenshot-cli.exe and greenshot-proxy, greenshot-mcp, the tests | `src\Greenshot\bin\Debug` |
| Release | Everything, plus the release files: checksum.SHA256, the SBOM, Greenshot Light, the installers and greenshot-mcp | `src\Greenshot\bin\Release` and `installer\` |
| Debug Light | Greenshot Light only: Greenshot, Greenshot.Base, Greenshot.Editor (and the build tasks) | `src\Greenshot\bin\Debug-Light` |
| Release Light | The same, optimized | `src\Greenshot\bin\Release-Light` |

Debug never makes checksums, an SBOM or installers; only Release does.

Greenshot Light is the basics only: no plugins, no AI tools (greenshot-mcp) and no browser extension. That code is not in its Greenshot.exe at all: it is left out with `#if !GREENSHOT_LIGHT` and `<Compile Remove>` in Greenshot.csproj, and the build fails when one of those types is still in the exe. Pick "Debug Light" in the solution configuration dropdown to run it with F5. On the command line, `dotnet build src\Greenshot\Greenshot.csproj -c DebugLight` (or `/p:GreenshotEdition=Light`) does the same.

The edition is in Greenshot.exe (`[AssemblyMetadata("GreenshotEdition", "Light")]` and the product name in its file properties); `Greenshot.Base.Core.EditionInfo` reads it, for the About window, the tray icon, the log and the bug reports. Another edition is another value of the `GreenshotEdition` property (see `src\Directory.Build.props`).

Adding images:
--------------

Images, icons and sounds for Windows Forms are embedded as plain files, not in .resx files: binary data in a .resx needs System.Resources.Extensions and its dependencies in the output, and the build fails when a .resx contains anything but strings.

* An image of the editor goes to `src\Greenshot.Editor\Resources\<control>.Image.png`, e.g. `btnSave.Image.png`. The wildcard `EmbeddedResource` in Greenshot.Editor.csproj (LogicalName `Greenshot.Editor.Forms.ImageEditorForm.%(Filename)`) picks it up, nothing else to add there. The icons of the tray menu work the same way with `src\Greenshot\Resources\Tray` (LogicalName `Greenshot.Shell.TrayMenu.%(Filename)`).
* Anything else: `<EmbeddedResource Include="..." LogicalName="<Namespace>.<Type>.<name>" />` in the project, the type being the one the resource belongs to.
* Load it with `EmbeddedResources.GetImage`, `GetIcon` or `GetBytes(typeof(<Type>), "<name>")`; the caller disposes what it gets.
* Never set an image with the Image property in the Windows Forms designer, it writes the image into the .resx. Assign it in the code of the form, e.g. in `ApplyImages()` of ImageEditorForm.cs.
* WPF is not affected: its images are `Resource` items with pack URIs, as before.

How to contribute:
------------------

* Create your own fork of the main repository
* Make desired changes (REMEMBER: keep commits small and concise, don't try to add multiple separate changes at once)
* Submit a pull request for review. Your request will be reviewed and either accepted, denied, or you may be asked to make revisions before your code is accepted.
