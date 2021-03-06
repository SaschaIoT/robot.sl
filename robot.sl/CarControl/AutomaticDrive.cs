﻿using robot.sl.Audio.AudioPlaying;
using robot.sl.Helper;
using robot.sl.Sensors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace robot.sl.CarControl
{
    public class AutomaticDrive
    {
        private const int DS_ULTRASONIC_MIN_RANGE_CENTIMETERS = 60;
        private const int DS_LASER_ANALOG_TOP_MIN_RANGE_CENTIMETERS = 25;
        private const int DS_LASER_ANALOG_TOP_MAX_RANGE_CENTIMETERS = 65;
        private const int DS_LASER_MIDDLE_TOP_MIN_RANGE_MILLIMETERS = 700;
        private const int DS_LASER_MIDDLE_BOTTOM_MIN_RANGE_MILLIMETERS = 700;
        private const int DS_LASER_BOTTOM_MIN_RANGE_MILLIMETERS = 700;

        private const int CHECK_FORWARD_HANG_AFTER = 1500;
        private const int SERVO_HALF_MOVE_TIME_MILLISECONDS = 850;
        private const int SERVO_QUARTER_MOVE_TIME_MILLISECONDS = SERVO_HALF_MOVE_TIME_MILLISECONDS / 2;

        //Dependeny objects
        private MotorController _motorController;
        private ServoController _servoController;
        private DistanceSensorUltrasonic _distanceSensorUltrasonic;
        private DistanceSensorLaserAnalog _distanceSensorLaserAnalogTop;
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
        private volatile bool _isCliffSensorOn = true;
        private DateTime? _drivingForward;

        public AutomaticDrive(MotorController motorController,
                              ServoController servoController,
                              DistanceSensorUltrasonic distanceSensorUltrasonic,
                              DistanceSensorLaserAnalog distanceSensorLaserAnalogTop,
                              DistanceSensorLaser distanceSensorLaserMiddleTop,
                              DistanceSensorLaser distanceSensorLaserMiddleBottom,
                              DistanceSensorLaser distanceSensorLaserBottom)
        {
            _motorController = motorController;
            _servoController = servoController;
            _distanceSensorUltrasonic = distanceSensorUltrasonic;
            _distanceSensorLaserAnalogTop = distanceSensorLaserAnalogTop;
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

                _isStopping = false;

                if (speakOff)
                    await AudioPlayerController.PlayAsync(AudioName.AutomaticDriveOff);

                _threadWaiter.Set();
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

        public bool GetCliffSensorState()
        {
            return _isCliffSensorOn;
        }

        public async Task SetCliffSensorState(bool toggle, bool on = false)
        {
            await CliffSensorStateSynchronous.Call(async () =>
            {
                if (toggle)
                {
                    on = _isCliffSensorOn == false;
                }

                if (_isCliffSensorOn == false && on)
                {
                    _isCliffSensorOn = on;
                    await AudioPlayerController.PlayAsync(AudioName.CliffSensorOn);
                }
                else if (_isCliffSensorOn && on == false)
                {
                    _isCliffSensorOn = on;
                    await AudioPlayerController.PlayAsync(AudioName.CliffSensorOff);
                }
                else if (_isCliffSensorOn && on)
                {
                    await AudioPlayerController.PlayAsync(AudioName.CliffSensorAlreadyOn);
                }
                else if (_isCliffSensorOn == false && on == false)
                {
                    await AudioPlayerController.PlayAsync(AudioName.CliffSensorAlreadyOff);
                }
            });
        }

        private void StartInternalAsync()
        {
            var thread = new Thread(() =>
            {
                try
                {
                    _distanceSensorLaserAnalogTop.ClearDistancesFiltered();
                    _distanceSensorLaserMiddleTop.ClearDistancesFiltered();
                    _distanceSensorLaserMiddleBottom.ClearDistancesFiltered();
                    _distanceSensorLaserBottom.ClearDistancesFiltered();
                    _distanceSensorUltrasonic.ClearDistancesFiltered();

                    _cancellationTokenSource = new CancellationTokenSource();

                    StartInternal(_cancellationTokenSource.Token).Wait();

                    _threadWaiter.WaitOne();
                }
                catch (Exception exception)
                {
                    Logger.WriteAsync(nameof(AutomaticDrive), exception).Wait();
                    SystemController.ShutdownApplicationAsync(true).Wait();
                }
            });
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
        }

        private async Task StartInternal(CancellationToken cancellationToken)
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
                var waitServoPositionTask = Task.Delay(SERVO_HALF_MOVE_TIME_MILLISECONDS, cancellationToken);

                await Task.WhenAll(startAutomaticDriveSpeechTask, waitServoPositionTask);

                _isForward = true;

                while (_isStopping == false)
                {
                    await Task.Delay(25, cancellationToken);

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

                            await TurnLeftAsync(700, cancellationToken, true);
                        }
                        else if (freeDirection == FreeDirection.LeftMiddle)
                        {
                            if (_isStopping)
                                break;

                            await TurnLeftAsync(350, cancellationToken, false);
                        }
                        else if (freeDirection == FreeDirection.Right)
                        {
                            if (_isStopping)
                                break;

                            await TurnRightAsync(700, cancellationToken, true);
                        }
                        else if (freeDirection == FreeDirection.RightMiddle)
                        {
                            if (_isStopping)
                                break;

                            await TurnRightAsync(350, cancellationToken, false);
                        }
                        else if (freeDirection == FreeDirection.Falling)
                        {
                            if (_isStopping)
                                break;

                            await TurnBackwardAsync(600, cancellationToken, false);
                            await TurnFullAsync(cancellationToken);
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

        private async Task TurnRightAsync(int milliseconds, CancellationToken cancellationToken, bool checkHang)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = 1,
                RightCircle = true
            };

            await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);

            await Task.Delay(milliseconds, cancellationToken);

            if (checkHang && await CheckHangAsync(cancellationToken))
                return;

            carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);
        }

        private async Task TurnBackwardAsync(int milliseconds, CancellationToken cancellationToken, bool checkHang)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = SPEED,
                ForwardBackward = true
            };

            await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);

            await Task.Delay(milliseconds, cancellationToken);

            if (checkHang && await CheckHangAsync(cancellationToken))
                return;

            carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);
        }

        private async Task TurnLeftAsync(int milliseconds, CancellationToken cancellationToken, bool checkHang)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = 1,
                LeftCircle = true
            };

            await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);

            await Task.Delay(milliseconds, cancellationToken);

            if (checkHang && await CheckHangAsync(cancellationToken))
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

            await Task.Delay(1250, cancellationToken);

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

                await TurnBackwardAsync(600, cancellationToken, false);
                await TurnLeftAsync(700, cancellationToken, false);

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
                await Task.Delay(SERVO_HALF_MOVE_TIME_MILLISECONDS, cancellationToken);
            }

            var dsTopMaxDistanceNotReached = false;
            var forwardFree = false;

            await CliffSensorStateSynchronous.Call(async () =>
            {
                //Do not cancel operation, because after it sensor is in buggy state and does not work until power cycle
                var distanceSensorReadings = await ReadDistances();

                dsTopMaxDistanceNotReached = _isCliffSensorOn == false || (distanceSensorReadings.LaserDistanceTop < DS_LASER_ANALOG_TOP_MAX_RANGE_CENTIMETERS);

                if (distanceSensorReadings.UltrasonicDistance > DS_ULTRASONIC_MIN_RANGE_CENTIMETERS
                    && (_isCliffSensorOn == false || (distanceSensorReadings.LaserDistanceTop > DS_LASER_ANALOG_TOP_MIN_RANGE_CENTIMETERS))
                    && dsTopMaxDistanceNotReached
                    && distanceSensorReadings.LaserDistanceMiddleTop > DS_LASER_MIDDLE_TOP_MIN_RANGE_MILLIMETERS
                    && distanceSensorReadings.LaserDistanceMiddleBottom > DS_LASER_MIDDLE_BOTTOM_MIN_RANGE_MILLIMETERS
                    && distanceSensorReadings.LaserDistanceBottom > DS_LASER_BOTTOM_MIN_RANGE_MILLIMETERS)
                {
                    _isForward = true;
                    forwardFree = true;
                }
            });

            if (forwardFree)
                return FreeDirection.Forward;

            _distanceSensorLaserAnalogTop.ClearDistancesFiltered();
            _distanceSensorLaserMiddleTop.ClearDistancesFiltered();
            _distanceSensorLaserMiddleBottom.ClearDistancesFiltered();
            _distanceSensorLaserBottom.ClearDistancesFiltered();
            _distanceSensorUltrasonic.ClearDistancesFiltered();

            if (dsTopMaxDistanceNotReached == false)
            {
                return FreeDirection.Falling;
            }

            _isForward = false;

            var carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            await _motorController.MoveCarAsync(carMoveCommand, MotorCommandSource.AutomaticDrive);

            //Left
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalLeft);
            await Task.Delay(SERVO_HALF_MOVE_TIME_MILLISECONDS, cancellationToken);
            var dsUltrasonicDistance = await _distanceSensorUltrasonic.GetDistance();

            if (dsUltrasonicDistance > DS_ULTRASONIC_MIN_RANGE_CENTIMETERS)
            {
                return FreeDirection.Left;
            }

            //LeftMiddle
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalLeftMiddle);
            await Task.Delay(SERVO_QUARTER_MOVE_TIME_MILLISECONDS, cancellationToken);
            dsUltrasonicDistance = await _distanceSensorUltrasonic.GetDistance();

            if (dsUltrasonicDistance > DS_ULTRASONIC_MIN_RANGE_CENTIMETERS)
            {
                return FreeDirection.LeftMiddle;
            }

            //RightMiddle
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalRightMiddle);
            await Task.Delay(SERVO_HALF_MOVE_TIME_MILLISECONDS, cancellationToken);
            dsUltrasonicDistance = await _distanceSensorUltrasonic.GetDistance();

            if (dsUltrasonicDistance > DS_ULTRASONIC_MIN_RANGE_CENTIMETERS)
            {
                return FreeDirection.RightMiddle;
            }

            //Right
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalRight);
            await Task.Delay(SERVO_QUARTER_MOVE_TIME_MILLISECONDS, cancellationToken);
            dsUltrasonicDistance = await _distanceSensorUltrasonic.GetDistance();

            if (dsUltrasonicDistance > DS_ULTRASONIC_MIN_RANGE_CENTIMETERS)
            {
                return FreeDirection.Right;
            }

            return FreeDirection.None;
        }

        private async Task<DistanceSensorReadings> ReadDistances()
        {
            var distanceSensorReadings = new DistanceSensorReadings();

            var distanceSensorUltrasonicTask = _distanceSensorUltrasonic.GetDistanceFiltered();
            var distanceSensorLaserMiddleTopTask = Task.Factory.StartNew(() => distanceSensorReadings.LaserDistanceMiddleTop = _distanceSensorLaserMiddleTop.GetDistanceFiltered());
            var distanceSensorLaserMiddleBottomTask = Task.Factory.StartNew(() => distanceSensorReadings.LaserDistanceMiddleBottom = _distanceSensorLaserMiddleBottom.GetDistanceFiltered());
            var distanceSensorLaserBottomTask = Task.Factory.StartNew(() => distanceSensorReadings.LaserDistanceBottom = _distanceSensorLaserBottom.GetDistanceFiltered());

            if (_isCliffSensorOn)
            {
                var distanceSensorAnalogTopTask = _distanceSensorLaserAnalogTop.GetDistanceFiltered();

                //Do not cancel operation, because after it sensor is in buggy state and does not work until power cycle
                await Task.WhenAll(distanceSensorUltrasonicTask, distanceSensorAnalogTopTask, distanceSensorLaserMiddleTopTask, distanceSensorLaserMiddleBottomTask, distanceSensorLaserBottomTask);
                distanceSensorReadings.UltrasonicDistance = distanceSensorUltrasonicTask.Result;
                distanceSensorReadings.LaserDistanceTop = distanceSensorAnalogTopTask.Result;
            }
            else
            {
                //Do not cancel operation, because after it sensor is in buggy state and does not work until power cycle
                await Task.WhenAll(distanceSensorUltrasonicTask, distanceSensorLaserMiddleTopTask, distanceSensorLaserMiddleBottomTask, distanceSensorLaserBottomTask);
                distanceSensorReadings.UltrasonicDistance = distanceSensorUltrasonicTask.Result;
            }

            return distanceSensorReadings;
        }
    }

    public class DistanceSensorReadings
    {
        public double LaserDistanceTop { get; set; }
        public int LaserDistanceMiddleTop { get; set; }
        public int LaserDistanceMiddleBottom { get; set; }
        public int LaserDistanceBottom { get; set; }
        public int UltrasonicDistance { get; set; }
    }
}