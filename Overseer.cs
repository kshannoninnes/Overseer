using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Overseer.Models;
using Overseer.Services.Misc;
using Overseer.Services.WeebApi;
using Overseer.Services.Logging;
using Overseer.Services.Discord;

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
                Environment.GetEnvironmentVariable("TestToken");
            #else
                Environment.GetEnvironmentVariable("OverseerToken");
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

        private async Task<IServiceProvider> ConfigureServices()
        {
            var db = new GenericDatabaseManager("overseer.db");
            await db.CreateTable<EnforcedUser>();

            var map = new ServiceCollection()
                .AddSingleton<IDatabaseManager>(db)
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddScoped<ILogger, LoggingService>()
                .AddScoped<UserManager>()
                .AddScoped<EmbedManager>()
                .AddScoped<AnimeFetcher>()
                .AddScoped<MangaFetcher>()
                .AddScoped<IApiService, AnilistApiService>();

            return map.BuildServiceProvider();
        }
    }
}
