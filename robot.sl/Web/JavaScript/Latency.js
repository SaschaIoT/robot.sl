var _globalRequestTimes = new Array();
var _lastLatencyStatusUpdate = new Date();
var _firstLatencyStatusUpdate = true;

function ProcessGlobalRequestTime(currentRequestStart, isErrorRequest) {

    _globalRequestTimes.push({
        duration: new Date().getTime() - currentRequestStart,
        isErrorRequest: isErrorRequest
    });

    if (_globalRequestTimes.length > 3) {
        _globalRequestTimes = _globalRequestTimes.slice(_globalRequestTimes.length - 3, 3);
    }

    var connectionLost = true;
    for (var i = 0; i < _globalRequestTimes.length; i++) {
        if (_globalRequestTimes[i].isErrorRequest === false) {
            connectionLost = false;
            break;
        }
    }

    var sumRequestTimes = 0;
    for (var i = 0; i < _globalRequestTimes.length; i++) {
        sumRequestTimes += _globalRequestTimes[i].duration;
    }

    var averageRequestTimes = Math.round(sumRequestTimes / _globalRequestTimes.length);

    var latencyStatusColor = document.getElementById("latency-status-color");
    var latencyStatusMilliseconds = document.getElementById("latency-status-milliseconds");

    var now = new Date();
    if (_firstLatencyStatusUpdate === true
        || (now - _lastLatencyStatusUpdate) >= 1000) {

        _firstLatencyStatusUpdate = false;
        _lastLatencyStatusUpdate = now;

        if (connectionLost === true) {
            latencyStatusColor.classList.add("latency-bad");
            latencyStatusColor.classList.remove("latency-good");
            latencyStatusColor.classList.remove("latency-okay");

            latencyStatusMilliseconds.innerHTML = "";
            latencyLost.classList.remove("latency-lost-hide");
        } else if (averageRequestTimes <= 200) {
            latencyStatusColor.classList.add("latency-good");
            latencyStatusColor.classList.remove("latency-bad");
            latencyStatusColor.classList.remove("latency-okay");
            latencyStatusMilliseconds.innerHTML = averageRequestTimes + " ms";
        } else if (averageRequestTimes <= 400) {
            latencyStatusColor.classList.add("latency-okay");
            latencyStatusColor.classList.remove("latency-good");
            latencyStatusColor.classList.remove("latency-bad");
            latencyStatusMilliseconds.innerHTML = averageRequestTimes + " ms";
        } else {
            latencyStatusColor.classList.add("latency-bad");
            latencyStatusColor.classList.remove("latency-okay");
            latencyStatusColor.classList.remove("latency-good");

            var averageRequestTimesCorrected = averageRequestTimes.toString();
            if (averageRequestTimesCorrected > 2000)
            {
                averageRequestTimesCorrected = ">2000";
            }

            latencyStatusMilliseconds.innerHTML = averageRequestTimesCorrected + " ms";
        }

        if (connectionLost === false) {
            latencyLost.classList.add("latency-lost-hide");
        }
    }
}