---
layout: post
status: publish
published: true
title: Small bugfix release today
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 199
wordpress_url: http://getgreenshot.org/?p=199
date: !binary |-
  MjAxMC0wNy0xMiAyMjo0OTozNiArMDIwMA==
date_gmt: !binary |-
  MjAxMC0wNy0xMiAyMDo0OTozNiArMDIwMA==
categories:
- Releases
tags:
- '0.8'
- release
- bugfix
comments:
- id: 16
  author: Stefan Thieme
  author_email: info@isnoroot.de
  author_url: ''
  date: !binary |-
    MjAxMC0wNy0xNSAyMDozNjo0MSArMDIwMA==
  date_gmt: !binary |-
    MjAxMC0wNy0xNSAxODozNjo0MSArMDIwMA==
  content: ! 'Dear Greenshots,


    I really like the features you built into the software, especially how you can
    highlight or obfuscate some things.

    That said I appreciate your work and would like to give you some positive feedback,
    please remind me if I may have missed something or you have changed the behaviour
    I am describing in a later version. I have Greenshot 0.8.0 Build 0627 installed.


    Being used to take a lot of screenshots let me make some recommendations for further
    cleansing up of the tool.

    * I am used to select a specific rectangle with Ctrl+Print, which usually gives
    me most Control of what I select.

    * Selecting a Window/Section is the second most important feature, currently I
    have to press Print and then press the space bar. I would prefer to either select
    this hotkey, e.g. simply Print or Alt+Print. <ins datetime="2010-07-24T07:37:00+00:00">(1)</ins>

    Also the space bar sometimes hangs in my version, I have to select a small rectangle
    and before releasing the Mouse Button I hit space.

    * The Editor Window that appears should be triggerable by the space bar, this
    way I can decide before releasing the mouse button whether I want to edit/highlight/obfuscate
    the screenshot I am going to take. This mode setting could be shown by a small
    test or an edit Icon somewhere top left/right corner of the screen. <ins datetime="2010-07-24T07:37:00+00:00">(2)</ins>

    * In the Edit window I usually want to make some adjustements that I want to either
    save or copy into another application. Currently I have to select Ctrl+A and Ctrl+C
    to update the contents of the ClipBoard and also for saving I have to select Ctrl+A
    and Ctrl+S to retain my changes.

    I would like to be able to define this task, i.e. when I choose to edit a screenshot,
    see previous point, I usually prefer to save/copy the changed version. Could you
    make this a Preferences Option by default.  <ins datetime="2010-07-24T07:37:00+00:00">(3)</ins>

    * When closing the Editor I currently get a request that asks me whether I want
    to save the file. Usually I have either copied it to the clipboard and inserted
    it into a document or want to have it saved. This is a general Preferences Options
    as well, I should be able to select whether this last screenshot is saved automatically
    to some folder Edited Screenshots. The current default Preferences Option of automatically
    saving the Original Screenshot is fine as well, but I am mostly interested in
    the edited. Maybe both Options for Autosave can coexist with either different
    folders or _edited_ pre/postfix notations to the filenames. <ins datetime="2010-07-24T07:37:00+00:00">(4)</ins>

    The current default behaviour to confirm every Editor window before closing is
    a tedious task, for which it should be possible to disable this safeguard feature
    in the Preferences for power users.


    I assume these are mostly minor changes that would propel your tool as the number
    one screenshot solution on my USB stick with portable apps, as well as on my Linux
    notebook for heavy mugshot usage. I hope that you will accept these usability/accessibility
    additions as your current shortcuts for Lines, Arrows, Rectangles, Ellipses, Text
    Obfuscation and Highlighting are already very intuitive to me!


    Kind regards,

    Stefan'
