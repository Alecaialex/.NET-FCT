using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shiori.Infra.BackgroundServices
{
    public class TopAnimeUpdateService : BackgroundService
    {
        private readonly IJikanApiService _jikanService;
        private readonly ILogger<TopAnimeUpdateService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(3);

        public TopAnimeUpdateService(IJikanApiService jikanService, IServiceScopeFactory scopeFactory, ILogger<TopAnimeUpdateService> logger)
        {
            _jikanService = jikanService;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Actualizando top animes desde Jikan...");

                    var topAnimes = await _jikanService.GetTopAnimesAsync();

                    if (topAnimes != null && topAnimes.Any())
                    {
                        int addedCount = 0;
                        int updatedCount = 0;

                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var _animeRepository = scope.ServiceProvider.GetRequiredService<IAnimeRepository>();

                            foreach (var extAnime in topAnimes)
                            {
                                var existing = await _animeRepository.GetAnimeByJikanIdAsync(extAnime.MalId);

                                if (existing == null)
                                {
                                    bool isAdded = await _animeRepository.AddAnimeToDbAsync(extAnime);

                                    if (isAdded)
                                    {
                                        addedCount++;
                                    }
                                }
                                else
                                {
                                    bool isUpdated = await _animeRepository.UpdateAnimeAsync(extAnime);
                                    if (isUpdated)
                                    {
                                        updatedCount++;
                                    }
                                }
                            }
                        }

                        _logger.LogInformation($"Top animes actualizados. Agregados: {addedCount}, Actualizados: {updatedCount}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al actualizar el top de animes desde Jikan.");
                }

                await Task.Delay(_updateInterval, stoppingToken);
            }
        }
    }
}
