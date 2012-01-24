/// Javscript logic for the Greenshot extension
var greenshotPrefObserver = {
	register: function() {
		// First we'll need the preference services to look for preferences.
		var prefService = Components.classes["@mozilla.org/preferences-service;1"].getService(Components.interfaces.nsIPrefService);
		// For this._branch we ask that the preferences for extensions.myextension. and children
		this._branch = prefService.getBranch("extensions.greenshot.");
		// Now we queue the interface called nsIPrefBranch2. This interface is described as:    
		// "nsIPrefBranch2 allows clients to observe changes to pref values."  
		this._branch.QueryInterface(Components.interfaces.nsIPrefBranch2);
		// Finally add the observer.  
		this._branch.addObserver("", this, false);
	},
	unregister: function() {
	if (!this._branch) return;
	this._branch.removeObserver("", this);
	},
	readDestination: function() {
		return "http://" + this._branch.getCharPref("host") + ":" + this._branch.getIntPref("port");
	},
	observe: function(aSubject, aTopic, aData) {
		if(aTopic != "nsPref:changed") return;
		// aSubject is the nsIPrefBranch we're observing (after appropriate QI)
		// aData is the name of the pref that's been changed (relative to aSubject)
		switch (aData) {
			case "host":
				this.readDestination();
				break;
			case "port":
				this.readDestination();
				break;
		}
	}
}
greenshotPrefObserver.register();

function doMenuClick() {
	capture();
}
function doStatusbarClick() {
	capture();
}

function sendImage(dataUrl, title) {
	var destination = greenshotPrefObserver.readDestination();
	try {
		// Send the image via a HTTP Request to our localhost
		$.ajax({
			cache: false,
			type: "POST",
			contentType: "image/png;base64",
			dataType: "text",
			processData: false,
			data: dataUrl,
			url: destination + '?title=' + escape(encodeUTF8(title)),
			success : function (text) {
				alert("Sent image to: " + destination);
				//document.getElementById("statusbar-display").label = "Greenshot: " + text;
			},
			error : function () {
				alert("Couldn't send image to '" + destination + "' please check if Greenshot is running!");
			}
		});
	} catch (exception) {
		alert(exception.message);
	}
}
///
/// Capture the current opened window
/// and send it to Greenshot
///
function capture() {
	// Used variables
	var width;
	var height;
	var xOffset;
	var yOffset;
	var context;

	try {
		// Get document width & height
		width = $(window.content.document).width();
		height = $(window.content.document).height();
		
		// Create canvas
		var canvas = window.content.document.createElement('canvas');
		
		// change the canvas size to the document size
		canvas.setAttribute('width', width);
		canvas.setAttribute('height', height);

		// Get the drawing context
		context = canvas.getContext("2d");

		// Set the scroll location to top-left, so we don't have problems with funny banners that move while scrolling
		xOffset = window.content.window.scrollX;
		yOffset = window.content.window.scrollY;
		window.content.window.scrollTo(0,0);
		
	} catch (exception) {
	}
	// continue after timeout, so the page is scrolled correctly and has time to move it's items, e.g. for Jira
	setTimeout(	function(){
		try {
			// Draw the window to the context
			context.drawWindow(window.content.window, 0, 0, width, height, 'rgb(255,255,255)');
			window.content.window.scrollTo(xOffset,yOffset);

			// Send the canvas
			
			sendImage(canvas.toDataURL("image/png"), window.content.document.title);
		} catch (exception) {
			alert(exception.message);
		}
	}, 300);
}

function captureImage(imageToCapture) {
	try {
		var canvas = window.content.document.createElement('canvas');
		
		// change the canvas size to the document size
		canvas.setAttribute('width', imageToCapture.width);
		canvas.setAttribute('height', imageToCapture.height);

		// Get the drawing context
		context = canvas.getContext("2d");
		context.drawImage(imageToCapture, 0, 0);
		
		// Get filename from url
		var title
		if (imageToCapture.alt) {
			title =imageToCapture.alt;
		} else {
			title = imageToCapture.src.replace(/\?.*/,'').replace(/^.*(\\|\/|\:)/, '').replace(/\.[^.]*/,'');
		}
		// Send the canvas
		sendImage(canvas.toDataURL("image/png"), title);
	} catch (exception) {
		alert(exception.message);
	}
}

// private method for UTF-8 encoding
function encodeUTF8(string) {
	string = string.replace(/\r\n/g,"\n");
	var utftext = "";
	for (var n = 0; n < string.length; n++) {
		var c = string.charCodeAt(n);
		if (c < 128) {
			utftext += String.fromCharCode(c);
		} else if((c > 127) && (c < 2048)) {
			utftext += String.fromCharCode((c >> 6) | 192);
			utftext += String.fromCharCode((c & 63) | 128);
		} else {
			utftext += String.fromCharCode((c >> 12) | 224);
			utftext += String.fromCharCode(((c >> 6) & 63) | 128);
			utftext += String.fromCharCode((c & 63) | 128);
		}
	}
	return utftext;
}

function ShowHideGreenshotMenuItem(event) {
	try {
		gContextMenu.showItem("greenshot-menu-item", gContextMenu.onImage);
	} catch (exception) {
		alert(exception.message);
	}
}

function InitializeGreenshotExtension() {
	try {
		var contextMenu = document.getElementById("contentAreaContextMenu");
		if (contextMenu) {
			contextMenu.addEventListener("popupshowing", ShowHideGreenshotMenuItem, false);
		}
	} catch (exception) {
		alert(exception.message);
	}
}
window.addEventListener("load", InitializeGreenshotExtension, false);
