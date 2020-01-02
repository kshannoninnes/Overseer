using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Overseer.Handlers;
using Overseer.Models;
using System.Linq.Expressions;
using System;

namespace Overseer.Services
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1822:Mark members as static", Justification = "Logically operates on an instance")]
    public class UserService
    {
        private readonly DiscordSocketClient _client;
        private readonly DatabaseHandler _db;
        private readonly ILogger _logger;

        public UserService(DiscordSocketClient client, DatabaseHandler db, ILogger logger)
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
        public async Task<bool> IsTrackedAsync(ulong id)
        {
            var pred = await GetIdPredicate(id);
            var isEnforced = await _db.Exists(pred);

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

            await _db.InsertAsync(enforcedUser);
            await user.ModifyAsync(x => x.Nickname = enforcedName);
        }

        public async Task RevertUserAsync(IGuildUser user)
        {
            var pred = await GetIdPredicate(user.Id);
            var enforcedUser = await _db.GetAsync(pred);
            await _db.RemoveAsync(enforcedUser);
            await user.ModifyAsync(x => x.Nickname = enforcedUser.Nickname);
        }

        public async Task<int> RenameAllUsersAsync(IEnumerable<IGuildUser> users, string enforcedName)
        {
            var numUsersRenamed = 0;
            foreach(var user in users)
            {
                var pred = await GetIdPredicate(user.Id);
                var isEnforced = await _db.Exists(pred);

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
                var pred = await GetIdPredicate(user.Id);
                var isEnforced = await _db.Exists(pred);

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
            var allUsers = (await guild.GetUsersAsync(CacheMode.AllowDownload, options: opt)) as IEnumerable<SocketGuildUser>;
            var bot = (await guild.GetCurrentUserAsync()) as SocketGuildUser;

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
            _client.GuildMemberUpdated += MaintainAllNicknames;
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
        private async Task MaintainAllNicknames(SocketGuildUser before, SocketGuildUser after)
        {
            if (await IsTrackedAsync(after.Id))
            {
                var pred = await GetIdPredicate(after.Id);
                var enforcedNickname = (await _db.GetAsync(pred)).EnforcedNickname;
                var hasEnforcedName = enforcedNickname != null;

                var bot = after.Guild.CurrentUser;
                var canModify = await CanModify(bot, after);

                var wrongNickname = after.Nickname == null || !after.Nickname.Equals(enforcedNickname);

                if (hasEnforcedName && canModify && wrongNickname)
                {
                    await after.ModifyAsync(x => x.Nickname = enforcedNickname);
                    await _logger.Log(LogSeverity.Info, $"{after.Username}'s nickname changed to {enforcedNickname}", nameof(MaintainAllNicknames), "Discord.NET");
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
            if (await IsTrackedAsync(user.Id))
            {
                var id = await GetIdPredicate(user.Id);
                var enforcedNickname = (await _db.GetAsync(id)).EnforcedNickname;
                await user.ModifyAsync(x => x.Nickname = enforcedNickname);
                await _logger.Log(LogSeverity.Info, $"{user.Username}'s nickname set to {enforcedNickname}", nameof(SetNicknameOnNewUser), "Discord.NET");
            }
        }

        private async Task<Expression<Func<EnforcedUser, bool>>> GetIdPredicate(ulong id)
        {
            await Task.CompletedTask;
            var idStr = id.ToString();

            return (x) => x.Id.Equals(idStr);
        }
    }
}
