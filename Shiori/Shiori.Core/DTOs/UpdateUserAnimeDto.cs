using Shiori.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shiori.Core.DTOs
{
    // DTO para actualización de un anime en la lista del usuario
    public class UpdateUserAnimeDto
    {
        public int? Score { get; set; }
        public int? Progress { get; set; }
        public UserAnimeStatus? Status { get; set; }
    }
}
