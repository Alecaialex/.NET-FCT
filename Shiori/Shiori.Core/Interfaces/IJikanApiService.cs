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

        // Obtener detalles de un anime por su ID en Jikan
        Task<AnimeExternalDto?> GetAnimeByJikanIdAsync(int jikanId);
    }
}
