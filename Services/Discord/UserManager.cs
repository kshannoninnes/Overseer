using System;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using Overseer.Models;
using Overseer.Services.Logging;

namespace Overseer.Services.Discord
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1822:Mark members as static", Justification = "Logically operates on an instance")]
    public class UserManager
    {
        private readonly DiscordSocketClient _client;
        private readonly EnforcedUserContext _db;
        private readonly ILogger _logger;

        public UserManager(DiscordSocketClient client, EnforcedUserContext db, ILogger logger)
        {
            _client = client;
            _db = db;
            _logger = logger;
            StartMaintainingAsync();
        }

        /// <summary>
        ///     Determine if a user is currently tracked
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> IsForcedAsync(ulong id)
        {
            var isEnforced = await UserExistsAsync(id);

            return isEnforced;
        }

        public async Task RenameUserAsync(IGuildUser user, string enforcedName)
        {
            var enforcedUser = new EnforcedUser
            {
                Id = user.Id.ToString(),
                Nickname = user.Nickname ?? string.Empty,
                EnforcedNickname = enforcedName
            };

            await AddUserAsync(enforcedUser);
            await user.ModifyAsync(x => x.Nickname = enforcedName);
        }

        public async Task RevertUserAsync(IGuildUser user)
        {
            var pred = await GetIdPredicate(user.Id);
            var removedUser = await RemoveUserAsync(user.Id);
            await user.ModifyAsync(x => x.Nickname = removedUser.Nickname);
        }

        // TODO Remove code duplication from these 2 methods
        public async Task<int> RenameAllUsersAsync(IEnumerable<IGuildUser> users, string enforcedName)
        {
            var numUsersRenamed = 0;
            foreach (var user in users)
            {
                var isEnforced = await UserExistsAsync(user.Id);

                var bot = await user.Guild.GetCurrentUserAsync();
                var canModify = await CanModify(bot as SocketGuildUser, user as SocketGuildUser);

                if (!isEnforced && canModify)
                {
                    await RenameUserAsync(user, enforcedName);
                    numUsersRenamed++;
                }
            }

            return numUsersRenamed;
        }

        public async Task<int> RevertAllUsersAsync(IEnumerable<IGuildUser> users)
        {
            var numUsersReverted = 0;
            foreach (var user in users)
            {
                var isEnforced = await UserExistsAsync(user.Id);

                var bot = await user.Guild.GetCurrentUserAsync();
                var canModify = await CanModify(bot as SocketGuildUser, user as SocketGuildUser);

                if (isEnforced && canModify)
                {
                    await RevertUserAsync(user);
                    numUsersReverted++;
                }
            }

            return numUsersReverted;
        }

        /// <summary>
        ///     Get an IEnumerable of IGuildUser's that the bot can modify
        /// </summary>
        /// <param name="guild">The guild in which the users and bot coexist</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result 
        ///     contains an IEnumerable of IGuildUsers who can be modified by the bot.
        /// </returns>
        public async Task<IEnumerable<IGuildUser>> GetActionableUsersAsync(IGuild guild)
        {
            var opt = new RequestOptions() { RetryMode = RetryMode.AlwaysRetry };
            var allUsers = await guild.GetUsersAsync(CacheMode.AllowDownload, options: opt) as IReadOnlyCollection<SocketGuildUser>;
            var bot = await guild.GetCurrentUserAsync() as SocketGuildUser;

            return allUsers.Where(x => !x.IsBot && x.Hierarchy < bot.Hierarchy);
        }

        /// <summary>
        ///     Check whether the bot can modify a user
        /// </summary>
        /// <param name="bot">The SocketGuildUser representation of the bot</param>
        /// <param name="user">The SocketGuildUser the bot wants to modify</param>
        /// <returns>
        ///     True if the bot can modify the user, false otherwise.
        /// </returns>
        public async Task<bool> CanModify(SocketGuildUser bot, SocketGuildUser user)
        {
            await Task.CompletedTask;
            var botAboveUser = bot.Hierarchy > user.Hierarchy;
            var botHasPerms = bot.GuildPermissions.ManageNicknames;

            return botHasPerms && botAboveUser;
        }

        /// <summary>
        ///     Start enforcement of user nicknames
        /// </summary>
        /// <returns>
        ///     A task representing the asynchronous start operation
        /// </returns>
        public Task StartMaintainingAsync()
        {
            _client.GuildMemberUpdated += MaintainNicknames;
            _client.UserJoined += SetNicknameOnNewUser;
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Event handler for GuildMemberUpdated, raised within the Discord.NET framework
        ///     when a SocketGuildUser is updated. Ensures the user's nickname isn't changed
        ///     away from the maintained name.
        /// </summary>
        /// <param name="before">A clone of the user pre-change</param>
        /// <param name="after">The user post-change</param>
        /// <returns>
        ///     A task representing the asynchronous set operation.
        /// </returns>
        private async Task MaintainNicknames(SocketGuildUser before, SocketGuildUser after)
        {
            if (await IsForcedAsync(after.Id))
            {
                var pred = await GetIdPredicate(after.Id);
                var enforcedNickname = (await GetUserAsync(after.Id)).EnforcedNickname;
                var hasEnforcedName = enforcedNickname != null;

                var bot = after.Guild.CurrentUser;
                var canModify = await CanModify(bot, after);

                var wrongNickname = after.Nickname == null || !after.Nickname.Equals(enforcedNickname);

                if (hasEnforcedName && canModify && wrongNickname)
                {
                    await after.ModifyAsync(x => x.Nickname = enforcedNickname);
                    await _logger.Log(LogSeverity.Info, $"Undoing {after.Username}'s nickname change from {after.Nickname}", nameof(MaintainNicknames));
                }
            }
        }

        /// <summary>
        ///     Event handler for UserJoined, raised within the Discord.NET framework
        ///     when a SocketGuildUser joins the guild. Sets the new user's nickname
        ///     to the maintained nickname.
        /// </summary>
        /// <param name="user">The user who joined the guild</param>
        /// <returns>
        ///     A task representing the asynchronous set operation.
        /// </returns>
        private async Task SetNicknameOnNewUser(SocketGuildUser user)
        {
            if (await IsForcedAsync(user.Id))
            {
                var id = await GetIdPredicate(user.Id);
                var enforcedNickname = (await GetUserAsync(user.Id)).EnforcedNickname;
                await user.ModifyAsync(x => x.Nickname = enforcedNickname);
                await _logger.Log(LogSeverity.Info, $"{user.Username}'s nickname set to {enforcedNickname}", nameof(SetNicknameOnNewUser));
            }
        }

        private Task<Expression<Func<EnforcedUser, bool>>> GetIdPredicate(ulong id)
        {
            Expression<Func<EnforcedUser, bool>> predicate = x => x.Id.Equals(id.ToString());

            return Task.FromResult(predicate);
        }

        private async Task AddUserAsync(EnforcedUser user)
        {
            await _db.AddAsync(user);
            await _db.SaveChangesAsync();
        }

        private async Task<EnforcedUser> RemoveUserAsync(ulong id)
        {
            var user = await _db.FindAsync<EnforcedUser>(id.ToString());
            _db.Remove(user);
            await _db.SaveChangesAsync();
            return user;
        }

        private Task<bool> UserExistsAsync(ulong id)
        {
            return Task.FromResult(_db.EnforcedUsers.Select(x => x.Id.Equals(id.ToString())).Any());
        }

        private Task<EnforcedUser> GetUserAsync(ulong id)
        {
            return Task.FromResult(_db.Find<EnforcedUser>(id.ToString()));
        }
    }
}
