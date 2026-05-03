using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Shiori.Core.DTOs
{
    // DTO para la respuesta de animes top de Jikan
    public class TopAnimeResponseDto
    {
        [JsonPropertyName("data")]
        public IEnumerable<AnimeExternalDto> Data { get; set; } = new List<AnimeExternalDto>();
    }
}
