using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Overseer.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// TODO Replace if-elses with a decorator-style validator
// TODO Change command names
namespace Overseer.Modules
{
    [Name("Nicknames"), RequireOwner(Group = "Permission"), RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
    public class NicknameModule : ModuleBase<SocketCommandContext>
    {
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        private readonly UserService _us;
        private readonly LoggingService _logger;

        public NicknameModule(UserService us, LoggingService logger)
        {
            _us = us;
            _logger = logger;
        }

        [Name("Rename"), Command("rename"), Summary("Enforce a non-admin user's nickname.\n\n**Usage**: >rename [all | @user] [name]")]
        public async Task RenameAsync([Summary("the user to be renamed")] SocketGuildUser user, [Remainder][Summary("The name to set on the user")] string name)
        {
            var bot = Context.Guild.CurrentUser;
            var caller = Context.User.Username;
            var target = user.Username;

            try
            {
                var botCanModifyUser = await _us.CanModify(bot, user);
                var userIsEnforced = await _us.IsTrackedAsync(user.Id);

                if (userIsEnforced)
                {
                    var targetAlreadyEnforced = $"{target} is already enforced";
                    await _logger.LogError(caller, nameof(RenameAsync), targetAlreadyEnforced);
                    await ReplyAsync($"{targetAlreadyEnforced}. Type `>revert @{target}` to stop nickname enforcement.");
                    return;
                }
                else if (!botCanModifyUser)
                {
                    var inadequatePermissions = $"Inadequate permissions to modify {target}";
                    await _logger.LogError(caller, nameof(RenameAsync), inadequatePermissions);
                    await ReplyAsync($"{inadequatePermissions}. Ensure bot has a higher role than the user and try again.");
                    return;
                }
                else
                {
                    await _us.RenameUserAsync(user, name);
                    await _logger.LogInfo($"{caller} renamed {target} to {name}");
                }
            }
            catch(CommandException e)
            {
                await _logger.LogError(caller, nameof(RenameAsync), e.Message);
                await ReplyAsync("Could not execute __**rename**__");
            }
        }

        [Name("Rename"), Command("rename", RunMode = RunMode.Async)]
        public async Task RenameAsync([Summary("all")] string target, [Remainder][Summary("The name to give to users")] string name)
        {
            var caller = Context.User.Username;
            var arg = target.Replace("\'", string.Empty).Replace("\"", string.Empty);

            try
            {
                if (!arg.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var invalidTarget = $"\"{target}\" is not a valid target.";
                    await _logger.LogError(caller, nameof(RenameAsync), invalidTarget);
                    await ReplyAsync($"{invalidTarget}.");
                    return;
                }

                if (!(await semaphore.WaitAsync(0))) // Ensure only 1 instance of a long running command is active at any given time
                {
                    var alreadyRunning = $"Another command is still in progress. Please wait until it's finished before trying again.";
                    await _logger.LogError(caller, nameof(RenameAsync), alreadyRunning);
                    await ReplyAsync($"{alreadyRunning}");
                    return;
                }

                var stopwatch = new Stopwatch();
                var msg = $"Beginning mass rename to {name}.";
                await _logger.LogInfo(msg);
                await ReplyAsync(msg);

                stopwatch.Start();
                var actionableUsers = await _us.GetActionableUsersAsync(Context.Guild);
                var totalUsersRenamed = await _us.RenameAllUsersAsync(actionableUsers, name);
                stopwatch.Stop();

                var duration = Math.Round(stopwatch.Elapsed.TotalSeconds, 1);
                var renameComplete = "Mass rename complete."; 
                await _logger.LogInfo($"{renameComplete} {totalUsersRenamed} users renamed over {duration}s.");
                await ReplyAsync(renameComplete);
            }
            catch (CommandException e)
            {
                await _logger.LogError(caller, nameof(RenameAsync), e.Message);
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
            var target = user.Username;

            try
            {
                var userIsEnforced = await _us.IsTrackedAsync(user.Id);
                var botCanModifyUser = await _us.CanModify(bot, user);

                if (!userIsEnforced)
                {
                    var targetNotEnforced = $"{target} is not currently being enforced";
                    await _logger.LogError(caller, nameof(RevertAsync), targetNotEnforced);
                    await ReplyAsync($"{targetNotEnforced}. Type `>rename @{user.Username} [name to enforce]` to begin nickname enforcement.");
                    return;
                }

                if (!botCanModifyUser)
                {
                    var inadequatePermissions = $"Inadequate permissions to modify {target}";
                    await _logger.LogError(caller, nameof(RenameAsync), inadequatePermissions);
                    await ReplyAsync($"{inadequatePermissions}. Ensure bot has a higher role than the user and try again.");
                    return;
                }
                else
                {
                    await _us.RevertUserAsync(user);
                    await _logger.LogInfo($"Restored nickname for {user.Username}");
                }
            }
            catch (CommandException e)
            {
                await _logger.LogError(caller, nameof(RevertAsync), e.Message);
                await ReplyAsync("Could not execute __**revert**__");
            }
        }

        [Name("Revert"), Command("revert", RunMode = RunMode.Async)]
        public async Task RevertAsync([Summary("all")] string target)
        {
            var caller = Context.User.Username;
            var arg = target.Replace("\'", string.Empty).Replace("\"", string.Empty);

            try
            {
                if (!arg.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var invalidTarget = $"\"{target}\" is not a valid target.";
                    await _logger.LogError(caller, nameof(RenameAsync), invalidTarget);
                    await ReplyAsync($"{invalidTarget}");
                    return;
                }

                if (!(await semaphore.WaitAsync(0))) // Ensure only 1 instance of a long running command is active at any given time
                {
                    var alreadyRunning = $"Another command is still in progress. Please wait until it's finished before trying again.";
                    await _logger.LogError(caller, nameof(RenameAsync), alreadyRunning);
                    await ReplyAsync($"{alreadyRunning}");
                    return;
                }

                var stopwatch = new Stopwatch();
                var msg = $"Beginning mass revert.";
                await _logger.LogInfo(msg);
                await ReplyAsync(msg);

                stopwatch.Start();
                var actionableUsers = await _us.GetActionableUsersAsync(Context.Guild);
                var totalUsersReverted = await _us.RevertAllUsersAsync(actionableUsers);
                stopwatch.Stop();

                var duration = Math.Round(stopwatch.Elapsed.TotalSeconds, 1);
                var revertComplete = "Mass revert complete.";
                await _logger.LogInfo($"{revertComplete} {totalUsersReverted} users reverted over {duration}s.");
                await ReplyAsync(revertComplete);
            }
            catch (CommandException e)
            {
                await _logger.LogError(caller, nameof(RevertAsync), e.Message);
                await ReplyAsync("Could not execute __**revert**__");
            }
            finally
            {
                semaphore.Release(1);
            }
        }
    }
}
