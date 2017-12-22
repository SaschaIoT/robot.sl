//Key: false, Mouse: true
var speeControlLeftRight_LastKeyOrMouse = false;

var directionControlUpCurrent = false;
var directionControlLeftCurrent = false;
var directionControlRightCurrent = false;
var directionControlDownCurrent = false;
var speedControlForwardCurrent = false;
var speedControlBackwardCurrent = false;
var directionLefRightKeyIsMoving = false;

var leftKeyDown = false;
var rightKeyDown = false;

var mousemoveXPositive;
var mousemoveXNegative;
var mousemoveYNegative;
var mousemoveYPositive;

var mouseSennsitivy = 50;

var entryCoordinatesLastMoveX = 0;
var entryCoordinatesLastMoveY = 0;
var entryCoordinates = { x: 0, y: 0 };

var speedControlLeftRightMouseActionLast = new Date();
var speedControlLeftRightMouseActionLastMilliseconds = 0;

function SendCarControlCommand() {

    var speedControlLeftRight = 0;
    if (speeControlLeftRight_LastKeyOrMouse) {

        speedControlLeftRight = speedControlLeftRightMouseActionLastMilliseconds;
        if (speedControlLeftRight > 500) {
            speedControlLeftRight = 500;
        } else if (speedControlLeftRight < 0) {
            speedControlLeftRight = 0;
        }

        //Dynamic speed, equals to mouse speed
        speedControlLeftRight = 1 - (speedControlLeftRight / 500);

        speedControlLeftRight = Math.round(speedControlLeftRight * 100) / 100;

        //Min Speed
        if (speedControlLeftRight < 0.65) {
            speedControlLeftRight = 0.65;
        }

    } else {
        speedControlLeftRight = 1;
    }

    var carControlCommand = {
        directionControlUp: directionControlUpCurrent,
        directionControlLeft: directionControlLeftCurrent,
        directionControlRight: directionControlRightCurrent,
        directionControlDown: directionControlDownCurrent,
        speedControlForward: speedControlForwardCurrent,
        speedControlBackward: speedControlBackwardCurrent,
        speedControlLeftRight: speedControlLeftRight
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
    speedControlLeftRightMouseActionLastMilliseconds = 0;
    leftKeyDown = false;
    rightKeyDown = false;
    speeControlLeftRight_LastKeyOrMouse = false;
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
            leftKeyDown = true;
            directionLefRightKeyIsMoving = true;
            directionControlLeftCurrent = true;
            speeControlLeftRight_LastKeyOrMouse = false;
        } else if (keycode === 83) { //S
            speedControlBackwardCurrent = true;
        } else if (keycode === 68) { //D
            rightKeyDown = true;
            directionLefRightKeyIsMoving = true;
            directionControlRightCurrent = true;
            speeControlLeftRight_LastKeyOrMouse = false;
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
            leftKeyDown = false;
            directionControlLeftCurrent = false;
            directionLefRightKeyIsMoving = false;
        } else if (keycode === 83) { //S
            speedControlBackwardCurrent = false;
        } else if (keycode === 68) { //D
            rightKeyDown = false;
            directionControlRightCurrent = false;
            directionLefRightKeyIsMoving = false;
        }
    }
}

document.body.addEventListener('mousemove', function (e) {

    if (document.pointerLockElement === document.body ||
        document.mozPointerLockElement === document.body ||
        document.webkitPointerLockElement === document.body) {

        var movementX = e.movementX || e.webkitMovementX;
        var movementY = e.movementY || e.webkitMovementY;

        if (movementX === undefined) {
            movementX = 0;
        }

        if (movementY === undefined) {
            movementY = 0;
        }

        entryCoordinates.x = entryCoordinates.x + movementX;
        entryCoordinates.y = entryCoordinates.y + movementY;

        if (entryCoordinates.x >= (entryCoordinatesLastMoveX + mouseSennsitivy)
            && directionLefRightKeyIsMoving === false) {

            var now = new Date();
            speedControlLeftRightMouseActionLastMilliseconds = now - speedControlLeftRightMouseActionLast;
            speedControlLeftRightMouseActionLast = now;

            directionControlRightCurrent = true;
            speeControlLeftRight_LastKeyOrMouse = true;

            if (mousemoveXPositive !== undefined) {
                window.clearTimeout(mousemoveXPositive);
            }

            entryCoordinates.x = 0;
            entryCoordinatesLastMoveX = 0;

            mousemoveXPositive = window.setTimeout(function () {
                if (rightKeyDown === false) {
                    directionControlRightCurrent = false;
                }
            }, 200);
        }

        if (entryCoordinates.x <= (entryCoordinatesLastMoveX - mouseSennsitivy)
            && directionLefRightKeyIsMoving === false) {

            var now = new Date();
            speedControlLeftRightMouseActionLastMilliseconds = now - speedControlLeftRightMouseActionLast;
            speedControlLeftRightMouseActionLast = now;

            directionControlLeftCurrent = true;
            speeControlLeftRight_LastKeyOrMouse = true;

            if (mousemoveXNegative !== undefined) {
                window.clearTimeout(mousemoveXNegative);
            }

            entryCoordinates.x = 0;
            entryCoordinatesLastMoveX = 0;

            mousemoveXNegative = window.setTimeout(function () {
                if (leftKeyDown === false) {
                    directionControlLeftCurrent = false;
                }
            }, 200);
        }

        if (entryCoordinates.y <= (entryCoordinatesLastMoveY - mouseSennsitivy)) {
            directionControlUpCurrent = true;

            if (mousemoveYPositive !== undefined) {
                window.clearTimeout(mousemoveYPositive);
            }

            entryCoordinates.y = 0;
            entryCoordinatesLastMoveY = 0;

            mousemoveYPositive = window.setTimeout(function () {
                directionControlUpCurrent = false;
            }, 200);
        }

        if (entryCoordinates.y >= (entryCoordinatesLastMoveY + mouseSennsitivy)) {
            directionControlDownCurrent = true;

            if (mousemoveYNegative !== undefined) {
                window.clearTimeout(mousemoveYNegative);
            }

            entryCoordinatesLastMoveY = 0;
            entryCoordinates.y = 0;

            mousemoveYNegative = window.setTimeout(function () {
                directionControlDownCurrent = false;
            }, 200);
        }
    }
}, true);

document.body.addEventListener("click", function (e) {

    if (!(document.pointerLockElement === document.body ||
        document.mozPointerLockElement === document.body ||
        document.webkitPointerLockElement === document.body)) {

        document.body.requestPointerLock = document.body.requestPointerLock ||
            document.body.mozRequestPointerLock ||
            document.body.webkitRequestPointerLock;

        document.body.requestPointerLock();
    }

}, true);

document.addEventListener('pointerlockchange', PointerLockChanged, true);
document.addEventListener('mozpointerlockchange', PointerLockChanged, true);
document.addEventListener('webkitpointerlockchange', PointerLockChanged, true);