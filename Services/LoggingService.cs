using Discord;
using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Overseer.Services
{
    public class LoggingService
    {
        private const string DateFormat = "yyyy-MM-dd";

        private readonly string _logDirectory;
        private readonly int _sourcePadLength;

        public LoggingService(string logDirectory, int sourcePadLength)
        {
            _logDirectory = logDirectory;
            _sourcePadLength = sourcePadLength;

            Initialize();
        }

        private void Initialize()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public async Task Log(LogMessage msg)
        {
            var filename = $"{_logDirectory}/{DateTime.Now.ToString(DateFormat)}.log";
            var text = msg.ToString(padSource: _sourcePadLength);
            using var writer = File.AppendText(filename);

            writer.WriteLine(text);
            Console.WriteLine(text);

            await Task.CompletedTask;
        }
    }
}
