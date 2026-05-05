using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Shiori.Core.Interfaces;
using Shiori.Core.Configurations;

namespace Shiori.Infra.Workers
{
    public class TopAnimeUpdateService : BackgroundService
    {
        private readonly IJikanApiService _jikanService;
        private readonly ILogger<TopAnimeUpdateService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly AppConfig _config;

        public TopAnimeUpdateService(IJikanApiService jikanService, IServiceScopeFactory scopeFactory, ILogger<TopAnimeUpdateService> logger, AppConfig config)
        {
            _jikanService = jikanService;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var time = _config.UpdateIntervalHours > 0 ? _config.UpdateIntervalHours : 24;
            var delay = TimeSpan.FromHours(time);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Actualizando top animes desde Jikan...");

                    // Obtenemos los DTOs de la API
                    var topAnimes = await _jikanService.GetTopAnimesAsync();

                    if (topAnimes != null && topAnimes.Any())
                    {
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var animeService = scope.ServiceProvider.GetRequiredService<IAnimeService>();

                            foreach (var extAnime in topAnimes)
                            {
                                await animeService.GetOrImportAnimeAsync(extAnime.MalId);
                            }
                        }

                        _logger.LogInformation("Proceso de actualización de Top Animes finalizado.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al actualizar el top de animes desde Jikan.");
                }

                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}