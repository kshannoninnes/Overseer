using System.Threading.Tasks;

using Overseer.Models;
using Overseer.Services.Logging;

namespace Overseer.Services.WeebApi
{
    public class MangaFetcher : MediaFetcher
    {
        private const string SearchType = "MANGA";

        public MangaFetcher(ILogger logger) : base(logger) { }

        public override async Task<OverseerMedia> GetMediaAsync(string title)
        {
            return await GetMediaAsync(title, SearchType);
        }
    }
}