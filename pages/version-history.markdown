---
layout: page 
title: Version history
permalink: /version-history/
categories: []
tags: []
comments: []
tags: [uses_alerts]
---
Do you want to have a look into the source code? 
Do you need an unstable or previous release? 
Do you prefer an installer-less ZIP or the PortableApps distribution?
You'll find everything below.

<i class="fa fa-exclamation-triangle"></i> **Please note:** Unstable versions are not thoroughly tested and might include faulty behavior or experimental features. If you do not like surprises you should rather [download the latest stable version](/downloads/).
{:.alert.alert-warning}
 
{% for release in site.github.releases %}
**{{ release.name }}**

	{% for asset in release.assets %}
- [{{ asset.name }}]({{ asset.browser_download_url }}) ({{ asset.created_at | date_to_string }})
 	{% endfor %}
{% endfor %}
 
