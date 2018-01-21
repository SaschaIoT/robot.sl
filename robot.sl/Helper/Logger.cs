using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace robot.sl.Helper
{
    public static class Logger
    {
        public const string FILE_NAME = "log.txt";
        const string LOG_ENTRY_BEGIN = "[Log Entry Begin]";
        const string LOG_ENTRY_END = "[Log Entry End]";
        const int LOG_FILE_MAX_LENGTH = 250000;

        public static async Task WriteAsync(string message, Exception exception)
        {
            await WriteAsync($"{message}: {exception}");
        }

        public static async Task WriteAsync(string message)
        {
            //Parallel file writing cause exception, prevent application from crashing
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                await localFolder.CreateFileAsync(FILE_NAME, CreationCollisionOption.OpenIfExists);
                var logFile = await localFolder.GetFileAsync(FILE_NAME);

                var logEntryOld = await FileIO.ReadTextAsync(logFile);
                var logEntryNew = $"\r\n{LOG_ENTRY_BEGIN}\r\n[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:.fff")}]\r\n{message}\r\n{LOG_ENTRY_END}\r\n";
                var logEntry = logEntryNew + logEntryOld;

                if (logEntry.Length > LOG_FILE_MAX_LENGTH)
                    logEntry = logEntry.Substring(0, LOG_FILE_MAX_LENGTH);

                await FileIO.WriteTextAsync(logFile, logEntry);
            }
            catch (Exception) { }
        }

        public static async Task DeleteAsync()
        {
            var logFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(FILE_NAME);
            if (logFile != null)
            {
                await logFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
        }
    }
}
