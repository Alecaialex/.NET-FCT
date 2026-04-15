using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace BibliotecaAPI.Utilidades
{
    public class FiltroTiempoEjecucion : IAsyncActionFilter
    {
        private readonly ILogger<FiltroTiempoEjecucion> logger;

        public FiltroTiempoEjecucion(ILogger<FiltroTiempoEjecucion> logger)
        {
            this.logger = logger;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var stopwatch = Stopwatch.StartNew();
            logger.LogInformation($"Inicio acción: {context.ActionDescriptor.DisplayName}");

            await next();

            stopwatch.Stop();
            logger.LogInformation($"Fin acción: {context.ActionDescriptor.DisplayName} - {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
