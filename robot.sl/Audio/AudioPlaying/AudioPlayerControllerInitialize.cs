using robot.sl.Helper;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace robot.sl.Audio.AudioPlaying
{
    public static partial class AudioPlayerController
    {
        private static async Task AddAudioFileAsync(AudioName audioName, string text)
        {
            IStorageFile storageFile = null;
            var soundFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(EnumHelper.GetName(audioName));
            if (soundFile == null)
            {
                using (var headsetStream = await SpeechSynthesis.SpeakAsStreamAsync(text))
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

        public static async Task InitializeAsync()
        {
            CarSpeakerOn = true;
            HeadsetSpeakerOn = true;
            SoundModeOn = false;

            _headsetSpeaker = new AudioPlayer();
            await _headsetSpeaker.InitializeAsync(true);

            _carSpeaker = new AudioPlayer();
            await _carSpeaker.InitializeAsync(false);
            
            //Each speaker must have its own stream and storage file. The stream must not be copied either
            //otherwise there will be a ViolationAccess exception.

            //Welcome
            await AddAudioFileAsync(AudioName.Welcome, "Hallo Freunde, ich bin bereit zum Spielen!");
            //Forward
            await AddAudioFileAsync(AudioName.Forward, "Vor");
            //Backward
            await AddAudioFileAsync(AudioName.Backward, "Zurück");
            //Left
            await AddAudioFileAsync(AudioName.Left, "Links");
            //Right
            await AddAudioFileAsync(AudioName.Right, "Rechts");
            //Stop
            await AddAudioFileAsync(AudioName.Stop, "Stop");
            //Turn
            await AddAudioFileAsync(AudioName.Turn, "Wenden");
            //SlightlyLeft
            await AddAudioFileAsync(AudioName.SlightlyLeft, "Leicht Links");
            //SlightlyRight
            await AddAudioFileAsync(AudioName.SlightlyRight, "Leicht Rechts");
            //VerySlightlyLeft
            await AddAudioFileAsync(AudioName.VerySlightlyLeft, "Ganz leicht Links");
            //VerySlightlyRight
            await AddAudioFileAsync(AudioName.VerySlightlyRight, "Ganz leicht Rechts");
            //Slow
            await AddAudioFileAsync(AudioName.Slow, "Langsam");
            //Normal
            await AddAudioFileAsync(AudioName.Normal, "Normal");
            //Fast
            await AddAudioFileAsync(AudioName.Fast, "Schnell");
            //DanceOn
            await AddAudioFileAsync(AudioName.DanceOn, "Jetzt tanze ich.");
            //DanceOff
            await AddAudioFileAsync(AudioName.DanceOff, "Jetzt tanze ich nicht mehr.");
            //DanceOnAlready
            await AddAudioFileAsync(AudioName.DanceOnAlready, "Ich tanze bereits.");
            //DanceOffAlready
            await AddAudioFileAsync(AudioName.DanceOffAlready, "Tanzen ist bereits deaktiviert.");
            //CliffSensorOn
            await AddAudioFileAsync(AudioName.CliffSensorOn, "Der Klippensensor wurde aktiviert.");
            //CliffSensorOff
            await AddAudioFileAsync(AudioName.CliffSensorOff, "Der Klippensensor wurde deaktiviert.");
            //CliffSensorAlreadyOn
            await AddAudioFileAsync(AudioName.CliffSensorAlreadyOn, "Der Klippensensor ist bereits aktiviert.");
            //CliffSensorAlreadyOff
            await AddAudioFileAsync(AudioName.CliffSensorAlreadyOff, "Der Klippensensor ist bereits deaktiviert.");

            var steuerungsbefehle = "Folgende Steuerungsbefehle sind möglich: vor, zurück, links, rechts, stop, wenden, leicht links, leicht rechts, ganz leicht links, ganz leicht rechts, langsam, normal, schnell, Aktiviere Tanzen, Deaktiviere Tanzen, Kamera hoch, Kamera runter, Kamera leicht hoch, Kamera leicht runter, Aktiviere automatisches Fahren und deaktiviere automatisches Fahren, ";
            var systembefehle = "Folgende Systembefehle sind möglich: Befehl, Steuerungsbefehle, Systembefehle, aktiviere Lautsprecher, deaktiviere Lautsprecher, aktiviere Headset Lautsprecher, deaktiviere Headset Lautsprecher, aktiviere Fahrzeug Lautsprecher, deaktiviere Fahrzeug Lautsprecher, aktiviere Sound Modus, deaktiviere Sound Modus, aktiviere Klippensensor und deaktiviere Klippensensor";
            //Commands
            await AddAudioFileAsync(AudioName.Commands, steuerungsbefehle + systembefehle);
            //ControlCommands
            await AddAudioFileAsync(AudioName.ControlCommands, steuerungsbefehle);
            //SystemCommands
            await AddAudioFileAsync(AudioName.SystemCommands, systembefehle);
            //StrongVibration
            await AddAudioFileAsync(AudioName.StrongVibration, "Aufpassen, ich bin nicht unzerstörbar.");
            //Stand
            await AddAudioFileAsync(AudioName.Stand, "Mir ist langweilig, fahre mich doch ein bisschen herum.");
            //AutomatischesFahrenFesthaengen
            await AddAudioFileAsync(AudioName.AutomatischesFahrenFesthaengen, "Ich stecke fest, ein Moment ich versuche mich zu befreien.");
            //Shutdown
            await AddAudioFileAsync(AudioName.Shutdown, "Bis bald, ich schalte mich nun aus.");
            //Reboot
            await AddAudioFileAsync(AudioName.Restart, "Bis gleich, ich starte mich neu.");
            //AutomaticDriveOnAlready
            await AddAudioFileAsync(AudioName.AutomaticDriveOnAlready, "Automatisches Fahren ist bereits aktiviert.");
            //AutomaticDriveOffAlready
            await AddAudioFileAsync(AudioName.AutomaticDriveOffAlready, "Automatisches Fahren ist nicht aktiviert.");
            //AutomaticDriveOn
            await AddAudioFileAsync(AudioName.AutomaticDriveOn, "Jetzt übernehme ich die Steuerung.");
            //AutomaticDriveOff
            await AddAudioFileAsync(AudioName.AutomaticDriveOff, "Jetzt kannst Du mich wieder Steuern.");
            //ReallyRestart
            await AddAudioFileAsync(AudioName.ReallyRestart, "Möchtest Du mich wirklich neustarten?");
            //ReallyShutdown
            await AddAudioFileAsync(AudioName.ReallyShutdown, "Möchtest Du mich wirklich ausschalten?");
            //GamepadVibrationOn
            await AddAudioFileAsync(AudioName.GamepadVibrationOn, "Die Gamepad Vibration ist nun aktiv.");
            //GamepadVibrationOff
            await AddAudioFileAsync(AudioName.GamepadVibrationOff, "Die Gamepad Vibration wurde deaktivirt.");
            //AppError
            await AddAudioFileAsync(AudioName.AppError, "Ich bin abgestürzt, ich stehe gleich wieder zur Verfügung.");
            //SoundModeAlreadyOn
            await AddAudioFileAsync(AudioName.SoundModeAlreadyOn, "Der Sound Modus automatisches Fahren ist bereits an.");
            //HeadsetSpeakerOff
            await AddAudioFileAsync(AudioName.HeadsetSpeakerOff, "Der Headset Lautsprecher ist jetzt aus.");
            //HeadsetSpeakerAlreadyOff
            await AddAudioFileAsync(AudioName.HeadsetSpeakerAlreadyOff, "Der Headset Lautsprecher ist bereits aus.");
            //HeadsetSpeakerOn
            await AddAudioFileAsync(AudioName.HeadsetSpeakerOn, "Der Headset Lautsprecher ist jetzt an.");
            //HeadsetSpeakerAlreadyOn
            await AddAudioFileAsync(AudioName.HeadsetSpeakerAlreadyOn, "Der Headset Lautsprecher ist bereits an.");
            //CarSpeakerOff
            await AddAudioFileAsync(AudioName.CarSpeakerOff, "Der Fahrzeug Lautsprecher ist jetzt aus.");
            //CarSpeakerAlreadyOff
            await AddAudioFileAsync(AudioName.CarSpeakerAlreadyOff, "Der Fahrzeug Lautsprecher ist bereits aus.");
            //CarSpeakerOn
            await AddAudioFileAsync(AudioName.CarSpeakerOn, "Der Fahrzeug Lautsprecher ist jetzt an.");
            //CarSpeakerAlreadyOn
            await AddAudioFileAsync(AudioName.CarSpeakerAlreadyOn, "Der Fahrzeug Lautsprecher ist bereits an.");
            //AllSpeakerOff
            await AddAudioFileAsync(AudioName.AllSpeakerOff, "Alle Lautsprecher sind jetzt aus.");
            //AllSpeakerAlreadyOff
            await AddAudioFileAsync(AudioName.AllSpeakerAlreadyOff, "Alle Lautsprecher sind bereits aus.");
            //AllSpeakerOn
            await AddAudioFileAsync(AudioName.AllSpeakerOn, "Alle Lautsprecher sind jetzt an.");
            //AllSpeakerAlreadyOn
            await AddAudioFileAsync(AudioName.AllSpeakerAlreadyOn, "Alle Lautsprecher sind bereits an.");
            //SoundModeOn
            await AddAudioFileAsync(AudioName.SoundModeOn, "Der Sound Modus automatisches Fahren ist jetzt an.");
            //SoundModusAlreadyOff
            await AddAudioFileAsync(AudioName.SoundModusAlreadyOff, "Der Sound Modus automatisches Fahren ist bereits aus.");
            //SoundModusOff
            await AddAudioFileAsync(AudioName.SoundModusOff, "Der Sound Modus automatisches Fahren ist jetzt aus.");
            //CarSpeakerOffHeadsetSpeakerOnSoundModeOff
            await AddAudioFileAsync(AudioName.CarSpeakerOffHeadsetSpeakerOnSoundModeOff, "Der Fahrzeug Lautsprecher ist aus. Der Headset Lautsprecher ist an. Der Sound Modus automatisches Fahren ist aus.");
            //CarSpeakerOnHeadsetSpeakerOffSoundModeOff
            await AddAudioFileAsync(AudioName.CarSpeakerOnHeadsetSpeakerOffSoundModeOff, "Der Fahrzeug Lautsprecher ist an. Der Headset Lautsprecher ist aus. Der Sound Modus automatisches Fahren ist aus.");
            //CarSpeakerOffHeadsetSpeakerOnSoundModeOn
            await AddAudioFileAsync(AudioName.CarSpeakerOffHeadsetSpeakerOnSoundModeOn, "Der Fahrzeug Lautsprecher ist aus. Der Headset Lautsprecher ist an. Der Sound Modus automatisches Fahren ist an.");
            //CarSpeakerOnHeadsetSpeakerOffSoundModeOn
            await AddAudioFileAsync(AudioName.CarSpeakerOnHeadsetSpeakerOffSoundModeOn, "Der Fahrzeug Lautsprecher ist an. Der Headset Lautsprecher ist aus. Der Sound Modus automatisches Fahren ist an.");
            //AllSpeakerOffSoundModeOff
            await AddAudioFileAsync(AudioName.AllSpeakerOffSoundModeOff, "Alle Lautsprecher sind aus. Der Sound Modus automatisches Fahren ist ebenfalls an.");
            //AllSpeakerOffSoundModeOn
            await AddAudioFileAsync(AudioName.AllSpeakerOffSoundModeOn, "Alle Lautsprecher sind aus. Der Sound Modus automatisches Fahren ist an.");
            //AllSpeakerOnSoundModeOff
            await AddAudioFileAsync(AudioName.AllSpeakerOnSoundModeOff, "Alle Lautsprecher sind an. Der Sound Modus automatisches Fahren ist aus.");
            //AllSpeakerOnSoundModeOn
            await AddAudioFileAsync(AudioName.AllSpeakerOnSoundModeOn, "Alle Lautsprecher sind an. Der Sound Modus automatisches Fahren ist ebenfalls an.");
            //CameraUp
            await AddAudioFileAsync(AudioName.CameraUp, "Kamera hoch");
            //CameraDown
            await AddAudioFileAsync(AudioName.CameraDown, "Kamera runter");
            //CameraSlightlyUp
            await AddAudioFileAsync(AudioName.CameraSlightlyUp, "Kamera leicht hoch");
            //CameraSlightlyDown
            await AddAudioFileAsync(AudioName.CameraSlightlyDown, "Kamera leicht runter");
            //TurnToLongLeft
            await AddAudioFileAsync(AudioName.TurnToLongLeft, "Mir ist schwindelig, dreh doch mal wieder nach rechts.");
            //TurnToLongRight
            await AddAudioFileAsync(AudioName.TurnToLongRight, "Mir ist schwindelig, dreh doch mal wieder nach links.");
        }
    }
}
