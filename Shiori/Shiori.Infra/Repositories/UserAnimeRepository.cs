using Microsoft.EntityFrameworkCore;
using Shiori.Core.Entities;
using Shiori.Core.Enums;
using Shiori.Core.Interfaces;
using Shiori.Infra.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shiori.Infra.Repositories
{
    public class UserAnimeRepository : IUserAnimeRepository
    {
        private readonly AppDbContext _context;

        public UserAnimeRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task AddUserAnimeAsync(User? user, int jikanId, UserAnimeStatus status = UserAnimeStatus.Watching)
        {
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
            var userAnime = await _context.UserAnimes.FirstOrDefaultAsync(ua => ua.UserId == user!.Id && ua.AnimeId == jikanId);
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
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
