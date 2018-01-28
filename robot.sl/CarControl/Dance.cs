using robot.sl.Audio.AudioPlaying;
using robot.sl.Helper;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace robot.sl.CarControl
{
    public class Dance
    {
        //Dependeny objects
        private MotorController _motorController;

        public bool IsRunning
        {
            get
            {
                return _isStopped == false;
            }
        }

        private CancellationTokenSource _cancellationTokenSource;

        private volatile bool _isStopped = true;
        private volatile bool _isStopping = false;

        public Dance(MotorController motorController)
        {
            _motorController = motorController;
        }

        public async Task StartAsync()
        {
            await DanceSynchronous.Call(async () =>
            {
                if (_isStopped == false)
                {
                    await AudioPlayerController.PlayAsync(AudioName.TanzenOnAlready);

                    return;
                }

                _isStopped = false;

                _cancellationTokenSource = new CancellationTokenSource();

                StartInternal(_cancellationTokenSource.Token);
            });
        }

        public async Task StopAsync()
        {
            await StopAsync(true, true);
        }

        public async Task StopAsync(bool speakStart, bool speakStartedAlreay)
        {
            await DanceSynchronous.Call(async () =>
            {
                if (_isStopped)
                {
                    if(speakStartedAlreay)
                        await AudioPlayerController.PlayAsync(AudioName.TanzenOffAlready);

                    return;
                }

                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel(true);
                }

                _isStopping = true;

                while (_isStopped == false)
                {
                    await Task.Delay(10);
                }

                _isStopping = false;

                if (speakStart)
                    await AudioPlayerController.PlayAsync(AudioName.TanzenOff);
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
                await _motorController.MoveCarAsync(new CarMoveCommand
                {
                    Speed = 0
                }, MotorCommandSource.Dance);

                await AudioPlayerController.PlayAndWaitAsync(AudioName.TanzenOn, cancellationToken);

                while (_isStopping == false)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await _motorController.MoveCarAsync(new CarMoveCommand
                    {
                        ForwardBackward = true,
                        Speed = 1
                    }, MotorCommandSource.Dance);

                    await Task.Delay(300, cancellationToken);

                    if (_isStopping)
                        break;

                    await _motorController.MoveCarAsync(new CarMoveCommand
                    {
                        RightCircle = true,
                        ForwardBackward = true,
                        Speed = 1
                    }, MotorCommandSource.Dance);

                    await Task.Delay(300, cancellationToken);

                    if (_isStopping)
                        break;

                    await _motorController.MoveCarAsync(new CarMoveCommand
                    {
                        LeftCircle = true,
                        ForwardBackward = true,
                        Speed = 1
                    }, MotorCommandSource.Dance);

                    await Task.Delay(300, cancellationToken);

                    if (_isStopping)
                        break;

                    await _motorController.MoveCarAsync(new CarMoveCommand
                    {
                        ForwardBackward = false,
                        Speed = 1
                    }, MotorCommandSource.Dance);

                    await Task.Delay(300, cancellationToken);

                    if (_isStopping)
                        break;

                    await _motorController.MoveCarAsync(new CarMoveCommand
                    {
                        RightCircle = true,
                        ForwardBackward = false,
                        Speed = 1
                    }, MotorCommandSource.Dance);

                    await Task.Delay(300, cancellationToken);

                    if (_isStopping)
                        break;

                    await _motorController.MoveCarAsync(new CarMoveCommand
                    {
                        LeftCircle = true,
                        ForwardBackward = false,
                        Speed = 1
                    }, MotorCommandSource.Dance);

                    await Task.Delay(300, cancellationToken);

                    if (_isStopping)
                        break;

                    await _motorController.MoveCarAsync(new CarMoveCommand
                    {
                        ForwardBackward = true,
                        RightLeft = -0.5,
                        Speed = 1
                    }, MotorCommandSource.Dance);

                    await Task.Delay(500, cancellationToken);

                    if (_isStopping)
                        break;

                    await _motorController.MoveCarAsync(new CarMoveCommand
                    {
                        ForwardBackward = true,
                        LeftCircle = true,
                        Speed = 1
                    }, MotorCommandSource.Dance);

                    await Task.Delay(1500, cancellationToken);

                    if (_isStopping)
                        break;
                }
            }
            catch (OperationCanceledException) { }

            var carMoveCommandEnd = new CarMoveCommand
            {
                Speed = 0
            };

            await _motorController.MoveCarAsync(carMoveCommandEnd, MotorCommandSource.Other);

            _isStopped = true;
        }
    }
}