- id: 23
  author: greenshot
  author_email: jens.klingen@jklm.de
  author_url: ''
  date: !binary |-
    MjAxMC0wNy0yNCAxMDoyMjo0NiArMDIwMA==
  date_gmt: !binary |-
    MjAxMC0wNy0yNCAwODoyMjo0NiArMDIwMA==
  content: ! 'Thanks for your feedback and suggestions, Stefan.


    (1) Can''t you use the existing hotkey <em>Alt+Print</em> in order to capture
    windows? You should not have to start region mode and then switch to window mode
    by hitting the space key....

    And yes, there are some special situations in which the space bar would not work
    during region mode - this has something to do with window focus. We are aware
    of this and will fix it.


    (2) I have added this as a <a href="https://sourceforge.net/tracker/?func=detail&amp;aid=3033934&amp;group_id=191585&amp;atid=937975"
    rel="nofollow">feature request</a>. Seems to be a useful feature to me. Feel free
    to monitor the tracker item and add new feature requests if you have more ideas
    how Greenshot could be improved :)


    (3) and (4) I am not sure if I understood this correctly, but these two points
    are basically referring to the same things, right? When using the editor screenshot
    destination in combination with clipboard and/or save to file destination, you
    would prefer to have the image copied/saved when closing the editor window, or
    maybe both - before and after editing. Is that correct?


    Thanks again,

    Jens'
- id: 58
  author: Jozef
  author_email: jozefu@gmail.com
  author_url: ''
  date: !binary |-
    MjAxMC0wOC0wMiAxMjo1MzowOCArMDIwMA==
  date_gmt: !binary |-
    MjAxMC0wOC0wMiAxMDo1MzowOCArMDIwMA==
  content: ! "Dear Greenshots.\r\nYour program is very nice. But it would be better,
    if the program should have function paste (add) new screenshot into the previous
    image,  (screenshots). What do you think about it? \r\nThanks.\r\nJozef"
- id: 151
  author: greenshot
  author_email: greenshot-developers@lists.sourceforge.net
  author_url: http://getgreenshot.org/
  date: !binary |-
    MjAxMC0wOC0wOSAyMjozNzo0OCArMDIwMA==
  date_gmt: !binary |-
    MjAxMC0wOC0wOSAyMDozNzo0OCArMDIwMA==
  content: ! "Thanks for your feedback, Jozef.\r\nYou can do this already, however
    (currently) not directly.\r\nJust take two screenshots, so that there are two
    editor windows open. Copy one of the screenshots to the clipboard (Ctrl+Shift+C),
    focus the other editor window and paste the image (Ctrl+V). Here you are, screenshot
    within screenshot :)"
- id: 365
  author: free dating sites
  author_email: ''
  author_url: http://www.freedatingsites24.com#1
  date: !binary |-
    MjAxMy0wNy0xOSAwMzowNTowOCArMDIwMA==
  date_gmt: !binary |-
    MjAxMy0wNy0xOSAwMTowNTowOCArMDIwMA==
  content: ! '<strong>free dating sites...</strong>


    I am a long time in the past I study your weblog and possesses extended been stating
    that you’re an incredible writer...'
- id: 3832
  author: backlink checker
  author_email: ''
  author_url: http://www.youtube.com/watch?v=j-WG-qFA2Jc
  date: !binary |-
    MjAxNC0wOC0xMiAxMzoxNToxOSArMDIwMA==
  date_gmt: !binary |-
    MjAxNC0wOC0xMiAxMToxNToxOSArMDIwMA==
  content: ! '<strong>backlink checker</strong>


    [...]Here are a few of the web sites we suggest for our visitors[...]'
---
<p>The current version, Greenshot 0.8.0-0627, fixes some bugs and/or annoyances of the previous version. This includes a few errors that occurred under special circumstances. The most obvious changes in general are that new editor windows now open in the same position where previous editor windows have been closed, highlight and obfuscator elements no longer re-use the last used values (e.g. for line thickness and color) from other elements (e.g. rectangle) and that larger font sizes are allowed in the editor (has been limited to 24px before).</p>
<p>We recommend updating to the <a href="/current/" rel="nofollow">current version of Greenshot</a>.</p>
