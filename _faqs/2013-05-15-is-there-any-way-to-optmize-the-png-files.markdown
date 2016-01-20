---
layout: faq
status: publish
published: true
title: Is there any way to optmize the PNG files?
tags: []

---
<p>As of version 1.1, Greenshot has functionality to use PNG optimizers, like OptiPNG, which can reduce the filesize of PNG files drastically. Currently this functionality is disabled by default as we don't include a tool due to size and license reasons. So this feature needs to be enabled manually in the Greenshot.ini file (see <a href="/faq/where-does-greenshot-store-its-configuration-settings/">Where does Greenshot store its configuration settings?</a>) by configuring the path to the executable under the section <code>[Core]</code> in the setting <code>OptimizePNGCommand</code> and eventually (default works with OptiPNG) the command line arguments in the setting <code>OptimizePNGCommandArguments</code>. Note: The command is called whenever a PNG is written or uploaded, so if it is very slow Greenshot will take longer.</p>
<p>Example:<br />
<code><br />
; Optional command to execute on a temporary PNG file, the command should overwrite the file and Greenshot will read it back. Note: this command is also executed when uploading PNG's!<br />
OptimizePNGCommand=C:\Tools\optipng.exe</p>
<p>; Arguments for the optional command to execute on a PNG, {0} is replaced by the temp-filename from Greenshot. Note: Temp-file is deleted afterwards by Greenshot.<br />
OptimizePNGCommandArguments="{0}"<br />
</code></p>
<p><strong>See also:</strong><br />
<a href="/faq/where-does-greenshot-store-its-configuration-settings/">Where does Greenshot store its configuration settings?</a></p>
