using Discord;
using Discord.Commands;
using Overseer.Services;
using System;
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

            await _logger.LogInfo($"{Context.User.Username} invoked \"help\".");
            await ReplyAsync(embed: embedBuilder.Build());
        }

        [Command("help")]
        public async Task HelpAsync(string commandName)
        {
            var caller = Context.User.Username;
            var commands = _commandService.Commands;
            var command = new List<CommandInfo>(commands).Find(x => x.Name.Equals(commandName, System.StringComparison.OrdinalIgnoreCase));

            if (command == null)
            {
                var commandNotFound = $"Command \"{commandName}\" not found.";
                await _logger.LogError(caller, nameof(HelpAsync), commandNotFound);
                await ReplyAsync(commandNotFound);
                return;
            }

            var embedBuilder = new EmbedBuilder
            {
                Title = command.Name,
                Description = command.Summary,
                Color = Defaults.Embed.Color
            };

            await _logger.LogInfo($"{Context.User.Username} invoked \"help\" for command \"{commandName}\".");
            await ReplyAsync(embed: embedBuilder.Build());

        }
    }
}
