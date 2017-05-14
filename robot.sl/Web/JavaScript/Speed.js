var webSocketSpeed;
var speedTime;

function GetSpeed() {

    webSocketSpeed = new WebSocket('ws://192.168.0.101:80/Controller');

    webSocketSpeed.onopen = function () {
        webSocketHelper.waitUntilWebsocketReady(function () {
            webSocketSpeed.send(JSON.stringify({ command: "Speed" }));
        }, webSocketSpeed, 0);
    };

    webSocketSpeed.onmessage = function () {

        var speed = JSON.parse(event.data).parameter;

        UpdateSpeed(speed);

        webSocketHelper.waitUntilWebsocketReady(function () {
            webSocketSpeed.send(JSON.stringify({ command: "Speed" }));
        }, webSocketSpeed, 0);

        speedTime = new Date().getTime();
    };
}

function UpdateSpeed(speed) {
    document.getElementById("roundsPerMinute").innerHTML = speed.RoundsPerMinute;
    document.getElementById("kilometerPerHour").innerHTML = speed.KilometerPerHour;
}

function KeepAliveGetSpeed() {

    var duration = 0;
    if (speedTime !== undefined) {
        duration = new Date().getTime() - speedTime
    }

    if (speedTime !== undefined
        && duration <= requestTimeout) {

        setTimeout(function () {
            KeepAliveGetSpeed();
        }, 50);
    } else {

        if (webSocketSpeed !== undefined) {
            try {
                webSocketSpeed.close();
            } catch (e) { }
        }

        GetSpeed();

        speedTime = new Date().getTime();
        KeepAliveGetSpeed();
    }
}

KeepAliveGetSpeed();