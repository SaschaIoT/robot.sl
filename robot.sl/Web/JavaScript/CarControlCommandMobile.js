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

function processTouches(touches, isTouchmove) {
    var touchedElements = new Array();
    
    for (var touchId = 0; touchId < touches.length; touchId++) {

        var touch = touches.item(touchId);

        var element;
        if (isTouchmove === true) {
            element = document.elementFromPoint(touch.clientX, touch.clientY);
        }
        else {
            element = touch.target;
        }

        if (element === undefined || element === null)
            continue;

        if (element.id === directionControlUp.id) {
            
            touchedElements.push(directionControlUp.id);
            directionControlUp.classList.add("up-arrow-hover");
            directionControlUpCurrent = true;

        } else if (element.id === directionControlLeft.id) {

            touchedElements.push(directionControlLeft.id);
            directionControlLeft.classList.add("left-arrow-hover");
            directionControlLeftCurrent = true;

        } else if (element.id === directionControlRight.id) {

            touchedElements.push(directionControlRight.id);
            directionControlRight.classList.add("right-arrow-hover");
            directionControlRightCurrent = true;

        } else if (element.id === directionControlDown.id) {

            touchedElements.push(directionControlDown.id);
            directionControlDown.classList.add("down-arrow-hover");
            directionControlDownCurrent = true;

        } else if (element.id === speedControlForward.id) {

            touchedElements.push(speedControlForward.id);
            speedControlForward.classList.add("up-arrow-hover");
            speedControlForwardCurrent = true;

        } else if (element.id === speedControlBackward.id) {

            touchedElements.push(speedControlBackward.id);
            speedControlBackward.classList.add("down-arrow-hover");
            speedControlBackwardCurrent = true;
        }
    }

    if (!arrayContains("direction-control-up", touchedElements)) {
        directionControlUp.classList.remove("up-arrow-hover");
        directionControlUpCurrent = false;
    }

    if (!arrayContains("direction-control-left", touchedElements)) {
        directionControlLeft.classList.remove("left-arrow-hover");
        directionControlLeftCurrent = false;
    }

    if (!arrayContains("direction-control-right", touchedElements)) {
        directionControlRight.classList.remove("right-arrow-hover");
        directionControlRightCurrent = false;
    }

    if (!arrayContains("direction-control-down", touchedElements)) {
        directionControlDown.classList.remove("down-arrow-hover");
        directionControlDownCurrent = false;
    }

    if (!arrayContains("speed-control-forward", touchedElements)) {
        speedControlForward.classList.remove("up-arrow-hover");
        speedControlForwardCurrent = false;
    }

    if (!arrayContains("speed-control-backward", touchedElements)) {
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
        speedControlBackward: speedControlBackwardCurrent
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
document.addEventListener('touchmove', function (event) {

    processTouches(event.touches, true);

}, false);

document.addEventListener('touchstart', function (event) {

    processTouches(event.touches, false);
    preventDefaults(event);

}, false);

document.addEventListener('touchend', function (e) {

    processTouches(e.touches, false);
    preventDefaults(e);

}, false);

document.addEventListener('touchcancel', function (e) {

    processTouches(e.touches, false);
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