using System.Threading.Tasks;

using Overseer.Models;

namespace Overseer.Services.WeebApi
{
    public class AnimeFetcher : IMediaFetcher
    {
        private const string SearchType = "ANIME";

        private readonly IApiService _fetcher;

        public AnimeFetcher(IApiService fetcher)
        {
            _fetcher = fetcher;
        }

        public async Task<OverseerMedia> GetAsync(string title)
        {
            return await _fetcher.GetMediaAsync(title, SearchType);
        }
    }
}
