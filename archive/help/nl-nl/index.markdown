---
layout: page
status: publish
published: true
title: Greenshot help
categories: []
tags: []
comments: []
---
<p><small>Version 0.8 Nederlanse vertaling van de help door Jurjen Ladenius</small></p>
<h2>Inhoud</h2>
<ol>
<li><a href="#screenshot">Een screenshot maken</a></li>
<ol>
<li><a href="#capture-region">Regio vastleggen</a></li>
<li><a href="#capture-last-region">Laast gebruikte regio vastleggen</a></li>
<li><a href="#capture-window">Window vangen</a></li>
<li><a href="#capture-fullscreen">Gehele beeldscherm vastleggen</a></li>
</ol>
<li><a href="#editor">Using the image editor</a></li>
<ol>
<li><a href="#editor-shapes">Drawing shapes</a></li>
<li><a href="#editor-text">Adding text</a></li>
<li><a href="#editor-highlight">Highlighting things</a></li>
<li><a href="#editor-obfuscate">Obfuscating things</a></li>
<li><a href="#editor-crop">Cropping the screenshot</a></li>
<li><a href="#editor-reuse-elements">Re-using drawn elements</a></li>
<li><a href="#editor-export">Exporting the screenshot</a></li>
</ol>
<li><a href="#settings">The settings dialog</a></li>
<ol>
<li><a href="#settings-general">General settings</a></li>
<li><a href="#settings-output">Output settings</a></li>
<li><a href="#settings-printer">Printer settings</a></li>
</ol>
<li><a href="#help">Want to help?</a></li>
<ol>
<li><a href="#help-donate">Consider a donation</a></li>
<li><a href="#help-spread">Spread the word</a></li>
<li><a href="#help-translate">Submit a translation</a></li>
</ol>
</ol>
<p>	<a name="screenshot"></a></p>
<h2>Een screenshot maken</h2>
<p>
		U kunt een screenshot maken door de <kbd>Print</kbd> toets van uw keyboard te drukken of<br />
		rechts op het Greenshot icon in de systray te klikken<br><br />
		Er zijn meerdere mogelijkheden om een screenshot te maken:
	</p>
<p>	<a name="capture-region"></a></p>
<h3>Regio vastleggen <kbd>Print</kbd></h3>
<p>
		De "regio vastleggen" modus maakt het mogelijk om een gebied van het beeldscherm te markeren voor een screenshot.<br><br />
		Nadat de regio vastlegen modus gestart wordt verandert de mouse-cursor in een kruis.<br />
		Stuur de muis naar een hoek van het gebied waarvan u een screenshot wilt hebben en<br />
		druk op de linker muisknop en hou deze gedrukt. Beweeg nu de muis naar de tegenovergestelde hoek<br />
		van uw doelgebied, tijdens het bewegen ziet u een groeiende groene rechthoek.<br />
		Op het moment dat u de linker muisknop los laat word het gehele groene gebied vastgelegd.
	</p>
<p class="hint">
		U kunt de <kbd>spatie</kbd> toets gebruiken om tussen de "regio vastleggen" en de<br />
		<a href="#capture-window">window</a> modus te wisselen. De Esc-toets breekt het vastleggen af.
	</p>
<p class="hint">
		Als u een zeer exact gebied wilt vastleggen is het misschen eenvoudiger om eerst een groter<br />
		gebied te nemen en dan de screeshot <a href="#editor-crop">bij te snijden</a> in de<br />
		Greenshot beeld bewerking.
	</p>
<p>	<a name="capture-last-region"></a></p>
<h3>Laast gebruikte regio vastleggen <kbd>Shift</kbd> + <kbd>Print</kbd></h3>
<p>
		Als u al een keer een <a href="#capture-region">regio</a> of een <a href="#capture-window">window</a><br />
		vastgelegt heeft, heeft u met deze optie de mogelijkheid nog een keer de zelfde regio vast te leggen.
	</p>
<p>	<a name="capture-window"></a></p>
<h3>Window vangen <kbd>Alt</kbd> + <kbd>Print</kbd></h3>
<p>
		Maakt een screenshot van het aktive window.
	</p>
