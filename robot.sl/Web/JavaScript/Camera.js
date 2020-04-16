var webSocketVideoFrame = [];
var frameTime = [];
var streamWebCamElement = document.querySelector("#streamWebCam");
var lastTimestamp = 0;
var timestampLength = 8;
var imageMimeType = "image/jpeg";

DataView.prototype.getUint64 = function (byteOffset, littleEndian) {
    const left = this.getUint32(byteOffset, littleEndian);
    const right = this.getUint32(byteOffset + 4, littleEndian);
    const combined = new Number(littleEndian ? left + 2 ** 32 * right : 2 ** 32 * left + right);

    return combined;
};

function DisplayVideoFrame(imageData) {
    var imageUrl = URL.createObjectURL(imageData);
    imageUrl.load = () => {
        URL.revokeObjectURL(imageUrl);
    };
    streamWebCamElement.src = imageUrl;
}

function GetVideoFrames(id) {

    webSocketVideoFrame[id] = new WebSocket('ws://192.168.0.101:80/VideoFrame');
    webSocketVideoFrame[id].binaryType = "arraybuffer";

    webSocketVideoFrame[id].onmessage = function (e) {

        if (frameTime[id] !== undefined) {
            UpdateLatency(frameTime[id], false);
        }

        var data = e.data;
        var timestamp = new DataView(data.slice(0, timestampLength)).getUint64(0, true);

        if (timestamp > lastTimestamp) {
            lastTimestamp = timestamp;

            //if (frameTime[id]) {
            //    console.log(new Date().getTime() - frameTime[id]);
            //}

            frameTime[id] = new Date().getTime();

            let frame = new Blob([data.slice(timestampLength, data.byteLength)], { type: imageMimeType });
            DisplayVideoFrame(frame);
        }
    };
}

function KeepAliveGetVideoFrames(id) {

    var duration = 0;
    if (frameTime[id] !== undefined) {
        duration = new Date().getTime() - frameTime[id];
    }

    if (frameTime[id] !== undefined && duration <= requestTimeout) {
        setTimeout(function () {
            KeepAliveGetVideoFrames(id);
        }, 50);
    } else {

        if (webSocketVideoFrame[0] !== undefined) {
            try {
                webSocketVideoFrame[0].close();
            } catch (e) { }
        }

        if (frameTime[id] !== undefined) {
            UpdateLatency(frameTime[id], true);
        } else if (!frameTime[0] && !frameTime[1] && !frameTime[2]) {
            UpdateLatency(2001, true);
        }

        GetVideoFrames(id);

        setTimeout(function () {
            KeepAliveGetVideoFrames(id);
        }, 4000);
    }
}

KeepAliveGetVideoFrames(0);
KeepAliveGetVideoFrames(1);
KeepAliveGetVideoFrames(2);