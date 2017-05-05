document.addEventListener(fullScreenAPI.fullScreenEventName, function () {
    setFullScreenButton();
});

var fullscreenButton = document.getElementById("fullscreenButton");
fullscreenButton.addEventListener("click", function (e) {

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