---
layout: faq
status: publish
published: true
title: My PrntScreen key no longer works with Greenshot.... why?
tags: []

---
<p>Most probably, this is due to other software reserving the <kbd>PrntScreen</kbd> key. This could basically be any software that considers itself good for handling screenshots, but in most of the cases we hear of, the problem is caused by Microsoft OneNote or Dropbox.</p>
<p>Are you using <strong>OneNote</strong>? Look for "Auto save" in OneNote's settings dialog and uncheck the "Automatically save screenshot..." option and you should be fine.</p>	
<p>Are you using <strong>Dropbox</strong>? Several users reported similar problems after installing Dropbox or updating it. Newer versions of Dropbox come with a feature that copies screenshots to your Dropbox account. For this reasons, it registers the <kbd>PrntScreen</kbd> key and Greenshot is no longer notified when you hit the key.</p>
<p>Many people are confused by this, so obviously Dropbox does not make its changes to the system configuration transparent enough. Feel free to contact Dropbox support about this.</p>
<p>There are various workarounds available, pick one :)</p>
<ul>
<li>Uninstall Dropbox</li>
<li>Deactivate Dropbox</li>
<li>Turn off Dropbox' screenshot feature in the Dropbox settings</li>
<li>Change Greenshot's hotkey for screenshots to anything else in the Greenshot settings</li>
</ul>
