using System;
using System.Collections.Generic;
using System.Text;

namespace Shiori.Core.DTOs
{
    public class UserStatsDto
    {
        public int TotalAnimes { get; set; }
        public int TotalEpisodesWatched { get; set; }
        public double TotalDaysWatched { get; set; }
        public double MeanScore { get; set; }

        // Distribución de estado (viendo, completado, etc...) para gráficos
        public Dictionary<string, int> StatusDistribution { get; set; } = new();
    }
}