---
layout: faq
status: publish
published: true
title: How can I turn off logging?
tags: []
 
---
To turn off logging, locate the file log4net.xml, which is located in the same folder as the Greenshot.exe file. Create a backup of that file and open it in a text editor. (Note: if Greenshot is installed in your "Programs" directory, you might need to do this as Windows administrator.)

Instead of

    <level value="INFO"/>
    
you can insert

    <level value="OFF"/>
    
to turn off logging completey or

    <level value="ERROR"/>


**See also:**

* [Where can I find Greenshot's log file?](/faq/where-can-i-find-greenshots-log-file/)
* [How can I make Greenshot write debug information to its log file?](/faq/how-can-i-make-greenshot-write-debug-information-to-its-log-file/)
