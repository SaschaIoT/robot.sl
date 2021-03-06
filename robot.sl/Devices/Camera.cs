﻿using robot.sl.Exceptions;
using robot.sl.Helper;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private volatile int _stoppedWorkerCount = GET_FRAME_WORKER_COUNT;

        //Get video frame workers
        private const int GET_FRAME_WORKER_COUNT = 2;
        private long _frameDuration = 0;
        private volatile Stopwatch _lastFrame = new Stopwatch();
        private volatile Stopwatch _lastFrameAdded = new Stopwatch();
        private volatile object _lastFrameAddedLock = new object();

        //Check if camera support resolution before change
        private const int VIDEO_WIDTH = 1280;
        private const int VIDEO_HEIGHT = 720;

        private const double IMAGE_QUALITY_PERCENT = 0.4d;
        private BitmapPropertySet _imageQuality;

        public async Task InitializeAsync()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAndAwaitAsync(CoreDispatcherPriority.Normal, async () =>
            {
                _lastFrame.Start();
                _lastFrameAdded.Start();

                _imageQuality = new BitmapPropertySet();
                var imageQualityValue = new BitmapTypedValue(IMAGE_QUALITY_PERCENT, Windows.Foundation.PropertyType.Single);
                _imageQuality.Add("ImageQuality", imageQualityValue);

                _mediaCapture = new MediaCapture();

                _mediaCapture.Failed += async (MediaCapture mediaCapture, MediaCaptureFailedEventArgs args) =>
                {
                    await Logger.WriteAsync($"Camera Failed Event: {args.Code}, {args.Message}");

                    if (args.Code == 2147942414
                       || args.Code == 3222093442
                       || args.Code == 3222093474)
                    {
                        await Logger.WriteAsync($"Reinitialize camera.");

                        await InitializeAsync();
                    }
                };

                var settings = new MediaCaptureInitializationSettings()
                {
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

                if (!videoDeviceController.BacklightCompensation.TrySetValue(videoDeviceController.BacklightCompensation.Capabilities.Max))
                {
                    throw new RobotSlException("Could not set max backlight compensation to camera.");
                }

                if (!videoDeviceController.Exposure.TrySetAuto(true))
                {
                    throw new RobotSlException("Could not set auto exposure to camera.");
                }

                var videoFormat = mediaFrameSource.SupportedFormats.Where(sf => sf.VideoFormat.Width == VIDEO_WIDTH
                                                                                && sf.VideoFormat.Height == VIDEO_HEIGHT
                                                                                && sf.Subtype == "MJPG")
                                                                   .OrderByDescending(m => m.FrameRate.Numerator / m.FrameRate.Denominator).First();

                await mediaFrameSource.SetFormatAsync(videoFormat);

                _mediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(mediaFrameSource);

                await _mediaFrameReader.StartAsync();
            });
        }

        private void ProcessFrames()
        {
            _stoppedWorkerCount--;

            while (!_isStopping)
            {
                try
                {
                    using (var frame = _mediaFrameReader.TryAcquireLatestFrame())
                    {
                        var frameDuration = new Stopwatch();
                        frameDuration.Start();

                        if (frame == null
                            || frame.VideoMediaFrame == null
                            || frame.VideoMediaFrame.SoftwareBitmap == null)
                            continue;

                        var timestamp = BitConverter.GetBytes(DateTime.Now.Ticks).ToList();

                        using (var stream = new InMemoryRandomAccessStream())
                        {
                            using (var bitmap = SoftwareBitmap.Convert(frame.VideoMediaFrame.SoftwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore))
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

                                    lock (_lastFrameAddedLock)
                                    {
                                        if (_lastFrameAdded.Elapsed.Subtract(frameDuration.Elapsed) > TimeSpan.Zero)
                                        {
                                            timestamp.AddRange(image);
                                            Frame = timestamp.ToArray();

                                            _lastFrameAdded = frameDuration;
                                            _frameDuration = frameDuration.ElapsedMilliseconds;
                                        }
                                    }

                                    encoder = null;
                                }
                            }
                        }
                    }
                }
                catch (ObjectDisposedException) { }
                catch (Exception exception)
                {
                    Logger.WriteAsync(nameof(Camera), exception).Wait();
                }
            }

            _stoppedWorkerCount++;
        }

        public void Start()
        {
            for (int workerNumber = 0; workerNumber < GET_FRAME_WORKER_COUNT; workerNumber++)
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        ProcessFrames();
                    }
                    catch (Exception exception)
                    {
                        Logger.WriteAsync(nameof(Camera), exception).Wait();
                        SystemController.ShutdownApplicationAsync(true).Wait();
                    }
                });
                thread.Priority = ThreadPriority.Normal;
                thread.Start();
            }
        }

        public async Task StopAsync()
        {
            _isStopping = true;

            while (_stoppedWorkerCount != GET_FRAME_WORKER_COUNT)
            {
                await Task.Delay(10);
            }

            _isStopping = false;

            await _mediaFrameReader.StopAsync();
        }
    }
}