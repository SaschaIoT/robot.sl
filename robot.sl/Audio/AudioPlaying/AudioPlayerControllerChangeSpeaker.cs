using System.Threading.Tasks;

namespace robot.sl.Audio.AudioPlaying
{
    public static partial class AudioPlayerController
    {
        public static async Task SetAllSpeakerOnOff(bool speakerOn)
        {
            if (speakerOn)
            {
                if (CarSpeakerOn && HeadsetSpeakerOn)
                {
                    await Play(AudioName.AllSpeakerAlreadyOn);
                }
                else
                {
                    await Play(AudioName.AllSpeakerOn);
                }

                CarSpeakerOn = true;
                HeadsetSpeakerOn = true;
            }
            else
            {
                if (!CarSpeakerOn && !HeadsetSpeakerOn)
                {
                    await Play(AudioName.AllSpeakerAlreadyOff);
                }
                else
                {
                    await Play(AudioName.AllSpeakerOff);
                }

                CarSpeakerOn = false;
                HeadsetSpeakerOn = false;
            }
        }

        public static async Task SetCarSpeakerOnOffToggle()
        {
            await SetCarSpeakerOnOff(!CarSpeakerOn);
        }

        public static async Task SetCarSpeakerOnOff(bool carSpeakerOn)
        {
            if (carSpeakerOn)
            {
                if (CarSpeakerOn)
                {
                    await Play(AudioName.CarSpeakerAlreadyOn);
                }
                else
                {
                    await Play(AudioName.CarSpeakerOn);
                }

                CarSpeakerOn = true;
            }
            else
            {
                if (!CarSpeakerOn)
                {
                    await Play(AudioName.CarSpeakerAlreadyOff);
                }
                else
                {
                    await Play(AudioName.CarSpeakerOff);
                }

                CarSpeakerOn = false;
            }
        }

        public static async Task SetHeadsetSpeakerOnOffToggle()
        {
            await SetHeadsetSpeakerOnOff(!HeadsetSpeakerOn);
        }

        public static async Task SetHeadsetSpeakerOnOff(bool headsetSpeakerOn)
        {
            if (headsetSpeakerOn)
            {
                if (HeadsetSpeakerOn)
                {
                    await Play(AudioName.HeadsetSpeakerAlreadyOn);
                }
                else
                {
                    await Play(AudioName.HeadsetSpeakerOn);
                }

                HeadsetSpeakerOn = true;
            }
            else
            {
                if (!HeadsetSpeakerOn)
                {
                    await Play(AudioName.HeadsetSpeakerAlreadyOff);
                }
                else
                {
                    await Play(AudioName.HeadsetSpeakerOff);
                }

                HeadsetSpeakerOn = false;
            }
        }

        public static async Task SetSoundModeOnOffToggle()
        {
            await SetSoundModeOnOff(!SoundModeOn);
        }

        public static async Task SetSoundModeOnOff(bool soundModeOn)
        {
            if (soundModeOn)
            {
                if (SoundModeOn)
                {
                    await Play(AudioName.SoundModeAlreadyOn);
                }
                else
                {
                    await Play(AudioName.SoundModeOn);
                }

                SoundModeOn = true;
            }
            else
            {
                if (!SoundModeOn)
                {
                    await Play(AudioName.SoundModusAlreadyOff);
                }
                else
                {
                    await Play(AudioName.SoundModusOff);
                }

                SoundModeOn = false;
            }
        }

        public static async Task PlaySpeakerOnOffSoundMode()
        {
            if (CarSpeakerOn && HeadsetSpeakerOn)
            {
                if (SoundModeOn)
                {
                    await Play(AudioName.AllSpeakerOnSoundModeOn);
                }
                else
                {
                    await Play(AudioName.AllSpeakerOnSoundModeOff);
                }
            }
            else if (!CarSpeakerOn && !HeadsetSpeakerOn)
            {
                if (SoundModeOn)
                {
                    await Play(AudioName.AllSpeakerOffSoundModeOn);
                }
                else
                {
                    await Play(AudioName.AllSpeakerOffSoundModeOff);
                }
            }
            else
            {
                if (SoundModeOn)
                {
                    if (CarSpeakerOn)
                    {
                        await Play(AudioName.CarSpeakerOnHeadsetSpeakerOffSoundModeOn);
                    }
                    else if (HeadsetSpeakerOn)
                    {
                        await Play(AudioName.CarSpeakerOffHeadsetSpeakerOnSoundModeOn);
                    }
                }
                else
                {
                    if (CarSpeakerOn)
                    {
                        await Play(AudioName.CarSpeakerOnHeadsetSpeakerOffSoundModeOff);
                    }
                    else if (HeadsetSpeakerOn)
                    {
                        await Play(AudioName.CarSpeakerOffHeadsetSpeakerOnSoundModeOff);
                    }
                }
            }
        }
    }
}
