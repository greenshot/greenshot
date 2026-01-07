---
layout: post
status: publish
published: true
title: ⚠️ Security Fix - Please Update to Latest Release
tags:
- status
- '1.3'
---

**A security issue has been found in Greenshot up to version 1.3.304. We have released a fix and recommend to update to the [new version](https://getgreenshot.org/downloads) as soon as possible.**

Kudos to Github user [@lihnucs](https://github.com/lihnucs), who has responsibly disclosed the problem to us with a very detailed and well-analyzed report. Thanks a lot 💚

All technical details can be found in the security advisory [security advisory GHSA-7hvw-q8q5-gpmj](https://github.com/greenshot/greenshot/security/advisories/GHSA-7hvw-q8q5-gpmj).
Less technical summary: if you are using Greenshot's External Command Plugin to call shell interpreters (like CMD or Powershell), an attacker could execute custom commands by fiddling with Greenshot's configuration file on your PC, or by making you open a file with a manipulated filename.

Stay safe, and remember: always get Greenshot from getgreenshot.org
