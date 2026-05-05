using Shiori.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Shiori.Core.DTOs
{
    public class UserAnimeFilterDto
    {
        public UserAnimeStatus? Status { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La página debe ser mayor o igual a 1.")]
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}