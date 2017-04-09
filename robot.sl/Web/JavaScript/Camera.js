var _lastImageRequest = new Date();
var _imageRequestStart;
function StreamWebCam() {

    _lastImageRequest = new Date();
    _imageRequestStart = new Date().getTime();

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "http://192.168.0.101/VideoFrame" + new Date().getTime().toString() + ".jpeg", true);
    xhr.responseType = "arraybuffer";

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

        if (xhr.status === 200) {
            var blob = new Blob([xhr.response], { type: "image/jpeg" });
            var url = webkitURL.createObjectURL(blob);
            document.querySelector("#streamWebCam").src = url;
        }

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