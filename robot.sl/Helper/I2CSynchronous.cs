using System;

namespace robot.sl.Helper
{
    public static class I2CSynchronous
    {
        private static volatile object _lock = new object();

        public static void Call(Action action)
        {
            lock (_lock)
            {
                action();
            }
        }
    }
}
