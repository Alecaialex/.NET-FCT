using Shiori.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shiori.Core.Entities
{
    public class UserAnime
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int AnimeId { get; set; }
        public UserAnimeStatus Status { get; set; } = UserAnimeStatus.Planning;
        public int? Score { get; set; }
        public int? Progress { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? FinishDate { get; set; }
        public User? User { get; set; }
        public Anime? Anime { get; set; }
    }
}
