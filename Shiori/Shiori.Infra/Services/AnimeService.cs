using NLog;
using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;

namespace Shiori.Infra.Services
{
    public class AnimeService : IAnimeService
    {
        private readonly IAnimeRepository _animeRepo;
        private readonly IJikanApiService _jikanApi;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public AnimeService(IAnimeRepository animeRepo, IJikanApiService jikanApi)
        {
            _animeRepo = animeRepo;
            _jikanApi = jikanApi;
        }

        // Obtener anime de la BD, si no existe, obtener desde Jikan
        public async Task<Anime?> GetOrImportAnimeAsync(int jikanId)
        {
            var localAnime = await _animeRepo.GetAnimeByJikanIdAsync(jikanId);
            if (localAnime != null)
                return localAnime;

            var externalDto = await _jikanApi.GetAnimeByJikanIdAsync(jikanId);
            if (externalDto == null)
                return null;

            var newAnime = MapDtoToEntity(externalDto);

            await _animeRepo.AddAnimeToDbAsync(newAnime);

            return newAnime;
        }

        // Búsqueda en Jikan
        public async Task<IEnumerable<Anime?>> SearchExternalAsync(string animeName)
        {
            var externalDtos = await _jikanApi.SearchAnimesAsync(animeName);

            return externalDtos.Select(MapDtoToEntity);
        }

        // Mapear los datos recibidos a la entidad de Anime
        private Anime MapDtoToEntity(AnimeExternalDto dto)
        {
            return new Anime
            {
                JikanId = dto.MalId,
                Title = dto.Title,
                EnglishTitle = dto.TitleEnglish,
                Synopsis = dto.Synopsis,
                Type = dto.Type ?? "Unknown",
                Episodes = dto.Episodes ?? 0,
                ImageUrl = dto.Images?.Jpg?.ImageUrl ?? dto.Images?.Jpg?.ImageUrl,
                Score = dto.Score ?? 0.0,
                Status = dto.Status ?? "Unknown"
            };
        }
    }
}