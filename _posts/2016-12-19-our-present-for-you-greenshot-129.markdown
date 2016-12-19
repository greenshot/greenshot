---
layout: post
status: publish
published: true
title: ! 'Our present for you: Greenshot 1.2.9 brings a lot of improvements and fixes'
tags:
- 1.2
- bugfix
- release
---
Admittedly, we're a bit early with our gift. But as a piece of software cannot be wrapped and aligned beautifully with fancy boxes under the tree, we rather hand it over to you right now, so that you can start having fun with the new version before spending time with your beloved friends and family.

So, what's new in this version? More than 500 hours of work went into it - we have improved window screenshots in Windows 10 (which suffered a little cropping in prior versions), also there's an optimization to reduce the problems with uploading to Imgur, which some of you have encountered (actually caused by a per-user rate limiting applied by Imgur).

If you used to use Greenshot for uploading screenshots to Jira: good news, Greenshot's Jira plugin is now compatible with Jira 7, and a lot faster. It also got some convenient improvements, e.g. it remembers the last viewed issues. 

Especially business users might be happy to hear that Greenshot's installer is digitally signed as of now, so that you can recognize that you're actually installing the original version provided by us, and not some potentially malware-infested version from someone else.

(By the way, if you want to make sure you are using the original version, you should never download Greenshot from anywhere else than from our website. Just keep in mind: always get Greenshot from getgreenshot.org.)

You see, this release has things to offer for anyone - you can see the full changelog below. So just go ahead, <a href ="/downloads/">download</a> and "unwrap" our gift and have a great time.

Yours with best wishes,

the Greenshot team

    All details to our tickets can be found here: https://greenshot.atlassian.net
    
    1.2.9.103-e315179 RELEASE
    
    Fixed bugs:
    * BUG-1762 Dropshadow & tornedge prompts for settings every time
    * BUG-1812 Editor opens outside the screen
    * BUG-1876 Already running message, multi user environment
    * BUG-1884 OCR text has trailing blank spaces
    * BUG-1887 Editor hangs on exit - hang time proportional to number of objects
    * BUG-1890 Slight cropping around window on Windows 10
    * BUG-1892 Greenshot saves blank JPG file with reduce colors
    * BUG-1894 Imgur remote server error 429
    * BUG-1896 JIRA Plugin doesn't work with JIRA Server v7.0
    * BUG-1898 Specify GPLv3 in the license text
    * BUG-1908 External Command, add commands at startup
    * BUG-1918 Editor Speechbubble Artifacts when shadow and transparent
    * BUG-1933 Greenshot Installer sets bad registry key permission
    * BUG-1935 Delay when pasting and ShapeShifter from FlameFusion is running
    * BUG-1941 Error when creating a speech bubble in the editor
    * BUG-1945 Failure starting Greenshot at system startup
    * BUG-1949 Can't delete Imgur upload
    * BUG-1965 Activation border around window is visible in the capture
    * BUG-1991 Greenshot portable (PAF) uses wrong log configuration
    * BUG-1992 OutputFilePath setting is wrong config used on a different system
    * BUG-2011 Editor issues when the font is gone or broken
    * BUG-2043 Fixed an issue which occured after wakeup from sleep
    * BUG-2059 Reduce the update check interval
    * BUG-2070 Installing Greenshot deletes everything in the target directory
    * BUG-2071 Authentication not completing with Box.com
    
    Added features:
    * FEATURE-863 The start value of the counters can now be changed
    * FEATURE-916 Added icon save format (capture will be resized to 16x16, 32x32, 48x48 or 256x256)
    * FEATURE-919 Added a way to increase the editor canvas size (use Ctrl + / Ctrl -)
    * FEATURE-945 Added environment variables resolving to the external command plugin
    * FEATURE-946 Updated to Inno-Setup 5.5.9 for improved installer security
    * FEATURE-958 Greenshot is now using code signing certificates
    
    Added or changed translations:
    * CatalÃ  by Gabriel Guix.
    * Nederlands by Stephan Paternotte
    * Czech by Petr Toman
