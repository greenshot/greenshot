# Greenshot 1.3 – Release Notes (für Anwender)

## Wichtigste Änderungen

- **Wichtiges Sicherheitsupdate:** Greenshot prüft HTTPS-Zertifikate jetzt korrekt. Dadurch wird die Verbindung zu Online-Diensten deutlich besser vor Mitlesen und Manipulation geschützt.
- **Mehr Schutz für verbundene Konten:** Tokens für Dienste wie Imgur, Dropbox, Box, Flickr, Confluence und Jira sind mit diesem Fix besser abgesichert.
- **Sicherer im Alltag:** Der Fix ist standardmäßig aktiv und schützt ohne zusätzliche Einrichtung.

## Kurz erklärt: Was war das Problem?

In älteren 1.3-Versionen hat Greenshot bei sicheren Internetverbindungen Zertifikate zu großzügig akzeptiert. In unsicheren Netzwerken (z. B. öffentliches WLAN) konnte dadurch jemand dazwischengehen und Datenverkehr mitlesen oder verändern.

## Empfehlung

Bitte auf Greenshot 1.3 aktualisieren, um den Sicherheitsfix zu erhalten.

---

Detaillierter technischer Changelog: [TECHNICAL-CHANGELOG-1.3.md](./TECHNICAL-CHANGELOG-1.3.md)
