var settingButton = document.getElementById("setting");
settingButton.addEventListener("mouseover", function (e) {

    settingButton.addEventListener("mouseleave", settingMouseLeave);
    settingMenu.classList.remove("setting-menu-hide");

    e.preventDefault();
    e.stopPropagation();
});

var settingMouseLeave = function (e) {
    settingMenu.classList.add("setting-menu-hide");

    e.preventDefault();
    e.stopPropagation();
};

settingButton.addEventListener("mouseleave", settingMouseLeave);

settingButton.addEventListener("click", function (e) {

    e.preventDefault();
    e.stopPropagation();
});

var speakerOn = true;
var speakerOnOffElement = document.getElementById("speakerOnOff");
speakerOnOffElement.addEventListener("click", function (e) {

    var speakerOnOffString = "";
    if (speakerOn === false) {
        speakerOnOffString = "true";
    }
    else {
        speakerOnOffString = "false";
    }

    var request = new XMLHttpRequest();
    request.open("GET", "http://192.168.0.101/speakerOnOff?on=" + speakerOnOffString + "?time=" + new Date().getTime(), true);
    request.responseType = "json";
    request.timeout = requestTimeout;
    request.send();

    e.preventDefault();
    e.stopPropagation();
});

var carSpeakerOn = true;
var carSpeakerOnOffElement = document.getElementById("carSpeakerOnOff");
carSpeakerOnOffElement.addEventListener("click", function (e) {

    var carSpeakerOnOffString = "";
    if (carSpeakerOn === false) {
        carSpeakerOnOffString = "true";
    }
    else {
        carSpeakerOnOffString = "false";
    }

    var request = new XMLHttpRequest();
    request.open("GET", "http://192.168.0.101/carSpeakerOnOff?on=" + carSpeakerOnOffString + "?time=" + new Date().getTime(), true);
    request.responseType = "json";
    request.timeout = requestTimeout;
    request.send();

    e.preventDefault();
    e.stopPropagation();
});

var headsetSpeakerOn = true;
var headsetSpeakerOnOffElement = document.getElementById("headsetSpeakerOnOff");
headsetSpeakerOnOffElement.addEventListener("click", function (e) {

    var headsetSpeakerOnOffString = "";
    if (headsetSpeakerOn === false) {
        headsetSpeakerOnOffString = "true";
    }
    else {
        headsetSpeakerOnOffString = "false";
    }

    var request = new XMLHttpRequest();
    request.open("GET", "http://192.168.0.101/headsetSpeakerOnOff?on=" + headsetSpeakerOnOffString + "?time=" + new Date().getTime(), true);
    request.responseType = "json";
    request.timeout = requestTimeout;
    request.send();

    e.preventDefault();
    e.stopPropagation();
});

var soundModeOn = true;
var soundModeOnOffElement = document.getElementById("soundModeOnOff");
soundModeOnOffElement.addEventListener("click", function (e) {

    var soundModeOnOffString = "";
    if (soundModeOn === false) {
        soundModeOnOffString = "true";
    }
    else {
        soundModeOnOffString = "false";
    }

    var request = new XMLHttpRequest();
    request.open("GET", "http://192.168.0.101/soundModeOnOff?on=" + soundModeOnOffString + "?time=" + new Date().getTime(), true);
    request.responseType = "json";
    request.timeout = requestTimeout;
    request.send();

    e.preventDefault();
    e.stopPropagation();
});

var danceOn = true;
var danceOnOffElement = document.getElementById("danceOnOff");
danceOnOffElement.addEventListener("click", function (e) {

    var danceOnOffString = "";
    if (danceOn === false) {
        danceOnOffString = "true";
    }
    else {
        danceOnOffString = "false";
    }

    var request = new XMLHttpRequest();
    request.open("GET", "http://192.168.0.101/danceOnOff?on=" + danceOnOffString + "?time=" + new Date().getTime(), true);
    request.responseType = "json";
    request.timeout = requestTimeout;
    request.send();

    e.preventDefault();
    e.stopPropagation();
});

var cliffSensorOn = true;
var cliffSensorOnOffElement = document.getElementById("cliffSensorOnOff");
cliffSensorOnOffElement.addEventListener("click", function (e) {

    var cliffSensorOnOffString = "";
    if (cliffSensorOn === false) {
        cliffSensorOnOffString = "true";
    }
    else {
        cliffSensorOnOffString = "false";
    }

    var request = new XMLHttpRequest();
    request.open("GET", "http://192.168.0.101/cliffSensorOnOff?on=" + cliffSensorOnOffString + "?time=" + new Date().getTime(), true);
    request.responseType = "json";
    request.timeout = requestTimeout;
    request.send();

    e.preventDefault();
    e.stopPropagation();
});

var ausschalten = document.getElementById("ausschalten");
ausschalten.addEventListener("click", function (e) {

    if (confirm('Willst Du mich wirklich ausschalten?')) {
        var request = new XMLHttpRequest();
        request.open("GET", "http://192.168.0.101/ausschalten?time=" + new Date().getTime(), true);
        request.responseType = "json";
        request.timeout = requestTimeout;
        request.send();
    }

    e.preventDefault();
    e.stopPropagation();
});

var neustarten = document.getElementById("neustarten");
neustarten.addEventListener("click", function (e) {

    if (confirm('Willst Du mich wirklich neustarten?')) {
        var request = new XMLHttpRequest();
        request.open("GET", "http://192.168.0.101/neustarten?time=" + new Date().getTime(), true);
        request.responseType = "json";
        request.timeout = requestTimeout;
        request.send();
    }

    e.preventDefault();
    e.stopPropagation();
});

var automaticDriveOn = false;
var automaticDriveButton = document.getElementById("automaticDriveButton");
automaticDriveButton.addEventListener('click', function (e) {
    
    var automaticDriveParameter = "<RequestBody>" + JSON.stringify({ automaticDrive: !automaticDriveOn }) + "</RequestBody>";

    var request = new XMLHttpRequest();
    request.open("GET", "http://192.168.0.101/AutomaticDrive/" + new Date().getTime() + automaticDriveParameter + ".html", true);
    request.timeout = requestTimeout;
    request.send("data=" + encodeURIComponent(automaticDriveParameter));

    e.preventDefault();
    e.stopPropagation();
});