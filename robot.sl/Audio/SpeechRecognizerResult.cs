using robot.sl.Audio.AudioPlaying;
using robot.sl.CarControl;
using robot.sl.Helper;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.SpeechRecognition;

namespace robot.sl.Audio
{
    public partial class SpeechRecognition
    {
        private bool _recognationForwardBackward = true;
        private bool _recognationIsDriving = false;
        private double _recognationSpeed = 0.6;
        private bool _recognationShouldDancing = false;
        private volatile bool _dancingStopped = true;
        private CancellationTokenSource _recognationDanceCancellationTokenSource;
        private DateTime? _shouldRestart = null;
        private DateTime? _shouldShutdown = null;
        private ushort _servoSpeechMoveSpeed = 30;
        private ushort _servoSpeechMoveLightSpeed = 10;

        private async void RecognationResult(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            await StopDancing();

            var recognation = args.Result.Text;
            switch (recognation)
            {
                case "Kamera hoch":
                    await AudioPlayerController.Play(AudioName.CameraUp);
                    _servoController.MoveServo(new CarControlCommand
                    {
                        DirectionControlUpDownStepSpeed = _servoSpeechMoveSpeed,
                        DirectionControlUp = true
                    });
                    break;
                case "Kamera leicht hoch":
                    await AudioPlayerController.Play(AudioName.CameraLightUp);
                    _servoController.MoveServo(new CarControlCommand
                    {
                        DirectionControlUpDownStepSpeed = _servoSpeechMoveLightSpeed,
                        DirectionControlUp = true
                    });
                    break;
                case "Kamera runter":
                    await AudioPlayerController.Play(AudioName.CameraDown);
                    _servoController.MoveServo(new CarControlCommand
                    {
                        DirectionControlUpDownStepSpeed = _servoSpeechMoveSpeed,
                        DirectionControlDown = true
                    });
                    break;
                case "Kamera leicht runter":
                    await AudioPlayerController.Play(AudioName.CameraLightDown);
                    _servoController.MoveServo(new CarControlCommand
                    {
                        DirectionControlUpDownStepSpeed = _servoSpeechMoveLightSpeed,
                        DirectionControlDown = true
                    });
                    break;
                case "Vor":
                    await AudioPlayerController.Play(AudioName.Vor);
                    _motorController.MoveCar(null, new CarMoveCommand
                    {
                        ForwardBackward = true,
                        Speed = _recognationSpeed
                    });
                    _recognationForwardBackward = true;
                    _recognationIsDriving = true;
                    break;
                case "Zurück":
                    await AudioPlayerController.Play(AudioName.Zurueck);
                    _motorController.MoveCar(null, new CarMoveCommand
                    {
                        ForwardBackward = false,
                        Speed = _recognationSpeed
                    });
                    _recognationForwardBackward = false;
                    _recognationIsDriving = true;
                    break;
                case "Links":
                    await AudioPlayerController.Play(AudioName.Links);
                    _motorController.MoveCar(null, new CarMoveCommand
                    {
                        LeftCircle = true,
                        ForwardBackward = _recognationForwardBackward,
                        Speed = 1
                    });
                    await Task.Delay(TimeSpan.FromMilliseconds(700));
                    RecognationForwardBackwardStop();
                    break;
                case "Rechts":
                    await AudioPlayerController.Play(AudioName.Rechts);
                    _motorController.MoveCar(null, new CarMoveCommand
                    {
                        RightCircle = true,
                        ForwardBackward = _recognationForwardBackward,
                        Speed = 1
                    });
                    await Task.Delay(TimeSpan.FromMilliseconds(700));
                    RecognationForwardBackwardStop();
                    break;
                case "Stop":
                    await AudioPlayerController.Play(AudioName.Stop);
                    _motorController.MoveCar(null, new CarMoveCommand
                    {
                        Speed = 0
                    });
                    _recognationIsDriving = false;
                    _recognationForwardBackward = true;
                    break;
                case "Wenden":
                    await AudioPlayerController.Play(AudioName.Wenden);
                    _motorController.MoveCar(null, new CarMoveCommand
                    {
                        RightCircle = true,
                        Speed = 1
                    });
                    await Task.Delay(TimeSpan.FromMilliseconds(1250));
                    RecognationForwardBackwardStop();
                    break;
                case "Leicht Links":
                    await AudioPlayerController.Play(AudioName.LeichtLinks);
                    _motorController.MoveCar(null, new CarMoveCommand
                    {
                        LeftCircle = true,
                        ForwardBackward = _recognationForwardBackward,
                        Speed = 1
                    });
                    await Task.Delay(TimeSpan.FromMilliseconds(350));
                    RecognationForwardBackwardStop();
                    break;
                case "Leicht Rechts":
                    await AudioPlayerController.Play(AudioName.LeichtRechts);
                    _motorController.MoveCar(null, new CarMoveCommand
                    {
                        RightCircle = true,
                        ForwardBackward = _recognationForwardBackward,
                        Speed = 1
                    });
                    await Task.Delay(TimeSpan.FromMilliseconds(350));
                    RecognationForwardBackwardStop();
                    break;
                case "Langsam":
                    await AudioPlayerController.Play(AudioName.Langsam);
                    _recognationSpeed = 0.3;
                    RecognationForwardBackwardStop();
                    break;
                case "Normal":
                    await AudioPlayerController.Play(AudioName.Normal);
                    _recognationSpeed = 0.6;
                    RecognationForwardBackwardStop();
                    break;
                case "Schnell":
                    await AudioPlayerController.Play(AudioName.Schnell);
                    _recognationSpeed = 1;
                    RecognationForwardBackwardStop();
                    break;
                case "Tanzen":
                    await AudioPlayerController.Play(AudioName.Tanzen);
                    _recognationForwardBackward = true;
                    _recognationIsDriving = false;
                    _recognationShouldDancing = true;
                    RecognationDance();
                    break;
                case "Aktiviere automatisches Fahren":
                    _automaticDrive.Start();
                    break;
                case "Deaktiviere automatisches Fahren":
                    await _automaticDrive.Stop();
                    break;
                case "Befehle":
                    await AudioPlayerController.Play(AudioName.Befehl);
                    break;
                case "Steuerungsbefehle":
                    await AudioPlayerController.Play(AudioName.Steuerungsbefehle);
                    break;
                case "Systembefehle":
                    await AudioPlayerController.Play(AudioName.Systembefehle);
                    break;
                case "Starte Dich neu":
                case "Neustarten":
                    await AudioPlayerController.PlayAndWaitAsync(AudioName.ReallyRestart);
                    _shouldRestart = DateTime.Now;
                    _shouldShutdown = null;
                    break;
                case "Schalte Dich aus":
                case "Auschalten":
                    await AudioPlayerController.PlayAndWaitAsync(AudioName.ReallyShutdown);
                    _shouldShutdown = DateTime.Now;
                    _shouldRestart = null;
                    break;
                case "Nein":
                    _shouldRestart = null;
                    _shouldShutdown = null;
                    break;
                case "Ja":
                    if (_shouldShutdown.HasValue && DateTime.Now <= _shouldShutdown.Value.AddSeconds(15))
                    {
                        await SystemController.Shutdown();
                    }
                    else if (_shouldRestart.HasValue && DateTime.Now <= _shouldRestart.Value.AddSeconds(15))
                    {
                        await SystemController.Restart();
                    }
                    _shouldRestart = null;
                    _shouldShutdown = null;
                    break;
                case "Aktiviere Lautsprecher":
                    await AudioPlayerController.SetAllSpeakerOnOff(true);
                    break;
                case "Deaktiviere Lautsprecher":
                    await AudioPlayerController.SetAllSpeakerOnOff(false);
                    break;
                case "Aktiviere Headset Lautsprecher":
                    await AudioPlayerController.SetHeadsetSpeakerOnOff(true);
                    break;
                case "Deaktiviere Headset Lautsprecher":
                    await AudioPlayerController.SetHeadsetSpeakerOnOff(false);
                    break;
                case "Aktiviere Fahrzeug Lautsprecher":
                    await AudioPlayerController.SetCarSpeakerOnOff(true);
                    break;
                case "Deaktiviere Fahrzeug Lautsprecher":
                    await AudioPlayerController.SetCarSpeakerOnOff(false);
                    break;
                case "Aktiviere Sound Modus":
                    await AudioPlayerController.SetSoundModeOnOff(true);
                    break;
                case "Deaktiviere Sound Modus":
                    await AudioPlayerController.SetSoundModeOnOff(false);
                    break;
                case "Lautsprecher und Sound Modus":
                    await AudioPlayerController.PlaySpeakerOnOffSoundMode();
                    break;
            }

            if (recognation != "Starte Dich neu"
               && recognation != "Neustarten"
               && recognation != "Schalte Dich aus"
               && recognation != "Auschalten")
            {
                _shouldRestart = null;
                _shouldShutdown = null;
            }

            if (_isStopped)
            {
                StopInternal();
            }
        }

