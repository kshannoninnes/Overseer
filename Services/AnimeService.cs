using GraphQL.Client;
using GraphQL.Common.Request;
using Newtonsoft.Json;
using Overseer.Models;
using System.Threading.Tasks;

namespace Overseer.Services
{
    public class AnimeService : MediaFetcher
    {
        private const string SearchType = "ANIME";

        public AnimeService(ILogger logger) : base(logger) { }

        public async override Task<OverseerMedia> GetMediaAsync(string title)
        {
            return await GetMediaAsync(title, SearchType);
        }
    }
}
