using robot.sl.Helper;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace robot.sl.Audio.AudioPlaying
{
    /// <summary>
    /// Speaker 1: Creative Sound Blaster Play! 2
    /// Speaker 2: Logitech G933 Headset
    /// </summary>
    public static partial class AudioPlayerController
    {
        private static AudioPlayer _headsetSpeaker;
        private static AudioPlayer _carSpeaker;

        public static bool CarSpeakerOn { get; set; }
        public static bool HeadsetSpeakerOn { get; set; }
        public static bool SoundModeOn { get; set; }
        private static volatile bool _stopped = false;
        private static volatile bool _neverSpeak = false;
        private static Guid _lastIdentifier = Guid.Empty;

        public static void Stop()
        {
            _stopped = true;
        }

        public static async Task PlayAsync(AudioName AudioPlayerAudioName, double? headsetGain, double? speakerGain)
        {
            await PlayAsync(AudioPlayerAudioName, headsetGain, speakerGain, false, null);
        }
        
        public static async Task PlayAndWaitAsync(AudioName AudioPlayerAudioName, double? headsetGain, double? speakerGain)
        {
            await PlayAsync(AudioPlayerAudioName, headsetGain, speakerGain, true, null);
        }

        public static async Task PlayAsync(AudioName AudioPlayerAudioName)
        {
            await PlayAsync(AudioPlayerAudioName, null, null, false, null);
        }

        public static async Task PlayAndWaitAsync(AudioName AudioPlayerAudioName, CancellationToken cancellationToken)
        {
            await PlayAsync(AudioPlayerAudioName, null, null, true, cancellationToken);
        }

        public static async Task<bool> PlayAndWaitAsync(AudioName AudioPlayerAudioName)
        {
            var currentPlay = await PlayAsync(AudioPlayerAudioName, null, null, true, null);
            return currentPlay;
        }
        
        private static async Task<bool> PlayAsync(AudioName audioName, double? headsetGain, double? speakerGain, bool wait, CancellationToken? cancellationToken)
        {
            var speakerOnOff = GetSpeakerOnOff(audioName);
            if (speakerOnOff.CarSpeakerOn == false && speakerOnOff.HeadsetSpeakerOn == false)
                return false;

            var identifier = Guid.NewGuid();
            _lastIdentifier = identifier;

            if (headsetGain == null)
            {
                headsetGain = 0.8;
            }

            if (speakerGain == null)
            {
                speakerGain = 3.2;
            }

            if (speakerOnOff.CarSpeakerOn && speakerOnOff.HeadsetSpeakerOn)
            {
                var headsetSpeaker = _headsetSpeaker.PlayAsync(EnumHelper.GetName(audioName), headsetGain.Value, cancellationToken);
                var carSpeaker = _carSpeaker.PlayAsync(EnumHelper.GetName(audioName), speakerGain.Value, cancellationToken);

                if (wait)
                {
                    await Task.WhenAll(new Task[] { headsetSpeaker, carSpeaker });
                }
            }
            else if (speakerOnOff.CarSpeakerOn)
            {
                var carSpeaker = _carSpeaker.PlayAsync(EnumHelper.GetName(audioName), speakerGain.Value, cancellationToken);

                if (wait)
                {
                    await carSpeaker;
                }
            }
            else if (speakerOnOff.HeadsetSpeakerOn)
            {
                var headesetSpeaker = _headsetSpeaker.PlayAsync(EnumHelper.GetName(audioName), headsetGain.Value, cancellationToken);

                if (wait)
                {
                    await headesetSpeaker;
                }
            }

            var currentPlay = identifier == _lastIdentifier;
            return currentPlay;
        }

        private static SpeakerOnOff GetSpeakerOnOff(AudioName audioName)
        {
            var speakerOnOff = new SpeakerOnOff
            {
                CarSpeakerOn = CarSpeakerOn,
                HeadsetSpeakerOn = HeadsetSpeakerOn
            };
            
            if(_neverSpeak)
            {
                speakerOnOff.CarSpeakerOn = false;
                speakerOnOff.HeadsetSpeakerOn = false;
            }
            else if (audioName == AudioName.AppError)
            {
                speakerOnOff.CarSpeakerOn = true;
                speakerOnOff.HeadsetSpeakerOn = true;
                _neverSpeak = true;
            }
            else if (audioName == AudioName.Shutdown || audioName == AudioName.Restart)
            {
                speakerOnOff.CarSpeakerOn = true;
                speakerOnOff.HeadsetSpeakerOn = true;
            }
            else
            {
                if (_stopped)
                {
                    speakerOnOff.CarSpeakerOn = false;
                    speakerOnOff.HeadsetSpeakerOn = false;
                }
                else
                {
                    if (audioName == AudioName.GamepadVibrationOn
                        || audioName == AudioName.GamepadVibrationOff
                        || audioName == AudioName.SoundModeAlreadyOn
                        || audioName == AudioName.HeadsetSpeakerOff
                        || audioName == AudioName.HeadsetSpeakerAlreadyOff
                        || audioName == AudioName.HeadsetSpeakerOn
                        || audioName == AudioName.HeadsetSpeakerAlreadyOn
                        || audioName == AudioName.CarSpeakerOff
                        || audioName == AudioName.CarSpeakerAlreadyOff
                        || audioName == AudioName.CarSpeakerOn
                        || audioName == AudioName.CarSpeakerAlreadyOn
                        || audioName == AudioName.AllSpeakerOff
                        || audioName == AudioName.AllSpeakerAlreadyOff
                        || audioName == AudioName.AllSpeakerOn
                        || audioName == AudioName.AllSpeakerAlreadyOn
                        || audioName == AudioName.SoundModeOn
                        || audioName == AudioName.SoundModusAlreadyOff
                        || audioName == AudioName.SoundModusOff
                        || audioName == AudioName.CliffSensorOn
                        || audioName == AudioName.CliffSensorOff
                        || audioName == AudioName.AutomaticDriveOn_Status
                        || audioName == AudioName.AutomaticDriveOff_Status
                        || audioName == AudioName.DanceOn_Status
                        || audioName == AudioName.DanceOff_Status
                        || audioName == AudioName.Commands
                        || audioName == AudioName.SystemCommands
                        || audioName == AudioName.ControlCommands
                        || audioName == AudioName.ReallyRestart
                        || audioName == AudioName.ReallyShutdown)
                    {
                        speakerOnOff.CarSpeakerOn = true;
                        speakerOnOff.HeadsetSpeakerOn = true;
                    }
                    else if (SoundModeOn
                             && audioName != AudioName.AutomatischesFahrenFesthaengen
                             && audioName != AudioName.AutomaticDriveOn
                             && audioName != AudioName.AutomaticDriveOff)
                    {
                        speakerOnOff.CarSpeakerOn = false;
                        speakerOnOff.HeadsetSpeakerOn = false;
                    }
                }
            }

            return speakerOnOff;
        }
    }

    public class SpeakerOnOff
    {
        public bool HeadsetSpeakerOn { get; set; }
        public bool CarSpeakerOn { get; set; }
    }
}