        private async Task StopDancing()
        {
            if (_recognationShouldDancing
                && _recognationDanceCancellationTokenSource != null)
            {
                _recognationDanceCancellationTokenSource.Cancel();
            }

            _recognationShouldDancing = false;

            while (!_dancingStopped)
            {
                await Task.Delay(10);
            }
        }

        private async void RecognationDance()
        {
            _dancingStopped = false;

            _recognationDanceCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _recognationDanceCancellationTokenSource.Token;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                while (_recognationShouldDancing)
                {
                    _motorController.MoveCar(null, new CarMoveCommand
                    {
                        ForwardBackward = true,
                        Speed = 1
                    });

                    await Task.Delay(300, cancellationToken);

                    if (!_recognationShouldDancing)
                        break;

                    _motorController.MoveCar(null, new CarMoveCommand
                    {
                        RightCircle = true,
                        ForwardBackward = true,
                        Speed = 1
                    });

                    await Task.Delay(300, cancellationToken);

                    if (!_recognationShouldDancing)
                        break;

                    _motorController.MoveCar(null, new CarMoveCommand
                    {
                        LeftCircle = true,
                        ForwardBackward = true,
                        Speed = 1
                    });

                    await Task.Delay(300, cancellationToken);

                    if (!_recognationShouldDancing)
                        break;

                    _motorController.MoveCar(null, new CarMoveCommand
                    {
                        ForwardBackward = false,
                        Speed = 1
                    });

                    await Task.Delay(300, cancellationToken);

                    if (!_recognationShouldDancing)
                        break;

                    _motorController.MoveCar(null, new CarMoveCommand
                    {
                        RightCircle = true,
                        ForwardBackward = false,
                        Speed = 1
                    });

                    await Task.Delay(300, cancellationToken);

                    if (!_recognationShouldDancing)
                        break;

                    _motorController.MoveCar(null, new CarMoveCommand
                    {
                        LeftCircle = true,
                        ForwardBackward = false,
                        Speed = 1
                    });

                    await Task.Delay(300, cancellationToken);

                    if (!_recognationShouldDancing)
                        break;

                    _motorController.MoveCar(null, new CarMoveCommand
                    {
                        ForwardBackward = true,
                        RightLeft = -0.5,
                        Speed = 1
                    });

                    await Task.Delay(500, cancellationToken);

                    if (!_recognationShouldDancing)
                        break;

                    _motorController.MoveCar(null, new CarMoveCommand
                    {
                        ForwardBackward = true,
                        LeftCircle = true,
                        Speed = 1
                    });

                    await Task.Delay(1500, cancellationToken);

                    if (!_recognationShouldDancing)
                        break;
                }
            }
            catch (OperationCanceledException) { }

            _dancingStopped = true;
        }

        private void RecognationForwardBackwardStop()
        {
            if (!_recognationIsDriving)
            {
                _motorController.MoveCar(null, new CarMoveCommand
                {
                    Speed = 0
                });
            }
            else
            {
                _motorController.MoveCar(null, new CarMoveCommand
                {
                    ForwardBackward = _recognationForwardBackward,
                    Speed = _recognationSpeed
                });
            }
        }
    }
}
