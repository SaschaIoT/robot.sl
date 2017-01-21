var _lastImageRequest = new Date();
var _imageRequestStart;
function StreamWebCam() {

    _lastImageRequest = new Date();
    _imageRequestStart = new Date().getTime();

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "VideoFrameTime=" + new Date().getTime().toString() + ".html", true);
    xhr.responseType = "blob";

    xhr.onreadystatechange = function () {
        if (xhr.readyState == 4) {
            if (xhr.status === 200) {
                var urlCreator = window.URL || window.webkitURL;
                var imageUrl = urlCreator.createObjectURL(xhr.response);
                document.querySelector("#streamWebCam").src = imageUrl;
            }
        }
    }

    xhr.timeout = xhttpRequestTimeout;
    xhr.ontimeout = function () {
        ProcessGlobalRequestTime(_imageRequestStart, true);
        StreamWebCamLimited();
    }

    xhr.onerror = function () {
        ProcessGlobalRequestTime(_imageRequestStart, true);
        StreamWebCamLimited();
    }

    xhr.onload = function () {
        ProcessGlobalRequestTime(_imageRequestStart, false);
        StreamWebCamLimited();
    }

    xhr.send();
}

function StreamWebCamLimited() {
    var millisecondsSinceLastRequest = new Date() - _lastImageRequest;

    if (millisecondsSinceLastRequest >= getNewCameraFrameAfterMilliseconds) {
        setTimeout(function () { StreamWebCam(); }, 0);

    } else {
        var newRequestIn = getNewCameraFrameAfterMilliseconds - millisecondsSinceLastRequest;
        setTimeout(function () { StreamWebCam(); }, newRequestIn);
    }
}

StreamWebCam();