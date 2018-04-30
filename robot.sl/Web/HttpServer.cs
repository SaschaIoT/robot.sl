using robot.sl.Audio.AudioPlaying;
using robot.sl.CarControl;
using robot.sl.Devices;
using robot.sl.Helper;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;

namespace robot.sl.Web
{
    public class HttpServerController
    {
        private HttpServer _httpServer;
        private ManualResetEvent _threadWaiter = new ManualResetEvent(false);

        public HttpServerController(MotorController motorController,
                                    ServoController servoController,
                                    AutomaticDrive automaticDrive,
                                    Camera camera,
                                    Dance dance)
        {

            Task.Factory.StartNew(() =>
            {

                _httpServer = new HttpServer(motorController,
                                             servoController,
                                             automaticDrive,
                                             camera,
                                             dance);
                _httpServer.StartAsync();
                _threadWaiter.WaitOne();

            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
             .AsAsyncAction()
             .AsTask()
             .ContinueWith((t) =>
             {

                 Logger.WriteAsync(nameof(HttpServerController), t.Exception).Wait();
                 SystemController.ShutdownApplicationAsync(true).Wait();

             }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void Stop()
        {
            _httpServer.Stop();
            _threadWaiter.Set();
        }
    }

    public sealed class HttpServer
    {
        private const uint BUFFER_SIZE = 3024;
        private readonly StreamSocketListener _listener;
        private volatile bool _isStopped;

        //Dependency objects
        private MotorController _motorController;
        private ServoController _servoController;
        private AutomaticDrive _automaticDrive;
        private Camera _camera;
        private Dance _dance;

        public HttpServer(MotorController motorController,
                          ServoController servoController,
                          AutomaticDrive automaticDrive,
                          Camera camera,
                          Dance dance)
        {
            _motorController = motorController;
            _servoController = servoController;
            _automaticDrive = automaticDrive;
            _camera = camera;
            _dance = dance;

            _listener = GetStreamSocketListener();
        }

        private StreamSocketListener GetStreamSocketListener()
        {
            var streamSocketListener = new StreamSocketListener();
            streamSocketListener.ConnectionReceived += ProcessRequest;
            streamSocketListener.Control.KeepAlive = false;
            streamSocketListener.Control.NoDelay = true;
            streamSocketListener.Control.QualityOfService = SocketQualityOfService.LowLatency;

            return streamSocketListener;
        }

        public void Stop()
        {
            _isStopped = true;
        }

        public async void StartAsync()
        {
            await _listener.BindServiceNameAsync(80.ToString());
        }

        private async void ProcessRequest(StreamSocketListener streamSocktetListener, StreamSocketListenerConnectionReceivedEventArgs eventArgs)
        {
            if (_isStopped)
            {
                return;
            }

            try
            {
                var socket = eventArgs.Socket;

                //Read request
                var request = await ReadRequestAsync(socket);

                //Write Response
                await WriteResponseAsync(request, socket);

                socket.InputStream.Dispose();
                socket.OutputStream.Dispose();
                socket.Dispose();
            }
            catch (TaskCanceledException) { }
            catch (Exception) { }
        }

        private async Task<HttpServerRequest> ReadRequestAsync(StreamSocket socket)
        {
            var request = string.Empty;
            var error = false;

            var inputStream = socket.InputStream;

            var data = new byte[BUFFER_SIZE];
            var buffer = data.AsBuffer();

            var startReadRequest = DateTime.Now;
            while (!HttpGetRequestHasUrl(request))
            {
                if (DateTime.Now.Subtract(startReadRequest) >= TimeSpan.FromMilliseconds(5000))
                {
                    error = true;
                    return new HttpServerRequest(null, true);
                }

                var readed = await inputStream.ReadAsync(buffer, BUFFER_SIZE, InputStreamOptions.Partial);

                request += Encoding.UTF8.GetString(data, 0, (int)readed.Length);
            }

            return new HttpServerRequest(request, error);
        }

        private async Task WriteResponseAsync(HttpServerRequest request, StreamSocket socket)
        {
            var outputStream = socket.OutputStream;

            //Request read not successfully
            if (request.Error)
            {
                HttpServerResponse.WriteResponseError("Request wurde nicht vollständig übermittelt oder zu langsam übermittelt und ist daher nicht vollständig.", outputStream);
            }
            //Get javascript
            else if (request.Url.StartsWith("/javascript", StringComparison.OrdinalIgnoreCase))
            {
                HttpServerResponse.WriteResponseFile(ToFolderPath(request.Url), HttpContentType.JavaScript, outputStream);
            }
            //Get style
            else if (request.Url.StartsWith("/styles", StringComparison.OrdinalIgnoreCase))
            {
                HttpServerResponse.WriteResponseFile(ToFolderPath(request.Url), HttpContentType.Css, outputStream);
            }
            //Get image
            else if (request.Url.StartsWith("/images", StringComparison.OrdinalIgnoreCase))
            {
                HttpServerResponse.WriteResponseFile(ToFolderPath(request.Url), HttpContentType.Png, outputStream);
            }
            //Toggle automatic drive on/off
            else if (request.Url.StartsWith("/automaticdrive", StringComparison.OrdinalIgnoreCase))
            {
                if (request.Body == null)
                {
                    HttpServerResponse.WriteResponseError("Request wurde nicht vollständig übermittelt.", outputStream);
                    return;
                }

                var automaticDriveOn = request.Body["automaticDrive"].GetBoolean();

                if (automaticDriveOn)
                {
                    await _automaticDrive.StartAsync();
                }
                else
                {
                    await _automaticDrive.StopAsync();
                }

                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Get speaker states (on/off) and automatic drive state (on/off)
            //Set all speaker on
            else if (request.Url.StartsWith("/speakeronoff?on=true", StringComparison.OrdinalIgnoreCase))
            {
                await AudioPlayerController.SetAllSpeakerOnOffAsync(true);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set all speaker off
            else if (request.Url.StartsWith("/speakeronoff?on=false", StringComparison.OrdinalIgnoreCase))
            {
                await AudioPlayerController.SetAllSpeakerOnOffAsync(false);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set car speaker on
            else if (request.Url.StartsWith("/carspeakeronoff?on=true", StringComparison.OrdinalIgnoreCase))
            {
                await AudioPlayerController.SetCarSpeakerOnOffAsync(true);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set car speaker off
            else if (request.Url.StartsWith("/carspeakeronoff?on=false", StringComparison.OrdinalIgnoreCase))
            {
                await AudioPlayerController.SetCarSpeakerOnOffAsync(false);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set headset speaker on
            else if (request.Url.StartsWith("/headsetspeakeronoff?on=true", StringComparison.OrdinalIgnoreCase))
            {
                await AudioPlayerController.SetHeadsetSpeakerOnOffAsync(true);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set headset speaker off
            else if (request.Url.StartsWith("/headsetspeakeronoff?on=false", StringComparison.OrdinalIgnoreCase))
            {
                await AudioPlayerController.SetHeadsetSpeakerOnOffAsync(false);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set sound mode on
            else if (request.Url.StartsWith("/soundmodeonoff?on=true", StringComparison.OrdinalIgnoreCase))
            {
                await AudioPlayerController.SetSoundModeOnOffAsync(true);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set sound mode off
            else if (request.Url.StartsWith("/soundmodeonoff?on=false", StringComparison.OrdinalIgnoreCase))
            {
                await AudioPlayerController.SetSoundModeOnOffAsync(false);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set dance on
            else if (request.Url.StartsWith("/danceonoff?on=true", StringComparison.OrdinalIgnoreCase))
            {
                await _dance.StartAsync();
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set dance off
            else if (request.Url.StartsWith("/danceonoff?on=false", StringComparison.OrdinalIgnoreCase))
            {
                await _dance.StopAsync();
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set cliff sensor on
            else if (request.Url.StartsWith("/cliffsensoronoff?on=true", StringComparison.OrdinalIgnoreCase))
            {
                await _automaticDrive.SetCliffSensorState(false, true);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set cliff sensor off
            else if (request.Url.StartsWith("/cliffsensoronoff?on=false", StringComparison.OrdinalIgnoreCase))
            {
                await _automaticDrive.SetCliffSensorState(false, false);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Shutdown Windows
            else if (request.Url.StartsWith("/ausschalten", StringComparison.OrdinalIgnoreCase))
            {
                await SystemController.ShutdownAsync();
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Restart Windows
            else if (request.Url.StartsWith("/neustarten", StringComparison.OrdinalIgnoreCase))
            {
                await SystemController.RestartAsync();
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Get log file
            else if (request.Url.StartsWith("/log", StringComparison.OrdinalIgnoreCase))
            {
                var logFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(Logger.FILE_NAME);
                if (logFile != null)
                {
                    HttpServerResponse.WriteResponseFileFromLocalFolder(Logger.FILE_NAME, HttpContentType.Text, outputStream);
                }
                else
                {
                    HttpServerResponse.WriteResponseText("Log file is empty", outputStream);
                }
            }
            //Delete log file
            else if (request.Url.StartsWith("/deletelog", StringComparison.OrdinalIgnoreCase))
            {
                await Logger.DeleteAsync();
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Get camera frame
            else if (request.Url.StartsWith("/videoframe", StringComparison.OrdinalIgnoreCase))
            {
                if (_camera.Frame != null)
                {
                    var webSocket = new WebSocket(socket, request, _camera, _motorController, _servoController, _automaticDrive, _dance);
                    await webSocket.StartAsync();
                }
                else
                {
                    HttpServerResponse.WriteResponseError("Not camera fram available. Maybe there is an error or camera is not started.", outputStream);
                }
            }
            //Get State, Speed, CarControlCommand, ServerVideoFrameRate
            else if (request.Url.StartsWith("/controller", StringComparison.OrdinalIgnoreCase))
            {
                var webSocket = new WebSocket(socket, request, _camera, _motorController, _servoController, _automaticDrive, _dance);
                await webSocket.StartAsync();
            }
            //Get desktop html view
            else if (request.Url.StartsWith("/desktop", StringComparison.OrdinalIgnoreCase))
            {
                HttpServerResponse.WriteResponseFile(@"\Views\Desktop.html", HttpContentType.Html, outputStream);
            }
            //Get mobile html view
            else if (request.Url.StartsWith("/mobile", StringComparison.OrdinalIgnoreCase))
            {
                HttpServerResponse.WriteResponseFile(@"\Views\Mobile.html", HttpContentType.Html, outputStream);
            }
            //Get view that redirect to desktop oder mobile view
            else
            {
                HttpServerResponse.WriteResponseFile(@"\Views\Index.html", HttpContentType.Html, outputStream);
            }
        }

        private bool HttpGetRequestHasUrl(string httpRequest)
        {
            var regex = new Regex("GET.*HTTP.*\r\n", RegexOptions.IgnoreCase);
            return regex.IsMatch(httpRequest.ToUpper());
        }

        private string ToFolderPath(string relativeUrl)
        {
            var folderPath = relativeUrl.Replace('/', '\\');
            return folderPath;
        }
    }
}
