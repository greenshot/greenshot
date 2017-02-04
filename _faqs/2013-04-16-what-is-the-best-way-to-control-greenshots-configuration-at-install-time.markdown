---
layout: faq
status: publish
published: true
title: What is the best way to control Greenshot's configuration at install time?
# override permalink for to keep old URLs intact
permalink: /faq/what-is-the-best-way-to-control-greenshots-configuration-at-install-time/
tags: []

---

This documentation will help you if want to have more control over Greenshots configuration, for instance to pre-set a language or specifiy settings for your companies JIRA/Confluence server.

Except for the functionality that Greenshot is started when Windows starts, all it's settings are stored in [.ini files](https://en.wikipedia.org/wiki/INI_file).
Every added plug-in will has it's own section in the configuration, making sure that developers of plug-in don't need to concern themselves with how the configuration is written.

Greenshot has an intelligent configuration system, and it is readable and even changeable by humans!
To support manual editing the greenshot.ini automatically adds comments to every setting when this is written by Greenshot.
*But if you plan to change something manually, we advice you to stop Greenshot first!!*

If a setting or even a configuration file is missing Greenshot will either take default settings supplied by the developer or a system administrator.
This has the nice advantage that if things no longer work, a setting or the complete configuration can simply be removed.

With our system it's possible to supply specific settings as default, or even make them non changeable.
To understand the possibilities, you first need to know where, how and in what order Greenshot reads it's configuration.

Greenshot knows of 3 different files, which are loaded in the following order:

1. greenshot-defaults.ini: this specifies the defaults which are used if no other setting are available. (At first start)
2. greenshot.ini: this is the normal file, with all the settings of the user, which is written by Greenshot. This overrules the settings in the greenshot-defaults.ini file.
3. greenshot-fixed.ini: has settings which will overrule all settings in the files above.

Greenshot will look for every mentioned file, in the described order, first in the same location as the executable (e.g. installation directory) and if there is not there than in %APPDATA%\Greenshot (e.g. ```C:\Users\USERNAME\AppData\Roaming\Greenshot\```).

The configuration is build from zero, setting for setting, by using the following 4 steps:

1. Take the default set by the developer
2. If a greenshot-defaults.ini was found, and the setting can be found in there, the value from 1 is overwritten.
3. If a greenshot.ini was found, and the setting can be found in there, the value from 2 is overwritten.
4. If a greenshot-fixed.ini was found, and the setting can be found in there, the value from 3 is overwritten.

Greenshot will use the resulting setting, and when every single setting in the complete configuration is processed it will write the complete configuration to it's greenshot.ini file (and only there).

Let's look at a use-case which was asked for a lot of times:
For instance you want to rollout Greenshot in your company and you want to make sure the user doesn't need to select the language (which is asked if nothing was set)?
You can **either** copy a greenshot-defaults.ini, if you want the user to be able to change it, or a greenshot-fixed.ini in the same directory as Greenshot was installed.

To set the default language to Dutch, this file will need to have the following content:

```
[Core]
Language=nl-NL
```

**See also:**

[Where does Greenshot store its configuration settings?](/faq/where-does-greenshot-store-its-configuration-settings/)
