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
        private ushort _servoMinValue = 170;
        private ushort _servoMaxValue = 465;
        private ushort _servoCameraVerticalValue = 318;
        private volatile bool _isStopped = false;

        public void Stop()
        {
            _isStopped = true;
        }

        public async Task Initialize()
        {
            PwmController = new PwmController(0x41);
            await PwmController.Initialize();
            PwmController.SetDesiredFrequency(60);

            PwmController.SetPwm(0, 0, 333);
            PwmController.SetPwm(1, 0, 318);
            PwmController.SetPwm(2, 0, 135);
            PwmController.SetPwm(3, 0, 202);
        }

        public void MoveServo(CarControlCommand carControlCommand)
        {
            if(_isStopped)
            {
                return;
            }

            if (carControlCommand.DirectionControlUp)
            {
                if (_servoMinValue == _servoCameraVerticalValue)
                {
                    return;
                }

                _servoCameraVerticalValue -= carControlCommand.DirectionControlUpDownStepSpeed;

                if (_servoCameraVerticalValue < _servoMinValue)
                {
                    _servoCameraVerticalValue = _servoMinValue;
                }

                PwmController.SetPwm(1, 0, _servoCameraVerticalValue);
            }
            else if (carControlCommand.DirectionControlDown)
            {
                if (_servoMaxValue == _servoCameraVerticalValue)
                {
                    return;
                }

                _servoCameraVerticalValue += carControlCommand.DirectionControlUpDownStepSpeed;

                if (_servoCameraVerticalValue > _servoMaxValue)
                {
                    _servoCameraVerticalValue = _servoMaxValue;
                }

                PwmController.SetPwm(1, 0, _servoCameraVerticalValue);
            }
        }
    }
}
