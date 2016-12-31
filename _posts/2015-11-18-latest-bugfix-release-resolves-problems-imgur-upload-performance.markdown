---
layout: post
status: publish
published: true
title: ! 'Latest bugfix release resolves problems with Imgur upload and performance'
release_version: 1.2.8.12
tags:
- 1.2
- bugfix
- release


---
<p>We have recently published another bugfix release, fixing two annoying problems many of you might have encountered during the last days.</p>
<ul>
<li>Broken anonymous upload to Imgur: a lot of users reported that they can no longer upload to Imgur anonymously. Unfortunately, Imgur simply deprecated and shut down the old version of their API, without announcing or even documenting this step. We could have reacted in time if we had known about the change before it was made; with the change not even being documented, we have lost several days analyzing the issue. We would have preferred if all of you had been able to use Greenshot with Imgur as you always did, and of course we would have preferred to implement something useful in that time. Actions like this make working with 3rd-party-APIs quite painful, for developers and for users. We&#8217;re glad the issue is fixed, but we are not amused, Imgur. :-/</li>
<li>Greenshot sometimes slow to respond: another issue inflicted by a third party. You may have noticed that our website (currently hosted at Sourceforge) is unavailable quite often lately, as the Sourceforge systems often have performance issues or even downtimes. Sourceforge&#8217;s project web is also used by Greenshot to check for new versions. Unfortunately, the implementation of our update check was not ideal, so that the enormous number of failed requests slowed down the program. Even though we worked around this issue by fixing our update check, the instability of our website is <em>one of</em> the things we are generally quite unhappy with at Sourceforge; we have started to move parts of our development infrastructure over to Github. This website also be moved as soon as the migration is finished.</li>
</ul>
<p>These two major issues are fixed by the latest update, you can download it from our <a href="/downloads/" title="Downloads">downloads </a>page. By the way: please be sure to always download Greenshot from our official website</strong>, which is <a href="http://getgreenshot.org/">http://getgreenshot.org/</a>. A lot of platforms offer downloads of free and open source software as well, but there have been several cases of installer files being corrupted or bundled with malware. So: <strong>always get greenshot from getgreenshot.org</strong> :) </p>
<p>Here is the complete changelog for the bugfix release:</p>


    1.2.8.12-cab854b RELEASE
    There were some major issues with the authenticated (non anonymous) uploads to Imgur.
    After contacting Imgur they told us that their old API was deprecated or disabled, unfortunately this was not communicated.
    Although we are working hard on Greenshot 1.3, where we changed most of the Imgur code already, we can't release it.
    We did see a need to fix the imgur uploads, so in this release we quickly updated the Imgur API.
    This should resolve a lot of tickets that were reported to us.
    
    Additionally our website http://getgreenshot.org is hosted by sourceforge and they seem to have a lot of stability problems.
    Due to these problems a bug in the Greenshot update check manifested itself and causes Greenshot to get slow or even stop responding.
    In this version we fix the bug in the update check, and we are also working on a solution for the instability with our website.
    
    Here is the list of chances:
    
    Bugs Resolved:
    
    BUG-1527 / BUG-1848 / BUG-1850 / BUG-1851 / BUG-1859 : Greenshot stops responding, hangs or crashes
    BUG-1843 / BUG-1844 / BUG-1846 : Imgur problems with authenticated uploads
    BUG-1864: Imgur link wasn't copied to the clipboard

    Features:

    FEATURE-896: Use Imgur with HTTPS (with changing the Imgur API from V2 to V3 this was already required for the upload anyway.)
