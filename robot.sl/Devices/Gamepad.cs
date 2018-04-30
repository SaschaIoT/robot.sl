using robot.sl.Audio.AudioPlaying;
using robot.sl.CarControl;
using robot.sl.Helper;
using robot.sl.Sensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Gaming.Input;

namespace robot.sl.Devices
{
    /// <summary>
    /// Microsoft Xbox One Controller
    /// </summary>
    public class GamepadController
    {
        private Gamepad _gamepad;
        private bool _motorStopped = true;
        private bool _servoStopped = true;
        private bool _gamepadShouldVibrate = true;
        private volatile bool _isGamepadReadingStopped = true;
        private volatile bool _isGamepadVibrationStopped = true;
        private volatile bool _shutdown = false;
        private volatile bool _isGamepadVibrationShutdown = true;

        //Dependencies
        private MotorController _motorController;
        private ServoController _servoController;
        private AccelerometerGyroscopeSensor _acceleratorSensor;
        private AutomaticDrive _automaticDrive;
        private Dance _dance;

        public async Task StopAsync()
        {
            _gamepad = null;

            _shutdown = true;

            while (!_isGamepadReadingStopped || !_isGamepadVibrationStopped)
            {
                await Task.Delay(10);
            }
        }

        public GamepadController(MotorController motorController,
                                 ServoController servoController,
                                 AutomaticDrive automaticDrive,
                                 AccelerometerGyroscopeSensor acceleratorSensor,
                                 Dance dance)
        {
            if (_shutdown)
            {
                return;
            }

            _motorController = motorController;
            _servoController = servoController;
            _automaticDrive = automaticDrive;
            _acceleratorSensor = acceleratorSensor;
            _dance = dance;

            Gamepad.GamepadAdded += GamepadAdded;
            Gamepad.GamepadRemoved += GamepadRemoved;
        }

