using System.Threading.Tasks;

using Discord;
using Discord.Commands;

using Overseer.Models;
using Overseer.Exceptions;
using Overseer.Services.WeebApi;
using Overseer.Services.Discord;
using Overseer.Services.Logging;

namespace Overseer.Commands
{
    [Name("Weebshit")]
    public class AnilistCommands : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger _logger;
        private readonly EmbedManager _embedService;
        private readonly IMediaFetcher _mangaFetcher;
        private readonly IMediaFetcher _animeFetcher;

        public AnilistCommands(ILogger logger, EmbedManager es, MangaFetcher mf, AnimeFetcher af)
        {
            _logger = logger;
            _embedService = es;
            _mangaFetcher = mf;
            _animeFetcher = af;
        }

        [Name("Manga"), Command("manga"), Summary("Find manga by name.\n\n**Usage**: .manga [title]")]
        public async Task GetMangaAsync([Summary("Manga title to search for")] [Remainder] string mangaTitle) => await GetMediaAsync(mangaTitle, _mangaFetcher);

        [Name("Anime"), Command("anime"), Summary("Find anime by name.\n\n**Usage**: .anime [title]")]
        public async Task GetAnimeAsync([Summary("Anime title to search for")] [Remainder] string animeTitle) => await GetMediaAsync(animeTitle, _animeFetcher);

        private async Task GetMediaAsync(string title, IMediaFetcher fetcher)
        {
            var caller = Context.User.Username;
            var methodName = nameof(GetMediaAsync);

            try
            {
                var media = await fetcher.GetAsync(title);
                var embed = await _embedService.CraftEmbed(media);

                await _logger.Log(LogSeverity.Info, $"{title} matched to {media.Title.Romaji}", methodName, caller);
                await ReplyAsync(embed: embed);
            }
            catch (UpstreamApiException e)
            {
                await _logger.Log(LogSeverity.Error, e.Message, methodName, caller);
                await ReplyAsync(e.Message);
            }
        }
    }
}
