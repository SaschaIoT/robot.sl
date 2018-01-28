var directionControlUpCurrent = false;
var directionControlLeftCurrent = false;
var directionControlRightCurrent = false;
var directionControlDownCurrent = false;
var speedControlForwardCurrent = false;
var speedControlBackwardCurrent = false;

function SendCarControlCommand() {

    var carControlCommand = {
        directionControlUp: directionControlUpCurrent,
        directionControlLeft: directionControlLeftCurrent,
        directionControlRight: directionControlRightCurrent,
        directionControlDown: directionControlDownCurrent,
        speedControlForward: speedControlForwardCurrent,
        speedControlBackward: speedControlBackwardCurrent
    };

    webSocketHelper.waitUntilWebsocketReady(function () {
        webSocketCarControlCommand.send(JSON.stringify({ command: "CarControlCommand", parameter: carControlCommand }));
    }, webSocketCarControlCommand, 0);
}

function Stop() {
    directionControlUpCurrent = false;
    directionControlLeftCurrent = false;
    directionControlRightCurrent = false;
    directionControlDownCurrent = false;
    speedControlForwardCurrent = false;
    speedControlBackwardCurrent = false;
}

function PointerLockChanged() {
    if (!(document.pointerLockElement === document.body ||
        document.mozPointerLockElement === document.body ||
        document.webkitPointerLockElement === document.body)) {

        Stop();
    }
}

window.onblur = function () {
    Stop();
};

var webSocketCarControlCommand;
var carControlCommandTime;

function GetCarControlCommand() {

    webSocketCarControlCommand = new WebSocket('ws://192.168.0.101:80/Controller');

    webSocketCarControlCommand.onopen = function () {
        SendCarControlCommand();
    };

    webSocketCarControlCommand.onmessage = function () {

        setTimeout(function () {
            SendCarControlCommand();
        }, 40);

        carControlCommandTime = new Date().getTime();
    };
}

function KeepAliveCarControlCommand() {

    var duration = 0;
    if (carControlCommandTime !== undefined) {
        duration = new Date().getTime() - carControlCommandTime
    }

    if (carControlCommandTime !== undefined
        && duration <= requestTimeout) {

        setTimeout(function () {
            KeepAliveCarControlCommand();
        }, 50);
    } else {

        if (webSocketCarControlCommand !== undefined) {
            try {
                webSocketCarControlCommand.close();
            } catch (e) { }
        }

        GetCarControlCommand();

        setTimeout(function () {
            KeepAliveCarControlCommand();
        }, 4000);
    }
}

KeepAliveCarControlCommand();

//Event listener
document.body.onkeydown = function (event) {

    if (document.pointerLockElement === document.body ||
        document.mozPointerLockElement === document.body ||
        document.webkitPointerLockElement === document.body) {

        event = event || window.event;
        var keycode = event.charCode || event.keyCode;
        if (keycode === 87) { //W
            speedControlForwardCurrent = true;
        } else if (keycode === 65) { //A
            directionControlLeftCurrent = true;
        } else if (keycode === 83) { //S
            speedControlBackwardCurrent = true;
        } else if (keycode === 68) { //D
            directionControlRightCurrent = true;
        } else if (keycode === 69) { //E
            directionControlUpCurrent = true;
        } else if (keycode === 81) { //Q
            directionControlDownCurrent = true;
        }
    }
}

document.body.onkeyup = function (event) {

    if (document.pointerLockElement === document.body ||
        document.mozPointerLockElement === document.body ||
        document.webkitPointerLockElement === document.body) {

        event = event || window.event;
        var keycode = event.charCode || event.keyCode;
        if (keycode === 87) { //W
            speedControlForwardCurrent = false;
        } else if (keycode === 65) { //A
            directionControlLeftCurrent = false;
        } else if (keycode === 83) { //S
            speedControlBackwardCurrent = false;
        } else if (keycode === 68) { //D
            directionControlRightCurrent = false;
        } else if (keycode === 69) { //E
            directionControlUpCurrent = false;
        } else if (keycode === 81) { //Q
            directionControlDownCurrent = false;
        }
    }
}

document.body.addEventListener("click", function (e) {

    if (!(document.pointerLockElement === document.body ||
        document.mozPointerLockElement === document.body ||
        document.webkitPointerLockElement === document.body)) {

        document.body.requestPointerLock = document.body.requestPointerLock ||
            document.body.mozRequestPointerLock ||
            document.body.webkitRequestPointerLock;

        document.body.requestPointerLock();
    }

}, false);

document.addEventListener('pointerlockchange', PointerLockChanged, false);
document.addEventListener('mozpointerlockchange', PointerLockChanged, false);
document.addEventListener('webkitpointerlockchange', PointerLockChanged, false);