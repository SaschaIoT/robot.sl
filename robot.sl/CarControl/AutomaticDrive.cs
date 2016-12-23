using robot.sl.Audio.AudioPlaying;
using robot.sl.Sensors;
using System;
using System.Threading;
using System.Threading.Tasks;
using robot.sl.Helper;

namespace robot.sl.CarControl
{
    public class AutomaticDrive
    {
        private const int MIN_DETECTION_CM = 40;

        //Dependeny objects
        private MotorController _motorController;
        private ServoController _servoController;
        private DistanceMeasurementSensor _distanceMeasurementSensor = null;
        
        private const int MIDDLE_VERTICAL = 580;
        private const int FRONT_MIDDLE_HORIZONTAL = 372;
        private const int FRONT_LEFT_HORIZONTAL = 155;
        private const int FRONT_LEFT_MIDDLE_HORIZONTAL = 244;
        private const int FRONT_RIGHT_HORIZONTAL = 595;
        private const int FRONT_RIGHT_MIDDLE_HORIZONTAL = 485;

        public bool IsRunning
        {
            get
            {
                return !_isStopped;
            }
        }

        private const double SPEED = 0.6;

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

            _isStopped = false;

            Driving = null;

            _cancellationTokenSource = new CancellationTokenSource();

            StartInternal(_cancellationTokenSource.Token);

            await AudioPlayerController.PlayAndWaitAsync(AudioName.StartAutomaticDrive);
        }

        public async Task Stop()
        {
            if (_isStopped)
            {
                return;
            }

            Driving = null;

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }

            _isStopping = true;

            while (!_isStopped)
            {
                await Task.Delay(10);
            }

            _isStopping = false;

            await AudioPlayerController.PlayAndWaitAsync(AudioName.StopAutomaticDrive);
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
                cancellationToken.ThrowIfCancellationRequested();

                _servoController.PwmController.SetPwm(2, 0, FRONT_MIDDLE_HORIZONTAL);
                _servoController.PwmController.SetPwm(3, 0, MIDDLE_VERTICAL);

                await Task.Delay(TimeSpan.FromMilliseconds(1500), cancellationToken);

                _isForward = true;

                while (!_isStopping)
                {
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

                            await TurnLeft(700, cancellationToken, true);
                        }
                        else if (freeDirection == FreeDirection.LeftMiddle)
                        {
                            if (_isStopping)
                                break;

                            await TurnLeft(350, cancellationToken, false);
                        }
                        else if (freeDirection == FreeDirection.Right)
                        {
                            if (_isStopping)
                                break;

                            await TurnRight(700, cancellationToken, true);
                        }
                        else if (freeDirection == FreeDirection.RightMiddle)
                        {
                            if (_isStopping)
                                break;

                            await TurnRight(350, cancellationToken, false);
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

            _servoController.PwmController.SetPwm(2, 0, 155);
            _servoController.PwmController.SetPwm(3, 0, 182);

            _isStopped = true;
        }

        private async Task TurnRight(int milliseconds, CancellationToken cancellationToken, bool checkHang)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = 1,
                RightCircle = true
            };

            _motorController.MoveCar(null, carMoveCommand);

            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);

            if (checkHang)
            {
                await CheckHang(cancellationToken);
            }

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

        private async Task TurnLeft(int milliseconds, CancellationToken cancellationToken, bool checkHang)
        {
            var carMoveCommand = new CarMoveCommand
            {
                Speed = 1,
                LeftCircle = true
            };

            _motorController.MoveCar(null, carMoveCommand);

            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);

            if (checkHang)
            {
                await CheckHang(cancellationToken);
            }

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

                await AudioPlayerController.PlayAndWaitAsync(AudioName.AutomatischesFahrenFesthaengen).WithCancellation(cancellationToken);

                await TurnBackward(1000, cancellationToken);
                await TurnLeft(700, cancellationToken, false);

                carMoveCommand = new CarMoveCommand
                {
                    Speed = 0
                };

                _motorController.MoveCar(null, carMoveCommand);
            }
        }

        private async Task<FreeDirection> GetFreeDirection(CancellationToken cancellationToken)
        {
            if (!_isForward)
            {
                _servoController.PwmController.SetPwm(2, 0, FRONT_MIDDLE_HORIZONTAL);
                await Task.Delay(1000, cancellationToken);
            }

            var currentDistance = await _distanceMeasurementSensor.ReadDistanceInCm(3);

            if (currentDistance > MIN_DETECTION_CM)
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

            //Left
            _servoController.PwmController.SetPwm(2, 0, FRONT_LEFT_HORIZONTAL);
            await Task.Delay(500, cancellationToken);
            currentDistance = await _distanceMeasurementSensor.ReadDistanceInCm(3);

            if (currentDistance > MIN_DETECTION_CM)
            {
                return FreeDirection.Left;
            }

            //LeftMiddle
            _servoController.PwmController.SetPwm(2, 0, FRONT_LEFT_MIDDLE_HORIZONTAL);
            await Task.Delay(250, cancellationToken);
            currentDistance = await _distanceMeasurementSensor.ReadDistanceInCm(3);

            if (currentDistance > MIN_DETECTION_CM)
            {
                return FreeDirection.LeftMiddle;
            }

            //RightMiddle
            _servoController.PwmController.SetPwm(2, 0, FRONT_RIGHT_MIDDLE_HORIZONTAL);
            await Task.Delay(250, cancellationToken);
            currentDistance = await _distanceMeasurementSensor.ReadDistanceInCm(3);

            if (currentDistance > MIN_DETECTION_CM)
            {
                return FreeDirection.RightMiddle;
            }

            //Right
            _servoController.PwmController.SetPwm(2, 0, FRONT_RIGHT_HORIZONTAL);
            await Task.Delay(250, cancellationToken);
            currentDistance = await _distanceMeasurementSensor.ReadDistanceInCm(3);

            if (currentDistance > MIN_DETECTION_CM)
            {
                return FreeDirection.Right;
            }

            return FreeDirection.None;
        }
    }
}
