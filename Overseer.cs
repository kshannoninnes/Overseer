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
using System.Diagnostics;

namespace Overseer
{
    class Overseer
    {
        static void Main()
        {
            int retryDelay = 5;
            int attempts = 0;

            while (true)
            {
                try
                {
                    attempts++;
                    new Overseer().MainAsync().GetAwaiter().GetResult();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.StackTrace);

                    if (attempts >= 5)
                        break;

                    Thread.Sleep(retryDelay * attempts * 1000);
                }
            }
        }

        private string envToken;
        private DiscordSocketClient _client;
        private CommandService _commands;
        private CommandHandler _handler;
        private IServiceProvider _services;

        private Overseer() { }

        // Discord.NET framework is all asynchronous, so it requires an async main method
        private async Task MainAsync()
        {
            try
            {
                _client = new DiscordSocketClient(new DiscordSocketConfig { AlwaysDownloadUsers = true });
                _commands = new CommandService(new CommandServiceConfig { CaseSensitiveCommands = false });
                _services = await ConfigureServices();
                _handler = new CommandHandler(_services, _commands, _client);

                envToken = Environment.GetEnvironmentVariable("OverseerToken");
                SetEnvToken();

                await _handler.InitializeAsync();
                await _client.SetGameAsync("for .help", type: ActivityType.Watching);
                await _client.LoginAsync(TokenType.Bot, envToken);
                await _client.StartAsync();
                await Task.Delay(Timeout.Infinite);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
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

        [Conditional("DEBUG")]
        private void SetEnvToken()
        {
            envToken = Environment.GetEnvironmentVariable("MareauProdToken");
        }
    }
}
