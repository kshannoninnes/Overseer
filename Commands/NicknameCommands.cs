using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Overseer.Services.Discord;
using Overseer.Services.Logging;

// TODO Replace if-elses with a decorator-style validator
namespace Overseer.Commands
{
    [Name("Nicknames"), RequireOwner(Group = "Permission"), RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
    public class NicknameCommands : ModuleBase<SocketCommandContext>
    {
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        private readonly ILogger _logger;
        private readonly UserManager _us;

        public NicknameCommands(UserManager us, ILogger logger)
        {
            _logger = logger;
            _us = us;
        }

        [Name("Rename"), Command("rename"), Summary("Enforce a non-admin user's nickname.\n\n**Usage**: >rename [all | @user] [name]")]
        public async Task RenameAsync([Summary("the user to be renamed")] SocketGuildUser user, [Remainder][Summary("The name to set on the user")] string name)
        {
            var bot = Context.Guild.CurrentUser;
            var caller = Context.User.Username;
            var method = nameof(RenameAsync);
            var target = user.Username;

            try
            {
                var botCanModifyUser = await _us.CanModify(bot, user);
                var userIsEnforced = await _us.IsTrackedAsync(user.Id);

                if (userIsEnforced)
                {
                    var targetAlreadyEnforced = $"{target} is already enforced.";
                    await _logger.Log(LogSeverity.Error, targetAlreadyEnforced, method, caller);
                    await ReplyAsync($"{targetAlreadyEnforced}. Type `>revert @{target}` to stop nickname enforcement.");
                    return;
                }
                else if (!botCanModifyUser)
                {
                    var inadequatePermissions = $"Inadequate permissions to modify {target}.";
                    await _logger.Log(LogSeverity.Error, inadequatePermissions, method, caller);
                    await ReplyAsync($"{inadequatePermissions}. Ensure bot has a higher role than the user and try again.");
                    return;
                }
                else
                {
                    await _us.RenameUserAsync(user, name);
                    await _logger.Log(LogSeverity.Info, $"{target} renamed to {name}.", method, caller);
                }
            }
            catch (CommandException e)
            {
                await _logger.Log(LogSeverity.Error, e.Message, method, caller);
                await ReplyAsync("Could not execute __**rename**__");
            }
        }

        [Name("Rename"), Command("rename", RunMode = RunMode.Async)]
        public async Task RenameAsync([Summary("all")] string target, [Remainder][Summary("The name to give to users")] string name)
        {
            var caller = Context.User.Username;
            var method = nameof(RenameAsync);
            var arg = target.Replace("\'", string.Empty).Replace("\"", string.Empty);

            try
            {
                if (!arg.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var invalidTarget = $"\"{target}\" is not a valid target.";
                    await _logger.Log(LogSeverity.Error, invalidTarget, method, caller);
                    await ReplyAsync($"{invalidTarget}.");
                    return;
                }

                if (!await semaphore.WaitAsync(0)) // Ensure only 1 instance of a long running command is active at any given time
                {
                    var alreadyRunning = $"Another command is still in progress. Please wait until it's finished before trying again.";
                    await _logger.Log(LogSeverity.Error, alreadyRunning, method, caller);
                    await ReplyAsync($"{alreadyRunning}");
                    return;
                }

                var stopwatch = new Stopwatch();
                var msg = $"Beginning mass rename to {name}.";
                await _logger.Log(LogSeverity.Info, msg, method, caller);
                await ReplyAsync(msg);

                stopwatch.Start();
                var actionableUsers = await _us.GetActionableUsersAsync(Context.Guild);
                var totalUsersRenamed = await _us.RenameAllUsersAsync(actionableUsers, name);
                stopwatch.Stop();

                var duration = Math.Round(stopwatch.Elapsed.TotalSeconds, 1);
                await _logger.Log(LogSeverity.Info, $"Renamed {totalUsersRenamed} in {duration}s.", method, caller);
                await ReplyAsync("Mass rename complete.");
            }
            catch (CommandException e)
            {
                await _logger.Log(LogSeverity.Error, e.Message, method, caller);
                await ReplyAsync("Could not execute __**rename**__");
            }
            finally
            {
                semaphore.Release(1);
            }
        }

        [Name("Revert"), Command("revert"), Summary("Stop enforcing a non-admin user's nickname.\n\n**Usage**: >revert [all | @user]")]
        public async Task RevertAsync([Summary("the user to be reverted")] SocketGuildUser user)
        {
            var bot = Context.Guild.CurrentUser;
            var caller = Context.User.Username;
            var method = nameof(RevertAsync);
            var target = user.Username;

            try
            {
                var userIsEnforced = await _us.IsTrackedAsync(user.Id);
                var botCanModifyUser = await _us.CanModify(bot, user);

                if (!userIsEnforced)
                {
                    var targetNotEnforced = $"{target} is not currently being enforced.";
                    await _logger.Log(LogSeverity.Error, targetNotEnforced, method, caller);
                    await ReplyAsync($"{targetNotEnforced}. Type `>rename @{user.Username} [name to enforce]` to begin nickname enforcement.");
                    return;
                }

                if (!botCanModifyUser)
                {
                    var inadequatePermissions = $"Inadequate permissions to modify {target}.";
                    await _logger.Log(LogSeverity.Error, inadequatePermissions, method, caller);
                    await ReplyAsync($"{inadequatePermissions}. Ensure bot has a higher role than the user and try again.");
                    return;
                }
                else
                {
                    await _us.RevertUserAsync(user);
                    await _logger.Log(LogSeverity.Info, $"Restored nickname for {user.Username}.", method, caller);
                }
            }
            catch (CommandException e)
            {
                await _logger.Log(LogSeverity.Error, e.Message, method, caller);
                await ReplyAsync("Could not execute __**revert**__");
            }
        }

        [Name("Revert"), Command("revert", RunMode = RunMode.Async)]
        public async Task RevertAsync([Summary("all")] string target)
        {
            var caller = Context.User.Username;
            var method = nameof(RevertAsync);
            var arg = target.Replace("\'", string.Empty).Replace("\"", string.Empty);

            try
            {
                if (!arg.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var invalidTarget = $"\"{target}\" is not a valid target.";
                    await _logger.Log(LogSeverity.Error, invalidTarget, method, caller);
                    await ReplyAsync(invalidTarget);
                    return;
                }

                if (!await semaphore.WaitAsync(0)) // Ensure only 1 instance of a long running command is active at any given time
                {
                    var alreadyRunning = $"Another command is still in progress. Please wait until it's finished before trying again.";
                    await _logger.Log(LogSeverity.Error, alreadyRunning, method, caller);
                    await ReplyAsync($"{alreadyRunning}");
                    return;
                }

                var stopwatch = new Stopwatch();
                var msg = $"Beginning mass revert.";
                await _logger.Log(LogSeverity.Info, msg, method, caller);
                await ReplyAsync(msg);

                stopwatch.Start();
                var actionableUsers = await _us.GetActionableUsersAsync(Context.Guild);
                var totalUsersReverted = await _us.RevertAllUsersAsync(actionableUsers);
                stopwatch.Stop();

                var duration = Math.Round(stopwatch.Elapsed.TotalSeconds, 1);
                await _logger.Log(LogSeverity.Info, $"{totalUsersReverted} users reverted over {duration}s.", method, caller);
                await ReplyAsync("Mass revert complete.");
            }
            catch (CommandException e)
            {
                await _logger.Log(LogSeverity.Error, e.Message, method, caller);
                await ReplyAsync("Could not execute __**revert**__");
            }
            finally
            {
                semaphore.Release(1);
            }
        }
    }
}
