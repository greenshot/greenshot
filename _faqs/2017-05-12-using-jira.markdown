---
layout: faq
status: publish
published: true
title: Using Jira
permalink: /faq/using-jira/
tags: []
---

Greenshot can be configured to upload screenshots to your Jira Server or Jira Cloud instance. With version 1.2.9 and later Greenshot will, as soon as a Jira connection was made once, monitor every window title change and use this information to detect the most recent Jira tickets you saw in a browser or email. These tickets will be sorted, from most to least recently seen, and are available in the destination picker as sub-entries of the Jira plugin.

Important notice:
The upload window that Greenshot provides when no ticket was selected, can only be populated via filters which are stored by the account which is used to connect. You will need to create at least one filter, otherwise the window will stay empty.
