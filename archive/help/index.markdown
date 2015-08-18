---
layout: page
status: publish
published: true
title: Help
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 366
wordpress_url: http://getgreenshot.org/
date: !binary |-
  MjAxMi0wNC0wOCAxODoyNDowMyArMDIwMA==
date_gmt: !binary |-
  MjAxMi0wNC0wOCAxNjoyNDowMyArMDIwMA==
categories: []
tags: []
comments: []
---
<p><small>Version 0.8<!-- - English translation of help content by YOUR_NAME--></small></p>
<h2>Contents</h2>
<ol>
<li><a href="#screenshot">Creating a screenshot</a></li>
<ol>
<li><a href="#capture-region">Capture region</a></li>
<li><a href="#capture-last-region">Capture last region</a></li>
<li><a href="#capture-window">Capture window</a></li>
<li><a href="#capture-fullscreen">Capture fullscreen</a></li>
<li><a href="#capture-ie">Capture Internet Explorer</a></li>
</ol>
<li><a href="#editor">Using the image editor</a></li>
<ol>
<li><a href="#editor-shapes">Drawing shapes</a></li>
<li><a href="#editor-text">Adding text</a></li>
<li><a href="#editor-highlight">Highlighting things</a></li>
<li><a href="#editor-obfuscate">Obfuscating things</a></li>
<li><a href="#editor-crop">Cropping the screenshot</a></li>
<li><a href="#editor-adding-graphics">Adding graphics to a screenshot</a></li>
<li><a href="#editor-reuse-elements">Re-using drawn elements</a></li>
<li><a href="#editor-export">Exporting the screenshot</a></li>
</ol>
<li><a href="#settings">The settings dialog</a></li>
<ol>
<li><a href="#settings-general">General settings</a></li>
<li><a href="#settings-capture">Capture settings</a></li>
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
<h2>Creating a screenshot</h2>
<p>
		You can create a screenshot either by using the <kbd>Print</kbd> key on your keyboard<br />
		or by right clicking the Greenshot icon in the systray.<br><br />
		There are several options for creating a screenshot:
	</p>
<p>	<a name="capture-region"></a></p>
<h3>Capture region <kbd>Print</kbd></h3>
<p>
		The region capture mode allows you to select a part of you screen to be shot.<br><br />
		After starting region mode, you will see a crosshair pointing out the mouse<br />
		position on the screen. Click and hold where you want one of the corners of your<br />
		screenshot to be. Still holding down the mouse button, drag the mouse to define<br />
		the rectangle to be shot. When the green rectangle covers the area you want to<br />
		be captured in your screenshot, release the mouse button.
	</p>
<p class="hint">
		You can use the <kbd>Space</kbd> key to switch between region and<br />
		<a href="#capture-window">window</a> mode.
	</p>
<p class="hint">
		If you want to capture an exact area, it might be easier to select the initial<br />
		screenshot area slightly larger and to <a href="#editor-crop">crop</a> the screenshot<br />
		afterwards using Greenshot's image editor.
	</p>
<p>	<a name="capture-last-region"></a></p>
<h3>Capture last region <kbd>Shift</kbd> + <kbd>Print</kbd></h3>
<p>
		If you did a <a href="#capture-region">region</a> or <a href="#capture-window">window</a> capture<br />
		before, you can capture the same region again using this option.
	</p>
<p>	<a name="capture-window"></a></p>
<h3>Capture window <kbd>Alt</kbd> + <kbd>Print</kbd></h3>
<p>
		Creates a screenshot of the window which is currently active.
	</p>
<p class="hint">
		The <a href="#settings">settings dialog</a> offers an option not to capture<br />
		the active window right away, but allowing you to select one interactively.<br />
		If this option is selected, you may select a window by clicking it (As in<br />
		<a href="#capture-region">region mode</a>, Greenshot will highlight the area<br />
		that will be captured).<br>If you want a child window to be captured (e.g. a browser<br />
		viewport (without toolsbars etc.) or a single frame of a web page using framesets)<br />
		point the mouse cursor to the window and hit the <kbd>PgDown</kbd> key. After<br />
		doing so, you can select child elements of the window to be captured.
	</p>
<p class="hint">
		Capturing context menus on their own is different: using the "Capture window"<br />
		shortcut would make the context menu disappear, and obviously the same would happen<br />
		if you used Greenshot's context menu in order to create the screenshot. If you want<br />
		to capture a context menu you have just brought up by right-clicking anything,<br />
		simply activate region mode <kbd>Print</kbd>, then press the <kbd>Space</kbd> key.
	</p>
