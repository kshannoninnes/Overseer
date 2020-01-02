using System;
using System.Reflection;
using System.Threading.Tasks;

using Discord.Commands;
using Discord.WebSocket;

namespace Overseer.Services.Misc
{
    public class CommandHandler
    {
        private readonly IServiceProvider _services;
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;

        public CommandHandler(IServiceProvider services, CommandService commands, DiscordSocketClient client)
        {
            _services = services;
            _commands = commands;
            _client = client;
        }

        public async Task InitializeAsync()
        {
            // Pass the service provider to the second parameter of
            // AddModulesAsync to inject dependencies to all modules 
            // that may require them.
            await _commands.AddModulesAsync(
                assembly: Assembly.GetEntryAssembly(),
                services: _services);
            _client.MessageReceived += HandleCommandAsync;
        }

        public async Task HandleCommandAsync(SocketMessage msg)
        {
            if (!(msg is SocketUserMessage message)) return;


            int argPos = 0;

            if (!(message.HasCharPrefix('.', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            var context = new SocketCommandContext(_client, message);

            // Pass the service provider to the ExecuteAsync method for
            // precondition checks.
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: _services);
        }
    }
}
