using Discord;
using GraphQL.Common.Response;
using Overseer.Exceptions;
using Overseer.Models;
using Overseer.Services.Logging;
using System.Threading.Tasks;

namespace Overseer.Services.WeebApi
{
    public abstract class AbstractApiService
    {
        public abstract Task<OverseerMedia> GetMediaAsync(string search, string type);

        private readonly ILogger _logger;

        protected AbstractApiService(ILogger logger)
        {
            _logger = logger;
        }

        protected async Task ValidateApiResponse(GraphQLResponse res)
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

        protected static string CraftQuery(ReleaseType type)
        {
            var releaseType = type == ReleaseType.Manga ? "chapters" : "episodes";

            return "query MediaSearch($search:String, $type:MediaType) " +
                   "{ " +
                        "Media (search: $search, type: $type) " +
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
