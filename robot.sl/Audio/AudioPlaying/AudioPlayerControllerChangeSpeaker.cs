using System.Threading.Tasks;

namespace robot.sl.Audio.AudioPlaying
{
    public static partial class AudioPlayerController
    {
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

        public static async Task PlaySpeakerOnOffSoundModeAsync()
        {
            if (CarSpeakerOn && HeadsetSpeakerOn)
            {
                if (SoundModeOn)
                {
                    await PlayAsync(AudioName.AllSpeakerOnSoundModeOn);
                }
                else
                {
                    await PlayAsync(AudioName.AllSpeakerOnSoundModeOff);
                }
            }
            else if (!CarSpeakerOn && !HeadsetSpeakerOn)
            {
                if (SoundModeOn)
                {
                    await PlayAsync(AudioName.AllSpeakerOffSoundModeOn);
                }
                else
                {
                    await PlayAsync(AudioName.AllSpeakerOffSoundModeOff);
                }
            }
            else
            {
                if (SoundModeOn)
                {
                    if (CarSpeakerOn)
                    {
                        await PlayAsync(AudioName.CarSpeakerOnHeadsetSpeakerOffSoundModeOn);
                    }
                    else if (HeadsetSpeakerOn)
                    {
                        await PlayAsync(AudioName.CarSpeakerOffHeadsetSpeakerOnSoundModeOn);
                    }
                }
                else
                {
                    if (CarSpeakerOn)
                    {
                        await PlayAsync(AudioName.CarSpeakerOnHeadsetSpeakerOffSoundModeOff);
                    }
                    else if (HeadsetSpeakerOn)
                    {
                        await PlayAsync(AudioName.CarSpeakerOffHeadsetSpeakerOnSoundModeOff);
                    }
                }
            }
        }
    }
}
