---
layout: page
status: publish
published: true
title: Version history
permalink: /version-history/
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 322
wordpress_url: http://getgreenshot.org/
date: !binary |-
  MjAxMi0wMy0yNCAxMTozNzo1OSArMDEwMA==
date_gmt: !binary |-
  MjAxMi0wMy0yNCAwOTozNzo1OSArMDEwMA==
categories: []
tags: []
comments: []
---

{% for release in site.github.releases %}
   {{release.name}}}
	{% for asset in release.assets %}
 		* [{{asset.name}}]({{asset.browser_download_url}})
 	{% endfor %}
{% endfor %}
<!-- TODO retrieve downloadable files -->

<div class="two-col left-box">
<h2>Download other versions of Greenshot</h2>
<p><strong>Please note:</strong> Unstable versions are not thoroughly tested and might include faulty behavior or experimental features. If you do not like surprises you should rather <a href="/current/">download the latest stable version</a>. </p>
<h3>Installers</h3>
<p><?=$installers?></p>
</div>
<div class="two-col right-box">
<h3>ZIP archives</h3>
<p><?=$zips?></p>
<h3>Source code</h3>
<p><?=$srcs?></p>
</div>
<div style="clear:both"></div>
