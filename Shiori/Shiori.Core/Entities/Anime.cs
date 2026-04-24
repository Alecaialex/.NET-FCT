using System;
using System.Collections.Generic;
using System.Text;

namespace Shiori.Core.Entities
{
    public class Anime
    {
        public int JikanId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? EnglishTitle { get; set; }
        public string? ImageUrl { get; set; }
        public string? Synopsis { get; set; }
        public double Score { get; set; }
        public int Rank { get; set; }
        public int Popularity { get; set; }
        public int? Episodes { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
        public DateTime? AiredFrom { get; set; }
        public DateTime? AiredTo { get; set; }
        public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
        public ICollection<UserAnime> UserAnimes { get; set; } = new List<UserAnime>();
        public ICollection<AnimeMetric> Metrics { get; set; } = new List<AnimeMetric>();
    }
}
