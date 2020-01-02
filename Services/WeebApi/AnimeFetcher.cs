using System.Threading.Tasks;

using Overseer.Models;
using Overseer.Services.Logging;

namespace Overseer.Services.WeebApi
{
    public class AnimeFetcher : MediaFetcher
    {
        private const string SearchType = "ANIME";

        public AnimeFetcher(ILogger logger) : base(logger) { }

        public async override Task<OverseerMedia> GetMediaAsync(string title)
        {
            return await GetMediaAsync(title, SearchType);
        }
    }
}
