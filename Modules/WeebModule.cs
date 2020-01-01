using Discord;
using Discord.Commands;
using Overseer.Exceptions;
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
        public async Task GetMangaAsync([Summary("Manga title to search for")] [Remainder] string mangaTitle) => await GetMediaAsync(mangaTitle, ReleaseType.Manga);

        [Name("Anime"), Command("anime"), Summary("Find anime by name.\n\n**Usage**: >anime [title]")]
        public async Task GetAnimeAsync([Summary("Anime title to search for")] [Remainder] string animeTitle) => await GetMediaAsync(animeTitle, ReleaseType.Anime);

        private async Task GetMediaAsync(string title, ReleaseType type)
        {
            var caller = Context.User.Username;

            try
            {
                var media = await _ws.GetMedia(title, type);
                var embed = await _ws.BuildEmbed(media, type);

                await _logger.LogInfo($"{type.ToString()} \"{media.Title.Romaji}\" retreived from upstream API.");
                await ReplyAsync(embed: embed);
            }
            catch(UpstreamApiException e)
            {
                await _logger.LogError(caller, nameof(GetMediaAsync), e.Message);
                await ReplyAsync($"Error retreiving {type.ToString()} info.");
            }
        }
    }
}
