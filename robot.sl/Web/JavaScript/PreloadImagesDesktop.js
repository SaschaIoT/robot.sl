var path = "../Images/"
var images = [];

function preloadImages(imagePaths) {
    for (var index = 0; index < imagePaths.length; index++) {
        images[index] = new Image();
        images[index].src = path + imagePaths[index];
    }
}

preloadImages(
    ["setting-icon@30x30.png",
        "automatic-drive@64x64.png",
        "automatic-drive-active@64x64.png",
        "fullscreen@64x64.png",
        "fullscreen-active@64x64.png",
        "latency@24x24.png",
        "latency-lost@64x64.png",
        "tachometer@24x24.png",
        "audio_volume_high@24x24.png",
        "audio_volume_mute@24x24.png",
        "close@24x24.png",
        "reload@24x24.png"]
);