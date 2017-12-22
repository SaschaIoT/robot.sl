function workRemoveTouches(touches) {
    for (var touchId = 0; touchId < touches.length; touchId++) {
        var myLocation = touches.item(touchId);
        var element = document.elementFromPoint(myLocation.clientX, myLocation.clientY);

        if (element.id === directionControlUp.id) {
            directionControlUp.classList.remove("up-arrow-hover");
            directionControlUpCurrent = false;
        }

        if (element.id === directionControlLeft.id) {
            directionControlLeft.classList.remove("left-arrow-hover");
            directionControlLeftCurrent = false;
        }

        if (element.id === directionControlRight.id) {
            directionControlRight.classList.remove("right-arrow-hover");
            directionControlRightCurrent = false;
        }

        if (element.id === directionControlDown.id) {
            directionControlDown.classList.remove("down-arrow-hover");
            directionControlDownCurrent = false;
        }

        if (element.id === speedControlForward.id) {
            speedControlForward.classList.remove("up-arrow-hover");
            speedControlForwardCurrent = false;
        }

        if (element.id === speedControlBackward.id) {
            speedControlBackward.classList.remove("down-arrow-hover");
            speedControlBackwardCurrent = false;
        }
    }
}

var directionControlUpCurrent = false;
var directionControlLeftCurrent = false;
var directionControlRightCurrent = false;
var directionControlDownCurrent = false;
var speedControlForwardCurrent = false;
var speedControlBackwardCurrent = false;

var directionControlUp = document.getElementById("direction-control-up");
var directionControlLeft = document.getElementById("direction-control-left");
var directionControlRight = document.getElementById("direction-control-right");
var directionControlDown = document.getElementById("direction-control-down");
var speedControlForward = document.getElementById("speed-control-forward");
var speedControlBackward = document.getElementById("speed-control-backward");

function workTouches(touches) {
    var touchedElemnts = new Array();

    for (var touchId = 0; touchId < touches.length; touchId++) {

        var myLocation = touches.item(touchId);
        var element = document.elementFromPoint(myLocation.clientX, myLocation.clientY);

        if (element !== undefined && element.id !== undefined) {

            if (element.id === directionControlUp.id) {

                touchedElemnts.push(directionControlUp.id);
                directionControlUp.classList.add("up-arrow-hover");
                directionControlUpCurrent = true;

            }

            if (element.id === directionControlLeft.id) {

                touchedElemnts.push(directionControlLeft.id);
                directionControlLeft.classList.add("left-arrow-hover");
                directionControlLeftCurrent = true;
            }

            if (element.id === directionControlRight.id) {

                touchedElemnts.push(directionControlRight.id);
                directionControlRight.classList.add("right-arrow-hover");
                directionControlRightCurrent = true;
            }

            if (element.id === directionControlDown.id) {

                touchedElemnts.push(directionControlDown.id);
                directionControlDown.classList.add("down-arrow-hover");
                directionControlDownCurrent = true;
            }

            if (element.id === speedControlForward.id) {

                touchedElemnts.push(speedControlForward.id);
                speedControlForward.classList.add("up-arrow-hover");
                speedControlForwardCurrent = true;

            }

            if (element.id === speedControlBackward.id) {

                touchedElemnts.push(speedControlBackward.id);
                speedControlBackward.classList.add("down-arrow-hover");
                speedControlBackwardCurrent = true;
            }
        }
    }

    if (!arrayContains("direction-control-up", touchedElemnts)) {
        directionControlUp.classList.remove("up-arrow-hover");
        directionControlUpCurrent = false;
    }

    if (!arrayContains("direction-control-left", touchedElemnts)) {
        directionControlLeft.classList.remove("left-arrow-hover");
        directionControlLeftCurrent = false;
    }

    if (!arrayContains("direction-control-right", touchedElemnts)) {
        directionControlRight.classList.remove("right-arrow-hover");
        directionControlRightCurrent = false;
    }

    if (!arrayContains("direction-control-down", touchedElemnts)) {
        directionControlDown.classList.remove("down-arrow-hover");
        directionControlDownCurrent = false;
    }

    if (!arrayContains("speed-control-forward", touchedElemnts)) {
        speedControlForward.classList.remove("up-arrow-hover");
        speedControlForwardCurrent = false;
    }

    if (!arrayContains("speed-control-backward", touchedElemnts)) {
        speedControlBackward.classList.remove("down-arrow-hover");
        speedControlBackwardCurrent = false;
    }
}

function arrayContains(value, array) {
    return (array.indexOf(value) > -1);
}

function preventDefaults(e) {
    e = e || window.event;
    var target = e.target || e.srcElement;
    if (!target.className.match(/\baltNav\b/)) {
        e.returnValue = false;
        e.cancelBubble = true;
        if (e.preventDefault) {
            e.preventDefault();
        }
        return false;
    }
}

function SendCarControlCommand() {

    var carControlCommand = {
        directionControlUp: directionControlUpCurrent,
        directionControlLeft: directionControlLeftCurrent,
        directionControlRight: directionControlRightCurrent,
        directionControlDown: directionControlDownCurrent,
        speedControlForward: speedControlForwardCurrent,
        speedControlBackward: speedControlBackwardCurrent,
        speedControlLeftRight: 1
    };
    
    webSocketHelper.waitUntilWebsocketReady(function () {
        webSocketCarControlCommand.send(JSON.stringify({ command: "CarControlCommand", parameter: carControlCommand }));
    }, webSocketCarControlCommand, 0);
}

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
document.body.addEventListener('touchmove', function (event) {

    workTouches(event.touches);

    preventDefaults(event);

}, true);

document.body.addEventListener('touchstart', function (event) {

    workTouches(event.touches);

    preventDefaults(event);

}, true);

document.addEventListener('touchend', function (e) {

    workRemoveTouches(e.changedTouches);
    preventDefaults(e);

}, false);

document.addEventListener('touchcancel', function (e) {

    workRemoveTouches(e.changedTouches);
    preventDefaults(e);

}, false);

window.onblur = function () {
    directionControlUpCurrent = false;
    directionControlLeftCurrent = false;
    directionControlRightCurrent = false;
    directionControlDownCurrent = false;
    speedControlForwardCurrent = false;
    speedControlBackwardCurrent = false;
};