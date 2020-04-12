---
layout: page
status: publish
published: true
title: Greenshot Hilfe
permalink: /help/de-de/
categories: []
tags: []
comments: []
---
<div class="pull-right">{% include help-nav.html %}</div>

<small>Version 1.2.10<!-- - Deutsche Übersetzung der Hilfe von IHR_NAME--></small>


<h2>Inhalt</h2>
<ol>
<li><a href="#screenshot">Erstellung eines Screenshots</a></li>
<ol>
<li><a href="#capture-region">Bereich abfotografieren</a></li>
<li><a href="#capture-last-region">Zuletzt gewählten Bereich abfotografieren</a></li>
<li><a href="#capture-window">Fenster abfotografieren</a></li>
<li><a href="#capture-fullscreen">Kompletten Bildschirm abfotografieren</a></li>
<li><a href="#capture-ie">Internet Explorer abfotografieren</a></li>
</ol>

<li><a href="#editor">Verwendung des Bildeditors</a></li>
<ol>
<li><a href="#editor-shapes">Formen zeichnen</a></li>
<li><a href="#editor-text">Text hinzufügen</a></li>
<li><a href="#editor-highlight">Hervorheben</a></li>
<li><a href="#editor-obfuscate">Unkenntlich machen</a></li>
<li><a href="#editor-crop">Screenshot zuschneiden</a></li>
<li><a href="#editor-adding-graphics">Hinzufügen von Grafiken zum Screenshot</a></li>
<li><a href="#editor-reuse-elements">Elemente wiederverwenden</a></li>
<li><a href="#editor-export">Screenshot exportieren</a></li>
</ol>
<li><a href="#settings">Der Einstellungen-Dialog</a></li>
<ol>
<li><a href="#settings-general">Allgemeine Einstellungen</a></li>
<li><a href="#settings-capture">Abfotografieren Einstellungen</a></li>
<li><a href="#settings-output">Ausgabeeinstellungen</a></li>
<li><a href="#settings-printer">Druckereinstellungen</a></li>
</ol>
<li><a href="#help">Wollen Sie Greenshot unterstützen?</a></li>
<ol>
<li><a href="#help-donate">Spenden</a></li>
<li><a href="#help-spread">Weitersagen</a></li>
<li><a href="#help-translate">Übersetzen</a></li>
</ol>
</ol>

<a name="screenshot"></a>
<h2>Erstellung eines Screenshots</h2>
<p>
Zum Erstellen eines Screenshots können Sie entweder die <kbd>Drucken</kbd>-Taste
verwenden oder sie klicken das Greenshot-Icon im Systray mit der rechten Maustaste.<br>
Es gibt verschiedene Optionen bei der Erstellung eines Screenshots:
</p>

<a name="capture-region"></a>
<h3>Bereich abfotografieren <kbd>Drucken</kbd></h3>
<p>
Im Bereichsmodus können sie einen Bildschirmbereich auswählen, der abfotografiert
werden soll.<br>
Nach dem Starten des Bereichsmodus sehen Sie ein Fadenkreuz, das die Position
des Mousecursors auf dem Bildschirm anzeigt. Klicken und halten sie die linke
Maustaste an der Stelle, wo eine der Ecken Ihres Screenshots liegen soll.
Halten Sie die Maustaste gedrückt, während sie die Maus bewegen, um das Rechteck
zu markieren, von dem ein Screenshot erstellt werden soll. Wenn das grüne Rechteck
den Bereich bedeckt, den Sie aufnehmen wollen, lassen sie die Maustaste los.<br>
</p>
<p class="hint">
Mit Hilfe der <kbd>Leertaste</kbd> können sie vom Bereichsmodus in den
<a href"#capture-window">Fenstermodus</a> wechseln (und umgekehrt).
</p>
<p class="hint">
Wenn Sie einen exakten Bereich abfotografieren, ist es eventuell einfacher, zuerst
einen etwas größeren Bereich abzufotografieren und den Screenshot anschließend mit
Hilfe des Bildeditors <a href="#editor-crop">zuzuschneiden</a>.
</p>

<a name="capture-last-region"></a>
<h3>Zuletzt gewählten Bereich abfotografieren <kbd>Shift</kbd> + <kbd>Drucken</kbd></h3>
<p>
Wenn Sie vorher einen <a href="#capture-region">Bereich</a> oder ein 
<a href="#capture-window">Fenster</a> abfotografiert haben, können Sie mit Hilfe
dieser Option den gleichen Bereich noch einmal abfotografieren.
</p>

