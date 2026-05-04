using Shiori.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shiori.Core.Interfaces
{
    public interface IJikanApiService
    {
        // Obtener el top de animes actual
        Task<IEnumerable<AnimeExternalDto>> GetTopAnimesAsync(int limit = 25);

        // Buscar anime por su ID en Jikan
        Task<AnimeExternalDto?> GetAnimeByJikanIdAsync(int jikanId);

        // Buscar anime por nombre
        Task<IEnumerable<AnimeExternalDto>> SearchAnimesAsync(string query);
    }
}
