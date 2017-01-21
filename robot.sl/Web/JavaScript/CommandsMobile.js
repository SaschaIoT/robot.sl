var automaticDriveButton = document.getElementById("automaticDriveButton");

var speakerOn = true;
var speakerOnOffElement = document.getElementById("speakerOnOff");
speakerOnOffElement.addEventListener("touchstart", function (e) {

    speakerOnOffElement.classList.add("setting-menu-point-hover-hover");

    var speakerOnOffString = "";
    if (speakerOn === false) {
        speakerOnOffString = "true";
    }
    else {
        speakerOnOffString = "false";
    }

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "speakerOnOff?on=" + speakerOnOffString + "?time=" + new Date().getTime(), true);
    xhr.responseType = "json";
    xhr.timeout = xhttpRequestTimeout;
    xhr.send();

    e.preventDefault();
    e.stopPropagation();
});

speakerOnOffElement.addEventListener("touchend", function (e) {
    speakerOnOffElement.classList.remove("setting-menu-point-hover-hover");
});

var carSpeakerOn = true;
var carSpeakerOnOffElement = document.getElementById("carSpeakerOnOff");
carSpeakerOnOffElement.addEventListener("touchstart", function (e) {

    carSpeakerOnOffElement.classList.add("setting-menu-point-hover-hover");

    var carSpeakerOnOffString = "";
    if (carSpeakerOn === false) {
        carSpeakerOnOffString = "true";
    }
    else {
        carSpeakerOnOffString = "false";
    }

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "carSpeakerOnOff?on=" + carSpeakerOnOffString + "?time=" + new Date().getTime(), true);
    xhr.responseType = "json";
    xhr.timeout = xhttpRequestTimeout;
    xhr.send();

    e.preventDefault();
    e.stopPropagation();
});

carSpeakerOnOffElement.addEventListener("touchend", function (e) {
    carSpeakerOnOffElement.classList.remove("setting-menu-point-hover-hover");
});

var headsetSpeakerOn = true;
var headsetSpeakerOnOffElement = document.getElementById("headsetSpeakerOnOff");
headsetSpeakerOnOffElement.addEventListener("touchstart", function (e) {

    headsetSpeakerOnOffElement.classList.add("setting-menu-point-hover-hover");

    var headsetSpeakerOnOffString = "";
    if (headsetSpeakerOn === false) {
        headsetSpeakerOnOffString = "true";
    }
    else {
        headsetSpeakerOnOffString = "false";
    }

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "headsetSpeakerOnOff?on=" + headsetSpeakerOnOffString + "?time=" + new Date().getTime(), true);
    xhr.responseType = "json";
    xhr.timeout = xhttpRequestTimeout;
    xhr.send();

    e.preventDefault();
    e.stopPropagation();
});

headsetSpeakerOnOffElement.addEventListener("touchend", function (e) {
    headsetSpeakerOnOffElement.classList.remove("setting-menu-point-hover-hover");
});

var soundModeOn = true;
var soundModeOnOffElement = document.getElementById("soundModeOnOff");
soundModeOnOffElement.addEventListener("touchstart", function (e) {

    soundModeOnOffElement.classList.add("setting-menu-point-hover-hover");

    var soundModeOnOffString = "";
    if (soundModeOn === false) {
        soundModeOnOffString = "true";
    }
    else {
        soundModeOnOffString = "false";
    }

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "soundModeOnOff?on=" + soundModeOnOffString + "?time=" + new Date().getTime(), true);
    xhr.responseType = "json";
    xhr.timeout = xhttpRequestTimeout;
    xhr.send();

    e.preventDefault();
    e.stopPropagation();
});

soundModeOnOffElement.addEventListener("touchend", function (e) {
    soundModeOnOffElement.classList.remove("setting-menu-point-hover-hover");
});

var automaticDriveOn = false;

var ausschalten = document.getElementById("ausschalten");
ausschalten.addEventListener("touchstart", function (e) {

    if (confirm('Willst Du mich wirklich ausschalten?')) {
        var xhr = new XMLHttpRequest();
        xhr.open("GET", "ausschalten?time=" + new Date().getTime(), true);
        xhr.responseType = "json";
        xhr.timeout = xhttpRequestTimeout;
        xhr.send();
    }
});

var neustarten = document.getElementById("neustarten");
neustarten.addEventListener("touchstart", function (e) {

    if (confirm('Willst Du mich wirklich neustarten?')) {
        var xhr = new XMLHttpRequest();
        xhr.open("GET", "neustarten?time=" + new Date().getTime(), true);
        xhr.responseType = "json";
        xhr.timeout = xhttpRequestTimeout;
        xhr.send();
    }
});


automaticDriveButton.addEventListener('touchstart', function (e) {

    e.preventDefault();

    var http = new XMLHttpRequest();
    var automaticDriveParameter = "<RequestBody>" + JSON.stringify({ automaticDrive: !automaticDriveOn }) + "</RequestBody>";

    http.open("GET", "AutomaticDrive/" + new Date().getTime() + automaticDriveParameter + ".html", true);
    http.send("data=" + encodeURIComponent(automaticDriveParameter));
});