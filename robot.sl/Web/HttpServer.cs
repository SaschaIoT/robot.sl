using robot.sl.Audio.AudioPlaying;
using robot.sl.CarControl;
using robot.sl.Devices;
using robot.sl.Helper;
using robot.sl.Sensors;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;

namespace robot.sl.Web
{
    public class HttpServerController
    {
        private HttpServer _httpServer;

        public HttpServerController(MotorController motorController,
                                    ServoController servoController,
                                    AutomaticDrive automaticDrive,
                                    Camera camera)
        {
            Task.Factory.StartNew(() =>
            {
                _httpServer = new HttpServer(motorController,
                                             servoController,
                                             automaticDrive,
                                             camera);
                _httpServer.Start();
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
            .AsAsyncAction()
            .AsTask()
            .ContinueWith((t) =>
            {
                Logger.Write(nameof(HttpServerController), t.Exception).Wait();
                SystemController.ShutdownApplication(true).Wait();

            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void Stop()
        {
            _httpServer.Stop();
        }
    }

    public sealed class HttpServer
    {
        private const uint BUFFER_SIZE = 3024;
        private readonly StreamSocketListener _listener;
        private volatile bool _isStopped;
        private DateTime _moveCarLastAction = DateTime.Now;
        private const int MOVE_CAR_PERIOD_ACTION_MILLISECONDS = 100;
        private const int MOVE_CAR_ACTION_TIMEOUT_MILLISECONDS = 500;

        //Dependency objects
        private MotorController _motorController;
        private ServoController _servoController;
        private AutomaticDrive _automaticDrive;
        private Camera _camera;

        public HttpServer(MotorController motorController,
                          ServoController servoController,
                          AutomaticDrive automaticDrive,
                          Camera camera)
        {
            _motorController = motorController;
            _servoController = servoController;
            _automaticDrive = automaticDrive;
            _camera = camera;

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

        public async void Start()
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

                var request = string.Empty;
                var requestError = false;

                //Read request
                using (var inputStream = socket.InputStream)
                {
                    var data = new byte[BUFFER_SIZE];
                    var buffer = data.AsBuffer();

                    var startReadRequest = DateTime.Now;
                    while (!HttpGetRequestHasUrl(request))
                    {
                        if (DateTime.Now.Subtract(startReadRequest) >= TimeSpan.FromMilliseconds(5000))
                        {
                            requestError = true;
                            return;
                        }

                        var inputStreamReadTask = inputStream.ReadAsync(buffer, BUFFER_SIZE, InputStreamOptions.Partial);
                        var timeout = TimeSpan.FromMilliseconds(1000);
                        await TaskHelper.WithTimeoutAfterStart(ct => inputStreamReadTask.AsTask(ct), timeout);

                        request += Encoding.UTF8.GetString(data, 0, data.Length);
                    }
                }

                var requestMethod = request.Split('\n')[0];
                var requestParts = requestMethod.Split(' ');
                var relativeUrl = requestParts.Length > 1 ? requestParts[1] : string.Empty;

                //Write Response
                await WriteResponse(relativeUrl, request, requestError, socket.OutputStream);

                socket.InputStream.Dispose();
                socket.OutputStream.Dispose();
                socket.Dispose();
            }
            catch (TaskCanceledException) { }
            catch (Exception) { }
        }

        private async Task WriteResponse(string relativeUrl, string request, bool requestError, IOutputStream outputStream)
        {
            var relativeUrlLower = relativeUrl.ToLowerInvariant();

            //Request read not successfully
            if (requestError)
            {
                HttpServerResponse.WriteResponseError("Request wurde nicht vollständig übermittelt oder zu langsam übermittelt und ist daher nicht vollständig.", outputStream);
            }
            //Get javascript
            else if (relativeUrlLower.StartsWith("/javascript"))
            {
                HttpServerResponse.WriteResponseFile(ToFolderPath(relativeUrl), HttpContentType.JavaScript, outputStream);
            }
            //Get style
            else if (relativeUrlLower.StartsWith("/styles"))
            {
                HttpServerResponse.WriteResponseFile(ToFolderPath(relativeUrl), HttpContentType.Css, outputStream);
            }
            //Get image
            else if (relativeUrlLower.StartsWith("/images"))
            {
                HttpServerResponse.WriteResponseFile(ToFolderPath(relativeUrl), HttpContentType.Png, outputStream);
            }
            //Toggle automatic drive on/off
            else if (relativeUrlLower.StartsWith("/automaticdrive"))
            {
                var requestBodyResult = GetRequestBody(request);
                if (!requestBodyResult.Key)
                {
                    HttpServerResponse.WriteResponseError("Request wurde nicht vollständig übermittelt.", outputStream);
                }

                var automaticDriveOn = requestBodyResult.Value["automaticDrive"].GetBoolean();

                if (automaticDriveOn)
                {
                    _automaticDrive.Start();
                }
                else
                {
                    await _automaticDrive.Stop();
                }

                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Move car with command
            else if (relativeUrlLower.StartsWith("/carcontrolcommand"))
            {
                var requestBodyResult = GetRequestBody(request);
                if (!requestBodyResult.Key)
                {
                    HttpServerResponse.WriteResponseError("Request wurde nicht vollständig übermittelt.", outputStream);
                }

                var carControlCommandRequest = requestBodyResult.Value;
                var carControlCommand = new CarControlCommand
                {
                    DirectionControlUp = carControlCommandRequest["directionControlUp"].GetBoolean(),
                    DirectionControlLeft = carControlCommandRequest["directionControlLeft"].GetBoolean(),
                    DirectionControlRight = carControlCommandRequest["directionControlRight"].GetBoolean(),
                    DirectionControlDown = carControlCommandRequest["directionControlDown"].GetBoolean(),
                    SpeedControlForward = carControlCommandRequest["speedControlForward"].GetBoolean(),
                    SpeedControlBackward = carControlCommandRequest["speedControlBackward"].GetBoolean(),
                    SpeedControlLeftRight = carControlCommandRequest["speedControlLeftRight"].GetNumber(),
                    DirectionControlUpDownStepSpeed = 7
                };

                if (carControlCommand.DirectionControlLeft
                    && carControlCommand.DirectionControlRight)
                {
                    carControlCommand.DirectionControlLeft = false;
                    carControlCommand.DirectionControlRight = false;
                }
                
                CarMoveCommand(carControlCommand);

                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Get car speed
            else if (relativeUrlLower.StartsWith("/speed"))
            {
                var speedResponse = new JsonObject
                                        {
                                            { "RoundsPerMinute", JsonValue.CreateStringValue(SpeedSensor.RoundsPerMinute.ToString())},
                                            { "KilometerPerHour", JsonValue.CreateStringValue(string.Format("{0:0.00}", SpeedSensor.KilometerPerHour).Replace(".", ","))}
                                        };

                HttpServerResponse.WriteResponseJson(speedResponse.Stringify(), outputStream);
            }
            //Get speaker states (on/off) and automatic drive state (on/off)
            else if (relativeUrlLower.StartsWith("/getstate"))
            {
                var stateResponse = new JsonObject
                                {
                                    { "CarSpeakerOn", JsonValue.CreateBooleanValue(AudioPlayerController.CarSpeakerOn)},
                                    { "HeadsetSpeakerOn", JsonValue.CreateBooleanValue(AudioPlayerController.HeadsetSpeakerOn)},
                                    { "SoundModeOn", JsonValue.CreateBooleanValue(AudioPlayerController.SoundModeOn)},
                                    { "AutomaticDriveOn", JsonValue.CreateBooleanValue(_automaticDrive.IsRunning)}
                                };

                HttpServerResponse.WriteResponseJson(stateResponse.Stringify(), outputStream);
            }
            //Set all speaker on
            else if (relativeUrlLower.StartsWith("/speakeronoff?on=true"))
            {
                await AudioPlayerController.SetAllSpeakerOnOff(true);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set all speaker off
            else if (relativeUrlLower.StartsWith("/speakeronoff?on=false"))
            {
                await AudioPlayerController.SetAllSpeakerOnOff(false);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set car speaker on
            else if (relativeUrlLower.StartsWith("/carspeakeronoff?on=true"))
            {
                await AudioPlayerController.SetCarSpeakerOnOff(true);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set car speaker off
            else if (relativeUrlLower.StartsWith("/carspeakeronoff?on=false"))
            {
                await AudioPlayerController.SetCarSpeakerOnOff(false);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set headset speaker on
            else if (relativeUrlLower.StartsWith("/headsetspeakeronoff?on=true"))
            {
                await AudioPlayerController.SetHeadsetSpeakerOnOff(true);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set headset speaker off
            else if (relativeUrlLower.StartsWith("/headsetspeakeronoff?on=false"))
            {
                await AudioPlayerController.SetHeadsetSpeakerOnOff(false);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set sound mode on
            else if (relativeUrlLower.StartsWith("/soundmodeonoff?on=true"))
            {
                await AudioPlayerController.SetSoundModeOnOff(true);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Set sound mode off
            else if (relativeUrlLower.StartsWith("/soundmodeonoff?on=false"))
            {
                await AudioPlayerController.SetSoundModeOnOff(false);
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Shutdown Windows
            else if (relativeUrlLower.StartsWith("/ausschalten"))
            {
                await SystemController.Shutdown();
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Restart Windows
            else if (relativeUrlLower.StartsWith("/neustarten"))
            {
                await SystemController.Restart();
                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Get debug file
            else if (relativeUrlLower.StartsWith("/debug"))
            {
                var debugFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(Logger.FILE_NAME);
                if (debugFile != null)
                {
                    HttpServerResponse.WriteResponseFileFromLocalFolder(Logger.FILE_NAME, HttpContentType.Text, outputStream);
                }
                else
                {
                    HttpServerResponse.WriteResponseText("Debug file is empty", outputStream);
                }
            }
            //Get camera frame
            else if (relativeUrlLower.StartsWith("/videoframe"))
            {
                if (_camera.Frame != null)
                {
                    HttpServerResponse.WriteResponseFile(_camera.Frame, HttpContentType.Jpeg, outputStream);
                }
                else
                {
                    HttpServerResponse.WriteResponseError("Not camera fram available. Maybe there is an error or camera is not started.", outputStream);
                }
            }
            //Get desktop html view
            else if (relativeUrlLower.StartsWith("/desktop"))
            {
                HttpServerResponse.WriteResponseFile(@"\Views\Desktop.html", HttpContentType.Html, outputStream);
            }
            //Get mobile html view
            else if (relativeUrlLower.StartsWith("/mobile"))
            {
                HttpServerResponse.WriteResponseFile(@"\Views\Mobile.html", HttpContentType.Html, outputStream);
            }
            //Get view that redirect to desktop oder mobile view
            else
            {
                HttpServerResponse.WriteResponseFile(@"\Views\Index.html", HttpContentType.Html, outputStream);
            }
        }

        private KeyValuePair<bool, JsonObject> GetRequestBody(string request)
        {
            var regex = new Regex("<RequestBody>(.*)</RequestBody>");
            var requestBodyMatch = regex.Match(Uri.UnescapeDataString(request));

            if (requestBodyMatch.Groups.Count <= 1)
            {
                return new KeyValuePair<bool, JsonObject>(false, null);
            }

            var requestBody = requestBodyMatch.Groups[1].ToString();
            var jsonObject = JsonObject.Parse(requestBody);

            return new KeyValuePair<bool, JsonObject>(true, jsonObject);
        }

        private bool HttpGetRequestHasUrl(string httpRequest)
        {
            var regex = new Regex("^.*GET.*HTTP.*\\r\\n.*$", RegexOptions.Multiline);
            return regex.IsMatch(httpRequest.ToUpper());
        }

        private void CarMoveCommand(CarControlCommand carControlCommand)
        {
            var now = DateTime.Now;
            if (now.Subtract(_moveCarLastAction) >= TimeSpan.FromMilliseconds(MOVE_CAR_PERIOD_ACTION_MILLISECONDS)
                || (!carControlCommand.DirectionControlUp
                    && !carControlCommand.DirectionControlLeft
                    && !carControlCommand.DirectionControlRight
                    && !carControlCommand.DirectionControlDown
                    && !carControlCommand.SpeedControlForward
                    && !carControlCommand.SpeedControlBackward))
            {
                _motorController.MoveCar(carControlCommand, null);
                _servoController.MoveServo(carControlCommand);
                _moveCarLastAction = now;

                Task.Run(async () => { await CarControlCommandTimeout(now); });
            }
        }

        private async Task CarControlCommandTimeout(DateTime carControlCommand)
        {
            await Task.Delay(MOVE_CAR_ACTION_TIMEOUT_MILLISECONDS);

            if (_moveCarLastAction == carControlCommand)
            {
                _motorController.MoveCar(new CarControlCommand
                {
                    SpeedControlBackward = false,
                    SpeedControlForward = false,
                    DirectionControlLeft = false,
                    DirectionControlRight = false
                }, null);
            }
        }

        private string ToFolderPath(string relativeUrl)
        {
            var folderPath = relativeUrl.Replace('/', '\\');
            return folderPath;
        }
    }
}
