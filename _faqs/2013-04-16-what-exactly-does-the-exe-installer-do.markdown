---
layout: faq
status: publish
published: true
title: What exactly does the exe installer do?
tags: []

---

The Exe Installer does the following things:

1. check pre-requisites (.NET 2.0 or higher)
2. display the license, ask where and what to install
3. uninstall any previously installed Greenshot
4. copy all the Greenshot files, including selected languages and plugins, to `Program files\Greenshot` (the actual path depends on 32 or 64 bit OS)
5. create shortcuts and a un-installer
6. use [ngen](http://en.wikipedia.org/wiki/Native_Image_Generator) on the installed application, which should improve the performance (this can take a while)
7. optionally create a startup registry entry
8. open our website with thank-you, and check if the installed version is the most recent.
9. start Greenshot (if selected)

Except the startup registry there are no registry entries changed.
Our installer is created with Inno Setup, and details on the possible installer parameters can be found here: [http://unattended.sourceforge.net/InnoSetup_Switches_ExitCodes.html](http://unattended.sourceforge.net/InnoSetup_Switches_ExitCodes.html)

**See also:**

[In which cases should I use the ZIP package instead of the installer?](/faq/in-which-cases-should-i-use-the-zip-package-instead-of-the-installer/)

[Are there any dependencies to other software / frameworks?](/faq/are-there-any-dependencies-to-other-software-frameworks/)
