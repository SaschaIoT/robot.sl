using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.System;

namespace robot.sl.Helper
{
    public static class AudioDeviceController
    {
        //To allow to run IoTCoreAudioControlTool.exe from UWP apps, execute the following command with powershell on the Windows IoT Core device
        //reg.exe ADD "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\EmbeddedMode\ProcessLauncher" /f /v AllowedExecutableFilesList /t REG_MULTI_SZ /d "C:\Windows\System32\IoTCoreAudioControlTool.exe\0"

        private class CommandResult
        {
            public bool Error { get; set; }
            public string Result { get; set; }
        }

        public static async Task SetDefaultRenderDeviceVolumeAsync(int volume)
        {
            await ExecuteCommandAsync($"r {volume}");
        }

        public static async Task SetDefaultCaptureDeviceVolumeAsync(int volume)
        {
            await ExecuteCommandAsync($"c {volume}");
        }

        public static async Task SetDefaultRenderDeviceAsync(string renderDeviceName)
        {
            await SetDefaultDeviceAsync(renderDeviceName, true);
        }

        public static async Task SetDefaultCaptureDeviceAsync(string captureDeviceName)
        {
            await SetDefaultDeviceAsync(captureDeviceName, false);
        }

        private static async Task SetDefaultDeviceAsync(string deviceName, bool setRenderCaptureDevice)
        {
            var renderCaptureDevicesResult = await ExecuteCommandAsync("l");

            if (renderCaptureDevicesResult.Error)
                return;

            var renderCaptureDevices = renderCaptureDevicesResult.Result.Split(Environment.NewLine.ToArray());
            foreach (var device in renderCaptureDevices)
            {
                var properties = device.Split(',');

                if (properties == null || properties.Length < 4)
                    continue;

                if(((setRenderCaptureDevice && properties[1].ToLower().Contains("r")) 
                        || (!setRenderCaptureDevice && properties[1].ToLower().Contains("c")))
                    && properties[2].ToLower().Contains(deviceName.ToLower()))
                {
                    var setDefaultDeviceResult = await ExecuteCommandAsync($"d {properties[3]}");

                    if (setDefaultDeviceResult.Error)
                        return;
                }
            }
        }

        private static async Task<CommandResult> ExecuteCommandAsync(string command)
        {
            var commandResult = new CommandResult();

            var processLauncherOptions = new ProcessLauncherOptions();
            var standardOutput = new InMemoryRandomAccessStream();

            processLauncherOptions.StandardOutput = standardOutput;

            var processLauncherResult = await ProcessLauncher.RunToCompletionAsync(@"C:\Windows\System32\IoTCoreAudioControlTool.exe", command, processLauncherOptions);
            if (processLauncherResult.ExitCode == 0)
            {
                using (var outStreamRedirect = standardOutput.GetInputStreamAt(0))
                {
                    var size = standardOutput.Size;
                    using (var dataReader = new DataReader(outStreamRedirect))
                    {
                        var bytesLoaded = await dataReader.LoadAsync((uint)size);
                        var stringRead = dataReader.ReadString(bytesLoaded);
                        commandResult.Result = stringRead.Trim();
                    }
                }
            }
            else
            {
                commandResult.Error = true;

                await Logger.WriteAsync("Error executing IoTCoreAudioControlTool.exe command.");
            }

            return commandResult;
        }
    }
}
