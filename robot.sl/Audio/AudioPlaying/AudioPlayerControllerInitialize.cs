using robot.sl.Helper;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace robot.sl.Audio.AudioPlaying
{
    public static partial class AudioPlayerController
    {
        private static async Task AddAudioFile(AudioName audioName, string text)
        {
            IStorageFile storageFile = null;
            var soundFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(EnumHelper.GetName(audioName));
            if (soundFile == null)
            {
                using (var headsetStream = await SpeechSynthesis.SpeakAsStream(text))
                {
                    var localFolder = ApplicationData.Current.LocalFolder;
                    storageFile = await localFolder.CreateFileAsync(EnumHelper.GetName(audioName), CreationCollisionOption.FailIfExists);
                    using (var outputStream = await storageFile.OpenStreamForWriteAsync())
                    {
                        await headsetStream.AsStream().CopyToAsync(outputStream);
                        await outputStream.FlushAsync();
                    }
                }
            }

            storageFile = await ApplicationData.Current.LocalFolder.GetFileAsync(EnumHelper.GetName(audioName));
            await _headsetSpeaker.AddFileAsync(storageFile);
            await _carSpeaker.AddFileAsync(storageFile);
        }

        public static async Task Initialize()
        {
            CarSpeakerOn = true;
            HeadsetSpeakerOn = true;
            SoundModeOn = false;

            _headsetSpeaker = new AudioPlayer();
            await _headsetSpeaker.Initialize(true);

            _carSpeaker = new AudioPlayer();
            await _carSpeaker.Initialize(false);

            //Jede Box muss eine eigenen Stream und StorageFile erhalten, der Stream darf auch nicht kopiert werden
            //sonst kommt es zu einer ViolationAccess Exception

            //Welcome
            await AddAudioFile(AudioName.Welcome, "Hallo Freunde, ich bin bereit zum Spielen!");
            
            //Vor
            await AddAudioFile(AudioName.Vor, "Vor");
            //Zurück
            await AddAudioFile(AudioName.Zurueck, "Zurück");
            //Links
            await AddAudioFile(AudioName.Links, "Links");
            //Rechts
            await AddAudioFile(AudioName.Rechts, "Rechts");
            //Stop
            await AddAudioFile(AudioName.Stop, "Stop");
            //Wenden
            await AddAudioFile(AudioName.Wenden, "Wenden");
            //Leicht Links
            await AddAudioFile(AudioName.LeichtLinks, "Leicht Links");
            //Leicht Rechts
            await AddAudioFile(AudioName.LeichtRechts, "Leicht Rechts");
            //Langsam
            await AddAudioFile(AudioName.Langsam, "Langsam");
            //Normal
            await AddAudioFile(AudioName.Normal, "Normal");
            //Schnell
            await AddAudioFile(AudioName.Schnell, "Schnell");
            //Tanzen
            await AddAudioFile(AudioName.Tanzen, "Tanzen");
            var steuerungsbefehle = "Folgende Steuerungsbefehle sind möglich: Vor, Zurück, Links, Rechts, Stop, Wenden, Leicht Links, Leicht Rechts, Langsam, Normal, Schnell, Tanzen, Kamera hoch, Kamera leicht hoch, Kamera runter, Kamera leicht runter, Aktivire automatisches Fahren und Deaktivire automatisches Fahren, ";
            var systembefehle = "Folgende Systembefehle sind möglich: Befehl, Steuerungsbefehle, Systembefehle, Aktivire Lautsprecher, Deaktivire Lautsprecher, Aktivire Headset Lautsprecher, Deaktivire Headset Lautsprecher, Aktivire Fahrzeug Lautsprecher, Deaktivere Fahrzeug Lautsprecher, Lautsprecher und Sound Modus, Aktivire Sound Modus und Deaktivire Sound Modus";
            //Befehl
            await AddAudioFile(AudioName.Befehl, steuerungsbefehle + systembefehle);
            //Steuerungsbefehle
            await AddAudioFile(AudioName.Steuerungsbefehle, steuerungsbefehle);
            //Systembefehle
            await AddAudioFile(AudioName.Systembefehle, systembefehle);
            //StarkeVibration
            await AddAudioFile(AudioName.StarkeVibration, "Aufpassen, ich bin nicht unzerstörbar");
            //Steht
            await AddAudioFile(AudioName.Steht, "Mir ist langweilig, fahre mich doch ein bisschen herum.");
            //AutomatischesFahrenFesthaengen
            await AddAudioFile(AudioName.AutomatischesFahrenFesthaengen, "Ich stecke fest, ein Moment ich versuche mich zu befreien.");
            //Shutdown
            await AddAudioFile(AudioName.Shutdown, "Bis bald, ich schalte mich nun aus.");
            //Reboot
            await AddAudioFile(AudioName.Restart, "Bis gleich, ich starte mich neu.");
            //StartAutomaticDrive
            await AddAudioFile(AudioName.StartAutomaticDrive, "Jetzt übernehme ich die Steuerung.");
            //StopAutomaticDrive
            await AddAudioFile(AudioName.StopAutomaticDrive, "Jetzt kannst Du mich wieder Steuern.");
            //ReallyRestart
            await AddAudioFile(AudioName.ReallyRestart, "Möchtest Du mich wirklich neustarten?");
            //ReallyShutdown
            await AddAudioFile(AudioName.ReallyShutdown, "Möchtest Du mich wirklich ausschalten?");
            //GamepadVibrationOn
            await AddAudioFile(AudioName.GamepadVibrationOn, "Die Gamepad Vibration ist nun aktiv.");
            //GamepadVibrationOff
            await AddAudioFile(AudioName.GamepadVibrationOff, "Die Gamepad Vibration wurde deaktivirt.");
            //AppError
            await AddAudioFile(AudioName.AppError, "Ich bin abgestürzt, ich stehe gleich wieder zur Verfügung.");
            //SoundModeAlreadyOn
            await AddAudioFile(AudioName.SoundModeAlreadyOn, "Der Sound Modus automatisches Fahren ist bereits an.");
            //HeadsetSpeakerOff
            await AddAudioFile(AudioName.HeadsetSpeakerOff, "Der Headset Lautsprecher ist jetzt aus.");
            //HeadsetSpeakerAlreadyOff
            await AddAudioFile(AudioName.HeadsetSpeakerAlreadyOff, "Der Headset Lautsprecher ist bereits aus.");
            //HeadsetSpeakerOn
            await AddAudioFile(AudioName.HeadsetSpeakerOn, "Der Headset Lautsprecher ist jetzt an.");
            //HeadsetSpeakerAlreadyOn
            await AddAudioFile(AudioName.HeadsetSpeakerAlreadyOn, "Der Headset Lautsprecher ist bereits an.");
            //CarSpeakerOff
            await AddAudioFile(AudioName.CarSpeakerOff, "Der Fahrzeug Lautsprecher ist jetzt aus.");
            //CarSpeakerAlreadyOff
            await AddAudioFile(AudioName.CarSpeakerAlreadyOff, "Der Fahrzeug Lautsprecher ist bereits aus.");
            //CarSpeakerOn
            await AddAudioFile(AudioName.CarSpeakerOn, "Der Fahrzeug Lautsprecher ist jetzt an.");
            //CarSpeakerAlreadyOn
            await AddAudioFile(AudioName.CarSpeakerAlreadyOn, "Der Fahrzeug Lautsprecher ist bereits an.");
            //AllSpeakerOff
            await AddAudioFile(AudioName.AllSpeakerOff, "Alle Lautsprecher sind jetzt aus.");
            //AllSpeakerAlreadyOff
            await AddAudioFile(AudioName.AllSpeakerAlreadyOff, "Alle Lautsprecher sind bereits aus.");
            //AllSpeakerOn
            await AddAudioFile(AudioName.AllSpeakerOn, "Alle Lautsprecher sind jetzt an.");
            //AllSpeakerAlreadyOn
            await AddAudioFile(AudioName.AllSpeakerAlreadyOn, "Alle Lautsprecher sind jetzt an.");
            //SoundModeOn
            await AddAudioFile(AudioName.SoundModeOn, "Der Sound Modus automatisches Fahren ist jetzt an.");
            //SoundModusAlreadyOff
            await AddAudioFile(AudioName.SoundModusAlreadyOff, "Der Sound Modus automatisches Fahren ist bereits aus.");
            //SoundModusOff
            await AddAudioFile(AudioName.SoundModusOff, "Der Sound Modus automatisches Fahren ist jetzt aus.");
            //CarSpeakerOffHeadsetSpeakerOnSoundModeOff
            await AddAudioFile(AudioName.CarSpeakerOffHeadsetSpeakerOnSoundModeOff, "Der Fahrzeug Lautsprecher ist aus. Der Headset Lautsprecher ist an. Der Sound Modus automatisches Fahren ist aus.");
            //CarSpeakerOnHeadsetSpeakerOffSoundModeOff
            await AddAudioFile(AudioName.CarSpeakerOnHeadsetSpeakerOffSoundModeOff, "Der Fahrzeug Lautsprecher ist an. Der Headset Lautsprecher ist aus. Der Sound Modus automatisches Fahren ist aus.");
            //CarSpeakerOffHeadsetSpeakerOnSoundModeOn
            await AddAudioFile(AudioName.CarSpeakerOffHeadsetSpeakerOnSoundModeOn, "Der Fahrzeug Lautsprecher ist aus. Der Headset Lautsprecher ist an. Der Sound Modus automatisches Fahren ist an.");
            //CarSpeakerOnHeadsetSpeakerOffSoundModeOn
            await AddAudioFile(AudioName.CarSpeakerOnHeadsetSpeakerOffSoundModeOn, "Der Fahrzeug Lautsprecher ist an. Der Headset Lautsprecher ist aus. Der Sound Modus automatisches Fahren ist an.");
            //AllSpeakerOffSoundModeOff
            await AddAudioFile(AudioName.AllSpeakerOffSoundModeOff, "Alle Lautsprecher sind aus. Der Sound Modus automatisches Fahren ist ebenfalls an.");
            //AllSpeakerOffSoundModeOn
            await AddAudioFile(AudioName.AllSpeakerOffSoundModeOn, "Alle Lautsprecher sind aus. Der Sound Modus automatisches Fahren ist an.");
            //AllSpeakerOnSoundModeOff
            await AddAudioFile(AudioName.AllSpeakerOnSoundModeOff, "Alle Lautsprecher sind an. Der Sound Modus automatisches Fahren ist aus.");
            //AllSpeakerOnSoundModeOn
            await AddAudioFile(AudioName.AllSpeakerOnSoundModeOn, "Alle Lautsprecher sind an. Der Sound Modus automatisches Fahren ist ebenfalls an.");
            //CameraUp
            await AddAudioFile(AudioName.CameraUp, "Kamera hoch");
            //CameraDown
            await AddAudioFile(AudioName.CameraDown, "Kamera runter");
            //CameraLightUp
            await AddAudioFile(AudioName.CameraLightUp, "Kamera leicht hoch");
            //CameraLightDown
            await AddAudioFile(AudioName.CameraLightDown, "Kamera leicht runter");
            //TurnToLongLeft
            await AddAudioFile(AudioName.TurnToLongLeft, "Mir ist schwindelig, dreh doch mal wieder nach rechts.");
            //TurnToLongRight
            await AddAudioFile(AudioName.TurnToLongRight, "Mir ist schwindelig, dreh doch mal wieder nach links.");
        }
    }
}
