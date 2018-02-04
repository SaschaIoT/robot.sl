using System;
using Windows.Gaming.Input;

namespace robot.sl.CarControl
{
    public class CarControlCommand
    {
        public bool DirectionControlUp { get; set; }
        public bool DirectionControlLeft { get; set; }
        public bool DirectionControlRight { get; set; }
        public bool DirectionControlDown { get; set; }
        public bool SpeedControlForward { get; set; }
        public bool SpeedControlBackward { get; set; }
        public ushort DirectionControlUpDownStepSpeed { get; set; }

        const int DIRECTION_CONTROL_UP_DOWN_STEP_MAX_SPEED = 4;

        public CarControlCommand() { }

        public CarControlCommand(GamepadReading gamepadReading)
        {
            var deadzone = 0.25;

            var leftThumbstickY = gamepadReading.LeftThumbstickY;
            if ((leftThumbstickY > 0 && leftThumbstickY <= deadzone)
                || (leftThumbstickY < 0 && leftThumbstickY >= (deadzone * -1)))
            {
                leftThumbstickY = 0.0;
            }

            var directionControlUpDown = leftThumbstickY;

            if (directionControlUpDown > 0)
            {
                DirectionControlUp = true;
            }
            else if (directionControlUpDown < 0)
            {
                DirectionControlDown = true;
            }

            DirectionControlUpDownStepSpeed = (ushort)Math.Round(Math.Abs(directionControlUpDown) * DIRECTION_CONTROL_UP_DOWN_STEP_MAX_SPEED, 1);
        }
    }
}
