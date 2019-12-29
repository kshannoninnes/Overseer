using Discord;
using Discord.Commands;
using Overseer.Models;
using Overseer.Services;
using System.Threading.Tasks;

namespace Overseer.Modules
{
    [Name("Weebshit")]
    public class WeebModule : ModuleBase<SocketCommandContext>
    {
        private readonly LoggingService _logger;
        private readonly WeebService _ws;

        public WeebModule(LoggingService logger, WeebService ws)
        {
            _logger = logger;
            _ws = ws;
        }

        [Name("Manga"), Command("manga"), Summary("Find manga by name.\n\n**Usage**: >manga [title]")]
        public async Task GetMangaAsync([Summary("Manga title to search for")] [Remainder] string mangaTitle)
        {
            try
            {
                var type = ReleaseType.Manga;
                var manga = await _ws.GetMedia(mangaTitle, type);
                var embed = await _ws.BuildEmbed(manga, type);

                await _logger.Log(new LogMessage(LogSeverity.Info, Context.User.Username, $"{manga.title.romaji} retreived from upstream API."));
                await ReplyAsync(embed: embed);
            }
            catch(UpstreamApiException e)
            {
                await _logger.Log(new LogMessage(LogSeverity.Error, Context.User.Username, e.Message));
                await ReplyAsync("Error retreiving manga info.");
            }
        }

        [Name("Anime"), Command("anime"), Summary("Find anime by name.\n\n**Usage**: >anime [title]")]
        public async Task GetAnimeAsync([Summary("Anime title to search for")] [Remainder] string animeTitle)
        {
            try
            {
                var type = ReleaseType.Anime;
                var anime = await _ws.GetMedia(animeTitle, type);
                var embed = await _ws.BuildEmbed(anime, type);

                await _logger.Log(new LogMessage(LogSeverity.Info, Context.User.Username, $"{anime.title.romaji} retreived from upstream API."));
                await ReplyAsync(embed: embed);
            }
            catch(UpstreamApiException e)
            {
                await _logger.Log(new LogMessage(LogSeverity.Error, Context.User.Username, e.Message));
                await ReplyAsync("Error retrieving anime info.");
            }
        }
    }
}
