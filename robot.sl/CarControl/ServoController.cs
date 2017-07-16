using robot.sl.Helper;
using System.Threading.Tasks;

namespace robot.sl.CarControl
{
    /// <summary>
    /// Servos: Tower Pro MG 90 S
    /// PWM servo board: Adafruit 16-Channel PWM/Servo HAT for Raspberry Pi
    /// </summary>
    public class ServoController
    {
        public PwmController PwmController;

        private volatile bool _isStopped = false;
        private ushort _servoCameraVerticalValue = ServoPositions.CameraVerticalMiddle;

        public void Stop()
        {
            _isStopped = true;
        }

        public async Task Initialize()
        {
            PwmController = new PwmController(0x41);
            await PwmController.Initialize();
            PwmController.SetDesiredFrequency(50);

            PwmController.SetPwm(Servo.CameraHorizontal, 0, ServoPositions.CameraHorizontalMiddle);
            PwmController.SetPwm(Servo.CameraVertical, 0, ServoPositions.CameraVerticalMiddle);
            PwmController.SetPwm(Servo.DistanceSensorHorizontal, 0, ServoPositions.DistanceSensorHorizontalLeft);
            PwmController.SetPwm(Servo.DistanceSensorVertical, 0, ServoPositions.DistanceSensorVerticalTop);
        }

        public void MoveServo(CarControlCommand carControlCommand)
        {
            if(_isStopped)
            {
                return;
            }

            if (carControlCommand.DirectionControlUp)
            {
                if (ServoPositions.CameraVerticalTop == _servoCameraVerticalValue)
                {
                    return;
                }

                _servoCameraVerticalValue -= carControlCommand.DirectionControlUpDownStepSpeed;

                if (_servoCameraVerticalValue < ServoPositions.CameraVerticalTop)
                {
                    _servoCameraVerticalValue = ServoPositions.CameraVerticalTop;
                }

                PwmController.SetPwm(Servo.CameraVertical, 0, _servoCameraVerticalValue);
            }
            else if (carControlCommand.DirectionControlDown)
            {
                if (ServoPositions.CameraVerticalBottom == _servoCameraVerticalValue)
                {
                    return;
                }

                _servoCameraVerticalValue += carControlCommand.DirectionControlUpDownStepSpeed;

                if (_servoCameraVerticalValue > ServoPositions.CameraVerticalBottom)
                {
                    _servoCameraVerticalValue = ServoPositions.CameraVerticalBottom;
                }

                PwmController.SetPwm(Servo.CameraVertical, 0, _servoCameraVerticalValue);
            }
        }
    }
}
