var webSocketVideoFrame;
var frameTime;
var streamWebCamElement = document.querySelector("#streamWebCam");
var lastImageUrl;

function GetVideoFrames() {

    webSocketVideoFrame = new WebSocket('ws://192.168.0.101:80/VideoFrame');
    webSocketVideoFrame.binaryType = "arraybuffer";

    webSocketVideoFrame.onopen = function () {
        webSocketHelper.waitUntilWebsocketReady(function () {
            webSocketVideoFrame.send(JSON.stringify({ command: "VideoFrame" }));
        }, webSocketVideoFrame, 0);
    };

    webSocketVideoFrame.onmessage = function () {

        if (frameTime !== undefined)
            UpdateLatency(frameTime, false);

        var bytearray = new Uint8Array(event.data);

        var blob = new Blob([event.data], { type: "image/jpeg" });
        lastImageUrl = createObjectURL(blob);
        streamWebCamElement.src = lastImageUrl;

        frameTime = new Date().getTime();
    };
}

streamWebCamElement.addEventListener("load", function (e) {
    URL.revokeObjectURL(lastImageUrl);

    webSocketHelper.waitUntilWebsocketReady(function () {
        webSocketVideoFrame.send(JSON.stringify({ command: "VideoFrame" }));
    }, webSocketVideoFrame, 0);
});

function createObjectURL(blob) {
    if (window.webkitURL) {
        return window.webkitURL.createObjectURL(blob);
    } else if (window.URL && window.URL.createObjectURL) {
        return window.URL.createObjectURL(blob);
    } else {
        return null;
    }
}

function KeepAliveGetVideoFrames() {

    var duration = 0;
    if (frameTime !== undefined) {
        duration = new Date().getTime() - frameTime
    }

    if (frameTime !== undefined
        && duration <= requestTimeout) {

        setTimeout(function () {
            KeepAliveGetVideoFrames();
        }, 50);
    } else {

        if (webSocketVideoFrame !== undefined) {
            try {
                webSocketVideoFrame.close();
            } catch (e) { }
        }

        if (frameTime !== undefined) {
            UpdateLatency(duration, true);
        } else {
            UpdateLatency(2001, true);
        } 

        GetVideoFrames();

        setTimeout(function () {
            KeepAliveGetVideoFrames();
        }, 4000);
    }
}

KeepAliveGetVideoFrames();