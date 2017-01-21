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
}, false);

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

var carControlCommandActionNecessary = false;
function IsCarControlCommandActionNecessary(carControlCommand) {
    if (!carControlCommand.directionControlUp
        && !carControlCommand.directionControlLeft
        && !carControlCommand.directionControlRight
        && !carControlCommand.directionControlDown
        && !carControlCommand.speedControlForward
        && !carControlCommand.speedControlBackward) {
        return false;
    } else {
        return true;
    }
}

var _carControlCommandRequestStart;
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

    var isCarControlCommandActionNecessary = IsCarControlCommandActionNecessary(carControlCommand);

    if (!isCarControlCommandActionNecessary
        && !carControlCommandActionNecessary) {
        setTimeout(function () { SendCarControlCommand(); }, 20);
    } else {

        _carControlCommandRequestStart = new Date().getTime();

        if (!isCarControlCommandActionNecessary) {
            carControlCommandActionNecessary = false;
        } else {
            carControlCommandActionNecessary = true;
        }

        var http = new XMLHttpRequest();

        var carCommandParameter = "<RequestBody>" + JSON.stringify(carControlCommand) + "</RequestBody>";

        http.open("GET", "CarControlCommandTime" + new Date().getTime() + ".html?carCommandParameter=" + carCommandParameter, true);

        http.timeout = xhttpRequestTimeout;
        http.ontimeout = function () {
            ProcessGlobalRequestTime(_carControlCommandRequestStart, true);
            setTimeout(function () { SendCarControlCommand(); }, 100);
        }

        http.onerror = function () {
            ProcessGlobalRequestTime(_carControlCommandRequestStart, true);
            setTimeout(function () { SendCarControlCommand(); }, 100);
        }

        http.onload = function () {
            ProcessGlobalRequestTime(_carControlCommandRequestStart, false);
            setTimeout(function () { SendCarControlCommand(); }, 100);
        }

        http.send();
    }
}

SendCarControlCommand();

function Stop() {
    carControlCommandActionNecessary = true;
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

document.addEventListener('pointerlockchange', PointerLockChanged, false);
document.addEventListener('mozpointerlockchange', PointerLockChanged, false);
document.addEventListener('webkitpointerlockchange', PointerLockChanged, false);

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