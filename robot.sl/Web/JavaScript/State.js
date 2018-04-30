var webSocketState;
var stateTime;

function GetState() {

    webSocketState = new WebSocket('ws://192.168.0.101:80/Controller');

    webSocketState.onopen = function () {
        webSocketHelper.waitUntilWebsocketReady(function () {
            webSocketState.send(JSON.stringify({ command: "State" }));
        }, webSocketState, 0);
    };

    webSocketState.onmessage = function () {
        
        var state = JSON.parse(event.data).parameter;

        UpdateState(state);

        webSocketHelper.waitUntilWebsocketReady(function () {
            webSocketState.send(JSON.stringify({ command: "State" }));
        }, webSocketState, 0);

        stateTime = new Date().getTime();
    };
}

var speakerOnElement = document.getElementById("speakerOn");
var speakerOffElement = document.getElementById("speakerOff");
var carSpeakerOnElement = document.getElementById("carSpeakerOn");
var carSpeakerOffElement = document.getElementById("carSpeakerOff");
var headsetSpeakerOnElement = document.getElementById("headsetSpeakerOn");
var headsetSpeakerOffElement = document.getElementById("headsetSpeakerOff");
var soundModeOnElement = document.getElementById("soundModeOn");
var soundModeOffElement = document.getElementById("soundModeOff");
var danceOnElement = document.getElementById("danceOn");
var danceOffElement = document.getElementById("danceOff");
var cliffSensorOnElement = document.getElementById("cliffSensorOn");
var cliffSensorOffElement = document.getElementById("cliffSensorOff");

function UpdateState(state) {
    
    if (state.CarSpeakerOn === true && state.HeadsetSpeakerOn === true) {
        if (speakerOn !== true) {
            removeSettingMouseLeaveEvent();
            speakerOnElement.classList.remove("visible");
            speakerOffElement.classList.add("visible");
        }

        speakerOn = true;
    } else if (state.CarSpeakerOn === false && state.HeadsetSpeakerOn === false) {
        if (speakerOn !== false) {
            removeSettingMouseLeaveEvent();
            speakerOffElement.classList.remove("visible");
            speakerOnElement.classList.add("visible");
        }

        speakerOn = false;
    }
    else {
        if (speakerOn !== false) {
            removeSettingMouseLeaveEvent();
            speakerOffElement.classList.remove("visible");
            speakerOnElement.classList.add("visible");
        }

        speakerOn = false;
    }

    if (state.CarSpeakerOn === true) {
        if (carSpeakerOn !== true) {
            removeSettingMouseLeaveEvent();
            carSpeakerOnElement.classList.remove("visible");
            carSpeakerOffElement.classList.add("visible");
        }
    }
    else if (state.CarSpeakerOn === false) {
        if (carSpeakerOn !== false) {
            removeSettingMouseLeaveEvent();
            carSpeakerOffElement.classList.remove("visible");
            carSpeakerOnElement.classList.add("visible");
        }
    }

    if (state.HeadsetSpeakerOn === true) {
        if (headsetSpeakerOn !== true) {
            removeSettingMouseLeaveEvent();
            headsetSpeakerOnElement.classList.remove("visible");
            headsetSpeakerOffElement.classList.add("visible");
        }
    }
    else if (state.HeadsetSpeakerOn === false) {
        if (headsetSpeakerOn !== false) {
            removeSettingMouseLeaveEvent();
            headsetSpeakerOffElement.classList.remove("visible");
            headsetSpeakerOnElement.classList.add("visible");
        }
    }

    if (state.SoundModeOn === true) {
        if (soundModeOn !== true) {
            removeSettingMouseLeaveEvent();
            soundModeOnElement.classList.remove("visible");
            soundModeOffElement.classList.add("visible");
        }
    }
    else if (state.SoundModeOn === false) {
        if (soundModeOn !== false) {
            removeSettingMouseLeaveEvent();
            soundModeOffElement.classList.remove("visible");
            soundModeOnElement.classList.add("visible");
        }
    }

    if (state.AutomaticDriveOn === true) {
        if (automaticDriveOn !== true) {
            automaticDriveButton.classList.add("automatic-drive-active-button");
        }
    } else {
        if (automaticDriveOn !== false) {
            automaticDriveButton.classList.remove("automatic-drive-active-button");
        }
    }

    if (state.DanceOn === true) {
        if (danceOn !== true) {
            removeSettingMouseLeaveEvent();
            danceOnElement.classList.remove("visible");
            danceOffElement.classList.add("visible");
        }
    }
    else if (state.DanceOn === false) {
        if (danceOn !== false) {
            removeSettingMouseLeaveEvent();
            danceOffElement.classList.remove("visible");
            danceOnElement.classList.add("visible");
        }
    }

    if (state.CliffSensorOn === true) {
        if (cliffSensorOn !== true) {
            removeSettingMouseLeaveEvent();
            cliffSensorOnElement.classList.remove("visible");
            cliffSensorOffElement.classList.add("visible");
        }
    }
    else if (state.CliffSensorOn === false) {
        if (cliffSensorOn !== false) {
            removeSettingMouseLeaveEvent();
            cliffSensorOffElement.classList.remove("visible");
            cliffSensorOnElement.classList.add("visible");
        }
    }

    carSpeakerOn = state.CarSpeakerOn;
    headsetSpeakerOn = state.HeadsetSpeakerOn;
    soundModeOn = state.SoundModeOn;
    automaticDriveOn = state.AutomaticDriveOn;
    danceOn = state.DanceOn;
    cliffSensorOn = state.CliffSensorOn;
}

function removeSettingMouseLeaveEvent() {
    if (isDesktop) {
        settingButton.removeEventListener("mouseleave", settingMouseLeave);
    }
}

function KeepAliveGetState() {

    var duration = 0;
    if (stateTime !== undefined) {
        duration = new Date().getTime() - stateTime
    }

    if (stateTime !== undefined
        && duration <= requestTimeout) {

        setTimeout(function () {
            KeepAliveGetState();
        }, 50);
    } else {

        if (webSocketState !== undefined) {
            try {
                webSocketState.close();
            } catch (e) { }
        }
        
        GetState();

        setTimeout(function () {
            KeepAliveGetState();
        }, 4000);
    }
}

KeepAliveGetState();