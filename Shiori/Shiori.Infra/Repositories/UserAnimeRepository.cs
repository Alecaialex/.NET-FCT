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
                    logger.Warn("Intento de añadir anime {0} fallido porque el usuario es nulo", jikanId);
                    return false;
                }

                var exists = await _context.UserAnimes
                    .AnyAsync(ua => ua.UserId == user.Id && ua.AnimeId == jikanId);

                if (exists)
                    return true;

                await _context.UserAnimes.AddAsync(new UserAnime
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    AnimeId = jikanId,
                    Status = status,
                    StartDate = (status == UserAnimeStatus.Planning) ? null : DateTime.UtcNow
                });

                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error en BD al añadir el anime {0} al usuario {1}", jikanId, user.UserName);
                return false;
            }
        }

        // Actualizar un anime en la lista de un usuario
        public async Task<bool> UpdateUserAnimeAsync(User user, int jikanId, int? score, int? progress, UserAnimeStatus? status)
        {
            try
            {
                if (user == null)
                {
                    logger.Warn("Intento de actualizar anime {0} fallido porque el usuario es nulo", jikanId);
                    return false;
                }
                    

                var userAnime = await _context.UserAnimes
                    .FirstOrDefaultAsync(ua => ua.UserId == user.Id && ua.AnimeId == jikanId);

                if (userAnime == null)
                {
                    logger.Warn("No se encontró la relación UserAnime para Usuario {0}, Anime {1}", user.UserName, jikanId);
                    return false;
                }

                var anime = await _context.Animes.Where(a => a.JikanId == userAnime.AnimeId).FirstOrDefaultAsync();

                if (score.HasValue && score.Value !> 10 && score.Value !< 0)
                    userAnime.Score = score.Value;

                if (progress.HasValue && progress.Value !> anime!.Episodes && progress.Value !< 0)
                    userAnime.Progress = progress.Value;

                if (status.HasValue && status.Value != userAnime.Status)
                {
                    userAnime.Status = status.Value;

                    switch (status.Value)
                    {
                        case UserAnimeStatus.Planning:
                            userAnime.StartDate = null;
                            userAnime.FinishDate = null;
                            break;

                        case UserAnimeStatus.Watching:
                            if (userAnime.StartDate == null) userAnime.StartDate = DateTime.UtcNow;
                            userAnime.FinishDate = null;
                            break;

                        case UserAnimeStatus.Completed:
                            if (userAnime.StartDate == null) userAnime.StartDate = DateTime.UtcNow;
                            userAnime.FinishDate = DateTime.UtcNow;
                            break;

                        case UserAnimeStatus.Dropped:
                            break;

                        case UserAnimeStatus.OnHold:
                            break;
                    }
                }

                var result = await _context.SaveChangesAsync() > 0;
                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error en BD al actualizar progreso del anime {0} para {1}.", jikanId, user.UserName);
                return false;
            }
        }

        // Obtener la lista de animes de un usuario
        public async Task<IEnumerable<UserAnime>> GetUserAnimesAsync(User user, UserAnimeFilterDto? filter)
        {
            try
            {
                filter ??= new UserAnimeFilterDto();

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

                if (!userAnimes.Any())
                    return new UserStatsDto();

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
                        .ToDictionary(s => s.Key, s => s.Count())
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