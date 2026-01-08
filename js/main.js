
// Function called if AdBlock is detected
function adBlockDetected() {
    $('.ga-ldrbrd').append('<a href="/support/"><img src="/assets/bab-banner.png"></a>');
    $('.ga-skscrpr').append('<a href="/support/"><img src="/assets/bab-skyscraper.png"></a>');
    $('.ga-skscrpr-wrapper').hide();
}

// Recommended audit because AdBlock lock the file 'blockadblock.js' 
// If the file is not called, the variable does not exist 'blockAdBlock'
// This means that AdBlock is present
if(typeof blockAdBlock === 'undefined') {
    adBlockDetected();
} else {
    blockAdBlock.onDetected(adBlockDetected);
    //blockAdBlock.onNotDetected(adBlockNotDetected);
}	
