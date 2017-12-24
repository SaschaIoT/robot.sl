using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.System;

namespace robot.sl.Helper
{
    public static class DeviceController
    {
        //To allow to run IoTCoreAudioControlTool.exe and Powershell.exe from UWP apps, execute the following command with powershell on the Windows IoT Core device
        //reg.exe ADD "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\EmbeddedMode\ProcessLauncher" /f /v AllowedExecutableFilesList /t REG_MULTI_SZ /d "C:\Windows\System32\IoTCoreAudioControlTool.exe\0C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe\0"

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