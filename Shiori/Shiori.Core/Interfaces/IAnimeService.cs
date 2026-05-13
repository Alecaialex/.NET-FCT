using Shiori.Core.DTOs;

namespace Shiori.Core.Interfaces
{
    public interface IAnimeService
    {
        // Obtener un anime por su id
        Task<Anime?> GetOrImportAnimeAsync(int jikanId);

        // Buscar un anime en Jikan
        Task<IEnumerable<Anime?>> SearchExternalAsync(string animeName);
    }
}