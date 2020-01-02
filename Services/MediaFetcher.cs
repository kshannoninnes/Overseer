using Discord;
using GraphQL.Client;
using GraphQL.Common.Request;
using GraphQL.Common.Response;
using Newtonsoft.Json;
using Overseer.Exceptions;
using Overseer.Models;
using System.Threading.Tasks;

namespace Overseer.Services
{
    public abstract class MediaFetcher : IMediaFetcher
    {
        public abstract Task<OverseerMedia> GetMediaAsync(string search);

        private readonly ILogger _logger;

        protected MediaFetcher(ILogger logger)
        {
            _logger = logger;
        }

        protected async Task<OverseerMedia> GetMediaAsync(string search, string type)
        {
            var request = new GraphQLRequest
            {
                Query = CraftQuery(ReleaseType.Manga),
                Variables = new
                {
                    search,
                    type
                },
                OperationName = "MediaSearch"
            };
            using var graphqlClient = new GraphQLClient("https://graphql.anilist.co");
            var response = await graphqlClient.PostAsync(request);
            await ValidateApiResponse(response);

            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            return JsonConvert.DeserializeObject<OverseerMedia>(response.Data.Media.ToString(), settings);
        }

        private async Task ValidateApiResponse(GraphQLResponse res)
        {
            if (res.Errors != null)
            {
                var message = string.Empty;

                foreach (var error in res.Errors)
                {
                    await _logger.Log(LogSeverity.Error, error.Message, nameof(ValidateApiResponse));
                }

                throw new UpstreamApiException("Error retrieving data from API.");
            }
        }

        private static string CraftQuery(ReleaseType type)
        {
            var releaseType = (type == ReleaseType.Manga) ? "chapters" : "episodes";

            return "query MediaSearch($search:String, $type:MediaType) " +
                   "{ " +
                        $"Media (search: $search, type: $type) " +
                        "{ " +
                            "title { romaji } " +
                            "format " +
                            "siteUrl " +
                            "description " +
                            "coverImage { extraLarge } " +
                            "status " +
                            $"{releaseType} " +
                            "tags { name rank isMediaSpoiler } " +
                            "genres " +
                        "}" +
                   "}";

        }
    }
}
