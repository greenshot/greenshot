function sendCaptureToGreenshot(dataURL) {
	window.console.info('Sending data...');
	$.ajax({
			cache: false,
			type: "POST",
			contentType: "image/png;base64",
			dataType: "text",
			processData: false,
			data: dataURL,
			url: 'http://localhost:11234',
			success : function (text) {
				window.console.info('Got: ' + text);
			},
			error : function () {
				alert("Couldn't send capture, please check if Greenshot is running!");
			}
	});
}

function capture() {
	window.console.info('Starting capture');
	try {
		chrome.tabs.captureVisibleTab(null, {format:'png'}, captureTaken);
	} catch(exception) {
		alert( exception.toString());
	}
}

chrome.browserAction.onClicked.addListener(function(tab) {capture();}); 
