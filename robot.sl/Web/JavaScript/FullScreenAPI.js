(function() {
    var
        fullScreenAPI = {
            supportFullScreen: false,
            nonNativesupportFullScreen: false,
            isFullScreen: function() { return false; },
            requestFullScreen: function() {},
            cancelFullScreen: function() {},
            fullScreenEventName: '',
            prefix: ''
        },
        browserPrefixes = 'webkit moz o ms khtml'.split(' ');

    if (typeof document.cancelFullScreen != 'undefined') {
        fullScreenAPI.supportFullScreen = true;
    } else {
        for (var i = 0, il = browserPrefixes.length; i < il; i++ ) {
            fullScreenAPI.prefix = browserPrefixes[i];
 
            if (typeof document[fullScreenAPI.prefix + 'CancelFullScreen' ] != 'undefined' ) {
                fullScreenAPI.supportFullScreen = true;
                break;
            }
        }
    }
 
    if (fullScreenAPI.supportFullScreen) {
        fullScreenAPI.fullScreenEventName = fullScreenAPI.prefix + 'fullscreenchange';
 
        fullScreenAPI.isFullScreen = function() {
            switch (this.prefix) {
                case '':
                    return document.fullScreen;
                case 'webkit':
                    return document.webkitIsFullScreen;
                default:
                    return document[this.prefix + 'FullScreen'];
            }
        }
        fullScreenAPI.requestFullScreen = function(element) {
            return (this.prefix === '') ? element.requestFullScreen() : element[this.prefix + 'RequestFullScreen']();
        }
        fullScreenAPI.cancelFullScreen = function() {
            return (this.prefix === '') ? document.cancelFullScreen() : document[this.prefix + 'CancelFullScreen']();
        }
    }

    window.fullScreenAPI = fullScreenAPI;
})();