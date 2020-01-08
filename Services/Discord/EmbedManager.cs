using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using StringUtils;

using Discord;

using Overseer.Models;
using Overseer.Constants;
using Overseer.Services.Logging;

// TODO try to refactor CraftEmbed to not require ReleaseType param
namespace Overseer.Services.Discord
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1822:Mark members as static", Justification = "Logically operates on an instance")]
    public class EmbedManager
    {
        private readonly ILogger _logger;

        public EmbedManager(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<Embed> CraftEmbed(OverseerMedia media)
        {
            // attributes
            var title = await FormatTitle(media.Title.Romaji);
            var desc = await FormatDescription(media.Description);
            var url = media.SiteUrl.ToString();
            var color = EmbedConstants.Color;
            var thumbnail = media.CoverImage.ExtraLarge;
            var footer = new EmbedFooterBuilder
            {
                Text = await FormatFooterStatus(media)
            };

            var eb = new EmbedBuilder
            {
                Title = title,
                Description = desc,
                Url = url,
                Color = color,
                ThumbnailUrl = thumbnail,
                Footer = footer
            };

            // fields
            var tags = await GetTagList(media.Tags);
            var genresAndTags = string.Join(", ", tags.Concat(media.Genres));
            eb.AddField("Tags", genresAndTags, true);

            return eb.Build();
        }

        private Task<string> FormatTitle(string title)
        {
            return Task.FromResult($"**{title}**");
        }

        private Task<string> FormatDescription(string description)
        {
            var origLength = description.Length;
            var maxLength = 185;
            var separator = " ";
            var blacklist = new List<string>
            {
                "<.*?>",
                "\\(Source.*?\\)",
                "<br>",
                "\n+"
            };

            description = description
                .Trim()
                .RemoveSubstrings(blacklist)
                .Join(separator, maxLength);
            if (description.Length < origLength) description += "...";

            return Task.FromResult(description);
        }

        private Task<string> FormatFooterStatus(OverseerMedia media)
        {
            string res;

            if (media.NextAiringEpisode == null)
            {
                res = media.Status.ToTitleCase();
            }
            else
            {
                var duration = TimeSpan.FromSeconds(media.NextAiringEpisode.TimeUntilAiring);

                res = "Next Episode: ";
                res += (duration.TotalHours > 23) ? $"{duration.Days}d {duration.Hours}h {duration.Minutes}m"   // dd hh mm if > 23h
                    : (duration.TotalMinutes > 59) ? $"{duration.Hours}h {duration.Minutes}m"                   // hh mm if > 59m
                    : $"{duration.Minutes}m";                                                                   // mm default
            }

            return Task.FromResult(res);
        }

        private Task<IEnumerable<string>> GetTagList(List<Tag> tagList)
        {
            var tagNameList = tagList.Where(x => !x.IsMediaSpoiler && x.Rank >= 50).Select(x => x.Name);
            return Task.FromResult(tagNameList);
        }
    }
}
