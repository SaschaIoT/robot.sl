using System;
using Windows.System;

namespace robot.sl.Helper
{
    public static class DeviceController
    {
        public static void ShutdownDevice()
        {
            ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.Zero);
        }

        public static void RestartDevice()
        {
            ShutdownManager.BeginShutdown(ShutdownKind.Restart, TimeSpan.Zero);
        }
    }
}