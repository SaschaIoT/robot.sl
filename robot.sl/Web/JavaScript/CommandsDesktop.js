var settingButton = document.getElementById("setting");
settingButton.addEventListener("mouseover", function (e) {

    settingButton.addEventListener("mouseleave", settingMouseLeave);

    e.preventDefault();
    e.stopPropagation();

    settingMenu.classList.remove("setting-menu-hide");
});

var settingMouseLeave = function (e) {

    e.preventDefault();
    e.stopPropagation();

    settingMenu.classList.add("setting-menu-hide");
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

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "http://192.168.0.101/speakerOnOff?on=" + speakerOnOffString + "?time=" + new Date().getTime(), true);
    xhr.responseType = "json";
    xhr.timeout = xhttpRequestTimeout;
    xhr.send();

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

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "http://192.168.0.101/carSpeakerOnOff?on=" + carSpeakerOnOffString + "?time=" + new Date().getTime(), true);
    xhr.responseType = "json";
    xhr.timeout = xhttpRequestTimeout;
    xhr.send();

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

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "http://192.168.0.101/headsetSpeakerOnOff?on=" + headsetSpeakerOnOffString + "?time=" + new Date().getTime(), true);
    xhr.responseType = "json";
    xhr.timeout = xhttpRequestTimeout;
    xhr.send();

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

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "http://192.168.0.101/soundModeOnOff?on=" + soundModeOnOffString + "?time=" + new Date().getTime(), true);
    xhr.responseType = "json";
    xhr.timeout = xhttpRequestTimeout;
    xhr.send();

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

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "http://192.168.0.101/danceOnOff?on=" + danceOnOffString + "?time=" + new Date().getTime(), true);
    xhr.responseType = "json";
    xhr.timeout = xhttpRequestTimeout;
    xhr.send();

    e.preventDefault();
    e.stopPropagation();
});

var ausschalten = document.getElementById("ausschalten");
ausschalten.addEventListener("click", function (e) {

    if (confirm('Willst Du mich wirklich ausschalten?')) {
        var xhr = new XMLHttpRequest();
        xhr.open("GET", "http://192.168.0.101/ausschalten?time=" + new Date().getTime(), true);
        xhr.responseType = "json";
        xhr.timeout = xhttpRequestTimeout;
        xhr.send();
    }

    e.preventDefault();
    e.stopPropagation();
});

var neustarten = document.getElementById("neustarten");
neustarten.addEventListener("click", function (e) {

    if (confirm('Willst Du mich wirklich neustarten?')) {
        var xhr = new XMLHttpRequest();
        xhr.open("GET", "http://192.168.0.101/neustarten?time=" + new Date().getTime(), true);
        xhr.responseType = "json";
        xhr.timeout = xhttpRequestTimeout;
        xhr.send();
    }

    e.preventDefault();
    e.stopPropagation();
});

var automaticDriveOn = false;
var automaticDriveButton = document.getElementById("automaticDriveButton");
automaticDriveButton.addEventListener('click', function (e) {

    e.preventDefault();
    e.stopPropagation();

    var http = new XMLHttpRequest();
    var automaticDriveParameter = "<RequestBody>" + JSON.stringify({ automaticDrive: !automaticDriveOn }) + "</RequestBody>";
    http.open("GET", "http://192.168.0.101/AutomaticDrive/" + new Date().getTime() + automaticDriveParameter + ".html", true);

    http.send("data=" + encodeURIComponent(automaticDriveParameter));
});