<p class="hint">
		In de <a href="#settings">voorkeursinstellingen</a> is een optie om niet<br />
		het aktive window te vangen, maar om een window interaktiv uit te kiezen.<br />
		Als deze optie aan staat kunt u een window selekteren door erop te klikken.<br />
		(net als in de <a href="#capture-region">regio vastleggen</a> modus tekent Greenshot<br />
		een groene rechhoek om het window wat gekozen wordt.)<br><br />
		Als u gedeeltes van een window wilt vangen, bijvoorbeeld het zichtbare gedeelte<br />
		van de website die op dit moment in Internet Explorer getoont word, dan beweeg de muis<br />
		over het window een druk de <kbd>PgDown</kbd> toets. Op dit moment is het mogelijk<br />
		om kleinere gedeeltes van het window te selekteren en te vangen.
	</p>
<p>	<a name="capture-fullscreen"></a></p>
<h3>Gehele beeldscherm vastleggen <kbd>Control</kbd> + <kbd>Print</kbd></h3>
<p>
		Maakt een screenshot van het gehele beeldscherm, ook als er meerdere monitoren gebruikt worden.
	</p>
<p>	<a name="editor"></a></p>
<h2>De Greenshot beeld bewerking gebruiken</h2>
<p>
		Greenshot komt met een eenvoudig te gebruiken beeld bewerking die verschillende handige werktuigen heeft en het<br />
		mogelijk maakt om vormen en annotaties op een screenshot te maken. Het is zelfs mogelijk om bepaalde delen van<br />
		uw screenshot onherkenbaar te maken of te markeren waardoor het beter opvalt.
	</p>
<p class="hint">
		De Greenshot beeld bewerking kan niet alleen voor screenshot maar ook voor andere beelden uit bestanden of het klembord gebruikt worden.<br />
		Klik hiervoor met de rechter muis knop op het Greenshot icon in de systray en selekteer <em>Open beeld uit bestand</em><br />
		of <em>Laad beeld van het klembord</em>.
	</p>
<p class="hint">
		By default, the image editor will be opened whenever a screenshot is<br />
		captured. If you do not want to use the image editor, you can disable this<br />
		behavior in the <a href="#settings">settings dialog</a>.
	</p>
<p>	<a name="editor-shapes"></a></p>
<h3>Drawing shapes</h3>
<p>
		Select one of the shape drawing tools from the toolbar on the left hand side<br />
		of the image editor or from the <em>Object</em> menu. There is also a key assigned<br />
		to each tool for your convenience.<br><br />
		Available shapes are: rectangle <kbd>R</kbd>, ellipse <kbd>E</kbd>, line <kbd>L</kbd><br />
		and arrow <kbd>A</kbd>.<br><br />
		Click, hold down the mouse button and drag to define position and size of the shape.<br />
		Release the mouse button when you are done.
	</p>
<p>
		You can move or resize existing shapes after selecting the selection tool<br />
		<kbd>ESC</kbd> from the toolbar.<br>For every element type there is a specific<br />
		set of options available to change the look of the element (e.g. line thickness,<br />
		line color, fill color). You can change the options for an existing element after<br />
		selecting it, but also for the next element to be drawn after selecting a drawing tool.
	</p>
<p class="hint">
		You can select multiple elements for editing at a time. In order to select multiple<br />
		elements, hold down the <kbd>Shift</kbd> key while clicking the elements.
	</p>
<p>	<a name="editor-text"></a></p>
<h3>Adding text</h3>
<p>
		Usage of the text tool <kbd>T</kbd> is similar to the usage of the<br />
		<a href="#editor-shapes">shape</a> tools. Just draw the text element to the desired<br />
		size, then type in the text.<br><br />
		Double click an existing text element to edit the text.
	</p>
<p>	<a name="editor-highlight"></a></p>
<h3>Highlighting things</h3>
<p>
		After selecting the highlight tool <kbd>H</kbd>, you can define the area to be<br />
		highlighted exactly like you would draw a <a href="#editor-shapes">shape</a>.<br><br />
		There are several options for highlighting, which you can choose from by clicking<br />
		the leftmost button in the toolbar on top:
	</p>
<ul>
<li><em>Highlight text</em>: highlights an area by applying a bright color to it, like<br />
			an office text highlighter</li>
