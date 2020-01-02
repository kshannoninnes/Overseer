using System.Threading.Tasks;

using Overseer.Models;

namespace Overseer.Services.WeebApi
{
    public class MangaFetcher : IMediaFetcher
    {
        private const string SearchType = "MANGA";

        private readonly AbstractApiService _fetcher;

        public MangaFetcher(AbstractApiService fetcher)
        {
            _fetcher = fetcher;
        }

        public async Task<OverseerMedia> GetAsync(string title)
        {
            return await _fetcher.GetMediaAsync(title, SearchType);
        }
    }
}