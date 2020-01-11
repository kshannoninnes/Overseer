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
    class Overseer
    {
        static async Task Main()
        {
            var client = new DiscordSocketClient(new DiscordSocketConfig { AlwaysDownloadUsers = true });
            var commands = new CommandService(new CommandServiceConfig { CaseSensitiveCommands = false });
            var services = await ConfigureServices(client, commands);
            var handler = new CommandHandler(services, commands, client);

            string EnvToken;
            #if DEBUG
                EnvToken = Environment.GetEnvironmentVariable("TestToken");
            #else
                EnvToken = Environment.GetEnvironmentVariable("OverseerToken");
            #endif

            await handler.InitializeAsync();
            await client.SetGameAsync("for .help", type: ActivityType.Watching);
            await client.LoginAsync(TokenType.Bot, EnvToken);
            await client.StartAsync();
            await Task.Delay(Timeout.Infinite);
        }

        private static async Task<IServiceProvider> ConfigureServices(DiscordSocketClient client, CommandService commands)
        {
            var db = new GenericDatabaseManager("overseer.db");
            await db.CreateTable<EnforcedUser>();

            var map = new ServiceCollection()
                .AddSingleton<IDatabaseManager>(db)
                .AddSingleton(client)
                .AddSingleton(commands)
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
