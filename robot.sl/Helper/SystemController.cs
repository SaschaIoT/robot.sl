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

        public static void InitializeAsync(AccelerometerGyroscopeSensor accelerometerSensor,
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

        public static async Task StopAllAsync()
        {
            if (!_initialized)
            {
                return;
            }

            var stopTask = Task.Run(async () =>
            {
                _httpServerController.Stop();
                await _camera.StopAsync();
                await _gamepadController.StopAsync();
                await _speechRecognation.StopAsync();
                await _automaticDrive.StopAsync(false);
                _servoController.Stop();
                _motorController.Stop();
                await _automaticSpeakController.StopAsync();
                await _accelerometerSensor.StopAsync();
                AudioPlayerController.Stop();
                await SpeedSensor.StopAsync();

                ShutdownMotorsServos();
            });

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));

            if (timeoutTask == await Task.WhenAny(stopTask, timeoutTask))
            {
                await Logger.WriteAsync($"{nameof(SystemController)}, {nameof(StopAllAsync)}: Could not completely stop application before closing.");
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

        public static async Task ShutdownAsync()
        {
            if (!_initialized)
            {
                return;
            }

            var stopAll = StopAllAsync();
            var shutdownSound = AudioPlayerController.PlayAndWaitAsync(AudioName.Shutdown);

            await Task.WhenAll(new[] { stopAll, shutdownSound });

            await DeviceController.ShutdownDeviceAsync();
        }

        public static async Task RestartAsync()
        {
            if (!_initialized)
            {
                return;
            }

            await StopAllAsync();
            await AudioPlayerController.PlayAndWaitAsync(AudioName.Restart);
            await DeviceController.RestartDeviceAsync();
        }

        public static async Task SetDefaultRenderDeviceVolumeAsync(int volume)
        {
            await AudioDeviceController.SetDefaultRenderDeviceVolumeAsync(volume);
        }

        public static async Task SetDefaultCaptureDeviceVolumeAsync(int volume)
        {
            await AudioDeviceController.SetDefaultCaptureDeviceVolumeAsync(volume);
        }

        public static async Task SetDefaultRenderDeviceAsync(string renderDeviceName)
        {
            await AudioDeviceController.SetDefaultRenderDeviceAsync(renderDeviceName);
        }

        public static async Task SetDefaultCaptureDeviceAsync(string captureDeviceName)
        {
            await AudioDeviceController.SetDefaultCaptureDeviceAsync(captureDeviceName);
        }

        public static async Task ShutdownApplicationAsync(bool unhandeledException)
        {
            await ShutdownApplicationAsync(unhandeledException, true);
        }

        public static async Task ShutdownApplicationAsync(bool unhandeledException, bool stopAll)
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
                if(stopAll)
                    await StopAllAsync();
            }
            catch (Exception) { }

            Application.Current.Exit();
        }
    }
}
