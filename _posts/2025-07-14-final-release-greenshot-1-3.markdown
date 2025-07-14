---
layout: post
status: publish
published: true
title: 🎉 Greenshot 1.3 Final Release!
tags:
- status
- '1.3'
---


# 🎉 Greenshot v1.3.290 RC1 Released — Fixed Vulnerability, Zoom, improved HiDPI awareness & More!

We're excited to announce the release of **Greenshot v1.3.290 (Final Release)** – a significant step forward for one of the most popular open-source screenshot tools for Windows. 
This version introduces **new features**, **UI improvements**, and **important bug fixes** that refine the overall user experience.



## 🛠 Fixed Vulerability Issue

Greenshot 1.2.10 and below allowed arbitrary code execution because .NET content is insecurely deserialized when a .greenshot file is opened.
This long awaited fix has already been provided in unstable versions and now found it's way into the final release of version 1.3.



## 🔍 Zooming In on Productivity

One of the most requested features is finally here:  
✅ **Zoom functionality in the editor** – Easily zoom in and out using **CTRL + mouse wheel**, allowing for better precision when editing screenshots.



## 🛠 Fixes That Matter

This release also tackles several issues that have affected usability and stability:

- 🔧 **Improved DPI awareness**: Interface elements like handles and containers now scale correctly on high-resolution displays.
- ✨ **Dynamic filename placeholder for random alphanumerics** – Ensures more flexibility and security when saving screenshots.
- 🧾 Fixed Unicode rendering in the editor for better multilingual support.
- 🐧 Improved Wine compatibility for Linux/WSL users.
- 📨 Fixed MAPI detection for email integrations.
- 🧹 Removed embedded browser component to reduce dependencies and potential security issues.
- ✍️ Improved object scaling behavior when using the Shift key.
- 🔁 Fixed duplicated shapes (ellipses, highlights) in the editor.
- 💬 Resolved text rendering problems inside callout containers.



## 🌍 Languages & Clean Code

- Several translations updated (English, Italian, Czech, Japanese, Traditional Chinese, and more).
- Major internal code cleanup to support long-term maintainability and performance.


## ✅ Final Thoughts

Greenshot v1.3.290 RC1 is a robust, more polished version that lays the groundwork for future development. 
Whether you’re a power user or a casual screenshot taker, this release offers **more control, sharper performance, and cleaner usability**.


