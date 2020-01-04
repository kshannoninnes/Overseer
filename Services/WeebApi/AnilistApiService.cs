using Newtonsoft.Json;
using System.Threading.Tasks;

using GraphQL.Client;
using GraphQL.Common.Request;

using Overseer.Models;
using Overseer.Services.Logging;

namespace Overseer.Services.WeebApi
{
    public class AnilistApiService : AbstractApiService
    {
        public AnilistApiService(ILogger logger) : base(logger) { }

        public override async Task<OverseerMedia> GetMediaAsync(string search, string type)
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
    }
}
