using robot.sl.Helper;
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

        public static void Stop()
        {
            _stopped = true;
        }

        public static async Task Play(AudioName AudioPlayerAudioName, double? headsetGain, double? speakerGain)
        {
            await Play(AudioPlayerAudioName, headsetGain, speakerGain, false);
        }

        public static async Task PlayAndWaitAsync(AudioName AudioPlayerAudioName, double? headsetGain, double? speakerGain)
        {
            await Play(AudioPlayerAudioName, headsetGain, speakerGain, true);
        }

        public static async Task Play(AudioName AudioPlayerAudioName)
        {
            await Play(AudioPlayerAudioName, null, null, false);
        }

        public static async Task PlayAndWaitAsync(AudioName AudioPlayerAudioName)
        {
            await Play(AudioPlayerAudioName, null, null, true);
        }
        
        private static async Task Play(AudioName audioName, double? headsetGain, double? speakerGain, bool wait)
        {
            var speakerOnOff = GetSpeakerOnOff(audioName);

            if (headsetGain == null)
            {
                headsetGain = 0.3;
            }

            if (speakerGain == null)
            {
                speakerGain = 5.0;
            }

            if (speakerOnOff.CarSpeakerOn
                && speakerOnOff.HeadsetSpeakerOn)
            {
                var headsetSpeaker = _headsetSpeaker.Play(EnumHelper.GetName(audioName), headsetGain.Value);
                var carSpeaker = _carSpeaker.Play(EnumHelper.GetName(audioName), speakerGain.Value);

                if (wait)
                {
                    await Task.WhenAll(new Task[] { headsetSpeaker, carSpeaker });
                }
            }
            else if (speakerOnOff.CarSpeakerOn)
            {
                var carSpeaker = _carSpeaker.Play(EnumHelper.GetName(audioName), speakerGain.Value);

                if (wait)
                {
                    await carSpeaker;
                }
            }
            else if (speakerOnOff.HeadsetSpeakerOn)
            {
                var headesetSpeaker = _headsetSpeaker.Play(EnumHelper.GetName(audioName), headsetGain.Value);

                if (wait)
                {
                    await headesetSpeaker;
                }
            }
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
            else if (audioName != AudioName.Shutdown
                     && audioName != AudioName.Restart
                     && audioName != AudioName.AppError)
            {
                if (_stopped)
                {
                    speakerOnOff.CarSpeakerOn = false;
                    speakerOnOff.HeadsetSpeakerOn = false;
                }
                else
                {
                    if (audioName == AudioName.Shutdown
                        || audioName == AudioName.Restart
                        || audioName == AudioName.AppError
                        || audioName == AudioName.GamepadVibrationOn
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
                        || audioName == AudioName.CarSpeakerOffHeadsetSpeakerOnSoundModeOff
                        || audioName == AudioName.CarSpeakerOnHeadsetSpeakerOffSoundModeOff
                        || audioName == AudioName.CarSpeakerOffHeadsetSpeakerOnSoundModeOn
                        || audioName == AudioName.CarSpeakerOnHeadsetSpeakerOffSoundModeOn
                        || audioName == AudioName.AllSpeakerOffSoundModeOff
                        || audioName == AudioName.AllSpeakerOffSoundModeOn
                        || audioName == AudioName.AllSpeakerOnSoundModeOff
                        || audioName == AudioName.AllSpeakerOnSoundModeOn
                        || audioName == AudioName.ReallyRestart
                        || audioName == AudioName.ReallyShutdown)
                    {
                        speakerOnOff.CarSpeakerOn = true;
                        speakerOnOff.HeadsetSpeakerOn = true;
                    }
                    else if (SoundModeOn
                             && audioName != AudioName.AutomatischesFahrenFesthaengen
                             && audioName != AudioName.StartAutomaticDrive
                             && audioName != AudioName.StopAutomaticDrive)
                    {
                        speakerOnOff.CarSpeakerOn = false;
                        speakerOnOff.HeadsetSpeakerOn = false;
                    }
                }
            }
            else
            {
                speakerOnOff.CarSpeakerOn = true;
                speakerOnOff.HeadsetSpeakerOn = true;
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
