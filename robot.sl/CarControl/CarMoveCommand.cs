using Windows.Gaming.Input;

namespace robot.sl.CarControl
{
    public class CarMoveCommand
    {
        /// <summary>
        /// From 0.0 to 1.0
        /// </summary>
        public double Speed { get; set; }
        /// <summary>
        /// Forward: true Backward: false
        /// </summary>
        public bool ForwardBackward { get; set; }
        /// <summary>
        /// Right: from -1.0 to 0.0 Left: from 0.0 to 1.0
        /// </summary>
        public double RightLeft { get; set; }

        public bool LeftCircle { get; set; }
        public bool RightCircle { get; set; }

        public CarMoveCommand() { }

        public CarMoveCommand(CarControlCommand carControlCommand)
        {
            if (carControlCommand.SpeedControlForward || carControlCommand.SpeedControlBackward)
            {
                Speed = 1;

                if (carControlCommand.SpeedControlForward)
                {
                    ForwardBackward = true;
                }
            }

            if (carControlCommand.DirectionControlLeft)
            {
                Speed = carControlCommand.SpeedControlLeftRight;
                ForwardBackward = true;
                LeftCircle = true;
            }
            else if (carControlCommand.DirectionControlRight)
            {
                Speed = carControlCommand.SpeedControlLeftRight;
                ForwardBackward = true;
                RightCircle = true;
            }
        }

        public CarMoveCommand(GamepadReading gamepadReading)
        {
            var deadzone = 0.25;

            var leftThumbstickX = gamepadReading.LeftThumbstickX;
            if ((leftThumbstickX > 0 && leftThumbstickX <= deadzone)
                || (leftThumbstickX < 0 && leftThumbstickX >= (deadzone * -1)))
            {
                leftThumbstickX = 0.0;
            }

            var rightTrigger = gamepadReading.RightTrigger <= deadzone ? 0.0 : gamepadReading.RightTrigger;
            var leftTrigger = gamepadReading.LeftTrigger <= deadzone ? 0.0 : gamepadReading.LeftTrigger;

            var rightLeftTrigger = (rightTrigger > 0.0) && (leftTrigger > 0.0);

            if (!rightLeftTrigger
                && rightTrigger > 0.0)
            {
                Speed = rightTrigger;
                ForwardBackward = true;
            }
            else if (!rightLeftTrigger
                     && leftTrigger > 0.0)
            {
                Speed = leftTrigger;
                ForwardBackward = false;
            }
            else
            {
                Speed = 0.0;
                ForwardBackward = true;
            }

            RightLeft = leftThumbstickX;

            if (RightLeft <= -0.985)
            {
                LeftCircle = true;
            }
            else if (RightLeft >= 0.985)
            {
                RightCircle = true;
            }

            if (RightLeft != 0.0 && Speed == 0.0)
            {
                if (RightLeft < 0)
                {
                    LeftCircle = true;
                    Speed = 1;
                }
                else if (RightLeft > 0)
                {
                    RightCircle = true;
                    Speed = 1;
                }
            }
        }
    }
}
