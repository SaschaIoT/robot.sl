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
        private const int MIN_DETECTION_MILLIMETERS = 350;
        private ForwardDirection _forwardDirection = ForwardDirection.LeftMiddle;

        //Dependeny objects
        private MotorController _motorController;
        private ServoController _servoController;
        private DistanceMeasurementSensor _distanceMeasurementSensor = null;

        private ManualResetEvent _threadWaiter = new ManualResetEvent(false);

        public bool IsRunning
        {
            get
            {
                return !_isStopped;
            }
        }

        private const double SPEED = 0.5;

        private bool _isForward = true;

        private CancellationTokenSource _cancellationTokenSource;

        private volatile bool _isStopped = true;
        private volatile bool _isStopping = false;

        private DateTime? _isDriving = null;
        public DateTime? Driving
        {
            get
            {
                return _isDriving;
            }
            set
            {
                if (value == null)
                {
                    _isDriving = null;
                }
                else if (_isDriving == null)
                {
                    _isDriving = value;
                }
            }
        }

        public AutomaticDrive(MotorController motorController,
                              ServoController servoController,
                              DistanceMeasurementSensor distanceMeasurementSensor)
        {
            _motorController = motorController;
            _servoController = servoController;
            _distanceMeasurementSensor = distanceMeasurementSensor;
        }

        public void Start()
        {
            Task.Factory.StartNew(() =>
            {

                StartInternal().Wait();
                _threadWaiter.WaitOne();

            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
             .AsAsyncAction()
             .AsTask()
             .ContinueWith((t) =>
             {
                 Logger.Write(nameof(AutomaticDrive), t.Exception).Wait();
                 SystemController.ShutdownApplication(true).Wait();

             }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task StartInternal()
        {
            if (!_isStopped)
            {
                return;
            }

            _distanceMeasurementSensor.Start();

            _isStopped = false;

            Driving = null;

            _cancellationTokenSource = new CancellationTokenSource();

            StartInternal(_cancellationTokenSource.Token);

            await AudioPlayerController.Play(AudioName.StartAutomaticDrive);
        }

        public async Task Stop()
        {
            await Stop(true);
        }

        public async Task Stop(bool speak)
        {
            if (_isStopped)
            {
                return;
            }

            Driving = null;

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

            await _distanceMeasurementSensor.Stop();

            if (speak)
                await AudioPlayerController.Play(AudioName.StopAutomaticDrive);
        }

        public async Task StartStopToggle()
        {
            if (!_isStopped)
            {
                await Stop();
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
                _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.CameraHorizontalMiddle);
                _servoController.PwmController.SetPwm(Servo.DistanceSensorVertical, 0, ServoPositions.DistanceSensorVerticalMiddle);

                await Task.Delay(TimeSpan.FromMilliseconds(1500), cancellationToken);

                _isForward = true;

                while (!_isStopping)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken);

                    CarMoveCommand carMoveCommand = null;

                    var freeDirection = await GetFreeDirection(cancellationToken);

                    if (freeDirection == FreeDirection.Forward)
                    {
                        if (_isStopping)
                            break;

                        if (Driving.HasValue && DateTime.Now >= Driving.Value.AddMilliseconds(700))
                        {
                            await CheckHang(cancellationToken);
                        }

                        carMoveCommand = new CarMoveCommand
                        {
                            Speed = SPEED
                        };

                        _motorController.MoveCar(null, carMoveCommand);

                        Driving = DateTime.Now;
                    }
                    else
                    {
                        if (freeDirection == FreeDirection.Left)
                        {
                            if (_isStopping)
                                break;

                            await TurnLeft(700, cancellationToken);
                        }
                        else if (freeDirection == FreeDirection.LeftMiddle)
                        {
                            if (_isStopping)
                                break;

                            await TurnLeft(350, cancellationToken);
                        }
                        else if (freeDirection == FreeDirection.Right)
                        {
                            if (_isStopping)
                                break;

                            await TurnRight(700, cancellationToken);
                        }
                        else if (freeDirection == FreeDirection.RightMiddle)
                        {
                            if (_isStopping)
                                break;

                            await TurnRight(350, cancellationToken);
                        }
                        else if (freeDirection == FreeDirection.None)
                        {
                            if (_isStopping)
                                break;

                            await TurnFull(cancellationToken);
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

            Driving = null;

            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalLeft);
            _servoController.PwmController.SetPwm(Servo.DistanceSensorVertical, 0, ServoPositions.DistanceSensorVerticalTop);

            _isStopped = true;
        }

        private async Task TurnRight(int milliseconds, CancellationToken cancellationToken)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = 1,
                RightCircle = true
            };

            _motorController.MoveCar(null, carMoveCommand);

            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);

            carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            _motorController.MoveCar(null, carMoveCommand);

            Driving = null;
        }

        private async Task TurnBackward(int milliseconds, CancellationToken cancellationToken)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = SPEED,
                ForwardBackward = true
            };

            _motorController.MoveCar(null, carMoveCommand);

            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);

            await CheckHang(cancellationToken);

            carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            _motorController.MoveCar(null, carMoveCommand);

            Driving = null;
        }

        private async Task TurnLeft(int milliseconds, CancellationToken cancellationToken)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = 1,
                LeftCircle = true
            };

            _motorController.MoveCar(null, carMoveCommand);

            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);

            carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            _motorController.MoveCar(null, carMoveCommand);

            Driving = null;
        }

        private async Task TurnFull(CancellationToken cancellationToken)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = 1,
                LeftCircle = true
            };

            _motorController.MoveCar(null, carMoveCommand);

            await Task.Delay(TimeSpan.FromMilliseconds(1250), cancellationToken);

            await CheckHang(cancellationToken);

            carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            _motorController.MoveCar(null, carMoveCommand);

            Driving = null;
        }

        private async Task CheckHang(CancellationToken cancellationToken)
        {
            if (!SpeedSensor.IsDriving)
            {
                var carMoveCommand = new CarMoveCommand
                {
                    Speed = 0
                };

                _motorController.MoveCar(null, carMoveCommand);

                Driving = null;

                await AudioPlayerController.PlayAndWaitAsync(AudioName.AutomatischesFahrenFesthaengen, cancellationToken);

                await TurnBackward(1000, cancellationToken);
                await TurnLeft(700, cancellationToken);

                carMoveCommand = new CarMoveCommand
                {
                    Speed = 0
                };

                _motorController.MoveCar(null, carMoveCommand);
            }
        }

        private async Task<FreeDirection> GetFreeDirection(CancellationToken cancellationToken)
        {
            if (_isForward == false)
            {
                _forwardDirection = ForwardDirection.ForwardThenRightMiddle;

                _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalMiddle);
                await Task.Delay(1000, cancellationToken);
            }
            else
            {
                if (_forwardDirection == ForwardDirection.LeftMiddle)
                {
                    _forwardDirection = ForwardDirection.ForwardThenRightMiddle;

                    _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalLeftMiddle);
                    await Task.Delay(250, cancellationToken);
                }
                else if (_forwardDirection == ForwardDirection.ForwardThenLeftMiddle)
                {
                    _forwardDirection = ForwardDirection.LeftMiddle;

                    _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalMiddle);
                    await Task.Delay(250, cancellationToken);
                }
                else if (_forwardDirection == ForwardDirection.ForwardThenRightMiddle)
                {
                    _forwardDirection = ForwardDirection.RightMiddle;

                    _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalMiddle);
                    await Task.Delay(250, cancellationToken);
                }
                else if (_forwardDirection == ForwardDirection.RightMiddle)
                {
                    _forwardDirection = ForwardDirection.ForwardThenLeftMiddle;

                    _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalRightMiddle);
                    await Task.Delay(250, cancellationToken);
                }
            }

            var currentDistance = await _distanceMeasurementSensor.GetDistanceInMillimeters();

            if (currentDistance > MIN_DETECTION_MILLIMETERS)
            {
                _isForward = true;
                return FreeDirection.Forward;
            }

            _isForward = false;

            var carMoveCommand = new CarMoveCommand
            {
                Speed = 0
            };

            _motorController.MoveCar(null, carMoveCommand);

            Driving = null;

            var timeLeft = 500;
            if (_forwardDirection == ForwardDirection.LeftMiddle)
                timeLeft = 250;
            else if (_forwardDirection == ForwardDirection.RightMiddle)
                timeLeft = 750;

            //Left
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalLeft);
            await Task.Delay(timeLeft, cancellationToken);
            currentDistance = await _distanceMeasurementSensor.GetDistanceInMillimeters();

            if (currentDistance > MIN_DETECTION_MILLIMETERS)
            {
                return FreeDirection.Left;
            }

            //LeftMiddle
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalLeftMiddle);
            await Task.Delay(250, cancellationToken);
            currentDistance = await _distanceMeasurementSensor.GetDistanceInMillimeters();

            if (currentDistance > MIN_DETECTION_MILLIMETERS)
            {
                return FreeDirection.LeftMiddle;
            }

            //RightMiddle
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalRightMiddle);
            await Task.Delay(250, cancellationToken);
            currentDistance = await _distanceMeasurementSensor.GetDistanceInMillimeters();

            if (currentDistance > MIN_DETECTION_MILLIMETERS)
            {
                return FreeDirection.RightMiddle;
            }

            //Right
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalRight);
            await Task.Delay(250, cancellationToken);
            currentDistance = await _distanceMeasurementSensor.GetDistanceInMillimeters();

            if (currentDistance > MIN_DETECTION_MILLIMETERS)
            {
                return FreeDirection.Right;
            }

            return FreeDirection.None;
        }
    }

    public enum ForwardDirection
    {
        ForwardThenLeftMiddle = 0,
        ForwardThenRightMiddle = 1,
        LeftMiddle = 2,
        RightMiddle = 3
    }
}