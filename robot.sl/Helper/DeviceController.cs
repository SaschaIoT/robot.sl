using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.System;

namespace robot.sl.Helper
{
    public static class DeviceController
    {
        private class CommandResult
        {
            public bool Error { get; set; }
            public string Result { get; set; }
        }

        public static async Task ShutdownDevice()
        {
            await ExecuteCommand($"\"shutdown -s -t 0\"");
        }

        public static async Task RestartDevice()
        {
            await ExecuteCommand($"\"shutdown -r -t 0\"");
        }

        private static async Task ExecuteCommand(string command)
        {
            await ProcessLauncher.RunToCompletionAsync(@"CmdWrapper.exe", command);
        }
    }
}