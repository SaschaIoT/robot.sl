function GetState() {

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "GetState?time=" + new Date().getTime(), true);
    xhr.responseType = "json";

    xhr.onreadystatechange = function () {
        if (xhr.readyState == 4) {
            if (xhr.status === 200) {
                var state = xhr.response;

                var speakerOnElement = document.getElementById("speakerOn");
                var speakerOffElement = document.getElementById("speakerOff");
                var carSpeakerOnElement = document.getElementById("carSpeakerOn");
                var carSpeakerOffElement = document.getElementById("carSpeakerOff");
                var headsetSpeakerOnElement = document.getElementById("headsetSpeakerOn");
                var headsetSpeakerOffElement = document.getElementById("headsetSpeakerOff");
                var soundModeOnElement = document.getElementById("soundModeOn");
                var soundModeOffElement = document.getElementById("soundModeOff");

                if (state.CarSpeakerOn === true && state.HeadsetSpeakerOn === true) {
                    if (speakerOn !== true) {
                        speakerOnElement.classList.remove("visible");
                        speakerOffElement.classList.add("visible");
                    }

                    speakerOn = true;
                } else if (state.CarSpeakerOn === false && state.HeadsetSpeakerOn === false) {
                    if (speakerOn !== false) {
                        speakerOffElement.classList.remove("visible");
                        speakerOnElement.classList.add("visible");
                    }

                    speakerOn = false;
                }
                else {
                    if (speakerOn !== false) {
                        speakerOffElement.classList.remove("visible");
                        speakerOnElement.classList.add("visible");
                    }

                    speakerOn = false;
                }

                if (state.CarSpeakerOn === true) {
                    if (carSpeakerOn !== true) {
                        carSpeakerOnElement.classList.remove("visible");
                        carSpeakerOffElement.classList.add("visible");
                    }
                }
                else if (state.CarSpeakerOn === false) {
                    if (carSpeakerOn !== false) {
                        carSpeakerOffElement.classList.remove("visible");
                        carSpeakerOnElement.classList.add("visible");
                    }
                }

                if (state.HeadsetSpeakerOn === true) {
                    if (headsetSpeakerOn !== true) {
                        headsetSpeakerOnElement.classList.remove("visible");
                        headsetSpeakerOffElement.classList.add("visible");
                    }
                }
                else if (state.HeadsetSpeakerOn === false) {
                    if (headsetSpeakerOn !== false) {
                        headsetSpeakerOffElement.classList.remove("visible");
                        headsetSpeakerOnElement.classList.add("visible");
                    }
                }

                if (state.SoundModeOn === true) {
                    if (soundModeOn !== true) {
                        soundModeOnElement.classList.remove("visible");
                        soundModeOffElement.classList.add("visible");
                    }
                }
                else if (state.SoundModeOn === false) {
                    if (soundModeOn !== false) {
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

                carSpeakerOn = state.CarSpeakerOn;
                headsetSpeakerOn = state.HeadsetSpeakerOn;
                soundModeOn = state.SoundModeOn;
                automaticDriveOn = state.AutomaticDriveOn;
            }
        }
    }

    xhr.timeout = xhttpRequestTimeout;
    xhr.ontimeout = function () {
        setTimeout(function () {
            GetState();
        }, 350);
    }

    xhr.onerror = function () {
        setTimeout(function () {
            GetState();
        }, 350);
    }

    xhr.onload = function () {
        setTimeout(function () {
            GetState();
        }, 350);
    }

    xhr.send();
}
GetState();