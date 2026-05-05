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

        /// <summary>
        /// Busca un anime en la BD local; si no existe, lo importa de Jikan, lo guarda y lo devuelve.
        /// Garantiza que el retorno sea siempre la entidad local 'Anime'.
        /// </summary>
        public async Task<Anime> GetOrImportAnimeAsync(int jikanId)
        {
            // 1. Intentar buscar en BD local
            var localAnime = await _animeRepo.GetAnimeByJikanIdAsync(jikanId);
            if (localAnime != null) return localAnime;

            // 2. Si no existe, llamar a Jikan
            var externalDto = await _jikanApi.GetAnimeByJikanIdAsync(jikanId);
            if (externalDto == null) throw new KeyNotFoundException($"Anime con ID {jikanId} no encontrado en Jikan.");

            // 3. Mapear DTO a Entidad local usando el método centralizado
            var newAnime = MapDtoToEntity(externalDto);

            // 4. Guardar en BD para futuras consultas
            await _animeRepo.AddAnimeToDbAsync(newAnime);

            return newAnime;
        }

        /// <summary>
        /// Realiza una búsqueda únicamente en la base de datos local.
        /// </summary>
        public async Task<IEnumerable<Anime>> SearchLocalAsync(string query)
        {
            logger.Info("Ejecutando búsqueda local en BD con query: {query}", query);
            return await _animeRepo.SearchAnimesAsync(query);
        }

        /// <summary>
        /// Realiza una búsqueda en la API externa y mapea los resultados a entidades 'Anime'.
        /// Esto soluciona el error CS0738 al coincidir con el retorno esperado por la interfaz.
        /// </summary>
        public async Task<IEnumerable<Anime>> SearchExternalAsync(string query)
        {
            logger.Info("Ejecutando búsqueda externa en Jikan con query: {query}", query);

            var externalDtos = await _jikanApi.SearchAnimesAsync(query);

            // Mapeamos cada DTO de la lista a nuestra entidad local 'Anime'
            return externalDtos.Select(MapDtoToEntity);
        }
        public async Task<IEnumerable<AnimeExternalDto>> GetTopAnimesAsync(int limit)
        {
            return await _jikanApi.GetTopAnimesAsync(limit);
        }

        /// <summary>
        /// Método privado para centralizar la lógica de mapeo y evitar código repetido.
        /// </summary>
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