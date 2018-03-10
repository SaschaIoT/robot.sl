using System;
using System.Threading;

namespace robot.sl.Helper
{
    public static class I2CSynchronous
    {
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public static void Call(Action action)
        {
            semaphoreSlim.Wait();

            try
            {
                action();
            }
            finally { semaphoreSlim.Release(); }
        }
    }
}
