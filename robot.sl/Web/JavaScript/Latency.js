var requestTimes = new Array();
var lastUpdate = new Date();
var _firstLatencyStatusUpdate = true;

function UpdateLatency(startTime, error) {

    requestTimes.push({
        duration: new Date().getTime() - startTime,
        error: error
    });

    if (requestTimes.length >= 4) {
        requestTimes = requestTimes.slice(1);
    }

    var connectionLost = true;
    for (var clIndex = 0; clIndex < requestTimes.length; clIndex++) {
        if (requestTimes[clIndex].error === false) {
            connectionLost = false;
            break;
        }
    }

    var requestTimesSum = 0;
    for (var rtsIndex = 0; rtsIndex < requestTimes.length; rtsIndex++) {
        requestTimesSum += requestTimes[rtsIndex].duration;
    }

    var averageRequestTimes = Math.round(requestTimesSum / requestTimes.length);

    var latencyStatusColor = document.getElementById("latency-status-color");
    var latencyStatusMilliseconds = document.getElementById("latency-status-milliseconds");

    var now = new Date();
    var time = now - lastUpdate;
    if (time >= 500) {

        lastUpdate = now;

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
            if (averageRequestTimes > 2000)
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