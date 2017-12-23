namespace robot.sl.Helper
{
    public static class Servo
    {
        public static byte CameraHorizontal { get; } = 0;
        public static byte CameraVertical { get; } = 1;
        public static byte DistanceSensorHorizontal { get; } = 2;
        public static byte DistanceSensorVertical { get; } = 3;
    }

    public static class ServoPositions
    {
        public static ushort CameraHorizontalMiddle { get; } = 279;

        public static ushort CameraVerticalMiddle { get; } = 255;
        public static ushort CameraVerticalTop { get; } = 145;
        public static ushort CameraVerticalBottom { get; } = 360;

        public static ushort DistanceSensorHorizontalLeft { get; } = 95;
        public static ushort DistanceSensorHorizontalMiddle { get; } = 278;
        public static ushort DistanceSensorHorizontalLeftMiddle { get; } = 170;
        public static ushort DistanceSensorHorizontalRight { get; } = 455;
        public static ushort DistanceSensorHorizontalRightMiddle { get; } = 370;

        public static ushort DistanceSensorVerticalTop { get; } = 130;
        public static ushort DistanceSensorVerticalMiddle { get; } = 400;
    }
}
