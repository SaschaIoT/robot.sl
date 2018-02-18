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

        private const int FULL_SPEED = 1;
        private const int NO_SPEED = 0;
        private const double MIN_SPEED = 0.45;
        private const double MIN_SPEED_LEFT_RIGHT = 0.65;
        private const double THUMBSTICK_X_LEFT_CIRCLE = 0.985;
        private const double THUMBSTICK_X_RIGHT_CIRCLE = THUMBSTICK_X_LEFT_CIRCLE * -1;
        private const double THUMBSTICK_DEADZONE = 0.25;

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
                Speed = 1;
                ForwardBackward = true;
                LeftCircle = true;
            }
            else if (carControlCommand.DirectionControlRight)
            {
                Speed = 1;
                ForwardBackward = true;
                RightCircle = true;
            }
        }

        public CarMoveCommand(GamepadReading gamepadReading)
        {
            //Left/right
            var leftRightThumbstick = gamepadReading.LeftThumbstickX;
            if ((leftRightThumbstick > 0 && leftRightThumbstick <= THUMBSTICK_DEADZONE)
                || (leftRightThumbstick < 0 && leftRightThumbstick >= (THUMBSTICK_DEADZONE * -1)))
            {
                leftRightThumbstick = 0;
            }
            RightLeft = leftRightThumbstick;

            //Forward/backward trigger
            var forwardTrigger = gamepadReading.RightTrigger <= THUMBSTICK_DEADZONE ? 0 : gamepadReading.RightTrigger;
            var backwardTrigger = gamepadReading.LeftTrigger <= THUMBSTICK_DEADZONE ? 0 : gamepadReading.LeftTrigger;
            var bothTrigger = forwardTrigger > 0 && backwardTrigger > 0;

            //Forward or backward
            if (bothTrigger == false
                && (forwardTrigger > 0 || backwardTrigger > 0))
            {
                var speed = 0.0;

                //Forward
                if (forwardTrigger > 0)
                {
                    speed = forwardTrigger;
                    ForwardBackward = true;
                }
                //Backward
                else if (backwardTrigger > 0)
                {
                    speed = backwardTrigger;
                    ForwardBackward = false;
                }

                //Min speed
                if(leftRightThumbstick != 0)
                {
                    if (speed < MIN_SPEED_LEFT_RIGHT)
                    {
                        Speed = MIN_SPEED_LEFT_RIGHT;
                    }
                    else
                        Speed = speed;
                }
                else if (speed < MIN_SPEED)
                {
                    Speed = MIN_SPEED;
                }
                else
                {
                    Speed = speed;
                }

                //Left circle
                if (leftRightThumbstick <= THUMBSTICK_X_RIGHT_CIRCLE)
                {
                    LeftCircle = true;
                }
                //Right circle
                else if (leftRightThumbstick >= THUMBSTICK_X_LEFT_CIRCLE)
                {
                    RightCircle = true;
                }
            }
            //No speed and left
            else if (leftRightThumbstick > 0)
            {
                LeftCircle = true;
                Speed = FULL_SPEED;
            }
            //No speed and right
            else if (leftRightThumbstick < 0)
            {
                RightCircle = true;
                Speed = FULL_SPEED;
            }
            //No speed
            else
            {
                Speed = NO_SPEED;
                ForwardBackward = true;
            }
        }
    }
}
