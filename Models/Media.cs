using Newtonsoft.Json;
using System.Collections.Generic;

namespace Overseer.Models
{
    public class Media
    {
        public Title title { get; set; }
        public string siteUrl { get; set; }
        public string description { get; set; }
        public CoverImage coverImage { get; set; }
        public string status { get; set; }
        public string format { get; set; }
        public int numReleases { get; set; }
        [JsonProperty("chapters")]
        private int chapters { set { numReleases = value; } }
        [JsonProperty("episodes")]
        private int episodes { set { numReleases = value; } }
        public List<string> genres { get; set; }
        public List<Tag> tags { get; set; }
    }

    public class Title
    {
        public string romaji { get; set; }
    }

    public class CoverImage
    {
        public string extraLarge { get; set; }
    }

    public class Tag
    {
        public string name { get; set; }
        public int rank { get; set; }
        public bool isMediaSpoiler { get; set; }
    }


}
