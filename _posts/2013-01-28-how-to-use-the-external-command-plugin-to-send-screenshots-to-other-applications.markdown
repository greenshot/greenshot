---
layout: post
status: publish
published: true
title: How to use the External Command Plugin to send screenshots to other applications

tags:
- plugin

---
<p><a href="/assets/wp-content/uploads/2013/01/1-configure-external-commands.png"><img src="/assets/wp-content/uploads/2013/01/1-configure-external-commands.png" alt="Configuring external commands" width="245" height="300" class="alignright size-medium wp-image-756" /></a>
<p> Ever wondered what is the quickest way to have Greenshot output images to other applications on your PC, e.g. your favorite image manipulation software, upload application or instant messenger?</p>
<p>The Greenshot installer comes with a handy plugin called "External Command Plugin", allowing Greenshot to output files to a large number of other programs on your computer. E.g. if you prefer Adobe Photoshop or Inkscape for editing your screenshots, it is an easy task to configure custom destinations which you then can select like any other destination. This will work for any program that offers a <a href="http://en.wikipedia.org/wiki/Command_Line_Interface" target="_blank">command line interface</a> accepting an image path.</p>
<h3>What is a command line interface?</h3>
<p>A command line interface can be used to send commands to a program, e.g.</p>
<p><code>explorer.exe C:</code><br />
will open Windows Explorer and display the contents of your local hard drive C:</p>
<p><code>copy myfile.txt myfile-2.txt</code><br />
will create a copy of myfile.txt, creating a new file called myfile-2.txt</p>
<p><code>mspaint.exe "C:\path\to\image.png"</code><br />
will open MS Paint to edit image.png located in C:\path\to\ (of course, these are just examples, there's probably no directory called C:\path\to on your hard drive)</p>
<p>You can execute commands yourself using the Windows command line tool (called cmd.exe)</p>
<p>But you can also configure Greenshot to execute commands like these which comes very handy if you often need to pass your screenshots to other software.</p>
<h3>So how does Greenshot's External Command Plugin work?</h3>
<p><a href="/assets/wp-content/uploads/2013/01/2-external-command-list.png"><img src="/assets/wp-content/uploads/2013/01/2-external-command-list-300x198.png" alt="List of external commands" title="" width="300" height="198" class="alignleft size-medium wp-image-721" /></a> It's easy... Right click the Greenshot systray icon and click "Configure external commands".</p>
<p>(If it is not there, you are either using an old version of Greenshot or did not select to install the External Command plugin during the installation process. In both cases, you should <a href="/downloads/">download the latest version of Greenshot's installer</a> and install it. When the installer asks about plugins to install, make sure to check the box next to "External Command Plugin", and proceed with the installation.)</p>
<p>After doing so, the configuration dialog opens up. For your convenience, we have already added MS Paint to the list (i.e. you can already use it as a screenshot destination from Greenshot's image editor or destination picker.)<br />
Let's have a look at its configuration: click "MS Paint" in the list and then the "Edit" button on the right-hand side, you'll see the configuration options for the MS Paint command:<br />
<a href="/assets/wp-content/uploads/2013/01/3-edit-external-command.png"><img src="/assets/wp-content/uploads/2013/01/3-edit-external-command-300x173.png" alt="External command configuration window" title="" width="300" height="173" class="alignright size-medium wp-image-722" /></a>
<ul>
<li><strong>Name:</strong> how the external command is displayed by Greenshot, you can put in there whatever you like, e.g. <code>MS Paint</code></li>
<li><strong>Command:</strong> path and file name of the executable program accepting the image, e.g. <code>C:\Windows\System32\mspaint.exe</code></li>
<li><strong>Argument:</strong> additional information to pass to the program, e.g. <code>"{0}"</code></li>
</ul>
<p> In the Argument section <code>{0}</code> will be replaced with the path of the temporary file you are about to export from Greenshot. You should wrap this in double quotes, otherwise it could lead to unexpected behavior if the file name or path contains whitespace characters.</p>
<p>You can close this window. Now that you've had a look at the MS Paint command, you're ready to configure external commands yourself. Simply use the "New" button to add new commands and they will be available in the editor and destination picker.</p>
<p>Please note: not every program offers a command line interface, and arguments are often different than in this example. We (the Greenshot development team) will not be able to offer guidance for working with third party software. Read the other program's documentation or contact its developers to find out whether and how it is possible to pass an image via command line arguments. </p>
