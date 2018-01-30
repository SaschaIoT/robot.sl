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
        public static ushort CameraHorizontalMiddle { get; } = 283;

        public static ushort CameraVerticalMiddle { get; } = 280;
        public static ushort CameraVerticalTop { get; } = 210;
        public static ushort CameraVerticalBottom { get; } = 370;

        public static ushort DistanceSensorHorizontalLeft { get; } = 120;
        public static ushort DistanceSensorHorizontalLeftMiddle { get; } = 180;
        public static ushort DistanceSensorHorizontalMiddle { get; } = 295;
        public static ushort DistanceSensorHorizontalRight { get; } = 470;
        public static ushort DistanceSensorHorizontalRightMiddle { get; } = 390;

        public static ushort DistanceSensorVerticalTop { get; } = 160;
        public static ushort DistanceSensorVerticalMiddle { get; } = 252;
    }
}