---
layout: post
status: publish
published: true
title: How to Upload Screenshots to a Web Server via FTP, SCP or SFTP
tags:
- windows
- plugin
- howto
---
<p><img src="/assets/wp-content/uploads/2013/11/winscp-logo.gif" alt="WinSCP logo" width="64" height="64" class="alignleft size-full wp-image-1041" /> <strong>Ever needed to upload screenshots directly to a server using the FTP, SCP or SFTP protocol? Actually it is quite easy to harness WinSCP's great scripting capabilities with Greenshot's flexible external command plugin. This blog post provides a simple WinSCP upload script and explains how to make Greenshot call the script to upload your screenshot.</strong></p>
<p>Sidenote: we know, a specialized plugin for this would be a nice thing to have. A WinSCP plugin is definitely near the top of our todo list, but it probably will not be available before <a href="/2013/10/16/current-development-status-future-plans/">Greenshot 2.0</a>. So in the meantime the following method is a good alternative.</p>
<p>If WinSCP is not installed on your system yet, you can download it from <a href="http://winscp.net/">http://winscp.net/</a>, it is free and open source, too.<br />
If you have not heard of Greenshot's External Command Plugin yet or if you are not familiar with the Windows command line, you might want to have a look at <a href="/2013/01/28/how-to-use-the-external-command-plugin-to-send-screenshots-to-other-applications/">How to use the External Command Plugin to send screenshots to other applications</a>.</p>
<h2>Configuring the upload script</h2>
<p>It just needs a few lines to put together a WinSCP script that opens a connection, uploads a single file and closes the connection again - we have prepared a simple script for your convenience. Simply <a href="/assets/wp-content/uploads/2013/11/winscpupload.txt">download the WinSCP upload script</a> and store it on your hard disc. Of course, the script needs to know where to upload the files to, so you need to change your the connection settings (in the line starting with <code>open</code>) as well as the target directory on the server (starting with <code>cd</code>).</p>
<h2>Testing the upload script</h2>
<p>We recommend to test the script from the command line to see whether everything is alright. You can invoke the script by entering something like this into your command line:<br />
<code>"C:\path\to\WinSCP.com" /script="C:\path\to\winscpupload.txt" /parameter // """C:\path\to\testfile.jpg"""</code><br />
Of course, <code>C:\path\to\</code> should reference the path to the files on your local hard disc.</p>
<p>If everything works fine, the script should output something like<br />
<code><br />
batch           abort<br />
confirm         off<br />
Searching for host...<br />
Connecting to host...<br />
Authenticating...<br />
Using username "user".<br />
Authenticating with pre-entered password.<br />
Authenticated.<br />
Starting the session...<br />
Reading remote directory...<br />
Session started.<br />
Active session: [1] user@example.com<br />
/remote/path/to/directory/<br />
testfile.jpg |         80 KiB |   37,8 KiB/s | binary | 100%<br />
Session 'user@example.com' closed.<br />
No session.<br />
</code></p>
<h2>Configuring Greenshot to call the upload script</h2>
<p>The rest is pretty easy:</p>
<ol>
<li>Select "Configure external commands" from Greenshot's main menu</li>
<li>Click the "New" button and enter the following values</li>
<li><strong>Name:</strong> <code>WinSCP Upload</code></li>
<li><strong>Command:</strong> <code>C:\path\to\WinSCP.com</code></li>
<li><strong>Argument:</strong> <code>/script="C:\path\to\winscpupload.txt" /parameter // """{0}"""</code></li>
</ol>
<p><a href="/assets/wp-content/uploads/2013/11/greenshot-external-command-winscp.png"><img src="/assets/wp-content/uploads/2013/11/greenshot-external-command-winscp-300x172.png" alt="Configuring external command for WinSCP upload script" width="300" height="172" class="alignright size-medium wp-image-1054" /></a> Take care of the correct syntax, especially the quotes: if there is a space somewhere in a file system path, you need to wrap it into quotes. The last part of "Argument" is literally <code>"""{0}"""</code>, including the 6 double quotes and curly braces. (Greenshot will replace <code>{0}</code> with the filename of the screenshot.)</p>
<p>That's it. After confirming the new external command, you can use the <code>WinSCP Upload</code> option for future screenshots, from the destination picker or the editor's "File" menu.</p>
