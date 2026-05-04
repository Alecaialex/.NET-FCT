using Microsoft.EntityFrameworkCore;
using Shiori.Core.Entities;
using Shiori.Core.Enums;
using Shiori.Core.Interfaces;
using Shiori.Infra.Data;

namespace Shiori.Infra.Repositories
{
    public class UserAnimeRepository : IUserAnimeRepository
    {
        private readonly AppDbContext _context;

        public UserAnimeRepository(AppDbContext context)
        {
            _context = context;
        }

        // Añadir un anime a la lista del usuario
        public async Task AddUserAnimeAsync(User? user, int jikanId, UserAnimeStatus status = UserAnimeStatus.Watching)
        {
            // Verificar si ya existe la relación
            var exists = await _context.UserAnimes
                .AnyAsync(ua => ua.UserId == user!.Id && ua.AnimeId == jikanId);

            if (exists) return;

            // Si no existe, crear la relación
            await _context.UserAnimes.AddAsync(new UserAnime
            {
                Id = Guid.NewGuid(),
                UserId = user!.Id,
                AnimeId = jikanId,
                Status = status,
                StartDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserAnimeAsync(User? user, int jikanId, int? score, int? progress, UserAnimeStatus? status)
        {
            var userAnime = await _context.UserAnimes
                .FirstOrDefaultAsync(ua => ua.UserId == user!.Id && ua.AnimeId == jikanId);

            if (userAnime != null)
            {
                if (score.HasValue)
                {
                    userAnime.Score = score.Value;
                }
                if (progress.HasValue)
                {
                    userAnime.Progress = progress.Value;
                }
                if (status.HasValue)
                {
                    userAnime.Status = status.Value;

                    if (status.Value == UserAnimeStatus.Completed)
                    {
                        userAnime.FinishDate = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
            }
        }
    }
}