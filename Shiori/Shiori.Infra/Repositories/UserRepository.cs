using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;
using Shiori.Infra.Data;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace Shiori.Infra.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _contextAccessor;

        public UserRepository(AppDbContext context, UserManager<User> userManager, IHttpContextAccessor contextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _contextAccessor = contextAccessor;
        }

        // Crear usuario
        public async Task<bool> CreateUserAsync(User user)
        {
            // Añadir el usuario a BD
            var result = await _userManager.CreateAsync(user);
            return result.Succeeded;
        }

        // Actualizar usuario
        public async Task<bool> UpdateUserAsync(User user)
        {
            // Sacar usuario actual de la BD, si no existe se devuelve false
            var existingUser = await _context.Users.FindAsync(user.Id);
            if (existingUser == null)
            {
                return false;
            }

            // Actualizar los datos del usuario
            existingUser.Email = user.Email;
            existingUser.Role = user.Role;

            return await _context.SaveChangesAsync() > 0;
        }

        // Obtener usuario por email
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            // Devolver el usuario si se encuentra el email
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        // Obtener usuario por ID
        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            // Devolver el usuario si se encuentra el ID
            return await _context.Users.FindAsync(id);
        }

        // Obtener animes en la lista del usuario
        public async Task<IEnumerable<UserAnime>> GetUserAnimesAsync(Guid userId)
        {
            // Buscar en UserAnimes los que tengan ese ID de usuario
            // y devolverlos en una lista, incluyendo detalles del anime
            return await _context.UserAnimes
                                 .Include(ua => ua.Anime)
                                 .Where(ua => ua.UserId == userId)
                                 .ToListAsync();
        }

        // Obtener UserAnime mediante la ID de Jikan
        public async Task<UserAnime?> GetUserAnimeByJikanIdAsync(Guid userid, int jikanId)
        {
            // Buscar el anime indicado, si no existe devolver null
            var anime = await _context.Animes
                                      .FirstOrDefaultAsync(a => a.JikanId == jikanId);

            if (anime == null)
            {
                return null;
            }

            // Devolver el UserAnime que coincide con el anime y el user id indicados
            return await _context.UserAnimes
                                 .FirstOrDefaultAsync(ua => ua.UserId == userid
                                                         && ua.AnimeId == anime.JikanId);
        }

        // Crear un nuevo UserAnime
        public async Task<UserAnime> CreateOrUpdateUserAnimeAsync(UserAnime userAnime)
        {
            // Buscamos si ya existe esa relación UserAnime
            var existing = await _context.UserAnimes.FindAsync(userAnime.Id);

            // Si existe, modificamos los datos a los indicados
            if (existing != null)
            {
                existing.Status = userAnime.Status;
                existing.Score = userAnime.Score;
                existing.Progress = userAnime.Progress;
                existing.StartDate = userAnime.StartDate ?? DateTime.UtcNow;
            }

            // Si no existe, lo creamos
            else
            {
                _context.UserAnimes.Add(userAnime);
            }

            await _context.SaveChangesAsync();
            return userAnime;
        }

        // Obtener el usuario actual mediante el email en el token
        public async Task<User?> GetCurrentUser()
        {
            var emailClaim = _contextAccessor.HttpContext?.User.Claims.Where(c => c.Type == "email").FirstOrDefault();
            if (emailClaim == null)
            {
                return null;
            }

            return await _userManager.FindByEmailAsync(emailClaim.Value);
        }
    }
}
