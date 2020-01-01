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
                Color = Defaults.Embed.Color
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

            await _logger.Log(new LogMessage(LogSeverity.Error, $"{Context.User.Username}", $"\"Help\" command invoked."));
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
                        Color = Defaults.Embed.Color
                    };

                    await _logger.Log(new LogMessage(LogSeverity.Error, $"{Context.User.Username}", $"\"Help\" command invoked with argument \"{commandName}\""));
                    await ReplyAsync(embed: embedBuilder.Build());
                    return;
                }
            }

            await _logger.Log(new LogMessage(LogSeverity.Error, $"{Context.User.Username}", $"\"Help\" command failed with argument \"{commandName}\""));
            await ReplyAsync($"Command {commandName} not found");
        }
    }
}
