function GetSpeed() {

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "Speed?time=" + new Date().getTime(), true);
    xhr.responseType = "json";

    xhr.onreadystatechange = function () {
        if (xhr.readyState == 4) {
            if (xhr.status === 200) {
                var speedResponse = xhr.response;
                document.getElementById("roundsPerMinute").innerHTML = speedResponse.RoundsPerMinute;
                document.getElementById("kilometerPerHour").innerHTML = speedResponse.KilometerPerHour;
            }
        }
    }

    xhr.timeout = xhttpRequestTimeout;
    xhr.ontimeout = function () {
        setTimeout(function () {
            GetSpeed();
        }, 250);
    }

    xhr.onerror = function () {
        setTimeout(function () {
            GetSpeed();
        }, 250);
    }

    xhr.onload = function () {
        setTimeout(function () {
            GetSpeed();
        }, 250);
    }

    xhr.send();
}
GetSpeed();