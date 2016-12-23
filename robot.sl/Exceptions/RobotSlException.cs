using System;

namespace robot.sl.Exceptions
{
    public class RobotSlException : Exception
    {
        public RobotSlException() { }

        public RobotSlException(string message) : base(message) { }

        public RobotSlException(string message, Exception innerException) : base(message, innerException) { }
    }
}
