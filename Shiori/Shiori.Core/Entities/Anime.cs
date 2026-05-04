using Shiori.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Anime
{
    [Key]
    public int JikanId { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? EnglishTitle { get; set; }
    public string? ImageUrl { get; set; }
    public string? Synopsis { get; set; }
    public double? Score { get; set; }
    public int? Rank { get; set; }
    public int? Popularity { get; set; }
    public int? Episodes { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public DateTime? AiredFrom { get; set; }
    public DateTime? AiredTo { get; set; }
}