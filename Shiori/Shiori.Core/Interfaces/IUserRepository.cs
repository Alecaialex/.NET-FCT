using Shiori.Core.Entities;

namespace Shiori.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> CreateUserAsync(User user);
        Task<bool> UpdateUserAsync(User user);
        Task<IEnumerable<UserAnime>> GetUserAnimesAsync(Guid userId);
        Task<UserAnime?> GetUserAnimeByJikanIdAsync(Guid userid, int jikanId);
        Task<UserAnime> CreateOrUpdateUserAnimeAsync(UserAnime userAnime);
        Task<User?> GetCurrentUser();
    }
}
