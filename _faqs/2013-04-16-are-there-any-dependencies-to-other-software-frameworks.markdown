---
layout: faq
status: publish
published: true
title: Are there any dependencies to other software / frameworks?
tags: []

---
<p>Yes. Greenshot 1.3 is built for the <strong>Microsoft .NET Framework 4.7.2</strong> and needs version 4.7.2 or newer to run. Windows 10 (version 1803 and later) and Windows 11 already include it, so on these systems nothing extra has to be installed.</p>
<p>The .NET Framework 4.x is part of Windows: newer versions such as 4.8 and 4.8.1 replace older ones in place, and Greenshot runs on whatever 4.7.2 or newer version is installed. Microsoft supports .NET Framework 4.7.2, 4.8 and 4.8.1 for as long as the Windows version they are installed on, and delivers their security updates through Windows Update. See Microsoft's <a href="https://learn.microsoft.com/lifecycle/products/microsoft-net-framework">.NET Framework lifecycle</a> for details. Greenshot does not need the older .NET Framework 2.0 or 3.5.</p>
<p>The <code>System.*.dll</code> files and other libraries in the Greenshot folder come with Greenshot. They are libraries Greenshot uses, built for the .NET Framework. The version number in their file properties (for example 4.6, 6.0 or 8.0) is the version of that library, not a version of .NET that has to be installed. Greenshot does not use .NET 6 or .NET 8.</p>
<p>If you are unsure whether the .NET Framework 4.7.2 or newer is installed on your system, simply use our exe installer. It will check whether it is there, and if not offer to download and install it for you (if you want).</p>
<p><strong>See also:</strong><br />
<a href="/faq/what-exactly-does-the-exe-installer-do/">What exactly does the exe installer do?</a><br />
<a href="/faq/is-greenshort-really-portable/">Is Greenshort really portable?</a></p>
