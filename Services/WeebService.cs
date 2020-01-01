using Discord;
using GraphQL.Client;
using GraphQL.Common.Request;
using GraphQL.Common.Response;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Overseer.Handlers;
using Overseer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Overseer.Services
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1822:Mark members as static", Justification = "Logically operates on an instance")]
    public class WeebService
    {
        private readonly LoggingService _logger;

        public WeebService(LoggingService logger)
        {
            _logger = logger;
        }

        public async Task<OverseerMedia> GetMedia(string title, ReleaseType type)
        {
            var request = new GraphQLRequest
            {
                Query = await CraftQuery(type),
                Variables = new
                {
                    search = title
                }
            };
            using var graphqlClient = new GraphQLClient("https://graphql.anilist.co");
            var response = await graphqlClient.PostAsync(request);
            await ValidateResponse(response);

            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            return JsonConvert.DeserializeObject<OverseerMedia>(response.Data.Media.ToString(), settings);
        }

        public async Task<Embed> BuildEmbed(OverseerMedia media, ReleaseType type)
        {
            // attributes
            var title = $"**{media.Title.Romaji}**";
            var url = media.SiteUrl;
            var desc = await FormatDescription(media.Description);
            var color = Defaults.Embed.Color;
            var thumbnail = media.CoverImage.ExtraLarge;

            // fields
            var status = await GetStatus(media.Status);
            var format = await GetFormat(media.Format);
            var releaseTypeName = await GetType(type);
            var releaseNum = await GetNumberOfReleases(media.NumReleases);
            var genres = await GetGenres(media.Genres);
            var tags = await GetTags(media.Tags);

            var eb = new EmbedBuilder
            {
                Title = title,
                Url = url.ToString(),
                Description = desc,
                Color = color,
                ThumbnailUrl = thumbnail
            };

            // TODO refactor for consistency: get an embed field builder from all the above "GetX" methods, then add them using the AddField(EFB) method
            await AddField(eb, "Status", status, true);
            await AddField(eb, "Type", format, true);
            await AddField(eb, releaseTypeName, releaseNum, true);
            await AddField(eb, "Genres", genres, true);
            await AddField(eb, "Tags", tags, true);
            await AddEmptyField(eb, true);

            return eb.Build();
        }

        private async Task AddField(EmbedBuilder eb, string name, string value, bool inline)
        {
            await Task.CompletedTask;
            if(!string.IsNullOrWhiteSpace(name))
            {
                value = string.IsNullOrWhiteSpace(value) ? "N/A" : value;
                eb.AddField(name, value, inline);
            }
        }

        private async Task AddEmptyField(EmbedBuilder eb, bool inline)
        {
            await AddField(eb, "\u200b", "\u200b", inline);
        }

        private async Task<string> CraftQuery(ReleaseType type)
        {
            await Task.CompletedTask;
            string releaseType;
            string mediaType;

            switch (type)
            {
                case ReleaseType.Manga:
                    releaseType = "chapters";
                    mediaType = "MANGA";
                    break;
                default:
                    releaseType = "episodes";
                    mediaType = "ANIME";
                    break;
            }

            return "query MediaSearch($search: String) { " +
                        $"Media (search: $search, type: {mediaType}) {{ " +
                        "title { romaji } " +
                        "format " +
                        "siteUrl " +
                        "description " +
                        "coverImage { extraLarge } " +
                        "status " +
                        $"{releaseType} " +
                        "tags { name rank isMediaSpoiler } " +
                        "genres " +
                    "} }";

        }

        private async Task ValidateResponse(GraphQLResponse res)
        {
            await Task.CompletedTask;
            if (res.Errors != null)
            {
                var message = string.Empty;

                foreach (var error in res.Errors)
                {
                    message += error.Message + "\n";
                }

                throw new UpstreamApiException(message[0..^1]);
            }
        }

        private async Task<string> RemoveBlacklistedSubstrings(string text)
        {
            await Task.CompletedTask;
            List<string> blacklistedSubstrings = new List<string> { 
                "<.*?>", 
                "\\(Source.*?\\)" 
            };

            foreach(var entry in blacklistedSubstrings)
            {
                text = Regex.Replace(text, entry, string.Empty);
            }

            return text;
        }

        private async Task<string> GetStatus(string status)
        {
            status = status.Replace("_", " ");
            return await CapitalizeFirstLetter(status);
        }

        private async Task<string> GetFormat(string type)
        {
            return await CapitalizeFirstLetter(type);
        }

        private async Task<string> GetType(ReleaseType type)
        {
            await Task.CompletedTask;
            return (type == ReleaseType.Manga) ? "Chapters" : "Episodes";
        }

        private async Task<string> GetNumberOfReleases(int numReleases)
        {
            await Task.CompletedTask;
            return (numReleases == 0) ? "TBD" : numReleases.ToString(); // If episodes or chapters == 0, then it's still being released
        }

        private async Task<string> GetGenres(List<string> genreList)
        {
            await Task.CompletedTask;
            return string.Join(", ", genreList);
        }

        private async Task<string> GetTags(List<Tag> tagList)
        {
            await Task.CompletedTask;
            return string.Join(", ", (tagList.Where(t => !t.IsMediaSpoiler && t.Rank > 50).Select(t => t.Name)));
        }

        private async Task<string> CapitalizeFirstLetter(string text)
        {
            await Task.CompletedTask;
            string capitalizedText = (text.Length < 3) ? text.ToUpper() : char.ToUpper(text[0]) + text.Substring(1).ToLower();
            
            return capitalizedText;
        }

        private async Task<string> FormatInlineFieldLength(string text)
        {
            var maxLength = 30;
            var separator = ", ";
            
            return await FormatLength(text, maxLength, separator);
        }

        private async Task<string> FormatLength(string text, int maxLength, string separator, bool breakOnOverflow = false)
        {
            await Task.CompletedTask;
            var parts = text.Split(separator);
            var newText = string.Empty;
            var length = 0;

            foreach(var part in parts)
            {
                if(length + part.Length > maxLength)
                {
                    if (breakOnOverflow) 
                    { 
                        newText += $" ....."; 
                        break; 
                    }

                    var addedText = $"\n{part}{separator}";
                    newText += addedText;
                    length = addedText.Length;
                }
                else
                {
                    var addedText = $"{part}{separator}";
                    newText += addedText;
                    length += addedText.Length;
                }
            }

            return newText[0..^2];
        }

        private async Task<string> FormatDescription(string description)
        {
            await Task.CompletedTask;
            description = description.Trim().Replace("<br>", "");
            description = Regex.Replace(description, "\n+", " ");
            description = await RemoveBlacklistedSubstrings(description);

            var maxLength = 185;
            var separator = " ";
            var newDesc = await FormatLength(description, maxLength, separator, breakOnOverflow: true);
            
            return newDesc;
        }
    }
}
