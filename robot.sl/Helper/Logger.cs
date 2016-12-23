using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace robot.sl.Helper
{
    public static class Logger
    {
        public const string FILE_NAME = "debug.txt";

        public static async Task Write(string message, Exception exception)
        {
            await Write($"{message}: {exception}");
        }

        public static async Task Write(string message)
        {
            //Parallel file writing cause exception, prevent application from crashing
            try
            {
                var messageLines = new List<string>() { message };
                var localFolder = ApplicationData.Current.LocalFolder;
                await localFolder.CreateFileAsync(FILE_NAME, CreationCollisionOption.OpenIfExists);
                var logFile = await localFolder.GetFileAsync(FILE_NAME);
                await FileIO.AppendLinesAsync(logFile, messageLines);
            }
            catch (Exception) { }
        }
    }
}
