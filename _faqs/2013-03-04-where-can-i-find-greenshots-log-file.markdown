---
layout: faq
status: publish
published: true
title: Where can I find Greenshot's log file?
# override permalink for to keep old URLs intact
permalink: /faq/where-can-i-find-greenshots-log-file/
tags: []
 
---
In case you encounter things not working as expected in Greenshot, the log file might help you: it hopefully contains some information on what went wrong, and maybe even why.
Greenshot's log file is called Greenshot.log. Depending on which version of Greenshot you are using on which version of Windows, the log file can be found in one of the following directories:

**If you are using the installer version of Greenshot**, the file is stored in your (local) application data folder, which is a path like

* `C:\Users\%USERNAME%\AppData\Local\Greenshot\` in Windows Vista and newer version or
* `C:\Documents and Settings\%USERNAME%\Local Settings\Application Data\Greenshot\` in Windows XP


**If you are using the ZIP version of Greenshot**, the file is stored within the directory where you unzipped Greenshot to. This allows you to put Greenshot e.g. on a memory stick and use it as a portable application.

Shortcut: if you want to have a look at the log file quickly, you can right-click Greenshot's systray icon, select "About Greenshot" and hit the <kbd>L</kbd> key on your keyboard - Greenshot.log will open in your default text editor.

If you want to provide your log file along with a bug report, it might be a good idea to rename Greenshot.log to something else and reproduce the error again. A fresh Greenshot.log will be created, but its file size will be a lot smaller, since previous log messages are not included (which usually do not provide further information about the actual problem anyway).

**See also:**

* [How can I turn off logging?](/faq/how-can-i-turn-off-logging/)
* [How can I make Greenshot write debug information to its log file?](/faq/how-can-i-make-greenshot-write-debug-information-to-its-log-file/)
* [Where should I report bugs?](/faq/where-should-i-report-bugs/)
* [Where does Greenshot store its configuration settings?](/faq/where-does-greenshot-store-its-configuration-settings/)
* [Is Greenshort really portable?](/faq/is-greenshort-really-portable/)
