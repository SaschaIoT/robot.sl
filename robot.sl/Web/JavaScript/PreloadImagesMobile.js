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
        "arrow-left@64x64.png",
        "arrow-left-pressed@64x64.png",
        "arrow-right@64x64.png",
        "arrow-right-pressed@64x64.png",
        "arrow-up@64x64.png",
        "arrow-up-pressed@64x64.png",
        "arrow-bottom@64x64.png",
        "arrow-bottom-pressed@64x64.png",
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
        "reload@24x24.png",
        "dance_on@24x24.png",
        "dance_off@24x24.png"]
);