<li><em>Highlight area</em>: blurs<a href="#hint-blur">*</a> and darkens everything outside the selected area</li>
<li><em>Grayscale</em>: everything outside the selected area will be turned to grayscale</li>
<li><em>Magnify</em>: the selected area will be displayed magnified</li>
</ul>
<p>	<a name="editor-obfuscate"></a></p>
<h3>Obfuscating things</h3>
<p>
		Obfuscating parts of a screenshot is a good idea if it contains data which is not<br />
		intended for other people to see, e.g. bank account data, names, passwords or faces on images.<br><br />
		Use the obfuscate tool <kbd>O</kbd> exactly like the <a href="#editor-highlight">highlight</a><br />
		tool.<br><br />
		Available options for obfuscation are:
	</p>
<ul>
<li><em>Pixelize</em>: increases the pixel size for the selected area</li>
<li><em>Blur</em><a href="#hint-blur">*</a>: blurs the selected area</li>
</ul>
<p>	<a name="hint-blur"></a></p>
<p class="hint">
		* Depeding on the performance of your computer, applying a blur effect might slow down<br />
		Greenshot's image editor. If you feel the image editor reacting slowly as soon as a<br />
		blur is applied, try reducing the value for <em>Preview quality</em> in the toolbar or<br />
		decrease the value for <em>Blur radius</em>.<br><br />
		If the blur performance is still too bad for you to work with, you might prefer<br />
		to use the pixelize effect instead.
	</p>
<p>	<a name="editor-crop"></a></p>
<h3>Cropping the screenshot</h3>
<p>
		If you only need a part of the screenshot you have captured, use the crop tool <kbd>C</kbd><br />
		to crop it to the desired area.<br><br />
		After selecting the crop tool, draw a rectangle for the area of the screenshot you want<br />
		to keep. You can resize the selected area like any other element.<br><br />
		When you are content with your selection, use the confirm button in the toolbar or hit<br />
		the <kbd>Enter</kbd> key. You can cancel cropping by clicking the cancel button or hitting<br />
		<kbd>ESC</kbd>.
	</p>
<p>	<a name="editor-reuse-elements"></a></p>
<h3>Re-using drawn elements</h3>
<p>
		If you find yourself using the same or similar elements on most of your screenshots<br />
		(e.g. a textfield containing browser type and version, or obfuscating the same<br />
		element on several screenshots) you can re-use elements.<br><br />
		Select <em>Save objects to file</em> from the <em>Object</em> menu to save the current<br />
		set of elements for re-using it later. <em>Load objects from file</em> applies the<br />
		same elements to another screenshot.
	</p>
<p>	<a name="editor-export"></a></p>
<h3>Exporting the screenshot</h3>
<p>
		After editing the screenshot, you can export the result for different purposes,<br />
		depending on your needs. You can access all export options through the <em>File</em><br />
		menu, the topmost toolbar or via shortcuts:
	</p>
