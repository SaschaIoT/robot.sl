﻿using robot.sl.Audio.AudioPlaying;
using robot.sl.CarControl;
using robot.sl.Devices;
using robot.sl.Sensors;
using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace robot.sl.Web
{
    public enum OpCode
    {
        Undefined = -1,
        Text = 129,
        Binary = 130
    }

    public class WebSocket
    {
        private IInputStream _inputStream;
        private IOutputStream _outputStream;
        private HttpServerRequest _httpServerRequest;
        private const uint BUFFER_SIZE = 3024;

        private DateTime _moveCarLastAction = DateTime.Now;
        private const int MOVE_CAR_PERIOD_ACTION_MILLISECONDS = 30;
        private const int MOVE_CAR_ACTION_TIMEOUT_MILLISECONDS = 500;
        private bool _moveCarStopped = true;

        //Dependencies
        private Camera _camera;
        private MotorController _motorController;
        private ServoController _servoController;
        private AutomaticDrive _automaticDrive;
        private Dance _dance;

        public WebSocket(StreamSocket socket,
                         HttpServerRequest httpServerRequest,
                         Camera camera,
                         MotorController motorController,
                         ServoController servoController,
                         AutomaticDrive automaticDrive,
                         Dance dance)
        {
            _inputStream = socket.InputStream;
            _outputStream = socket.OutputStream;
            _httpServerRequest = httpServerRequest;
            _motorController = motorController;
            _servoController = servoController;
            _camera = camera;
            _automaticDrive = automaticDrive;
            _dance = dance;
        }

        public async Task StartAsync()
        {
            if(await CheckWebSocketVersionSupportAsync())
            {
                await ReadFramesAsync();
            }
        }
        
        private async Task<bool> CheckWebSocketVersionSupportAsync()
        {
            var webSocketVersion = new Regex("Sec-WebSocket-Version:(.*)", RegexOptions.IgnoreCase).Match(_httpServerRequest.Request).Groups[1].Value.Trim();
            if(webSocketVersion != "13")
            {
                await WriteUpgradeRequiredAsync();

                return false;
            }
            else
            {
                await WriteHandshakeAsync();

                return true;
            }
        }

        private async Task WriteUpgradeRequiredAsync()
        {
            var response = Encoding.UTF8.GetBytes("HTTP/1.1 426 Upgrade Required" + Environment.NewLine
                + "Connection: Upgrade" + Environment.NewLine
                + "Upgrade: websocket" + Environment.NewLine
                + "Sec-WebSocket-Version: 13" + Environment.NewLine
                + Environment.NewLine);

            await _outputStream.WriteAsync(response.AsBuffer());
            await _outputStream.FlushAsync();
        }

        private async Task WriteHandshakeAsync()
        {
            var response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                + "Connection: Upgrade" + Environment.NewLine
                + "Upgrade: websocket" + Environment.NewLine
                + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                    SHA1.Create().ComputeHash(
                        Encoding.UTF8.GetBytes(
                            new Regex("Sec-WebSocket-Key:(.*)", RegexOptions.IgnoreCase).Match(_httpServerRequest.Request).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                        )
                    )
                ) + Environment.NewLine
                + Environment.NewLine);

            await _outputStream.WriteAsync(response.AsBuffer());
            await _outputStream.FlushAsync();
        }

        private async Task ProcessFrameAsync(string frameContent)
        {
            var content = JsonObject.Parse(frameContent);
            var command = content["command"].GetString();

            switch (command)
            {
                case "VideoFrame":

                    var cameraFrame = _camera.Frame.ToArray();
                    await WriteFrameAsync(cameraFrame, OpCode.Binary);

                    break;
                case "CarControlCommand":

                    var parameter = content["parameter"].GetObject();
                    var carControlCommand = new CarControlCommand
                    {
                        DirectionControlUp = parameter["directionControlUp"].GetBoolean(),
                        DirectionControlLeft = parameter["directionControlLeft"].GetBoolean(),
                        DirectionControlRight = parameter["directionControlRight"].GetBoolean(),
                        DirectionControlDown = parameter["directionControlDown"].GetBoolean(),
                        SpeedControlForward = parameter["speedControlForward"].GetBoolean(),
                        SpeedControlBackward = parameter["speedControlBackward"].GetBoolean(),
                        DirectionControlUpDownStepSpeed = 7
                    };

                    if (carControlCommand.DirectionControlLeft
                        && carControlCommand.DirectionControlRight)
                    {
                        carControlCommand.DirectionControlLeft = false;
                        carControlCommand.DirectionControlRight = false;
                    }

                    await CarMoveCommand(carControlCommand);

                    var carControlCommandResponse = new JsonObject
                        {
                            { "command", JsonValue.CreateStringValue("CarControlCommand") }
                        };

                    await WriteFrameAsync(carControlCommandResponse.Stringify());

                    break;
                case "Speed":

                    var speed = new JsonObject
                        {
                            { "command", JsonValue.CreateStringValue("Speed") },
                            { "parameter", new JsonObject
                                {
                                    { "RoundsPerMinute", JsonValue.CreateStringValue(SpeedSensor.RoundsPerMinute.ToString())},
                                    { "KilometerPerHour", JsonValue.CreateStringValue(string.Format("{0:0.00}", SpeedSensor.KilometerPerHour).Replace(".", ","))}
                                }
                            }
                        };

                    await WriteFrameAsync(speed.Stringify());

                    break;
                case "State":

                    var state = new JsonObject
                        {
                            { "command", JsonValue.CreateStringValue("State") },
                            { "parameter", new JsonObject
                                {
                                    { "CarSpeakerOn", JsonValue.CreateBooleanValue(AudioPlayerController.CarSpeakerOn)},
                                    { "HeadsetSpeakerOn", JsonValue.CreateBooleanValue(AudioPlayerController.HeadsetSpeakerOn)},
                                    { "SoundModeOn", JsonValue.CreateBooleanValue(AudioPlayerController.SoundModeOn)},
                                    { "AutomaticDriveOn", JsonValue.CreateBooleanValue(_automaticDrive.IsRunning)},
                                    { "DanceOn", JsonValue.CreateBooleanValue(_dance.IsRunning)}
                                }
                            }
                        };

                    await WriteFrameAsync(state.Stringify());

                    break;
                //Commet in for server side frame rate measurement
                //case "ServerVideoFrameRate":

                //    var serverVideoFrameRate = new JsonObject
                //        {
                //            { "command", JsonValue.CreateStringValue("ServerVideoFrameRate") },
                //            { "parameter", new JsonObject
                //                {
                //                { "FrameRate", JsonValue.CreateNumberValue(_camera.FrameRate)}
                //                }
                //            }
                //        };

                //    await WriteFrame(serverVideoFrameRate.Stringify());

                //    break;
            }
        }

        private async Task WriteFrameAsync(string data)
        {
            var dataBytes = Encoding.UTF8.GetBytes(data);
            await WriteFrameAsync(dataBytes, OpCode.Text);
        }

        private async Task WriteFrameAsync(byte[] data, OpCode opCode)
        {
            byte[] header = new byte[2];

            if (opCode == OpCode.Text)
            {
                header[0] = 129;
            }
            else if (opCode == OpCode.Binary)
            {
                header[0] = 130;
            }

            if (data.Length <= 125)
            {
                header[1] = (byte)data.Length;
            }
            else if (data.Length >= 126 && data.Length <= 65535)
            {
                header[1] = 126;

                var length = Convert.ToUInt16(data.Length);
                var lengthBytes = BitConverter.GetBytes(length);
                Array.Reverse(lengthBytes, 0, lengthBytes.Length);

                header = header.Concat(lengthBytes).ToArray();

                //var lengthBytes = new byte[2];
                //lengthBytes[0] = (byte)((data.Length >> 8) & 255);
                //lengthBytes[1] = (byte)(data.Length & 255);

                //lengthBytes.CopyTo(header, 2);
            }
            else
            {
                header[1] = 127;

                var length = Convert.ToUInt64(data.Length);
                var lengthBytes = BitConverter.GetBytes(length);
                Array.Reverse(lengthBytes, 0, lengthBytes.Length);

                header = header.Concat(lengthBytes).ToArray();
            }

            var headerData = header.Concat(data).ToArray();

            await _outputStream.WriteAsync(headerData.AsBuffer());
            await _outputStream.FlushAsync();
        }

        private async Task ReadFramesAsync()
        {
            var data = new byte[BUFFER_SIZE];
            var buffer = data.AsBuffer();
            var frameData = Array.Empty<byte>();

            while (true)
            {
                await Task.Delay(1);
                
                var readBytes = await _inputStream.ReadAsync(buffer, BUFFER_SIZE, InputStreamOptions.Partial);

                var readBytesLength = (int)readBytes.Length;

                if (readBytesLength >= 2)
                {
                    var newData = data.Take(readBytesLength);
                    frameData = frameData.Concat(newData).ToArray();

                    readBytesLength = frameData.Length;

                    var opCode = OpCode.Undefined;

                    if (frameData[0] == 136) //Close frame was send
                    {
                        var closeFrame = new byte[] { 136, 0 };
                        await _outputStream.WriteAsync(closeFrame.AsBuffer());
                        await _outputStream.FlushAsync();
                        return;
                    }
                    else if (frameData[0] == 129)
                    {
                        opCode = OpCode.Text;
                    }
                    else if (frameData[0] == 130)
                    {
                        opCode = OpCode.Binary;
                    }

                    var contentLength = (long)(frameData[1] & 127);

                    var indexFirstMask = 2;

                    if (contentLength == 126)
                    {
                        if (readBytesLength < 4)
                            continue;

                        Array.Reverse(frameData, 2, 2);

                        contentLength = BitConverter.ToInt16(frameData, 2);
                        indexFirstMask = 4;
                    }
                    else if (contentLength == 127)
                    {
                        if (readBytesLength < 10)
                            continue;

                        Array.Reverse(frameData, 2, 8);

                        contentLength = BitConverter.ToInt64(frameData, 2);

                        indexFirstMask = 10;
                    }

                    var maskLength = 4;
                    var indexFirstDataByte = indexFirstMask + maskLength;

                    var frameLength = contentLength + indexFirstDataByte;

                    if (readBytesLength < frameLength) //Is complete frame read?
                        continue;

                    var masks = frameData.Skip(indexFirstMask).Take(maskLength).ToArray();

                    byte[] decoded = new byte[contentLength];

                    for (int i = indexFirstDataByte, j = 0; i < frameLength; i++, j++)
                    {
                        decoded[j] = (byte)(frameData[i] ^ masks.ElementAt(j % 4));
                    }

                    if (frameData.Length > frameLength)
                    {
                        frameData = frameData.Skip(frameData.Length).ToArray();
                    }
                    else
                    {
                        frameData = Array.Empty<byte>();
                    }

                    data = new byte[BUFFER_SIZE];
                    buffer = data.AsBuffer();

                    if (opCode == OpCode.Text)
                    {
                        await ProcessFrameAsync(Encoding.UTF8.GetString(decoded, 0, decoded.Length));
                    }
                }
            }
        }

        private async Task CarMoveCommand(CarControlCommand carControlCommand)
        {
            var stopp = (!carControlCommand.DirectionControlUp
                         && !carControlCommand.DirectionControlLeft
                         && !carControlCommand.DirectionControlRight
                         && !carControlCommand.DirectionControlDown
                         && !carControlCommand.SpeedControlForward
                         && !carControlCommand.SpeedControlBackward);

            var now = DateTime.Now;
            if (((stopp && _moveCarStopped) == false
                 && now.Subtract(_moveCarLastAction) >= TimeSpan.FromMilliseconds(MOVE_CAR_PERIOD_ACTION_MILLISECONDS))
                || (stopp && _moveCarStopped == false))
            {
                _moveCarStopped = stopp;

                await _motorController.MoveCarAsync(carControlCommand, MotorCommandSource.Other);
                await _servoController.MoveServo(carControlCommand);
                _moveCarLastAction = now;

                CarControlCommandTimeoutAsync(now);
            }
        }

        private async void CarControlCommandTimeoutAsync(DateTime carControlCommand)
        {
            await Task.Delay(MOVE_CAR_ACTION_TIMEOUT_MILLISECONDS);

            if (_moveCarLastAction == carControlCommand)
            {
                await _motorController.MoveCarAsync(new CarControlCommand
                {
                    SpeedControlBackward = false,
                    SpeedControlForward = false,
                    DirectionControlLeft = false,
                    DirectionControlRight = false
                }, MotorCommandSource.Other);
            }
        }
    }
}
