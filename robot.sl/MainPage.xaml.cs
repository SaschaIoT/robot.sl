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
        private DistanceSensorLaser _distanceSensorLaserTop;
        private DistanceSensorLaser _distanceSensorLaserMiddleTop;
        private DistanceSensorLaser _distanceSensorLaserMiddleBottom;
        private DistanceSensorLaser _distanceSensorLaserBottom;
        private Multiplexer _multiplexer;
        private Dance _dance;

        private const int HEADSET_AUDIO_RENDER_VOLUME = 70;
        private const int SPEAKER_AUDIO_RENDER_VOLUME = 80;
        private const int HEADSET_AUDIO_CAPTURE_VOLUME = 50;
                
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
                _multiplexer = new Multiplexer();
                await _multiplexer.InitializeAsync();
                
                _distanceSensorLaserTop = new DistanceSensorLaser(_multiplexer, MultiplexerDevice.DistanceLaserSensorTop, LightResponse.HIGH);
                await _distanceSensorLaserTop.InitializeAsync();

                _distanceSensorLaserMiddleTop = new DistanceSensorLaser(_multiplexer, MultiplexerDevice.DistanceLaserSensorMiddleTop, LightResponse.HIGH);
                await _distanceSensorLaserMiddleTop.InitializeAsync();

                _distanceSensorLaserMiddleBottom = new DistanceSensorLaser(_multiplexer, MultiplexerDevice.DistanceLaserSensorMiddleBottom, LightResponse.HIGH);
                await _distanceSensorLaserMiddleBottom.InitializeAsync();

                _distanceSensorLaserBottom = new DistanceSensorLaser(_multiplexer, MultiplexerDevice.DistanceLaserSensorBottom, LightResponse.HIGH);
                await _distanceSensorLaserBottom.InitializeAsync();
                
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
                _dance = new Dance(_motorController);

                _servoController = new ServoController();
                await _servoController.InitializeAsync();

                _distanceSensorUltrasonic = new DistanceSensorUltrasonic();
                await _distanceSensorUltrasonic.InitializeAsync();

                _automaticDrive = new AutomaticDrive(_motorController, _servoController, _distanceSensorUltrasonic, _distanceSensorLaserTop, _distanceSensorLaserMiddleTop, _distanceSensorLaserMiddleBottom, _distanceSensorLaserBottom);

                _speechRecognation = new SpeechRecognition();
                await _speechRecognation.InitialzeAsync(_motorController, _servoController, _automaticDrive, _dance);

                await _motorController.Initialize(_automaticSpeakController, _automaticDrive, _dance, _speechRecognation);

                _camera.Start();

                _httpServerController = new HttpServerController(_motorController, _servoController, _automaticDrive, _camera, _dance);

                _gamepadController = new GamepadController(_motorController, _servoController, _automaticDrive, _accelerometerSensor, _dance);

                _speechRecognation.Start();

                SystemController.InitializeAsync(_accelerometerSensor, _automaticSpeakController, _motorController, _servoController, _automaticDrive, _camera, _httpServerController, _speechRecognation, _gamepadController, _dance);

                await AudioPlayerController.PlayAndWaitAsync(AudioName.Welcome);

                _automaticSpeakController.Start();
            }
            catch (Exception exception)
            {
                await Logger.WriteAsync($"{nameof(MainPage)}, {nameof(InitialzeAsync)}: ", exception);
                await SystemController.ShutdownApplicationAsync(true);
            }
        }
    }
}
