using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Overseer.Services
{
    public class LoggingService
    {
        private const string DateFormat = "yyyy-MM-dd";
        private const string OverseerSource = "Overseer";

        private readonly string _logDirectory;
        private readonly int _sourcePadLength;

        public LoggingService(DiscordSocketClient client, CommandService commands, string logDirectory, int sourcePadLength)
        {
            _logDirectory = logDirectory;
            _sourcePadLength = sourcePadLength;
            client.Log += Log;
            commands.Log += Log;

            Initialize();
        }

        private void Initialize()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public async Task LogInfo(string message)
        {
            await Task.CompletedTask;
            await Log(new LogMessage(LogSeverity.Info, OverseerSource, message));
        }

        public async Task LogError(string caller, string method, string reason)
        {
            await Task.CompletedTask;
            await Log(new LogMessage(LogSeverity.Error, OverseerSource, $"{caller}'s attempt to invoke {method} failed: {reason}"));
        }

        private async Task Log(LogMessage msg)
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
