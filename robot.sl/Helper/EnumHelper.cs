using robot.sl.Audio.AudioPlaying;
using System;

namespace robot.sl.Helper
{
    public static class EnumHelper
    {
        public static string GetName(AudioName value)
        {
            return Enum.GetName(typeof(AudioName), value);
        }
    }
}
