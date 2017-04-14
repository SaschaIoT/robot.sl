using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace robot.sl.Helper
{
    public static class Synchronous
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
