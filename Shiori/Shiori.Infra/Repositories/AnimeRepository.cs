using Microsoft.EntityFrameworkCore;
using NLog;
using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;
using Shiori.Infra.Data;

namespace Shiori.Infra.Repositories
{
    public class AnimeRepository : IAnimeRepository
    {
        private readonly AppDbContext _context;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public AnimeRepository(AppDbContext context)
        {
            _context = context;
        }

        // DateTime a UTC para compatibilidad con postgre
        private DateTime? ToUtc(DateTime? dt)
        {
            if (!dt.HasValue) return null;
            return DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc);
        }

        // Obtener un anime por su ID de Jikan
        public async Task<Anime?> GetAnimeByJikanIdAsync(int jikanId)
        {
            try
            {
                return await _context.Animes.FirstOrDefaultAsync(a => a.JikanId == jikanId);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error en BD al intentar obtener el anime con JikanId {Id}", jikanId);
                return null;
            }
        }

        // Buscar un anime mediante su nombre
        public async Task<IEnumerable<Anime>> SearchAnimesLocalAsync(string animeName, int page = 1)
        {
            try
            {
                var pageSize = 10;
                var skip = (page - 1) * pageSize;

                var results = await _context.Animes
                    .Where(a => a.Title.Contains(animeName) || (a.EnglishTitle != null && a.EnglishTitle.Contains(animeName)))
                    .OrderBy(a => a.Title)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error en BD al buscar el anime {0}", animeName);
                return Enumerable.Empty<Anime>();
            }
        }

        // Añadir un anime a la BD
        public async Task<bool> AddAnimeToDbAsync(Anime anime)
        {
            try
            {
                await _context.Animes.AddAsync(anime);
                var result = await _context.SaveChangesAsync();

                return result > 0;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error en BD al intentar insertar el anime {0}", anime.Title);
                return false;
            }
        }

        // Actualizar datos de un anime en la BD
        public async Task<bool> UpdateAnimeAsync(AnimeExternalDto anime)
        {
            try
            {
                var existing = await _context.Animes.FirstOrDefaultAsync(a => a.JikanId == anime.MalId);
                if (existing == null)
                {
                    logger.Warn("Error al actualizar el anime {0} con ID {1}. No existe en la BD", anime.Title, anime.MalId);
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
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error en BD al actualizar datos del anime {0} {1}", anime.Title, anime.MalId);
                return false;
            }
        }

        // Eliminar un anime de la BD
        public async Task<bool> DeleteAnimeAsync(int jikanId)
        {
            try
            {
                var existingAnime = await _context.Animes.FirstOrDefaultAsync(a => a.JikanId == jikanId);
                if (existingAnime == null)
                {
                    logger.Warn("Error al eliminar anime con ID {0} fallido: No existe en BD", jikanId);
                    return false;
                }

                var existingUserAnimes = await _context.UserAnimes
                    .Where(ua => ua.AnimeId == existingAnime.JikanId)
                    .ToListAsync();

                if (existingUserAnimes.Any())
                {
                    logger.Info("Eliminando {0} relaciones de usuarios asociadas al anime {1}.", existingUserAnimes.Count, jikanId);
                    _context.UserAnimes.RemoveRange(existingUserAnimes);
                }

                _context.Animes.Remove(existingAnime);
                var result = await _context.SaveChangesAsync();

                logger.Info("Anime {0} y sus dependencias eliminados con éxito.", jikanId);
                return result > 0;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error en BD al eliminar el anime con JikanId {0}", jikanId);
                return false;
            }
        }
    }
}