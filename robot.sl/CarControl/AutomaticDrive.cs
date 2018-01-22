using robot.sl.Audio.AudioPlaying;
using robot.sl.Helper;
using robot.sl.Sensors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace robot.sl.CarControl
{
    public class AutomaticDrive
    {
        private const int DS_ULTRASONIC_MIN_RANGE_MILLIMETERS = 350;
        private const int DS_LASER_DOWN_MIN_RANGE_MILLIMETERS = 700;
        private const int DS_LASER_UP_MIN_RANGE_MILLIMETERS = 700;
        
        private const int CHECK_FORWARD_HANG_AFTER = 700;

        //Dependeny objects
        private MotorController _motorController;
        private ServoController _servoController;
        private DistanceSensorUltrasonic _distanceSensorUltrasonic;
        private DistanceSensorLaser _distanceSensorLaserUp;
        private DistanceSensorLaser _distanceSensorLaserDown;

        public bool IsRunning
        {
            get
            {
                return !_isStopped;
            }
        }

        private const double SPEED = 0.55;

        private ManualResetEvent _threadWaiter = new ManualResetEvent(false);
        private CancellationTokenSource _cancellationTokenSource;

        private volatile bool _isStopped = true;
        private volatile bool _isStopping = false;
        private bool _isForward = true;
        private DateTime? _drivingForward;

        public AutomaticDrive(MotorController motorController,
                              ServoController servoController,
                              DistanceSensorUltrasonic distanceSensorUltrasonic,
                              DistanceSensorLaser distanceSensorLaserUp,
                              DistanceSensorLaser distanceSensorLaserDown)
        {
            _motorController = motorController;
            _servoController = servoController;
            _distanceSensorUltrasonic = distanceSensorUltrasonic;
            _distanceSensorLaserUp = distanceSensorLaserUp;
            _distanceSensorLaserDown = distanceSensorLaserDown;
        }

        public void Start()
        {
            Task.Factory.StartNew(() =>
            {

                StartInternalAsync().Wait();
                _threadWaiter.WaitOne();

            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
             .AsAsyncAction()
             .AsTask()
             .ContinueWith((t) =>
             {
                 Logger.WriteAsync(nameof(AutomaticDrive), t.Exception).Wait();
                 SystemController.ShutdownApplicationAsync(true).Wait();

             }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task StartInternalAsync()
        {
            if (!_isStopped)
            {
                return;
            }

            _distanceSensorLaserUp.ClearDistancesFiltered();
            _distanceSensorLaserDown.ClearDistancesFiltered();
            _distanceSensorUltrasonic.ClearDistancesFiltered();
            _distanceSensorUltrasonic.Start();

            _isStopped = false;

            _cancellationTokenSource = new CancellationTokenSource();

            StartInternal(_cancellationTokenSource.Token);

            await AudioPlayerController.PlayAsync(AudioName.StartAutomaticDrive);
        }

        public async Task StopAsync()
        {
            await StopAsync(true);
        }

        public async Task StopAsync(bool speak)
        {
            if (_isStopped)
            {
                return;
            }

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel(true);
            }

            _isStopping = true;

            while (!_isStopped)
            {
                await Task.Delay(10);
            }

            _threadWaiter.Set();

            _isStopping = false;

            await _distanceSensorUltrasonic.StopAsync();

            if (speak)
                await AudioPlayerController.PlayAsync(AudioName.StopAutomaticDrive);
        }

        public async Task StartStopToggleAsync()
        {
            if (!_isStopped)
            {
                await StopAsync();
            }
            else
            {
                Start();
            }
        }

        private async void StartInternal(CancellationToken cancellationToken)
        {
            try
            {
                _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalMiddle);
                _servoController.PwmController.SetPwm(Servo.DistanceSensorVertical, 0, ServoPositions.DistanceSensorVerticalMiddle);

                await Task.Delay(TimeSpan.FromMilliseconds(1500), cancellationToken);

                _isForward = true;

                while (!_isStopping)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken);

                    CarMoveCommand carMoveCommand = null;

                    var freeDirection = await GetFreeDirectionAsync(cancellationToken);

                    if (freeDirection == FreeDirection.Forward)
                    {
                        if (_isStopping)
                            break;
                        
                        if (_drivingForward.HasValue && DateTime.Now >= _drivingForward.Value.AddMilliseconds(CHECK_FORWARD_HANG_AFTER))
                        {
                            if (await CheckHangAsync(cancellationToken))
                                continue;
                        }

                        carMoveCommand = new CarMoveCommand
                        {
                            Speed = SPEED
                        };

                        _motorController.MoveCar(null, carMoveCommand);

                        _drivingForward = DateTime.Now;
                    }
                    else
                    {
                        _drivingForward = null;

                        if (freeDirection == FreeDirection.Left)
                        {
                            if (_isStopping)
                                break;

                            await TurnLeftAsync(700, cancellationToken);
                        }
                        else if (freeDirection == FreeDirection.LeftMiddle)
                        {
                            if (_isStopping)
                                break;

                            await TurnLeftAsync(350, cancellationToken);
                        }
                        else if (freeDirection == FreeDirection.Right)
                        {
                            if (_isStopping)
                                break;

                            await TurnRightAsync(700, cancellationToken);
                        }
                        else if (freeDirection == FreeDirection.RightMiddle)
                        {
                            if (_isStopping)
                                break;

                            await TurnRightAsync(350, cancellationToken);
                        }
                        else if (freeDirection == FreeDirection.None)
                        {
                            if (_isStopping)
                                break;

                            await TurnFullAsync(cancellationToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }

            var carMoveCommandEnd = new CarMoveCommand
            {
                Speed = 0
            };

            _motorController.MoveCar(null, carMoveCommandEnd);

            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalLeft);
            _servoController.PwmController.SetPwm(Servo.DistanceSensorVertical, 0, ServoPositions.DistanceSensorVerticalTop);

            _isStopped = true;
        }

        private async Task TurnRightAsync(int milliseconds, CancellationToken cancellationToken)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = 1,
                RightCircle = true
            };

            _motorController.MoveCar(null, carMoveCommand);

            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);

            if (await CheckHangAsync(cancellationToken))
                return;

            carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            _motorController.MoveCar(null, carMoveCommand);
        }

        private async Task TurnBackwardAsync(int milliseconds, CancellationToken cancellationToken)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = SPEED,
                ForwardBackward = true
            };

            _motorController.MoveCar(null, carMoveCommand);

            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);

            if (await CheckHangAsync(cancellationToken))
                return;

            carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            _motorController.MoveCar(null, carMoveCommand);
        }

        private async Task TurnLeftAsync(int milliseconds, CancellationToken cancellationToken)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = 1,
                LeftCircle = true
            };

            _motorController.MoveCar(null, carMoveCommand);

            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);

            if (await CheckHangAsync(cancellationToken))
                return;

            carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            _motorController.MoveCar(null, carMoveCommand);
        }

        private async Task TurnFullAsync(CancellationToken cancellationToken)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = 1,
                LeftCircle = true
            };

            _motorController.MoveCar(null, carMoveCommand);

            await Task.Delay(TimeSpan.FromMilliseconds(1250), cancellationToken);

            if (await CheckHangAsync(cancellationToken))
                return;

            carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            _motorController.MoveCar(null, carMoveCommand);
        }

        private async Task<bool> CheckHangAsync(CancellationToken cancellationToken)
        {
            var hang = false;

            if (SpeedSensor.IsDriving == false)
            {
                hang = true;

                var carMoveCommand = new CarMoveCommand
                {
                    Speed = 0
                };

                _motorController.MoveCar(null, carMoveCommand);

                await AudioPlayerController.PlayAndWaitAsync(AudioName.AutomatischesFahrenFesthaengen, cancellationToken);

                await TurnBackwardAsync(1000, cancellationToken);
                await TurnLeftAsync(700, cancellationToken);

                carMoveCommand = new CarMoveCommand
                {
                    Speed = 0
                };

                _motorController.MoveCar(null, carMoveCommand);
            }

            return hang;
        }

        private async Task<FreeDirection> GetFreeDirectionAsync(CancellationToken cancellationToken)
        {
            if (_isForward == false)
            {
                _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalMiddle);
                await Task.Delay(500, cancellationToken);
            }

            var dsLaserDistanceUp = 0;
            var dsLaserDistanceDown = 0;

            var dsUltrasonicDistanceTask = _distanceSensorUltrasonic.GetDistanceFiltered();
            var dsDistanceSensorLaserUpTask = Task.Factory.StartNew(() => dsLaserDistanceUp = _distanceSensorLaserUp.GetDistanceFiltered());
            var dsDistanceSensorLaserDownTask = Task.Factory.StartNew(() => dsLaserDistanceDown = _distanceSensorLaserDown.GetDistanceFiltered());

            await Task.WhenAll(dsUltrasonicDistanceTask, dsDistanceSensorLaserUpTask, dsDistanceSensorLaserDownTask);
            var dsUltrasonicDistance = dsUltrasonicDistanceTask.Result;

            if (dsUltrasonicDistance > DS_ULTRASONIC_MIN_RANGE_MILLIMETERS
                && dsLaserDistanceUp > DS_LASER_UP_MIN_RANGE_MILLIMETERS
                && dsLaserDistanceDown > DS_LASER_DOWN_MIN_RANGE_MILLIMETERS)
            {
                _isForward = true;
                return FreeDirection.Forward;
            }

            _distanceSensorLaserUp.ClearDistancesFiltered();
            _distanceSensorLaserDown.ClearDistancesFiltered();
            _distanceSensorUltrasonic.ClearDistancesFiltered();

            _isForward = false;

            var carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            _motorController.MoveCar(null, carMoveCommand);

            //Left
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalLeft);
            await Task.Delay(500, cancellationToken);
            dsUltrasonicDistance = await _distanceSensorUltrasonic.GetDistance();

            if (dsUltrasonicDistance > DS_ULTRASONIC_MIN_RANGE_MILLIMETERS)
            {
                return FreeDirection.Left;
            }

            //LeftMiddle
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalLeftMiddle);
            await Task.Delay(250, cancellationToken);
            dsUltrasonicDistance = await _distanceSensorUltrasonic.GetDistance();

            if (dsUltrasonicDistance > DS_ULTRASONIC_MIN_RANGE_MILLIMETERS)
            {
                return FreeDirection.LeftMiddle;
            }

            //RightMiddle
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalRightMiddle);
            await Task.Delay(500, cancellationToken);
            dsUltrasonicDistance = await _distanceSensorUltrasonic.GetDistance();

            if (dsUltrasonicDistance > DS_ULTRASONIC_MIN_RANGE_MILLIMETERS)
            {
                return FreeDirection.RightMiddle;
            }

            //Right
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalRight);
            await Task.Delay(250, cancellationToken);
            dsUltrasonicDistance = await _distanceSensorUltrasonic.GetDistance();

            if (dsUltrasonicDistance > DS_ULTRASONIC_MIN_RANGE_MILLIMETERS)
            {
                return FreeDirection.Right;
            }

            return FreeDirection.None;
        }
    }
}