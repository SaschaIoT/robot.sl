var requestTimes = new Array();
var lastUpdate = new Date();
var _firstLatencyStatusUpdate = true;

function UpdateLatency(startTime, error) {

    requestTimes.push({
        duration: new Date().getTime() - startTime,
        error: error
    });

    if (requestTimes.length >= 16) {
        requestTimes = requestTimes.slice(1);
    }

    var requestTimesConnectionLost;
    if (requestTimes.length >= 15) {
        requestTimesConnectionLost = requestTimes.slice(12);
    } else {
        requestTimesConnectionLost = requestTimes;
    }

    var connectionLost = true;
    for (var index = 0; index < requestTimesConnectionLost.length; index++) {
        if (requestTimesConnectionLost[index].error === false) {
            connectionLost = false;
            break;
        }
    }

    var requestTimesSum = 0;
    for (var index = 0; index < requestTimes.length; index++) {
        requestTimesSum += requestTimes[index].duration;
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