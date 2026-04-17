using Newtonsoft.Json;

namespace Motivation.Models.Mobile
{
    public enum Highlated
    {
        Green,
        Red  
    }

    public class Card
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("subtitle")]
        public string Subtitle { get; set; } = string.Empty;

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        [JsonProperty("sort_index")]
        public int SortIndex { get; set; }

        [JsonProperty("clickable")]
        public bool Clickable { get; set; }

        [JsonProperty("need_progressbar")]
        public bool NeedProgressBar { get; set; }

        [JsonProperty("highlited", NullValueHandling = NullValueHandling.Ignore)]
        public Highlated? Highlited { get; set; }
    }
}
