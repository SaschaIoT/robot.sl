function GetServerVideoFrameRate() {

    _lastImageRequest = new Date();
    _imageRequestStart = new Date().getTime();

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "http://192.168.0.101/ServerVideoFrameRate" + new Date().getTime().toString(), true);
    xhr.responseType = "json";

    xhr.timeout = xhttpRequestTimeout;
    xhr.ontimeout = function () {
        setTimeout(function () {
            GetServerVideoFrameRate();
        }, 50);
    }

    xhr.onerror = function () {
        
        setTimeout(function () {
            GetServerVideoFrameRate();
        }, 50);
    }

    xhr.onload = function () {

        if (xhr.readyState == 4) {
            if (xhr.status === 200) {
                var frameRate = xhr.response;
                 console.log("Server video frame rate: " + frameRate.FrameRate)
            }
        }

        setTimeout(function () {
            GetServerVideoFrameRate();
        }, 50);
    }

    xhr.send();
}

GetServerVideoFrameRate();