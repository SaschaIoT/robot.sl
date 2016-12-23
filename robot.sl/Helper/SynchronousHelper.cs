using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

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

    public static class QueuedLock
    {
        private static object innerLock = new object();
        private static volatile int ticketsCount = 0;
        private static volatile int ticketToRide = 1;
        
        public static void Enter()
        {
            int myTicket = Interlocked.Increment(ref ticketsCount);
            Monitor.Enter(innerLock);
            while (true)
            {

                if (myTicket == ticketToRide)
                {
                    return;
                }
                else
                {
                    Monitor.Wait(innerLock);
                }
            }
        }

        public static void Exit()
        {
            Interlocked.Increment(ref ticketToRide);
            Monitor.PulseAll(innerLock);
            Monitor.Exit(innerLock);
        }
    }
}