<ul>
<li><em>Save</em> <kbd>Control</kbd> + <kbd>S</kbd>: saves the image to a file (if the image has already been saved, else displays <em>Save as...</em> dialog</li>
<li><em>Save as...</em> <kbd>Control</kbd> + <kbd>Shift</kbd> + <kbd>S</kbd>: lets you choose location, filename and image format for the file to save</li>
<li><em>Copy image to clipboard</em> <kbd>Control</kbd> + <kbd>Shift</kbd> + <kbd>C</kbd>: puts a copy of the image into the clipboard, allowing to paste into other programs</li>
<li><em>Print...</em> <kbd>Control</kbd> + <kbd>P</kbd>: sends the image to a printer</li>
<li><em>E-Mail</em> <kbd>Control</kbd> + <kbd>E</kbd>: opens a new message in your default e-mail client, adding the image as attachment</li>
</ul>
<p class="hint">
		After saving an image from the editor, right-click the status bar at the bottom of<br />
		the editor window to either copy the file path into the clipboard or open the<br />
		containing directory in Windows Explorer.
	</p>
<p>	<a name="settings"></a></p>
<h2>The settings dialog</h2>
<p>	<a name="settings-general"></a></p>
<h3>General settings</h3>
<ul>
<li><em>Language</em>: The language you prefer to be used.<br><br />
			You can download additional language files for Greenshot <a href="#">here</a>. </li>
<li><em>Register hotkeys</em>: If checked, Greenshot can be used with the <kbd>Print</kbd> key.</li>
<li><em>Launch Greenshot on startup</em>: Start the program when the system has been booted.</li>
<li><em>Show flashlight</em>: Visual feedback when doing a capture</li>
<li><em>Play camera sound</em>: Audible feedback when doing a capture</li>
<li><em>Capture mousepointer</em>: If checked, the mousepointer will be captured. The pointer is handled is a separate element in the editor, so that you can move or remove it later.</li>
<li><em>Use interactive window capture mode</em>: Instead of capturing the active window right away, interactive mode<br />
			allows you to select the window to capture. It is also possible to capture child windows, see <a href="#capture-window">window capture</a>.</li>
</ul>
<p>	<a name="settings-output"></a></p>
<h3>Output settings</h3>
<ul>
<li><em>Screenshot destination</em>: Allows you to choose the destination(s) for your screenshot right after capturing it.</li>
<li><em>Preferred output file settings</em>: Directory and filename to be used when saving directly or to be suggested when saving (using the save-as dialog). Click the <em>?</em> button to learn more about the placeholders that can be used as filename pattern.</li>
<li><em>JPEG settings</em>: Quality to be used when saving JPEG files</li>
</ul>
<p>	<a name="settings-printer"></a></p>
<h3>Printer settings</h3>
<ul>
<li><em>Shrink printout to fit paper size</em>: If the image would exceed paper size, it will be shrinked to fit on the page.</li>
<li><em>Enlarge printout to fit paper size</em>: If the image is smaller than the paper size, it will be scaled to be printed as large as possible without exceeding paper size.</li>
<li><em>Rotate printout to page orientation</em>: Will rotate a landscape format image by 90° for printing.</li>
</ul>
<p>	<a name="help"></a></p>
<h2>U wilt helpen?</h2>
<p>
		Op dit moment zoeken we geen hulp voor de ontwikkeling. Maar u kunt meerdere dingen doen om<br />
		het Greenshot ontwikkelteam te ondersteunen.<br><br />
		Bij voorbaat dank :)
	</p>
<p>	<a name="help-donate"></a></p>
<h3>Overweeg een donatie</h3>
<p>
		We stoppen een hoop tijd en werk in Greenshot om goede software gratis en open tot uw beschiking te stellen!<br />
		Als u het gevoel heeft dat Greenshot u meer produktief maakt, u of uw firma een hoop tijd en geld bespaard<br />
		of als u eenvoudig Greenshot en het idee van open source software goed vindt:<br />
		Overweeg een donatie ter eren van onze inspanningen.<br><br />
		Kijk AUB op onze home page om te lezen hoe u het Greenshot ontwikkelteam kunt ondersteunen:<br><br />
		<a href="/support/">http://getgreenshot.org/support/</a>
	</p>
<p>	<a name="help-spread"></a></p>
<h3>Vertel het door</h3>
<p>
		Als u Greenshot goed vind, vertel het door: vertel uw vrienden en kollegas over Greenshot.<br />
		Uw aanhang en achterban ook! :)<br><br />
		Geef Greenshot goede kritiek in software portals of verlink Greenshot in uw blog of website.
	</p>
<p>	<a name="help-translate"></a></p>
<h3>Maak een vertaling</h3>
<p>
		Greenshot is niet in uw favorite taal vertaald? Als u denkt dat u software kunt vertalen dan bent u welkom!<br />
		Als u een geregisteerde gebruiker bij sourceforge.net bent kunt u uw vertaling in onze<br />
		<a href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">vertalings tracker</a> bekent maken.<br><br />
		Voordat u begin is het verstandig om te kijken of Greenshot niet al in de taal vertaalt is, zie de<br />
		<a href="#">downloads pagina</a>. Ook kunt u op onze <a href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">vertalings tracker</a> kijken,<br />
		het zou kunnen dat al iemand aan de vertaling werkt of misschien hierover een diskussie heeft.<br></p>
<p>		Een belangrijk punt is dat we vertalingen alleen op onze website zetten als het door iemand vertaalt is die<br />
		ook een sourceforge.net gebruiker is. Omdat we zeer waarschijnlijk de vertaling niet begrijpen is het nodig<br />
		dat we de gebruiker kunnen vinden, alleen dan kunnen we bij nieuwe Greenshot versies ook deze taal weer uitleveren.
	</p>
