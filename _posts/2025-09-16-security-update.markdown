---
layout: post
status: publish
published: true
title: ⚠️ Security Fix - Please Update to Latest Release
tags:
- status
- '1.3'
---

**A security issue has been found in Greenshot up to version 1.3.300. We have released a fix and recommend to update to the [new version](https://getgreenshot.org/downloads) as soon as possible.**

Kudos to Github user [@RipFran](https://github.com/RipFran), who has responsibly disclosed the problem to us with a very detailed and well-analyzed report, and also helped with re-testing after we implemented a fix. Thanks so much 💚

All technical details can be found in the [security advisory GHSA-8f7f-x7ww-xx5](https://github.com/greenshot/greenshot/security/advisories/GHSA-8f7f-x7ww-xx5w).\
Less technical summary: if a malicious process is already running on your computer, it could send a crafted message to the Greenshot process, making it execute functionality under Greenshot's name.
This vulnerability could be leveraged by an attacker to by-pass security measures that could, for example, block the creation of new processes by untrusted processes.

Stay safe, and remember: always get Greenshot from getgreenshot.org

