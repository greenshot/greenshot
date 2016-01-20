---
layout: faq
status: publish
published: true
title: What is the best way to control Greenshot's configuration at install time?
# override permalink for to keep old URLs intact
permalink: /faq/what-is-the-best-way-to-control-greenshots-configuration-at-install-time/
tags: []

---
<p>Greenshot offers several mechanisms for better control over its configuration, especially useful when deploying Greenshot to several computers, e.g. roll-out in a company.</p>
<p>Greenshot looks for config files in the following locations (and accepts the first location it finds a config file in):</p>
<ol>
<li>installation directory (where Greenshot.exe is located)</li>
<li>the executing user's roaming application data directory (e.g. <code>C:\Users\USERNAME\AppData\Roaming\Greenshot\</code>)</li>
</ol>
<p>There may be three files to control the configuration, each one of them may have all or a subset of the configuration parameters Greenshot offers. The files are loaded in the following order, each one overwriting the configuration parameters of the previous (if set):</p>
<ol>
<li>greenshot-defaults.ini</li>
<li>greenshot.ini</li>
<li>greenshot-fixed.ini</li>
</ol>
<p>Use <strong>greenshot-defaults.ini</strong> to provide your users with a common default configuration, e.g. where files should be stored or whether Greenshot should check for newer versions.<br />
<strong>greenshot.ini</strong> is used by Greenshot to store any settings changed by the user.<br />
Use<strong>greenshot-fixed.ini</strong> to force a set of configuration settings whenever Greenshot starts up. E.g. you might want to ensure that all users use the grayscale option for printing. This file is loaded last, so specifying a setting in here will overwrite the same setting (if present) from the previous files. In most cases fixed settings can also not be changed in Greenshot's settings dialog. If a user succeeds in changing the configuration manually, it will be overwritten again when Greenshot is started next time.</p>
<p>Greenshot will not modify greenshot-defaults.ini or greenshot-fixed.ini. If greenshot.ini is not found in both locations, it will be created automatically in the roaming application data directory, the configuration will then be aggregated from Greenshot's default configuration and greenshot-defaults.ini/greenshot-fixed.ini (if present).</p>
<p><strong>See also:</strong><br />
<a href="/faq/where-does-greenshot-store-its-configuration-settings/" title="Where does Greenshot store its configuration settings?">Where does Greenshot store its configuration settings?</a></p>
