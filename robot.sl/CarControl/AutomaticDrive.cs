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
        private const int DS_ULTRASONIC_MIN_RANGE_MILLIMETERS_DRIVING = 350;
        private const int DS_ULTRASONIC_MIN_RANGE_MILLIMETERS_FREE_DIRECTION_SEARCH = 700;
        private const int DS_LASER_TOP_MIN_RANGE_MILLIMETERS = 700;
        private const int DS_LASER_MIDDLE_TOP_MIN_RANGE_MILLIMETERS = 700;
        private const int DS_LASER_MIDDLE_BOTTOM_MIN_RANGE_MILLIMETERS = 700;
        private const int DS_LASER_BOTTOM_MAX_RANGE_MILLIMETERS = 380;

        private const int CHECK_FORWARD_HANG_AFTER = 1500;

        //Dependeny objects
        private MotorController _motorController;
        private ServoController _servoController;
        private DistanceSensorUltrasonic _distanceSensorUltrasonic;
        private DistanceSensorLaser _distanceSensorLaserTop;
        private DistanceSensorLaser _distanceSensorLaserMiddleTop;
        private DistanceSensorLaser _distanceSensorLaserMiddleBottom;
        private DistanceSensorLaser _distanceSensorLaserBottom;

        public bool IsRunning
        {
            get
            {
                return _isStopped == false;
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
                              DistanceSensorLaser distanceSensorLaserTop,
                              DistanceSensorLaser distanceSensorLaserMiddleTop,
                              DistanceSensorLaser distanceSensorLaserMiddleBottom,
                              DistanceSensorLaser distanceSensorLaserBottom)
        {
            _motorController = motorController;
            _servoController = servoController;
            _distanceSensorUltrasonic = distanceSensorUltrasonic;
            _distanceSensorLaserTop = distanceSensorLaserTop;
            _distanceSensorLaserMiddleTop = distanceSensorLaserMiddleTop;
            _distanceSensorLaserMiddleBottom = distanceSensorLaserMiddleBottom;
            _distanceSensorLaserBottom = distanceSensorLaserBottom;
        }

        public async Task StartAsync()
        {
            await AutomaticDriveSynchronous.Call(async () =>
            {
                if (_isStopped == false)
                {
                    await AudioPlayerController.PlayAsync(AudioName.AutomaticDriveOnAlready);

                    return;
                }

                _isStopped = false;

                StartInternalAsync();
            });
        }

        private void StartInternalAsync()
        {
            Task.Factory.StartNew(() =>
            {
                _distanceSensorLaserTop.ClearDistancesFiltered();
                _distanceSensorLaserMiddleTop.ClearDistancesFiltered();
                _distanceSensorLaserMiddleBottom.ClearDistancesFiltered();
                _distanceSensorLaserBottom.ClearDistancesFiltered();
                _distanceSensorUltrasonic.ClearDistancesFiltered();
                _distanceSensorUltrasonic.Start();

                _cancellationTokenSource = new CancellationTokenSource();

                StartInternal(_cancellationTokenSource.Token);

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

        public async Task StopAsync()
        {
            await StopAsync(true, true);
        }

        public async Task StopAsync(bool speakOff, bool speakOffAlready)
        {
            await AutomaticDriveSynchronous.Call(async () =>
            {
                if (_isStopped)
                {
                    if (speakOffAlready)
                        await AudioPlayerController.PlayAsync(AudioName.AutomaticDriveOffAlready);

                    return;
                }

                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel(true);
                    _cancellationTokenSource = null;
                }

                _isStopping = true;

                while (_isStopped == false)
                {
                    await Task.Delay(10);
                }

                _threadWaiter.Set();

                _isStopping = false;

                await _distanceSensorUltrasonic.StopAsync();

                if (speakOff)
                    await AudioPlayerController.PlayAsync(AudioName.AutomaticDriveOff);
            });
        }

        public async Task StartStopToggleAsync()
        {
            if (_isStopped == false)
            {
                await StopAsync();
            }
            else
            {
                await StartAsync();
            }
        }

        private async void StartInternal(CancellationToken cancellationToken)
        {
            try
            {
                var carMoveCommand = new CarMoveCommand
                {
                    Speed = 0
                };

                await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);

                _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalMiddle);
                _servoController.PwmController.SetPwm(Servo.DistanceSensorVertical, 0, ServoPositions.DistanceSensorVerticalMiddle);

                var startAutomaticDriveSpeechTask = AudioPlayerController.PlayAndWaitAsync(AudioName.AutomaticDriveOn, cancellationToken);
                var waitServoPositionTask = Task.Delay(TimeSpan.FromMilliseconds(1500), cancellationToken);

                await Task.WhenAll(startAutomaticDriveSpeechTask, waitServoPositionTask);

                _isForward = true;

                while (_isStopping == false)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken);

                    var freeDirection = await GetFreeDirectionAsync(cancellationToken);

                    if (freeDirection == FreeDirection.Forward)
                    {
                        if (_isStopping)
                            break;

                        if (_drivingForward.HasValue && DateTime.Now >= _drivingForward.Value.AddMilliseconds(CHECK_FORWARD_HANG_AFTER))
                        {
                            if (await CheckHangAsync(cancellationToken))
                            {
                                _drivingForward = null;
                                continue;
                            }
                        }

                        carMoveCommand = new CarMoveCommand
                        {
                            Speed = SPEED
                        };

                        await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);

                        if (_drivingForward.HasValue == false)
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

            await _motorController.MoveCarAsync(carMoveCommandEnd, MotorCommandSource.AutomaticDrive);

            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalLeft);
            _servoController.PwmController.SetPwm(Servo.DistanceSensorVertical, 0, ServoPositions.DistanceSensorVerticalTop);

            _drivingForward = null;
            _isStopped = true;
        }

        private async Task TurnRightAsync(int milliseconds, CancellationToken cancellationToken)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = 1,
                RightCircle = true
            };

            await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);

            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);

            if (await CheckHangAsync(cancellationToken))
                return;

            carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);
        }

        private async Task TurnBackwardAsync(int milliseconds, CancellationToken cancellationToken)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = SPEED,
                ForwardBackward = true
            };

            await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);

            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);

            if (await CheckHangAsync(cancellationToken))
                return;

            carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);
        }

        private async Task TurnLeftAsync(int milliseconds, CancellationToken cancellationToken)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = 1,
                LeftCircle = true
            };

            await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);

            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);

            if (await CheckHangAsync(cancellationToken))
                return;

            carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);
        }

        private async Task TurnFullAsync(CancellationToken cancellationToken)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = 1,
                LeftCircle = true
            };

            await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);

            await Task.Delay(TimeSpan.FromMilliseconds(1250), cancellationToken);

            if (await CheckHangAsync(cancellationToken))
                return;

            carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);
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

                await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);

                await AudioPlayerController.PlayAndWaitAsync(AudioName.AutomatischesFahrenFesthaengen, cancellationToken);

                await TurnBackwardAsync(1000, cancellationToken);
                await TurnLeftAsync(700, cancellationToken);

                carMoveCommand = new CarMoveCommand
                {
                    Speed = 0
                };

                await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);
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

            var dsLaserDistanceTop = 0;
            var dsLaserDistanceMiddleTop = 0;
            var dsLaserDistanceMiddleBottom = 0;
            var dsLaserDistanceBottom = 0;

            var dsUltrasonicDistanceTask = _distanceSensorUltrasonic.GetDistanceFiltered();
            var dsDistanceSensorLaserTopTask = Task.Factory.StartNew(() => dsLaserDistanceTop = _distanceSensorLaserTop.GetDistanceFiltered());
            var dsDistanceSensorLaserMiddleTopTask = Task.Factory.StartNew(() => dsLaserDistanceMiddleTop = _distanceSensorLaserMiddleTop.GetDistanceFiltered());
            var dsDistanceSensorLaserMiddleBottomTask = Task.Factory.StartNew(() => dsLaserDistanceMiddleBottom = _distanceSensorLaserMiddleBottom.GetDistanceFiltered());
            var dsDistanceSensorLaserBottomTask = Task.Factory.StartNew(() => dsLaserDistanceBottom = _distanceSensorLaserBottom.GetDistanceFiltered());

            await Task.WhenAll(dsUltrasonicDistanceTask, dsDistanceSensorLaserTopTask, dsDistanceSensorLaserMiddleTopTask, dsDistanceSensorLaserMiddleBottomTask, dsDistanceSensorLaserBottomTask);
            var dsUltrasonicDistance = dsUltrasonicDistanceTask.Result;

            if (dsUltrasonicDistance > DS_ULTRASONIC_MIN_RANGE_MILLIMETERS_DRIVING
                && dsLaserDistanceTop > DS_LASER_TOP_MIN_RANGE_MILLIMETERS
                && dsLaserDistanceMiddleTop > DS_LASER_MIDDLE_TOP_MIN_RANGE_MILLIMETERS
                && dsLaserDistanceMiddleBottom > DS_LASER_MIDDLE_BOTTOM_MIN_RANGE_MILLIMETERS
                && dsLaserDistanceBottom <= DS_LASER_BOTTOM_MAX_RANGE_MILLIMETERS)
            {
                _isForward = true;
                return FreeDirection.Forward;
            }

            _distanceSensorLaserTop.ClearDistancesFiltered();
            _distanceSensorLaserMiddleTop.ClearDistancesFiltered();
            _distanceSensorLaserMiddleBottom.ClearDistancesFiltered();
            _distanceSensorLaserBottom.ClearDistancesFiltered();
            _distanceSensorUltrasonic.ClearDistancesFiltered();

            _isForward = false;

            var carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);

            //Left
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalLeft);
            await Task.Delay(500, cancellationToken);
            dsUltrasonicDistance = await _distanceSensorUltrasonic.GetDistance();

            if (dsUltrasonicDistance > DS_ULTRASONIC_MIN_RANGE_MILLIMETERS_FREE_DIRECTION_SEARCH)
            {
                return FreeDirection.Left;
            }

            //LeftMiddle
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalLeftMiddle);
            await Task.Delay(250, cancellationToken);
            dsUltrasonicDistance = await _distanceSensorUltrasonic.GetDistance();

            if (dsUltrasonicDistance > DS_ULTRASONIC_MIN_RANGE_MILLIMETERS_FREE_DIRECTION_SEARCH)
            {
                return FreeDirection.LeftMiddle;
            }

            //RightMiddle
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalRightMiddle);
            await Task.Delay(500, cancellationToken);
            dsUltrasonicDistance = await _distanceSensorUltrasonic.GetDistance();

            if (dsUltrasonicDistance > DS_ULTRASONIC_MIN_RANGE_MILLIMETERS_FREE_DIRECTION_SEARCH)
            {
                return FreeDirection.RightMiddle;
            }

            //Right
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalRight);
            await Task.Delay(250, cancellationToken);
            dsUltrasonicDistance = await _distanceSensorUltrasonic.GetDistance();

            if (dsUltrasonicDistance > DS_ULTRASONIC_MIN_RANGE_MILLIMETERS_FREE_DIRECTION_SEARCH)
            {
                return FreeDirection.Right;
            }

            return FreeDirection.None;
        }
    }
}