var webSocketServerVideoFrameRate;
var serverVideoFrameRateTime;

function GetServerVideoFrameRate() {

    webSocketServerVideoFrameRate = new WebSocket('ws://192.168.0.101:80/Controller');

    webSocketServerVideoFrameRate.onopen = function () {
        webSocketHelper.waitUntilWebsocketReady(function () {
            webSocketServerVideoFrameRate.send(JSON.stringify({ command: "ServerVideoFrameRate" }));
        }, webSocketServerVideoFrameRate, 0);
    };

    webSocketServerVideoFrameRate.onmessage = function () {

        var serverVideoFrameRate = JSON.parse(event.data).parameter;

        UpdateServerVideoFrameRate(serverVideoFrameRate);

        webSocketHelper.waitUntilWebsocketReady(function () {
            webSocketServerVideoFrameRate.send(JSON.stringify({ command: "ServerVideoFrameRate" }));
        }, webSocketServerVideoFrameRate, 0);

        serverVideoFrameRateTime = new Date().getTime();
    };
}

function UpdateServerVideoFrameRate(serverVideoFrameRate) {
    console.log("Server video frame rate: " + serverVideoFrameRate.FrameRate)
}

function KeepAliveGetServerVideoFrameRate() {

    var duration = 0;
    if (serverVideoFrameRateTime !== undefined) {
        duration = new Date().getTime() - serverVideoFrameRateTime
    }

    if (serverVideoFrameRateTime !== undefined
        && duration <= requestTimeout) {

        setTimeout(function () {
            KeepAliveGetServerVideoFrameRate();
        }, 50);
    } else {

        if (webSocketServerVideoFrameRate !== undefined) {
            try {
                webSocketServerVideoFrameRate.close();
            } catch (e) { }
        }

        GetServerVideoFrameRate();

        serverVideoFrameRateTime = new Date().getTime();

        setTimeout(function () {
            KeepAliveGetServerVideoFrameRate();
        }, 1000);
    }
}

KeepAliveGetServerVideoFrameRate();