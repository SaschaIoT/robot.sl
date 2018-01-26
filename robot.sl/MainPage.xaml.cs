using robot.sl.Audio;
using robot.sl.Audio.AudioPlaying;
using robot.sl.CarControl;
using robot.sl.Devices;
using robot.sl.Helper;
using robot.sl.Sensors;
using robot.sl.Web;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace robot.sl
{
    public sealed partial class MainPage : Page
    {
        private AccelerometerGyroscopeSensor _accelerometerSensor;
        private AutomaticSpeakController _automaticSpeakController;
        private MotorController _motorController;
        private ServoController _servoController;
        private DistanceSensorUltrasonic _distanceSensorUltrasonic;
        private AutomaticDrive _automaticDrive;
        private Camera _camera;
        private HttpServerController _httpServerController;
        private GamepadController _gamepadController;
        private SpeechRecognition _speechRecognation;
        private DistanceSensorLaser _distanceSensorLaserDown;
        private DistanceSensorLaser _distanceSensorLaserUp;

        private const int HEADSET_AUDIO_RENDER_VOLUME = 70;
        private const int SPEAKER_AUDIO_RENDER_VOLUME = 80;
        private const int HEADSET_AUDIO_CAPTURE_VOLUME = 50;

        private const int DISTANCE_SENSOR_LASER_UP_SHDN_PIN = 2;
        private const int DISTANCE_SENSOR_LASER_DEFAULT_DEVICE_ADDRESS = 0x29;
        private const int DISTANCE_SENSOR_LASER_DOWN_DEVICE_ADDRESS = 0x30;

        public MainPage()
        {
            InitializeComponent();
            
            Loaded += PageLoaded;
        }

        private async void PageLoaded(object sender, RoutedEventArgs eventArgs)
        {
            await InitialzeAsync();
        }

        private async Task InitialzeAsync()
        {
            try
            {
                _distanceSensorLaserUp = new DistanceSensorLaser();
                await _distanceSensorLaserUp.SetDevicePowerAsync(false, DISTANCE_SENSOR_LASER_UP_SHDN_PIN);

                _distanceSensorLaserDown = new DistanceSensorLaser();
                await _distanceSensorLaserDown.InitializeAsync(DISTANCE_SENSOR_LASER_DEFAULT_DEVICE_ADDRESS, DISTANCE_SENSOR_LASER_DOWN_DEVICE_ADDRESS);
                _distanceSensorLaserDown.SetDeviceAddress(DISTANCE_SENSOR_LASER_DOWN_DEVICE_ADDRESS);
                await _distanceSensorLaserDown.InitializeAsync(DISTANCE_SENSOR_LASER_DOWN_DEVICE_ADDRESS);
                _distanceSensorLaserDown.Configure();

                await _distanceSensorLaserUp.SetDevicePowerAsync(true, DISTANCE_SENSOR_LASER_UP_SHDN_PIN);
                await _distanceSensorLaserUp.InitializeAsync();
                _distanceSensorLaserUp.Configure();
                
                await SystemController.SetDefaultRenderDeviceAsync(DeviceNameHelper.SpeakerRenderDevice);
                await SystemController.SetDefaultRenderDeviceVolumeAsync(SPEAKER_AUDIO_RENDER_VOLUME);

                await SystemController.SetDefaultRenderDeviceAsync(DeviceNameHelper.HeadsetRenderDevice);
                await SystemController.SetDefaultRenderDeviceVolumeAsync(HEADSET_AUDIO_RENDER_VOLUME);

                await SystemController.SetDefaultCaptureDeviceAsync(DeviceNameHelper.HeadsetCaptureDevice);
                await SystemController.SetDefaultCaptureDeviceVolumeAsync(HEADSET_AUDIO_CAPTURE_VOLUME);

                _camera = new Camera();
                await _camera.InitializeAsync();

                SpeedSensor.Initialize();
                SpeedSensor.Start();

                SpeechSynthesis.Initialze();

                await AudioPlayerController.InitializeAsync();

                _accelerometerSensor = new AccelerometerGyroscopeSensor();
                await _accelerometerSensor.InitializeAsync();
                _accelerometerSensor.Start();

                _automaticSpeakController = new AutomaticSpeakController(_accelerometerSensor);

                _motorController = new MotorController();
                await _motorController.Initialize(_automaticSpeakController);

                _servoController = new ServoController();
                await _servoController.InitializeAsync();

                _distanceSensorUltrasonic = new DistanceSensorUltrasonic();
                await _distanceSensorUltrasonic.InitializeAsync();

                _automaticDrive = new AutomaticDrive(_motorController, _servoController, _distanceSensorUltrasonic, _distanceSensorLaserUp, _distanceSensorLaserDown);

                _speechRecognation = new SpeechRecognition();
                await _speechRecognation.InitialzeAsync(_motorController, _servoController, _automaticDrive);
                _speechRecognation.Start();

                _gamepadController = new GamepadController(_motorController, _servoController, _automaticDrive, _accelerometerSensor);

                _camera.Start();

                _httpServerController = new HttpServerController(_motorController, _servoController, _automaticDrive, _camera);

                SystemController.InitializeAsync(_accelerometerSensor, _automaticSpeakController, _motorController, _servoController, _automaticDrive, _camera, _httpServerController, _speechRecognation, _gamepadController);

                await AudioPlayerController.PlayAndWaitAsync(AudioName.Welcome);

                _automaticSpeakController.Start();
            }
            catch (Exception exception)
            {
                await Logger.WriteAsync($"{nameof(MainPage)}, {nameof(InitialzeAsync)}: ", exception);

                await Task.Delay(TimeSpan.FromSeconds(20));
                DeviceController.RestartDevice();
            }
        }
    }
}
