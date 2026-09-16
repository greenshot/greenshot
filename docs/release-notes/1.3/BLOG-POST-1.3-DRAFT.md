---
layout: post
status: draft
published: false
title: "Greenshot 1.3: Sicherheitsupdate ist da"
tags:
- status
- '1.3'
---

Mit Greenshot 1.3 steht ein wichtiges Sicherheitsupdate bereit. Wir empfehlen allen Nutzerinnen und Nutzern das zeitnahe Update über die offizielle [Download-Seite](https://getgreenshot.org/downloads/).

## Was ist neu?

Greenshot 1.3 behebt ein Sicherheitsproblem bei sicheren Internetverbindungen (HTTPS). Dadurch sind Verbindungen zu angebundenen Online-Diensten besser vor Manipulation und Mitlesen geschützt.

## Kurz und einfach erklärt

In manchen Situationen hat Greenshot früher Zertifikate zu leicht akzeptiert. In einem unsicheren Netzwerk (zum Beispiel in einem fremden WLAN) hätte sich dadurch jemand zwischen Greenshot und einem Onlinedienst schalten können.

Mit dem Update prüft Greenshot diese Zertifikate jetzt korrekt. Das macht den Datenaustausch deutlich sicherer.

## Warum das wichtig ist

Viele Nutzer verbinden Greenshot mit Diensten wie Imgur, Box, Dropbox, Flickr, Confluence oder Jira. Der Fix reduziert das Risiko, dass dabei sensible Verbindungsdaten abgefangen oder verändert werden.

## Danke für verantwortungsvolle Offenlegung

Vielen Dank an die Security-Community für die verantwortungsvolle Offenlegung solcher Themen. Das hilft uns, Greenshot für alle sicherer zu machen.

---

Benutzerfreundliche Release Notes: [USER-CHANGELOG-1.3.md](./USER-CHANGELOG-1.3.md)
