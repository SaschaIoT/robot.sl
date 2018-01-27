using robot.sl.Audio.AudioPlaying;
using robot.sl.CarControl;
using robot.sl.Helper;
using System;
using System.Threading.Tasks;
using Windows.Media.SpeechRecognition;

namespace robot.sl.Audio
{
    public partial class SpeechRecognition
    {
        private bool _recognationForwardBackward = true;
        private bool _recognationIsDriving = false;
        private double _recognationSpeed = 0.6;
        private DateTime? _shouldRestart = null;
        private DateTime? _shouldShutdown = null;
        private ushort _servoSpeechMoveSpeed = 30;
        private ushort _servoSpeechMoveLightSpeed = 10;

        private async void RecognationResult(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            try
            {
                var recognation = args.Result.Text;
                switch (recognation)
                {
                    case "Kamera hoch":
                        await AudioPlayerController.PlayAsync(AudioName.CameraUp);
                        await _servoController.MoveServo(new CarControlCommand
                        {
                            DirectionControlUpDownStepSpeed = _servoSpeechMoveSpeed,
                            DirectionControlUp = true
                        });
                        break;
                    case "Kamera leicht hoch":
                        await AudioPlayerController.PlayAsync(AudioName.CameraLightUp);
                        await _servoController.MoveServo(new CarControlCommand
                        {
                            DirectionControlUpDownStepSpeed = _servoSpeechMoveLightSpeed,
                            DirectionControlUp = true
                        });
                        break;
                    case "Kamera runter":
                        await AudioPlayerController.PlayAsync(AudioName.CameraDown);
                        await _servoController.MoveServo(new CarControlCommand
                        {
                            DirectionControlUpDownStepSpeed = _servoSpeechMoveSpeed,
                            DirectionControlDown = true
                        });
                        break;
                    case "Kamera leicht runter":
                        await AudioPlayerController.PlayAsync(AudioName.CameraLightDown);
                        await _servoController.MoveServo(new CarControlCommand
                        {
                            DirectionControlUpDownStepSpeed = _servoSpeechMoveLightSpeed,
                            DirectionControlDown = true
                        });
                        break;
                    case "Vor":
                        await AudioPlayerController.PlayAsync(AudioName.Vor);
                        await _motorController.MoveCarAsync(new CarMoveCommand
                        {
                            ForwardBackward = true,
                            Speed = _recognationSpeed
                        }, MotorCommandSource.SpeechRecognation);
                        _recognationForwardBackward = true;
                        _recognationIsDriving = true;
                        break;
                    case "Zurück":
                        await AudioPlayerController.PlayAsync(AudioName.Zurueck);
                        await _motorController.MoveCarAsync(new CarMoveCommand
                        {
                            ForwardBackward = false,
                            Speed = _recognationSpeed
                        }, MotorCommandSource.SpeechRecognation);
                        _recognationForwardBackward = false;
                        _recognationIsDriving = true;
                        break;
                    case "Links":
                        await AudioPlayerController.PlayAsync(AudioName.Links);
                        await _motorController.MoveCarAsync(new CarMoveCommand
                        {
                            LeftCircle = true,
                            ForwardBackward = _recognationForwardBackward,
                            Speed = 1
                        }, MotorCommandSource.SpeechRecognation);
                        await Task.Delay(TimeSpan.FromMilliseconds(700));
                        await RecognationForwardBackwardStop();
                        break;
                    case "Rechts":
                        await AudioPlayerController.PlayAsync(AudioName.Rechts);
                        await _motorController.MoveCarAsync(new CarMoveCommand
                        {
                            RightCircle = true,
                            ForwardBackward = _recognationForwardBackward,
                            Speed = 1
                        }, MotorCommandSource.SpeechRecognation);
                        await Task.Delay(TimeSpan.FromMilliseconds(700));
                        await RecognationForwardBackwardStop();
                        break;
                    case "Stop":
                        await AudioPlayerController.PlayAsync(AudioName.Stop);
                        await _motorController.MoveCarAsync(new CarMoveCommand
                        {
                            Speed = 0
                        }, MotorCommandSource.SpeechRecognation);
                        _recognationIsDriving = false;
                        _recognationForwardBackward = true;
                        break;
                    case "Wenden":
                        await AudioPlayerController.PlayAsync(AudioName.Wenden);
                        await _motorController.MoveCarAsync(new CarMoveCommand
                        {
                            RightCircle = true,
                            Speed = 1
                        }, MotorCommandSource.SpeechRecognation);
                        await Task.Delay(TimeSpan.FromMilliseconds(1250));
                        await RecognationForwardBackwardStop();
                        break;
                    case "Leicht Links":
                        await AudioPlayerController.PlayAsync(AudioName.LeichtLinks);
                        await _motorController.MoveCarAsync(new CarMoveCommand
                        {
                            LeftCircle = true,
                            ForwardBackward = _recognationForwardBackward,
                            Speed = 1
                        }, MotorCommandSource.SpeechRecognation);
                        await Task.Delay(TimeSpan.FromMilliseconds(350));
                        await RecognationForwardBackwardStop();
                        break;
                    case "Leicht Rechts":
                        await AudioPlayerController.PlayAsync(AudioName.LeichtRechts);
                        await _motorController.MoveCarAsync(new CarMoveCommand
                        {
                            RightCircle = true,
                            ForwardBackward = _recognationForwardBackward,
                            Speed = 1
                        }, MotorCommandSource.SpeechRecognation);
                        await Task.Delay(TimeSpan.FromMilliseconds(350));
                        await RecognationForwardBackwardStop();
                        break;
                    case "Langsam":
                        await AudioPlayerController.PlayAsync(AudioName.Langsam);
                        _recognationSpeed = 0.3;
                        await RecognationForwardBackwardStop();
                        break;
                    case "Normal":
                        await AudioPlayerController.PlayAsync(AudioName.Normal);
                        _recognationSpeed = 0.6;
                        await RecognationForwardBackwardStop();
                        break;
                    case "Schnell":
                        await AudioPlayerController.PlayAsync(AudioName.Schnell);
                        _recognationSpeed = 1;
                        await RecognationForwardBackwardStop();
                        break;
                    case "Aktiviere Tanzen":
                        _recognationForwardBackward = true;
                        _recognationIsDriving = false;
                        await _dance.StartAsync();
                        break;
                    case "Deaktiviere Tanzen":
                        _recognationForwardBackward = true;
                        _recognationIsDriving = false;
                        await _dance.StopAsync();
                        break;
                    case "Aktiviere automatisches Fahren":
                        _recognationForwardBackward = true;
                        _recognationIsDriving = false;
                        await _automaticDrive.StartAsync();
                        break;
                    case "Deaktiviere automatisches Fahren":
                        _recognationForwardBackward = true;
                        _recognationIsDriving = false;
                        await _automaticDrive.StopAsync();
                        break;
                    case "Befehle":
                        await AudioPlayerController.PlayAsync(AudioName.Befehl);
                        break;
                    case "Steuerungsbefehle":
                        await AudioPlayerController.PlayAsync(AudioName.Steuerungsbefehle);
                        break;
                    case "Systembefehle":
                        await AudioPlayerController.PlayAsync(AudioName.Systembefehle);
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
                            await SystemController.ShutdownAsync();
                        }
                        else if (_shouldRestart.HasValue && DateTime.Now <= _shouldRestart.Value.AddSeconds(15))
                        {
                            await SystemController.RestartAsync();
                        }
                        _shouldRestart = null;
                        _shouldShutdown = null;
                        break;
                    case "Aktiviere Lautsprecher":
                        await AudioPlayerController.SetAllSpeakerOnOffAsync(true);
                        break;
                    case "Deaktiviere Lautsprecher":
                        await AudioPlayerController.SetAllSpeakerOnOffAsync(false);
                        break;
                    case "Aktiviere Headset Lautsprecher":
                        await AudioPlayerController.SetHeadsetSpeakerOnOffAsync(true);
                        break;
                    case "Deaktiviere Headset Lautsprecher":
                        await AudioPlayerController.SetHeadsetSpeakerOnOffAsync(false);
                        break;
                    case "Aktiviere Fahrzeug Lautsprecher":
                        await AudioPlayerController.SetCarSpeakerOnOffAsync(true);
                        break;
                    case "Deaktiviere Fahrzeug Lautsprecher":
                        await AudioPlayerController.SetCarSpeakerOnOffAsync(false);
                        break;
                    case "Aktiviere Sound Modus":
                        await AudioPlayerController.SetSoundModeOnOffAsync(true);
                        break;
                    case "Deaktiviere Sound Modus":
                        await AudioPlayerController.SetSoundModeOnOffAsync(false);
                        break;
                    case "Lautsprecher und Sound Modus":
                        await AudioPlayerController.PlaySpeakerOnOffSoundModeAsync();
                        break;
                    case "Ganz leicht links":
                        await AudioPlayerController.PlayAsync(AudioName.GanzLeichtLinks);
                        await _motorController.MoveCarAsync(new CarMoveCommand
                        {
                            LeftCircle = true,
                            ForwardBackward = _recognationForwardBackward,
                            Speed = 1
                        }, MotorCommandSource.SpeechRecognation);
                        await Task.Delay(TimeSpan.FromMilliseconds(175));
                        await RecognationForwardBackwardStop();
                        break;
                    case "Ganz leicht rechts":
                        await AudioPlayerController.PlayAsync(AudioName.GanzLeichtRechts);
                        await _motorController.MoveCarAsync(new CarMoveCommand
                        {
                            RightCircle = true,
                            ForwardBackward = _recognationForwardBackward,
                            Speed = 1
                        }, MotorCommandSource.SpeechRecognation);
                        await Task.Delay(TimeSpan.FromMilliseconds(175));
                        await RecognationForwardBackwardStop();
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
                    await StopInternal();
                }
            }
            catch (Exception exception)
            {
                await Logger.WriteAsync($"{nameof(SpeechRecognition)}, {nameof(RecognationResult)}: ", exception);

                SystemController.ShutdownApplicationAsync(true).Wait();
            }
        }
        
        private async Task RecognationForwardBackwardStop()
        {
            if (_recognationIsDriving == false)
            {
                await _motorController.MoveCarAsync(new CarMoveCommand
                {
                    Speed = 0
                }, MotorCommandSource.SpeechRecognation);
            }
            else
            {
                await _motorController.MoveCarAsync(new CarMoveCommand
                {
                    ForwardBackward = _recognationForwardBackward,
                    Speed = _recognationSpeed
                }, MotorCommandSource.SpeechRecognation);
            }
        }
    }
}
