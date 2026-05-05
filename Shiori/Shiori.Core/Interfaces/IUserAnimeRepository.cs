using Shiori.Core.Entities;
using Shiori.Core.Enums;
using Shiori.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiori.Core.Interfaces
{
    public interface IUserAnimeRepository
    {
        // Añadir un anime a la lista del usuario
        Task<bool> AddUserAnimeAsync(User user, int jikanId, UserAnimeStatus status = UserAnimeStatus.Watching);

        // Actualizar el progreso de un anime en la lista del usuario
        Task<bool> UpdateUserAnimeAsync(User user, int jikanId, int? score, int? progress, UserAnimeStatus? status);

        // Obtener todos los animes de la lista de un usuario
        Task<IEnumerable<UserAnime>> GetUserAnimesAsync(User user, UserAnimeFilterDto? userAnimeFilterDto);

        // Obtener estadísticas del usuario
        Task<UserStatsDto> GetUserStatsAsync(User user);
    }
}