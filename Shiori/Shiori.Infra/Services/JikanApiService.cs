using Shiori.Core.DTOs;
using Shiori.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Linq;

namespace Shiori.Infra.Services
{
    public class JikanApiService : IJikanApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<JikanApiService> _logger;
        private const string ClientName = "JikanClient";

        public JikanApiService(IHttpClientFactory httpClientFactory, ILogger<JikanApiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private HttpClient GetClient() => _httpClientFactory.CreateClient(ClientName);

        // Obtener el top de animes desde Jikan
        public async Task<IEnumerable<AnimeExternalDto>> GetTopAnimesAsync(int limit = 25)
        {
            try
            {
                var client = GetClient();
                var response = await client.GetFromJsonAsync<TopAnimeResponseDto>($"top/anime?limit={limit}");

                if (response?.Data == null)
                    return Enumerable.Empty<AnimeExternalDto>();

                return response.Data;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "No se pudo contactar con Jikan para obtener el Top Animes");
                return Enumerable.Empty<AnimeExternalDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al procesar el top de animes desde Jikan");
                return Enumerable.Empty<AnimeExternalDto>();
            }
        }

        // Obtener anime por id desde Jikan
        public async Task<AnimeExternalDto?> GetAnimeByJikanIdAsync(int jikanId)
        {
            try
            {
                var client = GetClient();
                var url = $"anime/{jikanId}/full";

                var response = await client.GetFromJsonAsync<AnimeResponseDataDto>(url);

                if (response?.Data == null)
                    return null;

                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el anime con ID {Id} desde Jikan", jikanId);
                return null;
            }
        }

        // Buscar animes por nombre en Jikan
        public async Task<IEnumerable<AnimeExternalDto>> SearchAnimesAsync(string animeName)
        {
            try
            {
                var client = GetClient();
                var response = await client.GetFromJsonAsync<TopAnimeResponseDto>(
                    $"anime?q={Uri.EscapeDataString(animeName)}");

                if (response?.Data == null || !response.Data.Any())
                    return Enumerable.Empty<AnimeExternalDto>();

                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar animes por nombre '{0}' en Jikan", animeName);
                return Enumerable.Empty<AnimeExternalDto>();
            }
        }
    }
}