        private void GamepadAdded(object sender, Gamepad gamepad)
        {
            _gamepad = gamepad;

            Task.Factory.StartNew(() =>
            {

                StartGamepadReadingAsync(gamepad).Wait();

            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
            .AsAsyncAction()
             .AsTask()
             .ContinueWith((t) =>
             {

                 Logger.WriteAsync($"{nameof(Gamepad)}, {nameof(StartGamepadReadingAsync)}: ", t.Exception).Wait();
                 SystemController.ShutdownApplicationAsync(true).Wait();

             }, TaskContinuationOptions.OnlyOnFaulted);

            Task.Factory.StartNew(() =>
            {

                StartGamepadVibrationAsync(gamepad).Wait();

            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
             .AsAsyncAction()
             .AsTask()
             .ContinueWith((t) =>
             {

                 Logger.WriteAsync($"{nameof(Gamepad)}, {nameof(StartGamepadVibrationAsync)}: ", t.Exception).Wait();
                 SystemController.ShutdownApplicationAsync(true).Wait();

             }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async void GamepadRemoved(object sender, Gamepad gamepad)
        {
            _gamepad = null;

            while (!_isGamepadReadingStopped || !_isGamepadVibrationStopped)
            {
                await Task.Delay(10);
            }

            _motorController.GetMotor(1).Run(MotorAction.RELEASE);
            _motorController.GetMotor(2).Run(MotorAction.RELEASE);
            _motorController.GetMotor(3).Run(MotorAction.RELEASE);
            _motorController.GetMotor(4).Run(MotorAction.RELEASE);
        }

        private async Task StartGamepadReadingAsync(Gamepad gamepad)
        {
            _isGamepadReadingStopped = false;

            var buttonDownTimeMiddle = TimeSpan.FromSeconds(2);
            var buttonDownTimeLong = TimeSpan.FromSeconds(5);
            var viewButton = new GamepadButtonDown(TimeSpan.Zero, GamepadButtons.View);
            var menuButton = new GamepadButtonDown(TimeSpan.Zero, GamepadButtons.Menu);
            var dPadUpButton = new GamepadButtonDown(buttonDownTimeMiddle, GamepadButtons.DPadUp);
            var dPadDownButton = new GamepadButtonDown(buttonDownTimeMiddle, GamepadButtons.DPadDown);
            var dPadLeftButton = new GamepadButtonDown(buttonDownTimeMiddle, GamepadButtons.DPadLeft);
            var dPadRightButton = new GamepadButtonDown(buttonDownTimeMiddle, GamepadButtons.DPadRight);
            var yButton = new GamepadButtonDown(buttonDownTimeMiddle, GamepadButtons.Y);
            var bButton = new GamepadButtonDown(buttonDownTimeMiddle, GamepadButtons.B);
            var aButton = new GamepadButtonDown(buttonDownTimeMiddle, GamepadButtons.A);
            var xButton = new GamepadButtonDown(buttonDownTimeMiddle, GamepadButtons.X);
            var xRightShoulderButton = new GamepadButtonDown(buttonDownTimeLong, GamepadButtons.X, GamepadButtons.RightShoulder);
            var aRightShoulderButton = new GamepadButtonDown(buttonDownTimeLong, GamepadButtons.A, GamepadButtons.RightShoulder);

            var xRightShoulderButtonNotClickableButtons = new List<GamepadButtons>
                    {
                        GamepadButtons.Y
                    };

            var aRightShoulderButtonNotClickableButtons = new List<GamepadButtons>
                    {
                        GamepadButtons.X
                    };

            var dPadUpNotClickableButtons = new List<GamepadButtons>
                    {
                        GamepadButtons.DPadDown,
                        GamepadButtons.DPadLeft,
                        GamepadButtons.DPadRight
                    };
            var dPadDownNotClickableButtons = new List<GamepadButtons>
                    {
                        GamepadButtons.DPadUp,
                        GamepadButtons.DPadLeft,
                        GamepadButtons.DPadRight
                    };
            var dPadLeftNotClickableButtons = new List<GamepadButtons>
                    {
                        GamepadButtons.DPadUp,
                        GamepadButtons.DPadDown,
                        GamepadButtons.DPadRight
                    };
            var dPadRightNotClickableButtons = new List<GamepadButtons>
                    {
                        GamepadButtons.DPadUp,
                        GamepadButtons.DPadDown,
                        GamepadButtons.DPadLeft
                    };

            while (_gamepad == gamepad)
            {
                //gamepad variable could be null
                var gamepadReadingTry = gamepad?.GetCurrentReading();
                if (!gamepadReadingTry.HasValue)
                    break;

                var gamepadReading = gamepadReadingTry.Value;

                var motorCarMoveCommand = new CarMoveCommand(gamepadReading);
                var servoCarControlCommand = new CarControlCommand(gamepadReading);

                // Motor
                if ((_motorStopped && motorCarMoveCommand.Speed == 0.0) == false)
                {
                    _motorStopped = false;

                    await _motorController.MoveCarAsync(motorCarMoveCommand, MotorCommandSource.Other);
                }
                
                if (motorCarMoveCommand.Speed == 0.0)
                {
                    _motorStopped = true;
                }

                // Servo
                if ((_servoStopped
                    && servoCarControlCommand.DirectionControlUp == false
                    && servoCarControlCommand.DirectionControlDown == false) == false)
                {
                    _servoStopped = false;
                    
                    await _servoController.MoveServo(servoCarControlCommand);
                }

                // Servo not moving
                if (servoCarControlCommand.DirectionControlUp == false
                    && servoCarControlCommand.DirectionControlDown == false)
                {
                    _servoStopped = true;
                }

                // Enable/ disable gamepad vibration
                var viewButtonResult = viewButton.UpdateGamepadButtonState(gamepadReading);
                if (viewButtonResult.ButtonClicked)
                {
                    _gamepadShouldVibrate = !_gamepadShouldVibrate;

                    if (_gamepadShouldVibrate)
                    {
                        await AudioPlayerController.PlayAsync(AudioName.GamepadVibrationOn);
                    }
                    else
                    {
                        await AudioPlayerController.PlayAsync(AudioName.GamepadVibrationOff);
                    }
                }

                //Automatic drive toggle
                var menuButtonResult = menuButton.UpdateGamepadButtonState(gamepadReading);
                if (menuButtonResult.ButtonClicked)
                {
                    await _automaticDrive.StartStopToggleAsync();
                }

                var dPadUpButtonResult = dPadUpButton.UpdateGamepadButtonState(gamepadReading, dPadUpNotClickableButtons);
                var dPadDownButtonResult = dPadDownButton.UpdateGamepadButtonState(gamepadReading, dPadDownNotClickableButtons);
                var dPadLeftButtonResult = dPadLeftButton.UpdateGamepadButtonState(gamepadReading, dPadLeftNotClickableButtons);
                var dPadRightButtonResult = dPadRightButton.UpdateGamepadButtonState(gamepadReading, dPadRightNotClickableButtons);

                //All speaker on
                if (dPadUpButtonResult.ButtonClicked)
                {
                    await AudioPlayerController.SetAllSpeakerOnOffAsync(true);
                }

                //All speaker off
                if (dPadDownButtonResult.ButtonClicked)
                {
                    await AudioPlayerController.SetAllSpeakerOnOffAsync(false);
                }

                //Car speaker on/off toggle
                if (dPadLeftButtonResult.ButtonClicked)
                {
                    await AudioPlayerController.SetCarSpeakerOnOffToggle();
                }

                //Headset speaker on/off toggle
                if (dPadRightButtonResult.ButtonClicked)
                {
                    await AudioPlayerController.SetHeadsetSpeakerOnOffToggle();
                }

                //Sound mode on/off toggle
                var yButtonResult = yButton.UpdateGamepadButtonState(gamepadReading);
                if (yButtonResult.ButtonClicked)
                {
                    await AudioPlayerController.SetSoundModeOnOffToggle();
                }

                //Speak current speaker and sound mode state
                var bButtonResult = bButton.UpdateGamepadButtonState(gamepadReading);
                if (bButtonResult.ButtonClicked)
                {
                    AudioPlayerController.PlaySpeakerOnOffSoundModeAsync(_automaticDrive, _dance);
                }

                //Dance on/off toggle
                var aButtonResult = aButton.UpdateGamepadButtonState(gamepadReading);
                if (aButtonResult.ButtonClicked)
                {
                    await _dance.StartStopToggleAsync();
                }

                //Cliff sensor on/off toggle
                var xButtonResult = xButton.UpdateGamepadButtonState(gamepadReading);
                if (xButtonResult.ButtonClicked)
                {
                    await _automaticDrive.SetCliffSensorState(true);
                }

                //Shutdown
                var xRightShoulderButtonResult = xRightShoulderButton.UpdateGamepadButtonState(gamepadReading, xRightShoulderButtonNotClickableButtons);
                if (xRightShoulderButtonResult.ButtonClicked)
                {
                    _isGamepadReadingStopped = true;
                    await SystemController.ShutdownAsync();
                }

                //Restart
                var aRightShoulderButtonResult = aRightShoulderButton.UpdateGamepadButtonState(gamepadReading, aRightShoulderButtonNotClickableButtons);
                if (aRightShoulderButtonResult.ButtonClicked)
                {
                    _isGamepadReadingStopped = true;
                    await SystemController.RestartAsync();
                }

                await Task.Delay(25);
            }

            _isGamepadReadingStopped = true;
        }

        private async Task StartGamepadVibrationAsync(Gamepad gamepad)
        {
            _isGamepadVibrationStopped = false;

            var vibrations = new List<double>();

            while (_gamepad == gamepad)
            {
                if (!_gamepadShouldVibrate)
                {
                    if (!_isGamepadVibrationShutdown)
                    {
                        _isGamepadVibrationShutdown = true;

                        gamepad.Vibration = new GamepadVibration
                        {
                            LeftMotor = 0,
                            LeftTrigger = 0,
                            RightMotor = 0,
                            RightTrigger = 0
                        };
                    }

                    await Task.Delay(100);
                    continue;
                }

                _isGamepadVibrationShutdown = true;

                var acceleration = _acceleratorSensor.ReadLinearAcceleration();

                //Vibration driving over carpet
                var carpetVibration = 0.2;
                var vibrationSpeed = (((Math.Abs(acceleration.AccelerationX) + Math.Abs(acceleration.AccelerationY) + Math.Abs(acceleration.AccelerationZ))) / 3) - carpetVibration;

                if (vibrations.Count >= 8)
                {
                    var acceleterationVibrationFactor = 2;
                    var vibrationMaxTrigger = vibrations.Max() * acceleterationVibrationFactor;
                    var vibrationMaxMotor = vibrationMaxTrigger * 1;
                    vibrationMaxTrigger = vibrationMaxTrigger * 0.15;

                    if (vibrationMaxTrigger > 1)
                        vibrationMaxTrigger = 1;
                    else if (vibrationMaxTrigger < 0)
                        vibrationMaxTrigger = 0;

                    if (vibrationMaxMotor > 1)
                        vibrationMaxMotor = 1;
                    else if (vibrationMaxMotor < 0)
                        vibrationMaxMotor = 0;

                    gamepad.Vibration = new GamepadVibration
                    {
                        LeftMotor = vibrationMaxMotor,
                        LeftTrigger = vibrationMaxTrigger,
                        RightMotor = vibrationMaxMotor,
                        RightTrigger = vibrationMaxTrigger
                    };

                    vibrations.Clear();
                }

                vibrations.Add(vibrationSpeed);

                await Task.Delay(10);
            }

            gamepad.Vibration = new GamepadVibration
            {
                LeftMotor = 0,
                LeftTrigger = 0,
                RightMotor = 0,
                RightTrigger = 0
            };

            _isGamepadVibrationShutdown = true;
            _gamepadShouldVibrate = true;
            _isGamepadVibrationStopped = true;
        }
    }
}
