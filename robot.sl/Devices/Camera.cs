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
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace robot.sl.Devices
{
    /// <summary>
    /// Camera: ELP 2.8mm wide angle lens 1080p HD USB Camera Module (ELP-USBFHD01M-L28)
    /// </summary>
    public class Camera
    {
        public byte[] Frame { get; set; }
        private MediaCapture _mediaCapture;
        private MediaFrameReader _mediaFrameReader;

        private volatile bool _isStopping = false;
        private volatile bool _isStopped = true;

        //Check if camera support resolution before change
        private const int VIDEO_WIDTH = 640;
        private const int VIDEO_HEIGHT = 480;

        private const double IMAGE_QUALITY_PERCENT = 0.8d;
        private BitmapPropertySet _imageQuality;

        public async Task Initialize()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAndAwaitAsync(CoreDispatcherPriority.Normal, async () =>
            {
                _imageQuality = new BitmapPropertySet();
                var imageQualityValue = new BitmapTypedValue(IMAGE_QUALITY_PERCENT, Windows.Foundation.PropertyType.Single);
                _imageQuality.Add("ImageQuality", imageQualityValue);

                _mediaCapture = new MediaCapture();

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

                var frameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();

                var settings = new MediaCaptureInitializationSettings()
                {
                    SharingMode = MediaCaptureSharingMode.ExclusiveControl,

                    //With CPU the results contain always SoftwareBitmaps, otherwise with GPU
                    //they preferring D3DSurface
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu,

                    //Capture only video, no audio
                    StreamingCaptureMode = StreamingCaptureMode.Video
                };

                await _mediaCapture.InitializeAsync(settings);

                var mediaFrameSource = _mediaCapture.FrameSources.First().Value;
                var videoDeviceController = mediaFrameSource.Controller.VideoDeviceController;

                videoDeviceController.DesiredOptimization = Windows.Media.Devices.MediaCaptureOptimization.Quality;
                videoDeviceController.PrimaryUse = Windows.Media.Devices.CaptureUse.Video;

                if (!videoDeviceController.BacklightCompensation.TrySetValue(videoDeviceController.BacklightCompensation.Capabilities.Min))
                {
                    throw new Exception("Could not set min backlight compensation to camera.");
                }

                if (!videoDeviceController.Exposure.TrySetAuto(true))
                {
                    throw new Exception("Could not set auto exposure to camera.");
                }

                var videoFormat = mediaFrameSource.SupportedFormats.First(sf => sf.VideoFormat.Width == VIDEO_WIDTH
                                                                                && sf.VideoFormat.Height == VIDEO_HEIGHT
                                                                                && sf.Subtype == "YUY2");

                await mediaFrameSource.SetFormatAsync(videoFormat);

                _mediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(mediaFrameSource);

                await _mediaFrameReader.StartAsync();
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GarbageCollectorCanWorkHere() { }

        private void ProcessFrames()
        {
            _isStopped = false;

            while (!_isStopping)
            {
                try
                {
                    GarbageCollectorCanWorkHere();

                    using (var frame = _mediaFrameReader.TryAcquireLatestFrame())
                    {
                        if (frame == null
                            || frame.VideoMediaFrame == null
                            || frame.VideoMediaFrame.SoftwareBitmap == null)
                            continue;

                        using (var stream = new InMemoryRandomAccessStream())
                        {
                            using (var bitmap = SoftwareBitmap.Convert(frame.VideoMediaFrame.SoftwareBitmap, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied))
                            {
                                var imageTask = BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream, _imageQuality).AsTask();
                                imageTask.Wait();
                                var encoder = imageTask.Result;
                                encoder.SetSoftwareBitmap(bitmap);

                                //Rotate image 180 degrees
                                var transform = encoder.BitmapTransform;
                                transform.Rotation = BitmapRotation.Clockwise180Degrees;

                                var flushTask = encoder.FlushAsync().AsTask();
                                flushTask.Wait();

                                using (var asStream = stream.AsStream())
                                {
                                    asStream.Position = 0;

                                    var image = new byte[asStream.Length];
                                    asStream.Read(image, 0, image.Length);

                                    Frame = image;

                                    encoder = null;
                                }
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Logger.Write(nameof(Camera), exception).Wait();
                }
            }

            _isStopped = true;
        }

        public void Start()
        {
            Task.Factory.StartNew(() =>
            {
                ProcessFrames();

            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
            .AsAsyncAction()
            .AsTask()
            .ContinueWith((t) =>
            {
                Logger.Write(nameof(Camera), t.Exception).Wait();
                SystemController.ShutdownApplication(true).Wait();

            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public async Task Stop()
        {
            _isStopping = true;

            while (!_isStopped)
            {
                await Task.Delay(10);
            }

            _isStopping = false;

            await _mediaFrameReader.StopAsync();
        }
    }
}