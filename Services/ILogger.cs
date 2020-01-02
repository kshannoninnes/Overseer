using Discord;
using System.Threading.Tasks;

namespace Overseer.Services
{
    public interface ILogger
    {
        public Task Log(LogSeverity severity, string message, string method, string caller = "Overseer");
    }
}
