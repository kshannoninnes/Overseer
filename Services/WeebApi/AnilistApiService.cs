using Newtonsoft.Json;
using System.Threading.Tasks;

using GraphQL.Client;
using GraphQL.Common.Request;
using GraphQL.Common.Response;

using Discord;

using Overseer.Models;
using Overseer.Services.Logging;
using Overseer.Exceptions;
using System;

namespace Overseer.Services.WeebApi
{
    public class AnilistApiService : IApiService
    {
        private readonly ILogger _logger;

        public AnilistApiService(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<OverseerMedia> GetMediaAsync(string search, string type)
        {
            // Get type string here, pass in ReleaseType instead
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
                foreach (var error in res.Errors)
                {
                    await _logger.Log(LogSeverity.Error, error.Message, nameof(ValidateApiResponse));
                }

                throw new UpstreamApiException("Error retrieving data from API.");
            }
        }

        private static string CraftQuery(ReleaseType type)
        {
            var releaseType = type == ReleaseType.Manga ? "chapters" : "episodes";

            return "query MediaSearch($search:String, $type:MediaType) " +
                   "{ " +
                        "Media (search: $search, type: $type) " +
                        "{ " +
                            "title { romaji } " +
                            "format " +
                            "description " +
                            "nextAiringEpisode { timeUntilAiring } " +
                            "siteUrl " +
                            "coverImage { extraLarge } " +
                            "status " +
                            "tags { name rank isMediaSpoiler } " +
                            "genres " +
                        "}" +
                   "}";

        }
    }
}
