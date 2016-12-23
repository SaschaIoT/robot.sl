function isFullScreen() {
    var fullScreen = document.fullScreenElement !== undefined
        || document.mozFullscreenEnabled
        || document.webkitIsFullScreen
        || document.msFullscreenElement ? true : false;

    return fullScreen;
}

document.addEventListener("webkitfullscreenchange", function () {

    setFullScreenButton();
});

document.addEventListener("mozfullscreenchange", function () {

    setFullScreenButton();
});

document.addEventListener("fullscreenchange", function () {

    setFullScreenButton();
});

document.addEventListener("MSFullscreenChange", function () {

    setFullScreenButton();
});

var fullscreenButton = document.getElementById("fullscreenButton");

function isiOSDevice() {

    var iDevices = [
      'iPad Simulator',
      'iPhone Simulator',
      'iPod Simulator',
      'iPad',
      'iPhone',
      'iPod'
    ];

    if (!!navigator.platform) {
        while (iDevices.length) {
            if (navigator.platform === iDevices.pop()) { return true; }
        }
    }

    return false;
}

if (isiOSDevice()) {
    fullscreenButton.style.display = "none";
}

fullscreenButton.addEventListener("touchstart", function (e) {

    e.preventDefault();
    e.stopPropagation();

    toggleFullScreen(document.body);
});

function setFullScreenButton() {

    if (isFullScreen()) {
        fullscreenButton.classList.add("fullscreen-active-button");
    } else {
        fullscreenButton.classList.remove("fullscreen-active-button");
    }
}

function toggleFullScreen(elem) {
    if ((document.fullScreenElement !== undefined && document.fullScreenElement === null) || (document.msFullscreenElement !== undefined && document.msFullscreenElement === null) || (document.mozFullScreen !== undefined && !document.mozFullScreen) || (document.webkitIsFullScreen !== undefined && !document.webkitIsFullScreen)) {
        if (elem.requestFullScreen) {
            elem.requestFullScreen();
        } else if (elem.mozRequestFullScreen) {
            elem.mozRequestFullScreen();
        } else if (elem.webkitRequestFullScreen) {
            elem.webkitRequestFullScreen(Element.ALLOW_KEYBOARD_INPUT);
        } else if (elem.msRequestFullscreen) {
            elem.msRequestFullscreen();
        }
    } else {
        if (document.cancelFullScreen) {
            document.cancelFullScreen();
        } else if (document.mozCancelFullScreen) {
            document.mozCancelFullScreen();
        } else if (document.webkitCancelFullScreen) {
            document.webkitCancelFullScreen();
        } else if (document.msExitFullscreen) {
            document.msExitFullscreen();
        }
    }
}