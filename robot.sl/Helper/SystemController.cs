using robot.sl.Audio;
using robot.sl.Audio.AudioPlaying;
using robot.sl.CarControl;
using robot.sl.Devices;
using robot.sl.Sensors;
using robot.sl.Web;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace robot.sl.Helper
{
    public static class SystemController
    {
        private const string SERVICE_USERNAME = "TechnicalServiceUser";
        private const string SERVICE_USER_PASSWORD = "stWiFIH23";

        //Dependency objects
        private static AccelerometerGyroscopeSensor _accelerometerSensor;
        private static AutomaticSpeakController _automaticSpeakController;
        private static MotorController _motorController;
        private static ServoController _servoController;
        private static AutomaticDrive _automaticDrive;
        private static Camera _camera;
        private static HttpServerController _httpServerController;
        private static SpeechRecognition _speechRecognation;
        private static GamepadController _gamepadController;

        private static bool _initialized = false;

        public static IFormatProvider Culture { get; private set; }

        public static void Initialize(AccelerometerGyroscopeSensor accelerometerSensor,
                                      AutomaticSpeakController automaticSpeakController,
                                      MotorController motorController,
                                      ServoController servoController,
                                      AutomaticDrive automaticDrive,
                                      Camera camera,
                                      HttpServerController httpServerController,
                                      SpeechRecognition speechRecognation,
                                      GamepadController gamepadController)
        {
            _accelerometerSensor = accelerometerSensor;
            _automaticSpeakController = automaticSpeakController;
            _motorController = motorController;
            _servoController = servoController;
            _automaticDrive = automaticDrive;
            _camera = camera;
            _httpServerController = httpServerController;
            _speechRecognation = speechRecognation;
            _gamepadController = gamepadController;

            _initialized = true;
        }

        public static async Task StopAll()
        {
            var stopTask = Task.Run(async () =>
            {
                ShutdownMotorsServos();

                _httpServerController.Stop();
                await _camera.Stop();
                await _gamepadController.Stop();
                await _speechRecognation.Stop();
                await _automaticDrive.Stop();
                _servoController.Stop();
                _motorController.Stop();
                await _automaticSpeakController.Stop();
                await _accelerometerSensor.Stop();
                AudioPlayerController.Stop();
                SpeedSensor.Stop();

                ShutdownMotorsServos();
            });

            var timeout = TimeSpan.FromSeconds(5);
            await TaskHelper.WithTimeoutAfterStart(ct => stopTask.WithCancellation(ct), timeout);
        }

        private static void ShutdownMotorsServos()
        {
            _servoController.PwmController.SetPwm(0, 0, 340);
            _servoController.PwmController.SetPwm(1, 0, 318);
            _servoController.PwmController.SetPwm(2, 0, 169);
            _servoController.PwmController.SetPwm(3, 0, 182);
            _servoController.PwmController.SetAllPwm(4096, 0);

            _motorController.GetMotor(1).Run(MotorAction.RELEASE);
            _motorController.GetMotor(2).Run(MotorAction.RELEASE);
            _motorController.GetMotor(3).Run(MotorAction.RELEASE);
            _motorController.GetMotor(4).Run(MotorAction.RELEASE);
        }

        public static async Task Shutdown()
        {
            if (!_initialized)
            {
                return;
            }

            await StopAll();
            await AudioPlayerController.PlayAndWaitAsync(AudioName.Shutdown);
            await ProcessLauncher.RunToCompletionAsync(@"CmdWrapper.exe", "\"shutdown -s -t 0\"");
        }

        public static async Task Restart()
        {
            if (!_initialized)
            {
                return;
            }

            await StopAll();
            await AudioPlayerController.PlayAndWaitAsync(AudioName.Restart);
            await ProcessLauncher.RunToCompletionAsync(@"CmdWrapper.exe", "\"shutdown -r -t 0\"");
        }

        public static async Task SetAudioRenderVolume(int volume, bool retryOnException)
        {
            var result = await ProcessLauncher.RunToCompletionAsync(@"SetAudioRenderVolume.exe", volume.ToString(CultureInfo.InvariantCulture));
            if (result.ExitCode != 200)
            {
                await Logger.Write("Could not set audio render volume.");
            }
        }

        public static async Task SetAudioCaptureVolume(double volume, bool retryOnException)
        {
            //96.14% db equals 90% volume with Logitech G933 Headset
            var result = await ProcessLauncher.RunToCompletionAsync(@"SetAudioCaptureVolume.exe", volume.ToString(CultureInfo.InvariantCulture));
            if (result.ExitCode != 200)
            {
                await Logger.Write("Could not set audio capture volume.");
            }
        }

        public static async Task ShutdownApplication(bool unhandeledException)
        {
            if (unhandeledException)
            {
                try
                {
                    await AudioPlayerController.PlayAndWaitAsync(AudioName.AppError);
                }
                catch (Exception) { }
            }

            try
            {
                await StopAll();
            }
            catch (Exception) { }

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Application.Current.Exit();
                });
        }
    }
}
