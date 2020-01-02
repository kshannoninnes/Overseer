using GraphQL.Client;
using GraphQL.Common.Request;
using Newtonsoft.Json;
using Overseer.Models;
using System.Threading.Tasks;

namespace Overseer.Services
{
    public class MangaService : MediaFetcher
    {
        private const string SearchType = "MANGA";

        public MangaService(ILogger logger) : base(logger) { }

        public override async Task<OverseerMedia> GetMediaAsync(string title)
        {
            return await GetMediaAsync(title, SearchType);
        }
    }
}