<p>	<a name="capture-fullscreen"></a></p>
<h3>Capture fullscreen <kbd>Control</kbd> + <kbd>Print</kbd></h3>
<p>
		Creates a screenshot of the complete screen.
	</p>
<p>	<a name="capture-ie"></a></p>
<h3>Capture Internet Explorer <kbd>Control</kbd> + <kbd>Shift</kbd> + <kbd>Print</kbd></h3>
<p>
		Comfortably creates a screenshot of a web page currently opened in Internet Explorer.<br />
		Use Greenshot's context menu to select the Internet Explorer tab to capture, or hit<br />
		<kbd>Crtl</kbd> + <kbd>Shift</kbd> + <kbd>Print</kbd> to capture the active tab.
	</p>
<p>	<a name="editor"></a></p>
<h2>Using the image editor</h2>
<p>
		Greenshot comes with an easy-to-use image editor, providing a handy featureset<br />
		to add annotations or shapes to a screenshot. It even allows to highlight or<br />
		obfuscate parts of your screenshot.
	</p>
<p class="hint">
		Greenshot's image editor may not only be used for screenshots. You can also<br />
		open images for editing from a file or from clipboard. Simply right click<br />
		the Greenshot icon in the systray and select <em>Open image from file</em><br />
		or <em>Open image from clipboard</em>, respectively.
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
		Available shapes are: rectangle <kbd>R</kbd>, ellipse <kbd>E</kbd>, line <kbd>L</kbd>,<br />
		arrow <kbd>A</kbd> and freehand line <kbd>F</kbd>.<br><br />
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
<p class="hint">
		If you want to draw equilateral shapes (e.g. force a rectangle to be a square) hold<br />
		down <kbd>Shift</kbd> while drawing. When drawing lines or arrows, holding down <kbd>Shift</kbd><br />
		results in the line's angle being rounded in steps 15°.<br><br />
		You can also use <kbd>Shift</kbd> if you want to resize an existing object maintaining it's aspect ratio.
	</p>
<p class="hint">
		When drawing or scaling, you can hold down <kbd>Ctrl</kbd> to have the object anchored in<br />
		it's geometrical middle. I.e. the object is resized in the opposite direction, too. (This<br />
		is very handy if you want to draw an ellipse around something on your screenshot.)
	</p>
<p>	<a name="editor-text"></a></p>
<h3>Adding text</h3>
<p>
		Usage of the text tool <kbd>T</kbd> is similar to the usage of the<br />
		<a href="#editor-shapes">shape</a> tools. Just draw the text element to the desired<br />
		size, then type in the text.<br><br />
		Double click an existing text element to edit the text.<br><br />
		Hit <kbd>Return</kbd> or <kbd>Enter</kbd> when you have finished editing.
	</p>
<p class="hint">
		If you need to insert line breaks within a text box, hit <kbd>Shift</kbd> + <kbd>Return</kbd> or<br />
		<kbd>Shift</kbd> + <kbd>Enter</kbd>.
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
<p class="hint">
		<em>Auto-Crop</em>: If you need to crop a border of solid background color from your screenshot,<br />
		simply choose <em>Auto-Crop</em> from the <em>Edit</em> menu and Greenshot will automatically<br />
		select the area for cropping.
	</p>
<p>	<a name="editor-adding-graphics"></a></p>
<h3>Adding graphics to a screenshot</h3>
<p>
		You can simply add graphics or images to your screenshot by dragging and dropping an image<br />
		file into the editor window. You can also insert screenshots of other windows by selecting<br />
		<em>Insert window</em> from the <em>Edit</em> menu. A list of all open windows appears,<br />
		allowing you to select one for insertion.
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
			You can download additional language files for Greenshot <a target="_blank" href="/downloads/">here</a>. </li>
<li><em>Launch Greenshot on startup</em>: Start the program when the system has been booted.</li>
<li><em>Hotkeys</em>: Customize the hotkeys to be used to create screenshots.</li>
<li><em>Use default system proxy</em>: If checked, Greenshot uses the default system proxy to check for updates.</li>
<li><em>Update check interval in days</em>: Greenshot can check for updates automatically. Use this setting to adjust the<br />
			interval (in days) or set it to 0 to turn off update checks.</li>
