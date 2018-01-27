using System;
using System.Threading;
using System.Threading.Tasks;

namespace robot.sl.Helper
{
    public static class DanceSynchronous
    {
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public static async Task Call(Func<Task> action)
        {
            await semaphoreSlim.WaitAsync();

            try
            {
                await action();
            }
            finally { semaphoreSlim.Release(); }
        }

        public static async Task Call(Action action)
        {
            await semaphoreSlim.WaitAsync();

            try
            {
                action();
            }
            finally { semaphoreSlim.Release(); }
        }
    }
}