<a name="capture-window"></a>
<h3>Fenster abfotografieren <kbd>Alt</kbd> + <kbd>Drucken</kbd></h3>
<p>
Erstellt einen Screenshot des momentan aktiven Fensters.
</p>
<p class="hint">
Im <a href="#settings">Einstellungen-Dialog</a> können Sie einstellen, dass nicht
sofort das aktive Fenster abfotografiert werden soll, sondern stattdessen der
interaktive Modus gestartet wird. Wenn diese Option ausgewählt ist, können Sie
ein Fenster auswählen, indem Sie darauf klicken (wie beim 
<a href="#capture-region">Bereichsmodus</a> hebt Greenshot auch hier den Bereich 
hervor, der abfotografiert werden wird).<br>
Wenn Sie ein Kind-Fenster abfotografieren wollen (z.B. einen Browser-Viewport (ohne 
Symbolleisten usw. oder einen einzelnen Frame einer Webseite mit Framesets) ziehen
Sie den Mauszeiger auf das Fenster und drücken Sie die <kbd>Bild ab</kbd>-Taste.
Anschließend können Sie Kind-Elemente des Fensters durch anklicken auswählen.
</p>

<a name="capture-fullscreen"></a>
<h3>Kompletten Bildschirm abfotografieren <kbd>Strg</kbd> + <kbd>Drucken</kbd></h3>
<p>
Erstellt einen Screenhot vom gesamten Bildschirm.
</p>

<a name="capture-ie"></a>
<h3>Internet Explorer abfotografieren <kbd>Control</kbd> + <kbd>Shift</kbd> + <kbd>Print</kbd></h3>
<p>
Erstellt komfortabel einen Screenshot von einer Webseite, die gerade im Internet Explorer
geöffnet ist. Benutzen Sie Greenshots Kontextmenü, um das abzufotografierende Tab im Internet
Explorer aus einer Liste zu wählen oder drücken Sie <kbd>Strg</kbd> + <kbd>Shift</kbd> + <kbd>Drucken</kbd>
um das aktive Tab abzufotografieren.
</p>

<a name="editor"></a>
<h2>Verwendung des Bildeditors</h2>
<p>
Greenshot bietet Ihnen einen einfaches Bildbearbeitungswerkzeug mit praktischen
Möglichkeiten; ein Screenshot kann beispielsweise mit Anmerkungen und Formen
ergänzt werden. Es ist auch möglich, Teile des Screenshots hervorzuheben oder
unkenntlich zu machen.
</p>
<p class="hint">
Sie können Greenshots Bildeditor nicht nur für Screenshorts verwenden. Sie
können auch Bilder aus Dateien oder aus der Zwischenablage zur Bearbeitung
öffnen. Klicken Sie einfach mit der rechten Maustaste auf das Greenshot-Icon im
Systray und wählen Sie <em>Bild aus Datei öffnen</em> bzw. <em>Bild aus
Zwischenablage öffnen</em>.
</p>
<p class="hint">
Standardmäßig wird der Bildbearbeiter immer geöffnet, wenn ein Screenshot
gemacht wird. Wenn Sie den Bildbearbeiter nicht verwenden wollen, können 
Sie dies in den <a href="#settings">Einstellungen</a> deaktivieren.
</p>


<a name="editor-shapes"></a>
<h3>Formen zeichnen</h3>
<p>
Wählen Sie eines der Form-Zeichnen-Werkzeuge aus der Werkzeugliste auf der linken
Seite des Bildeditors oder aus dem Menü <em>Objekt</em>. Zur schnelleren
Bedienung ist jedem Werkzeug ein Buchstabe zugeordnet.<br>
Folgende Formen sind verfügbar: Rechteck <kbd>R</kbd>, Ellipse <kbd>E</kbd>, 
Linie <kbd>L</kbd> und Pfeil <kbd>A</kbd>.<br>
Klicken Sie die linke Maustaste, halten Sie diese gedrückt und bewegen Sie die Maus,
um den Position und Größe der Form zu bestimmen. Lassen Sie die Maustaste los,
wenn Sie fertig sind.
</p>
<p>
Sie können bereits gemalte Formen verschieben oder ihre Größe ändern. Wählen Sie
hierzu das Auswahlwerkzeug <kbd>ESC</kbd> aus der Werkzeugleiste.<br>
Jede Element-Art hat außerdem bestimmte Einstellungen, mit denen das Aussehen des
Elements verändert werden kann (z.B. Linienstärke, Rahmenfarbe, Hintergrundfarbe).
Sie können diese Einstellungen für ein bereits gezeichnetes Element ändern, wenn
es ausgewählt ist. Sie können die Einstellungen aber auch vor dem Zeichnen eines
Elements anpassen, direkt nach der Auswahl eines Zeichnen-Werkzeugs.
</p>
<p class="hint">
Sie können mehrere Elemente gleichzeitig zur Bearbeitung auswählen. Halten Sie
hierzu die <kbd>Shift</kbd>-Taste gedrückt, während Sie die Elemente anklicken.

<a name="editor-text"></a>
<h3>Text hinzufügen</h3>
<p>
Das Textwerkzeug <kbd>T</kbd> wird ähnlich verwendet wie die 
<a href="#editor-shapes">Formen-Werkzeuge</a>.
Zeichnen Sie einfach ein Textelement in der gewünschten Größe und geben Sie
den gewünschten Text ein.<br>
Durch Doppelklicken können Sie den Text eines bestehenden Textelements bearbeiten.<br>
Drücken Sie <kbd>Return</kbd> oder <kbd>Enter</kbd> um die Bearbeitung des Textes zu beenden.
</p>
<p class="hint">
Wenn Sie Zeilenumbrüche innerhalb einer Textbox benötigen, drücken Sie <kbd>Shift</kbd> + <kbd>Return</kbd> oder
<kbd>Shift</kbd> + <kbd>Enter</kbd>.
</p>

<a name="editor-highlight"></a>
<h3>Hervorheben</h3>
<p>
Nach der Auswahl des Hervorhebungs-Werkzeugs <kbd>H</kbd> können Sie den hervorzuhebenden
Bereich wählen, gehen Sie hierzu vor wie beim Zeichnen von <a href="#editor-shapes">Formen</a>.<br>
Sie habe verschiedene Möglichkeiten der Hervorhebung, wählen Sie ein durch Klicken
der Schaltfläche links in der Symbolleiste über dem Screenshot:
</p>
<ul>
<li><em>Textmarker</em>: hinterlegt den Bereich mit einer Farbe ihrer Wahl</li>
<li><em>Bereich hervorheben</em>: alles außerhalb des Bereichs wird weichgezeichnet<a href="#hint-blur">*</a> 
und leicht verdunkelt</li>
<li><em>Graustufen</em>: alles außerhalb des Bereichs wird in Graustufen dargestellt</li>
<li><em>Magnify</em>: der Bereich wird vergrößert dargestellt</li>
</ul>

<a name="editor-obfuscate"></a>
<h3>Unkenntlich machen</h3>
<p>
Wenn ein Screenshot Daten enthält, die nicht weitergegeben werden sollen (z.B. Kontodaten,
Namen, Passwörter oder Gesichter auf Bildern), ist es sinnvoll diese unkenntlich zu machen.<br>
Das Unkenntlich-machen-Werkzeug <kbd>O</kbd> wird verwendet wie das 
<a href="#editor-highlight">Hervorheben</a>-Werkzeug.<br>
Folgende Möglichkeiten stehen zur Verfügung:
</p>
<ul>
<li><em>Verpixeln</em>: im Bereich werden die Pixel vergrößert</li>
<li><em>Weichzeichnen</em><a href="#hint-blur">*</a>: der Bereich wird weichgezeichnet</li>
</ul>
<a name="hint-blur"></a>
<p class="hint">
* Je nach Leistung Ihres Computers kann die Verwendung von Weichnzeichner-Effekten
Greenshots Bildeditor verlangsamen. Wenn Sie merken, dass der Bildeditor zu langsam
reagiert sobald ein Weichzeichner verwendet wird, reduzieren Sie den Wert 
<em>Vorschauqualiät</em> in der Symbolleiste oder stellen Sie einen kleineren
<em>Weichzeichner-Radius</em> ein.<br>
Sollte die Reaktionsgeschwindigkeit dann immer noch unzufriedenstellend sein, sollten
Sie dem Verpixeln-Werkzeug den Vorzug geben.
</p>

<a name="editor-crop"></a>
<h3>Screenshot zuschneiden</h3>
<p>
Wenn Sie nur einen Teil des Screenshots benötigen, können Sie das Zuschneiden-Werkzeug
<kbd>C</kbd> verwenden, um den Screenshot auf die gewünschte Größe zuzuschneiden.<br>
Wählen Sie das Zuschneiden-Werkzeug, zeichnen Sie dann ein Rechteck über den Bereich
des Screenshots, den Sie behalten wollen. Sie können die Größe des ausgewählten 
Bereichs ändern, wie bei jedem anderen Element.<br>
Wenn Sie mit Ihrer Auswahl zufrieden sind, klicken Sie die Bestätigen-Schaltfläche in
der Symbolleiste oder drücken Sie die <kbd>Enter</kbd>-Taste. Sie können den Vorgang
abbrechen, indem Sie die Abbrechen-Schaltfläche klicken oder <kbd>ESC</kbd> drücken.
</p>
<p class="hint">
<em>Auto-Crop</em>: Wenn Sie einen einfarbigen Rahmen von Ihrem Screenshot entfernen möchten,
wählen Sie einfach <em>Automatisch zuschneiden</em> aus dem Menü <em>Bearbeiten</em> und
Greenshot wählt automatisch einen Bereich für den Zuschnitt aus.
</p>

<a name="editor-adding-graphics"></a>
<h3>Adding graphics to a screenshot</h3>
<p>
You can simply add graphics or images to your screenshot by dragging and dropping an image
file into the editor window. You can also insert screenshots of other windows by selecting
<em>Insert window</em> from the <em>Edit</em> menu. A list of all open windows appears, 
allowing you to select one for insertion.
</p>

<a name="editor-reuse-elements"></a>
<h3>Elemente wiederverwenden</h3>
<p>
Wenn Sie regelmäßig die gleichen oder ähnliche Elemente in Ihren Screenshots
verwenden (z.B. ein Textfeld, in dem Browsertyp und -version angegeben sind, oder
Verpixelung des gleichen Elements auf mehreren Screenshots) können Sie diese
Elemente wiederverwenden.<br>
Wählen Sie <em>Objekte in Datei speichern</em> aus dem <em>Objekt</em>-Menü um
die aktuellen Elemente zur späteren Wiederverwendung zu speichern. <em>Objekte
aus Datei laden</em> fügt die Elemente dann zu einem anderen Screenshot hinzu.
</p>

<a name="editor-export"></a>
<h3>Screenshot exportieren</h3>
<p>
Nach der Bearbeitung des Screenshots können Sie das Ergebnis auf verschiedene
Arten exportieren, je nach Bedarf. Alle Export-Optionen sind über das <em>Datei</em>-
Menü, die obere Symbolleiste oder über Tastaturkürzel verfügbar:
</p>
<ul>
<li><em>Speichern</em> <kbd>Strg</kbd> + <kbd>S</kbd>: speichert die Grafik in eine Datei (wenn es bereits gespeichert wurde, ansonsten wird der Dialog <em>Speichern unter...</em> angezeigt</li>
<li><em>Speichern unter...</em> <kbd>Strg</kbd> + <kbd>Shift</kbd> + <kbd>S</kbd>: öffnet einen Dialog, in dem Sie Verzeichnis, Dateiname und Grafikformat für die zu speichernde Grafik wählen können</li>
<li><em>Grafik in Zwichenablage kopieren</em> <kbd>Strg</kbd> + <kbd>Shift</kbd> + <kbd>C</kbd>: legt eine Kopie der Grafik in der Zwischenablage ab, so dass sie direkt in andere Programme eingefügt werden kann</li>
<li><em>Drucken...</em> <kbd>Strg</kbd> + <kbd>P</kbd>: sendet die Grafik an einen Drucker</li>
<li><em>E-Mail</em> <kbd>Strg</kbd> + <kbd>E</kbd>: öffnet eine neue Nachricht in Ihrem Standard-E-Mail-Programm und hängt die Grafik als Datei an</li>
</ul>
<p class="hint">
Nach dem Speichern einer Grafik im Bildeditor können Sie mit der rechten Maustaste auf
die Statusleiste am unteren Rand des Editor-Fensters klicken, um entweder den Dateipfad
zu öffnen.
</p>


<a name="settings"></a>
<h2>Der Einstellungen-Dialog</h2>

<a name="settings-general"></a>
<h3>Allgemeine Einstellungen</h3>
<ul>
<li><em>Sprache</em>: Die Sprache, in der Sie Greenshot verwenden möchten.<br>
<a target="_blank" href="http://getgreenshot.org/downloads/">Hier</a> können Sie weitere Sprachen für Greenshot herunterladen. </li>
<li><em>Greenshot mit Windows starten</em>: Das Programm wird automatisch gestartet wenn das System hochfährt.</li>
<li><em>Tastenkombinationen</em>: Konfigurieren Sie die Tastenkombinationen für das Erstellen von Screenshots</li>
<li><em>Standard-Proxyserver des Betriebssystems verwenden</em>: Wenn ausgewählt, wird der im Betriebssystem eingetragene Standard-Proxyserver verwendet, um nach Updates zu suchen.</li>
<li><em>Aktualisierungsprüfung in Tagen</em>: Greenshot kann automatisch nach Updates suchen. Verwenden Sie die Einstellung, um das Interval zu ändern
(in days) oder setzen sie den Wert auf 0, um die automatische Suche nach Updates zu deaktivieren..</li>
</ul>

<a name="settings-capture"></a>
<h3>Abfotografieren Einstellungen</h3>
<ul>
<li><em>Mauszeiger mit abfotografieren</em>: Wenn diese Option aktiviert ist, wird der Mauszeiger abfotografiert. Der Zeiger wird im Editor als eigenes Element eingefügt, so dass Sie ihn anschließend noch verschieben oder löschen können.</li>
<li><em>Kamera-Klang abspielen</em>: Hörbares feedback beim Erstellen eines Screenshots</li>
<li><em>Millisekunden warten vor Abfotografieren</em>: Stellen Sie die Verzögerung ein, bevor der Screenshot erstellt wird.</li>
<li><em>Fenster interaktiv abfotografieren</em>: Anstatt sofort das aktive Fenster abzufotografieren, können Sie im 
interaktivem Modus ein Fenster auswählen. Es ist auch möglich, Kind-Fenster abzufotografieren, siehe <a href="#capture-window">Fenster abfotografieren</a>.</li>
<li>
<em>Aero Design abfotografieren (nur Windows Vista / 7)</em>: Wenn Sie Greenshot auf Windows Vista oder Windows 7 verwenden und Aero-Design Fenster
aktiviert haben, können Sie einstellen, wie mit transparenten Fensterrahmen umgegangen werden soll, wenn ein Screenshot
im Fenstermodus angefertigt wird. Verwenden Sie diese Einstellung, um zu vermeiden, dass durchscheinende Elemente im Hintergrund mit
abfotografiert werden.
<ul>
<li><em>Auto</em>: Greenshot entscheiden lassen, wie mit Transparenzen umgegangen werden soll.</li>
<li><em>Wie angezeigt</em>: Transparente Rahmen werden abfotografiert, wie sie auf dem Bildschirm angezeigt werden.</li>
<li><em>Standardfarbe verwenden</em>: Eine einheitliche Hintergrundfarbe wird anstelle von Transparenz verwendet.</li>
<li><em>Eigene Farbe verwenden</em>: Wählen Sie eine Farbe aus, die anstelle von Transparenz verwendet werden soll.</li>
<li><em>Transparenz erhalten</em>: Die Transparenz der Rahmen wird beim Abfotografieren erhalten, Elemente im Hintergrund werden nicht mit abfotografiert. (Hinweis: 
	transparent Bereiche werden im Editor mit einem Schachbrettmuster dargestellt. Das Muster wird nicht mit exportiert, wenn der Screenshot gespeichert wird.
	Denke Sie daran, den Screenshot als PNG zu speichern, da nur dieses Format die volle Unterstützung für
	Transparenz bietet.)</li>
</ul>
</li>
<li><em>Internet Explorer abfotografieren</em>: Komfortables Abfotografieren von Webseiten im Internet Explorer aktivieren.</li>
<li><em>Editorfenster an Screenshotgröße anpassen</em>: Wenn ausgewählt, wird das Editorfenster automatisch auf die Größe des Screenshots vergrößert oder verkleinert.</li>
</ul>

<a name="settings-output"></a>
<h3>Ausgabeeinstellungen</h3>
<ul>
<li><em>Screenshot Ziel</em>: Sie können ein oder mehrere Möglichkeiten wählen, was mit dem Screenshot direkt nach der Erstellung geschehen soll.</li>
<li><em>Bevorzugte Ausgabedatei-Einstellungen</em>: Verzeichnis und Dateiname, die verwendet werden sollten wenn Screenshots direkt gespeichert werden, bzw. die vorgeschlagen werden sollen, wenn mit dem Speichern-unter-Dialog gespeichert wird. Klicken Sie die <em>?</em>-Schaltfläche um zu erfahren, welche Platzhalter für das Dateiname-Muster verwendet werden können.</li>
<li><em>JPEG-Einstellungen</em>: Qualitätsstufe für den Export von JPEG-Dateien</li>
</ul>

<a name="settings-printer"></a>
<h3>Druckereinstellungen</h3>
<ul>
<li><em>Ausdruck auf Seitengröße verkleinern</em>: Verkleinert die Grafik, wenn sie zu groß für das Papierformat ist.</li>
<li><em>Ausdruck auf Seitengröße vergrößern</em>: Vergrößert die Grafik, wenn sie kleiner als das Papierformat ist.</li>
<li><em>Drehung des Ausdrucks an das Seitenformat anpassen</em>: Dreht eine Querformat-Grafik für den Ausdruck um 90&deg;.</li>
<li><em>Farben umkehren</em>: Vor dem Druck werden die Farben des Screenshots invertiert, z.B. nützlich wenn ein Screenshot von weißer Schrift auf schwarzem Grund gedruckt wird (um Toner/Tinte zu sparen).</li>
</ul>


<a name="settings-plugins"></a>
<h3>Zusatzmodule Einstellungen</h3>
<p>
Zeigt eine Liste der installierten Greenshot Zusatzmodule an. Wählen Sie einen Eintrag der List und klicken Sie
<em>Konfiguration</em> um die Einstellungen eines Zusatzmoduls zu konfigurieren.
</p>


<a name="help"></a>
<h2>Wollen Sie Greenshot unterstützen?</h2>

<p>
Wir benötigen momentan keine Hilfe bei der Entwicklung von Greenshot. Es gibt aber
unterschiedliche Möglichkeiten, wenn Sie Greenshot und das Entwickler-Team 
unterstützen wollen.<br>
Vielen Dank im Voraus :)
</p>

<a name="help-donate"></a>
<h3>Spenden</h3>
<p>
Wir stecken sehr viel Arbeit in Greenshot oder verbringen einige Zeit damit, ein
gutes Programm kostenlos und open source zur Verfügung zu stellen. Wenn Sie
merken, dass Greenshot Ihnen hilft, produktiver zu sein, wenn es Ihnen (oder 
Ihrer Firma) viel Zeit und Geld spart, oder wenn Sie einfach Greenshot und die
Open-Source-Idee mögen: bitte ziehen Sie in Betracht, unserem Aufwand etwas
Anerkennung durch eine Spende zukommen zu lassen.<br>
Bitte werfen Sie einen Blick auf unsere Webseite, um zu sehen, wie Sie das 
Greenshot Entwicker-Team unterstützen können:
<a target="_blank" href="http://getgreenshot.org/support/">http://getgreenshot.org/support/</a>
</p>

<a name="help-spread"></a>
<h3>Weitersagen</h3>
<p>
Wenn Sie Greenshot mögen, sagen Sie es weiter: erzählen Sie Ihren Freunden und Kollegen 
von Greenshot. Und Ihren Followern :)<br>
Bewerten Sie Greenshot auf Software-Portalen oder verlinken Sie unsere Webseite von
Ihrem Blog oder Ihrer eigenen Webseite.
</p>

