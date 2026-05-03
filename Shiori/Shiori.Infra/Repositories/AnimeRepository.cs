using Microsoft.EntityFrameworkCore;
using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;
using Shiori.Infra.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace Shiori.Infra.Repositories
{
    public class AnimeRepository : IAnimeRepository
    {
        private readonly AppDbContext _context;

        public AnimeRepository(AppDbContext context)
        {
            _context = context;
        }

        private DateTime? ToUtc(DateTime? dt)
        {
            if (!dt.HasValue) return null;
            return DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc);
        }

        public async Task<Anime?> GetAnimeByJikanIdAsync(int jikanId)
        {
            return await _context.Animes.FirstOrDefaultAsync(a => a.JikanId == jikanId);
        }

        public async Task<IEnumerable<Anime>> SearchAnimesAsync(string query, int page = 1)
        {
            var pageSize = 10;
            var skip = (page - 1) * pageSize;

            return await _context.Animes
                .Where(a => a.Title.Contains(query) || (a.EnglishTitle != null && a.EnglishTitle.Contains(query)))
                .OrderBy(a => a.Title)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> AddAnimeToDbAsync(AnimeExternalDto anime)
        {
            var bdAnime = new Anime
            {
                JikanId = anime.MalId,
                Title = anime.Title,
                EnglishTitle = anime.TitleEnglish,
                ImageUrl = anime.Images?.Jpg?.ImageUrl,
                Synopsis = anime.Synopsis,
                Score = anime.Score,
                Rank = anime.Rank,
                Popularity = anime.Popularity,
                Episodes = anime.Episodes,
                Status = anime.Status,
                Type = anime.Type,
                AiredFrom = ToUtc(anime.Aired?.From),
                AiredTo = ToUtc(anime.Aired?.To)
            };
            _context.Animes.Add(bdAnime);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAnimeAsync(AnimeExternalDto anime)
        {
            var existing = await _context.Animes.FirstOrDefaultAsync(a => a.JikanId == anime.MalId);
            if (existing == null)
            {
                return false;
            }

            existing.Title = anime.Title;
            existing.EnglishTitle = anime.TitleEnglish;
            existing.ImageUrl = anime.Images?.Jpg?.ImageUrl;
            existing.Synopsis = anime.Synopsis;
            existing.Score = anime.Score;
            existing.Rank = anime.Rank;
            existing.Popularity = anime.Popularity;
            existing.Episodes = anime.Episodes;
            existing.Status = anime.Status;
            existing.Type = anime.Type;
            existing.AiredFrom = ToUtc(anime.Aired?.From);
            existing.AiredTo = ToUtc(anime.Aired?.To);

            _context.Animes.Update(existing);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}