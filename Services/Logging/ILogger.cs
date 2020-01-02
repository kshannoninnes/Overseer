using System.Threading.Tasks;

using Discord;

namespace Overseer.Services.Logging
{
    public interface ILogger
    {
        public Task Log(LogSeverity severity, string message, string method, string caller = "Overseer");
    }
}
