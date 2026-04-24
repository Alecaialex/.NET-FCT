using System;
using System.Collections.Generic;
using System.Text;

namespace Shiori.Core.Entities
{
    public class AnimeMetric
    {
        public Guid Id { get; set; }
        public int AnimeId { get; set; }
        public DateTime SnapshotDate { get; set; }
        public double GlobalScore { get; set; }
        public int GlobalRank { get; set; }
        public int PopularityRank { get; set; }
        public Anime? Anime { get; set; }
    }
}