👉 [Download Greenshot v1.3.290](https://getgreenshot.org/downloads/)



---


#### Change Log

Here is the full list of changes since the latest Greenshot 1.2 version

* Fixed Insecure Deserialization Arbitrary Code Execution - CVE-2023-34634 https://github.com/greenshot/greenshot/issues/579
* Update master to latest, first 1.2.9 by @Lakritzator in https://github.com/greenshot/greenshot/pull/70
* Release/1.2.9 bf2 -> master by @Lakritzator in https://github.com/greenshot/greenshot/pull/71
* Release/1.2.10 -> master by @Lakritzator in https://github.com/greenshot/greenshot/pull/72
* Typo fix in language-en-US.xml by @clpo13 in https://github.com/greenshot/greenshot/pull/76
* Update/Add language-ja-JP Files by @Rukoto in https://github.com/greenshot/greenshot/pull/69
* Sync master to develop by @Lakritzator in https://github.com/greenshot/greenshot/pull/105
* Zoom for the editor (Feature-84) by @KillyMXI in https://github.com/greenshot/greenshot/pull/201
* Converters fix for release 1.3 by @KillyMXI in https://github.com/greenshot/greenshot/pull/204
* Feature/win10 secure version support by @Lakritzator in https://github.com/greenshot/greenshot/pull/207
* Release/1.3 by @bovirus in https://github.com/greenshot/greenshot/pull/224
* Add placeholder for random alphanumerics in filename by @peterfab9845 in https://github.com/greenshot/greenshot/pull/216
* Propagate DPI Changes down to Drawable Containers and Adorners and Resize Grippers Accordingly by @jklingen in https://github.com/greenshot/greenshot/pull/200
* Update setup.iss by @bovirus in https://github.com/greenshot/greenshot/pull/228
* italian language files update by @bovirus in https://github.com/greenshot/greenshot/pull/230
* Update Italian language by @bovirus in https://github.com/greenshot/greenshot/pull/238
* Update Italian language by @bovirus in https://github.com/greenshot/greenshot/pull/237
* Italian language update by @bovirus in https://github.com/greenshot/greenshot/pull/241
* Update Italian language (removed double string) by @bovirus in https://github.com/greenshot/greenshot/pull/245
* Update installer script by @bovirus in https://github.com/greenshot/greenshot/pull/243
* Improve DPI support by @Lakritzator in https://github.com/greenshot/greenshot/pull/254
* Update English language by @bovirus in https://github.com/greenshot/greenshot/pull/242
* Fix an issue when running Greenshot via Wine by @Lakritzator in https://github.com/greenshot/greenshot/pull/262
* Reduce maintenance for Windows 10 toast by @Lakritzator in https://github.com/greenshot/greenshot/pull/265
* BUG-2693: Fix for MAPI detection by @Lakritzator in https://github.com/greenshot/greenshot/pull/266
* Add CTRL+Wheel zoom by @k41c in https://github.com/greenshot/greenshot/pull/282
* Code cleanup for the copyright and fixed BUG-2736 by @Lakritzator in https://github.com/greenshot/greenshot/pull/286
* Fix Unicode text drawing by @Lakritzator in https://github.com/greenshot/greenshot/pull/287
* Update Italian language by @bovirus in https://github.com/greenshot/greenshot/pull/290
* Update Help (IT) by @bovirus in https://github.com/greenshot/greenshot/pull/291
* Update URLs to HTTPS by @TotalCaesar659 in https://github.com/greenshot/greenshot/pull/292
* Improved Drag & Drop support + cleanup by @Lakritzator in https://github.com/greenshot/greenshot/pull/294
* Fix text rendering inside text / speech bubble containers by @Lakritzator in https://github.com/greenshot/greenshot/pull/297
* Get rid of embedded browser by @Lakritzator in https://github.com/greenshot/greenshot/pull/255
* Fix Inconsistent Scale Behavior when Scaling Objects with Shift Modifier by @jklingen in https://github.com/greenshot/greenshot/pull/300
* Fix typos by @TotalCaesar659 in https://github.com/greenshot/greenshot/pull/301
* Project cleanup by @Lakritzator in https://github.com/greenshot/greenshot/pull/302
* Fix Ellipse and Highlight duplication bug (#322). by @Ishmaeel in https://github.com/greenshot/greenshot/pull/331
* Corrected czech translation by @Masv-MiR in https://github.com/greenshot/greenshot/pull/330
* Update of Czech language file by @svatas in https://github.com/greenshot/greenshot/pull/332
* FEATURE-1196 by @EricCogen in https://github.com/greenshot/greenshot/pull/339
* Add Traditional Chinese to website and installer by @5idereal in https://github.com/greenshot/greenshot/pull/343
* fix typo in language-website-en-US.xml by @5idereal in https://github.com/greenshot/greenshot/pull/344
* Update README.md by @erl-mallard in https://github.com/greenshot/greenshot/pull/351
* Update language-zh-TW.xml by @5idereal in https://github.com/greenshot/greenshot/pull/345
* Add shortcuts (0-9, +/-) for foreground color, background color, line thickness, bold and shadow (#338) by @Lakritzator in https://github.com/greenshot/greenshot/pull/366
* Update language-de-DE.xml by @Mr-Update in https://github.com/greenshot/greenshot/pull/316
* prevent negative fontsize by @Christian-Schulz in https://github.com/greenshot/greenshot/pull/382
* Feature Improve file format support by @Lakritzator in https://github.com/greenshot/greenshot/pull/385
* Enhanced ability to crop an image vertically and horizontally. #249 by @Christian-Schulz in https://github.com/greenshot/greenshot/pull/388
* Update Italian language by @bovirus in https://github.com/greenshot/greenshot/pull/394
* IsConfirmable for IDrawableContainer by @Christian-Schulz in https://github.com/greenshot/greenshot/pull/399
* Refactoring to use Dapplo.Windows by @Lakritzator in https://github.com/greenshot/greenshot/pull/398
* Fix initial crop selection by @Christian-Schulz in https://github.com/greenshot/greenshot/pull/407
* Fix of BUG2951 by @jdavila71 in https://github.com/greenshot/greenshot/pull/431
* add install option to disable the default win11 prtscr tool by @jglathe in https://github.com/greenshot/greenshot/pull/484
* Add Release Script by @jklingen in https://github.com/greenshot/greenshot/pull/581
* Calculate optimal font size for StepLabel  #457 by @Christian-Schulz in https://github.com/greenshot/greenshot/pull/460
* Publish Unsigned Release on Commit and Purge CloudFlare Cache on Pages Build by @jklingen in https://github.com/greenshot/greenshot/pull/583
* #572 Fix Error when Opening .greenshot File with Arrows by @FF-Brown in https://github.com/greenshot/greenshot/pull/574
* Fix scaling with fixed ratio by @jairbubbles in https://github.com/greenshot/greenshot/pull/514
* fix: handle picky Win11 ToastNotificationService by @jglathe in https://github.com/greenshot/greenshot/pull/487
* Resize hotkey by @FF-Brown in https://github.com/greenshot/greenshot/pull/480

#### New Contributors
* @clpo13 made their first contribution in https://github.com/greenshot/greenshot/pull/76
* @Rukoto made their first contribution in https://github.com/greenshot/greenshot/pull/69
* @peterfab9845 made their first contribution in https://github.com/greenshot/greenshot/pull/216
* @k41c made their first contribution in https://github.com/greenshot/greenshot/pull/282
* @Ishmaeel made their first contribution in https://github.com/greenshot/greenshot/pull/331
* @Masv-MiR made their first contribution in https://github.com/greenshot/greenshot/pull/330
* @svatas made their first contribution in https://github.com/greenshot/greenshot/pull/332
* @EricCogen made their first contribution in https://github.com/greenshot/greenshot/pull/339
* @5idereal made their first contribution in https://github.com/greenshot/greenshot/pull/343
* @erl-mallard made their first contribution in https://github.com/greenshot/greenshot/pull/351
* @jdavila71 made their first contribution in https://github.com/greenshot/greenshot/pull/431
* @jglathe made their first contribution in https://github.com/greenshot/greenshot/pull/484
* @FF-Brown made their first contribution in https://github.com/greenshot/greenshot/pull/574
* @jairbubbles made their first contribution in https://github.com/greenshot/greenshot/pull/514

