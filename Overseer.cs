using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Overseer.Services;
using Overseer.Handlers;
using Overseer.Models;

namespace Overseer
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Main class runs forever")]
    class Overseer
    {
        static void Main() => new Overseer().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandService _commands;
        private CommandHandler _handler;
        private IServiceProvider _services;

        private static string EnvToken =>
            #if DEBUG
                Environment.GetEnvironmentVariable("MareauProdToken");
            #else
                return Environment.GetEnvironmentVariable("OverseerToken");
            #endif

        // Discord.NET framework is all asynchronous, so it requires an async main method
        private async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig { AlwaysDownloadUsers = true });
            _commands = new CommandService(new CommandServiceConfig { CaseSensitiveCommands = false });
            _services = await ConfigureServices();
            _handler = new CommandHandler(_services, _commands, _client);

            await _handler.InitializeAsync();
            await _client.SetGameAsync("for .help", type: ActivityType.Watching);
            await _client.LoginAsync(TokenType.Bot, EnvToken);
            await _client.StartAsync();
            await Task.Delay(Timeout.Infinite);
        }

        // Ensure all services have have any dependencies injected before registration
        private async Task<IServiceProvider> ConfigureServices()
        {
            var db = new DatabaseHandler("overseer.db");
            await db.CreateTable<EnforcedUser>();

            var logger = new LoggingService(logDirectory: "Logs", sourcePadLength: 20);
            _client.Log += logger.Log;
            _commands.Log += logger.Log;

            var userService = new UserService(db, _client, logger);
            await userService.StartMaintainingAsync();

            var weebService = new WeebService(logger);

            var map = new ServiceCollection()
                .AddSingleton(logger)
                .AddSingleton(userService)
                .AddSingleton(weebService);

            return map.BuildServiceProvider();
        }
    }
}
