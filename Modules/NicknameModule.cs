using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Overseer.Services;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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

        [Name("Rename"), Command("rename"), Summary("Enforce a non-admin user's nickname.\n\n**Usage**: >rename [all/@user] [name]")]
        public async Task RenameAsync([Summary("the user to be renamed")] SocketGuildUser user, [Remainder][Summary("The name to set on the user")] string name)
        {
            try
            {
                var isEnforced = await _us.IsTrackedAsync(user.Id);
                if (isEnforced)
                {
                    await ReplyAsync($"Already enforcing {user.Username}. Type `>revert @{user.Username}` to stop nickname enforcement.");
                    await _logger.Log(new LogMessage(LogSeverity.Info, $"{Context.User.Username}", $"{user.Username} already tracked."));
                    return;
                }

                var bot = Context.Guild.CurrentUser;
                var canModify = await _us.CanModify(bot, user);
                if (canModify)
                {
                    await _us.RenameUserAsync(user, name);
                    await _logger.Log(new LogMessage(LogSeverity.Info, $"{Context.User.Username}", $"{user.Username} renamed to {name}"));
                }
                else
                {
                    var msg = $"Unable to modify {user.Username} due to permissions hierarchy.";
                    await ReplyAsync(msg);
                    await _logger.Log(new LogMessage(LogSeverity.Info, $"{Context.User.Username}", msg));
                }
            }
            catch(Exception e)
            {
                await ReplyAsync("Error running command \"rename\"");
                await _logger.Log(new LogMessage(LogSeverity.Error, $"{Context.User.Username}", $"Error running \"rename @{user.Username}\": {e.ToString()}"));
            }
        }

        [Name("Rename"), Command("rename", RunMode = RunMode.Async)]
        public async Task RenameAsync(string target, [Remainder][Summary("The name to give to users")] string name)
        {
            try
            {
                var arg = target.Replace("\'", string.Empty).Replace("\"", string.Empty);
                if (!arg.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    await ReplyAsync($"Invalid target. Type `>rename @user` or `>rename all` to start nickname enforcement.");
                    return;
                }

                if (!(await semaphore.WaitAsync(0))) // Ensure only 1 instance of a long running command is active at any given time
                {
                    await _logger.Log(new LogMessage(LogSeverity.Warning, $"{Context.User.Username}", $">rename invoked before previous operation complete."));
                    await ReplyAsync("A command is still in progress.");
                    return;
                }

                var stopwatch = new Stopwatch();
                await _logger.Log(new LogMessage(LogSeverity.Info, $"{Context.User.Username}", $"Renaming of all guild users began at {DateTime.Now}"));
                stopwatch.Start();
                await ReplyAsync($"Renaming all users to {name}.");

                var actionableUsers = await _us.GetActionableUsersAsync(Context.Guild);
                var totalUsersRenamed = await _us.RenameAllUsersAsync(actionableUsers, name);

                stopwatch.Stop();
                await ReplyAsync("All users renamed.");
                var duration = Math.Round(stopwatch.Elapsed.TotalSeconds, 1);
                await _logger.Log(new LogMessage(LogSeverity.Info, $"{Context.User.Username}", $"Renamed {totalUsersRenamed} users in {duration} seconds."));

            }
            catch (Exception e)
            {
                await ReplyAsync("Error running command \"rename all\"");
                await _logger.Log(new LogMessage(LogSeverity.Error, $"{Context.User.Username}", $"Error running command: {e.ToString()}"));
            }
            finally
            {
                semaphore.Release(1);
            }
        }
        
        [Name("Revert"), Command("revert"), Summary("Stop enforcing a non-admin user's nickname.\n\n**Usage**: >revert [all/@user]")]
        public async Task RevertAsync([Summary("the user to be reverted")] SocketGuildUser user)
        {
            try
            {
                var isEnforced = await _us.IsTrackedAsync(user.Id);
                if (!isEnforced)
                {
                    await ReplyAsync($"{user.Nickname ?? user.Username} is not currently being enforced. Type `>rename @{user.Username} [name to enforce]` to begin nickname enforcement.");
                    await _logger.Log(new LogMessage(LogSeverity.Info, $"{Context.User.Username}", $"{user.Username} not currently tracked."));
                    return;
                }

                var bot = Context.Guild.CurrentUser;
                var canModify = await _us.CanModify(bot, user);
                if (canModify)
                {
                    await _us.RevertUserAsync(user);
                    await _logger.Log(new LogMessage(LogSeverity.Info, $"{Context.User.Username}", $"{user.Username}'s nickname restored."));
                }
                else
                {
                    var msg = $"Unable to modify {user.Username} due to permissions hierarchy.";
                    await ReplyAsync(msg);
                    await _logger.Log(new LogMessage(LogSeverity.Info, $"{Context.User.Username}", msg));
                }
            }
            catch (Exception e)
            {
                await ReplyAsync("Error running command \"revert\"");
                await _logger.Log(new LogMessage(LogSeverity.Error, $"{Context.User.Username}", $"Error running command: {e.ToString()}"));
            }
        }

        [Name("Revert"), Command("revert", RunMode = RunMode.Async)]
        public async Task RevertAsync(string target)
        {
            try
            {
                var arg = target.Replace("\'", string.Empty).Replace("\"", string.Empty);
                if (!arg.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    await ReplyAsync($"Invalid target. Type `>revert @user` or `>revert all` to start nickname enforcement.");
                    return;
                }

                if (!(await semaphore.WaitAsync(0))) // Ensure only 1 instance of a long running command is active at any given time
                {
                    await _logger.Log(new LogMessage(LogSeverity.Warning, $"{Context.User.Username}", $">revert invoked before previous operation complete."));
                    await ReplyAsync("A command is still in progress.");
                    return;
                }

                var stopwatch = new Stopwatch();
                await _logger.Log(new LogMessage(LogSeverity.Info, $"{Context.User.Username}", $"Reverting of all user nicknames began at {DateTime.Now}"));
                stopwatch.Start();
                await ReplyAsync("Reverting all user nicknames.");

                var actionableUsers = await _us.GetActionableUsersAsync(Context.Guild);
                var totalUsersReverted = await _us.RevertAllUsersAsync(actionableUsers);

                stopwatch.Stop();
                await ReplyAsync("All users reverted.");
                var duration = Math.Round(stopwatch.Elapsed.TotalSeconds, 1);
                await _logger.Log(new LogMessage(LogSeverity.Info, $"{Context.User.Username}", $"Renamed {totalUsersReverted} users in {duration} seconds."));
            }
            catch (Exception e)
            {
                await ReplyAsync("Error running command \"revert all\"");
                await _logger.Log(new LogMessage(LogSeverity.Error, $"{Context.User.Username}", $"Error running command: {e.ToString()}"));
            }
            finally
            {
                semaphore.Release(1);
            }
        }
    }
}
