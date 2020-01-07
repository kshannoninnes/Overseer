using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Discord;

using Overseer.Models;
using Overseer.Constants;
using Overseer.Services.Logging;
using System;

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
            var title = $"**{media.Title.Romaji}**";
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
            await AddField(eb, "Tags", genresAndTags, false);

            return eb.Build();
        }

        private async Task AddField(EmbedBuilder eb, string name, string value, bool inline)
        {
            await Task.CompletedTask;
            if (!string.IsNullOrWhiteSpace(name))
            {
                value = string.IsNullOrWhiteSpace(value) ? "N/A" : value;
                eb.AddField(name, value, inline);
            }
        }

        private async Task AddEmptyField(EmbedBuilder eb, bool inline)
        {
            await AddField(eb, "\u200b", "\u200b", inline);
        }

        private async Task<string> RemoveBlacklistedSubstrings(string text)
        {
            await Task.CompletedTask;
            List<string> blacklistedSubstrings = new List<string> {
                "<.*?>",
                "\\(Source.*?\\)"
            };

            foreach (var entry in blacklistedSubstrings)
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
            return type == ReleaseType.Manga ? "Chapters" : "Episodes";
        }

        private async Task<IEnumerable<string>> GetTagList(List<Tag> tagList)
        {
            await Task.CompletedTask;
            return tagList.Where(x => !x.IsMediaSpoiler && x.Rank >= 50).Select(x => x.Name);
        }

        private async Task<string> CapitalizeFirstLetter(string text)
        {
            await Task.CompletedTask;
            string capitalizedText = text.Length < 3 ? text.ToUpper() : char.ToUpper(text[0]) + text.Substring(1).ToLower();

            return capitalizedText;
        }

        private async Task<string> FormatLength(string text, int maxLength, string separator, bool breakOnOverflow = false)
        {
            await Task.CompletedTask;
            var parts = text.Split(separator);
            var newText = string.Empty;
            var length = 0;

            foreach (var part in parts)
            {
                if (length + part.Length > maxLength)
                {
                    if (breakOnOverflow)
                    {
                        newText += " .....";
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

            return newText[..^2];
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

        private async Task<string> FormatFooterStatus(OverseerMedia media)
        {
            string res;

            if (media.NextAiringEpisode == null)
            {
                res = await CapitalizeFirstLetter(media.Status);
            }
            else
            {
                var duration = TimeSpan.FromSeconds(media.NextAiringEpisode.TimeUntilAiring);

                // Xd Yh Zm if days remaining, else Yh Zm if hours remaining, else Zm
                res = "Next Episode: ";
                res += (duration.TotalHours > 23) ? $"{duration.Days}d {duration.Hours}h {duration.Minutes}m"
                    : (duration.TotalMinutes > 59) ? $"{duration.Hours}h {duration.Minutes}m"
                    : $"{duration.Minutes}m";
            }

            return res;
        }
    }
}
