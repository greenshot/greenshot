---
layout: post
status: publish
published: true
title: ! 'Release candidate 1 for 1.2.9'


tags:
- 1.2
- bugfix
- release


---
<p>Today we present you the first release candidate for Greenshot 1.2.9 which should fix a lot of issues with 1.2.8 and add some new features.</p>
<p>One of the most significant changes is that from this version onwards we sign Greenshot with a code signing certificate.
This should helps personal users and companies to feel more secure knowing the installed software is really from us.
We will write a separate blog on what we things we do to keep Greenshot secure without adware, malware and viruser.</p>
<p>Other significant changes:</p>
<ul>
<li>Imgur upload gives 403 errors</li>
<li>Jira API doesn't work anymore</li>
</ul>
<p>As always you can download it from our <a href="/downloads/" title="Downloads">downloads </a>page. By the way: please be sure to always download Greenshot from our official website</strong>, which is <a href="http://getgreenshot.org/">http://getgreenshot.org/</a>. A lot of platforms offer downloads of free and open source software as well, but there have been several cases of installer files being corrupted or bundled with malware. So: <strong>always get greenshot from getgreenshot.org</strong> :) </p>
<p>Here is the complete changelog for the 1.2.9 release candidate 1:</p>

	All details to our tickets can be found here: https://greenshot.atlassian.net

	1.2.9.97-f1d7072 RC1

	This is a release candidate for the coming release of Greenshot.
	**Testing is not finished, use at your own risk...**

	Changes for the following reported tickets were added since 1.2.8.12:

	Fixed bugs:
	* BUG-1762 Dropshadow & tornedge prompts for settings every time
	* BUG-1812 Editor opens outside the screen
	* BUG-1876 Already running message, multi user environment
	* BUG-1884 OCR text has trailing blank spaces
	* BUG-1887 Editor hangs on exit - hang time proportional to number of objects
	* BUG-1890 Slight cropping around window on Windows 10
	* BUG-1892 Greenshot saves blank JPG file with reduce colors
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

	Added features:
	* FEATURE-916 Added icon save format (capture will be resized to 16x16, 32x32, 48x48 or 256x256)
	* FEATURE-919 Added a way to increase the editor canvas size (use Ctrl + / Ctrl -)
	* FEATURE-945 Added environment variables resolving to the external command plugin
	* FEATURE-946 Updated to Inno-Setup 5.5.9 for improved installer security
	* FEATURE-958 Greenshot is now using code signing certificates

	Added translation:
	* Català by Gabriel Guix.
	* Nederlands by Stephan Paternotte
