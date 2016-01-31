---
layout: page
status: publish
published: true
title: Downloads
permalink: /downloads/
categories: []
comments: []
tags: menu
---
{% assign latestRelease = site.github.releases | first %}
<!-- TODO retrieve downloadable files -->
<div class="two-col left-box">
<h2>Download the latest stable release</h2>
<p style="display:table-row">
	{% for asset in latestRelease.assets %}
		{% if asset.name contains '.exe' %}
			<a href="{{ asset.browser_download_url }}" class="cta" title="Download the latest stable version of Greenshot" rel="nofollow" style="display:table-cell">Latest version</a>
		{% endif %}
	{% endfor %}
	<small style="display:table-cell;vertical-align:middle;padding-left:1em;">
		{% if latestRelease %}
			{{ latestRelease.name }}<br>{{ latestRelease.created_at | date_to_string %}}
		{% endif %}
	</small>
</p>
<p><a href="#" onclick="jQuery('#w8info').slideToggle();return false;">(Info for Windows 8 users)</a><br/> <span id="w8info" style="display:none">Windows might ask you to install .NET 3.5 when running Greenshot. You can skip this. <a href="/faq/why-does-windows-8-suggest-to-install-earlier-net-versions-when-starting-greenshot/">Read more</a></span></p>
<p>In most cases, the latest stable version will be the best choice for you. However, if you are looking for the latest unstable version, need an older version or the ZIP distribution, you will find everything you need in the <a href="/version-history/" title="Download other versions of Greenshot">version history</a>.</p>

<h2>Source code</h2>
<p>If you want to download or have a look at the source code, you can do so at <a href="https://bitbucket.org/greenshot/greenshot/">Greenshot's Git respository</a> at BitBucket.</p>
</div>
<div class="two-col right-box">
<h2>Download translations</h2>
<p>If you want to use Greenshot in another language, simply download the appropriate language file and save it to the "Languages" subdirectory of Greenshot's installation directory.<br />
<strong>Note:</strong> <img src="/assets/wp-content/themes/greenshot/images/flags/us.gif" width="16" height="11" alt="EN" /> English and <img src="/assets/wp-content/themes/greenshot/images/flags/de.gif" width="16" height="11" alt="DE" /> German language files are included in the program by default.</p>
<p><?=$langs?></p>
<p><?=$helps?></p>
</div>
<div style="clear:both"></div>
