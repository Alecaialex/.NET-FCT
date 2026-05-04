using Shiori.Core.DTOs;
using Shiori.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;

namespace Shiori.Infra.Services
{
    public class JikanApiService : IJikanApiService
    {
        private readonly HttpClient _httpClient;

        public JikanApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<AnimeExternalDto>> GetTopAnimesAsync(int limit = 25)
        {
            var response = await _httpClient.GetFromJsonAsync<TopAnimeResponseDto>(
                $"top/anime?limit={limit}");

            return response?.Data ?? new List<AnimeExternalDto>();
        }

        // Obtener un anime por su ID de Jikan
        public async Task<AnimeExternalDto?> GetAnimeByJikanIdAsync(int jikanId)
        {
            var url = $"anime/{jikanId}/full";
            var response = await _httpClient.GetFromJsonAsync<AnimeResponseDataDto>(url);
            Console.WriteLine(response?.Data);
            var test = await _httpClient.GetStringAsync(url);
            Console.WriteLine(test);
            return response?.Data;
        }

        // Búsqueda de animes por nombre a Jikan
        public async Task<IEnumerable<AnimeExternalDto>> SearchAnimesAsync(string query)
        {
            var response = await _httpClient.GetFromJsonAsync<TopAnimeResponseDto>(
                $"anime?q={Uri.EscapeDataString(query)}");

            return response?.Data ?? new List<AnimeExternalDto>();
        }

    }
}