<a name="help-translate"></a>
<h3>Übersetzen</h3>
<p>
Gibt es Greenshot noch nicht in Ihrer bevorzugten Sprache? Wenn Sie sich in der
Lage fühlen, ein Programm zu übersetzen, sind Sie herzlich eingeladen.<br>
Wenn Sie registrierter Nutzer bei sourceforge.net sind, können Sie Übersetzungen
<a target="_blank" href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">hier</a> hochladen.<br>
Bitte stellen Sie vorher sicher, dass keine Übersetzung für Ihre Sprache auf unserer
<a target="_blank" href="http://getgreenshot.org/downloads/">Download-Seite</a> existiert. Prüfen Sie auch <a target="_blank" href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">hier</a>, ob
evtl. eine Übersetzung in Arbeit oder in Vorbereitung ist.<br>
Bitte bedenken Sie, dass wir eine Überstzung nur auf unserer Download-Seite zur 
Verfügung stellen werden, wenn Sie über ein sourceforge.net Benutzerkonto bereit gestellt
wurde. Da wir höchstwahrscheinlich nicht in der Lage sein werden, Ihre Übersetzung
zu verstehen, ist es gut, wenn andere sourceforge Nutzer Sie kontaktieren können,
um Verbesserungen vorzuschlagen oder die Übersetzung für eine nachfolgende Greenshot-Version
zu erweitern.
</p>
