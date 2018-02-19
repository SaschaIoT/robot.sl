using robot.sl.Audio.AudioPlaying;
using robot.sl.CarControl;
using robot.sl.Helper;
using robot.sl.Sensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace robot.sl.Audio
{
    public class AutomaticSpeakController
    {
        public CarMoveCommand CarMoveCommand { get; set; }
        private volatile bool _stopping = false;
        private volatile bool _isStopped = true;
        private DateTime? _carNotMoving = null;
        private CancellationTokenSource _cancellationTokenSource;

        //Dependency objects
        private AccelerometerGyroscopeSensor _accelerometer;

        public AutomaticSpeakController(AccelerometerGyroscopeSensor accelerometer)
        {
            _accelerometer = accelerometer;
        }

        public async Task StopAsync()
        {
            _stopping = true;

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel(true);
                _cancellationTokenSource = null;
            }

            while (_isStopped == false)
            {
                await Task.Delay(10);
            }

            _stopping = false;
        }

        public void Start()
        {
            Task.Factory.StartNew(() =>
            {

                StartInternalAsync().Wait();

            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
             .AsAsyncAction()
             .AsTask()
             .ContinueWith((t) =>
             {

                 Logger.WriteAsync(nameof(AutomaticSpeakController), t.Exception).Wait();
                 SystemController.ShutdownApplicationAsync(true).Wait();

             }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task StartInternalAsync()
        {
            _isStopped = false;
            var random = new Random();
            var randomMinutes = 0;

            DateTime? turnLeftStart = null;
            var turnLeftSpoken = false;
            DateTime? turnRightStart = null;
            var turnRightSpoken = false;
            var turns = new List<double>();

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            try
            {
                while (_stopping == false)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var acceleration = _accelerometer.ReadLinearAcceleration();

                    var carpetVibration = 0.2;
                    var vibrationSpeed = (((Math.Abs(acceleration.AccelerationX) + Math.Abs(acceleration.AccelerationY) + Math.Abs(acceleration.AccelerationZ))) / 3) - carpetVibration;

                    turns.Add(acceleration.GyroZ);
                    if (turns.Count >= 20)
                    {
                        var turnLeft = turns.Max() >= 60;
                        var turnRight = turns.Min() <= -60;

                        if (turnLeft
                            && !turnLeftStart.HasValue
                            && !turnLeftSpoken)
                        {
                            turnLeftStart = DateTime.Now;
                        }
                        else if (!turnLeft)
                        {
                            turnLeftSpoken = false;
                            turnLeftStart = null;
                        }

                        if (turnRight
                            && !turnRightStart.HasValue
                            && !turnRightSpoken)
                        {
                            turnRightStart = DateTime.Now;
                        }
                        else if (!turnRight)
                        {
                            turnRightSpoken = false;
                            turnRightStart = null;
                        }

                        turns.Clear();
                    }

                    if ((CarMoveCommand?.Speed == 0 || CarMoveCommand?.Speed == null)
                        && !_carNotMoving.HasValue)
                    {
                        _carNotMoving = DateTime.Now;
                        randomMinutes = random.Next(10, 15);
                    }
                    else if (CarMoveCommand != null && CarMoveCommand.Speed > 0)
                    {
                        _carNotMoving = null;
                    }

                    //Car not moving to long
                    if (_carNotMoving.HasValue
                        && DateTime.Now >= _carNotMoving.Value.AddMinutes(randomMinutes))
                    {
                        await AudioPlayerController.PlayAndWaitAsync(AudioName.Steht, cancellationToken);
                        _carNotMoving = null;
                    }
                    //Strong car vibration
                    else if (vibrationSpeed >= 0.35)
                    {
                        await AudioPlayerController.PlayAndWaitAsync(AudioName.StarkeVibration, cancellationToken);
                    }
                    //Turn to long left
                    else if (turnLeftStart.HasValue
                             && DateTime.Now >= turnLeftStart.Value.AddSeconds(5))
                    {
                        turnLeftStart = null;
                        turnLeftSpoken = true;
                        await AudioPlayerController.PlayAndWaitAsync(AudioName.TurnToLongLeft, cancellationToken);
                    }
                    //Turn to long right
                    else if (turnRightStart.HasValue
                             && DateTime.Now >= turnRightStart.Value.AddSeconds(5))
                    {
                        turnRightStart = null;
                        turnRightSpoken = true;
                        await AudioPlayerController.PlayAndWaitAsync(AudioName.TurnToLongRight, cancellationToken);
                    }

                    await Task.Delay(10, cancellationToken);
                }
            }
            catch (OperationCanceledException) { }

            _isStopped = true;
        }
    }
}
