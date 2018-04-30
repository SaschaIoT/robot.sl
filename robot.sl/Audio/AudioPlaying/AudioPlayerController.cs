﻿using robot.sl.Helper;
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

        public static async Task PlayAndWaitAsync(AudioName AudioPlayerAudioName)
        {
            await PlayAsync(AudioPlayerAudioName, null, null, true, null);
        }
        
        private static async Task PlayAsync(AudioName audioName, double? headsetGain, double? speakerGain, bool wait, CancellationToken? cancellationToken)
        {
            var speakerOnOff = GetSpeakerOnOff(audioName);

            if (headsetGain == null)
            {
                headsetGain = 0.8;
            }

            if (speakerGain == null)
            {
                speakerGain = 3.2;
            }

            if (speakerOnOff.CarSpeakerOn
                && speakerOnOff.HeadsetSpeakerOn)
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
                        || audioName == AudioName.CarSpeakerOffHeadsetSpeakerOnSoundModeOff
                        || audioName == AudioName.CarSpeakerOnHeadsetSpeakerOffSoundModeOff
                        || audioName == AudioName.CarSpeakerOffHeadsetSpeakerOnSoundModeOn
                        || audioName == AudioName.CarSpeakerOnHeadsetSpeakerOffSoundModeOn
                        || audioName == AudioName.AllSpeakerOffSoundModeOff
                        || audioName == AudioName.AllSpeakerOffSoundModeOn
                        || audioName == AudioName.AllSpeakerOnSoundModeOff
                        || audioName == AudioName.AllSpeakerOnSoundModeOn
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
