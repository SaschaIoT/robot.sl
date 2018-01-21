using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;

namespace robot.sl.Audio
{
    public static class SpeechSynthesis
    {
        private static SpeechSynthesizer _speechSynthesizer;
        public static void Initialze()
        {
            _speechSynthesizer = new SpeechSynthesizer();
            var info = (from m in SpeechSynthesizer.AllVoices
                        where m.Language == "de-DE"
                        && m.Gender == VoiceGender.Female
                        select m).FirstOrDefault();
            _speechSynthesizer.Voice = info;
        }

        public static async Task<SpeechSynthesisStream> SpeakAsStreamAsync(string speechText)
        {
            var speechStream = await _speechSynthesizer.SynthesizeTextToStreamAsync(speechText);
            return speechStream;
        }
    }
}
