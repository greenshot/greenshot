---
layout: faq
status: publish
published: true
title: Why does Windows 8 suggest to install earlier .NET versions when starting Greenshot?
tags: []

---
<p>In case you are using Windows 8, you might see a message when starting Greenshot, saying "An app on your PC needs the following WIndows feature: .NET Framework 3.5 (includes .NET 2.0 and 3.0)". <a href="/faq/why-does-windows-8-suggest-to-install-earlier-net-versions-when-starting-greenshot/an-app-on-your-pc-needs-the-following-windows-feature-net-framework-3-5-includes-net-2-0-and-3-0/" rel="attachment wp-att-1028"><img src="/assets/wp-content/uploads/2013/10/an-app-on-your-pc-needs-the-following-windows-feature-.net-framework-3.5-includes-.net-2.0-and-3.0-300x231.png" alt="An app on your PC needs the following WIndows feature" width="300" height="231" class="alignleft size-medium wp-image-1028" /></a></p>
<p><strong>Just skip it.</strong> As far as we know, Greenshot is working fine with .NET 4.0.</p>
<p>Greenshot 1.x is built to run with .NET Framework version 2.0/3.5, and Windows simply does not know that it can be run on .NET 4.0 also, and thus asks whether to install version 3.5.</p>
<p>If, contrary to expectations, you encounter any errors with this setup, please <a href="/tickets/">let us know</a>.</p>
