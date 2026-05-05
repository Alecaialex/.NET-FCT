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

        public async Task<IEnumerable<AnimeExternalDto>> GetTopAnimesAsync(int limit = 25)
        {
            try
            {
                _logger.LogInformation("Consultando Top Animes en Jikan. Límite solicitado: {Limit}", limit);

                var client = GetClient();
                var response = await client.GetFromJsonAsync<TopAnimeResponseDto>($"top/anime?limit={limit}");

                if (response?.Data == null)
                {
                    _logger.LogWarning("La respuesta de Jikan para Top Animes fue exitosa pero no contenía datos.");
                    return Enumerable.Empty<AnimeExternalDto>();
                }

                _logger.LogInformation("Se recuperaron exitosamente {Count} animes del Top de Jikan.", response.Data.Count());
                return response.Data;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "ERROR CONEXIÓN HTTP: No se pudo contactar con Jikan para obtener el Top Animes.");
                return Enumerable.Empty<AnimeExternalDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al procesar el top de animes desde Jikan.");
                return Enumerable.Empty<AnimeExternalDto>();
            }
        }

        public async Task<AnimeExternalDto?> GetAnimeByJikanIdAsync(int jikanId)
        {
            try
            {
                _logger.LogInformation("Solicitando información detallada a Jikan para el ID: {Id}", jikanId);

                var client = GetClient();
                var url = $"anime/{jikanId}/full";

                var response = await client.GetFromJsonAsync<AnimeResponseDataDto>(url);

                if (response?.Data == null)
                {
                    _logger.LogWarning("Jikan no encontró información para el anime con ID: {Id}", jikanId);
                    return null;
                }

                _logger.LogDebug("Información de '{Title}' recuperada con éxito de Jikan.", response.Data.Title);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el anime con ID {Id} desde Jikan.", jikanId);
                return null;
            }
        }

        public async Task<IEnumerable<AnimeExternalDto>> SearchAnimesAsync(string query)
        {
            try
            {
                _logger.LogInformation("Iniciando búsqueda externa en Jikan con la query: '{Query}'", query);

                var client = GetClient();
                var response = await client.GetFromJsonAsync<TopAnimeResponseDto>(
                    $"anime?q={Uri.EscapeDataString(query)}");

                if (response?.Data == null || !response.Data.Any())
                {
                    _logger.LogInformation("La búsqueda en Jikan para '{Query}' no obtuvo resultados.", query);
                    return Enumerable.Empty<AnimeExternalDto>();
                }

                _logger.LogInformation("Búsqueda en Jikan finalizada. Encontrados {Count} resultados para '{Query}'.",
                    response.Data.Count(), query);

                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar animes por nombre '{Query}' en Jikan.", query);
                return Enumerable.Empty<AnimeExternalDto>();
            }
        }
    }
}