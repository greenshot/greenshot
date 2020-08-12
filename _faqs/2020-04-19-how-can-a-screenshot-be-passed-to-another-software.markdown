---
layout: faq
status: publish
published: true
title: How can a screenshot be passed to another software?
tags: []

---
This can be done by the so called _external commands_. Here you find some interesting descriptions about them:
* [How to use the External Command Plugin to send screenshots to other applications](https://getgreenshot.org/2013/01/28/how-to-use-the-external-command-plugin-to-send-screenshots-to-other-applications/)
* [How to send an image to Skype using command line arguments](https://getgreenshot.org/2013/02/17/how-to-send-an-image-to-skype-using-command-line-arguments/)
* [How to Upload Screenshots to a Web Server via FTP, SCP or SFTP](https://getgreenshot.org/2013/11/07/how-to-upload-screenshots-to-a-web-server-via-ftp-scp-or-sftp/)

The screenshots are saved in the temporary folder defined by %TEMP%.

Furthermore, own batch scripts can be created to realise more complicated or customized tasks. 
As a hint: _%1_ is the first argument passed to the script and most likely the file name sent from Greenshot by "{0}".
_%1_ includes the surrounding quotes whereby _%~1_ removes them in a Windows batch file.
