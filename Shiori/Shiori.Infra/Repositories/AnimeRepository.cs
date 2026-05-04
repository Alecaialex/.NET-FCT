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

        // Convertir fechas a UTC para poder usarlas en postgre
        private DateTime? ToUtc(DateTime? dt)
        {
            if (!dt.HasValue) return null;
            return DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc);
        }

        // Obtener un anime por su ID de Jikan
        public async Task<Anime?> GetAnimeByJikanIdAsync(int jikanId)
        {
            return await _context.Animes.FirstOrDefaultAsync(a => a.JikanId == jikanId);
        }

        // Buscar un anime por su nombre
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

        // Añadir un nuevo anime a la BD
        public async Task<bool> AddAnimeToDbAsync(AnimeExternalDto anime)
        {
            var bdAnime = new Anime
            {
                JikanId = anime.MalId,
                Title = anime.Title,
                EnglishTitle = anime.TitleEnglish,
                ImageUrl = anime.Images?.Jpg?.ImageUrl,
                Synopsis = anime.Synopsis,
                Score = anime.Score ?? 0.0,
                Rank = anime.Rank,
                Popularity = anime.Popularity ?? 0,
                Episodes = anime.Episodes ?? 0,
                Status = anime.Status ?? "Unknown",
                Type = anime.Type ?? "Unknown",
                AiredFrom = ToUtc(anime.Aired?.From),
                AiredTo = ToUtc(anime.Aired?.To)
            };
            _context.Animes.Add(bdAnime);
            return await _context.SaveChangesAsync() > 0;
        }

        // Actualizar datos de un anime en la BD
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
            existing.Score = anime.Score ?? 0.0;
            existing.Rank = anime.Rank;
            existing.Popularity = anime.Popularity ?? 0;
            existing.Episodes = anime.Episodes ?? 0;
            existing.Status = anime.Status ?? "Unknown";
            existing.Type = anime.Type ?? "Unknown";
            existing.AiredFrom = ToUtc(anime.Aired?.From);
            existing.AiredTo = ToUtc(anime.Aired?.To);

            _context.Animes.Update(existing);
            return await _context.SaveChangesAsync() > 0;
        }

        // Borrar un anime de la BD
        public async Task<bool> DeleteAnimeAsync(int jikanId)
        {
            var existingAnime = await _context.Animes.FirstOrDefaultAsync(a => a.JikanId == jikanId);
            if (existingAnime == null)
            {
                return false;
            }

            // Buscar y eliminar las relaciones de UserAnime que tengan este anime
            var existingUserAnimes = await _context.UserAnimes.Where(ua => ua.AnimeId == existingAnime.JikanId).ToListAsync();
            foreach (var userAnime in existingUserAnimes)
            {
                _context.UserAnimes.Remove(userAnime);
            }

            _context.Animes.Remove(existingAnime);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}