#nullable enable

using System;

namespace Jellyfin.Plugin.xThemeSong.Models
{
    public class YouTubeSearchResult
    {
        public string VideoId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public TimeSpan? Duration { get; set; }
        public string Url { get; set; } = string.Empty;
        public int MatchScore { get; set; }
    }
}
