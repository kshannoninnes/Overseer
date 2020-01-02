using Discord;
using Discord.Commands;
using Overseer.Exceptions;
using Overseer.Models;
using Overseer.Services;
using System.Threading.Tasks;

namespace Overseer.Modules
{
    [Name("Weebshit")]
    public class AnilistModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger _logger;
        private readonly EmbedService _embedService;
        private readonly IMediaFetcher _mangaFetcher;
        private readonly IMediaFetcher _animeFetcher;

        public AnilistModule(ILogger logger, EmbedService es, MangaService mf, AnimeService af)
        {
            _logger = logger;
            _embedService = es;
            _mangaFetcher = mf;
            _animeFetcher = af;
        }

        [Name("Manga"), Command("manga"), Summary("Find manga by name.\n\n**Usage**: >manga [title]")]
        public async Task GetMangaAsync([Summary("Manga title to search for")] [Remainder] string mangaTitle) => await GetMediaAsync(mangaTitle, ReleaseType.Manga, _mangaFetcher);

        [Name("Anime"), Command("anime"), Summary("Find anime by name.\n\n**Usage**: >anime [title]")]
        public async Task GetAnimeAsync([Summary("Anime title to search for")] [Remainder] string animeTitle) => await GetMediaAsync(animeTitle, ReleaseType.Anime, _animeFetcher);

        private async Task GetMediaAsync(string title, ReleaseType type, IMediaFetcher fetcher)
        {
            var caller = Context.User.Username;
            var methodName = nameof(GetMediaAsync);

            try
            {
                var media = await fetcher.GetMediaAsync(title);
                var embed = await _embedService.CraftEmbed(media, type);

                await _logger.Log(LogSeverity.Info, $"\"{title}\" matched to \"{media.Title.Romaji}\".", methodName, caller);
                await ReplyAsync(embed: embed);
            }
            catch(UpstreamApiException e)
            {
                await _logger.Log(LogSeverity.Error, e.Message, methodName, caller);
                await ReplyAsync(e.Message);
            }
        }
    }
}
