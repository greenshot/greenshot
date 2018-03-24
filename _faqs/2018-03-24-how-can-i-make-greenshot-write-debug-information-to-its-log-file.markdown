---
layout: faq
status: publish
published: true
title: How can I make Greenshot write debug information to its log file?
tags: []
 
---
Whenever you experience unexpected behavior in Greenshot, it is a good idea to consult [Greenshot's log file](/faq/where-can-i-find-greenshots-log-file/) - it's also helpful for us if you attach the relevant part of it when submitting a bug report to us. If the log output is not detailed enough for you (or us) to track down the issue, it is also possible to have some more detailed logging.

Therefore, locate the file log4net.xml, which is located in the same folder as the Greenshot.exe file. Create a backup of that file and open it in a text editor. (Note: if Greenshot is installed in your "Programs" directory, you might need to do this as Windows administrator.)

Instead of

    <level value="INFO"/>
    
you can insert

    <level value="DEBUG"/>
    
to get more details in the log file.


**See also:**

* [Where can I find Greenshot's log file?](/faq/where-can-i-find-greenshots-log-file/)
* [How can I turn off logging?](/faq/how-can-i-turn-off-logging/)
