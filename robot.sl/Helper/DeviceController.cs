using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.System;

namespace robot.sl.Helper
{
    public static class DeviceController
    {
        public static async Task ShutdownDevice()
        {
            await ExecuteCommand("shutdown -s -t 0");
        }

        public static async Task RestartDevice()
        {
            await ExecuteCommand("shutdown -r -t 0");
        }

        private static async Task ExecuteCommand(string command)
        {
            await ProcessLauncher.RunToCompletionAsync(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe", command);
        }
    }
}