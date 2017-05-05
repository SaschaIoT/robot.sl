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

document.addEventListener(fullScreenAPI.fullScreenEventName, function () {
    setFullScreenButton();
});

function setFullScreenButton() {

    if (fullScreenAPI.supportFullScreen === false) {
        return;
    }

    if (fullScreenAPI.isFullScreen()) {
        fullscreenButton.classList.add("fullscreen-active-button");
    } else {
        fullscreenButton.classList.remove("fullscreen-active-button");
    }
}

fullscreenButton.addEventListener("touchstart", function (e) {

    if (fullScreenAPI.supportFullScreen === false) {
        return;
    }

    e.preventDefault();
    e.stopPropagation();

    if (fullScreenAPI.isFullScreen() === false) {
        fullScreenAPI.requestFullScreen(document.documentElement)
    } else {
        fullScreenAPI.cancelFullScreen();
    }
});