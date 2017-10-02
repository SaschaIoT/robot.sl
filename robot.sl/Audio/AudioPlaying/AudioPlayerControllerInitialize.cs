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
            var audioFile1 = AddAudioFile(AudioName.Welcome, "Hallo Freunde, ich bin bereit zum Spielen!");
            
            //Vor
            var audioFile2 = AddAudioFile(AudioName.Vor, "Vor");
            //Zurück
            var audioFile3 = AddAudioFile(AudioName.Zurueck, "Zurück");
            //Links
            var audioFile4 = AddAudioFile(AudioName.Links, "Links");
            //Rechts
            var audioFile5 = AddAudioFile(AudioName.Rechts, "Rechts");
            //Stop
            var audioFile6 = AddAudioFile(AudioName.Stop, "Stop");
            //Wenden
            var audioFile7 = AddAudioFile(AudioName.Wenden, "Wenden");
            //Leicht Links
            var audioFile8 = AddAudioFile(AudioName.LeichtLinks, "Leicht Links");
            //Leicht Rechts
            var audioFile9 = AddAudioFile(AudioName.LeichtRechts, "Leicht Rechts");
            //Langsam
            var audioFile10 = AddAudioFile(AudioName.Langsam, "Langsam");
            //Normal
            var audioFile11 = AddAudioFile(AudioName.Normal, "Normal");
            //Schnell
            var audioFile12 = AddAudioFile(AudioName.Schnell, "Schnell");
            //Tanzen
            var audioFile13 = AddAudioFile(AudioName.Tanzen, "Tanzen");
            var steuerungsbefehle = "Folgende Steuerungsbefehle sind möglich: Vor, Zurück, Links, Rechts, Stop, Wenden, Leicht Links, Leicht Rechts, Langsam, Normal, Schnell, Tanzen, Kamera hoch, Kamera leicht hoch, Kamera runter, Kamera leicht runter, Aktivire automatisches Fahren und Deaktivire automatisches Fahren, ";
            var systembefehle = "Folgende Systembefehle sind möglich: Befehl, Steuerungsbefehle, Systembefehle, Aktivire Lautsprecher, Deaktivire Lautsprecher, Aktivire Headset Lautsprecher, Deaktivire Headset Lautsprecher, Aktivire Fahrzeug Lautsprecher, Deaktivere Fahrzeug Lautsprecher, Lautsprecher und Sound Modus, Aktivire Sound Modus und Deaktivire Sound Modus";
            //Befehl
            var audioFile14 = AddAudioFile(AudioName.Befehl, steuerungsbefehle + systembefehle);
            //Steuerungsbefehle
            var audioFile15 = AddAudioFile(AudioName.Steuerungsbefehle, steuerungsbefehle);
            //Systembefehle
            var audioFile16 = AddAudioFile(AudioName.Systembefehle, systembefehle);
            //StarkeVibration
            var audioFile17 = AddAudioFile(AudioName.StarkeVibration, "Aufpassen, ich bin nicht unzerstörbar");
            //Steht
            var audioFile18 = AddAudioFile(AudioName.Steht, "Mir ist langweilig, fahre mich doch ein bisschen herum.");
            //AutomatischesFahrenFesthaengen
            var audioFile19 = AddAudioFile(AudioName.AutomatischesFahrenFesthaengen, "Ich stecke fest, ein Moment ich versuche mich zu befreien.");
            //Shutdown
            var audioFile20 = AddAudioFile(AudioName.Shutdown, "Bis bald, ich schalte mich nun aus.");
            //Reboot
            var audioFile21 = AddAudioFile(AudioName.Restart, "Bis gleich, ich starte mich neu.");
            //StartAutomaticDrive
            var audioFile22 = AddAudioFile(AudioName.StartAutomaticDrive, "Jetzt übernehme ich die Steuerung.");
            //StopAutomaticDrive
            var audioFile23 = AddAudioFile(AudioName.StopAutomaticDrive, "Jetzt kannst Du mich wieder Steuern.");
            //ReallyRestart
            var audioFile24 = AddAudioFile(AudioName.ReallyRestart, "Möchtest Du mich wirklich neustarten?");
            //ReallyShutdown
            var audioFile25 = AddAudioFile(AudioName.ReallyShutdown, "Möchtest Du mich wirklich ausschalten?");
            //GamepadVibrationOn
            var audioFile26 = AddAudioFile(AudioName.GamepadVibrationOn, "Die Gamepad Vibration ist nun aktiv.");
            //GamepadVibrationOff
            var audioFile27 = AddAudioFile(AudioName.GamepadVibrationOff, "Die Gamepad Vibration wurde deaktivirt.");
            //AppError
            var audioFile28 = AddAudioFile(AudioName.AppError, "Ich bin abgestürzt, ich stehe gleich wieder zur Verfügung.");
            //SoundModeAlreadyOn
            var audioFile29 = AddAudioFile(AudioName.SoundModeAlreadyOn, "Der Sound Modus automatisches Fahren ist bereits an.");
            //HeadsetSpeakerOff
            var audioFile30 = AddAudioFile(AudioName.HeadsetSpeakerOff, "Der Headset Lautsprecher ist jetzt aus.");
            //HeadsetSpeakerAlreadyOff
            var audioFile31 = AddAudioFile(AudioName.HeadsetSpeakerAlreadyOff, "Der Headset Lautsprecher ist bereits aus.");
            //HeadsetSpeakerOn
            var audioFile32 = AddAudioFile(AudioName.HeadsetSpeakerOn, "Der Headset Lautsprecher ist jetzt an.");
            //HeadsetSpeakerAlreadyOn
            var audioFile33 = AddAudioFile(AudioName.HeadsetSpeakerAlreadyOn, "Der Headset Lautsprecher ist bereits an.");
            //CarSpeakerOff
            var audioFile34 = AddAudioFile(AudioName.CarSpeakerOff, "Der Fahrzeug Lautsprecher ist jetzt aus.");
            //CarSpeakerAlreadyOff
            var audioFile35 = AddAudioFile(AudioName.CarSpeakerAlreadyOff, "Der Fahrzeug Lautsprecher ist bereits aus.");
            //CarSpeakerOn
            var audioFile36 = AddAudioFile(AudioName.CarSpeakerOn, "Der Fahrzeug Lautsprecher ist jetzt an.");
            //CarSpeakerAlreadyOn
            var audioFile37 = AddAudioFile(AudioName.CarSpeakerAlreadyOn, "Der Fahrzeug Lautsprecher ist bereits an.");
            //AllSpeakerOff
            var audioFile38 = AddAudioFile(AudioName.AllSpeakerOff, "Alle Lautsprecher sind jetzt aus.");
            //AllSpeakerAlreadyOff
            var audioFile39 = AddAudioFile(AudioName.AllSpeakerAlreadyOff, "Alle Lautsprecher sind bereits aus.");
            //AllSpeakerOn
            var audioFile40 = AddAudioFile(AudioName.AllSpeakerOn, "Alle Lautsprecher sind jetzt an.");
            //AllSpeakerAlreadyOn
            var audioFile41 = AddAudioFile(AudioName.AllSpeakerAlreadyOn, "Alle Lautsprecher sind jetzt an.");
            //SoundModeOn
            var audioFile42 = AddAudioFile(AudioName.SoundModeOn, "Der Sound Modus automatisches Fahren ist jetzt an.");
            //SoundModusAlreadyOff
            var audioFile43 = AddAudioFile(AudioName.SoundModusAlreadyOff, "Der Sound Modus automatisches Fahren ist bereits aus.");
            //SoundModusOff
            var audioFile44 = AddAudioFile(AudioName.SoundModusOff, "Der Sound Modus automatisches Fahren ist jetzt aus.");
            //CarSpeakerOffHeadsetSpeakerOnSoundModeOff
            var audioFile45 = AddAudioFile(AudioName.CarSpeakerOffHeadsetSpeakerOnSoundModeOff, "Der Fahrzeug Lautsprecher ist aus. Der Headset Lautsprecher ist an. Der Sound Modus automatisches Fahren ist aus.");
            //CarSpeakerOnHeadsetSpeakerOffSoundModeOff
            var audioFile46 = AddAudioFile(AudioName.CarSpeakerOnHeadsetSpeakerOffSoundModeOff, "Der Fahrzeug Lautsprecher ist an. Der Headset Lautsprecher ist aus. Der Sound Modus automatisches Fahren ist aus.");
            //CarSpeakerOffHeadsetSpeakerOnSoundModeOn
            var audioFile47 = AddAudioFile(AudioName.CarSpeakerOffHeadsetSpeakerOnSoundModeOn, "Der Fahrzeug Lautsprecher ist aus. Der Headset Lautsprecher ist an. Der Sound Modus automatisches Fahren ist an.");
            //CarSpeakerOnHeadsetSpeakerOffSoundModeOn
            var audioFile48 = AddAudioFile(AudioName.CarSpeakerOnHeadsetSpeakerOffSoundModeOn, "Der Fahrzeug Lautsprecher ist an. Der Headset Lautsprecher ist aus. Der Sound Modus automatisches Fahren ist an.");
            //AllSpeakerOffSoundModeOff
            var audioFile49 = AddAudioFile(AudioName.AllSpeakerOffSoundModeOff, "Alle Lautsprecher sind aus. Der Sound Modus automatisches Fahren ist ebenfalls an.");
            //AllSpeakerOffSoundModeOn
            var audioFile50 = AddAudioFile(AudioName.AllSpeakerOffSoundModeOn, "Alle Lautsprecher sind aus. Der Sound Modus automatisches Fahren ist an.");
            //AllSpeakerOnSoundModeOff
            var audioFile51 = AddAudioFile(AudioName.AllSpeakerOnSoundModeOff, "Alle Lautsprecher sind an. Der Sound Modus automatisches Fahren ist aus.");
            //AllSpeakerOnSoundModeOn
            var audioFile52 = AddAudioFile(AudioName.AllSpeakerOnSoundModeOn, "Alle Lautsprecher sind an. Der Sound Modus automatisches Fahren ist ebenfalls an.");
            //CameraUp
            var audioFile53 = AddAudioFile(AudioName.CameraUp, "Kamera hoch");
            //CameraDown
            var audioFile54 = AddAudioFile(AudioName.CameraDown, "Kamera runter");
            //CameraLightUp
            var audioFile55 = AddAudioFile(AudioName.CameraLightUp, "Kamera leicht hoch");
            //CameraLightDown
            var audioFile56 = AddAudioFile(AudioName.CameraLightDown, "Kamera leicht runter");
            //TurnToLongLeft
            var audioFile57 = AddAudioFile(AudioName.TurnToLongLeft, "Mir ist schwindelig, dreh doch mal wieder nach rechts.");
            //TurnToLongRight
            var audioFile58 = AddAudioFile(AudioName.TurnToLongRight, "Mir ist schwindelig, dreh doch mal wieder nach links.");

            await Task.WhenAll(audioFile1, audioFile2, audioFile3, audioFile4, audioFile5, audioFile6, audioFile7, audioFile8, audioFile9, audioFile10,
                audioFile11, audioFile12, audioFile13, audioFile14, audioFile15, audioFile16, audioFile17, audioFile18, audioFile19, audioFile20,
                audioFile21, audioFile22, audioFile23, audioFile24, audioFile25, audioFile26, audioFile27, audioFile28, audioFile29, audioFile30,
                audioFile31, audioFile32, audioFile33, audioFile34, audioFile35, audioFile36, audioFile37, audioFile38, audioFile39, audioFile40,
                audioFile41, audioFile42, audioFile43, audioFile44, audioFile45, audioFile46, audioFile47, audioFile48, audioFile49, audioFile50,
                audioFile51, audioFile52, audioFile53, audioFile54, audioFile55, audioFile56, audioFile57, audioFile58);
        }
    }
}
