using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Overseer.Models
{
    // TODO Break this into 2 models, one for each type of media
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Required for deserialization")]
    public class OverseerMedia
    {
        public Title Title { get; set; }
        public Uri SiteUrl { get; set; }
        public string Description { get; set; }
        public CoverImage CoverImage { get; set; }
        public string Status { get; set; }
        public string Format { get; set; }
        public int NumReleases { get; set; }
        [JsonProperty("chapters")]
        private int Chapters { set { NumReleases = value; } }
        [JsonProperty("episodes")]
        private int Episodes { set { NumReleases = value; } }
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
}