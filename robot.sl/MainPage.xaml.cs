﻿using robot.sl.Audio;
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
        private DistanceMeasurementSensor _distanceMeasurementSensor;
        private AutomaticDrive _automaticDrive;
        private Camera _camera;
        private HttpServerController _httpServerController;
        private GamepadController _gamepadController;
        private SpeechRecognition _speechRecognation;

        private const int I2C_ADDRESS_SERVO = 56;

        private const int HEADSET_AUDIO_RENDER_VOLUME = 70;
        private const int SPEAKER_AUDIO_RENDER_VOLUME = 70;
        private const int HEADSET_AUDIO_CAPTURE_VOLUME = 50;

        public MainPage()
        {
            InitializeComponent();
            
            Loaded += PageLoaded;
        }

        private async void PageLoaded(object sender, RoutedEventArgs eventArgs)
        {
            await Initialze();
        }

        private async Task Initialze()
        {
            try
            {
                await SystemController.SetDefaultRenderDevice(DeviceNameHelper.SpeakerRenderDevice);
                await SystemController.SetDefaultRenderDeviceVolume(SPEAKER_AUDIO_RENDER_VOLUME);

                await SystemController.SetDefaultRenderDevice(DeviceNameHelper.HeadsetRenderDevice);
                await SystemController.SetDefaultRenderDeviceVolume(HEADSET_AUDIO_RENDER_VOLUME);

                await SystemController.SetDefaultCaptureDevice(DeviceNameHelper.HeadsetCaptureDevice);
                await SystemController.SetDefaultCaptureDeviceVolume(HEADSET_AUDIO_CAPTURE_VOLUME);

                _camera = new Camera();
                await _camera.Initialize();

                SpeedSensor.Initialize();
                SpeedSensor.Start();

                SpeechSynthesis.Initialze();

                await AudioPlayerController.Initialize();

                _accelerometerSensor = new AccelerometerGyroscopeSensor();
                await _accelerometerSensor.Initialize();
                _accelerometerSensor.Start();

                _automaticSpeakController = new AutomaticSpeakController(_accelerometerSensor);

                _motorController = new MotorController();
                await _motorController.Initialize(_automaticSpeakController);

                _servoController = new ServoController();
                await _servoController.Initialize();

                _distanceMeasurementSensor = new DistanceMeasurementSensor();
                await _distanceMeasurementSensor.Initialize(I2C_ADDRESS_SERVO);

                _automaticDrive = new AutomaticDrive(_motorController, _servoController, _distanceMeasurementSensor);

                _speechRecognation = new SpeechRecognition();
                await _speechRecognation.Initialze(_motorController, _servoController, _automaticDrive);
                _speechRecognation.Start();

                _gamepadController = new GamepadController(_motorController, _servoController, _automaticDrive, _accelerometerSensor);

                _camera.Start();

                _httpServerController = new HttpServerController(_motorController, _servoController, _automaticDrive, _camera);

                SystemController.Initialize(_accelerometerSensor, _automaticSpeakController, _motorController, _servoController, _automaticDrive, _camera, _httpServerController, _speechRecognation, _gamepadController);

                await AudioPlayerController.PlayAndWaitAsync(AudioName.Welcome);

                _automaticSpeakController.Start();
            }
            catch (Exception exception)
            {
                await Logger.Write($"{nameof(MainPage)}, {nameof(Initialze)}: ", exception);

                await Task.Delay(TimeSpan.FromSeconds(20));
                await DeviceController.RestartDevice();
            }
        }
    }
}
