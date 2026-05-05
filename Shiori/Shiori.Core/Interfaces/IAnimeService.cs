using Shiori.Core.DTOs;

namespace Shiori.Core.Interfaces
{
    public interface IAnimeService
    {
        Task<Anime> GetOrImportAnimeAsync(int jikanId);
        Task<IEnumerable<Anime>> SearchLocalAsync(string query);
        Task<IEnumerable<Anime>> SearchExternalAsync(string query);
        Task<IEnumerable<AnimeExternalDto>> GetTopAnimesAsync(int limit);
    }
}