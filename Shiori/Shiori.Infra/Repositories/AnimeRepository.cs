using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;
using Shiori.Infra.Data;

namespace Shiori.Infra.Repositories
{
    public class AnimeRepository : IAnimeRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AnimeRepository> _logger;

        public AnimeRepository(AppDbContext context, ILogger<AnimeRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Tipo de DateTime a UTC para compatibilidad con postgre
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
                _logger.LogDebug("Buscando en base de datos el anime con JikanId: {Id}", jikanId);
                return await _context.Animes.FirstOrDefaultAsync(a => a.JikanId == jikanId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DATABASE ERROR: Fallo al intentar obtener el anime con JikanId {Id}", jikanId);
                return null;
            }
        }

        // Buscar un anime mediante una query
        public async Task<IEnumerable<Anime>> SearchAnimesAsync(string query, int page = 1)
        {
            try
            {
                _logger.LogInformation("Ejecutando búsqueda local en BD. Query: '{Query}', Página: {Page}", query, page);

                var pageSize = 10;
                var skip = (page - 1) * pageSize;

                var results = await _context.Animes
                    .Where(a => a.Title.Contains(query) || (a.EnglishTitle != null && a.EnglishTitle.Contains(query)))
                    .OrderBy(a => a.Title)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("Búsqueda local completada. Encontrados {Count} registros.", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DATABASE ERROR: Error en búsqueda local para la consulta: {Query}", query);
                return Enumerable.Empty<Anime>();
            }
        }

        // Añadir un anime a la BD
        public async Task<bool> AddAnimeToDbAsync(AnimeExternalDto anime)
        {
            try
            {
                _logger.LogInformation("Intentando insertar nuevo anime en BD: {Title} (ID: {Id})", anime.Title, anime.MalId);

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

                await _context.Animes.AddAsync(bdAnime);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                    _logger.LogInformation("Anime '{Title}' insertado correctamente en la base de datos.", anime.Title);

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DATABASE ERROR: No se pudo insertar el anime {Title} (JikanId: {Id})", anime.Title, anime.MalId);
                return false;
            }
        }

        // Actualizar datos de un anime en la BD
        public async Task<bool> UpdateAnimeAsync(AnimeExternalDto anime)
        {
            try
            {
                _logger.LogInformation("Actualizando datos del anime: {Title} (ID: {Id})", anime.Title, anime.MalId);

                var existing = await _context.Animes.FirstOrDefaultAsync(a => a.JikanId == anime.MalId);
                if (existing == null)
                {
                    _logger.LogWarning("No se pudo actualizar el anime {Id} porque no existe en la base de datos.", anime.MalId);
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
                _logger.LogError(ex, "DATABASE ERROR: Error al actualizar datos del anime con JikanId {Id}", anime.MalId);
                return false;
            }
        }

        // Eliminar un anime de la BD
        public async Task<bool> DeleteAnimeAsync(int jikanId)
        {
            try
            {
                _logger.LogWarning("Iniciando eliminación física del anime con JikanId {Id}", jikanId);

                var existingAnime = await _context.Animes.FirstOrDefaultAsync(a => a.JikanId == jikanId);
                if (existingAnime == null)
                {
                    _logger.LogWarning("Intento de eliminar anime {Id} fallido: No existe en BD.", jikanId);
                    return false;
                }

                var existingUserAnimes = await _context.UserAnimes
                    .Where(ua => ua.AnimeId == existingAnime.JikanId)
                    .ToListAsync();

                if (existingUserAnimes.Any())
                {
                    _logger.LogInformation("Eliminando {Count} relaciones de usuarios asociadas al anime {Id}.", existingUserAnimes.Count, jikanId);
                    _context.UserAnimes.RemoveRange(existingUserAnimes);
                }

                _context.Animes.Remove(existingAnime);
                var result = await _context.SaveChangesAsync();

                _logger.LogInformation("Anime {Id} y sus dependencias eliminados con éxito.", jikanId);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DATABASE ERROR: Error crítico al eliminar el anime con JikanId {Id}", jikanId);
                return false;
            }
        }
    }
}