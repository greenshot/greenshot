---
layout: page
status: publish
published: true
title: Downloads
permalink: /downloads/
comments: []
tags: menu
---
{% assign latest_release = site.github.releases | where:"prerelease",false | where:"draft",false | first %}
<div class="two-col left-box">
<h2>Download the latest stable release</h2>

<table class="cta-button">
	<tr>
		<td>
			{% for asset in latest_release.assets %}
				{% if asset.name contains 'RELEASE.exe' %}
					<a href="{{ asset.browser_download_url }}" class="cta" title="Download the latest stable version of Greenshot" rel="nofollow" style="display:table-cell">Latest version</a>
				{% endif %}
			{% endfor %}
		</td>
	</tr>
	<tr>
		<td class="cta-description">
		{% if latest_release %}
			{{ latest_release.name }}<br>{{ latest_release.created_at | date_to_string %}}
		{% endif %}
		</td>
	</tr>
</table>

<p><a href="#" onclick="jQuery('#w8info').slideToggle();return false;">(Info for Windows 8 users)</a><br/> <span id="w8info" style="display:none">Windows might ask you to install .NET 3.5 when running Greenshot. You can skip this. <a href="/faq/why-does-windows-8-suggest-to-install-earlier-net-versions-when-starting-greenshot/">Read more</a></span></p>
<p>In most cases, the latest stable version will be the best choice for you. However, if you are looking for the latest unstable version, need an older version or the ZIP distribution, you will find everything you need in the <a href="/version-history/" title="Download other versions of Greenshot">version history</a>.</p>

<h2>Source code</h2>
<p>If you want to have a look at the source code, you can do so in Greenshot's Git repositories at
<a href="https://github.com/greenshot/greenshot/">GitHub</a> or 
<a href="https://bitbucket.org/greenshot/greenshot/">BitBucket</a>.</p>
</div>

<div style="clear:both"></div>
