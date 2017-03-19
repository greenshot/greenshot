---
layout: page
status: publish
published: true
title: Downloads
menu: Downloads
permalink: /downloads/
categories: []
comments: []
tags: [uses_alerts]
---
{% assign latest_release = site.github.releases | where:"prerelease",false | where:"draft",false | first %}
<div class="two-col left-box">
<h2>Download the latest stable release</h2>

{% if latest_release %}
	<h3>Latest Windows release version:</h3>
	{{ latest_release.name }}<br>{{ latest_release.created_at | date_to_string }}
{% endif %}

<div class="clearfix"></div>
<br>

{% for asset in latest_release.assets %}
	{% if asset.name contains 'RELEASE.exe' %}
		<div class="cta-button-main" style="margin-right: 2em;">
			<div class="btn-wrapper">
				<div class="btn"><a href="{{ asset.browser_download_url }}"><img src="../assets/download-win.png"></a></div><br>
				<div class="description">Greenshot for Windows<br>is free and open source!</div>
			</div>
		</div>
	{% endif %}
{% endfor %}

<div class="cta-button-main">
	<div class="btn-wrapper">
		<div class="btn"><a href="https://itunes.apple.com/us/app/greenshot/id1103915944" target="_blank"><img src="../assets/download-mac.png"></a></div><br>
		<div class="description">Only $1.99<br>to cover our own costs!</div>
	</div>
</div>

<div class="clearfix"></div>
<br><br>

<p class="alert alert-info">
	<i class="fa fa-info-circle"></i> In most cases, the latest stable version will be the best choice for you: it has been thoroughly tested by the community and is already used by myriads of people around the world. However, if you are looking for the latest unstable version, need an older version or the ZIP distribution, you will find everything you need in the <a href="/version-history/" title="Download other versions of Greenshot">version history</a>.
</p>


<p><a href="#" onclick="jQuery('#w8info').slideToggle();return false;">(Info for Windows 8 users)</a><br/> <span id="w8info" style="display:none">Windows might ask you to install .NET 3.5 when running Greenshot. You can skip this. <a href="/faq/why-does-windows-8-suggest-to-install-earlier-net-versions-when-starting-greenshot/">Read more</a></span></p>

<h2>Source code</h2>
<p>If you want to have a look at the source code, you can do so in Greenshot's Git repositories at
<a href="https://github.com/greenshot/greenshot/">GitHub</a> or
<a href="https://bitbucket.org/greenshot/greenshot/">BitBucket</a>.</p>
</div>

<div style="clear:both"></div>