</ul>
<p>	<a name="settings-capture"></a></p>
<h3>Capture settings</h3>
<ul>
<li><em>Capture mousepointer</em>: If checked, the mousepointer will be captured. The pointer is handled is a separate element in the editor, so that you can move or remove it later.</li>
<li><em>Play camera sound</em>: Audible feedback when doing a capture</li>
<li><em>Milliseconds to wait before capture</em>: Add a custom time lag before actually capturing the screen.</li>
<li><em>Use interactive window capture mode</em>: Instead of capturing the active window right away, interactive mode<br />
			allows you to select the window to capture. It is also possible to capture child windows, see <a href="#capture-window">window capture</a>.</li>
<li>
			<em>Aero style capture (Windows Vista / 7 only)</em>: If you are using Greenshot on Windows Vista or Windows 7 with aero-style windows enabled, you can<br />
			choose how transparent window borders are to be handled when creating a screenshot in window mode. Use this setting to avoid capturing elements in the<br />
			background shining through transparent borders.</p>
<ul>
<li><em>Auto</em>: Let Greenshot decide how to handle transparency.</li>
<li><em>As displayed</em>: Transparent borders are captured as displayed on screen.</li>
<li><em>Use default color</em>: A solid default color is applied instead of transparency.</li>
<li><em>Use custom color</em>: Pick a custom color to be applied instad of transparency.</li>
<li><em>Preserve transparency</em>: Borders are captured preserving transparency, not capturing elements which might be in the background. (Note: transparent<br />
					areas are displayed using a checked pattern in the editor. The pattern is not exported when saving the screenshot to a file. Keep in mind to save as PNG<br />
					file for full transparency support.)</li>
</ul>
</li>
<li><em>Internet Explorer capture</em>: Enable comfortable capturing of web pages using Internet Explorer.</li>
<li><em>Resize editor window to screenshot size</em>: If selected, the editor window will automatically be resized to fit the size of the screenshot.</li>
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
<li><em>Rotate printout to page orientation</em>: Will rotate a landscape format image by 90&deg; for printing.</li>
<li><em>Print with inverted colors</em>: Will invert the screenshot before printing it, useful e.g. when printing a screenshot of white text on black background (to save toner/ink).</li>
</ul>
<p>	<a name="settings-plugins"></a></p>
<h3>Plugin settings</h3>
<p>
		Displays a list of installed Greenshot plugins. Select one from the list and click <em>Configure</em> in order to access<br />
		the configuration of a plugin.
	</p>
<p>	<a name="help"></a></p>
<h2>Want to help?</h2>
<p>
		Currently, we do not need help in development. However, there are several things you<br />
		can do to support Greenshot and the development team.<br><br />
		Thanks in advance :)
	</p>
<p>	<a name="help-donate"></a></p>
<h3>Consider a donation</h3>
<p>
		We are putting a lot of work into Greenshot and spending quite some time to provide<br />
		a good piece of software for free and open source. If you feel<br />
		that it makes you more productive, if it saves you (or your company)<br />
		a lot of time and money, or if you simply like Greenshot and<br />
		the idea of open source software: please consider honoring our effort by donating.<br><br />
		Please have a look at our home page to see how you can support the Greenshot development team:<br><br />
		<a target="_blank" href="/support/">http://getgreenshot.org/support/</a>
	</p>
<p>	<a name="help-spread"></a></p>
<h3>Spread the word</h3>
<p>
		If you like Greenshot, let the people know: tell your friends and colleagues about Greenshot.<br />
		Your followers, too :)<br><br />
		Rate Greenshot in software portals or link to our home page from your blog or website.
	</p>
<p>	<a name="help-translate"></a></p>
<h3>Submit a translation</h3>
<p>
		Greenshot is not available in your preferred language? If you feel fit for translating<br />
		a piece of software, you are more than welcome.<br />
		If you are a registered user at sourceforge.net, you can submit translations to our<br />
		<a target="_blank" href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">translations tracker</a>.<br><br />
		Please make sure there is no existing translation for your language on our<br />
		<a target="_blank" href="/downloads/">downloads page</a>. Also check our <a href="https://sourceforge.net/tracker/?group_id=191585&atid=1368020">translations tracker</a>,<br />
		there might be a translation in progress, or at least in discussion.<br><br />
		Please note that we will only provide a translation on our downloads page if it has<br />
		been submitted through your sourceforge.net user account. Since we most probably are<br />
		not capable to understand your translation, it is good for other sourceforge users<br />
		to be able to contact you about improvements or enhancements in case of a new Greenshot<br />
		version.
	</p>
