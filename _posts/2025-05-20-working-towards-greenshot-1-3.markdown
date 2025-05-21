---
layout: post
status: publish
published: true
title: Working towards Greenshot 1.3
tags:
- status
- '1.3'
---

Long time no see... in our [last blog post](/2024/02/11/current-status-greenshot/) we explained why the project activity had slowed down so much, and that we're working on getting development up and running again. That was quite some time ago 🙈 Now we have some news to share: 

#### Code Signing Up and Running

Finally, we are able to create signed releases again 🎉 it took quite some time and effort, and we have learned that code signing for Windows is much more complicated than it was just a few years ago. This step is crucial in our mission, and the basis for the next steps to come.

For starters, we have recently released a signed installer of a recent unstable version (1.3.281), you can find it on our [version history](/version-history/) page. If the missing signing was the only reason for you not to switch to one of the unstable versions, you can now go ahead and grab it. Please note that this is still an *unstable* release, which means it hasn't been thoroughly tested and might include unexpected behavior. Also note that (automated) continuous builds are _not_ signed.

#### Next Steps

Our top priority is to publish a final (stable) version of Greenshot 1.3 as soon as possible, for future versions we are aiming for smaller releases and shorter release cycles. For the 1.3 release, this means:
* we will have a look at incoming merge requests, especially bug fixes
* we are going to release a release candidate for testing soon
* in order to avoid further delays for 1.3
  * we will not add more new features (there's already a lot in this release)
  * we will not fix any bugs that already exist in 1.2 (unless they are already fixed in the current 1.3 version, of course)
* we might even decide to release with minor/cosmetic bugs

Stay tuned 😎
