using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace Shiori.Core.DTOs
{
    // DTO para datos del anime
    public class AnimeExternalDto
    {
        [JsonPropertyName("mal_id")]
        public int MalId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("title_english")]
        public string? TitleEnglish { get; set; }

        [JsonPropertyName("images")]
        public ImageDto Images { get; set; } = new();

        [JsonPropertyName("score")]
        public double? Score { get; set; }

        [JsonPropertyName("rank")]
        public int? Rank { get; set; }

        [JsonPropertyName("popularity")]
        public int? Popularity { get; set; }

        [JsonPropertyName("episodes")]
        public int? Episodes { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("aired")]
        public AiredDto Aired { get; set; } = new();

        [JsonPropertyName("synopsis")]
        public string? Synopsis { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    public class ImageDto
    {
        [JsonPropertyName("jpg")]
        public JpgImageDto Jpg { get; set; } = new();
    }

    public class JpgImageDto
    {
        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class  AiredDto
    {
        [JsonPropertyName("from")]
        public DateTime? From { get; set; }
        [JsonPropertyName("to")]
        public DateTime? To { get; set; }
    }

    // DTO para el "Data" de la respuesta de Jikan
    public class AnimeResponseDataDto
    {
        [JsonPropertyName("data")]
        public AnimeExternalDto Data { get; set; } = new();
    }
}
