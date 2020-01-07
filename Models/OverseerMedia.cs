using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Overseer.Models
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Required for deserialization")]
    public class OverseerMedia
    {
        public Title Title { get; set; }
        public string Description { get; set; }
        public string Format { get; set; }
        public NextEpisode NextAiringEpisode { get; set; }
        public Uri SiteUrl { get; set; }
        public CoverImage CoverImage { get; set; }
        public string Status { get; set; }
        public List<string> Genres { get; set; }
        public List<Tag> Tags { get; set; }
    }

    public class Title
    {
        public string Romaji { get; set; }
    }

    public class CoverImage
    {
        public string ExtraLarge { get; set; }
    }

    public class Tag
    {
        public string Name { get; set; }
        public int Rank { get; set; }
        public bool IsMediaSpoiler { get; set; }
    }

    public class NextEpisode
    {
        public int TimeUntilAiring { get; set; }
    }
}