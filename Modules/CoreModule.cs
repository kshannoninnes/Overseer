using Discord;
using Discord.Commands;
using Overseer.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Overseer.Modules
{
    [Name("Core")]
    public class CoreModule : ModuleBase<SocketCommandContext>
    {
        private const uint EMBED_COLOR = 0x00ff00;

        private readonly LoggingService _logger;
        private readonly CommandService _commandService;

        public CoreModule(LoggingService logger, CommandService commandService)
        {
            _logger = logger;
            _commandService = commandService;
        }

        [Command("help")]
        [Summary("View a list of useful information for Overseer.\n\n**Usage**: >help [command]")]
        public async Task HelpAsync()
        {
            var modules = _commandService.Modules;
            var embedBuilder = new EmbedBuilder
            {
                Title = "Available Commands",
                Color = Defaults.Embed.COLOR
            };

            foreach (var module in modules)
            {
                var listOfCommands = new HashSet<string>();
                var moduleName = module.Name;
                foreach(var command in module.Commands)
                {
                    listOfCommands.Add(command.Name.ToLower());
                }
                var commandsText = string.Join(", ", listOfCommands);

                embedBuilder.AddField(moduleName, commandsText);
            }

            await ReplyAsync(embed: embedBuilder.Build());
        }

        [Command("help")]
        public async Task HelpAsync(string commandName)
        {
            var commands = _commandService.Commands;

            foreach (var command in commands)
            {
                if(command.Name.Equals(commandName, System.StringComparison.OrdinalIgnoreCase))
                {
                    var embedBuilder = new EmbedBuilder
                    {
                        Title = command.Name,
                        Description = command.Summary,
                        Color = Defaults.Embed.COLOR
                    };
                    await ReplyAsync(embed: embedBuilder.Build());
                    return;
                }
            }

            await ReplyAsync($"Command {commandName} not found");
        }
    }
}
