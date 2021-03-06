﻿using robot.sl.Audio.AudioPlaying;
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
                    await AudioPlayerController.PlayAsync(AudioName.DanceOnAlready);

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

        public async Task StopAsync(bool speakOff, bool speakOffAlready)
        {
            await DanceSynchronous.Call(async () =>
            {
                if (_isStopped)
                {
                    if(speakOffAlready)
                        await AudioPlayerController.PlayAsync(AudioName.DanceOffAlready);

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
                    await AudioPlayerController.PlayAsync(AudioName.DanceOff);
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

                await AudioPlayerController.PlayAndWaitAsync(AudioName.DanceOn, cancellationToken);

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

            await _motorController.MoveCarAsync(carMoveCommandEnd, MotorCommandSource.Dance);

            _isStopped = true;
        }
    }
}