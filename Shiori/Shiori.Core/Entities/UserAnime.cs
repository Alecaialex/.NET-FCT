using Shiori.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Shiori.Core.Entities
{
    public class UserAnime
    {
        // ID de la relación unica entre usuario y anime
        [Key]
        public Guid Id { get; set; }
        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }
        [Required]
        [ForeignKey(nameof(Anime))]
        public int AnimeId { get; set; }
        public UserAnimeStatus Status { get; set; } = UserAnimeStatus.Watching;
        public int? Score { get; set; }
        public int? Progress { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? FinishDate { get; set; }
        public User? User { get; set; }
        public Anime? Anime { get; set; }
    }
}
