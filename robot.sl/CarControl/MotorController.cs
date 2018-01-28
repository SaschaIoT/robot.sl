using robot.sl.Audio;
using robot.sl.Exceptions;
using robot.sl.Helper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace robot.sl.CarControl
{
    /// <summary>
    /// DC Motors from: 4WD Robot Smart Car Chassis Kits with Speed Encoder for Arduino
    /// PWM motor board: Adafruit DC and Stepper Motor HAT for Raspberry Pi
    /// </summary>
    public class Motor
    {
        public MotorController MotorController { get; set; }
        public int MotorNumber { get; set; }
        public int MotorPwmPin { get; set; }
        public int MotorIn1Pin { get; set; }
        public int MotorIn2Pin { get; set; }

        public Motor(MotorController motorController, int motorNumber)
        {
            MotorController = motorController;
            MotorNumber = motorNumber;
            int motorPwmPin = 0, motorIn2Pin = 0, motorIn1Pin = 0;

            if (motorNumber == 0)
            {
                motorPwmPin = 8;
                motorIn1Pin = 9;
                motorIn2Pin = 10;
            }
            else if (motorNumber == 1)
            {
                motorPwmPin = 13;
                motorIn1Pin = 12;
                motorIn2Pin = 11;
            }
            else if (motorNumber == 2)
            {
                motorPwmPin = 2;
                motorIn1Pin = 3;
                motorIn2Pin = 4;
            }
            else if (motorNumber == 3)
            {
                motorPwmPin = 7;
                motorIn1Pin = 6;
                motorIn2Pin = 5;
            }
            else
            {
                throw new RobotSlException("Motor must be between 1 and 4 inclusive");
            }

            MotorPwmPin = motorPwmPin;
            MotorIn1Pin = motorIn2Pin;
            MotorIn2Pin = motorIn1Pin;
        }

        public void Run(MotorAction command)
        {
            if (MotorController == null)
            {
                return;
            }

            if (command == MotorAction.FORWARD)
            {
                MotorController.SetPin(MotorIn2Pin, 0);
                MotorController.SetPin(MotorIn1Pin, 1);
            }
            else if (command == MotorAction.BACKWARD)
            {
                MotorController.SetPin(MotorIn1Pin, 0);
                MotorController.SetPin(MotorIn2Pin, 1);
            }
            else if (command == MotorAction.RELEASE)
            {
                MotorController.SetPin(MotorIn1Pin, 0);
                MotorController.SetPin(MotorIn2Pin, 0);
            }
        }

        public void SetSpeed(int speed)
        {
            if (speed < 0)
            {
                speed = 0;
            }
            else if (speed > 255)
            {
                speed = 255;
            }

            MotorController.PwmController.SetPwm((byte)MotorPwmPin, 0, (ushort)(speed * 16));
        }
    }

    public class MotorController
    {
        private int _i2caddress;
        private int _frequency;
        public PwmController PwmController;
        public List<Motor> Motors;
        private volatile bool _isStopped;
        private CarMoveCommand _lastCarMoveCommand;
        private MotorCommandSource _lastMotorCommandSource = MotorCommandSource.Other;

        //Dependencies
        private AutomaticSpeakController _automaticSpeakController;
        private AutomaticDrive _automaticDrive;
        private Dance _dance;
        private SpeechRecognition _speechRecognition;

        public void Stop()
        {
            _isStopped = true;
        }

        public async Task Initialize(AutomaticSpeakController automaticSpeakController,
                                     AutomaticDrive automaticDrive,
                                     Dance dance,
                                     SpeechRecognition speechRecognition,
                                     byte i2cAddress = 0x60,
                                     int frequency = 40)
        {
            _automaticSpeakController = automaticSpeakController;
            _automaticDrive = automaticDrive;
            _dance = dance;
            _speechRecognition = speechRecognition;

            _i2caddress = i2cAddress;
            _frequency = frequency;
            Motors = new List<Motor>
            {
                new Motor(this, 0),
                new Motor(this, 1),
                new Motor(this, 2),
                new Motor(this, 3)
            };

            PwmController = new PwmController(i2cAddress);
            await PwmController.InitializeAsync();
            PwmController.SetDesiredFrequency(_frequency);

            await MoveCarAsync(new CarMoveCommand
            {
                ForwardBackward = true,
                Speed = 0
            }, MotorCommandSource.Other);
        }

        public void SetPin(int pin, int value)
        {
            if (pin < 0 || pin > 15)
            {
                throw new RobotSlException("PWM pin must be between 0 and 15 inclusive");
            }

            if (value != 0 && value != 1)
            {
                throw new RobotSlException("Pin value must be 0 or 1!");
            }

            if (value == 0)
            {
                PwmController.SetPwm((byte)pin, 4096, 0);
            }
            else if (value == 1)
            {
                PwmController.SetPwm((byte)pin, 0, 4096);
            }
        }

        public Motor GetMotor(int num)
        {
            if (num < 1 || num > 4)
            {
                throw new RobotSlException("MotorHAT Motor must be between 1 and 4 inclusive");
            }

            return Motors[num - 1];
        }

        public async Task MoveCarAsync(CarControlCommand carControlCommand,
                                       MotorCommandSource motorCommandSource)
        {
            await MoveCarAsync(new CarMoveCommand(carControlCommand), motorCommandSource);
        }

        public async Task MoveCarAsync(CarMoveCommand carMoveCommand,
                                       MotorCommandSource motorCommandSource)
        {
            if (_isStopped)
            {
                return;
            }

            if (_lastMotorCommandSource == MotorCommandSource.AutomaticDrive
                && motorCommandSource != MotorCommandSource.AutomaticDrive)
            {
                var speak = motorCommandSource != MotorCommandSource.SpeechRecognation
                            && motorCommandSource != MotorCommandSource.Dance;

                await _automaticDrive.StopAsync(speak, false);
            }
            else if (_lastMotorCommandSource == MotorCommandSource.Dance
                     && motorCommandSource != MotorCommandSource.Dance)
            {
                var speak = motorCommandSource != MotorCommandSource.SpeechRecognation
                            && motorCommandSource != MotorCommandSource.AutomaticDrive;

                await _dance.StopAsync(speak, false);
            }
            else if(_lastMotorCommandSource == MotorCommandSource.SpeechRecognation
                    && motorCommandSource != MotorCommandSource.SpeechRecognation)
            {
                _speechRecognition.ResetDriving();
            }

            await MotorSynchronous.Call(() =>
            {
                _lastMotorCommandSource = motorCommandSource;

                //Straight away correction
                var leftCorrectionFaktor = 0.89;

                //Speed from 0 to 255
                var dcMotorMaxSpeed = 255;

                if (_lastCarMoveCommand != null
                   && _lastCarMoveCommand.ForwardBackward == carMoveCommand.ForwardBackward
                   && _lastCarMoveCommand.LeftCircle == carMoveCommand.LeftCircle
                   && _lastCarMoveCommand.RightCircle == carMoveCommand.RightCircle
                   && _lastCarMoveCommand.RightLeft == carMoveCommand.RightLeft
                   && _lastCarMoveCommand.Speed == carMoveCommand.Speed)
                {
                    return;
                }

                _lastCarMoveCommand = carMoveCommand;

                _automaticSpeakController.CarMoveCommand = carMoveCommand;

                var motorLeft1 = GetMotor(3);
                var motorLeft2 = GetMotor(4);
                var motorRight1 = GetMotor(1);
                var motorRight2 = GetMotor(2);

                if (carMoveCommand.Speed == 0)
                {
                    motorLeft1.SetSpeed(0);
                    motorLeft2.SetSpeed(0);
                    motorRight1.SetSpeed(0);
                    motorRight2.SetSpeed(0);

                    motorLeft1.Run(MotorAction.RELEASE);
                    motorLeft2.Run(MotorAction.RELEASE);
                    motorRight1.Run(MotorAction.RELEASE);
                    motorRight2.Run(MotorAction.RELEASE);
                }
                else if (carMoveCommand.RightCircle)
                {
                    var carSpeedFull = (int)Math.Round(carMoveCommand.Speed * dcMotorMaxSpeed, 0);

                    if (carMoveCommand.ForwardBackward)
                    {
                        motorLeft1.Run(MotorAction.FORWARD);
                        motorLeft2.Run(MotorAction.FORWARD);
                        motorRight1.Run(MotorAction.BACKWARD);
                        motorRight2.Run(MotorAction.BACKWARD);

                        motorLeft1.SetSpeed(carSpeedFull);
                        motorLeft2.SetSpeed(carSpeedFull);
                        motorRight1.SetSpeed(carSpeedFull);
                        motorRight2.SetSpeed(carSpeedFull);
                    }
                    else
                    {
                        motorLeft1.Run(MotorAction.BACKWARD);
                        motorLeft2.Run(MotorAction.BACKWARD);
                        motorRight1.Run(MotorAction.FORWARD);
                        motorRight2.Run(MotorAction.FORWARD);

                        motorLeft1.SetSpeed(carSpeedFull);
                        motorLeft2.SetSpeed(carSpeedFull);
                        motorRight1.SetSpeed(carSpeedFull);
                        motorRight2.SetSpeed(carSpeedFull);
                    }
                }
                else if (carMoveCommand.LeftCircle)
                {
                    var carSpeedFull = (int)Math.Round(carMoveCommand.Speed * dcMotorMaxSpeed, 0);

                    if (carMoveCommand.ForwardBackward)
                    {
                        motorLeft1.Run(MotorAction.BACKWARD);
                        motorLeft2.Run(MotorAction.BACKWARD);
                        motorRight1.Run(MotorAction.FORWARD);
                        motorRight2.Run(MotorAction.FORWARD);

                        motorLeft1.SetSpeed(carSpeedFull);
                        motorLeft2.SetSpeed(carSpeedFull);
                        motorRight1.SetSpeed(carSpeedFull);
                        motorRight2.SetSpeed(carSpeedFull);
                    }
                    else
                    {
                        motorLeft1.Run(MotorAction.FORWARD);
                        motorLeft2.Run(MotorAction.FORWARD);
                        motorRight1.Run(MotorAction.BACKWARD);
                        motorRight2.Run(MotorAction.BACKWARD);

                        motorLeft1.SetSpeed(carSpeedFull);
                        motorLeft2.SetSpeed(carSpeedFull);
                        motorRight1.SetSpeed(carSpeedFull);
                        motorRight2.SetSpeed(carSpeedFull);
                    }
                }
                //Left
                else if (carMoveCommand.RightLeft < 0)
                {
                    var carSpeedFull = (int)Math.Round(carMoveCommand.Speed * dcMotorMaxSpeed, 0);
                    var carSpeedSlow = (int)Math.Round(carMoveCommand.Speed * (1 - Math.Abs(carMoveCommand.RightLeft)) * dcMotorMaxSpeed, 0);

                    if (carMoveCommand.ForwardBackward)
                    {
                        motorLeft1.Run(MotorAction.FORWARD);
                        motorLeft2.Run(MotorAction.FORWARD);
                        motorRight1.Run(MotorAction.FORWARD);
                        motorRight2.Run(MotorAction.FORWARD);
                    }
                    else
                    {
                        motorLeft1.Run(MotorAction.BACKWARD);
                        motorLeft2.Run(MotorAction.BACKWARD);
                        motorRight1.Run(MotorAction.BACKWARD);
                        motorRight2.Run(MotorAction.BACKWARD);
                    }

                    var carSpeedSlowWithLeftCorrection = (int)Math.Round(carSpeedSlow * leftCorrectionFaktor);
                    motorLeft1.SetSpeed(carSpeedSlowWithLeftCorrection);
                    motorLeft2.SetSpeed(carSpeedSlowWithLeftCorrection);
                    motorRight1.SetSpeed(carSpeedFull);
                    motorRight2.SetSpeed(carSpeedFull);
                }
                //Right
                else if (carMoveCommand.RightLeft > 0)
                {
                    var carSpeedFull = (int)Math.Round(carMoveCommand.Speed * dcMotorMaxSpeed, 0);
                    var carSpeedSlow = (int)Math.Round(carMoveCommand.Speed * (1 - Math.Abs(carMoveCommand.RightLeft)) * dcMotorMaxSpeed, 0);

                    if (carMoveCommand.ForwardBackward)
                    {
                        motorLeft1.Run(MotorAction.FORWARD);
                        motorLeft2.Run(MotorAction.FORWARD);
                        motorRight1.Run(MotorAction.FORWARD);
                        motorRight2.Run(MotorAction.FORWARD);
                    }
                    else
                    {
                        motorLeft1.Run(MotorAction.BACKWARD);
                        motorLeft2.Run(MotorAction.BACKWARD);
                        motorRight1.Run(MotorAction.BACKWARD);
                        motorRight2.Run(MotorAction.BACKWARD);
                    }

                    var carSpeedFullWithLeftCorrection = (int)Math.Round(carSpeedFull * leftCorrectionFaktor);
                    motorLeft1.SetSpeed(carSpeedFullWithLeftCorrection);
                    motorLeft2.SetSpeed(carSpeedFullWithLeftCorrection);
                    motorRight1.SetSpeed(carSpeedSlow);
                    motorRight2.SetSpeed(carSpeedSlow);
                }
                else if (carMoveCommand.RightLeft == 0)
                {
                    var carSpeedFull = (int)Math.Round(carMoveCommand.Speed * dcMotorMaxSpeed, 0);

                    if (carMoveCommand.ForwardBackward)
                    {
                        motorLeft1.Run(MotorAction.FORWARD);
                        motorLeft2.Run(MotorAction.FORWARD);
                        motorRight1.Run(MotorAction.FORWARD);
                        motorRight2.Run(MotorAction.FORWARD);
                    }
                    else
                    {
                        motorLeft1.Run(MotorAction.BACKWARD);
                        motorLeft2.Run(MotorAction.BACKWARD);
                        motorRight1.Run(MotorAction.BACKWARD);
                        motorRight2.Run(MotorAction.BACKWARD);
                    }

                    var carSpeedFullWithLeftCorrection = (int)Math.Round(carSpeedFull * leftCorrectionFaktor);
                    motorLeft1.SetSpeed(carSpeedFullWithLeftCorrection);
                    motorLeft2.SetSpeed(carSpeedFullWithLeftCorrection);
                    motorRight1.SetSpeed(carSpeedFull);
                    motorRight2.SetSpeed(carSpeedFull);
                }
            });
        }
    }

    public enum MotorCommandSource
    {
        Other,
        AutomaticDrive,
        SpeechRecognation,
        Dance
    }
}
