using robot.sl.CarControl;
using System.Threading.Tasks;

namespace robot.sl.Audio.AudioPlaying
{
    public static partial class AudioPlayerController
    {
        private static int PAUSE_BETWENN_WORDS_MILLISECONDS = 300;

        public static async Task SetAllSpeakerOnOffAsync(bool speakerOn)
        {
            if (speakerOn)
            {
                if (CarSpeakerOn && HeadsetSpeakerOn)
                {
                    await PlayAsync(AudioName.AllSpeakerAlreadyOn);
                }
                else
                {
                    await PlayAsync(AudioName.AllSpeakerOn);
                }

                CarSpeakerOn = true;
                HeadsetSpeakerOn = true;
            }
            else
            {
                if (!CarSpeakerOn && !HeadsetSpeakerOn)
                {
                    await PlayAsync(AudioName.AllSpeakerAlreadyOff);
                }
                else
                {
                    await PlayAsync(AudioName.AllSpeakerOff);
                }

                CarSpeakerOn = false;
                HeadsetSpeakerOn = false;
            }
        }

        public static async Task SetCarSpeakerOnOffToggle()
        {
            await SetCarSpeakerOnOffAsync(!CarSpeakerOn);
        }

        public static async Task SetCarSpeakerOnOffAsync(bool carSpeakerOn)
        {
            if (carSpeakerOn)
            {
                if (CarSpeakerOn)
                {
                    await PlayAsync(AudioName.CarSpeakerAlreadyOn);
                }
                else
                {
                    await PlayAsync(AudioName.CarSpeakerOn);
                }

                CarSpeakerOn = true;
            }
            else
            {
                if (!CarSpeakerOn)
                {
                    await PlayAsync(AudioName.CarSpeakerAlreadyOff);
                }
                else
                {
                    await PlayAsync(AudioName.CarSpeakerOff);
                }

                CarSpeakerOn = false;
            }
        }

        public static async Task SetHeadsetSpeakerOnOffToggle()
        {
            await SetHeadsetSpeakerOnOffAsync(!HeadsetSpeakerOn);
        }

        public static async Task SetHeadsetSpeakerOnOffAsync(bool headsetSpeakerOn)
        {
            if (headsetSpeakerOn)
            {
                if (HeadsetSpeakerOn)
                {
                    await PlayAsync(AudioName.HeadsetSpeakerAlreadyOn);
                }
                else
                {
                    await PlayAsync(AudioName.HeadsetSpeakerOn);
                }

                HeadsetSpeakerOn = true;
            }
            else
            {
                if (!HeadsetSpeakerOn)
                {
                    await PlayAsync(AudioName.HeadsetSpeakerAlreadyOff);
                }
                else
                {
                    await PlayAsync(AudioName.HeadsetSpeakerOff);
                }

                HeadsetSpeakerOn = false;
            }
        }

        public static async Task SetSoundModeOnOffToggle()
        {
            await SetSoundModeOnOffAsync(!SoundModeOn);
        }

        public static async Task SetSoundModeOnOffAsync(bool soundModeOn)
        {
            if (soundModeOn)
            {
                if (SoundModeOn)
                {
                    await PlayAsync(AudioName.SoundModeAlreadyOn);
                }
                else
                {
                    await PlayAsync(AudioName.SoundModeOn);
                }

                SoundModeOn = true;
            }
            else
            {
                if (!SoundModeOn)
                {
                    await PlayAsync(AudioName.SoundModusAlreadyOff);
                }
                else
                {
                    await PlayAsync(AudioName.SoundModusOff);
                }

                SoundModeOn = false;
            }
        }

        public static async void PlaySpeakerOnOffSoundModeAsync(AutomaticDrive automaticDrive, Dance dance)
        {
            //CarSpeaker
            if(CarSpeakerOn)
            {
                if (await PlayAndWaitAsync(AudioName.CarSpeakerOn) == false)
                    return;
            }
            else
            {
                if (await PlayAndWaitAsync(AudioName.CarSpeakerOff) == false)
                    return;
            }

            await Task.Delay(PAUSE_BETWENN_WORDS_MILLISECONDS);

            //HeadsetSpeaker
            if (HeadsetSpeakerOn)
            {
                if (await PlayAndWaitAsync(AudioName.HeadsetSpeakerOn) == false)
                    return;
            }
            else
            {
                if (await PlayAndWaitAsync(AudioName.HeadsetSpeakerOff) == false)
                    return;
            }

            await Task.Delay(PAUSE_BETWENN_WORDS_MILLISECONDS);

            //SoundMode
            if (SoundModeOn)
            {
                if (await PlayAndWaitAsync(AudioName.SoundModeOn) == false)
                    return;
            }
            else
            {
                if (await PlayAndWaitAsync(AudioName.SoundModusOff) == false)
                    return;
            }

            await Task.Delay(PAUSE_BETWENN_WORDS_MILLISECONDS);

            //CliffSensor
            if (automaticDrive.GetCliffSensorState())
            {
                if (await PlayAndWaitAsync(AudioName.CliffSensorOn) == false)
                    return;
            }
            else
            {
                if (await PlayAndWaitAsync(AudioName.CliffSensorOff) == false)
                    return;
            }

            await Task.Delay(PAUSE_BETWENN_WORDS_MILLISECONDS);

            //AutomaticDrive
            if (automaticDrive.IsRunning)
            {
                if (await PlayAndWaitAsync(AudioName.AutomaticDriveOn_Status) == false)
                    return;
            }
            else
            {
                if (await PlayAndWaitAsync(AudioName.AutomaticDriveOff_Status) == false)
                    return;
            }

            await Task.Delay(PAUSE_BETWENN_WORDS_MILLISECONDS);

            //Dance
            if (dance.IsRunning)
            {
                if (await PlayAndWaitAsync(AudioName.DanceOn_Status) == false)
                    return;
            }
            else
            {
                if (await PlayAndWaitAsync(AudioName.DanceOff_Status) == false)
                    return;
            }
        }
    }
}
