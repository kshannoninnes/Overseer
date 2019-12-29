using Discord;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Overseer.Services
{
    public class LoggingService
    {
        private readonly string _logDirectory;

        public int SourcePadLength { get; private set; }

        public LoggingService(string logDirectory, int sourcePadLength)
        {
            _logDirectory = logDirectory;
            SourcePadLength = sourcePadLength;
        }

        public async Task Log(LogMessage msg)
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            var filename = $"{_logDirectory}/{DateTime.Now.ToString("yyyy-MM-dd")}.log";
            var text = msg.ToString(padSource: SourcePadLength);
            using var writer = File.AppendText(filename);

            writer.WriteLine(text);
            Console.WriteLine(text);

            await Task.CompletedTask;
        }
    }
}
