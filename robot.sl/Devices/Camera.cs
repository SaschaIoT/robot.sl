using robot.sl.Helper;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace robot.sl.Devices
{
    /// <summary>
    /// Camera: ELP 2.8mm wide angle lens 1920x1080 HD
    /// </summary>
    public class Camera
    {
        public byte[] Frame { get; set; }
        private MediaCapture _mediaCapture;
        private CaptureElement _captureElement;
        private volatile bool _isStopped = false;
        private volatile bool _isStopping = false;

        public async Task Stop()
        {
            _isStopping = true;

#if DEBUG
            _isStopped = true;
#endif

            while (!_isStopped)
            {
                await Task.Delay(10);
            }

            _isStopping = false;
        }

        public async Task Initialize()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                _mediaCapture = new MediaCapture();
                _captureElement = new CaptureElement();

                _mediaCapture.Failed += async (MediaCapture mediaCapture, MediaCaptureFailedEventArgs args) =>
                {
                    await Logger.Write($"Camera Failed Event: {args.Code}, {args.Message}");

                    if (args.Code == 2147942414
                       || args.Code == 3222093442)
                    {
                        await Logger.Write($"Reinitialize camera.");
                        
                        await Initialize();
                    }
                };

                await _mediaCapture.InitializeAsync();

                _mediaCapture.VideoDeviceController.DesiredOptimization = Windows.Media.Devices.MediaCaptureOptimization.Quality;
                _mediaCapture.VideoDeviceController.PrimaryUse = Windows.Media.Devices.CaptureUse.Video;

                //Flip frames 180 degrees
                var result = _mediaCapture.VideoDeviceController.BacklightCompensation.TrySetValue(_mediaCapture.VideoDeviceController.BacklightCompensation.Capabilities.Max);
                _mediaCapture.SetPreviewRotation(VideoRotation.Clockwise180Degrees);

                if (!_mediaCapture.VideoDeviceController.Exposure.TrySetAuto(true))
                {
                    throw new Exception("Could not set auto exposure to camera.");
                }

                //Set resolution
                var resolutions = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview);
                var resolutionsVideo = resolutions.Where(r => r.GetType() == typeof(VideoEncodingProperties)).Select(r => (VideoEncodingProperties)r);
                var targetResolutionVideo = resolutionsVideo.First(rv => rv.Width == 640 && rv.Height == 480 && rv.Subtype == "YUY2" && (rv.FrameRate != null && rv.FrameRate.Numerator == 30));

                await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, targetResolutionVideo);

                _captureElement.Source = _mediaCapture;

                await _mediaCapture.StartPreviewAsync();
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GarbageCollectorCanWorkHere() { }

        public void Start()
        {
            Task.Factory.StartNew(() =>
            {
                StartInternal();
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
            .AsAsyncAction()
            .AsTask()
            .ContinueWith((t) =>
            {
                Logger.Write(nameof(Camera), t.Exception).Wait();
                SystemController.ShutdownApplication(true).Wait();

            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void StartInternal()
        {
#if DEBUG
            return;
#endif
            _isStopped = false;

            while (!_isStopping)
            {
                try
                {
                    GarbageCollectorCanWorkHere();

                    using (var videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, 640, 480))
                    {
                        using (var stream = new InMemoryRandomAccessStream())
                        {
                            var frameTask = _mediaCapture.GetPreviewFrameAsync(videoFrame).AsTask();

                            var timeoutFrameTask = Task.WhenAny(frameTask, Task.Delay(500));
                            timeoutFrameTask.Wait();

                            if (timeoutFrameTask.Result == frameTask)
                            {
                                using (var bitmap = frameTask.Result)
                                {
                                    //Begin: Throw out of memory exception in debug mode
                                    var imageTask = BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream).AsTask();
                                    imageTask.Wait();
                                    var encoder = imageTask.Result;
                                    encoder.SetSoftwareBitmap(bitmap.SoftwareBitmap);
                                    //End

                                    ////Crop image. Make image size smaller for faster http video stream.
                                    //var transform = encoder.BitmapTransform;
                                    //var bounds = new BitmapBounds();
                                    //bounds.X = 112; //new width - old width / 2
                                    //bounds.Y = 84; //new height - old height / 2
                                    //bounds.Height = 312; //new height
                                    //bounds.Width = 416; //new width
                                    //transform.Bounds = bounds;

                                    //transform.ScaledWidth = 640; //old width
                                    //transform.ScaledHeight = 480; //old height

                                    var flushTask = encoder.FlushAsync().AsTask();
                                    flushTask.Wait();

                                    using (var asStream = stream.AsStream())
                                    {
                                        asStream.Position = 0;

                                        var image = new byte[asStream.Length];
                                        asStream.Read(image, 0, image.Length);

                                        Frame = image;

                                        encoder = null;
                                        bitmap.SoftwareBitmap.Dispose();
                                    }
                                }
                            }
                            else
                            {
                                Logger.Write($"Camera get frame Timeout").Wait();
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Logger.Write($"Camera get frame Exception", exception).Wait();
                }
            }

            _isStopped = true;
        }
    }
}
