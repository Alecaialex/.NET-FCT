using Shiori.Core.Entities;
using Shiori.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shiori.Core.Interfaces
{
    public interface IUserAnimeRepository
    {
        Task AddUserAnimeAsync(User? user, int jikanId, UserAnimeStatus status = UserAnimeStatus.Watching);
        Task UpdateUserAnimeAsync(User? user, int jikanId, int? score, int? progress, UserAnimeStatus? status);
    }
}
