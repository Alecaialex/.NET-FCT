using Microsoft.EntityFrameworkCore;
using NLog;
using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using Shiori.Core.Enums;
using Shiori.Core.Interfaces;
using Shiori.Infra.Data;

namespace Shiori.Infra.Repositories
{
    public class UserAnimeRepository : IUserAnimeRepository
    {
        private readonly AppDbContext _context;

        // Definimos el logger de NLog para esta clase
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public UserAnimeRepository(AppDbContext context)
        {
            _context = context;
        }

        // Añadir un anime a la lista de un usuario
        public async Task<bool> AddUserAnimeAsync(User user, int jikanId, UserAnimeStatus status = UserAnimeStatus.Watching)
        {
            try
            {
                if (user == null)
                {
                    logger.Warn("Intento de añadir anime {0} fallido: El objeto usuario es nulo.", jikanId);
                    return false;
                }

                logger.Info("Verificando si el anime {0} ya existe en la lista de {1}.", jikanId, user.UserName);

                var exists = await _context.UserAnimes
                    .AnyAsync(ua => ua.UserId == user.Id && ua.AnimeId == jikanId);

                if (exists)
                {
                    logger.Info("El anime {0} ya está en la lista del usuario {1}.", jikanId, user.UserName);
                    return true;
                }

                await _context.UserAnimes.AddAsync(new UserAnime
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    AnimeId = jikanId,
                    Status = status,
                    StartDate = (status == UserAnimeStatus.Planning) ? null : DateTime.UtcNow
                });

                var saved = await _context.SaveChangesAsync() > 0;

                if (saved)
                    logger.Info("ÉXITO: Anime {0} añadido correctamente a la lista de {1} con estado {2}.", jikanId, user.UserName, status);

                return saved;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "DATABASE ERROR: No se pudo añadir el anime {0} al usuario {1}.", jikanId, user?.UserName);
                return false;
            }
        }

        // Actualizar un anime en la lista de un usuario
        public async Task<bool> UpdateUserAnimeAsync(User user, int jikanId, int? score, int? progress, UserAnimeStatus? status)
        {
            try
            {
                if (user == null) return false;

                logger.Info("Actualizando progreso de {0} para el anime {1}.", user.UserName, jikanId);

                var userAnime = await _context.UserAnimes
                    .FirstOrDefaultAsync(ua => ua.UserId == user.Id && ua.AnimeId == jikanId);

                if (userAnime == null)
                {
                    logger.Warn("No se encontró la relación UserAnime para Usuario: {0}, Anime: {1}.", user.UserName, jikanId);
                    return false;
                }

                if (score.HasValue)
                {
                    logger.Debug("Cambiando Score de {0} a {1} para el anime {2}", userAnime.Score, score.Value, jikanId);
                    userAnime.Score = score.Value;
                }

                if (progress.HasValue)
                {
                    userAnime.Progress = progress.Value;
                }

                if (status.HasValue && status.Value != userAnime.Status)
                {
                    logger.Info("Cambiando estado de {0} a {1} para el usuario {2}.", userAnime.Status, status.Value, user.UserName);
                    userAnime.Status = status.Value;

                    switch (status.Value)
                    {
                        case UserAnimeStatus.Planning:
                            logger.Info("Reseteando fechas para el anime {0} (Plan to Watch).", jikanId);
                            userAnime.StartDate = null;
                            userAnime.FinishDate = null;
                            break;

                        case UserAnimeStatus.Watching:
                            if (userAnime.StartDate == null) userAnime.StartDate = DateTime.UtcNow;
                            userAnime.FinishDate = null;
                            break;

                        case UserAnimeStatus.Completed:
                            logger.Info("Anime {0} marcado como COMPLETADO por {1}.", jikanId, user.UserName);
                            if (userAnime.StartDate == null) userAnime.StartDate = DateTime.UtcNow;
                            userAnime.FinishDate = DateTime.UtcNow;
                            break;

                        case UserAnimeStatus.Dropped:
                            logger.Info("Anime {0} marcado como DROPPED por {1}.", jikanId, user.UserName);
                            break;

                        case UserAnimeStatus.OnHold:
                            logger.Info("Anime {0} marcado como ON HOLD por {1}.", jikanId, user.UserName);
                            break;
                    }
                }

                var result = await _context.SaveChangesAsync() > 0;
                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "DATABASE ERROR: Fallo al actualizar progreso del anime {0} para {1}.", jikanId, user?.UserName);
                return false;
            }
        }

        // Obtener la lista de animes de un usuario
        public async Task<IEnumerable<UserAnime>> GetUserAnimesAsync(User user, UserAnimeFilterDto? filter)
        {
            try
            {
                filter ??= new UserAnimeFilterDto();
                string statusLog = filter.Status.HasValue ? filter.Status.Value.ToString() : "Sin filtro";

                logger.Info("Consultando lista de {0}. Estado: {1}, Página: {2}",
                    user.UserName, statusLog, filter.Page);

                var query = _context.UserAnimes
                    .Include(ua => ua.Anime)
                    .Where(ua => ua.UserId == user.Id)
                    .AsQueryable();

                if (filter.Status.HasValue)
                {
                    query = query.Where(ua => ua.Status == filter.Status.Value);
                }

                int pageSize = filter.PageSize > 0 ? filter.PageSize : 10;
                int page = filter.Page > 0 ? filter.Page : 1;
                var skip = (page - 1) * pageSize;

                return await query
                    .OrderByDescending(ua => ua.StartDate)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error al recuperar lista de animes para {0}", user.UserName);
                return Enumerable.Empty<UserAnime>();
            }
        }

        // Obtener estadísticas del usuario
        public async Task<UserStatsDto> GetUserStatsAsync(User user)
        {
            try
            {
                var userAnimes = await _context.UserAnimes
                    .Include(ua => ua.Anime)
                    .Where(ua => ua.UserId == user.Id)
                    .ToListAsync();

                if (!userAnimes.Any()) return new UserStatsDto();

                int totalMinutes = 0;
                int totalEpisodes = 0;
                int scoredAnimesCount = 0;
                double sumScores = 0;

                foreach (var item in userAnimes)
                {

                    string animeType = item.Anime?.Type ?? "TV";

                    int durationPerUnit = animeType switch
                    {
                        "Movie" => 100,
                        "Special" => 30,
                        "OVA" => 30,
                        "Music" => 5,
                        _ => 24
                    };

                    int progress = item.Progress ?? 0;
                    totalMinutes += progress * durationPerUnit;
                    totalEpisodes += progress;

                    if (item.Score.HasValue && item.Score.Value > 0)
                    {
                        sumScores += (double)item.Score.Value;
                        scoredAnimesCount++;
                    }
                }

                return new UserStatsDto
                {
                    TotalAnimes = userAnimes.Count,
                    TotalEpisodesWatched = totalEpisodes,
                    TotalDaysWatched = Math.Round(totalMinutes / 1440.0, 2),
                    MeanScore = scoredAnimesCount > 0 ? Math.Round(sumScores / scoredAnimesCount, 2) : 0,
                    StatusDistribution = userAnimes
                        .GroupBy(ua => ua.Status.ToString())
                        .ToDictionary(g => g.Key, g => g.Count())
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error al calcular estadísticas para el usuario {0}", user.UserName);
                return new UserStatsDto();
            }
        }
    }
}