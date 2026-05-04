using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shiori.Core.Interfaces
{
    public interface IAnimeRepository
    {
        // Obtener anime por su título
        Task<IEnumerable<Anime>> SearchAnimesAsync(string query, int page = 1);

        // Añadir anime a la BD
        Task<bool> AddAnimeToDbAsync(AnimeExternalDto anime);

        // Actualizar datos del anime en la BD
        Task<bool> UpdateAnimeAsync(AnimeExternalDto anime);

        // Buscar si un anime ya existe en BD por su id de Jikan
        Task<Anime?> GetAnimeByJikanIdAsync(int jikanId);

        // Eliminar un anime por su id
        Task<bool> DeleteAnimeAsync(int jikanId);
    }
}
