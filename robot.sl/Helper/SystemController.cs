using robot.sl.Audio;
using robot.sl.Audio.AudioPlaying;
using robot.sl.CarControl;
using robot.sl.Devices;
using robot.sl.Sensors;
using robot.sl.Web;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace robot.sl.Helper
{
    public static class SystemController
    {
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
            if (!_initialized)
            {
                return;
            }

            var stopTask = Task.Run(async () =>
            {
                _httpServerController.Stop();
                await _camera.Stop();
                await _gamepadController.Stop();
                await _speechRecognation.Stop();
                await _automaticDrive.Stop(false);
                _servoController.Stop();
                _motorController.Stop();
                await _automaticSpeakController.Stop();
                await _accelerometerSensor.Stop();
                AudioPlayerController.Stop();
                await SpeedSensor.Stop();

                ShutdownMotorsServos();
            });

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));

            if (timeoutTask == await Task.WhenAny(stopTask, timeoutTask))
            {
                await Logger.Write($"{nameof(SystemController)}, {nameof(StopAll)}: Could not completely stop application before closing.");
            }
        }

        private static void ShutdownMotorsServos()
        {
            _servoController.PwmController.SetPwm(Servo.CameraHorizontal, 0, ServoPositions.CameraHorizontalMiddle);
            _servoController.PwmController.SetPwm(Servo.CameraVertical, 0, ServoPositions.CameraVerticalMiddle);
            _servoController.PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalLeft);
            _servoController.PwmController.SetPwm(Servo.DistanceSensorVertical, 0, ServoPositions.DistanceSensorVerticalTop);

            Task.Delay(2500).Wait();

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

            var stopAll = StopAll();
            var shutdownSound = AudioPlayerController.PlayAndWaitAsync(AudioName.Shutdown);

            await Task.WhenAll(new[] { stopAll, shutdownSound });

            await DeviceController.ShutdownDevice();
        }

        public static async Task Restart()
        {
            if (!_initialized)
            {
                return;
            }

            await StopAll();
            await AudioPlayerController.PlayAndWaitAsync(AudioName.Restart);
            await DeviceController.RestartDevice();
        }

        public static async Task SetDefaultRenderDeviceVolume(int volume)
        {
            await AudioDeviceController.SetDefaultRenderDeviceVolume(volume);
        }

        public static async Task SetDefaultCaptureDeviceVolume(int volume)
        {
            await AudioDeviceController.SetDefaultCaptureDeviceVolume(volume);
        }

        public static async Task SetDefaultRenderDevice(string renderDeviceName)
        {
            await AudioDeviceController.SetDefaultRenderDevice(renderDeviceName);
        }

        public static async Task SetDefaultCaptureDevice(string captureDeviceName)
        {
            await AudioDeviceController.SetDefaultCaptureDevice(captureDeviceName);
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

            Application.Current.Exit();
        }
    }
}
